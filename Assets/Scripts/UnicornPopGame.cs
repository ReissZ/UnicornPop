using System.Collections.Generic;
using UnityEngine;

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
    private GUIStyle hint;

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
        BuildEnvironment();
        BuildCamera();

        var world = new GameObject("World").transform;
        world.SetParent(transform, false);
        BuildGround(world);
        BuildClouds(world);
        BuildCastle(world);
        BuildCannonAndJester(world);

        unicorn = new GameObject("Unicorn").AddComponent<Unicorn>();
        unicorn.transform.SetParent(transform, false);
        unicorn.Build(new Vector3(0, 0, -5f), 0f);

        var cannon = new GameObject("CannonController").AddComponent<Cannon>();
        cannon.transform.SetParent(transform, false);
        cannon.Build(world, jester, cannonBarrel, cannonMuzzle, unicorn.transform, CrateX, 9.8f, 5.5f);
        SnapCamera();
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

        var trunk = new Color(0.5f, 0.33f, 0.2f);
        var leaves = new[] { new Color(0.28f, 0.62f, 0.3f), new Color(0.35f, 0.7f, 0.35f), new Color(0.95f, 0.6f, 0.75f) };
        for (var i = 0; i < 70; i++)
        {
            var x = Random.Range(-50f, 50f);
            var z = Random.Range(-28f, 52f);
            if (Mathf.Abs(x) < 5f && z > -10f) continue;
            var h = Random.Range(2.5f, 4.5f);
            Prim.Make(PrimitiveType.Cylinder, "Trunk", world, new Vector3(x, h * 0.5f, z), new Vector3(0.5f, h * 0.5f, 0.5f), trunk);
            var crown = Prim.Make(PrimitiveType.Sphere, "Crown", world, new Vector3(x, h + 0.8f, z), Vector3.one * Random.Range(2.4f, 3.6f), leaves[Random.Range(0, leaves.Length)]);
            crown.localScale = new Vector3(crown.localScale.x, crown.localScale.y * 1.15f, crown.localScale.z);
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
    }

    private void LateUpdate()
    {
        FollowCamera(Time.deltaTime);
    }

    private void SnapCamera() => FollowCamera(10f);

    private void FollowCamera(float dt)
    {
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
        if (hint == null)
        {
            hint = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            hint.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(12, 8, 700, 26), "W/S move   A/D turn   Space jump / flap (hold to glide)   Shift dash   Ctrl or C dive", hint);

        DrawBar(12, 40, 160, 12, unicorn.Stamina01, new Color(0.5f, 0.9f, 1f));
        DrawBar(12, 58, 160, 8, unicorn.DashReady01, new Color(1f, 0.85f, 0.3f));
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
