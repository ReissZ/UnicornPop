using System.Collections.Generic;
using UnityEngine;

// Dreamy backdrop: gradient sky dome, sun with rays, rainbow, shooting stars, far mountains,
// floating islands, a sparkling pond, a winding path and glowing flowers.
public sealed partial class Dreamland : MonoBehaviour
{
    public static readonly Color Horizon = new Color(0.96f, 0.80f, 0.86f);

    private static readonly Vector2 PondCenter = new Vector2(-20f, 18f);
    private static readonly Vector2 PondRadius = new Vector2(8f, 6f);

    private Camera cam;
    private Transform rig;
    private Transform sunRays;
    private readonly List<Transform> sparkles = new List<Transform>();
    private readonly List<float> sparklePhase = new List<float>();
    private readonly List<Transform> flowerClusters = new List<Transform>();
    private readonly List<float> clusterPhase = new List<float>();
    private readonly List<Transform> islands = new List<Transform>();
    private readonly List<float> islandBaseY = new List<float>();
    private Material[] flowerMats;
    private Color[] flowerColors;
    private Transform shootingStar;
    private Material shootingMat;
    private Vector3 starFrom, starTo;
    private float starT = 99f, starTimer = 5f;

    public static bool InPond(float x, float z, float pad = 1.5f)
    {
        var dx = (x - PondCenter.x) / (PondRadius.x + pad);
        var dz = (z - PondCenter.y) / (PondRadius.y + pad);
        return dx * dx + dz * dz < 1f;
    }

    // The path wanders from side to side and straightens out toward the castle gate.
    public static float PathX(float z) => 3f * Mathf.Sin(z * 0.09f) * Mathf.Clamp01((56f - z) / 14f);

    public void Build(Transform world, Camera camera)
    {
        cam = camera;
        rig = new GameObject("SkyRig").transform;
        rig.SetParent(transform, false);

        BuildSky();
        BuildMountains(world);
        BuildPath(world);
        BuildPond(world);
        BuildFlowers(world);
        BuildIslands(world);
        BuildThemes();
    }

    // ---------- Sky ----------

    private static Material SkyMat(int queue) => new Material(Shader.Find("Sprites/Default")) { renderQueue = queue };

    private static Mesh MakeMesh(List<Vector3> verts, List<Color> colors, List<int> tris)
    {
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private Transform SkyPiece(string name, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(rig, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    private Transform SkyBall(string name, Vector3 pos, float size, Color color, int queue)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(rig, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * size;
        var mat = SkyMat(queue);
        mat.color = color;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    private void BuildSky()
    {
        domeMesh = DomeMesh(300f);
        SkyPiece("Dome", domeMesh, SkyMat(1000));
        rainbowMat = SkyMat(1001);
        SkyPiece("Rainbow", RainbowMesh(new Vector3(-100f, -8f, 240f), 120f), rainbowMat);

        var sunPos = new Vector3(0.25f, 0.42f, 0.87f).normalized * 270f;
        SkyBall("SunHalo", sunPos, 90f, new Color(1f, 0.9f, 0.7f, 0.16f), 1002);
        SkyBall("SunGlow", sunPos, 55f, new Color(1f, 0.92f, 0.65f, 0.3f), 1003);

        sunRays = new GameObject("SunRays").transform;
        sunRays.SetParent(rig, false);
        sunRays.localPosition = sunPos;
        sunRays.localRotation = Quaternion.LookRotation(-sunPos);
        var rayMat = SkyMat(1004);
        rayMat.color = new Color(1f, 0.95f, 0.7f, 0.2f);
        for (var i = 0; i < 12; i++)
        {
            var pivot = new GameObject("Ray").transform;
            pivot.SetParent(sunRays, false);
            pivot.localRotation = Quaternion.Euler(0, 0, i * 30f);
            var ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(ray.GetComponent<Collider>());
            ray.transform.SetParent(pivot, false);
            ray.transform.localPosition = new Vector3(0, 52f, 0);
            ray.transform.localScale = new Vector3(i % 2 == 0 ? 7f : 4f, i % 2 == 0 ? 70f : 50f, 0.5f);
            ray.GetComponent<Renderer>().sharedMaterial = rayMat;
        }
        SkyBall("SunDisc", sunPos, 30f, new Color(1f, 0.97f, 0.8f, 1f), 1005);
        GatherSun();

        var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
        star.name = "ShootingStar";
        Destroy(star.GetComponent<Collider>());
        star.transform.SetParent(rig, false);
        shootingMat = SkyMat(1006);
        shootingMat.color = new Color(1f, 1f, 0.9f, 0.9f);
        star.GetComponent<Renderer>().sharedMaterial = shootingMat;
        star.SetActive(false);
        shootingStar = star.transform;
    }

    private static Mesh DomeMesh(float radius)
    {
        float[] angles = { -12f, 0f, 6f, 14f, 26f, 42f, 62f, 90f };
        Color[] cols =
        {
            Horizon, Horizon, new Color(1f, 0.78f, 0.74f), new Color(0.95f, 0.74f, 0.88f),
            new Color(0.8f, 0.72f, 0.97f), new Color(0.66f, 0.74f, 1f), new Color(0.52f, 0.68f, 1f), new Color(0.42f, 0.6f, 0.98f),
        };
        const int seg = 32;
        var verts = new List<Vector3>();
        var colors = new List<Color>();
        var tris = new List<int>();
        for (var r = 0; r < angles.Length; r++)
        {
            var el = angles[r] * Mathf.Deg2Rad;
            for (var s = 0; s < seg; s++)
            {
                var a = s / (float)seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(el) * Mathf.Sin(a), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(a)) * radius);
                colors.Add(cols[r]);
            }
        }
        for (var r = 0; r < angles.Length - 1; r++)
        {
            for (var s = 0; s < seg; s++)
            {
                var i0 = r * seg + s;
                var i1 = r * seg + (s + 1) % seg;
                var i2 = (r + 1) * seg + s;
                var i3 = (r + 1) * seg + (s + 1) % seg;
                tris.AddRange(new[] { i0, i2, i1, i1, i2, i3 });
            }
        }
        return MakeMesh(verts, colors, tris);
    }

    private static Mesh RainbowMesh(Vector3 center, float radius)
    {
        Color[] bands =
        {
            new Color(1f, 0.45f, 0.5f, 0.5f), new Color(1f, 0.7f, 0.4f, 0.5f), new Color(1f, 0.95f, 0.5f, 0.5f),
            new Color(0.55f, 0.9f, 0.6f, 0.5f), new Color(0.5f, 0.78f, 1f, 0.5f), new Color(0.6f, 0.6f, 1f, 0.5f), new Color(0.8f, 0.6f, 1f, 0.5f),
        };
        const int seg = 48;
        const float width = 4.5f;
        var verts = new List<Vector3>();
        var colors = new List<Color>();
        var tris = new List<int>();
        for (var b = 0; b < bands.Length; b++)
        {
            var rIn = radius + b * width;
            var rOut = rIn + width;
            for (var s = 0; s <= seg; s++)
            {
                var a = s / (float)seg * Mathf.PI;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                // The ends of the arc fade out into the hills.
                var fade = Mathf.Clamp01(Mathf.Sin(a) * 4f);
                var c = bands[b];
                c.a *= fade;
                verts.Add(center + dir * rIn);
                verts.Add(center + dir * rOut);
                colors.Add(c);
                colors.Add(c);
                if (s == seg) continue;
                var i = verts.Count - 2;
                tris.AddRange(new[] { i, i + 1, i + 2, i + 1, i + 3, i + 2 });
            }
        }
        return MakeMesh(verts, colors, tris);
    }

    // ---------- Ground scenery ----------

    private static void BuildMountains(Transform world)
    {
        var colors = new[] { new Color(0.78f, 0.7f, 0.95f), new Color(0.95f, 0.72f, 0.88f), new Color(0.65f, 0.78f, 0.98f) };
        for (var i = 0; i < 10; i++)
        {
            var x = Mathf.Lerp(-210f, 210f, i / 9f) + Random.Range(-15f, 15f);
            var z = Random.Range(150f, 195f);
            var h = Random.Range(50f, 105f);
            var w = h * Random.Range(1.3f, 1.9f);
            Prim.Cone("Mountain", world, new Vector3(x, h * 0.5f - 2f, z), new Vector3(w, h, w), colors[i % 3]);
        }
    }

    private static void BuildPath(Transform world)
    {
        var sand = new[] { new Color(0.94f, 0.85f, 0.66f), new Color(0.9f, 0.8f, 0.62f) };
        const float step = 2.2f;
        var n = 0;
        for (var z = -14f; z < 58f; z += step, n++)
        {
            var x = PathX(z);
            var angle = Mathf.Atan2(PathX(z + step) - x, step) * Mathf.Rad2Deg;
            Prim.Make(PrimitiveType.Cube, "Path", world, new Vector3(x, 0.02f, z), new Vector3(3.4f, 0.04f, step + 0.4f), sand[n % 2], new Vector3(0, angle, 0));
        }
    }

    private static Material Glow(Color color, float strength, float smooth = 0.25f)
    {
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * strength);
        m.SetFloat("_Glossiness", smooth);
        return m;
    }

    private void BuildPond(Transform world)
    {
        Prim.Make(PrimitiveType.Cylinder, "PondBank", world, new Vector3(PondCenter.x, 0.02f, PondCenter.y), new Vector3(PondRadius.x * 2f + 1.6f, 0.01f, PondRadius.y * 2f + 1.6f), new Color(0.95f, 0.88f, 0.7f));
        var water = Prim.Make(PrimitiveType.Cylinder, "Pond", world, new Vector3(PondCenter.x, 0.05f, PondCenter.y), new Vector3(PondRadius.x * 2f, 0.01f, PondRadius.y * 2f), Color.white);
        water.GetComponent<Renderer>().sharedMaterial = Glow(new Color(0.45f, 0.8f, 0.95f), 0.25f, 0.9f);

        for (var i = 0; i < 16; i++)
        {
            var a = Random.Range(0f, Mathf.PI * 2f);
            var r = Mathf.Sqrt(Random.value) * 0.9f;
            var p = new Vector3(PondCenter.x + Mathf.Cos(a) * PondRadius.x * r, 0.1f, PondCenter.y + Mathf.Sin(a) * PondRadius.y * r);
            var s = Prim.Make(PrimitiveType.Sphere, "Sparkle", world, p, Vector3.zero, Color.white);
            s.GetComponent<Renderer>().sharedMaterial = Glow(Color.white, 1.5f);
            sparkles.Add(s);
            sparklePhase.Add(Random.Range(0f, 6.3f));
        }
    }

    private void BuildFlowers(Transform world)
    {
        flowerColors = new[]
        {
            new Color(1f, 0.55f, 0.75f), new Color(1f, 0.9f, 0.4f), new Color(1f, 1f, 1f),
            new Color(0.8f, 0.65f, 1f), new Color(1f, 0.7f, 0.35f), new Color(0.55f, 0.8f, 1f),
        };
        flowerMats = new Material[flowerColors.Length];
        for (var i = 0; i < flowerMats.Length; i++) flowerMats[i] = Glow(flowerColors[i], 0.4f);

        var stem = new Color(0.3f, 0.6f, 0.3f);
        for (var c = 0; c < 55; c++)
        {
            float x, z;
            var tries = 0;
            do
            {
                x = Random.Range(-48f, 48f);
                z = Random.Range(-26f, 52f);
                tries++;
            }
            while (tries < 20 && (Mathf.Abs(x - PathX(z)) < 3.2f || InPond(x, z, 2f)));

            var cluster = new GameObject("Flowers").transform;
            cluster.SetParent(world, false);
            cluster.position = new Vector3(x, 0, z);
            var ci = Random.Range(0, flowerMats.Length);
            for (var k = 0; k < 4; k++)
            {
                var ox = Random.Range(-0.7f, 0.7f);
                var oz = Random.Range(-0.7f, 0.7f);
                var h = Random.Range(0.45f, 0.85f);
                Prim.Make(PrimitiveType.Cylinder, "Stem", cluster, new Vector3(ox, h * 0.5f, oz), new Vector3(0.05f, h * 0.5f, 0.05f), stem);
                var head = Prim.Make(PrimitiveType.Sphere, "Bloom", cluster, new Vector3(ox, h + 0.05f, oz), new Vector3(0.32f, 0.2f, 0.32f), Color.white);
                head.GetComponent<Renderer>().sharedMaterial = flowerMats[(ci + (k == 3 ? 1 : 0)) % flowerMats.Length];
            }
            flowerClusters.Add(cluster);
            clusterPhase.Add(Random.Range(0f, 6.3f));
        }
    }

    private void BuildIslands(Transform world)
    {
        BuildIsland(world, new Vector3(-36f, 16f, 26f), 11f);
        BuildIsland(world, new Vector3(38f, 20f, 38f), 9f);
        BuildIsland(world, new Vector3(-22f, 24f, 66f), 13f);
        BuildIsland(world, new Vector3(24f, 15f, 8f), 7f);
    }

    private void BuildIsland(Transform world, Vector3 pos, float size)
    {
        var isl = new GameObject("FloatingIsland").transform;
        isl.SetParent(world, false);
        isl.position = pos;

        Prim.Make(PrimitiveType.Cylinder, "Top", isl, Vector3.zero, new Vector3(size, 0.18f, size), new Color(0.5f, 0.82f, 0.5f));
        Prim.Cone("Underside", isl, new Vector3(0, -size * 0.45f, 0), new Vector3(size * 0.97f, size * 0.9f, size * 0.97f), new Color(0.7f, 0.62f, 0.85f), new Vector3(180f, 0, 0));
        Prim.Cone("Crystal", isl, new Vector3(0.1f * size, -size * 0.95f, 0.05f * size), new Vector3(size * 0.15f, size * 0.3f, size * 0.15f), new Color(0.6f, 0.9f, 1f), new Vector3(180f, 0, 0));

        var palette = new[] { new Color(0.98f, 0.62f, 0.78f), new Color(1f, 0.82f, 0.9f) };
        UnicornPopGame.BuildOak(isl, pos + new Vector3(-size * 0.15f, 0.18f, 0.1f * size), size * 0.05f, Random.Range(0f, 360f), palette);

        // A waterfall spills over the edge and ends in a puff of mist.
        var edge = size * 0.47f;
        var water = Prim.Make(PrimitiveType.Cube, "Waterfall", isl, new Vector3(edge, -7f, 0), new Vector3(size * 0.1f, 14f, 0.5f), Color.white);
        water.GetComponent<Renderer>().sharedMaterial = Glow(new Color(0.75f, 0.93f, 1f), 0.5f, 0.8f);
        var mist = Prim.Make(PrimitiveType.Sphere, "Mist", isl, new Vector3(edge, -14f, 0), new Vector3(3.5f, 1.2f, 3.5f), Color.white);
        mist.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 1f, 1f), 0.4f, 0.1f);

        islands.Add(isl);
        islandBaseY.Add(pos.y);
    }

    // ---------- Animation ----------

    private void LateUpdate()
    {
        if (cam == null) return;
        rig.position = cam.transform.position;
        var t = Time.time;

        sunRays.Rotate(0, 0, 1.5f * Time.deltaTime, Space.Self);

        for (var i = 0; i < flowerClusters.Count; i++)
        {
            flowerClusters[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 1.6f + clusterPhase[i]) * 5f, 0, Mathf.Cos(t * 1.3f + clusterPhase[i]) * 5f);
        }
        var pulse = 0.35f + 0.25f * Mathf.Sin(t * 2f);
        for (var i = 0; i < flowerMats.Length; i++) flowerMats[i].SetColor("_EmissionColor", flowerColors[i] * pulse);

        for (var i = 0; i < sparkles.Count; i++)
        {
            var s = Mathf.Max(0f, Mathf.Sin(t * 2.5f + sparklePhase[i]));
            sparkles[i].localScale = Vector3.one * (0.35f * s * s);
        }

        for (var i = 0; i < islands.Count; i++)
        {
            var p = islands[i].position;
            p.y = islandBaseY[i] + Mathf.Sin(t * 0.6f + i * 1.7f) * 0.6f;
            islands[i].position = p;
        }

        UpdateShootingStar(Time.deltaTime);
        UpdateTheme(Time.deltaTime);
    }

    private void UpdateShootingStar(float dt)
    {
        if (starT >= 1f)
        {
            shootingStar.gameObject.SetActive(false);
            starTimer -= dt;
            if (starTimer > 0f) return;
            starTimer = Random.Range(8f, 15f);
            starT = 0f;
            var x = Random.Range(-150f, 100f);
            starFrom = new Vector3(x, Random.Range(150f, 200f), 210f);
            starTo = starFrom + new Vector3(Random.Range(60f, 100f), -Random.Range(20f, 40f), 0);
            shootingStar.gameObject.SetActive(true);
        }
        starT += dt / 1.2f;
        shootingStar.localPosition = Vector3.Lerp(starFrom, starTo, starT);
        shootingStar.localRotation = Quaternion.LookRotation(starTo - starFrom);
        shootingStar.localScale = new Vector3(0.8f, 0.8f, 22f * Mathf.Sin(Mathf.Clamp01(starT) * Mathf.PI));
    }
}
