using System.Collections.Generic;
using UnityEngine;

public enum BalloonKind { Normal, Golden, Rainbow, Heart, Star, Storm, Boss }

// A balloon launched from the cannon. Air drag turns the throw into a slow float that eventually leaves the sky.
public sealed class Balloon : MonoBehaviour
{
    public const float Radius = 0.9f;
    private const float Drag = 0.35f;
    private const float EscapeHeight = 42f;

    // Popping and scoring read this list.
    public static readonly List<Balloon> Active = new List<Balloon>();

    // Raised when a balloon floats away unpopped.
    public static event System.Action<Balloon> Escaped;

    // Multiplies how fast balloons rise; the game raises it over time.
    public static float RiseScale = 1f;

    // Slow motion power-up: scales how fast balloons move.
    public static float TimeScale = 1f;

    public const float BossScale = 3.4f;

    public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);

    private static readonly Color[] Palette =
    {
        new Color(1f, 0.35f, 0.45f),
        new Color(0.35f, 0.65f, 1f),
        new Color(0.45f, 0.9f, 0.5f),
        new Color(0.7f, 0.45f, 1f),
        new Color(1f, 0.6f, 0.25f),
        new Color(1f, 0.5f, 0.8f),
    };

    // Ambient balloons hover in place and never escape.
    public bool Ambient;
    private Vector3 home;
    private Vector3 vel;
    private Vector3 drift;
    private float phase;
    private float punch;
    public int Hp = 1;
    public int MaxHp = 1;

    public BalloonKind Kind { get; private set; }
    public bool Golden => Kind == BalloonKind.Golden;
    public Color Color { get; private set; }

    public static Color KindColor(BalloonKind kind)
    {
        switch (kind)
        {
            case BalloonKind.Golden: return Gold;
            case BalloonKind.Rainbow: return new Color(1f, 0.55f, 0.85f);
            case BalloonKind.Heart: return new Color(1f, 0.25f, 0.4f);
            case BalloonKind.Star: return new Color(0.5f, 0.92f, 1f);
            case BalloonKind.Storm: return new Color(0.32f, 0.26f, 0.42f);
            case BalloonKind.Boss: return new Color(0.95f, 0.45f, 0.85f);
            default: return RandomColor();
        }
    }

    public static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    private static readonly Color[] RainbowBands =
    {
        new Color(1f, 0.35f, 0.4f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.95f, 0.4f),
        new Color(0.45f, 0.9f, 0.5f), new Color(0.4f, 0.7f, 1f), new Color(0.7f, 0.5f, 1f),
    };

    // Visual only; also used for the balloon the jester carries. Pivot is the balloon centre.
    public static Transform BuildVisual(string name, Transform parent, Color color, BalloonKind kind = BalloonKind.Normal)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        var white = new Color(1f, 1f, 1f);
        var knotY = -1.0f;

        switch (kind)
        {
            case BalloonKind.Heart:
                Prim.Make(PrimitiveType.Sphere, "LobeL", root, new Vector3(-0.42f, 0.35f, 0), new Vector3(1.25f, 1.35f, 1.3f), color);
                Prim.Make(PrimitiveType.Sphere, "LobeR", root, new Vector3(0.42f, 0.35f, 0), new Vector3(1.25f, 1.35f, 1.3f), color);
                Prim.Cone("Point", root, new Vector3(0, -0.45f, 0), new Vector3(1.75f, 1.5f, 1.2f), color, new Vector3(180, 0, 0));
                Prim.Make(PrimitiveType.Sphere, "Shine", root, new Vector3(-0.6f, 0.65f, -0.55f), new Vector3(0.3f, 0.4f, 0.2f), white);
                knotY = -1.25f;
                break;
            case BalloonKind.Star:
                Prim.Make(PrimitiveType.Sphere, "Core", root, Vector3.zero, new Vector3(1.1f, 1.1f, 0.9f), color);
                for (var i = 0; i < 5; i++)
                {
                    var a = i * 72f;
                    var dir = Quaternion.Euler(0, 0, -a) * Vector3.up;
                    Prim.Cone("Point", root, dir * 0.85f, new Vector3(0.7f, 1.2f, 0.7f), color, new Vector3(0, 0, -a));
                }
                Prim.Make(PrimitiveType.Sphere, "Shine", root, new Vector3(-0.25f, 0.25f, -0.4f), new Vector3(0.25f, 0.3f, 0.15f), white);
                knotY = -1.15f;
                break;
            case BalloonKind.Boss:
                Prim.Make(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(1.7f, 1.95f, 1.7f), color);
                Prim.Make(PrimitiveType.Sphere, "Shine", root, new Vector3(-0.4f, 0.45f, -0.62f), new Vector3(0.3f, 0.45f, 0.2f), white);
                for (var i = -1; i <= 1; i++)
                {
                    Prim.Cone("CrownSpike", root, new Vector3(i * 0.38f, 1.12f - Mathf.Abs(i) * 0.08f, 0), new Vector3(0.3f, 0.5f, 0.3f), new Color(1f, 0.82f, 0.2f), new Vector3(0, 0, -i * 14f));
                }
                Prim.Make(PrimitiveType.Cylinder, "CrownBand", root, new Vector3(0, 0.95f, 0), new Vector3(1.05f, 0.1f, 1.05f), new Color(1f, 0.82f, 0.2f));
                foreach (var sd in new[] { -1f, 1f })
                {
                    Prim.Make(PrimitiveType.Sphere, "Eye", root, new Vector3(0.38f * sd, 0.25f, -0.72f), new Vector3(0.4f, 0.46f, 0.2f), white);
                    Prim.Make(PrimitiveType.Sphere, "Pupil", root, new Vector3(0.36f * sd, 0.2f, -0.83f), new Vector3(0.18f, 0.22f, 0.12f), new Color(0.15f, 0.05f, 0.25f));
                    Prim.Make(PrimitiveType.Cube, "Brow", root, new Vector3(0.38f * sd, 0.62f, -0.74f), new Vector3(0.5f, 0.1f, 0.08f), new Color(0.3f, 0.1f, 0.4f), new Vector3(0, 0, -22f * sd));
                    Prim.Make(PrimitiveType.Sphere, "Cheek", root, new Vector3(0.58f * sd, -0.12f, -0.64f), new Vector3(0.28f, 0.18f, 0.1f), new Color(1f, 0.65f, 0.8f));
                }
                Prim.Make(PrimitiveType.Sphere, "Grin", root, new Vector3(0, -0.28f, -0.8f), new Vector3(0.55f, 0.2f, 0.1f), new Color(0.3f, 0.05f, 0.25f));
                Prim.Make(PrimitiveType.Cube, "Tooth", root, new Vector3(0, -0.2f, -0.86f), new Vector3(0.14f, 0.1f, 0.04f), white);
                knotY = -1.05f;
                break;
            case BalloonKind.Storm:
                Prim.Make(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(1.7f, 1.9f, 1.7f), color);
                Prim.Make(PrimitiveType.Sphere, "Puff", root, new Vector3(-0.55f, 0.75f, 0), new Vector3(0.9f, 0.6f, 0.9f), color * 1.25f);
                Prim.Make(PrimitiveType.Sphere, "Puff", root, new Vector3(0.5f, 0.8f, 0.1f), new Vector3(0.8f, 0.55f, 0.8f), color * 1.25f);
                foreach (var sd in new[] { -1f, 1f })
                {
                    Prim.Make(PrimitiveType.Sphere, "Eye", root, new Vector3(0.32f * sd, 0.15f, -0.72f), new Vector3(0.3f, 0.3f, 0.15f), white);
                    Prim.Make(PrimitiveType.Sphere, "Pupil", root, new Vector3(0.32f * sd, 0.12f, -0.8f), new Vector3(0.12f, 0.14f, 0.1f), Color.black);
                    Prim.Make(PrimitiveType.Cube, "Brow", root, new Vector3(0.32f * sd, 0.45f, -0.74f), new Vector3(0.45f, 0.09f, 0.08f), Color.black, new Vector3(0, 0, -25f * sd));
                }
                Prim.Cone("Bolt", root, new Vector3(0, -0.45f, -0.78f), new Vector3(0.5f, 0.7f, 0.1f), new Color(1f, 0.95f, 0.3f), new Vector3(180, 0, 0));
                break;
            default:
                Prim.Make(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(1.6f, 1.9f, 1.6f), kind == BalloonKind.Rainbow ? RainbowBands[0] : color);
                if (kind == BalloonKind.Rainbow)
                {
                    // Coloured rings wrapped around the ellipsoid make the stripes.
                    for (var i = 1; i < RainbowBands.Length; i++)
                    {
                        var y = -0.8f + i * 0.28f;
                        var r = (0.8f * Mathf.Sqrt(1f - (y / 0.95f) * (y / 0.95f)) + 0.02f) * 2f;
                        Prim.Make(PrimitiveType.Cylinder, "Band", root, new Vector3(0, y, 0), new Vector3(r, 0.14f, r), RainbowBands[i]);
                    }
                }
                Prim.Make(PrimitiveType.Sphere, "Shine", root, new Vector3(-0.35f, 0.4f, -0.6f), new Vector3(0.3f, 0.45f, 0.2f), white);
                break;
        }

        Prim.Cone("Knot", root, new Vector3(0, knotY, 0), new Vector3(0.3f, 0.3f, 0.3f), color * 0.8f, new Vector3(180, 0, 0));
        Prim.Make(PrimitiveType.Cylinder, "String", root, new Vector3(0, knotY - 1.0f, 0), new Vector3(0.04f, 0.9f, 0.04f), new Color(0.9f, 0.9f, 0.9f));
        return root;
    }

    public static Balloon Spawn(Transform parent, Vector3 position, Vector3 velocity, BalloonKind kind, Color color)
    {
        var visual = BuildVisual("Balloon", parent, color, kind);
        visual.position = position;
        var b = visual.gameObject.AddComponent<Balloon>();
        b.Kind = kind;
        b.Color = color;
        b.vel = velocity;
        b.drift = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.0f) * RiseScale, Random.Range(-0.6f, 0.2f));
        b.phase = Random.value * 6.28f;
        return b;
    }

    public static Balloon SpawnAmbient(Transform parent, Vector3 position, BalloonKind kind, Color color)
    {
        var b = Spawn(parent, position, Vector3.zero, kind, color);
        b.Ambient = true;
        b.home = position;
        return b;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        var dt = Time.deltaTime * TimeScale;
        if (Ambient)
        {
            var bt = Time.time * 0.5f + phase;
            transform.position = home + new Vector3(Mathf.Sin(bt * 0.8f) * 1.2f, Mathf.Sin(bt * 1.3f) * 0.9f, Mathf.Cos(bt * 0.7f) * 1.2f);
        }
        else
        {
            vel = Vector3.Lerp(vel, drift, 1f - Mathf.Exp(-Drag * dt));
            transform.position += vel * dt;
        }
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.7f + phase) * 7f, 0, Mathf.Cos(Time.time * 1.3f + phase) * 7f);

        if (Effects.Instance != null && Random.value < 0.25f)
        {
            if (Kind == BalloonKind.Golden) Effects.Instance.Burst(transform.position + Random.insideUnitSphere, Gold, 1, 0.5f, 0.6f);
            else if (Kind == BalloonKind.Star) Effects.Instance.Burst(transform.position + Random.insideUnitSphere, Color, 1, 0.5f, 0.6f);
            else if (Kind == BalloonKind.Rainbow) Effects.Instance.Burst(transform.position + Random.insideUnitSphere, RainbowBands[Random.Range(0, RainbowBands.Length)], 1, 0.5f, 0.6f);
            else if (Kind == BalloonKind.Heart) Effects.Instance.Burst(transform.position + Random.insideUnitSphere, new Color(1f, 0.6f, 0.75f), 1, 0.4f, 0.6f);
            else if (Kind == BalloonKind.Storm) Effects.Instance.Burst(transform.position + Random.insideUnitSphere * 1.2f, new Color(1f, 0.95f, 0.3f), 1, 2f, 0.25f);
        }
        if (Kind == BalloonKind.Heart) transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 6f + phase) * 0.06f);

        if (Kind == BalloonKind.Boss)
        {
            punch = Mathf.Max(0f, punch - Time.deltaTime * 3f);
            transform.localScale = Vector3.one * (BossScale * (1f + punch * 0.18f + Mathf.Sin(Time.time * 2.2f) * 0.03f));
        }

        if (transform.position.y > EscapeHeight)
        {
            Escaped?.Invoke(this);
            Pop();
        }
    }

    public void SetDrift(Vector3 d) => drift = d;

    // The boss squashes and is shoved away from the horn when it takes a hit.
    public void Hit(Vector3 dir)
    {
        punch = 1f;
        vel += dir * 5f;
    }

    // Drawn toward a point (the star magnet) without fighting the drift physics.
    public void Pull(Vector3 point, float radius, float speed, float dt)
    {
        var d = point - transform.position;
        var dist = d.magnitude;
        if (dist > radius || dist < 0.01f) return;
        transform.position += d / dist * speed * (1f - dist / radius * 0.6f) * dt;
    }

    // Removes the balloon immediately so it can't be hit or counted twice.
    public void Pop()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
