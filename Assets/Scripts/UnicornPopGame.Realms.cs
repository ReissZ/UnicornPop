using UnityEngine;

// Extra scenery that gives each realm its own look.
public sealed partial class UnicornPopGame
{
    private void BuildRealmScenery(Transform world)
    {
        if (realm.Scenery == SceneryKind.Mushrooms) BuildMushrooms(world);
        else if (realm.Scenery == SceneryKind.Crystals) BuildCrystals(world);
    }

    private static bool ScenerySpot(out float x, out float z)
    {
        for (var tries = 0; tries < 12; tries++)
        {
            x = Random.Range(-88f, 88f);
            z = Random.Range(-170f, 50f);
            if (Mathf.Abs(x) < 6f || Dreamland.InPond(x, z)) continue;
            return true;
        }
        x = z = 0f;
        return false;
    }

    private void BuildMushrooms(Transform world)
    {
        var caps = new[] { new Color(0.95f, 0.35f, 0.4f), new Color(0.7f, 0.45f, 0.95f), new Color(0.98f, 0.65f, 0.25f), new Color(0.4f, 0.8f, 0.95f) };
        var stem = new Color(0.97f, 0.93f, 0.85f);
        for (var i = 0; i < 110; i++)
        {
            if (!ScenerySpot(out var x, out var z)) continue;
            var s = i % 9 == 0 ? Random.Range(2.6f, 3.6f) : Random.Range(0.8f, 1.8f);
            var m = new GameObject("Mushroom").transform;
            m.SetParent(world, false);
            m.position = new Vector3(x, 0, z);
            m.localScale = Vector3.one * s;
            var cap = caps[Random.Range(0, caps.Length)];
            Prim.Make(PrimitiveType.Cylinder, "Stem", m, new Vector3(0, 0.5f, 0), new Vector3(0.45f, 0.5f, 0.45f), stem);
            Prim.Make(PrimitiveType.Sphere, "Cap", m, new Vector3(0, 1.05f, 0), new Vector3(1.6f, 0.9f, 1.6f), cap);
            for (var d = 0; d < 5; d++)
            {
                var a = d * 72f * Mathf.Deg2Rad;
                Prim.Make(PrimitiveType.Sphere, "Spot", m, new Vector3(Mathf.Sin(a) * 0.45f, 1.28f, Mathf.Cos(a) * 0.45f), new Vector3(0.22f, 0.12f, 0.22f), Color.white);
            }
        }
    }

    private void BuildCrystals(Transform world)
    {
        var tints = new[] { new Color(0.6f, 0.9f, 1f), new Color(0.85f, 0.7f, 1f), new Color(1f, 0.75f, 0.9f), new Color(0.7f, 1f, 0.9f) };
        for (var i = 0; i < 130; i++)
        {
            if (!ScenerySpot(out var x, out var z)) continue;
            var cluster = new GameObject("Crystals").transform;
            cluster.SetParent(world, false);
            cluster.position = new Vector3(x, 0, z);
            var tint = tints[Random.Range(0, tints.Length)];
            var big = i % 6 == 0 ? 2.2f : 1f;
            var n = Random.Range(3, 6);
            for (var c = 0; c < n; c++)
            {
                var h = Random.Range(1.6f, 4.2f) * big;
                var w = Random.Range(0.5f, 1.1f) * big;
                var lean = new Vector3(Random.Range(-18f, 18f), Random.Range(0f, 360f), Random.Range(-18f, 18f));
                Prim.Cone("Crystal", cluster, new Vector3(Random.Range(-1.2f, 1.2f), h * 0.45f, Random.Range(-1.2f, 1.2f)), new Vector3(w, h, w), Color.Lerp(tint, Color.white, Random.Range(0f, 0.4f)), lean);
            }
        }
    }
}
