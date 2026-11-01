using UnityEngine;

// Wave engine: each realm is a scripted run of waves that ends with the King's boss balloon.
public sealed partial class UnicornPopGame
{
    private const string StarsKey = "UnicornPopStars_";

    private readonly Realm realm = Realm.Current;
    private bool gateOpen;
    private bool bossDefeated;
    private bool realmCleared;
    private int stars;
    private float bannerT;
    private float bossRespawn;
    private int lastMilestone;
    private string bannerText = "", bannerSub = "";
    private Color bannerColor = Color.white;

    private float Progress => Mathf.Clamp01(DreamSaver.Saved / (float)realm.DreamGoal);

    private void Banner(string text, string sub, Color color, float seconds = 3f)
    {
        bannerText = text;
        bannerSub = sub;
        bannerColor = color;
        bannerT = seconds;
    }

    // Difficulty follows the dream meter rather than a clock, so exploring is never punished.
    private void UpdateRealm()
    {
        if (bannerT > 0f) bannerT -= Time.deltaTime;
        if (realmCleared) return;

        var p = Progress;
        cannon.Remaining = engaged ? 999 : 0;
        cannon.Tempo = realm.TempoScale * (0.8f + 0.9f * p) * (slowPower > 0f ? 0.5f : 1f) * (boss != null ? 0.6f : 1f);
        cannon.GoldenChance = 0.1f + 0.05f * p;
        cannon.DoubleChance = Mathf.Clamp01((p - 0.35f) * 0.5f);
        cannon.RainbowChance = p > 0.15f ? 0.05f : 0f;
        cannon.StarChance = p > 0.25f ? 0.04f : 0f;
        cannon.StormChance = p > 0.45f ? 0.06f + 0.08f * p : 0f;
        cannon.SpeedyChance = p > 0.1f ? (0.05f + 0.05f * p) * realm.SpeedyMix : 0f;
        cannon.SplitterChance = p > 0.25f ? 0.06f * realm.SplitterMix : 0f;
        cannon.ArmoredChance = p > 0.35f ? (0.05f + 0.06f * p) * realm.ArmoredMix : 0f;
        Balloon.RiseScale = 1f + 0.3f * p;

        var stage = p < 0.3f ? 0 : p < 0.6f ? 1 : p < 0.85f ? 2 : 3;
        if (stage != lastMilestone)
        {
            lastMilestone = stage;
            var theme = realm.Sky[stage];
            dreamland.SetTheme(theme);
            Music.SetTheme(theme);
            if (DreamSaver.Saved > 0) Banner(dreamland.ThemeName, "The dream light grows...", new Color(1f, 0.9f, 0.4f), 2.5f);
        }

        if (!gateOpen && DreamSaver.Saved >= realm.DreamGoal) OpenGate();
        if (!gateOpen) return;

        if (!puzzleDone) return;
        if (bossDefeated) ClearRealm();
        else if (boss != null) UpdateKingFight();
        else
        {
            // If the boss floated off, the King sends it back after a breather.
            bossRespawn -= Time.deltaTime;
            if (bossRespawn <= 0f) SpawnBoss();
        }
    }

    private void OpenGate()
    {
        gateOpen = true;
        kingGrump = 2f;
        Sfx.Record();
        effects.Text(unicorn.transform.position + Vector3.up * 4f, "The gate opens!", new Color(1f, 0.9f, 0.4f), 46);
        StartPuzzle();
    }

    private void ClearRealm()
    {
        realmCleared = true;
        cannon.Active = false;
        cannon.Remaining = 0;
        stars = hearts >= StartHearts ? 3 : hearts == 2 ? 2 : 1;
        Upgrades.Bank(DreamSaver.Saved + stars * 10);
        Realm.UnlockNext();
        var key = StarsKey + realm.Name;
        if (stars > PlayerPrefs.GetInt(key, 0))
        {
            PlayerPrefs.SetInt(key, stars);
            PlayerPrefs.Save();
        }
        if (score > best)
        {
            best = score;
            newRecord = true;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }
        foreach (var b in new System.Collections.Generic.List<Balloon>(Balloon.Active))
        {
            if (b.Ambient) continue;
            effects.Burst(b.transform.position, b.Color, 14, 6f);
            b.Pop();
        }
        Music.SetBoss(false);
        kingGrump = 3f;
        StartCoroutine(FireworkShow());
    }

    private void UpdateRealmCleared()
    {
        ShopInput();
        var next = Input.GetKeyDown(KeyCode.Return) || Pad.StartDown;
        if (next && Realm.HasNext) Realm.Select(Realm.Index + 1);
        if (Input.GetKeyDown(KeyCode.R) || next)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ---------- HUD ----------

    private void DrawWaveHud(float k)
    {
        DrawPuzzleHud(k);
        if (bannerT > 0f && !realmCleared)
        {
            var a = Mathf.Clamp01(bannerT * 2f);
            var pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.04f;
            var c = bannerColor;
            c.a = a;
            Shadowed(new Rect(0, Screen.height * 0.28f, Screen.width, 70 * k), bannerText, bigStyle, Mathf.RoundToInt(54 * k * pulse), c);
            Shadowed(new Rect(0, Screen.height * 0.28f + 62 * k, Screen.width, 40 * k), bannerSub, midStyle, Mathf.RoundToInt(28 * k), new Color(1f, 1f, 1f, a));
        }

        var w = 440 * k;
        var x = Screen.width * 0.5f - w * 0.5f;
        var y = Screen.height - 28 * k;
        var h = 12 * k;
        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(x - 3 * k, y - 3 * k, w + 6 * k, h + 6 * k), Texture2D.whiteTexture);
        var glow = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
        GUI.color = gateOpen ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.6f * glow, 0.85f);
        GUI.DrawTexture(new Rect(x, y, w * (realmCleared ? 1f : Progress), h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        var label = realmCleared ? realm.Name + " saved!" : gateOpen ? realm.KingName + "'s gate is open!" : "Dreams " + Mathf.Min(DreamSaver.Saved, realm.DreamGoal) + " / " + realm.DreamGoal;
        Shadowed(new Rect(0, y - 26 * k, Screen.width, 22 * k), label, scoreStyle, Mathf.RoundToInt(17 * k), new Color(1f, 0.9f, 0.7f));
    }

    private void ShopInput()
    {
        for (var i = 0; i < Upgrades.Names.Length; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
            if (Upgrades.Buy(i)) { Sfx.Golden(); shopFlash = 1f; } else Sfx.Lose();
        }
    }

    private float shopFlash;

    private void DrawShop(float k, float top)
    {
        shopFlash = Mathf.Max(0f, shopFlash - Time.deltaTime);
        Shadowed(new Rect(0, top, Screen.width, 34 * k), "Dream Shop   -   you have " + Upgrades.Wallet + " dreams", midStyle, Mathf.RoundToInt(26 * k), new Color(1f, 0.9f, 0.5f + shopFlash * 0.5f));
        for (var i = 0; i < Upgrades.Names.Length; i++)
        {
            var lvl = Upgrades.Level(i);
            var affordable = !Upgrades.Maxed(i) && Upgrades.Wallet >= Upgrades.Cost(i);
            var cost = Upgrades.Maxed(i) ? "MAX" : Upgrades.Cost(i) + " dreams";
            var text = "[" + (i + 1) + "] " + Upgrades.Names[i] + "  Lv " + lvl + "/" + Upgrades.MaxLevel + "  -  " + cost + "  (" + Upgrades.Blurbs[i] + ")";
            var c = affordable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            Shadowed(new Rect(0, top + (36 + i * 28) * k, Screen.width, 26 * k), text, scoreStyle, Mathf.RoundToInt(19 * k), c);
        }
    }

    private void DrawRealmCleared(float k)
    {
        GUI.color = new Color(0, 0, 0, 0.45f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        var cy = Screen.height * 0.5f;
        Shadowed(new Rect(0, cy - 200 * k, Screen.width, 80 * k), realm.Name + " saved!", bigStyle, Mathf.RoundToInt(66 * k), new Color(1f, 0.9f, 0.4f));
        Shadowed(new Rect(0, cy - 120 * k, Screen.width, 50 * k), realm.KingName + " is no longer worried.", midStyle, Mathf.RoundToInt(28 * k), Color.white);

        var s = 46 * k;
        for (var i = 0; i < 3; i++)
        {
            GUI.color = i < stars ? new Color(1f, 0.85f, 0.2f) : new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 1.5f * (s + 8 * k) + i * (s + 8 * k), cy - 70 * k, s, s), heartTex);
        }
        GUI.color = Color.white;
        Shadowed(new Rect(0, cy - 14 * k, Screen.width, 44 * k), "Score " + score + "   Dreams saved " + DreamSaver.Saved + "  (+" + stars * 10 + " star bonus)", midStyle, Mathf.RoundToInt(32 * k), Color.white);
        if (newRecord) Shadowed(new Rect(0, cy + 40 * k, Screen.width, 40 * k), "New high score!", midStyle, Mathf.RoundToInt(28 * k), new Color(1f, 0.9f, 0.3f));
        DrawShop(k, cy + 130 * k);
        Shadowed(new Rect(0, Screen.height - 40 * k, Screen.width, 30 * k), (Realm.HasNext ? "Press Enter for " + Realm.All[Realm.Index + 1].Name + "   -   R to replay" : "The Herdlight glows brighter...   Press R to play again"), midStyle, Mathf.RoundToInt(22 * k), Color.white);
    }
}
