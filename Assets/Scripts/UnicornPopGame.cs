using System.Collections.Generic;
using UnityEngine;

// Entire game is built at runtime from procedural sprites, so no art assets are required.
public sealed class UnicornPopGame : MonoBehaviour
{
    private enum State { Title, Playing, GameOver }

    private sealed class Balloon
    {
        public Transform Root;
        public Transform String;
        public Vector2 Velocity;
        public float FloatSpeed;
        public float SwayPhase;
        public float SwayAmount;
        public bool Golden;
        public float SpawnX;
    }

    private sealed class Particle
    {
        public Transform T;
        public SpriteRenderer R;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Gravity;
    }

    private const string HighScoreKey = "UnicornPop.HighScore";
    private const float WalkwayY = -2.2f;
    private const float UnicornMinX = -3.6f;
    private const float UnicornMaxX = 3.4f;
    private const float MoveSpeed = 7.5f;
    private const float Gravity = -34f;
    private const float JumpSpeed = 13.5f;
    private const int StartingLives = 3;

    private static readonly Color[] BalloonColors =
    {
        new Color(1f, 0.35f, 0.45f), new Color(0.35f, 0.7f, 1f), new Color(0.55f, 0.9f, 0.45f),
        new Color(1f, 0.6f, 0.2f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.55f, 0.85f)
    };

    private static readonly Color[] RainbowColors =
    {
        new Color(1f, 0.3f, 0.4f), new Color(1f, 0.65f, 0.25f), new Color(1f, 0.9f, 0.3f),
        new Color(0.4f, 0.85f, 0.45f), new Color(0.35f, 0.7f, 1f), new Color(0.7f, 0.45f, 1f)
    };

    private Sprite circle, square, triangle;
    private Texture2D heartTex;
    private Camera cam;
    private AudioSource audioSource;
    private AudioClip popClip, goldenClip, boomClip, jumpClip, loseClip;

    private Transform unicorn, unicornBody, hornBase, hornMid, hornTip, cannonBarrel, cannonRoot;
    private Transform[] legs;
    private Transform[] tailPieces;
    private Vector3 cannonBarrelRest;

    private readonly List<Balloon> balloons = new List<Balloon>();
    private readonly List<Particle> particles = new List<Particle>();
    private readonly List<Transform> clouds = new List<Transform>();

    private State state = State.Title;
    private float unicornX, unicornVy, unicornHeight;
    private int jumpsUsed;
    private int facing = 1;
    private float walkCycle;
    private float pressTime, pressStartX;
    private bool pressing;
    private float tapMoveTarget = float.NaN;

    private int score, lives, combo, best, popped;
    private float comboTimer, spawnTimer, playTime, recoil, invulnerable, shake;
    private float lastAspect;

    private GUIStyle bigStyle, midStyle, smallStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<UnicornPopGame>() == null)
        {
            new GameObject("UnicornPop").AddComponent<UnicornPopGame>();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        best = PlayerPrefs.GetInt(HighScoreKey, 0);
        BuildSprites();
        BuildAudio();
        BuildCamera();
        BuildWorld();
        BuildUnicorn();
        ResetRound();
    }

    // ---------- Construction ----------

    private void BuildSprites()
    {
        circle = MakeSprite(64, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 32f), 64f);
        square = MakeSprite(4, (x, y) => 1f, 4f);
        triangle = MakeSprite(64, (x, y) =>
        {
            // Apex at the top, base at the bottom.
            var half = (1f - y) * 0.5f;
            return Mathf.Clamp01((half - Mathf.Abs(x)) * 16f) * (y <= 1f ? 1f : 0f);
        }, 64f);

        heartTex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (var py = 0; py < 64; py++)
        {
            for (var px = 0; px < 64; px++)
            {
                var x = (px / 63f * 2f - 1f) * 1.25f;
                var y = (py / 63f * 2f - 1f) * 1.25f + 0.1f;
                var f = Mathf.Pow(x * x + y * y - 1f, 3f) - x * x * y * y * y;
                heartTex.SetPixel(px, py, new Color(1f, 1f, 1f, f < 0f ? 1f : 0f));
            }
        }
        heartTex.Apply();
    }

    private static Sprite MakeSprite(int size, System.Func<float, float, float> alpha, float ppu)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (var py = 0; py < size; py++)
        {
            for (var px = 0; px < size; px++)
            {
                var x = (px + 0.5f) / size * 2f - 1f;
                var y = (py + 0.5f) / size * 2f - 1f;
                // The triangle callback expects y in [-1,1] with +1 as the apex row.
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha(x, y)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    private void BuildAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        popClip = MakeClip(0.18f, (t, n) => (Mathf.Sin(t * Mathf.PI * 2f * (500f + t * 3000f)) * 0.5f + n * 0.5f) * Mathf.Exp(-t * 24f));
        goldenClip = MakeClip(0.5f, (t, n) => (Mathf.Sin(t * Mathf.PI * 2f * 880f) + Mathf.Sin(t * Mathf.PI * 2f * 1320f)) * 0.3f * Mathf.Exp(-t * 6f));
        boomClip = MakeClip(0.25f, (t, n) => (Mathf.Sin(t * Mathf.PI * 2f * 90f) * 0.7f + n * 0.3f) * Mathf.Exp(-t * 14f));
        jumpClip = MakeClip(0.12f, (t, n) => Mathf.Sin(t * Mathf.PI * 2f * (300f + t * 2500f)) * 0.3f * Mathf.Exp(-t * 14f));
        loseClip = MakeClip(0.4f, (t, n) => Mathf.Sin(t * Mathf.PI * 2f * (320f - t * 450f)) * 0.4f * Mathf.Exp(-t * 5f));
    }

    private static AudioClip MakeClip(float seconds, System.Func<float, float, float> sample)
    {
        const int rate = 44100;
        var data = new float[(int)(rate * seconds)];
        var rng = new System.Random(7);
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Clamp(sample(i / (float)rate, (float)(rng.NextDouble() * 2 - 1)), -1f, 1f) * 0.6f;
        }
        var clip = AudioClip.Create("sfx", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void BuildCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.8f, 1f);
        FitCamera();
    }

    private void FitCamera()
    {
        lastAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        cam.orthographicSize = Mathf.Max(5f, 5.4f / lastAspect);
    }

    private SpriteRenderer Spr(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, float rot = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, 0, rot);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return r;
    }

    private void BuildWorld()
    {
        var world = new GameObject("World").transform;
        world.SetParent(transform, false);

        // Soft sky bands and clouds.
        Spr("SkyHigh", world, square, new Vector2(0, 8), new Vector2(80, 14), new Color(0.42f, 0.7f, 1f), -100);
        Spr("SkyLow", world, square, new Vector2(0, -2), new Vector2(80, 8), new Color(0.78f, 0.92f, 1f), -99);
        Spr("Sun", world, circle, new Vector2(-5.5f, 3.4f), new Vector2(1.8f, 1.8f), new Color(1f, 0.95f, 0.6f), -98);
        for (var i = 0; i < 6; i++)
        {
            var c = new GameObject("Cloud").transform;
            c.SetParent(world, false);
            c.position = new Vector3(Random.Range(-9f, 9f), Random.Range(0.5f, 4.2f), 0);
            var s = Random.Range(0.8f, 1.4f);
            for (var k = 0; k < 3; k++)
            {
                Spr("p", c, circle, new Vector2((k - 1) * 0.7f * s, k == 1 ? 0.25f * s : 0f), new Vector2(1.1f * s, 0.8f * s), new Color(1f, 1f, 1f, 0.9f), -90);
            }
            clouds.Add(c);
        }

        // Distant hills.
        Spr("Hill1", world, circle, new Vector2(-6f, -5.2f), new Vector2(11f, 7f), new Color(0.55f, 0.82f, 0.55f), -50);
        Spr("Hill2", world, circle, new Vector2(6.5f, -5.6f), new Vector2(13f, 8f), new Color(0.45f, 0.75f, 0.5f), -49);

        // Castle.
        var stone = new Color(0.78f, 0.74f, 0.85f);
        var stoneDark = new Color(0.6f, 0.56f, 0.7f);
        Spr("CastleWall", world, square, new Vector2(0, WalkwayY - 15f), new Vector2(9.6f, 30f), stone, 0);
        Spr("Walkway", world, square, new Vector2(0, WalkwayY - 0.12f), new Vector2(9.6f, 0.24f), stoneDark, 2);
        for (var i = 0; i < 5; i++)
        {
            Spr("Brick", world, square, new Vector2(-3.8f + i * 1.9f, WalkwayY - 1.4f), new Vector2(1.5f, 0.5f), stoneDark, 1);
            Spr("Brick", world, square, new Vector2(-2.85f + i * 1.9f, WalkwayY - 2.4f), new Vector2(1.5f, 0.5f), stoneDark, 1);
        }
        Spr("Door", world, circle, new Vector2(0, WalkwayY - 3.4f), new Vector2(1.6f, 1.6f), new Color(0.45f, 0.3f, 0.35f), 1);
        Spr("DoorBase", world, square, new Vector2(0, WalkwayY - 4.2f), new Vector2(1.6f, 1.6f), new Color(0.45f, 0.3f, 0.35f), 1);

        BuildTower(world, -4.1f, stone, stoneDark, true);
        BuildTower(world, 4.1f, stone, stoneDark, false);

        // Cannon sits on top of the right tower.
        cannonRoot = new GameObject("Cannon").transform;
        cannonRoot.SetParent(world, false);
        cannonRoot.position = new Vector3(4.1f, 0.05f, 0);
        Spr("Wheel", cannonRoot, circle, new Vector2(0, 0.1f), new Vector2(0.7f, 0.7f), new Color(0.35f, 0.22f, 0.15f), 6);
        Spr("Hub", cannonRoot, circle, new Vector2(0, 0.1f), new Vector2(0.22f, 0.22f), new Color(0.85f, 0.7f, 0.3f), 8);
        var pivot = new GameObject("BarrelPivot").transform;
        pivot.SetParent(cannonRoot, false);
        pivot.localPosition = new Vector2(0, 0.15f);
        cannonBarrel = pivot;
        Spr("Barrel", pivot, square, new Vector2(0, 0.45f), new Vector2(0.42f, 1.0f), new Color(0.25f, 0.27f, 0.33f), 7);
        Spr("Muzzle", pivot, square, new Vector2(0, 0.95f), new Vector2(0.56f, 0.18f), new Color(0.15f, 0.16f, 0.2f), 8);
        Spr("Shine", pivot, square, new Vector2(-0.1f, 0.45f), new Vector2(0.06f, 0.8f), new Color(1f, 1f, 1f, 0.3f), 8);
        cannonBarrelRest = cannonBarrel.localPosition;
        cannonBarrel.localRotation = Quaternion.Euler(0, 0, 25f);
    }

    private void BuildTower(Transform world, float x, Color stone, Color dark, bool flag)
    {
        Spr("Tower", world, square, new Vector2(x, -1.2f - 7.5f), new Vector2(1.8f, 15f), stone, 3);
        Spr("TowerTop", world, square, new Vector2(x, -0.05f), new Vector2(2.1f, 0.3f), dark, 4);
        for (var i = 0; i < 3; i++)
        {
            Spr("Merlon", world, square, new Vector2(x - 0.8f + i * 0.8f, 0.3f), new Vector2(0.45f, 0.5f), dark, 4);
        }
        Spr("Window", world, circle, new Vector2(x, -1.5f), new Vector2(0.5f, 0.5f), new Color(0.3f, 0.25f, 0.45f), 5);
        if (flag)
        {
            Spr("Pole", world, square, new Vector2(x, 1.2f), new Vector2(0.07f, 2f), new Color(0.4f, 0.4f, 0.45f), 4);
            Spr("Pennant", world, triangle, new Vector2(x + 0.4f, 1.85f), new Vector2(0.5f, 0.8f), new Color(1f, 0.4f, 0.65f), 4, -90f);
        }
    }

    private void BuildUnicorn()
    {
        unicorn = new GameObject("Unicorn").transform;
        unicorn.SetParent(transform, false);
        unicornBody = new GameObject("Body").transform;
        unicornBody.SetParent(unicorn, false);

        var white = new Color(1f, 1f, 1f);
        var shade = new Color(0.88f, 0.86f, 0.97f);
        var pink = new Color(1f, 0.7f, 0.85f);
        const int o = 20;

        // Tail (rainbow, behind body).
        tailPieces = new Transform[RainbowColors.Length];
        for (var i = 0; i < RainbowColors.Length; i++)
        {
            var piece = Spr("Tail", unicornBody, circle, new Vector2(-0.6f - i * 0.05f, 0.78f - i * 0.1f), new Vector2(0.38f, 0.3f), RainbowColors[i], o - 3 - i % 2, 20f - i * 8f);
            tailPieces[i] = piece.transform;
        }

        // Back legs, then body, then front legs.
        legs = new Transform[4];
        var legX = new[] { -0.38f, -0.2f, 0.22f, 0.4f };
        for (var i = 0; i < 4; i++)
        {
            var pivot = new GameObject("Leg").transform;
            pivot.SetParent(unicornBody, false);
            pivot.localPosition = new Vector2(legX[i], 0.5f);
            Spr("Thigh", pivot, square, new Vector2(0, -0.22f), new Vector2(0.13f, 0.46f), i % 2 == 0 ? shade : white, o - 1);
            Spr("Hoof", pivot, square, new Vector2(0, -0.46f), new Vector2(0.15f, 0.1f), pink, o);
            legs[i] = pivot;
        }
        Spr("Torso", unicornBody, circle, new Vector2(0, 0.72f), new Vector2(1.25f, 0.62f), white, o + 1);
        Spr("Belly", unicornBody, circle, new Vector2(0, 0.6f), new Vector2(0.9f, 0.3f), shade, o + 1);
        Spr("Neck", unicornBody, circle, new Vector2(0.48f, 1.05f), new Vector2(0.38f, 0.75f), white, o + 1, -28f);

        // Rainbow mane down the neck.
        for (var i = 0; i < RainbowColors.Length; i++)
        {
            Spr("Mane", unicornBody, circle, new Vector2(0.22f - i * 0.0f + (i % 2) * 0.04f, 1.55f - i * 0.17f), new Vector2(0.3f, 0.2f), RainbowColors[i], o, 30f);
        }

        Spr("Head", unicornBody, circle, new Vector2(0.78f, 1.3f), new Vector2(0.56f, 0.46f), white, o + 3, 10f);
        Spr("Snout", unicornBody, circle, new Vector2(1.02f, 1.2f), new Vector2(0.3f, 0.26f), pink, o + 4, 10f);
        Spr("Nostril", unicornBody, circle, new Vector2(1.1f, 1.22f), new Vector2(0.04f, 0.04f), new Color(0.5f, 0.2f, 0.35f), o + 5);
        Spr("Ear", unicornBody, triangle, new Vector2(0.62f, 1.62f), new Vector2(0.14f, 0.26f), white, o + 2, -10f);
        Spr("Eye", unicornBody, circle, new Vector2(0.86f, 1.36f), new Vector2(0.13f, 0.15f), new Color(0.2f, 0.12f, 0.3f), o + 5);
        Spr("EyeShine", unicornBody, circle, new Vector2(0.88f, 1.39f), new Vector2(0.05f, 0.05f), Color.white, o + 6);
        Spr("Blush", unicornBody, circle, new Vector2(0.86f, 1.18f), new Vector2(0.14f, 0.09f), new Color(1f, 0.6f, 0.75f, 0.8f), o + 4);

        // Golden horn with a stripe, angled forward.
        var horn = Spr("Horn", unicornBody, triangle, new Vector2(0.9f, 1.72f), new Vector2(0.17f, 0.72f), new Color(1f, 0.85f, 0.3f), o + 4, -22f);
        Spr("HornStripe", horn.transform, square, new Vector2(0, -0.1f), new Vector2(0.55f, 0.07f), new Color(1f, 0.6f, 0.2f), o + 5);
        Spr("HornStripe", horn.transform, square, new Vector2(0, 0.2f), new Vector2(0.35f, 0.07f), new Color(1f, 0.6f, 0.2f), o + 5);
        hornBase = MakeMarker(horn.transform, new Vector2(0, -0.35f));
        hornMid = MakeMarker(horn.transform, new Vector2(0, 0.05f));
        hornTip = MakeMarker(horn.transform, new Vector2(0, 0.5f));
    }

    private static Transform MakeMarker(Transform parent, Vector2 localPos)
    {
        var t = new GameObject("Marker").transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    // ---------- Game flow ----------

    private void ResetRound()
    {
        foreach (var b in balloons) Destroy(b.Root.gameObject);
        balloons.Clear();
        score = 0;
        lives = StartingLives;
        combo = 0;
        popped = 0;
        playTime = 0f;
        spawnTimer = 1.2f;
        unicornX = 0f;
        unicornVy = 0f;
        unicornHeight = 0f;
        jumpsUsed = 0;
        invulnerable = 0f;
        tapMoveTarget = float.NaN;
    }

    private void StartGame()
    {
        ResetRound();
        state = State.Playing;
    }

    private void EndGame()
    {
        state = State.GameOver;
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt(HighScoreKey, best);
            PlayerPrefs.Save();
        }
    }

    private void Update()
    {
        var dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (Mathf.Abs((float)Screen.width / Mathf.Max(1, Screen.height) - lastAspect) > 0.001f) FitCamera();

        UpdateClouds(dt);
        UpdateParticles(dt);
        UpdateCannon(dt);

        if (state == State.Playing)
        {
            playTime += dt;
            UpdateInput(dt);
            UpdateUnicorn(dt);
            UpdateSpawning(dt);
            UpdateBalloons(dt);
            if (comboTimer > 0f) { comboTimer -= dt; if (comboTimer <= 0f) combo = 0; }
            if (invulnerable > 0f) invulnerable -= dt;
        }
        else
        {
            UpdateUnicornIdle(dt);
            UpdateBalloonsAfterGame(dt);
            var pressedStart = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
            if (pressedStart && (state == State.Title || Time.unscaledTime - gameOverTime > 0.6f)) StartGame();
        }

        shake = Mathf.MoveTowards(shake, 0f, dt * 2.5f);
        cam.transform.position = new Vector3(Random.Range(-shake, shake) * 0.3f, Random.Range(-shake, shake) * 0.3f, -10f);
    }

    private float gameOverTime;

    private void UpdateInput(float dt)
    {
        var dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;
        var jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        // Pointer: hold to run toward the finger, quick tap to jump.
        var worldX = cam.ScreenToWorldPoint(Input.mousePosition).x;
        if (Input.GetMouseButtonDown(0)) { pressing = true; pressTime = 0f; pressStartX = worldX; }
        if (pressing)
        {
            pressTime += dt;
            if (Input.GetMouseButton(0))
            {
                if (pressTime > 0.12f || Mathf.Abs(worldX - pressStartX) > 0.4f) tapMoveTarget = worldX;
            }
            if (Input.GetMouseButtonUp(0))
            {
                pressing = false;
                if (pressTime < 0.18f && Mathf.Abs(worldX - pressStartX) < 0.4f) jump = true;
                tapMoveTarget = float.NaN;
            }
        }

        if (dir == 0f && !float.IsNaN(tapMoveTarget))
        {
            var delta = tapMoveTarget - unicornX;
            if (Mathf.Abs(delta) > 0.15f) dir = Mathf.Sign(delta);
        }

        if (dir != 0f) facing = dir > 0 ? 1 : -1;
        unicornX = Mathf.Clamp(unicornX + dir * MoveSpeed * dt, UnicornMinX, UnicornMaxX);
        walkCycle += (dir != 0f && unicornHeight <= 0.001f ? 14f : 4f) * dt;
        walkingNow = dir != 0f;

        if (jump && jumpsUsed < 2)
        {
            unicornVy = JumpSpeed * (jumpsUsed == 0 ? 1f : 0.85f);
            if (jumpsUsed == 1) Burst(unicorn.position + Vector3.up * 0.2f, new Color(1f, 0.9f, 0.5f), 8, 3f);
            jumpsUsed++;
            audioSource.PlayOneShot(jumpClip, 0.5f);
        }
    }

    private bool walkingNow;

    private void UpdateUnicorn(float dt)
    {
        unicornVy += Gravity * dt;
        unicornHeight += unicornVy * dt;
        if (unicornHeight <= 0f)
        {
            unicornHeight = 0f;
            unicornVy = 0f;
            jumpsUsed = 0;
        }
        PoseUnicorn();
        CheckHornHits();
    }

    private void UpdateUnicornIdle(float dt)
    {
        walkingNow = false;
        walkCycle += 3f * dt;
        unicornVy = 0f;
        unicornHeight = 0f;
        PoseUnicorn();
    }

    private void PoseUnicorn()
    {
        unicorn.position = new Vector3(unicornX, WalkwayY + 0.46f + unicornHeight, 0);
        var sx = Mathf.Lerp(unicornBody.localScale.x, facing, 0.35f);
        var airborne = unicornHeight > 0.001f;
        var bob = airborne ? 0f : Mathf.Abs(Mathf.Sin(walkCycle)) * (walkingNow ? 0.07f : 0.02f);
        unicornBody.localScale = new Vector3(sx, 1f, 1f);
        unicornBody.localPosition = new Vector3(0, bob, 0);
        var tilt = airborne ? Mathf.Clamp(unicornVy * -0.6f, -12f, 12f) : 0f;
        unicornBody.localRotation = Quaternion.Euler(0, 0, tilt * (sx >= 0 ? 1 : -1) * 0.5f);

        for (var i = 0; i < legs.Length; i++)
        {
            var phase = i % 2 == 0 ? 0f : Mathf.PI;
            var swing = airborne ? (i < 2 ? -35f : 35f) : Mathf.Sin(walkCycle + phase) * (walkingNow ? 32f : 0f);
            legs[i].localRotation = Quaternion.Euler(0, 0, swing);
        }
        for (var i = 0; i < tailPieces.Length; i++)
        {
            var baseRot = 20f - i * 8f;
            tailPieces[i].localRotation = Quaternion.Euler(0, 0, baseRot + Mathf.Sin(Time.time * 6f + i * 0.5f) * 8f);
        }
    }

    private void CheckHornHits()
    {
        var points = new[] { hornBase.position, hornMid.position, hornTip.position };
        for (var i = balloons.Count - 1; i >= 0; i--)
        {
            var b = balloons[i];
            var c = b.Root.position;
            var rx = b.Golden ? 0.5f : 0.55f;
            var ry = b.Golden ? 0.6f : 0.68f;
            foreach (var p in points)
            {
                var dx = (p.x - c.x) / rx;
                var dy = (p.y - (c.y + 0.1f)) / ry;
                if (dx * dx + dy * dy < 1f)
                {
                    Pop(i);
                    break;
                }
            }
        }
    }

    private void Pop(int index)
    {
        var b = balloons[index];
        var pos = b.Root.position;
        var color = b.Root.GetComponentInChildren<SpriteRenderer>().color;
        combo = comboTimer > 0f ? combo + 1 : 1;
        comboTimer = 1.6f;
        var multiplier = Mathf.Min(combo, 8);
        var points = (b.Golden ? 5 : 1) * multiplier;
        score += points;
        popped++;
        Burst(pos, color, b.Golden ? 26 : 14, 5f);
        if (b.Golden) Burst(pos, new Color(1f, 0.95f, 0.5f), 12, 7f);
        audioSource.PlayOneShot(b.Golden ? goldenClip : popClip, 0.8f);
        shake = Mathf.Max(shake, 0.25f);
        floatingTexts.Add(new FloatText { Pos = pos, Text = "+" + points, Life = 0.9f, Golden = b.Golden });
        Destroy(b.Root.gameObject);
        balloons.RemoveAt(index);
    }

    // ---------- Balloons & cannon ----------

    private void UpdateSpawning(float dt)
    {
        spawnTimer -= dt;
        if (spawnTimer > 0f) return;
        var progress = Mathf.Clamp01(playTime / 100f);
        spawnTimer = Mathf.Lerp(1.7f, 0.6f, progress) * Random.Range(0.75f, 1.25f);
        FireBalloon(progress);
        // Occasional double shot later on.
        if (progress > 0.3f && Random.value < 0.25f * progress) StartCoroutine(DelayedShot(0.25f, progress));
    }

    private System.Collections.IEnumerator DelayedShot(float delay, float progress)
    {
        yield return new WaitForSeconds(delay);
        if (state == State.Playing) FireBalloon(progress);
    }

    private void FireBalloon(float progress)
    {
        var angle = Random.Range(100f, 165f);
        var speed = Random.Range(4.5f, 9f);
        var rad = angle * Mathf.Deg2Rad;
        var muzzle = cannonBarrel.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * 0.95f;

        var golden = Random.value < 0.08f;
        var b = new Balloon
        {
            Golden = golden,
            Velocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed,
            FloatSpeed = Random.Range(0.9f, 1.5f) + progress * 1.4f + (golden ? 0.7f : 0f),
            SwayPhase = Random.value * 6.28f,
            SwayAmount = Random.Range(0.3f, 0.8f)
        };
        var color = golden ? new Color(1f, 0.82f, 0.2f) : BalloonColors[Random.Range(0, BalloonColors.Length)];
        var scale = golden ? 0.85f : 1f;

        var root = new GameObject(golden ? "GoldenBalloon" : "Balloon").transform;
        root.SetParent(transform, false);
        root.position = muzzle;
        root.localScale = Vector3.one * scale;
        b.Root = root;
        Spr("Body", root, circle, Vector2.zero, new Vector2(0.95f, 1.2f), color, 30);
        Spr("Shine", root, circle, new Vector2(-0.18f, 0.25f), new Vector2(0.18f, 0.28f), new Color(1f, 1f, 1f, 0.55f), 31, 20f);
        Spr("Knot", root, triangle, new Vector2(0, -0.64f), new Vector2(0.16f, 0.14f), color * 0.85f, 30, 180f);
        var str = Spr("String", root, square, new Vector2(0, -1.0f), new Vector2(0.02f, 0.7f), new Color(0.35f, 0.35f, 0.4f), 29);
        b.String = str.transform;
        balloons.Add(b);

        recoil = 1f;
        shake = Mathf.Max(shake, 0.35f);
        cannonBarrel.localRotation = Quaternion.Euler(0, 0, angle - 90f);
        Burst(muzzle, new Color(0.9f, 0.9f, 0.95f), 7, 2.5f, 0.6f);
        audioSource.PlayOneShot(boomClip, 0.6f);
    }

    private void UpdateCannon(float dt)
    {
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 5f);
        cannonBarrel.localPosition = cannonBarrelRest + cannonBarrel.localRotation * Vector3.down * (recoil * 0.18f);
    }

    private void UpdateBalloons(float dt)
    {
        var top = cam.orthographicSize + 1.2f;
        for (var i = balloons.Count - 1; i >= 0; i--)
        {
            var b = balloons[i];
            MoveBalloon(b, dt);
            if (b.Root.position.y > top)
            {
                if (!b.Golden) LoseLife();
                Destroy(b.Root.gameObject);
                balloons.RemoveAt(i);
                if (state != State.Playing) return;
            }
        }
    }

    private void UpdateBalloonsAfterGame(float dt)
    {
        var top = cam.orthographicSize + 1.5f;
        for (var i = balloons.Count - 1; i >= 0; i--)
        {
            MoveBalloon(balloons[i], dt);
            if (balloons[i].Root.position.y > top)
            {
                Destroy(balloons[i].Root.gameObject);
                balloons.RemoveAt(i);
            }
        }
    }

    private static void MoveBalloon(Balloon b, float dt)
    {
        var sway = Mathf.Sin(Time.time * 2f + b.SwayPhase) * b.SwayAmount;
        var target = new Vector2(sway, b.FloatSpeed);
        b.Velocity = Vector2.Lerp(b.Velocity, target, 1f - Mathf.Exp(-1.4f * dt));
        b.Root.position += (Vector3)(b.Velocity * dt);
        b.Root.rotation = Quaternion.Euler(0, 0, -b.Velocity.x * 3f);
        b.String.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 4f + b.SwayPhase) * 8f);
    }

    private void LoseLife()
    {
        lives--;
        combo = 0;
        comboTimer = 0f;
        shake = 0.8f;
        audioSource.PlayOneShot(loseClip, 0.7f);
        if (lives <= 0)
        {
            gameOverTime = Time.unscaledTime;
            EndGame();
        }
    }

    // ---------- Effects ----------

    private sealed class FloatText { public Vector3 Pos; public string Text; public float Life; public bool Golden; }
    private readonly List<FloatText> floatingTexts = new List<FloatText>();

    private void Burst(Vector3 pos, Color color, int count, float speed, float life = 0.7f)
    {
        for (var i = 0; i < count; i++)
        {
            var r = Spr("Particle", transform, i % 3 == 0 ? triangle : circle, pos, Vector2.one * Random.Range(0.1f, 0.22f), color, 40, Random.value * 360f);
            var dir = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * speed;
            particles.Add(new Particle { T = r.transform, R = r, Velocity = dir, Life = life, MaxLife = life, Gravity = -6f });
        }
    }

    private void UpdateParticles(float dt)
    {
        for (var i = particles.Count - 1; i >= 0; i--)
        {
            var p = particles[i];
            p.Life -= dt;
            if (p.Life <= 0f) { Destroy(p.T.gameObject); particles.RemoveAt(i); continue; }
            p.Velocity.y += p.Gravity * dt;
            p.T.position += (Vector3)(p.Velocity * dt);
            p.T.Rotate(0, 0, 240f * dt);
            var c = p.R.color;
            c.a = p.Life / p.MaxLife;
            p.R.color = c;
        }
        for (var i = floatingTexts.Count - 1; i >= 0; i--)
        {
            floatingTexts[i].Life -= dt;
            floatingTexts[i].Pos += Vector3.up * dt * 1.5f;
            if (floatingTexts[i].Life <= 0f) floatingTexts.RemoveAt(i);
        }
    }

    private void UpdateClouds(float dt)
    {
        var half = cam.orthographicSize * lastAspect + 2f;
        foreach (var c in clouds)
        {
            c.position += Vector3.right * 0.25f * dt;
            if (c.position.x > half) c.position = new Vector3(-half, c.position.y, 0);
        }
    }

    // ---------- UI ----------

    private void EnsureStyles(float s)
    {
        if (bigStyle != null && Mathf.Approximately(styleScale, s)) return;
        styleScale = s;
        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        midStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }

    private float styleScale;

    private void Label(Rect r, string text, GUIStyle style, Color color)
    {
        var shadow = new Rect(r.x + 2, r.y + 2, r.width, r.height);
        GUI.color = new Color(0.2f, 0.1f, 0.4f, 0.7f);
        GUI.Label(shadow, text, style);
        GUI.color = color;
        GUI.Label(r, text, style);
        GUI.color = Color.white;
    }

    private void OnGUI()
    {
        var s = Mathf.Max(0.6f, Screen.height / 600f);
        EnsureStyles(s);
        float w = Screen.width, h = Screen.height;

        Label(new Rect(10 * s, 8 * s, 260 * s, 40 * s), "Score " + score, midStyle, Color.white);
        Label(new Rect(10 * s, 44 * s, 260 * s, 30 * s), "Best " + Mathf.Max(best, score), smallStyle, new Color(1f, 0.95f, 0.6f));
        for (var i = 0; i < StartingLives; i++)
        {
            GUI.color = i < lives ? new Color(1f, 0.3f, 0.5f) : new Color(1f, 1f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(w - (StartingLives - i) * 44 * s - 6 * s, 10 * s, 38 * s, 38 * s), heartTex);
        }
        GUI.color = Color.white;

        if (state == State.Playing && combo >= 2)
        {
            Label(new Rect(w * 0.5f - 150 * s, 10 * s, 300 * s, 40 * s), "Combo x" + Mathf.Min(combo, 8) + "!", midStyle, new Color(1f, 0.85f, 0.3f));
        }

        foreach (var ft in floatingTexts)
        {
            var sp = cam.WorldToScreenPoint(ft.Pos);
            Label(new Rect(sp.x - 60 * s, h - sp.y - 20 * s, 120 * s, 40 * s), ft.Text, midStyle, ft.Golden ? new Color(1f, 0.9f, 0.3f) : Color.white);
        }

        if (state == State.Title)
        {
            Label(new Rect(0, h * 0.12f, w, 90 * s), "Unicorn Pop!", bigStyle, new Color(1f, 0.6f, 0.9f));
            Label(new Rect(0, h * 0.12f + 80 * s, w, 40 * s), "Pop the balloons with your horn!", midStyle, Color.white);
            Label(new Rect(0, h * 0.12f + 125 * s, w, 30 * s), "A/D or arrows to run  -  Space to jump (twice for a double jump)", smallStyle, Color.white);
            Label(new Rect(0, h * 0.12f + 150 * s, w, 30 * s), "Touch: hold to run, tap to jump  -  Golden balloons are worth 5", smallStyle, Color.white);
            Label(new Rect(0, h * 0.12f + 195 * s, w, 40 * s), "Press Space or tap to start", midStyle, new Color(1f, 0.95f, 0.5f));
        }
        else if (state == State.GameOver)
        {
            Label(new Rect(0, h * 0.15f, w, 90 * s), "Game Over", bigStyle, new Color(1f, 0.5f, 0.6f));
            Label(new Rect(0, h * 0.15f + 85 * s, w, 40 * s), "Score " + score + "   Popped " + popped, midStyle, Color.white);
            if (score >= best && score > 0) Label(new Rect(0, h * 0.15f + 125 * s, w, 40 * s), "New best!", midStyle, new Color(1f, 0.9f, 0.3f));
            Label(new Rect(0, h * 0.15f + 170 * s, w, 40 * s), "Press Space or tap to play again", midStyle, new Color(1f, 0.95f, 0.5f));
        }
    }
}
