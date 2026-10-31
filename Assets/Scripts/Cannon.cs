using System.Collections;
using UnityEngine;

// Batch 3: the jester fetches a balloon from the crate, stuffs it into the cannon and fires it.
public sealed class Cannon : MonoBehaviour
{
    public const float CrateX = 4.3f;
    private const float LoadX = 2.9f;
    private const float SafeX = 2.3f;
    private const float WalkSpeed = 2.6f;
    private const float BaseAngle = 50f;

    // Seconds between the cannon firing and the jester heading back to the crate.
    public float Interval = 1.2f;
    public bool Active = true;

    private Transform jester;
    private Transform barrel;
    private Transform muzzle;
    private Transform world;
    private Transform held;
    private float shake;
    private float jesterX;

    public void Build(Transform worldRoot, Transform jesterRoot, Transform barrelPivot, Transform muzzleTip)
    {
        world = worldRoot;
        jester = jesterRoot;
        barrel = barrelPivot;
        muzzle = muzzleTip;
        jesterX = jester.position.x;
        StartCoroutine(Loop());
    }

    private void Update()
    {
        var wobble = Mathf.Sin(Time.time * 1.5f) * 1.5f + Mathf.Sin(Time.time * 70f) * shake;
        barrel.localRotation = Quaternion.Euler(0, 0, BaseAngle + wobble);
    }

    private void SetJester(float hop, float tilt = 0f)
    {
        jester.position = new Vector3(jesterX, UnicornPopGame.WalkwayY + hop, 0);
        jester.rotation = Quaternion.Euler(0, 0, tilt);
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            while (!Active) yield return Idle(0.1f, 3f, 0.1f);

            yield return Walk(CrateX);
            var color = Balloon.RandomColor();
            held = Balloon.BuildVisual("Held", jester, color);
            held.localPosition = new Vector3(0, 2f, 0);
            held.localScale = Vector3.one * 0.45f;
            yield return Idle(0.3f, 8f, 0.1f);

            yield return Walk(LoadX);
            yield return Load();

            yield return Walk(SafeX);
            // Fuse: the jester panics and the barrel rattles.
            shake = 2.5f;
            yield return Idle(0.6f, 16f, 0.12f);
            shake = 0f;

            Fire(color);
            yield return Idle(Interval, 5f, 0.1f);
        }
    }

    private IEnumerator Idle(float seconds, float hopSpeed, float hopHeight)
    {
        for (var t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetJester(Mathf.Abs(Mathf.Sin(Time.time * hopSpeed)) * hopHeight);
            yield return null;
        }
    }

    private IEnumerator Walk(float target)
    {
        while (Mathf.Abs(jesterX - target) > 0.03f)
        {
            jesterX = Mathf.MoveTowards(jesterX, target, WalkSpeed * Time.deltaTime);
            var phase = Time.time * 14f;
            SetJester(Mathf.Abs(Mathf.Sin(phase)) * 0.08f, Mathf.Sin(phase) * 6f);
            yield return null;
        }
    }

    private IEnumerator Load()
    {
        // Hop while the balloon is posted into the muzzle.
        held.SetParent(world, true);
        var from = held.position;
        var startScale = held.localScale;
        const float duration = 0.55f;
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var u = t / duration;
            SetJester(Mathf.Sin(u * Mathf.PI) * 0.5f);
            var arc = Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.6f;
            held.position = Vector3.Lerp(from, muzzle.position, u * u) + arc;
            held.localScale = startScale * Mathf.Lerp(1f, 0.3f, u);
            yield return null;
        }
        Destroy(held.gameObject);
        held = null;
        StartCoroutine(Recoil(0.9f, 0.2f));
    }

    private void Fire(Color color)
    {
        var velocity = new Vector2(Random.Range(-10f, -5f), Random.Range(3.8f, 5.2f));
        Balloon.Spawn(world, muzzle.position, velocity, color);
        StartCoroutine(Recoil(0.7f, 0.25f));
        StartCoroutine(Smoke());
    }

    private IEnumerator Recoil(float minScale, float seconds)
    {
        for (var t = 0f; t < seconds; t += Time.deltaTime)
        {
            var s = Mathf.Lerp(minScale, 1f, t / seconds);
            barrel.localScale = new Vector3(1f + (1f - s) * 0.4f, s, 1f);
            yield return null;
        }
        barrel.localScale = Vector3.one;
    }

    private IEnumerator Smoke()
    {
        const int count = 7;
        var puffs = new Transform[count];
        var dirs = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var c = Random.Range(0.85f, 1f);
            puffs[i] = Art.Spr("Puff", world, Art.Circle, muzzle.position, Vector2.one * 0.3f, new Color(c, c, c, 0.9f), 30).transform;
            dirs[i] = Random.insideUnitCircle * 1.5f + Vector2.up * 0.8f;
        }
        const float life = 0.6f;
        for (var t = 0f; t < life; t += Time.deltaTime)
        {
            var u = t / life;
            for (var i = 0; i < count; i++)
            {
                puffs[i].position += (Vector3)(dirs[i] * Time.deltaTime);
                puffs[i].localScale = Vector3.one * Mathf.Lerp(0.3f, 0.7f, u);
                var r = puffs[i].GetComponent<SpriteRenderer>();
                var col = r.color;
                col.a = 0.9f * (1f - u);
                r.color = col;
            }
            yield return null;
        }
        foreach (var p in puffs) Destroy(p.gameObject);
    }
}
