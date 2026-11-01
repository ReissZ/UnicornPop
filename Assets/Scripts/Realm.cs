// A realm is one castle, one Jester and one King. Saving enough dreams opens the King's gate.
public sealed class Realm
{
    public string Name;
    public string KingName;
    public int DreamGoal = 30;
    public int BossHp = 8;

    public static readonly Realm Meadow = new Realm
    {
        Name = "Sunny Meadow",
        KingName = "King Bartholomew",
        DreamGoal = 30,
        BossHp = 8,
    };
}
