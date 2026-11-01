using System.Collections.Generic;
using UnityEngine;

// The Jester's colour lock guards the gate, then the King's boss balloon fights in three phases.
public sealed partial class UnicornPopGame
{
    private static readonly Color[] LockColors =
    {
        new Color(1f, 0.35f, 0.45f), new Color(0.4f, 0.7f, 1f), new Color(0.45f, 0.9f, 0.5f), new Color(0.75f, 0.45f, 1f),
    };

    private readonly List<Balloon> lockBalloons = new List<Balloon>();
    private readonly List<int> lockOrder = new List<int>();
    private readonly Color[] lockColors = new Color[3];
    private bool puzzleActive, puzzleDone;
    private int lockStep;

    private int kingPhase;
    private float kingTimer;

    // ---------- Jester's lock ----------

    private void StartPuzzle()
    {
        puzzleActive = true;
        Banner("JINGLES' LOCK!", "Pop the three balloons in the order shown", new Color(1f, 0.85f, 0.3f), 4f);
        Sfx.Start();
        SpawnLock();
    }

    private void SpawnLock()
    {
        foreach (var b in lockBalloons) if (b != null) b.Pop();
        lockBalloons.Clear();
        lockOrder.Clear();
        lockStep = 0;

        var pool = new List<Color>(LockColors);
        for (var i = 0; i < 3; i++)
        {
            var pick = Random.Range(0, pool.Count);
            lockColors[i] = pool[pick];
            pool.RemoveAt(pick);
        }
        for (var i = 0; i < 3; i++) lockOrder.Add(i);
        for (var i = 2; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            var t = lockOrder[i];
            lockOrder[i] = lockOrder[j];
            lockOrder[j] = t;
        }
        for (var i = 0; i < 3; i++)
        {
            var pos = new Vector3(-8f + i * 8f, Random.Range(6f, 10f), CastleZ - 20f);
            var b = Balloon.SpawnAmbient(transform, pos, BalloonKind.Normal, lockColors[i]);
            b.PuzzleId = i;
            lockBalloons.Add(b);
        }
    }

    private void UpdatePuzzle()
    {
        for (var i = 0; i < lockBalloons.Count; i++)
        {
            var b = lockBalloons[i];
            if (b == null) continue;
            var next = lockOrder[lockStep] == i;
            b.transform.localScale = Vector3.one * (next ? 1.3f + 0.12f * Mathf.Sin(Time.time * 7f) : 1.1f);
        }
    }

    // Returns true when the balloon was a lock balloon and has been handled.
    private bool HandleLockPop(Balloon b)
    {
        if (b.PuzzleId < 0) return false;
        var pos = b.transform.position;
        if (b.PuzzleId == lockOrder[lockStep])
        {
            lockStep++;
            lockBalloons[b.PuzzleId] = null;
            effects.Burst(pos, b.Color, 28, 8f);
            effects.Text(pos + Vector3.up * 2f, lockStep + " / 3", Color.white, 34);
            Sfx.Golden();
            GameFeel.Shake(0.2f);
            DreamSaver.Release(pos, b.Color);
            b.Pop();
            if (lockStep >= 3) SolvePuzzle();
            return true;
        }

        effects.Burst(pos, new Color(0.4f, 0.3f, 0.5f), 16, 6f);
        effects.Text(pos + Vector3.up * 2f, "Hee hee! Wrong order!", new Color(1f, 0.5f, 0.6f), 28);
        Sfx.Boom();
        GameFeel.Shake(0.4f);
        kingGrump = 0.8f;
        lockBalloons[b.PuzzleId] = null;
        b.Pop();
        SpawnLock();
        return true;
    }

    private void SolvePuzzle()
    {
        puzzleActive = false;
        puzzleDone = true;
        lockBalloons.Clear();
        Banner("THE LOCK BREAKS!", realm.KingName + " rolls out his boss balloon...", new Color(1f, 0.55f, 0.85f), 3.5f);
        Sfx.Record();
        kingGrump = 2f;
        bossRespawn = 2f;
    }

    // ---------- King fight ----------

    private void UpdateKingFight()
    {
        if (boss == null) { kingPhase = 0; return; }
        var ratio = boss.Hp / (float)boss.MaxHp;
        var phase = ratio > 0.6f ? 1 : ratio > 0.3f ? 2 : 3;
        if (phase != kingPhase)
        {
            kingPhase = phase;
            kingTimer = 1.5f;
            if (phase == 2) Banner("THE KING GETS CROSS!", "The boss dashes and the cannon speeds up", new Color(1f, 0.6f, 0.4f), 2.5f);
            if (phase == 3) Banner("KING'S LAST STAND!", "Watch out for his speedy balloons", new Color(1f, 0.4f, 0.4f), 2.5f);
            if (phase > 1)
            {
                Sfx.Start();
                GameFeel.Shake(0.6f);
                kingGrump = 1.5f;
            }
        }

        var toPlayer = unicorn.transform.position - boss.transform.position;
        toPlayer.y = 0f;
        toPlayer = toPlayer.normalized;
        boss.SetDrift(toPlayer * (1.4f + 0.5f * (phase - 1)) + Vector3.up * (phase == 1 ? 0.7f : 0.5f));

        kingTimer -= Time.deltaTime;
        if (kingTimer > 0f) return;
        if (phase == 2)
        {
            // Royal dash: a sideways lunge.
            var side = Random.value < 0.5f ? -1f : 1f;
            boss.Kick(Vector3.Cross(Vector3.up, toPlayer) * side * 14f);
            kingTimer = 2.5f;
        }
        else if (phase == 3)
        {
            for (var i = 0; i < 2; i++)
            {
                var from = boss.transform.position + Vector3.right * (i == 0 ? -2f : 2f);
                var vel = toPlayer * 9f + Vector3.up * 1.5f + Vector3.right * (i == 0 ? -3f : 3f);
                Balloon.Spawn(transform, from, vel, BalloonKind.Speedy, Balloon.KindColor(BalloonKind.Speedy));
            }
            effects.Text(boss.transform.position + Vector3.up * 3f, "Fetch!", new Color(1f, 0.7f, 0.3f), 26);
            kingTimer = 4f;
        }
        else kingTimer = 2f;
    }

    private void DrawPuzzleHud(float k)
    {
        if (!puzzleActive) return;
        Shadowed(new Rect(0, Screen.height * 0.16f, Screen.width, 30 * k), "Jingles' Lock - pop in this order", scoreStyle, Mathf.RoundToInt(22 * k), new Color(1f, 0.9f, 0.5f));
        var s = 44 * k;
        var x0 = Screen.width * 0.5f - 1.5f * s - s * 0.25f;
        for (var i = 0; i < 3; i++)
        {
            var rect = new Rect(x0 + i * (s * 1.25f), Screen.height * 0.16f + 36 * k, s, s);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(rect.x - 3 * k, rect.y - 3 * k, s + 6 * k, s + 6 * k), Texture2D.whiteTexture);
            var c = lockColors[lockOrder[i]];
            GUI.color = i < lockStep ? new Color(c.r, c.g, c.b, 0.3f) : c;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            if (i == lockStep)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(rect.x, rect.yMax + 4 * k, s, 4 * k), Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;
    }
}
