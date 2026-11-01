using System.Collections.Generic;
using UnityEngine;

// Level themes: the sky, sun, light and fog blend smoothly between a sunny meadow, sunset, starry night,
// candy clouds and an aurora as the levels go up.
public sealed partial class Dreamland
{
    private struct Theme
    {
        public string Name;
        public Color Horizon, Low, Mid, Top;
        public Color SunTint;
        public float Glow, SunPitch, Stars, Candy, Aurora, Rainbow;
        public Color Light;
        public float LightIntensity, LightPitch;
        public Color AmbSky, AmbEquator, AmbGround;

        public static Theme Lerp(Theme a, Theme b, float t) => new Theme
        {
            Name = t < 0.5f ? a.Name : b.Name,
            Horizon = Color.Lerp(a.Horizon, b.Horizon, t),
            Low = Color.Lerp(a.Low, b.Low, t),
            Mid = Color.Lerp(a.Mid, b.Mid, t),
            Top = Color.Lerp(a.Top, b.Top, t),
            SunTint = Color.Lerp(a.SunTint, b.SunTint, t),
            Glow = Mathf.Lerp(a.Glow, b.Glow, t),
            SunPitch = Mathf.Lerp(a.SunPitch, b.SunPitch, t),
            Stars = Mathf.Lerp(a.Stars, b.Stars, t),
            Candy = Mathf.Lerp(a.Candy, b.Candy, t),
            Aurora = Mathf.Lerp(a.Aurora, b.Aurora, t),
            Rainbow = Mathf.Lerp(a.Rainbow, b.Rainbow, t),
            Light = Color.Lerp(a.Light, b.Light, t),
            LightIntensity = Mathf.Lerp(a.LightIntensity, b.LightIntensity, t),
            LightPitch = Mathf.Lerp(a.LightPitch, b.LightPitch, t),
            AmbSky = Color.Lerp(a.AmbSky, b.AmbSky, t),
            AmbEquator = Color.Lerp(a.AmbEquator, b.AmbEquator, t),
            AmbGround = Color.Lerp(a.AmbGround, b.AmbGround, t),
        };
    }

    private static Color C(float r, float g, float b) => new Color(r, g, b);

    private static readonly Theme[] Themes =
    {
        new Theme
        {
            Name = "Sunny Meadow",
            Horizon = Horizon, Low = C(0.95f, 0.74f, 0.88f), Mid = C(0.66f, 0.74f, 1f), Top = C(0.42f, 0.6f, 0.98f),
            SunTint = Color.white, Glow = 1f, SunPitch = 0f, Stars = 0f, Candy = 0f, Aurora = 0f, Rainbow = 1f,
            Light = C(1f, 0.96f, 0.88f), LightIntensity = 1.15f, LightPitch = 50f,
            AmbSky = C(0.6f, 0.66f, 0.95f), AmbEquator = C(0.85f, 0.75f, 0.85f), AmbGround = C(0.5f, 0.6f, 0.45f),
        },
        new Theme
        {
            Name = "Sunset Glow",
            Horizon = C(1f, 0.62f, 0.4f), Low = C(1f, 0.52f, 0.5f), Mid = C(0.82f, 0.4f, 0.62f), Top = C(0.35f, 0.28f, 0.6f),
            SunTint = C(1f, 0.6f, 0.35f), Glow = 1.4f, SunPitch = 19f, Stars = 0.25f, Candy = 0f, Aurora = 0f, Rainbow = 0.5f,
            Light = C(1f, 0.66f, 0.45f), LightIntensity = 1.0f, LightPitch = 16f,
            AmbSky = C(0.55f, 0.42f, 0.7f), AmbEquator = C(1f, 0.6f, 0.5f), AmbGround = C(0.45f, 0.35f, 0.4f),
        },
        new Theme
        {
            Name = "Starry Night",
            Horizon = C(0.32f, 0.3f, 0.6f), Low = C(0.2f, 0.2f, 0.5f), Mid = C(0.1f, 0.11f, 0.34f), Top = C(0.04f, 0.04f, 0.18f),
            SunTint = C(0.8f, 0.88f, 1f), Glow = 0.35f, SunPitch = -8f, Stars = 1f, Candy = 0f, Aurora = 0f, Rainbow = 0.1f,
            Light = C(0.6f, 0.7f, 1f), LightIntensity = 0.55f, LightPitch = 55f,
            AmbSky = C(0.28f, 0.3f, 0.6f), AmbEquator = C(0.3f, 0.3f, 0.55f), AmbGround = C(0.2f, 0.25f, 0.35f),
        },
        new Theme
        {
            Name = "Candy Clouds",
            Horizon = C(1f, 0.88f, 0.95f), Low = C(1f, 0.8f, 0.92f), Mid = C(0.8f, 0.9f, 1f), Top = C(0.7f, 0.85f, 1f),
            SunTint = C(1f, 0.85f, 0.95f), Glow = 1.1f, SunPitch = 6f, Stars = 0f, Candy = 1f, Aurora = 0f, Rainbow = 1f,
            Light = C(1f, 0.9f, 0.95f), LightIntensity = 1.2f, LightPitch = 52f,
            AmbSky = C(0.8f, 0.75f, 1f), AmbEquator = C(1f, 0.82f, 0.92f), AmbGround = C(0.6f, 0.7f, 0.6f),
        },
        new Theme
        {
            Name = "Aurora Dreams",
            Horizon = C(0.25f, 0.4f, 0.62f), Low = C(0.1f, 0.22f, 0.45f), Mid = C(0.06f, 0.12f, 0.32f), Top = C(0.03f, 0.05f, 0.2f),
            SunTint = C(0.85f, 1f, 0.95f), Glow = 0.3f, SunPitch = -6f, Stars = 0.9f, Candy = 0f, Aurora = 1f, Rainbow = 0f,
            Light = C(0.65f, 0.85f, 1f), LightIntensity = 0.6f, LightPitch = 58f,
            AmbSky = C(0.25f, 0.45f, 0.6f), AmbEquator = C(0.3f, 0.4f, 0.6f), AmbGround = C(0.2f, 0.3f, 0.4f),
        },
        new Theme
        {
            Name = "Mossy Glade",
            Horizon = C(0.7f, 0.9f, 0.78f), Low = C(0.68f, 0.9f, 0.76f), Mid = C(0.48f, 0.78f, 0.74f), Top = C(0.3f, 0.58f, 0.68f),
            SunTint = C(0.95f, 1f, 0.8f), Glow = 1f, SunPitch = 4f, Stars = 0f, Candy = 0f, Aurora = 0f, Rainbow = 0.6f,
            Light = C(0.92f, 1f, 0.8f), LightIntensity = 1.05f, LightPitch = 48f,
            AmbSky = C(0.5f, 0.75f, 0.7f), AmbEquator = C(0.6f, 0.8f, 0.65f), AmbGround = C(0.3f, 0.5f, 0.35f),
        },
        new Theme
        {
            Name = "Golden Canopy",
            Horizon = C(1f, 0.85f, 0.5f), Low = C(0.95f, 0.8f, 0.45f), Mid = C(0.7f, 0.72f, 0.5f), Top = C(0.3f, 0.5f, 0.5f),
            SunTint = C(1f, 0.85f, 0.4f), Glow = 1.4f, SunPitch = 14f, Stars = 0.1f, Candy = 0f, Aurora = 0f, Rainbow = 0.4f,
            Light = C(1f, 0.85f, 0.5f), LightIntensity = 1.0f, LightPitch = 22f,
            AmbSky = C(0.55f, 0.6f, 0.45f), AmbEquator = C(0.9f, 0.75f, 0.45f), AmbGround = C(0.35f, 0.4f, 0.25f),
        },
        new Theme
        {
            Name = "Firefly Night",
            Horizon = C(0.2f, 0.34f, 0.4f), Low = C(0.1f, 0.24f, 0.34f), Mid = C(0.07f, 0.14f, 0.26f), Top = C(0.03f, 0.05f, 0.14f),
            SunTint = C(0.8f, 1f, 0.9f), Glow = 0.3f, SunPitch = -8f, Stars = 1f, Candy = 0f, Aurora = 0.4f, Rainbow = 0.05f,
            Light = C(0.6f, 0.9f, 0.85f), LightIntensity = 0.55f, LightPitch = 56f,
            AmbSky = C(0.2f, 0.4f, 0.45f), AmbEquator = C(0.25f, 0.4f, 0.45f), AmbGround = C(0.12f, 0.25f, 0.22f),
        },
        new Theme
        {
            Name = "Crystal Dawn",
            Horizon = C(0.85f, 0.88f, 1f), Low = C(0.8f, 0.82f, 1f), Mid = C(0.7f, 0.85f, 1f), Top = C(0.55f, 0.7f, 0.98f),
            SunTint = C(0.9f, 0.95f, 1f), Glow = 1.1f, SunPitch = 8f, Stars = 0.15f, Candy = 0f, Aurora = 0f, Rainbow = 1f,
            Light = C(0.92f, 0.95f, 1f), LightIntensity = 1.15f, LightPitch = 44f,
            AmbSky = C(0.7f, 0.78f, 1f), AmbEquator = C(0.85f, 0.85f, 1f), AmbGround = C(0.6f, 0.6f, 0.8f),
        },
        new Theme
        {
            Name = "Prism Noon",
            Horizon = C(0.9f, 0.97f, 1f), Low = C(0.85f, 0.95f, 1f), Mid = C(0.6f, 0.85f, 1f), Top = C(0.4f, 0.7f, 1f),
            SunTint = Color.white, Glow = 1.3f, SunPitch = 0f, Stars = 0f, Candy = 0f, Aurora = 0f, Rainbow = 1f,
            Light = C(1f, 1f, 1f), LightIntensity = 1.25f, LightPitch = 58f,
            AmbSky = C(0.65f, 0.8f, 1f), AmbEquator = C(0.85f, 0.9f, 1f), AmbGround = C(0.6f, 0.65f, 0.85f),
        },
    };

    private const float ThemeBlendSeconds = 3f;

    private Mesh domeMesh;
    private Material rainbowMat;
    private Transform sunPivot;
    private readonly List<Material> sunMats = new List<Material>();
    private readonly List<Color> sunBase = new List<Color>();
    private Material starMat;
    private readonly List<Transform> starBalls = new List<Transform>();
    private readonly List<Material> candyMats = new List<Material>();
    private readonly List<Color> candyBase = new List<Color>();
    private Transform candyRoot;
    private readonly List<Material> auroraMats = new List<Material>();
    private readonly List<Transform> auroraBands = new List<Transform>();
    private Light sunLight;
    private Theme from = Themes[0], to = Themes[0], current = Themes[0];
    private float blend = 1f;

    public string ThemeName => to.Name;

    public void SnapTheme(int index)
    {
        var t = Themes[((index % Themes.Length) + Themes.Length) % Themes.Length];
        from = to = current = t;
        blend = 1f;
    }

    public void SetTheme(int index)
    {
        from = current;
        to = Themes[((index % Themes.Length) + Themes.Length) % Themes.Length];
        blend = 0f;
    }

    private void GatherSun()
    {
        sunPivot = new GameObject("SunPivot").transform;
        sunPivot.SetParent(rig, false);
        var names = new[] { "SunHalo", "SunGlow", "SunRays", "SunDisc" };
        foreach (var n in names)
        {
            var t = rig.Find(n);
            if (t != null) t.SetParent(sunPivot, false);
        }
        foreach (var r in sunPivot.GetComponentsInChildren<Renderer>())
        {
            var m = r.sharedMaterial;
            if (sunMats.Contains(m)) continue;
            sunMats.Add(m);
            sunBase.Add(m.color);
        }
    }

    private void BuildThemes()
    {
        // Stars share one material and twinkle by size.
        starMat = SkyMat(1001);
        starMat.color = new Color(1f, 1f, 0.95f, 0f);
        var stars = new GameObject("Stars").transform;
        stars.SetParent(rig, false);
        for (var i = 0; i < 170; i++)
        {
            var el = Random.Range(6f, 85f) * Mathf.Deg2Rad;
            var az = Random.Range(0f, Mathf.PI * 2f);
            var dir = new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az));
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(stars, false);
            s.transform.localPosition = dir * 280f;
            s.transform.localScale = Vector3.one * Random.Range(0.9f, 2.2f);
            s.GetComponent<Renderer>().sharedMaterial = starMat;
            starBalls.Add(s.transform);
        }

        BuildCandyClouds();
        BuildAurora();
    }

    private void BuildCandyClouds()
    {
        candyRoot = new GameObject("CandyClouds").transform;
        candyRoot.SetParent(rig, false);
        Color[] palette =
        {
            new Color(1f, 0.75f, 0.88f, 0.85f), new Color(0.75f, 1f, 0.9f, 0.85f),
            new Color(0.85f, 0.8f, 1f, 0.85f), new Color(1f, 0.95f, 0.75f, 0.85f),
        };
        foreach (var c in palette)
        {
            var m = SkyMat(1002);
            m.color = new Color(c.r, c.g, c.b, 0f);
            candyMats.Add(m);
            candyBase.Add(c);
        }
        for (var i = 0; i < 16; i++)
        {
            var el = Random.Range(8f, 38f) * Mathf.Deg2Rad;
            var az = i / 16f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            var dir = new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az));
            var cloud = new GameObject("Cloud").transform;
            cloud.SetParent(candyRoot, false);
            cloud.localPosition = dir * 260f;
            cloud.localRotation = Quaternion.LookRotation(-dir);
            var mat = candyMats[i % candyMats.Count];
            var size = Random.Range(22f, 38f);
            for (var p = 0; p < 5; p++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(puff.GetComponent<Collider>());
                puff.transform.SetParent(cloud, false);
                var k = p - 2;
                puff.transform.localPosition = new Vector3(k * size * 0.5f, Mathf.Cos(k * 0.7f) * size * 0.18f, 0);
                puff.transform.localScale = new Vector3(size * (1f - Mathf.Abs(k) * 0.14f), size * 0.7f * (1f - Mathf.Abs(k) * 0.12f), size * 0.35f);
                puff.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
    }

    private void BuildAurora()
    {
        Color[] bands = { new Color(0.3f, 1f, 0.6f), new Color(0.4f, 0.8f, 1f), new Color(0.8f, 0.5f, 1f) };
        for (var b = 0; b < bands.Length; b++)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            const int n = 28;
            var baseEl = (14f + b * 7f) * Mathf.Deg2Rad;
            for (var i = 0; i <= n; i++)
            {
                var az = Mathf.Lerp(-1.1f, 1.1f, i / (float)n);
                var wave = Mathf.Sin(i * 0.5f + b * 2f) * 0.05f;
                var lo = new Vector3(Mathf.Sin(az), 0, Mathf.Cos(az));
                var elLow = baseEl + wave;
                var elHigh = elLow + 0.3f + Mathf.Sin(i * 0.9f + b) * 0.07f;
                verts.Add(new Vector3(lo.x * Mathf.Cos(elLow), Mathf.Sin(elLow), lo.z * Mathf.Cos(elLow)) * 270f);
                verts.Add(new Vector3(lo.x * Mathf.Cos(elHigh), Mathf.Sin(elHigh), lo.z * Mathf.Cos(elHigh)) * 270f);
                var edge = Mathf.Sin(i / (float)n * Mathf.PI);
                cols.Add(new Color(bands[b].r, bands[b].g, bands[b].b, 0.75f * edge));
                cols.Add(new Color(bands[(b + 1) % 3].r, bands[(b + 1) % 3].g, bands[(b + 1) % 3].b, 0f));
                if (i < n)
                {
                    var v = i * 2;
                    tris.AddRange(new[] { v, v + 1, v + 2, v + 2, v + 1, v + 3, v, v + 2, v + 1, v + 2, v + 3, v + 1 });
                }
            }
            var mat = SkyMat(1002 + b);
            mat.color = new Color(1f, 1f, 1f, 0f);
            auroraMats.Add(mat);
            // Rotate each band a little so they fan out around the sky.
            var piece = SkyPiece("Aurora" + b, MakeMesh(verts, cols, tris), mat);
            piece.localRotation = Quaternion.Euler(0, b * 70f, 0);
            auroraBands.Add(piece);
        }
    }

    private void UpdateTheme(float dt)
    {
        if (sunLight == null)
        {
            foreach (var l in FindObjectsByType<Light>())
            {
                if (l.type == LightType.Directional) { sunLight = l; break; }
            }
        }

        if (blend < 1f)
        {
            blend = Mathf.Min(1f, blend + dt / ThemeBlendSeconds);
            current = Theme.Lerp(from, to, Mathf.SmoothStep(0f, 1f, blend));
            ApplyTheme(current);
        }
        AnimateThemeExtras(Time.time);
    }

    private void ApplyTheme(Theme t)
    {
        var colors = new Color[8 * 32];
        Color[] rows = { t.Horizon, t.Horizon, Color.Lerp(t.Horizon, t.Low, 0.8f), t.Low, Color.Lerp(t.Low, t.Mid, 0.5f), t.Mid, Color.Lerp(t.Mid, t.Top, 0.6f), t.Top };
        for (var r = 0; r < 8; r++)
        {
            for (var s = 0; s < 32; s++) colors[r * 32 + s] = rows[r];
        }
        domeMesh.colors = colors;

        RenderSettings.fogColor = t.Horizon;
        cam.backgroundColor = t.Horizon;
        RenderSettings.ambientSkyColor = t.AmbSky;
        RenderSettings.ambientEquatorColor = t.AmbEquator;
        RenderSettings.ambientGroundColor = t.AmbGround;
        if (sunLight != null)
        {
            sunLight.color = t.Light;
            sunLight.intensity = t.LightIntensity;
            sunLight.transform.rotation = Quaternion.Euler(t.LightPitch, -30f, 0f);
        }

        sunPivot.localRotation = Quaternion.Euler(t.SunPitch, 0, 0);
        for (var i = 0; i < sunMats.Count; i++)
        {
            var b = sunBase[i];
            var isDisc = b.a > 0.9f;
            var tint = isDisc ? Color.Lerp(b, t.SunTint, 0.8f) : new Color(b.r * t.SunTint.r, b.g * t.SunTint.g, b.b * t.SunTint.b);
            tint.a = isDisc ? 1f : Mathf.Clamp01(b.a * t.Glow);
            sunMats[i].color = tint;
        }
        rainbowMat.color = new Color(1f, 1f, 1f, t.Rainbow);
        for (var i = 0; i < candyMats.Count; i++)
        {
            var c = candyBase[i];
            candyMats[i].color = new Color(c.r, c.g, c.b, c.a * t.Candy);
        }
        candyRoot.gameObject.SetActive(t.Candy > 0.01f);
    }

    private void AnimateThemeExtras(float time)
    {
        var t = current;
        var starsOn = t.Stars > 0.01f;
        if (starsOn)
        {
            starMat.color = new Color(1f, 1f, 0.95f, t.Stars);
            for (var i = 0; i < starBalls.Count; i++)
            {
                var k = 0.75f + 0.25f * Mathf.Sin(time * 2f + i * 1.3f);
                starBalls[i].gameObject.SetActive(true);
                starBalls[i].localScale = Vector3.one * (1.5f * k);
            }
        }
        else if (starBalls.Count > 0 && starBalls[0].gameObject.activeSelf)
        {
            foreach (var s in starBalls) s.gameObject.SetActive(false);
        }

        for (var i = 0; i < auroraMats.Count; i++)
        {
            var pulse = 0.65f + 0.35f * Mathf.Sin(time * 0.7f + i * 1.7f);
            auroraMats[i].color = new Color(1f, 1f, 1f, t.Aurora * pulse);
            auroraBands[i].localRotation = Quaternion.Euler(0, i * 70f + Mathf.Sin(time * 0.15f + i) * 8f, 0);
            auroraBands[i].gameObject.SetActive(t.Aurora > 0.01f);
        }
    }
}
