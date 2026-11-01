using UnityEngine;

// Extra scenery that gives each realm its own look.
public sealed partial class UnicornPopGame
{
    private void BuildRealmScenery(Transform world)
    {
        if (realm.Scenery == SceneryKind.Mushrooms) BuildMushrooms(world);
        else if (realm.Scenery == SceneryKind.Crystals) BuildCrystals(world);
        else if (realm.Scenery == SceneryKind.Spires) BuildSpires(world);
        else if (realm.Scenery == SceneryKind.Candy) BuildCandy(world);
        else if (realm.Scenery == SceneryKind.Void) BuildVoid(world);
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

    private void BuildSpires(Transform world)
    {
        var rock = new[] { new Color(0.78f, 0.52f, 0.4f), new Color(0.66f, 0.42f, 0.38f), new Color(0.88f, 0.62f, 0.45f) };
        for (var i = 0; i < 90; i++)
        {
            if (!ScenerySpot(out var x, out var z)) continue;
            var spire = new GameObject("Spire").transform;
            spire.SetParent(world, false);
            spire.position = new Vector3(x, 0, z);
            var h = Random.Range(5f, 16f);
            var w = Random.Range(2f, 4.5f);
            var c = rock[Random.Range(0, rock.Length)];
            Prim.Make(PrimitiveType.Cylinder, "Pillar", spire, new Vector3(0, h * 0.5f, 0), new Vector3(w, h * 0.5f, w), c);
            Prim.Make(PrimitiveType.Cylinder, "Cap", spire, new Vector3(0, h, 0), new Vector3(w * 1.35f, 0.5f, w * 1.35f), Color.Lerp(c, Color.white, 0.25f));
            if (i % 4 == 0) Prim.Cone("Bolt", spire, new Vector3(0, h + 2.2f, 0), new Vector3(0.7f, 3.5f, 0.7f), new Color(1f, 0.95f, 0.5f), new Vector3(180f, 0f, 0f));
        }
    }

    private void BuildCandy(Transform world)
    {
        var cols = new[] { new Color(1f, 0.45f, 0.65f), new Color(0.5f, 0.85f, 1f), new Color(1f, 0.85f, 0.4f), new Color(0.7f, 0.55f, 1f), new Color(0.55f, 0.95f, 0.65f) };
        for (var i = 0; i < 120; i++)
        {
            if (!ScenerySpot(out var x, out var z)) continue;
            var c = cols[Random.Range(0, cols.Length)];
            var t = new GameObject("Candy").transform;
            t.SetParent(world, false);
            t.position = new Vector3(x, 0, z);
            if (i % 3 == 0)
            {
                var h = Random.Range(3f, 6f);
                Prim.Make(PrimitiveType.Cylinder, "Stick", t, new Vector3(0, h * 0.5f, 0), new Vector3(0.25f, h * 0.5f, 0.25f), Color.white);
                Prim.Make(PrimitiveType.Sphere, "Lolly", t, new Vector3(0, h + 0.8f, 0), Vector3.one * Random.Range(1.8f, 2.8f), c);
                Prim.Make(PrimitiveType.Sphere, "Swirl", t, new Vector3(0, h + 0.8f, -0.1f), new Vector3(1.1f, 1.1f, 1.6f), Color.Lerp(c, Color.white, 0.6f));
            }
            else if (i % 3 == 1)
            {
                var h = Random.Range(2f, 4.5f);
                for (var s = 0; s < 4; s++)
                    Prim.Make(PrimitiveType.Cylinder, "Stripe", t, new Vector3(0, h * (s + 0.5f) / 4f, 0), new Vector3(0.5f, h / 8f, 0.5f), s % 2 == 0 ? Color.white : c);
                Prim.Make(PrimitiveType.Sphere, "Hook", t, new Vector3(0.35f, h + 0.1f, 0), new Vector3(0.9f, 0.5f, 0.5f), c);
            }
            else
            {
                var s = Random.Range(1.5f, 3f);
                Prim.Make(PrimitiveType.Sphere, "Gumdrop", t, new Vector3(0, s * 0.35f, 0), new Vector3(s, s * 0.75f, s), c);
                Prim.Make(PrimitiveType.Sphere, "Sugar", t, new Vector3(0, s * 0.55f, 0), new Vector3(s * 0.75f, s * 0.4f, s * 0.75f), Color.Lerp(c, Color.white, 0.5f));
            }
        }
    }

    private void BuildVoid(Transform world)
    {
        var shard = new Color(0.45f, 0.3f, 0.7f);
        for (var i = 0; i < 80; i++)
        {
            if (!ScenerySpot(out var x, out var z)) continue;
            var h = Random.Range(3f, 12f);
            var tilt = new Vector3(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
            Prim.Cone("Shard", world, new Vector3(x, h * 0.5f, z), new Vector3(Random.Range(0.8f, 2f), h, Random.Range(0.8f, 2f)), Color.Lerp(shard, Color.black, Random.Range(0f, 0.5f)), tilt);
            if (i % 5 == 0) Prim.Make(PrimitiveType.Sphere, "Wisp", world, new Vector3(x, h + 1.5f, z), Vector3.one * 0.6f, new Color(0.8f, 0.7f, 1f));
        }
    }
}
