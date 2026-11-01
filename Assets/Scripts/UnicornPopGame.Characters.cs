using UnityEngine;

// Extra costume and face detail for the jester and the king, built from primitives.
public sealed partial class UnicornPopGame
{
    private static readonly Color Pink = new Color(1f, 0.5f, 0.75f);
    private static readonly Color Teal = new Color(0.3f, 0.85f, 0.8f);

    private void DetailJester(Color skin, Color purple, Color gold)
    {
        var white = new Color(0.98f, 0.97f, 1f);
        var hair = new Color(1f, 0.5f, 0.2f);
        Color[] accents = { gold, Pink, Teal };

        // Harlequin diamonds across the tunic, following the curve of the body.
        for (var row = 0; row < 3; row++)
        {
            var y = 1.0f + row * 0.3f;
            var xs = row % 2 == 0 ? new[] { -0.24f, 0f, 0.24f } : new[] { -0.12f, 0.12f };
            for (var i = 0; i < xs.Length; i++)
            {
                var x = xs[i];
                var z = 0.35f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (x / 0.45f) * (x / 0.45f))) + 0.005f;
                Prim.Make(PrimitiveType.Cube, "Diamond", jester, new Vector3(x, y, z), new Vector3(0.17f, 0.17f, 0.04f), accents[(row + i) % 3], new Vector3(0, 0, 45f));
            }
        }
        foreach (var by in new[] { 1.62f, 1.4f })
        {
            Prim.Make(PrimitiveType.Sphere, "Pom", jester, new Vector3(0, by, 0.34f), Vector3.one * 0.1f, white);
        }

        // Ruffled collar of little colour balls.
        for (var i = 0; i < 12; i++)
        {
            var a = i / 12f * Mathf.PI * 2f;
            Prim.Make(PrimitiveType.Sphere, "Ruff", jester, new Vector3(Mathf.Cos(a) * 0.4f, 1.8f, Mathf.Sin(a) * 0.34f), Vector3.one * 0.24f, accents[i % 3]);
        }

        // Sleeves with ruffled cuffs.
        foreach (var sd in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Capsule, "Sleeve", jester, new Vector3(sd * 0.55f, 1.42f, 0.05f), new Vector3(0.22f, 0.3f, 0.22f), sd < 0 ? gold : purple, new Vector3(0, 0, sd * 22f));
            Prim.Make(PrimitiveType.Sphere, "Cuff", jester, new Vector3(sd * 0.63f, 1.27f, 0.08f), new Vector3(0.3f, 0.12f, 0.3f), white);
        }

        // Striped leggings and curly shoes with bells.
        foreach (var sd in new[] { -1f, 1f })
        {
            var stripe = sd < 0 ? gold : purple;
            foreach (var by in new[] { 0.25f, 0.6f })
            {
                Prim.Make(PrimitiveType.Cylinder, "Stripe", jester, new Vector3(sd * 0.2f, by, 0), new Vector3(0.24f, 0.07f, 0.24f), stripe);
            }
            var shoe = sd < 0 ? gold : purple;
            Prim.Make(PrimitiveType.Sphere, "Shoe", jester, new Vector3(sd * 0.2f, 0.09f, 0.14f), new Vector3(0.3f, 0.18f, 0.55f), shoe);
            Prim.Cone("ShoeTip", jester, new Vector3(sd * 0.2f, 0.2f, 0.46f), new Vector3(0.12f, 0.3f, 0.12f), shoe, new Vector3(75f, 0, 0));
            Prim.Make(PrimitiveType.Sphere, "ShoeBell", jester, new Vector3(sd * 0.2f, 0.33f, 0.58f), Vector3.one * 0.1f, sd < 0 ? purple : gold);
        }

        // Head: eye whites, ears, tufts of hair, face paint and a hat band.
        foreach (var sd in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Sphere, "EyeWhite", jester, new Vector3(sd * 0.18f, 2.2f, 0.345f), new Vector3(0.22f, 0.24f, 0.1f), white);
            Prim.Make(PrimitiveType.Sphere, "Ear", jester, new Vector3(sd * 0.42f, 2.08f, 0f), new Vector3(0.1f, 0.2f, 0.14f), skin);
            Prim.Make(PrimitiveType.Sphere, "Hair", jester, new Vector3(sd * 0.4f, 2.3f, -0.05f), Vector3.one * 0.25f, hair);
            Prim.Make(PrimitiveType.Sphere, "Hair", jester, new Vector3(sd * 0.44f, 2.12f, -0.12f), Vector3.one * 0.2f, hair);
        }
        Prim.Make(PrimitiveType.Cube, "FacePaint", jester, new Vector3(-0.15f, 2.08f, 0.395f), new Vector3(0.08f, 0.08f, 0.02f), new Color(0.35f, 0.5f, 1f), new Vector3(0, 0, 45f));
        Prim.Make(PrimitiveType.Cube, "FacePaint", jester, new Vector3(0.15f, 2.08f, 0.395f), new Vector3(0.08f, 0.08f, 0.02f), Pink, new Vector3(0, 0, 45f));
        Prim.Make(PrimitiveType.Cylinder, "HatBand", jester, new Vector3(0, 2.43f, 0), new Vector3(0.66f, 0.04f, 0.66f), gold);
        foreach (var sd in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Sphere, "HatStud", jester, new Vector3(sd * 0.2f, 2.43f, 0.31f), Vector3.one * 0.07f, sd < 0 ? Pink : Teal);
        }
    }

    private void DetailKing(Color skin, Color robe, Color fur, Color gold)
    {
        var black = new Color(0.15f, 0.12f, 0.18f);
        var velvet = new Color(0.55f, 0.08f, 0.16f);
        var red = new Color(0.95f, 0.15f, 0.25f);
        var blue = new Color(0.25f, 0.5f, 1f);
        var green = new Color(0.3f, 0.9f, 0.5f);

        // Royal cape behind the robe.
        Prim.Make(PrimitiveType.Cube, "Cape", king, new Vector3(0, 1.1f, -0.72f), new Vector3(1.5f, 2.1f, 0.1f), velvet, new Vector3(-3f, 0, 0));
        Prim.Make(PrimitiveType.Cube, "CapeTrim", king, new Vector3(0, 0.1f, -0.75f), new Vector3(1.6f, 0.15f, 0.12f), fur);

        // Ermine spots on the hem and collar.
        for (var i = 0; i < 10; i++)
        {
            var a = i / 10f * Mathf.PI * 2f;
            Prim.Make(PrimitiveType.Sphere, "Spot", king, new Vector3(Mathf.Sin(a) * 0.81f, 0.1f, Mathf.Cos(a) * 0.71f), new Vector3(0.09f, 0.13f, 0.09f), black);
        }
        foreach (var x in new[] { -0.4f, -0.2f, 0f, 0.2f, 0.4f })
        {
            var z = 0.55f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (x / 0.65f) * (x / 0.65f)));
            Prim.Make(PrimitiveType.Sphere, "Spot", king, new Vector3(x, 1.93f, z + 0.01f), new Vector3(0.07f, 0.1f, 0.04f), black);
        }

        // Gold trim down the front, belt with a jewelled buckle and a medallion.
        Prim.Make(PrimitiveType.Cube, "Trim", king, new Vector3(0, 1.0f, 0.655f), new Vector3(0.18f, 1.7f, 0.03f), gold);
        Prim.Make(PrimitiveType.Cylinder, "Belt", king, new Vector3(0, 0.95f, 0), new Vector3(1.56f, 0.09f, 1.36f), new Color(0.45f, 0.25f, 0.12f));
        Prim.Make(PrimitiveType.Cube, "Buckle", king, new Vector3(0, 0.95f, 0.69f), new Vector3(0.24f, 0.2f, 0.05f), gold);
        Prim.Make(PrimitiveType.Sphere, "BuckleGem", king, new Vector3(0, 0.95f, 0.72f), new Vector3(0.11f, 0.11f, 0.05f), red);
        Prim.Make(PrimitiveType.Sphere, "Medallion", king, new Vector3(0, 1.62f, 0.66f), new Vector3(0.26f, 0.26f, 0.07f), gold);
        Prim.Make(PrimitiveType.Sphere, "MedallionGem", king, new Vector3(0, 1.62f, 0.7f), new Vector3(0.13f, 0.13f, 0.05f), blue);

        // Little royal shoes under the hem.
        foreach (var sd in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Sphere, "Shoe", king, new Vector3(sd * 0.3f, 0.06f, 0.66f), new Vector3(0.38f, 0.2f, 0.55f), velvet);
        }

        // Face: eye whites, fuller brows' neighbours, a curled moustache, side hair and ears.
        foreach (var sd in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Sphere, "EyeWhite", king, new Vector3(sd * 0.2f, 2.65f, 0.38f), new Vector3(0.2f, 0.19f, 0.1f), Color.white);
            Prim.Make(PrimitiveType.Sphere, "Moustache", king, new Vector3(sd * 0.13f, 2.4f, 0.47f), new Vector3(0.26f, 0.1f, 0.14f), new Color(0.92f, 0.9f, 0.95f), new Vector3(0, 0, -sd * 18f));
            Prim.Make(PrimitiveType.Sphere, "MoustacheTip", king, new Vector3(sd * 0.27f, 2.43f, 0.43f), Vector3.one * 0.09f, new Color(0.92f, 0.9f, 0.95f));
            Prim.Make(PrimitiveType.Sphere, "SideHair", king, new Vector3(sd * 0.46f, 2.5f, -0.02f), new Vector3(0.22f, 0.4f, 0.24f), fur);
            Prim.Make(PrimitiveType.Sphere, "Ear", king, new Vector3(sd * 0.49f, 2.52f, 0.06f), new Vector3(0.08f, 0.2f, 0.14f), skin);
            Prim.Make(PrimitiveType.Sphere, "Cheek", king, new Vector3(sd * 0.3f, 2.5f, 0.38f), new Vector3(0.14f, 0.1f, 0.06f), new Color(1f, 0.7f, 0.68f));
        }

        // Crown: velvet cap, jewels around the band and pearls on the points.
        Prim.Make(PrimitiveType.Sphere, "CrownCap", king, new Vector3(0, 3.12f, 0), new Vector3(0.62f, 0.5f, 0.62f), velvet);
        Color[] gems = { red, blue, green, red, blue, green };
        for (var i = 0; i < 6; i++)
        {
            var a = i / 6f * Mathf.PI * 2f + 0.5f;
            Prim.Make(PrimitiveType.Sphere, "CrownGem", king, new Vector3(Mathf.Cos(a) * 0.46f, 3.05f, Mathf.Sin(a) * 0.46f), Vector3.one * 0.11f, gems[i]);
        }
        for (var i = 0; i < 5; i++)
        {
            var a = i / 5f * Mathf.PI * 2f;
            Prim.Make(PrimitiveType.Sphere, "Pearl", king, new Vector3(Mathf.Cos(a) * 0.35f, 3.52f, Mathf.Sin(a) * 0.35f), Vector3.one * 0.11f, new Color(1f, 0.97f, 0.9f));
        }

        // Jewelled scepter in the right hand; it swings up when he cheers.
        Prim.Make(PrimitiveType.Cylinder, "Scepter", kingArmR, new Vector3(0, -0.95f, 0.22f), new Vector3(0.07f, 0.6f, 0.07f), gold);
        Prim.Make(PrimitiveType.Sphere, "ScepterOrb", kingArmR, new Vector3(0, -0.3f, 0.22f), Vector3.one * 0.26f, gold);
        Prim.Make(PrimitiveType.Sphere, "ScepterGem", kingArmR, new Vector3(0, -0.15f, 0.22f), Vector3.one * 0.15f, red);
        Prim.Make(PrimitiveType.Cylinder, "Cuff", kingArmL, new Vector3(0, -0.88f, 0), new Vector3(0.4f, 0.06f, 0.4f), fur);
        Prim.Make(PrimitiveType.Cylinder, "Cuff", kingArmR, new Vector3(0, -0.88f, 0), new Vector3(0.4f, 0.06f, 0.4f), fur);
    }
}
