using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 3D world: meadow, a castle in the distance with a jester and cannon on its wall, and a chase camera.
public sealed class UnicornPopGame : MonoBehaviour
{
    public const float CastleZ = 62f;
    public const float WallTopY = 9f;
    private const float CrateX = 14f;

    private static readonly Color Sky = new Color(0.62f, 0.82f, 1f);

    private Camera cam;
    private Unicorn unicorn;
    private Transform jester;
    private Transform cannonBarrel;
    private Transform cannonMuzzle;
    private readonly List<Transform> clouds = new List<Transform>();
    private Cannon cannon;
    private Effects effects;
    private GUIStyle hint, scoreStyle, bigStyle, midStyle;
    private Texture2D heartTex;

    private const int StartHearts = 3;
    private const int MaxCombo = 8;
    private const float ComboWindow = 2.2f;
    private const float HitRadius = 1.3f;

    private const float LevelSeconds = 20f;
    private int level = 1;
    private float playTime;
    private float levelBanner;
    private Transform king;
    private Transform kingArmL, kingArmR;
    private float kingCheer, kingGrump;
    private Vector3 kingHome;

    private int score;
    private int best;
    private bool newRecord;
    private int hearts = StartHearts;
    private int combo;
    private float comboTimer;
    private bool gameOver;
    private bool onTitle;
    private int startFrame;
    private Vector3 prevTip;
    private bool hasPrevTip;

    private const string BestKey = "UnicornPopBest";
    private static readonly Color[] Fireworks =
    {
        new Color(1f, 0.4f, 0.5f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.9f, 0.6f),
        new Color(0.4f, 0.7f, 1f), new Color(0.8f, 0.5f, 1f),
    };

    // A restart (R) goes straight back into the game instead of showing the title again.
    private static bool skipTitle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => skipTitle = false;

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
        Random.InitState(12345);
        best = PlayerPrefs.GetInt(BestKey, 0);
        BuildEnvironment();
        BuildCamera();
        effects = new GameObject("Effects").AddComponent<Effects>();
        effects.transform.SetParent(transform, false);
        gameObject.AddComponent<Sfx>();
        heartTex = MakeHeart();
        Balloon.Escaped += OnBalloonEscaped;

        var world = new GameObject("World").transform;
        world.SetParent(transform, false);
        BuildGround(world);
        BuildClouds(world);
        BuildCastle(world);
        BuildCannonAndJester(world);

        unicorn = new GameObject("Unicorn").AddComponent<Unicorn>();
        unicorn.transform.SetParent(transform, false);
        unicorn.Build(new Vector3(0, 0, -5f), 0f);

        // Scenery is seeded for a stable look; gameplay randomness is not.
        Random.InitState(System.Environment.TickCount);

        cannon = new GameObject("CannonController").AddComponent<Cannon>();
        cannon.transform.SetParent(transform, false);
        cannon.Active = false;
        cannon.Build(world, jester, cannonBarrel, cannonMuzzle, unicorn.transform, CrateX, 9.8f, 5.5f);

        onTitle = !skipTitle;
        unicorn.Controllable = !onTitle;
        cannon.Active = !onTitle;
        SnapCamera();
    }

    private void OnDestroy()
    {
        Balloon.Escaped -= OnBalloonEscaped;
    }

    private void StartGame()
    {
        onTitle = false;
        skipTitle = true;
        Balloon.RiseScale = 1f;
        cannon.Active = true;
        startFrame = Time.frameCount;
        Sfx.Start();
    }

    // ---------- Setup ----------

    private void BuildEnvironment()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.85f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.78f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Sky;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 190f;
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
        cam.orthographic = false;
        cam.fieldOfView = 62f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;
    }

    private void BuildGround(Transform world)
    {
        Prim.Make(PrimitiveType.Cube, "Ground", world, new Vector3(0, -0.5f, 30f), new Vector3(600f, 1f, 600f), new Color(0.45f, 0.78f, 0.4f));
        Prim.Make(PrimitiveType.Cube, "Path", world, new Vector3(0, 0.02f, 25f), new Vector3(5f, 0.04f, 60f), new Color(0.9f, 0.82f, 0.6f));

        var hillColors = new[] { new Color(0.4f, 0.72f, 0.4f), new Color(0.36f, 0.66f, 0.45f) };
        for (var i = 0; i < 14; i++)
        {
            var side = i % 2 == 0 ? -1f : 1f;
            var x = side * Random.Range(55f, 130f);
            var z = Random.Range(-40f, 140f);
            var r = Random.Range(18f, 40f);
            Prim.Make(PrimitiveType.Sphere, "Hill", world, new Vector3(x, -r * 0.55f, z), new Vector3(r * 2f, r * 1.2f, r * 2f), hillColors[i % 2]);
        }

        for (var i = 0; i < 70; i++)
        {
            var x = Random.Range(-50f, 50f);
            var z = Random.Range(-28f, 52f);
            if (Mathf.Abs(x) < 5f && z > -10f) continue;
            var pos = new Vector3(x, 0, z);
            var yaw = Random.Range(0f, 360f);
            var scale = Random.Range(0.85f, 1.5f);
            var palette = TreePalettes[Random.Range(0, TreePalettes.Length)];
            if (i % 3 == 0) BuildPine(world, pos, scale, palette);
            else BuildOak(world, pos, scale, yaw, palette);
        }
    }

    private static readonly Color Bark = new Color(0.42f, 0.28f, 0.17f);

    // Each entry is a dark and a light shade; the mix of storybook colours keeps the meadow whimsical.
    private static readonly Color[][] TreePalettes =
    {
        new[] { new Color(0.3f, 0.62f, 0.3f), new Color(0.4f, 0.74f, 0.36f) },
        new[] { new Color(0.3f, 0.62f, 0.3f), new Color(0.4f, 0.74f, 0.36f) },
        new[] { new Color(0.98f, 0.62f, 0.78f), new Color(1f, 0.82f, 0.9f) },
        new[] { new Color(0.98f, 0.62f, 0.78f), new Color(1f, 0.82f, 0.9f) },
        new[] { new Color(0.7f, 0.55f, 0.95f), new Color(0.85f, 0.75f, 1f) },
        new[] { new Color(0.3f, 0.78f, 0.78f), new Color(0.55f, 0.9f, 0.88f) },
        new[] { new Color(1f, 0.72f, 0.3f), new Color(1f, 0.86f, 0.5f) },
        new[] { new Color(0.55f, 0.85f, 0.5f), new Color(0.75f, 0.95f, 0.6f) },
    };

    // Stacked cones that get narrower toward the top, on a thin trunk.
    private static void BuildPine(Transform world, Vector3 pos, float scale, Color[] palette)
    {
        var tree = new GameObject("Pine").transform;
        tree.SetParent(world, false);
        tree.position = pos;
        tree.localScale = Vector3.one * scale;

        Prim.Make(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.9f, 0.45f), Bark);
        for (var t = 0; t < 4; t++)
        {
            var w = 3.6f - t * 0.8f;
            var h = 2.1f - t * 0.15f;
            Prim.Cone("Tier", tree, new Vector3(0, 1.7f + t * 1.15f + h * 0.5f, 0), new Vector3(w, h, w), Color.Lerp(palette[0] * 0.85f, palette[1], t / 3f));
        }
    }

    // Flared trunk, a couple of branches and a canopy made of several overlapping blobs.
    private static void BuildOak(Transform world, Vector3 pos, float scale, float yaw, Color[] palette)
    {
        var tree = new GameObject("Oak").transform;
        tree.SetParent(world, false);
        tree.position = pos;
        tree.rotation = Quaternion.Euler(0, yaw, 0);
        tree.localScale = Vector3.one * scale;

        Prim.Make(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 1.5f, 0), new Vector3(0.7f, 1.5f, 0.7f), Bark);
        Prim.Cone("Flare", tree, new Vector3(0, 0.45f, 0), new Vector3(1.5f, 0.9f, 1.5f), Bark);
        Prim.Make(PrimitiveType.Cylinder, "BranchL", tree, new Vector3(-0.6f, 2.8f, 0), new Vector3(0.28f, 0.8f, 0.28f), Bark, new Vector3(0, 0, 40));
        Prim.Make(PrimitiveType.Cylinder, "BranchR", tree, new Vector3(0.55f, 3.0f, 0.1f), new Vector3(0.25f, 0.7f, 0.25f), Bark, new Vector3(0, 0, -35));

        var baseGreen = palette[0];
        var light = palette[1];
        var blobs = new[]
        {
            new Vector4(0f, 4.1f, 0f, 3.2f),
            new Vector4(-1.3f, 3.7f, 0.5f, 2.3f),
            new Vector4(1.4f, 3.8f, -0.4f, 2.4f),
            new Vector4(0.2f, 3.6f, 1.4f, 2.1f),
            new Vector4(-0.3f, 3.7f, -1.4f, 2.2f),
            new Vector4(0.3f, 5.1f, 0.2f, 2.2f),
        };
        for (var b = 0; b < blobs.Length; b++)
        {
            var v = blobs[b];
            var color = b >= 3 ? light : baseGreen;
            Prim.Make(PrimitiveType.Sphere, "Leaves", tree, new Vector3(v.x, v.y, v.z), new Vector3(v.w, v.w * 0.8f, v.w), color);
        }
    }

    private void BuildClouds(Transform world)
    {
        for (var i = 0; i < 22; i++)
        {
            var c = new GameObject("Cloud").transform;
            c.SetParent(world, false);
            c.position = new Vector3(Random.Range(-110f, 110f), Random.Range(24f, 48f), Random.Range(-40f, 130f));
            var s = Random.Range(0.8f, 1.6f);
            for (var k = 0; k < 4; k++)
            {
                Prim.Make(PrimitiveType.Sphere, "Puff", c, new Vector3((k - 1.5f) * 3f * s, Random.Range(-0.5f, 0.8f), Random.Range(-1f, 1f)),
                    new Vector3(5f * s, 2.8f * s, 3.5f * s), new Color(1f, 1f, 1f));
            }
            clouds.Add(c);
        }
    }

    private void BuildCastle(Transform world)
    {
        var castle = new GameObject("Castle").transform;
        castle.SetParent(world, false);
        var stone = new Color(0.82f, 0.78f, 0.9f);
        var dark = new Color(0.62f, 0.58f, 0.74f);
        var roof = new Color(0.9f, 0.35f, 0.6f);
        var z = CastleZ;

        Prim.Make(PrimitiveType.Cube, "Wall", castle, new Vector3(0, 4.5f, z - 4f), new Vector3(40f, 9f, 3f), stone);
        Prim.Make(PrimitiveType.Cube, "Walkway", castle, new Vector3(0, WallTopY - 0.2f, z - 4.4f), new Vector3(40f, 0.4f, 4f), dark);
        for (var i = 0; i < 17; i++)
        {
            Prim.Make(PrimitiveType.Cube, "Merlon", castle, new Vector3(-19.2f + i * 2.4f, WallTopY + 0.6f, z - 5.6f), new Vector3(1.2f, 1.2f, 0.8f), stone);
        }
        Prim.Make(PrimitiveType.Cube, "Keep", castle, new Vector3(0, 11f, z + 4f), new Vector3(14f, 22f, 12f), stone);
        for (var i = 0; i < 5; i++)
        {
            Prim.Make(PrimitiveType.Cube, "KeepMerlon", castle, new Vector3(-5.6f + i * 2.8f, 22.6f, z - 1.8f), new Vector3(1.4f, 1.4f, 1.2f), stone);
        }
        Prim.Cone("KeepRoof", castle, new Vector3(0, 28f, z + 4f), new Vector3(10f, 12f, 10f), roof);
        Prim.Make(PrimitiveType.Cube, "Gate", castle, new Vector3(0, 3f, z - 5.6f), new Vector3(6f, 6f, 0.4f), new Color(0.42f, 0.28f, 0.34f));
        Prim.Make(PrimitiveType.Cylinder, "GateArch", castle, new Vector3(0, 6f, z - 5.6f), new Vector3(6f, 0.2f, 6f), new Color(0.42f, 0.28f, 0.34f), new Vector3(90, 0, 0));
        Prim.Make(PrimitiveType.Cube, "Bridge", castle, new Vector3(0, 0.1f, z - 9f), new Vector3(6f, 0.2f, 7f), new Color(0.5f, 0.33f, 0.2f));

        foreach (var sx in new[] { -20f, 20f })
        {
            Prim.Make(PrimitiveType.Cylinder, "Tower", castle, new Vector3(sx, 9f, z - 4f), new Vector3(7f, 9f, 7f), stone);
            Prim.Make(PrimitiveType.Cylinder, "TowerRing", castle, new Vector3(sx, 18.2f, z - 4f), new Vector3(8f, 0.4f, 8f), dark);
            Prim.Cone("TowerRoof", castle, new Vector3(sx, 22f, z - 4f), new Vector3(8.5f, 8f, 8.5f), roof);
            Prim.Make(PrimitiveType.Cube, "Window", castle, new Vector3(sx, 12f, z - 7.6f), new Vector3(1f, 2f, 0.3f), new Color(0.3f, 0.25f, 0.4f));
            Prim.Make(PrimitiveType.Cylinder, "Pole", castle, new Vector3(sx, 28.5f, z - 4f), new Vector3(0.15f, 2f, 0.15f), new Color(0.35f, 0.3f, 0.3f));
            Prim.Cone("Flag", castle, new Vector3(sx + 1.2f, 29.6f, z - 4f), new Vector3(2.4f, 1.6f, 0.15f), new Color(1f, 0.82f, 0.25f), new Vector3(0, 0, -90));
        }

        // Back towers fill out the skyline.
        foreach (var sx in new[] { -20f, 20f })
        {
            Prim.Make(PrimitiveType.Cylinder, "BackTower", castle, new Vector3(sx, 11f, z + 14f), new Vector3(7f, 11f, 7f), stone);
            Prim.Cone("BackRoof", castle, new Vector3(sx, 25f, z + 14f), new Vector3(8.5f, 8f, 8.5f), roof);
        }
        Prim.Make(PrimitiveType.Cube, "BackWall", castle, new Vector3(0, 5f, z + 14f), new Vector3(40f, 10f, 3f), stone);
    }

    private void BuildCannonAndJester(Transform world)
    {
        var wood = new Color(0.45f, 0.28f, 0.16f);
        var iron = new Color(0.25f, 0.27f, 0.33f);
        var z = CastleZ - 4.4f;

        var cannon = new GameObject("Cannon").transform;
        cannon.SetParent(world, false);
        cannon.position = new Vector3(8f, WallTopY, z);
        Prim.Make(PrimitiveType.Cube, "Carriage", cannon, new Vector3(0, 0.55f, 0), new Vector3(1.4f, 0.5f, 1.8f), wood);
        foreach (var sx in new[] { -0.8f, 0.8f })
        {
            Prim.Make(PrimitiveType.Cylinder, "Wheel", cannon, new Vector3(sx, 0.7f, 0), new Vector3(1.4f, 0.1f, 1.4f), wood, new Vector3(0, 0, 90));
        }
        var barrel = new GameObject("Barrel").transform;
        barrel.SetParent(cannon, false);
        barrel.localPosition = new Vector3(0, 1.2f, 0);
        // Aimed back over the meadow where the unicorn flies, and upward.
        barrel.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(-0.2f, 0.6f, -1f).normalized);
        Prim.Make(PrimitiveType.Cylinder, "Tube", barrel, new Vector3(0, 0.8f, 0), new Vector3(0.9f, 1.4f, 0.9f), iron);
        Prim.Make(PrimitiveType.Cylinder, "Muzzle", barrel, new Vector3(0, 2.2f, 0), new Vector3(1.15f, 0.15f, 1.15f), new Color(0.15f, 0.16f, 0.2f));
        Prim.Make(PrimitiveType.Sphere, "Breech", barrel, new Vector3(0, -0.2f, 0), Vector3.one * 0.95f, iron);
        cannonBarrel = barrel;
        cannonMuzzle = new GameObject("MuzzleTip").transform;
        cannonMuzzle.SetParent(barrel, false);
        cannonMuzzle.localPosition = new Vector3(0, 2.4f, 0);

        // Crate of balloons beside the cannon.
        var crate = new Vector3(CrateX, WallTopY, z);
        Prim.Make(PrimitiveType.Cube, "Crate", world, crate + Vector3.up * 0.6f, new Vector3(1.8f, 1.2f, 1.8f), new Color(0.55f, 0.36f, 0.2f));
        var tops = new[] { new Color(1f, 0.35f, 0.45f), new Color(0.35f, 0.65f, 1f), new Color(0.45f, 0.9f, 0.5f), new Color(1f, 0.85f, 0.3f) };
        for (var i = 0; i < tops.Length; i++)
        {
            Prim.Make(PrimitiveType.Sphere, "CrateBalloon", world, crate + new Vector3(-0.5f + (i % 2) * 1f, 1.5f, -0.4f + (i / 2) * 0.8f), Vector3.one * 0.8f, tops[i]);
        }

        BuildJester(world, new Vector3(5.5f, WallTopY, z));
        BuildKing(world, new Vector3(-9f, WallTopY, z));
    }

    private void BuildJester(Transform world, Vector3 pos)
    {
        jester = new GameObject("Jester").transform;
        jester.SetParent(world, false);
        jester.position = pos;
        jester.rotation = Quaternion.Euler(0, 180f, 0);

        var skin = new Color(1f, 0.85f, 0.72f);
        var purple = new Color(0.6f, 0.3f, 0.8f);
        var gold = new Color(1f, 0.8f, 0.2f);

        Prim.Make(PrimitiveType.Cylinder, "LegL", jester, new Vector3(-0.2f, 0.45f, 0), new Vector3(0.22f, 0.45f, 0.22f), purple);
        Prim.Make(PrimitiveType.Cylinder, "LegR", jester, new Vector3(0.2f, 0.45f, 0), new Vector3(0.22f, 0.45f, 0.22f), gold);
        Prim.Make(PrimitiveType.Capsule, "Body", jester, new Vector3(0, 1.3f, 0), new Vector3(0.9f, 0.6f, 0.7f), purple);
        Prim.Make(PrimitiveType.Cylinder, "Belt", jester, new Vector3(0, 1.1f, 0), new Vector3(0.92f, 0.06f, 0.72f), gold);
        Prim.Make(PrimitiveType.Sphere, "Head", jester, new Vector3(0, 2.1f, 0), Vector3.one * 0.85f, skin);
        Prim.Make(PrimitiveType.Sphere, "Nose", jester, new Vector3(0, 2.05f, 0.45f), Vector3.one * 0.18f, new Color(0.95f, 0.4f, 0.4f));
        foreach (var sx in new[] { -0.18f, 0.18f })
        {
            Prim.Make(PrimitiveType.Sphere, "Eye", jester, new Vector3(sx, 2.2f, 0.38f), Vector3.one * 0.12f, Color.black);
        }
        Prim.Make(PrimitiveType.Sphere, "Hand", jester, new Vector3(-0.65f, 1.2f, 0.1f), Vector3.one * 0.25f, skin);
        Prim.Make(PrimitiveType.Sphere, "Hand", jester, new Vector3(0.65f, 1.2f, 0.1f), Vector3.one * 0.25f, skin);

        // Two-pronged hat with bells.
        Prim.Cone("HatL", jester, new Vector3(-0.38f, 2.75f, 0), new Vector3(0.45f, 0.95f, 0.45f), purple, new Vector3(0, 0, 35));
        Prim.Cone("HatR", jester, new Vector3(0.38f, 2.75f, 0), new Vector3(0.45f, 0.95f, 0.45f), gold, new Vector3(0, 0, -35));
        Prim.Make(PrimitiveType.Sphere, "BellL", jester, new Vector3(-0.72f, 3.15f, 0), Vector3.one * 0.2f, gold);
        Prim.Make(PrimitiveType.Sphere, "BellR", jester, new Vector3(0.72f, 3.15f, 0), Vector3.one * 0.2f, purple);
    }

    // ---------- Per frame ----------

    private void Update()
    {
        foreach (var c in clouds)
        {
            var p = c.position;
            p.x += Time.deltaTime * 0.8f;
            if (p.x > 120f) p.x = -120f;
            c.position = p;
        }

        if (onTitle)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
            return;
        }

        // Hand over control a frame later so the key that started the game doesn't also trigger a jump.
        if (!unicorn.Controllable && !gameOver && Time.frameCount > startFrame)
        {
            unicorn.Controllable = true;
        }

        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        UpdateDifficulty();

        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) combo = 0;
        }
        CheckHornHits();
    }

    // Every LevelSeconds the cannon works faster, golden balloons get more common and balloons rise quicker.
    private void UpdateDifficulty()
    {
        playTime += Time.deltaTime;
        var newLevel = 1 + Mathf.FloorToInt(playTime / LevelSeconds);
        if (newLevel != level)
        {
            level = newLevel;
            levelBanner = 2f;
            Sfx.Start();
            effects.Text(unicorn.transform.position + Vector3.up * 4f, "Level " + level + "!", new Color(1f, 0.9f, 0.4f), 40);
            kingCheer = 1.2f;
        }
        var d = level - 1;
        cannon.Tempo = Mathf.Min(2.2f, 1f + d * 0.12f);
        cannon.GoldenChance = Mathf.Min(0.25f, 0.12f + d * 0.015f);
        cannon.DoubleChance = Mathf.Clamp01((d - 2) * 0.08f);
        Balloon.RiseScale = Mathf.Min(1.8f, 1f + d * 0.06f);
        if (levelBanner > 0f) levelBanner -= Time.deltaTime;
        UpdateKing();
    }

    private void BuildKing(Transform world, Vector3 pos)
    {
        king = new GameObject("King").transform;
        king.SetParent(world, false);
        king.position = pos;
        kingHome = pos;
        king.rotation = Quaternion.Euler(0, 180f, 0);

        var skin = new Color(1f, 0.85f, 0.72f);
        var robe = new Color(0.75f, 0.12f, 0.2f);
        var fur = new Color(0.97f, 0.95f, 0.92f);
        var gold = new Color(1f, 0.82f, 0.2f);

        Prim.Make(PrimitiveType.Cylinder, "Robe", king, new Vector3(0, 1.0f, 0), new Vector3(1.5f, 1.0f, 1.3f), robe);
        Prim.Make(PrimitiveType.Cylinder, "Hem", king, new Vector3(0, 0.1f, 0), new Vector3(1.6f, 0.1f, 1.4f), fur);
        Prim.Make(PrimitiveType.Sphere, "Collar", king, new Vector3(0, 1.95f, 0), new Vector3(1.3f, 0.45f, 1.1f), fur);
        Prim.Make(PrimitiveType.Sphere, "Head", king, new Vector3(0, 2.5f, 0), Vector3.one * 0.95f, skin);
        Prim.Make(PrimitiveType.Sphere, "Beard", king, new Vector3(0, 2.15f, 0.25f), new Vector3(0.8f, 0.7f, 0.5f), fur);
        Prim.Make(PrimitiveType.Sphere, "Nose", king, new Vector3(0, 2.5f, 0.5f), Vector3.one * 0.22f, new Color(0.95f, 0.55f, 0.5f));
        foreach (var sx in new[] { -0.2f, 0.2f })
        {
            Prim.Make(PrimitiveType.Sphere, "Eye", king, new Vector3(sx, 2.65f, 0.4f), Vector3.one * 0.12f, Color.black);
        }
        Prim.Make(PrimitiveType.Cylinder, "CrownBand", king, new Vector3(0, 3.05f, 0), new Vector3(0.9f, 0.14f, 0.9f), gold);
        for (var i = 0; i < 5; i++)
        {
            var a = i / 5f * Mathf.PI * 2f;
            Prim.Cone("CrownPoint", king, new Vector3(Mathf.Cos(a) * 0.35f, 3.3f, Mathf.Sin(a) * 0.35f), new Vector3(0.22f, 0.4f, 0.22f), gold);
        }

        kingArmL = new GameObject("ArmL").transform;
        kingArmL.SetParent(king, false);
        kingArmL.localPosition = new Vector3(-0.75f, 1.8f, 0);
        Prim.Make(PrimitiveType.Capsule, "Sleeve", kingArmL, new Vector3(0, -0.5f, 0), new Vector3(0.35f, 0.5f, 0.35f), robe);
        Prim.Make(PrimitiveType.Sphere, "Hand", kingArmL, new Vector3(0, -1.05f, 0), Vector3.one * 0.3f, skin);
        kingArmR = new GameObject("ArmR").transform;
        kingArmR.SetParent(king, false);
        kingArmR.localPosition = new Vector3(0.75f, 1.8f, 0);
        Prim.Make(PrimitiveType.Capsule, "Sleeve", kingArmR, new Vector3(0, -0.5f, 0), new Vector3(0.35f, 0.5f, 0.35f), robe);
        Prim.Make(PrimitiveType.Sphere, "Hand", kingArmR, new Vector3(0, -1.05f, 0), Vector3.one * 0.3f, skin);
    }

    // The king watches the unicorn, cheers on pops and stomps when a balloon escapes.
    private void UpdateKing()
    {
        var dt = Time.deltaTime;
        kingCheer = Mathf.Max(0f, kingCheer - dt);
        kingGrump = Mathf.Max(0f, kingGrump - dt);
        var hop = kingCheer > 0f ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.9f : 0f;
        var shake = kingGrump > 0f ? Mathf.Sin(Time.time * 50f) * 0.12f : 0f;
        king.position = kingHome + new Vector3(shake, hop, 0);

        var look = unicorn.transform.position - king.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
        {
            king.rotation = Quaternion.Slerp(king.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-3f * dt));
        }

        var armUp = kingCheer > 0f ? 150f + Mathf.Sin(Time.time * 14f) * 20f : (kingGrump > 0f ? 60f : 8f);
        kingArmL.localRotation = Quaternion.Euler(0, 0, -armUp);
        kingArmR.localRotation = Quaternion.Euler(0, 0, armUp);
    }

    // The horn is tested as a segment, and also as the path the tip swept this frame so dashes can't skip through a balloon.
    private void CheckHornHits()
    {
        var tip = unicorn.HornTip.position;
        var root = unicorn.HornBase.position;
        var prev = hasPrevTip ? prevTip : tip;
        prevTip = tip;
        hasPrevTip = true;

        for (var i = Balloon.Active.Count - 1; i >= 0; i--)
        {
            var b = Balloon.Active[i];
            var c = b.transform.position;
            if (SegmentDistance(c, root, tip) < HitRadius || SegmentDistance(c, prev, tip) < HitRadius)
            {
                PopBalloon(b);
            }
        }
    }

    private static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var len = ab.sqrMagnitude;
        var t = len < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
        return Vector3.Distance(p, a + ab * t);
    }

    private void PopBalloon(Balloon b)
    {
        combo = Mathf.Min(MaxCombo, comboTimer > 0f ? combo + 1 : 1);
        comboTimer = ComboWindow;
        var points = (b.Golden ? 5 : 1) * combo;
        score += points;

        var pos = b.transform.position;
        effects.Burst(pos, b.Color, b.Golden ? 36 : 20, b.Golden ? 9f : 6f);
        effects.Text(pos + Vector3.up, "+" + points, b.Golden ? Balloon.Gold : Color.white, b.Golden ? 38 : 28);
        if (combo >= 2) effects.Text(pos + Vector3.up * 2.2f, "COMBO x" + combo, new Color(1f, 0.6f, 0.85f), 22);
        if (b.Golden) Sfx.Golden(); else Sfx.Pop(combo);
        if (b.Golden || combo >= 4) kingCheer = 0.8f;
        b.Pop();
    }

    private void OnBalloonEscaped(Balloon b)
    {
        if (gameOver || b.Golden) return;
        hearts--;
        effects.Text(b.transform.position, "Escaped!", new Color(1f, 0.4f, 0.4f), 26);
        kingGrump = 0.9f;
        if (hearts > 0)
        {
            Sfx.Lose();
            return;
        }

        gameOver = true;
        cannon.Active = false;
        if (score > best)
        {
            best = score;
            newRecord = true;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
            Sfx.Record();
            StartCoroutine(FireworkShow());
        }
        else
        {
            Sfx.GameOver();
        }
    }

    private System.Collections.IEnumerator FireworkShow()
    {
        for (var i = 0; i < 14; i++)
        {
            var around = unicorn.transform.position + unicorn.Forward * Random.Range(8f, 16f) + Random.insideUnitSphere * 8f;
            around.y = Mathf.Max(around.y, 4f) + Random.Range(3f, 9f);
            effects.Burst(around, Fireworks[i % Fireworks.Length], 40, 11f, 1.2f);
            yield return new WaitForSeconds(0.18f);
        }
    }

    private static Texture2D MakeHeart()
    {
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (var py = 0; py < 64; py++)
        {
            for (var px = 0; px < 64; px++)
            {
                var x = (px / 63f * 2f - 1f) * 1.25f;
                var y = (py / 63f * 2f - 1f) * 1.25f + 0.1f;
                var f = Mathf.Pow(x * x + y * y - 1f, 3f) - x * x * y * y * y;
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, f < 0f ? 1f : 0f));
            }
        }
        tex.Apply();
        return tex;
    }

    private void LateUpdate()
    {
        FollowCamera(Time.deltaTime);
    }

    private void SnapCamera() => FollowCamera(10f);

    private void FollowCamera(float dt)
    {
        if (onTitle)
        {
            // Slow orbit that shows off the unicorn from the front.
            var c = unicorn.transform.position + Vector3.up * 1.5f;
            var a = Time.time * 0.35f + 0.6f;
            var want = c + new Vector3(Mathf.Sin(a), 0.25f, Mathf.Cos(a)) * 6.5f;
            cam.transform.position = Vector3.Lerp(cam.transform.position, want, 1f - Mathf.Exp(-3f * dt));
            var look = Quaternion.LookRotation(c - cam.transform.position, Vector3.up);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, look, 1f - Mathf.Exp(-6f * dt));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 50f, 1f - Mathf.Exp(-4f * dt));
            return;
        }

        var fwd = unicorn.Forward;
        var focus = unicorn.transform.position;
        var distance = unicorn.Dashing ? 8.5f : 7f;
        var desired = focus - fwd * distance + Vector3.up * (3.2f + Mathf.Max(0f, focus.y) * 0.15f);
        desired.y = Mathf.Max(desired.y, 1.2f);
        var k = 1f - Mathf.Exp(-6f * dt);
        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, k);
        var target = focus + Vector3.up * 1.6f + fwd * 4f;
        var rot = Quaternion.LookRotation(target - cam.transform.position, Vector3.up);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rot, 1f - Mathf.Exp(-9f * dt));
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, unicorn.Dashing ? 78f : 62f, 1f - Mathf.Exp(-8f * dt));
    }

    private void OnGUI()
    {
        var k = Screen.height / 720f;
        if (hint == null)
        {
            hint = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            hint.normal.textColor = Color.white;
            scoreStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            bigStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            midStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
        if (onTitle)
        {
            DrawTitle(k);
            return;
        }

        GUI.Label(new Rect(12, 8, 700, 26), "W/S move   A/D turn   Space jump / flap (hold to glide)   Shift dash   Ctrl or C dive", hint);
        DrawBar(12, 40, 160, 12, unicorn.Stamina01, new Color(0.5f, 0.9f, 1f));
        DrawBar(12, 58, 160, 8, unicorn.DashReady01, new Color(1f, 0.85f, 0.3f));

        GUI.Label(new Rect(12, 72, 200, 26), "Level " + level, hint);
        if (levelBanner > 0f)
        {
            Shadowed(new Rect(0, Screen.height * 0.3f, Screen.width, 70 * k), "Level " + level + " - faster!", bigStyle, Mathf.RoundToInt(54 * k), new Color(1f, 0.9f, 0.4f));
        }

        // Score and combo, top centre.
        Shadowed(new Rect(0, 6 * k, Screen.width, 60 * k), score.ToString(), scoreStyle, Mathf.RoundToInt(46 * k), Color.white);
        if (combo >= 2)
        {
            Shadowed(new Rect(0, 62 * k, Screen.width, 30 * k), "Combo x" + combo, scoreStyle, Mathf.RoundToInt(22 * k), new Color(1f, 0.6f, 0.85f));
            var w = 160 * k;
            DrawBar(Screen.width * 0.5f - w * 0.5f, 94 * k, w, 8 * k, comboTimer / ComboWindow, new Color(1f, 0.6f, 0.85f));
        }

        // Hearts, top right, with the best score under them.
        var size = 40 * k;
        for (var i = 0; i < StartHearts; i++)
        {
            GUI.color = i < hearts ? new Color(1f, 0.3f, 0.45f) : new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(Screen.width - (StartHearts - i) * (size + 6 * k) - 8 * k, 10 * k, size, size), heartTex);
        }
        GUI.color = Color.white;
        var bestStyle = new GUIStyle(scoreStyle) { alignment = TextAnchor.UpperRight };
        Shadowed(new Rect(0, 56 * k, Screen.width - 12 * k, 26 * k), "Best " + Mathf.Max(best, score), bestStyle, Mathf.RoundToInt(20 * k), new Color(1f, 0.9f, 0.5f));

        if (gameOver)
        {
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var cy = Screen.height * 0.5f;
            Shadowed(new Rect(0, cy - 120 * k, Screen.width, 80 * k), "Game Over", bigStyle, Mathf.RoundToInt(72 * k), new Color(1f, 0.5f, 0.7f));
            Shadowed(new Rect(0, cy - 30 * k, Screen.width, 50 * k), "Score " + score + "   Level " + level, midStyle, Mathf.RoundToInt(40 * k), Color.white);
            if (newRecord)
            {
                var pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;
                Shadowed(new Rect(0, cy + 22 * k, Screen.width, 40 * k), "New high score!", midStyle, Mathf.RoundToInt(32 * k * pulse), new Color(1f, 0.9f, 0.3f));
            }
            else
            {
                Shadowed(new Rect(0, cy + 22 * k, Screen.width, 40 * k), "Best " + best, midStyle, Mathf.RoundToInt(28 * k), new Color(1f, 0.9f, 0.5f));
            }
            Shadowed(new Rect(0, cy + 80 * k, Screen.width, 40 * k), "Press R to play again", midStyle, Mathf.RoundToInt(26 * k), Color.white);
        }
    }

    private void DrawTitle(float k)
    {
        GUI.color = new Color(0, 0, 0, 0.25f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var bob = Mathf.Sin(Time.time * 2f) * 6f * k;
        Shadowed(new Rect(0, 50 * k + bob, Screen.width, 110 * k), "Unicorn Pop", bigStyle, Mathf.RoundToInt(96 * k), new Color(1f, 0.55f, 0.85f));
        Shadowed(new Rect(0, 150 * k, Screen.width, 40 * k), "Pop the jester's balloons with your horn!", midStyle, Mathf.RoundToInt(26 * k), Color.white);

        var blink = 0.65f + 0.35f * Mathf.Sin(Time.time * 4f);
        Shadowed(new Rect(0, Screen.height - 210 * k, Screen.width, 50 * k), "Press Space, Enter or click to start", midStyle, Mathf.RoundToInt(34 * k * (0.96f + 0.04f * blink)), new Color(1f, 1f, 1f, blink));

        var lines = new[]
        {
            "W / S  fly forward / back        A / D  turn",
            "Space  jump, then flap (hold to glide)",
            "Shift  rainbow dash        Ctrl or C  dive",
            "Golden balloons are worth 5 and never cost a heart. 3 hearts total.",
        };
        for (var i = 0; i < lines.Length; i++)
        {
            Shadowed(new Rect(0, Screen.height - 150 * k + i * 26 * k, Screen.width, 26 * k), lines[i], midStyle, Mathf.RoundToInt(18 * k), Color.white);
        }
        if (best > 0)
        {
            Shadowed(new Rect(0, 200 * k, Screen.width, 36 * k), "Best " + best, midStyle, Mathf.RoundToInt(28 * k), new Color(1f, 0.9f, 0.5f));
        }
    }

    private static void Shadowed(Rect r, string text, GUIStyle style, int size, Color color)
    {
        style.fontSize = size;
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        GUI.color = color;
        GUI.Label(r, text, style);
        GUI.color = Color.white;
    }

    private static void DrawBar(float x, float y, float w, float h, float fill, Color color)
    {
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x + 1, y + 1, (w - 2) * Mathf.Clamp01(fill), h - 2), Texture2D.whiteTexture);
        GUI.color = old;
    }
}
