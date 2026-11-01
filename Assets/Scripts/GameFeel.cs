using UnityEngine;

// Game feel: camera shake, brief hit-freeze, speed lines and glowing hoofprints.
public sealed class GameFeel : MonoBehaviour
{
    private const int MaxPrints = 36;

    private static GameFeel instance;

    private Camera cam;
    private Unicorn unicorn;
    private float shake;
    private Vector3 lastOffset;
    private float freezeLeft;
    private float speedLines;
    private Vector3 lastPos;
    private bool hasLast;
    private float printDist;
    private int printIndex, hueIndex;
    private readonly Transform[] prints = new Transform[MaxPrints];
    private readonly float[] printAge = new float[MaxPrints];
    private readonly Material[] printMats = new Material[6];

    public static void Setup(Camera camera, Unicorn u)
    {
        if (instance == null) return;
        instance.cam = camera;
        instance.unicorn = u;
        instance.hasLast = false;
        instance.ResetAll();
    }

    public static void Shake(float amount) { if (instance != null) instance.shake = Mathf.Min(1.2f, Mathf.Max(instance.shake, amount)); }

    // Real-time pause of a few frames; makes big hits feel heavy.
    public static void Freeze(float seconds)
    {
        if (instance == null) return;
        instance.freezeLeft = Mathf.Max(instance.freezeLeft, seconds);
        Time.timeScale = 0.05f;
    }

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        for (var i = 0; i < printMats.Length; i++)
        {
            var c = Color.HSVToRGB(i / (float)printMats.Length, 0.4f, 1f);
            var m = new Material(Shader.Find("Standard")) { color = c };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.9f);
            printMats[i] = m;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Time.timeScale = 1f;
    }

    private void ResetAll()
    {
        shake = 0f;
        freezeLeft = 0f;
        Time.timeScale = 1f;
        for (var i = 0; i < MaxPrints; i++) if (prints[i] != null) prints[i].gameObject.SetActive(false);
    }

    // Call before the camera follows so last frame's shake doesn't feed back into it.
    public void BeginCamera()
    {
        if (cam != null) cam.transform.position -= lastOffset;
    }

    // Call after the camera follows to add this frame's shake.
    public void EndCamera()
    {
        if (cam == null) return;
        var dt = Time.unscaledDeltaTime;
        shake = Mathf.Max(0f, shake - dt * 2.2f);
        var s = shake * shake;
        lastOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * (0.35f * s);
        cam.transform.position += lastOffset;
        cam.transform.rotation *= Quaternion.Euler(0, 0, Random.Range(-1f, 1f) * 2.5f * s);

        if (freezeLeft > 0f)
        {
            freezeLeft -= dt;
            if (freezeLeft <= 0f) Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (unicorn == null) return;
        var dt = Time.deltaTime;
        var pos = unicorn.transform.position;
        var speed = hasLast && dt > 0f ? (pos - lastPos).magnitude / dt : 0f;
        speedLines = Mathf.MoveTowards(speedLines, unicorn.Dashing ? 1f : unicorn.WingBoost ? 0.45f : 0f, Time.unscaledDeltaTime * 5f);

        if (hasLast && unicorn.Grounded && speed > 2f)
        {
            printDist += speed * dt;
            if (printDist > 1.3f)
            {
                printDist = 0f;
                DropPrint(pos);
            }
        }
        lastPos = pos;
        hasLast = true;

        for (var i = 0; i < MaxPrints; i++)
        {
            if (prints[i] == null || !prints[i].gameObject.activeSelf) continue;
            printAge[i] += dt;
            var life = printAge[i] / 2.5f;
            if (life >= 1f) { prints[i].gameObject.SetActive(false); continue; }
            var k = 1f - life * life;
            prints[i].localScale = new Vector3(0.5f, 0.04f, 0.4f) * k;
        }
    }

    private void DropPrint(Vector3 pos)
    {
        var slot = printIndex++ % MaxPrints;
        if (prints[slot] == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Hoofprint";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            prints[slot] = go.transform;
        }
        var side = (printIndex % 2 == 0 ? 1f : -1f) * 0.35f;
        var p = prints[slot];
        p.position = pos + unicorn.transform.right * side + Vector3.up * 0.04f;
        p.rotation = Quaternion.Euler(0, unicorn.transform.eulerAngles.y, 0);
        p.GetComponent<Renderer>().sharedMaterial = printMats[hueIndex++ % printMats.Length];
        printAge[slot] = 0f;
        p.gameObject.SetActive(true);
    }

    private void OnGUI()
    {
        if (speedLines < 0.02f || Event.current.type != EventType.Repaint) return;
        var cx = Screen.width * 0.5f;
        var cy = Screen.height * 0.5f;
        var reach = Mathf.Sqrt(cx * cx + cy * cy);
        var old = GUI.color;
        var matrix = GUI.matrix;
        var t = Time.unscaledTime;
        for (var i = 0; i < 26; i++)
        {
            var ang = i / 26f * 360f + Mathf.Sin(i * 12.9f) * 6f;
            var flick = 0.5f + 0.5f * Mathf.Sin(t * 22f + i * 2.1f);
            var len = reach * (0.16f + 0.14f * flick) * speedLines;
            var from = reach * (0.62f + 0.1f * Mathf.Sin(i * 3.3f));
            GUI.color = new Color(1f, 1f, 1f, 0.28f * speedLines * (0.4f + flick * 0.6f));
            GUIUtility.RotateAroundPivot(ang, new Vector2(cx, cy));
            GUI.DrawTexture(new Rect(cx + from, cy - 1.5f, len, 3f), Texture2D.whiteTexture);
            GUI.matrix = matrix;
        }
        GUI.color = old;
    }
}
