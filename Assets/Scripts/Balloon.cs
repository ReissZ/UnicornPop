using System.Collections.Generic;
using UnityEngine;

// Batch 3: a balloon that is launched from the cannon, then drifts and floats away.
public sealed class Balloon : MonoBehaviour
{
    public const float Radius = 0.42f;
    private const float Drag = 1.5f;

    // Other systems (popping, scoring) read this list.
    public static readonly List<Balloon> Active = new List<Balloon>();

    private static readonly Color[] Palette =
    {
        new Color(1f, 0.35f, 0.45f),
        new Color(0.35f, 0.65f, 1f),
        new Color(0.45f, 0.9f, 0.5f),
        new Color(0.7f, 0.45f, 1f),
        new Color(1f, 0.6f, 0.25f),
        new Color(1f, 0.5f, 0.8f),
    };

    private Vector2 vel;
    private float driftX;
    private float floatSpeed;
    private float swayPhase;
    private Camera cam;

    public static Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    // Visual only; also used for the deflated balloon the jester carries.
    public static Transform BuildVisual(string name, Transform parent, Color color)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        Art.Spr("String", root, Art.Square, new Vector2(0, -0.9f), new Vector2(0.025f, 0.8f), new Color(0.4f, 0.4f, 0.45f), 16);
        Art.Spr("Knot", root, Art.Triangle, new Vector2(0, -0.5f), new Vector2(0.16f, 0.16f), color * 0.8f, 17, 180f);
        Art.Spr("Body", root, Art.Circle, Vector2.zero, new Vector2(0.84f, 1f), color, 18);
        Art.Spr("Shine", root, Art.Circle, new Vector2(-0.17f, 0.2f), new Vector2(0.2f, 0.3f), new Color(1f, 1f, 1f, 0.55f), 19, 20f);
        return root;
    }

    public static Balloon Spawn(Transform parent, Vector3 position, Vector2 velocity, Color color)
    {
        var visual = BuildVisual("Balloon", parent, color);
        visual.position = position;
        var b = visual.gameObject.AddComponent<Balloon>();
        b.vel = velocity;
        b.driftX = Random.Range(-0.5f, 0.1f);
        b.floatSpeed = Random.Range(0.8f, 1.1f);
        b.swayPhase = Random.value * 6.28f;
        b.cam = Camera.main;
        return b;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        var dt = Time.deltaTime;
        // The cannon throws the balloon, then air drag lets it settle into a gentle float.
        vel = Vector2.Lerp(vel, new Vector2(driftX, floatSpeed), 1f - Mathf.Exp(-Drag * dt));
        transform.position += (Vector3)(vel * dt);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2f + swayPhase) * 6f);

        var top = cam != null ? cam.transform.position.y + cam.orthographicSize : 6f;
        if (transform.position.y > top + 1.5f)
        {
            Destroy(gameObject);
        }
    }
}
