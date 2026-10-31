using UnityEngine;

// Procedural sprites so the game needs no imported assets.
public static class Art
{
    public static Sprite Circle { get; private set; }
    public static Sprite Square { get; private set; }
    public static Sprite Triangle { get; private set; }

    public static void Init()
    {
        if (Circle != null) return;
        Circle = Make(64, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 32f), 64f);
        Square = Make(4, (x, y) => 1f, 4f);
        // Apex at the top, base at the bottom.
        Triangle = Make(64, (x, y) => Mathf.Clamp01(((1f - y) * 0.5f - Mathf.Abs(x)) * 16f), 64f);
    }

    private static Sprite Make(int size, System.Func<float, float, float> alpha, float ppu)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (var py = 0; py < size; py++)
        {
            for (var px = 0; px < size; px++)
            {
                var x = (px + 0.5f) / size * 2f - 1f;
                var y = (py + 0.5f) / size * 2f - 1f;
                tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha(x, y)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
    }

    // Creates a tinted sprite; scale is the size in world units.
    public static SpriteRenderer Spr(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, float rot = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, 0, rot);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite;
        r.color = color;
        r.sortingOrder = order;
        return r;
    }
}
