using System.Collections;
using UnityEngine;

// The jester fetches a balloon from the crate, stuffs it into the cannon, backs off and fires it toward the player.
public sealed class Cannon : MonoBehaviour
{
    private const float WalkSpeed = 4.5f;
    private const float Drag = 0.35f;

    // Seconds between a shot and the jester heading back to the crate.
    public float Interval = 1.0f;
    public bool Active = true;

    private Transform world;
    private Transform jester;
    private Transform barrel;
    private Transform muzzle;
    private Transform player;
    private Transform held;
    private float crateX;
    private float loadX;
    private float safeX;
    private float baseY;
    private float z;
    private float x;
    private float shake;
    private Vector3 faceDir;
    private Quaternion baseRotation;

    public void Build(Transform worldRoot, Transform jesterRoot, Transform barrelPivot, Transform muzzleTip, Transform playerTransform, float crate, float load, float safe)
    {
        world = worldRoot;
        jester = jesterRoot;
        barrel = barrelPivot;
        muzzle = muzzleTip;
        player = playerTransform;
        crateX = crate;
        loadX = load;
        safeX = safe;
        x = jester.position.x;
        baseY = jester.position.y;
        z = jester.position.z;
        baseRotation = barrel.localRotation;
        faceDir = Vector3.back;
        StartCoroutine(Loop());
    }

    private void Update()
    {
        // Idle sway plus a rattle while the fuse burns.
        var rattle = Mathf.Sin(Time.time * 70f) * shake;
        barrel.localRotation = baseRotation * Quaternion.Euler(rattle, 0, rattle * 0.5f);
        if (faceDir.sqrMagnitude > 0.001f)
        {
            jester.rotation = Quaternion.Slerp(jester.rotation, Quaternion.LookRotation(faceDir), 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }

    private void Place(float hop)
    {
        jester.position = new Vector3(x, baseY + hop, z);
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            while (!Active) yield return null;

            yield return Walk(crateX);
            var golden = Random.value < 0.12f;
            var color = golden ? Balloon.Gold : Balloon.RandomColor();
            held = Balloon.BuildVisual("Held", jester, color);
            held.localPosition = new Vector3(0, 4.2f, 0);
            held.localScale = Vector3.one * 0.7f;
            yield return Idle(0.35f, 9f, 0.25f, Vector3.back);

            yield return Walk(loadX);
            yield return Load();

            yield return Walk(safeX);
            // Fuse: the jester jitters in panic while the barrel rattles.
            shake = 2.5f;
            yield return Idle(0.7f, 18f, 0.35f, Vector3.back);
            shake = 0f;

            Fire(color, golden);
            yield return Idle(Interval, 6f, 0.3f, Vector3.back);
        }
    }

    private IEnumerator Idle(float seconds, float hopSpeed, float hopHeight, Vector3 face)
    {
        faceDir = face;
        for (var t = 0f; t < seconds; t += Time.deltaTime)
        {
            Place(Mathf.Abs(Mathf.Sin(Time.time * hopSpeed)) * hopHeight);
            yield return null;
        }
    }

    private IEnumerator Walk(float target)
    {
        faceDir = new Vector3(Mathf.Sign(target - x), 0, 0);
        while (Mathf.Abs(x - target) > 0.05f)
        {
            x = Mathf.MoveTowards(x, target, WalkSpeed * Time.deltaTime);
            Place(Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.2f);
            yield return null;
        }
    }

    private IEnumerator Load()
    {
        // Face the cannon and hop while the balloon is posted into the muzzle.
        faceDir = new Vector3(-1f, 0, 0);
        held.SetParent(world, true);
        var from = held.position;
        var startScale = held.localScale;
        const float duration = 0.6f;
        for (var t = 0f; t < duration; t += Time.deltaTime)
        {
            var u = t / duration;
            Place(Mathf.Sin(u * Mathf.PI) * 0.8f);
            var arc = Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.5f;
            held.position = Vector3.Lerp(from, muzzle.position, u * u) + arc;
            held.localScale = startScale * Mathf.Lerp(1f, 0.25f, u);
            yield return null;
        }
        Destroy(held.gameObject);
        held = null;
        StartCoroutine(Recoil(0.9f, 0.2f));
    }

    private void Fire(Color color, bool golden)
    {
        var origin = muzzle.position;
        var target = player.position + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-4f, 6f));
        target.y = Random.Range(4f, 18f);
        // Air drag stops the throw about speed / drag away, so size the launch to reach the target.
        var flat = new Vector3(target.x - origin.x, 0, target.z - origin.z);
        var drift = 1.1f;
        var vel = flat * Drag;
        vel.y = (target.y - origin.y) * Drag + drift;
        Balloon.Spawn(world, origin, vel, color, golden);
        StartCoroutine(Recoil(0.65f, 0.25f));
        StartCoroutine(Smoke());
        Sfx.Boom();
    }

    private IEnumerator Recoil(float minScale, float seconds)
    {
        for (var t = 0f; t < seconds; t += Time.deltaTime)
        {
            var s = Mathf.Lerp(minScale, 1f, t / seconds);
            barrel.localScale = new Vector3(1f + (1f - s) * 0.4f, s, 1f + (1f - s) * 0.4f);
            yield return null;
        }
        barrel.localScale = Vector3.one;
    }

    private IEnumerator Smoke()
    {
        const int count = 9;
        var puffs = new Transform[count];
        var vels = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var c = Random.Range(0.85f, 1f);
            puffs[i] = Prim.Make(PrimitiveType.Sphere, "Smoke", null, muzzle.position, Vector3.one * 0.6f, new Color(c, c, c));
            vels[i] = Random.insideUnitSphere * 2.5f + Vector3.up * 1.2f;
        }
        const float life = 0.7f;
        for (var t = 0f; t < life; t += Time.deltaTime)
        {
            var u = t / life;
            for (var i = 0; i < count; i++)
            {
                puffs[i].position += vels[i] * Time.deltaTime;
                puffs[i].localScale = Vector3.one * Mathf.Lerp(0.6f, 1.8f, u) * (1f - u * 0.5f);
            }
            yield return null;
        }
        foreach (var p in puffs) Destroy(p.gameObject);
    }
}
