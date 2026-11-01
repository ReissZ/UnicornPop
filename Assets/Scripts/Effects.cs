using System.Collections.Generic;
using UnityEngine;

// Pop bursts and floating score text.
public sealed class Effects : MonoBehaviour
{
    private sealed class Spark
    {
        public Transform T;
        public Vector3 Vel;
        public float Life;
        public float MaxLife;
        public float Size;
    }

    private sealed class FloatText
    {
        public Vector3 Pos;
        public string Text;
        public float Life;
        public Color Color;
        public int Size;
    }

    public static Effects Instance { get; private set; }

    private readonly List<Spark> sparks = new List<Spark>();
    private readonly List<FloatText> texts = new List<FloatText>();
    private GUIStyle style;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Burst(Vector3 pos, Color color, int count, float speed, float life = 0.8f)
    {
        for (var i = 0; i < count; i++)
        {
            var size = Random.Range(0.15f, 0.35f);
            var t = Prim.Make(PrimitiveType.Sphere, "Spark", null, pos, Vector3.one * size, Color.Lerp(color, Color.white, Random.Range(0f, 0.4f)));
            var l = life * Random.Range(0.6f, 1f);
            sparks.Add(new Spark { T = t, Vel = Random.onUnitSphere * speed * Random.Range(0.4f, 1f), Life = l, MaxLife = l, Size = size });
        }
    }

    public void Text(Vector3 pos, string text, Color color, int size = 26)
    {
        texts.Add(new FloatText { Pos = pos, Text = text, Life = 1.1f, Color = color, Size = size });
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        for (var i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.Life -= dt;
            if (s.Life <= 0f)
            {
                Destroy(s.T.gameObject);
                sparks.RemoveAt(i);
                continue;
            }
            s.Vel.y -= 4f * dt;
            s.T.position += s.Vel * dt;
            s.T.localScale = Vector3.one * (s.Size * s.Life / s.MaxLife);
        }
        for (var i = texts.Count - 1; i >= 0; i--)
        {
            texts[i].Life -= dt;
            texts[i].Pos += Vector3.up * (2.2f * dt);
            if (texts[i].Life <= 0f) texts.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
        var scale = Screen.height / 720f;
        foreach (var t in texts)
        {
            var sp = cam.WorldToScreenPoint(t.Pos);
            if (sp.z <= 0f) continue;
            style.fontSize = Mathf.RoundToInt(t.Size * scale);
            var rect = new Rect(sp.x - 150, Screen.height - sp.y - 25, 300, 50);
            var a = Mathf.Clamp01(t.Life / 0.4f);
            GUI.color = new Color(0, 0, 0, 0.6f * a);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), t.Text, style);
            GUI.color = new Color(t.Color.r, t.Color.g, t.Color.b, a);
            GUI.Label(rect, t.Text, style);
        }
        GUI.color = Color.white;
    }
}
