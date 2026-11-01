using System.Collections.Generic;
using UnityEngine;

public enum PowerKind { RainbowTrail, SlowMo, WingBoost }

// A glowing orb floating in the sky. Fly into it to collect the power.
public sealed class PowerUp : MonoBehaviour
{
    public const float Lifetime = 16f;
    public const float PickupRadius = 2.6f;

    public static readonly List<PowerUp> Active = new List<PowerUp>();

    public PowerKind Kind { get; private set; }
    private Transform spinner;
    private float age;
    private Vector3 basePos;
    private Vector3 baseScale;

    public static Color KindColor(PowerKind kind)
    {
        switch (kind)
        {
            case PowerKind.RainbowTrail: return new Color(1f, 0.55f, 0.85f);
            case PowerKind.SlowMo: return new Color(0.45f, 0.7f, 1f);
            default: return new Color(1f, 0.85f, 0.3f);
        }
    }

    public static string KindName(PowerKind kind)
    {
        switch (kind)
        {
            case PowerKind.RainbowTrail: return "RAINBOW TRAIL!";
            case PowerKind.SlowMo: return "SLOW MOTION!";
            default: return "WING BOOST!";
        }
    }

    private static Material Glow(Color color, float strength)
    {
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * strength);
        m.SetFloat("_Glossiness", 0.8f);
        return m;
    }

    private static Transform Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
    {
        var t = Prim.Make(type, "Part", parent, pos, scale, Color.white, euler);
        t.GetComponent<Renderer>().sharedMaterial = mat;
        return t;
    }

    public static PowerUp Spawn(Transform parent, Vector3 pos, PowerKind kind)
    {
        var root = new GameObject("PowerUp").transform;
        root.SetParent(parent, false);
        root.position = pos;
        var p = root.gameObject.AddComponent<PowerUp>();
        p.Kind = kind;
        p.basePos = pos;
        p.baseScale = Vector3.one;

        var color = KindColor(kind);
        var orb = Glow(color, 1.1f);
        var white = Glow(Color.white, 1.4f);
        Part(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.5f, Glow(color * 0.7f, 0.5f));
        p.spinner = new GameObject("Spinner").transform;
        p.spinner.SetParent(root, false);
        var s = p.spinner;

        switch (kind)
        {
            case PowerKind.RainbowTrail:
                for (var i = 0; i < 7; i++)
                {
                    var a = i / 7f * Mathf.PI * 2f;
                    Part(s, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.2f, Mathf.Sin(a)) * 0.95f, Vector3.one * 0.32f, Glow(Color.HSVToRGB(i / 7f, 0.6f, 1f), 1.6f));
                }
                Part(s, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.7f, white);
                break;
            case PowerKind.SlowMo:
                Part(s, PrimitiveType.Cylinder, new Vector3(0, 0, 0), new Vector3(1.1f, 0.05f, 1.1f), white, new Vector3(90, 0, 0));
                Part(s, PrimitiveType.Cube, new Vector3(0, 0.22f, -0.04f), new Vector3(0.1f, 0.5f, 0.08f), orb);
                Part(s, PrimitiveType.Cube, new Vector3(0.16f, 0, -0.04f), new Vector3(0.36f, 0.1f, 0.08f), orb);
                break;
            default:
                foreach (var sd in new[] { -1f, 1f })
                {
                    Part(s, PrimitiveType.Sphere, new Vector3(sd * 0.55f, 0.15f, 0), new Vector3(0.9f, 0.1f, 0.4f), white, new Vector3(0, 0, sd * 25f));
                    Part(s, PrimitiveType.Sphere, new Vector3(sd * 0.5f, -0.05f, 0), new Vector3(0.6f, 0.08f, 0.3f), orb, new Vector3(0, 0, sd * 10f));
                }
                Part(s, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.5f, white);
                break;
        }
        return p;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        age += Time.deltaTime;
        transform.position = basePos + Vector3.up * (Mathf.Sin(age * 2f) * 0.5f);
        spinner.Rotation(age);

        // Shrinks away at the end; blinks over the last few seconds as a warning.
        var left = Lifetime - age;
        var blink = left < 4f ? 0.75f + 0.25f * Mathf.Sin(age * 14f) : 1f;
        var grow = Mathf.Clamp01(age * 3f) * Mathf.Clamp01(left * 2f);
        transform.localScale = baseScale * (grow * blink);
        if (Effects.Instance != null && Random.value < 0.15f)
        {
            Effects.Instance.Burst(transform.position + Random.insideUnitSphere * 0.9f, KindColor(Kind), 1, 0.4f, 0.6f);
        }
        if (left <= 0f) Collect();
    }

    public void Collect()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}

internal static class SpinExt
{
    public static void Rotation(this Transform t, float age) => t.localRotation = Quaternion.Euler(0, age * 90f, Mathf.Sin(age * 1.5f) * 12f);
}
