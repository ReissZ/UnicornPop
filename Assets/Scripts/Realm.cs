using UnityEngine;

public enum SceneryKind { None, Mushrooms, Crystals }

// A realm is one castle, one Jester and one King. Saving enough dreams opens the King's gate.
public sealed class Realm
{
    private const string IndexKey = "UnicornPopRealm";
    private const string UnlockKey = "UnicornPopUnlocked";

    public string Name, KingName, JesterName;
    public int DreamGoal = 30;
    public int BossHp = 8;
    public float TempoScale = 1f;
    public float SpeedyMix = 1f, SplitterMix = 1f, ArmoredMix = 1f;

    // Look of the land and castle.
    public Color Ground, HillA, HillB, Stone, StoneDark, Roof;
    public Color[][] Trees;
    public int TreeCount = 200;
    public int PineEvery = 3;
    public SceneryKind Scenery;

    // Sky themes the dream meter moves through (indices into Dreamland's themes).
    public int[] Sky = { 0, 1, 2, 4 };

    public static readonly Realm[] All =
    {
        new Realm
        {
            Name = "Sunny Meadow", KingName = "King Bartholomew", JesterName = "Jingles",
            DreamGoal = 30, BossHp = 8,
            Ground = new Color(0.45f, 0.78f, 0.4f), HillA = new Color(0.4f, 0.72f, 0.4f), HillB = new Color(0.36f, 0.66f, 0.45f),
            Stone = new Color(0.82f, 0.78f, 0.9f), StoneDark = new Color(0.62f, 0.58f, 0.74f), Roof = new Color(0.9f, 0.35f, 0.6f),
            Sky = new[] { 0, 1, 2, 4 },
        },
        new Realm
        {
            Name = "Enchanted Forest", KingName = "King Oberon the Overgrown", JesterName = "Tumble",
            DreamGoal = 36, BossHp = 10, TempoScale = 1.1f, SpeedyMix = 1.8f, SplitterMix = 1.5f,
            Ground = new Color(0.22f, 0.5f, 0.3f), HillA = new Color(0.16f, 0.4f, 0.28f), HillB = new Color(0.2f, 0.46f, 0.34f),
            Stone = new Color(0.55f, 0.66f, 0.55f), StoneDark = new Color(0.36f, 0.46f, 0.38f), Roof = new Color(0.3f, 0.7f, 0.5f),
            Trees = new[]
            {
                new[] { new Color(0.16f, 0.45f, 0.28f), new Color(0.25f, 0.6f, 0.35f) },
                new[] { new Color(0.12f, 0.38f, 0.32f), new Color(0.2f, 0.55f, 0.45f) },
                new[] { new Color(0.2f, 0.5f, 0.25f), new Color(0.45f, 0.72f, 0.3f) },
                new[] { new Color(0.55f, 0.4f, 0.8f), new Color(0.75f, 0.6f, 0.95f) },
                new[] { new Color(0.9f, 0.65f, 0.25f), new Color(1f, 0.8f, 0.4f) },
            },
            TreeCount = 380, PineEvery = 2, Scenery = SceneryKind.Mushrooms,
            Sky = new[] { 5, 6, 7, 4 },
        },
        new Realm
        {
            Name = "Crystal Islands", KingName = "King Glacius the Glittering", JesterName = "Prism",
            DreamGoal = 42, BossHp = 12, TempoScale = 1.15f, ArmoredMix = 2f, SplitterMix = 1.3f,
            Ground = new Color(0.78f, 0.72f, 0.95f), HillA = new Color(0.7f, 0.66f, 0.92f), HillB = new Color(0.62f, 0.78f, 0.95f),
            Stone = new Color(0.78f, 0.9f, 0.98f), StoneDark = new Color(0.55f, 0.7f, 0.88f), Roof = new Color(0.55f, 0.5f, 0.95f),
            Trees = new[]
            {
                new[] { new Color(0.6f, 0.85f, 0.95f), new Color(0.8f, 0.95f, 1f) },
                new[] { new Color(0.8f, 0.65f, 0.95f), new Color(0.92f, 0.82f, 1f) },
                new[] { new Color(0.95f, 0.7f, 0.85f), new Color(1f, 0.85f, 0.92f) },
            },
            TreeCount = 60, PineEvery = 2, Scenery = SceneryKind.Crystals,
            Sky = new[] { 8, 9, 3, 4 },
        },
    };

    public static int Index = Mathf.Clamp(PlayerPrefs.GetInt(IndexKey, 0), 0, Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 1), 1, All.Length) - 1);
    public static Realm Current => All[Mathf.Clamp(Index, 0, All.Length - 1)];
    public static bool HasNext => Index + 1 < All.Length;
    public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 1), 1, All.Length);

    public static void Select(int index)
    {
        Index = Mathf.Clamp(index, 0, All.Length - 1);
        PlayerPrefs.SetInt(IndexKey, Index);
        PlayerPrefs.Save();
    }

    public static void UnlockNext()
    {
        PlayerPrefs.SetInt(UnlockKey, Mathf.Max(Unlocked, Mathf.Min(All.Length, Index + 2)));
        PlayerPrefs.Save();
    }
}
