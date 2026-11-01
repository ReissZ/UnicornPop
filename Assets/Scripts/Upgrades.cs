using UnityEngine;

// Saved dreams are the currency. Upgrades persist between runs and realms.
public static class Upgrades
{
    public const int MaxLevel = 5;
    private const string WalletKey = "UnicornPopDreams";
    private const string LevelKey = "UnicornPopUpg_";

    public static readonly string[] Names = { "Horn Reach", "Light Wings", "Rainbow Dash", "Dream Magnet", "Head-Butt" };
    public static readonly string[] Blurbs =
    {
        "A wider horn pops more",
        "Flaps cost less stamina",
        "Dash recharges faster",
        "Wishes drift toward you",
        "Harder, wider head-butts (F / click)",
    };

    public const int Reach = 0, Wings = 1, Dash = 2, Magnet = 3, Butt = 4;

    public static int Wallet => PlayerPrefs.GetInt(WalletKey, 0);
    public static int Level(int i) => PlayerPrefs.GetInt(LevelKey + i, 0);
    public static int Cost(int i) => 15 + Level(i) * 15;
    public static bool Maxed(int i) => Level(i) >= MaxLevel;

    public static void Bank(int dreams)
    {
        if (dreams <= 0) return;
        PlayerPrefs.SetInt(WalletKey, Wallet + dreams);
        PlayerPrefs.Save();
    }

    public static bool Buy(int i)
    {
        if (Maxed(i) || Wallet < Cost(i)) return false;
        PlayerPrefs.SetInt(WalletKey, Wallet - Cost(i));
        PlayerPrefs.SetInt(LevelKey + i, Level(i) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static float HornReach => 1f + 0.12f * Level(Reach);
    public static float FlapCostScale => 1f - 0.14f * Level(Wings);
    public static float DashCooldownScale => 1f - 0.13f * Level(Dash);
    public static int ButtDamage => 1 + Level(Butt) / 2;
    public static float ButtRadius => 3.2f + 0.5f * Level(Butt);
    public static float ButtCooldownScale => 1f - 0.08f * Level(Butt);
    public static float MagnetRadius => Level(Magnet) == 0 ? 0f : 3f + 2.2f * Level(Magnet);
}
