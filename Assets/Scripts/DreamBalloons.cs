using System.Collections.Generic;
using UnityEngine;

// Dream balloons float around the whole meadow, waiting to be found and popped. They never escape.
public sealed class DreamBalloons : MonoBehaviour
{
    private const int Target = 22;
    private static Unicorn unicorn;
    private readonly List<Balloon> mine = new List<Balloon>();
    private float refill;

    public static void Setup(Unicorn u) => unicorn = u;

    private void Update()
    {
        if (unicorn == null) return;
        mine.RemoveAll(b => b == null);
        if (mine.Count >= Target) return;
        refill -= Time.deltaTime;
        if (refill > 0f) return;
        refill = mine.Count < Target / 2 ? 0.1f : 1.5f;
        Spawn();
    }

    private void Spawn()
    {
        var p = new Vector3(Random.Range(-40f, 40f), Random.Range(3f, 26f), Random.Range(-20f, 48f));
        // Don't pop one into existence on top of the unicorn.
        if ((p - unicorn.transform.position).sqrMagnitude < 100f) return;
        var r = Random.value;
        var kind = r < 0.08f ? BalloonKind.Golden : r < 0.14f ? BalloonKind.Heart : r < 0.2f ? BalloonKind.Star : r < 0.25f ? BalloonKind.Rainbow : BalloonKind.Normal;
        var color = kind == BalloonKind.Normal ? Balloon.RandomColor() : Balloon.KindColor(kind);
        mine.Add(Balloon.SpawnAmbient(transform, p, kind, color));
    }
}
