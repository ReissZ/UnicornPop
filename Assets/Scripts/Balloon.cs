using System.Collections.Generic;
using UnityEngine;

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

    private Vector3 vel;
    private Vector3 drift;
    private float phase;

    public bool Golden { get; private set; }
    public Color Color { get; private set; }

    public static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    // Visual only; also used for the balloon the jester carries. Pivot is the balloon centre.
    public static Transform BuildVisual(string name, Transform parent, Color color)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        Prim.Make(PrimitiveType.Sphere, "Body", root, Vector3.zero, new Vector3(1.6f, 1.9f, 1.6f), color);
        Prim.Make(PrimitiveType.Sphere, "Shine", root, new Vector3(-0.35f, 0.4f, -0.6f), new Vector3(0.3f, 0.45f, 0.2f), new Color(1f, 1f, 1f));
        Prim.Cone("Knot", root, new Vector3(0, -1.0f, 0), new Vector3(0.3f, 0.3f, 0.3f), color * 0.8f, new Vector3(180, 0, 0));
        Prim.Make(PrimitiveType.Cylinder, "String", root, new Vector3(0, -2.0f, 0), new Vector3(0.04f, 0.9f, 0.04f), new Color(0.9f, 0.9f, 0.9f));
        return root;
    }

    public static Balloon Spawn(Transform parent, Vector3 position, Vector3 velocity, Color color, bool golden = false)
    {
        var visual = BuildVisual("Balloon", parent, color);
        visual.position = position;
        var b = visual.gameObject.AddComponent<Balloon>();
        b.Golden = golden;
        b.Color = color;
        b.vel = velocity;
        b.drift = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.0f), Random.Range(-0.6f, 0.2f));
        b.phase = Random.value * 6.28f;
        return b;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        var dt = Time.deltaTime;
        vel = Vector3.Lerp(vel, drift, 1f - Mathf.Exp(-Drag * dt));
        transform.position += vel * dt;
        transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.7f + phase) * 7f, 0, Mathf.Cos(Time.time * 1.3f + phase) * 7f);

        if (Golden && Effects.Instance != null && Random.value < 0.25f)
        {
            Effects.Instance.Burst(transform.position + Random.insideUnitSphere, Gold, 1, 0.5f, 0.6f);
        }

        if (transform.position.y > EscapeHeight)
        {
            Escaped?.Invoke(this);
            Pop();
        }
    }

    // Removes the balloon immediately so it can't be hit or counted twice.
    public void Pop()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
