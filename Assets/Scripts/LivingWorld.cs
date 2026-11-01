using System.Collections.Generic;
using UnityEngine;

// Things that make the world feel alive: butterflies, fireflies, birds, castle banners, glowing windows, a drawbridge and a weathervane.
public sealed class LivingWorld : MonoBehaviour
{
    private sealed class Butterfly
    {
        public Transform Root, WingL, WingR;
        public Vector3 Home, Vel;
        public float Phase, Scare;
        public Color Dust;
    }

    private sealed class Bird
    {
        public Transform Root, WingL, WingR;
        public Vector3 Offset;
        public float Phase;
    }

    private Transform target;
    private readonly List<Butterfly> butterflies = new List<Butterfly>();
    private readonly List<Transform> fireflies = new List<Transform>();
    private readonly List<Vector3> fireflyHome = new List<Vector3>();
    private readonly List<float> fireflyPhase = new List<float>();
    private readonly List<Bird> birds = new List<Bird>();
    private readonly List<Transform> banners = new List<Transform>();
    private readonly List<Material> windowMats = new List<Material>();
    private Transform vane, bridge;
    private readonly Transform[] chains = new Transform[2];
    private float bridgeLift;
    private float castleZ;

    private static Material Glow(Color color, float strength)
    {
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * strength);
        return m;
    }

    private static readonly Dictionary<Color, Material> GlowCache = new Dictionary<Color, Material>();

    private static Material CachedGlow(Color color, float strength)
    {
        if (!GlowCache.TryGetValue(color, out var m))
        {
            m = Glow(color, strength);
            GlowCache[color] = m;
        }
        return m;
    }

    private static Transform Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
    {
        var t = Prim.Make(type, "Part", parent, pos, scale, Color.white, euler);
        t.GetComponent<Renderer>().sharedMaterial = mat;
        return t;
    }

    // Two-toned wing pair: a large forewing and a smaller hindwing, with glowing spots and a bright rim.
    private static Transform MagicWing(Transform parent, float side, Color main, Color accent)
    {
        var pivot = new GameObject("Wing").transform;
        pivot.SetParent(parent, false);
        var mainMat = CachedGlow(main, 0.45f);
        var accentMat = CachedGlow(accent, 0.8f);
        var rimMat = CachedGlow(Color.Lerp(main, Color.white, 0.7f), 1.1f);

        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.42f, 0, 0.16f), new Vector3(0.86f, 0.03f, 0.62f), mainMat, new Vector3(0, side * 18f, 0));
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.62f, 0.004f, 0.2f), new Vector3(0.5f, 0.03f, 0.4f), accentMat, new Vector3(0, side * 18f, 0));
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.3f, 0, -0.26f), new Vector3(0.52f, 0.03f, 0.4f), accentMat, new Vector3(0, -side * 20f, 0));
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.22f, 0.004f, -0.3f), new Vector3(0.28f, 0.03f, 0.22f), mainMat, new Vector3(0, -side * 20f, 0));
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.72f, 0.01f, 0.3f), Vector3.one * 0.1f, rimMat);
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.5f, 0.01f, 0.1f), Vector3.one * 0.07f, rimMat);
        Part(pivot, PrimitiveType.Sphere, new Vector3(side * 0.4f, 0.01f, -0.34f), Vector3.one * 0.07f, rimMat);
        return pivot;
    }

    private static Transform Wing(Transform parent, float side, Color color)
    {
        var pivot = new GameObject("Wing").transform;
        pivot.SetParent(parent, false);
        Prim.Make(PrimitiveType.Sphere, "Blade", pivot, new Vector3(side * 0.5f, 0, 0), new Vector3(1f, 0.04f, 0.7f), color);
        return pivot;
    }

    public void Build(Transform world, Transform unicorn, float castleZ)
    {
        target = unicorn;
        this.castleZ = castleZ;
        BuildButterflies(world);
        BuildFireflies(world);
        BuildBirds(world);
        BuildCastleLife(world);
    }

    private void BuildButterflies(Transform world)
    {
        var colors = new[]
        {
            new Color(1f, 0.55f, 0.75f), new Color(1f, 0.85f, 0.35f), new Color(0.6f, 0.75f, 1f),
            new Color(0.8f, 0.6f, 1f), new Color(1f, 0.65f, 0.4f),
        };
        for (var i = 0; i < 26; i++)
        {
            var root = new GameObject("Butterfly").transform;
            root.SetParent(world, false);
            var c = colors[i % colors.Length];
            var accent = colors[(i + 2) % colors.Length];
            var bodyMat = CachedGlow(new Color(0.35f, 0.25f, 0.5f), 0.2f);
            Part(root, PrimitiveType.Sphere, new Vector3(0, 0, 0.14f), new Vector3(0.09f, 0.09f, 0.1f), bodyMat);
            Part(root, PrimitiveType.Capsule, new Vector3(0, 0, -0.04f), new Vector3(0.07f, 0.13f, 0.07f), bodyMat, new Vector3(90, 0, 0));
            Part(root, PrimitiveType.Sphere, new Vector3(0, 0, -0.2f), new Vector3(0.06f, 0.06f, 0.14f), bodyMat);
            foreach (var sd in new[] { -1f, 1f })
            {
                Part(root, PrimitiveType.Cylinder, new Vector3(0.05f * sd, 0.05f, 0.27f), new Vector3(0.012f, 0.1f, 0.012f), bodyMat, new Vector3(60, 0, -20f * sd));
                Part(root, PrimitiveType.Sphere, new Vector3(0.085f * sd, 0.1f, 0.36f), Vector3.one * 0.035f, CachedGlow(Color.white, 1.4f));
            }
            var b = new Butterfly
            {
                Root = root,
                WingL = MagicWing(root, -1f, c, accent),
                WingR = MagicWing(root, 1f, c, accent),
                Home = new Vector3(Random.Range(-40f, 40f), Random.Range(1f, 4f), Random.Range(-20f, 50f)),
                Phase = Random.Range(0f, 6.3f),
                Dust = c,
            };
            root.position = b.Home;
            butterflies.Add(b);
        }
    }

    private void BuildFireflies(Transform world)
    {
        for (var i = 0; i < 45; i++)
        {
            var home = new Vector3(Random.Range(-45f, 45f), Random.Range(0.8f, 5f), Random.Range(-22f, 55f));
            var f = Prim.Make(PrimitiveType.Sphere, "Firefly", world, home, Vector3.one * 0.18f, Color.white);
            f.GetComponent<Renderer>().sharedMaterial = Glow(new Color(1f, 0.95f, 0.5f), 2.5f);
            fireflies.Add(f);
            fireflyHome.Add(home);
            fireflyPhase.Add(Random.Range(0f, 6.3f));
        }
    }

    private void BuildBirds(Transform world)
    {
        var colors = new[] { new Color(1f, 0.6f, 0.75f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.85f, 0.4f) };
        for (var i = 0; i < 3; i++)
        {
            var root = new GameObject("Bird").transform;
            root.SetParent(world, false);
            var c = colors[i];
            Prim.Make(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(0.3f, 0.28f, 0.45f), c);
            Prim.Make(PrimitiveType.Sphere, "Head", root, new Vector3(0, 0.12f, 0.25f), Vector3.one * 0.22f, c);
            Prim.Cone("Beak", root, new Vector3(0, 0.1f, 0.4f), new Vector3(0.08f, 0.14f, 0.08f), new Color(1f, 0.7f, 0.2f), new Vector3(90, 0, 0));
            Prim.Cone("Tail", root, new Vector3(0, 0.02f, -0.35f), new Vector3(0.18f, 0.3f, 0.04f), c, new Vector3(-90, 0, 0));
            birds.Add(new Bird
            {
                Root = root,
                WingL = Wing(root, -1f, c * 0.9f),
                WingR = Wing(root, 1f, c * 0.9f),
                Offset = new Vector3((i - 1) * 3.2f, 1.5f + (i == 1 ? 1.2f : 0f), -6f - i * 1.5f),
                Phase = i * 1.3f,
            });
            root.position = new Vector3(0, 6f, -10f);
        }
    }

    private void BuildCastleLife(Transform world)
    {
        var z = castleZ;
        var castle = new GameObject("CastleLife").transform;
        castle.SetParent(world, false);
        var colors = new[] { new Color(1f, 0.45f, 0.7f), new Color(1f, 0.82f, 0.25f), new Color(0.55f, 0.6f, 1f) };

        // Pennants hang from the keep and the towers.
        var anchors = new List<Vector3>();
        for (var i = 0; i < 6; i++) anchors.Add(new Vector3(-6f + i * 2.4f, 21.2f, z - 2.4f));
        foreach (var sx in new[] { -20f, 20f })
        {
            anchors.Add(new Vector3(sx - 2.2f, 16.5f, z - 7.6f));
            anchors.Add(new Vector3(sx + 2.2f, 16.5f, z - 7.6f));
        }
        for (var i = 0; i < anchors.Count; i++)
        {
            var pivot = new GameObject("Banner").transform;
            pivot.SetParent(castle, false);
            pivot.position = anchors[i];
            Prim.Make(PrimitiveType.Cube, "Rod", pivot, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.1f, 0.1f), new Color(0.35f, 0.3f, 0.3f));
            Prim.Cone("Cloth", pivot, new Vector3(0, -1.3f, 0), new Vector3(0.9f, 2.6f, 0.06f), colors[i % colors.Length], new Vector3(0, 0, 180));
            banners.Add(pivot);
        }

        // Windows that glow warm yellow, each flickering on its own.
        var windows = new List<Vector3>();
        for (var r = 0; r < 3; r++)
        {
            for (var c = 0; c < 3; c++) windows.Add(new Vector3(-3.6f + c * 3.6f, 9f + r * 4.6f, z - 2.05f));
        }
        foreach (var sx in new[] { -20f, 20f }) windows.Add(new Vector3(sx, 12f, z - 7.8f));
        foreach (var p in windows)
        {
            var w = Prim.Make(PrimitiveType.Cube, "GlowWindow", castle, p, new Vector3(1.1f, 2f, 0.2f), Color.white);
            var mat = Glow(new Color(1f, 0.85f, 0.45f), 1.4f);
            w.GetComponent<Renderer>().sharedMaterial = mat;
            windowMats.Add(mat);
            Prim.Make(PrimitiveType.Cube, "Sill", castle, p + new Vector3(0, -1.1f, -0.1f), new Vector3(1.5f, 0.15f, 0.35f), new Color(0.62f, 0.58f, 0.74f));
        }

        // Weathervane on the keep roof.
        var top = new Vector3(0, 33.8f, z + 4f);
        Prim.Make(PrimitiveType.Cylinder, "VanePole", castle, top, new Vector3(0.12f, 1.2f, 0.12f), new Color(0.35f, 0.3f, 0.3f));
        vane = new GameObject("Weathervane").transform;
        vane.SetParent(castle, false);
        vane.position = top + new Vector3(0, 1f, 0);
        Prim.Make(PrimitiveType.Cube, "Arrow", vane, Vector3.zero, new Vector3(0.12f, 0.12f, 2.4f), new Color(1f, 0.82f, 0.25f));
        Prim.Cone("Head", vane, new Vector3(0, 0, 1.3f), new Vector3(0.5f, 0.6f, 0.1f), new Color(1f, 0.82f, 0.25f), new Vector3(90, 0, 0));
        Prim.Cone("Fletch", vane, new Vector3(0, 0, -1.2f), new Vector3(0.9f, 0.7f, 0.08f), new Color(1f, 0.45f, 0.7f), new Vector3(-90, 0, 0));

        // Drawbridge: hinged at the gate, lifts when the unicorn gets close, held by two chains.
        var hinge = new GameObject("Drawbridge").transform;
        hinge.SetParent(castle, false);
        hinge.position = new Vector3(0, 0.1f, z - 5.8f);
        Prim.Make(PrimitiveType.Cube, "Planks", hinge, new Vector3(0, 0, -3.5f), new Vector3(5.6f, 0.25f, 7f), new Color(0.5f, 0.33f, 0.2f));
        for (var i = 0; i < 6; i++)
        {
            Prim.Make(PrimitiveType.Cube, "Plank", hinge, new Vector3(0, 0.14f, -0.6f - i * 1.15f), new Vector3(5.6f, 0.04f, 0.08f), new Color(0.35f, 0.22f, 0.13f));
        }
        bridge = hinge;
        for (var i = 0; i < 2; i++)
        {
            chains[i] = Prim.Make(PrimitiveType.Cylinder, "Chain", castle, Vector3.zero, new Vector3(0.1f, 1f, 0.1f), new Color(0.3f, 0.3f, 0.35f));
        }
    }

    private sealed class Glitter
    {
        public Transform T;
        public Vector3 Vel;
        public float Life, MaxLife, Size, Twinkle;
    }

    private readonly List<Glitter> glitter = new List<Glitter>();
    private Material[] glitterMats;

    // Tiny coloured motes that hang in the air, twinkle and slowly fade: a mist of rainbow glitter.
    private void EmitGlitter(Vector3 pos, Color tint)
    {
        if (glitter.Count > 500) return;
        if (glitterMats == null)
        {
            glitterMats = new Material[12];
            for (var i = 0; i < glitterMats.Length; i++) glitterMats[i] = Glow(Color.HSVToRGB(i / 12f, 0.5f, 1f), 1.6f);
        }
        var t = Prim.Make(PrimitiveType.Sphere, "Glitter", null, pos + Random.insideUnitSphere * 0.15f, Vector3.zero, Color.white);
        // Mostly rainbow hues, with some of the butterfly's own colour mixed in.
        var mat = Random.value < 0.3f ? CachedGlow(Color.Lerp(tint, Color.white, 0.3f), 1.6f) : glitterMats[Random.Range(0, glitterMats.Length)];
        t.GetComponent<Renderer>().sharedMaterial = mat;
        var life = Random.Range(0.9f, 1.8f);
        glitter.Add(new Glitter
        {
            T = t,
            Vel = Random.insideUnitSphere * 0.25f + Vector3.up * 0.12f,
            Life = life,
            MaxLife = life,
            Size = Random.Range(0.035f, 0.085f),
            Twinkle = Random.Range(0f, 6.3f),
        });
    }

    private void UpdateGlitter(float dt)
    {
        for (var i = glitter.Count - 1; i >= 0; i--)
        {
            var g = glitter[i];
            g.Life -= dt;
            if (g.Life <= 0f)
            {
                Destroy(g.T.gameObject);
                glitter.RemoveAt(i);
                continue;
            }
            g.T.position += g.Vel * dt;
            g.Vel *= Mathf.Exp(-1.2f * dt);
            var fade = Mathf.Sin(Mathf.Clamp01(g.Life / g.MaxLife) * Mathf.PI * 0.5f);
            var twinkle = 0.55f + 0.45f * Mathf.Sin(Time.time * 18f + g.Twinkle);
            g.T.localScale = Vector3.one * (g.Size * fade * twinkle);
        }
    }

    private void Update()
    {
        UpdateGlitter(Time.deltaTime);
        var t = Time.time;
        var dt = Time.deltaTime;
        var pos = target != null ? target.position : Vector3.zero;

        foreach (var b in butterflies)
        {
            var p = b.Root.position;
            var away = p - pos;
            away.y = 0;
            if (away.sqrMagnitude < 36f) b.Scare = 1.2f;
            b.Scare = Mathf.Max(0f, b.Scare - dt);

            var wander = new Vector3(Mathf.Sin(t * 0.7f + b.Phase) * 2.5f, Mathf.Sin(t * 1.3f + b.Phase * 2f) * 0.6f, Mathf.Cos(t * 0.6f + b.Phase) * 2.5f);
            var want = b.Home + wander - p;
            var vel = want.normalized * 1.2f;
            if (b.Scare > 0f) vel = away.normalized * 7f + Vector3.up * 2.5f;
            b.Vel = Vector3.Lerp(b.Vel, vel, 1f - Mathf.Exp(-4f * dt));
            p += b.Vel * dt;
            p.y = Mathf.Clamp(p.y, 0.6f, 14f);
            if (b.Scare <= 0f && p.y > b.Home.y + 1f) p.y -= dt;
            b.Root.position = p;
            if (b.Vel.sqrMagnitude > 0.05f) b.Root.rotation = Quaternion.Slerp(b.Root.rotation, Quaternion.LookRotation(new Vector3(b.Vel.x, 0, b.Vel.z) + Vector3.forward * 0.001f), 4f * dt);
            var flap = Mathf.Sin(t * (b.Scare > 0f ? 26f : 14f) + b.Phase) * 55f;
            var emit = dt * (b.Scare > 0f ? 40f : 14f);
            while (emit > 0f)
            {
                if (Random.value < emit) EmitGlitter(b.Root.position - b.Root.forward * 0.25f, b.Dust);
                emit -= 1f;
            }
            b.WingL.localRotation = Quaternion.Euler(0, 0, -flap);
            b.WingR.localRotation = Quaternion.Euler(0, 0, flap);
        }

        for (var i = 0; i < fireflies.Count; i++)
        {
            var h = fireflyHome[i];
            var ph = fireflyPhase[i];
            fireflies[i].position = h + new Vector3(Mathf.Sin(t * 0.5f + ph) * 1.5f, Mathf.Sin(t * 0.8f + ph) * 0.8f, Mathf.Cos(t * 0.4f + ph) * 1.5f);
            var glow = Mathf.Max(0f, Mathf.Sin(t * 1.6f + ph * 3f));
            fireflies[i].localScale = Vector3.one * (0.04f + 0.18f * glow);
        }

        foreach (var bird in birds)
        {
            var wanted = pos + target.rotation * bird.Offset + Vector3.up * (1f + Mathf.Sin(t * 2f + bird.Phase) * 0.4f);
            wanted.y = Mathf.Max(wanted.y, 1.2f);
            var from = bird.Root.position;
            var next = Vector3.Lerp(from, wanted, 1f - Mathf.Exp(-2.2f * dt));
            var dir = next - from;
            bird.Root.position = next;
            var face = dir.sqrMagnitude > 0.0001f ? dir : target.forward;
            bird.Root.rotation = Quaternion.Slerp(bird.Root.rotation, Quaternion.LookRotation(face), 5f * dt);
            var flap = Mathf.Sin(t * 12f + bird.Phase) * 50f;
            bird.WingL.localRotation = Quaternion.Euler(0, 0, -flap);
            bird.WingR.localRotation = Quaternion.Euler(0, 0, flap);
        }

        for (var i = 0; i < banners.Count; i++)
        {
            banners[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 2f + i) * 6f, 0, Mathf.Sin(t * 3f + i * 0.7f) * 9f);
        }
        for (var i = 0; i < windowMats.Count; i++)
        {
            var flicker = 1.1f + 0.35f * Mathf.Sin(t * 5f + i * 2.1f) + 0.15f * Mathf.Sin(t * 13f + i);
            windowMats[i].SetColor("_EmissionColor", new Color(1f, 0.85f, 0.45f) * flicker);
        }
        vane.rotation = Quaternion.Euler(0, 40f + Mathf.Sin(t * 0.6f) * 70f + Mathf.Sin(t * 1.9f) * 10f, 0);

        var near = Mathf.Abs(pos.x) < 14f && pos.z > castleZ - 34f;
        bridgeLift = Mathf.MoveTowards(bridgeLift, near ? 1f : 0f, dt * 0.7f);
        var angle = Mathf.SmoothStep(0f, 1f, bridgeLift) * 65f;
        bridge.localRotation = Quaternion.Euler(angle, 0, 0);
        var end = bridge.TransformPoint(new Vector3(0, 0, -7f));
        for (var i = 0; i < 2; i++)
        {
            var sx = i == 0 ? -2.6f : 2.6f;
            var a = new Vector3(sx, 7.2f, castleZ - 5.9f);
            var b = end + new Vector3(sx, 0.2f, 0);
            chains[i].position = (a + b) * 0.5f;
            chains[i].rotation = Quaternion.FromToRotation(Vector3.up, b - a);
            chains[i].localScale = new Vector3(0.1f, (b - a).magnitude * 0.5f, 0.1f);
        }
    }
}
