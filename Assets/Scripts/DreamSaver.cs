using System.Collections.Generic;
using UnityEngine;

// The horn pops a balloon and the dream inside is released as a glowing wisp, which the unicorn saves
// by drawing it into her horn in a flash of rainbow light.
public sealed class DreamSaver : MonoBehaviour
{
    private sealed class Mote
    {
        public Transform T;
        public Vector3 Vel;
        public float Life, MaxLife, Size, Drag;
        public bool Grow;
    }

    private sealed class Wisp
    {
        public Transform Core, A, B;
        public Vector3 Start, Control;
        public Color Color;
        public float Age, Size, Release;
        public bool Boss;
    }

    public static DreamSaver Instance { get; private set; }
    public static int Saved { get; private set; }

    private const int MaxWisps = 24;
    private const int MaxMotes = 420;

    private Unicorn unicorn;
    private readonly List<Mote> motes = new List<Mote>();
    private readonly List<Wisp> wisps = new List<Wisp>();
    private Transform hornGlow;
    private Material hornMat;
    private float hornFlash;
    private static readonly Color[] Rainbow =
    {
        new Color(1f, 0.35f, 0.45f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.95f, 0.45f),
        new Color(0.45f, 1f, 0.6f), new Color(0.45f, 0.8f, 1f), new Color(0.8f, 0.55f, 1f),
    };

    public static void Setup(Unicorn u)
    {
        if (Instance == null) return;
        Instance.unicorn = u;
        Saved = 0;
        foreach (var w in Instance.wisps) Instance.DestroyWisp(w);
        Instance.wisps.Clear();
        Instance.hornFlash = 0f;
    }

    // Called at the moment the horn pops a balloon.
    public static void Restore(int n) => Saved = n;

    public static void Release(Vector3 pos, Color color, bool boss = false)
    {
        if (Instance != null) Instance.DoRelease(pos, color, boss);
    }

    private void Awake()
    {
        Instance = this;
        hornMat = GlowMat(Color.white);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static Material GlowMat(Color c)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * 1.6f);
        return m;
    }

    private Transform Orb(string name, float size, Color color, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().sharedMaterial = GlowMat(color);
        return go.transform;
    }

    private void AddMote(Vector3 pos, Vector3 vel, Color color, float size, float life, float drag = 2f, bool grow = false)
    {
        if (motes.Count >= MaxMotes) return;
        var t = Prim.Make(PrimitiveType.Sphere, "Mote", transform, pos, Vector3.one * size, color);
        motes.Add(new Mote { T = t, Vel = vel, Life = life, MaxLife = life, Size = size, Drag = drag, Grow = grow });
    }

    private void DoRelease(Vector3 pos, Color color, bool boss)
    {
        var scale = boss ? 2.4f : 1f;

        // Pop flash, an expanding ring of light and pieces of the balloon skin.
        AddMote(pos, Vector3.zero, Color.white, 0.5f * scale, 0.16f, 0f, true);
        for (var i = 0; i < (boss ? 26 : 14); i++)
        {
            var a = i / (boss ? 26f : 14f) * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), Random.Range(-0.15f, 0.15f), Mathf.Sin(a));
            AddMote(pos, dir * 8f * scale, Color.Lerp(color, Color.white, 0.55f), 0.18f * scale, 0.45f, 3f);
        }
        for (var i = 0; i < (boss ? 16 : 8); i++)
        {
            var shard = Prim.Make(PrimitiveType.Cube, "Shard", transform, pos, new Vector3(0.28f, 0.28f, 0.04f) * scale, color, Random.insideUnitSphere * 180f);
            motes.Add(new Mote { T = shard, Vel = Random.onUnitSphere * Random.Range(3f, 7f) * scale + Vector3.up * 2f, Life = 0.7f, MaxLife = 0.7f, Size = 0.28f * scale, Drag = 1.2f });
        }

        if (wisps.Count >= MaxWisps)
        {
            Saved++;
            return;
        }
        var w = new Wisp
        {
            Start = pos,
            Color = Color.Lerp(color, Color.white, 0.35f),
            Size = boss ? 1.1f : 0.5f,
            Boss = boss,
            Release = boss ? 0.8f : 0.45f,
        };
        w.Core = Orb("Dream", w.Size, Color.Lerp(w.Color, Color.white, 0.5f));
        w.A = Orb("DreamMote", w.Size * 0.32f, w.Color, w.Core);
        w.B = Orb("DreamMote", w.Size * 0.24f, Color.white, w.Core);
        w.Core.position = pos;
        wisps.Add(w);
    }

    private void DestroyWisp(Wisp w)
    {
        if (w.Core != null) Destroy(w.Core.gameObject);
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        UpdateMotes(dt);
        if (unicorn == null || unicorn.HornTip == null) return;
        var horn = unicorn.HornTip.position;

        for (var i = wisps.Count - 1; i >= 0; i--)
        {
            var w = wisps[i];
            w.Age += dt;
            var tNow = Time.time;
            // Orbiting sparkles around the dream.
            var spin = tNow * 9f + i;
            var radius = w.Size * 1.1f;
            w.A.localPosition = new Vector3(Mathf.Cos(spin), Mathf.Sin(spin * 1.3f), Mathf.Sin(spin)) * radius / w.Size;
            w.B.localPosition = new Vector3(Mathf.Cos(spin + 3f), Mathf.Sin(spin * 0.8f + 2f), Mathf.Sin(spin + 3f)) * radius / w.Size;

            if (w.Age < w.Release)
            {
                // Released: the dream floats up and swells, as if taking its first breath.
                var s = w.Age / w.Release;
                var rise = w.Start + Vector3.up * (1.6f * (w.Boss ? 2f : 1f) * Mathf.Sin(s * Mathf.PI * 0.5f)) + new Vector3(Mathf.Sin(s * 6f), 0, Mathf.Cos(s * 6f)) * 0.25f;
                w.Core.position = rise;
                w.Core.localScale = Vector3.one * w.Size * (0.3f + 0.9f * Mathf.Sin(s * Mathf.PI * 0.5f) + 0.1f * Mathf.Sin(tNow * 20f));
                w.Control = rise;
                if (Random.value < 0.6f) AddMote(rise, Random.insideUnitSphere * 0.8f, w.Color, 0.1f, 0.4f);
                continue;
            }

            // Saved: it curves into the horn tip, speeding up as it goes.
            var flight = 0.55f;
            var u = Mathf.Clamp01((w.Age - w.Release) / flight);
            var ease = u * u * (3f - 2f * u);
            var from = w.Control;
            var mid = Vector3.Lerp(from, horn, 0.5f) + Vector3.up * 2.2f;
            var p = Vector3.Lerp(Vector3.Lerp(from, mid, ease), Vector3.Lerp(mid, horn, ease), ease);
            w.Core.position = p;
            w.Core.localScale = Vector3.one * w.Size * Mathf.Lerp(1f, 0.35f, ease);
            if (Random.value < 0.9f) AddMote(p, Random.insideUnitSphere * 0.6f, Rainbow[Random.Range(0, Rainbow.Length)], 0.12f, 0.35f);

            if (u >= 1f)
            {
                Absorb(w, horn);
                DestroyWisp(w);
                wisps.RemoveAt(i);
            }
        }

        UpdateHornGlow(horn);
    }

    private void Absorb(Wisp w, Vector3 horn)
    {
        Saved++;
        hornFlash = w.Boss ? 1.4f : 0.55f;
        var n = w.Boss ? 36 : 14;
        for (var i = 0; i < n; i++)
        {
            AddMote(horn, Random.onUnitSphere * Random.Range(2f, w.Boss ? 9f : 5f), Rainbow[i % Rainbow.Length], Random.Range(0.12f, 0.24f), 0.7f, 2.5f);
        }
        Sfx.Saved(Mathf.Min(1.5f, 1f + Saved % 5 * 0.06f));
    }

    private void UpdateHornGlow(Vector3 horn)
    {
        if (hornGlow == null) hornGlow = Orb("HornGlow", 0.01f, new Color(1f, 0.95f, 0.7f));
        hornFlash = Mathf.Max(0f, hornFlash - Time.deltaTime * 2.2f);
        hornGlow.position = horn;
        // Quick swell, then fade, as the dream is absorbed.
        hornGlow.localScale = Vector3.one * (hornFlash > 0.02f ? 0.25f + hornFlash * 1.1f : 0.001f);
    }

    private void UpdateMotes(float dt)
    {
        for (var i = motes.Count - 1; i >= 0; i--)
        {
            var m = motes[i];
            m.Life -= dt;
            if (m.Life <= 0f || m.T == null)
            {
                if (m.T != null) Destroy(m.T.gameObject);
                motes.RemoveAt(i);
                continue;
            }
            m.Vel *= Mathf.Exp(-m.Drag * dt);
            m.T.position += m.Vel * dt;
            var k = m.Life / m.MaxLife;
            m.T.localScale = Vector3.one * (m.Grow ? m.Size * (1f + (1f - k) * 3f) * k : m.Size * k);
        }
    }
}
