using UnityEngine;

// Title-screen cinematic, looping until the player starts: the unicorn patrols the sky and does a loop,
// lands to watch the jester and king celebrate, then gets a comically shocked face, and takes off again.
public sealed partial class UnicornPopGame
{
    private const float TitleLoop = 28f;
    private const float FlipStart = 4.5f, FlipEnd = 6.2f;
    private const float PatrolEnd = 9f, LandEnd = 13.5f, FaceStart = 19f, FaceEnd = 25f;
    private const float SurpriseAt = 20.2f;
    private static readonly Vector3 LandPos = new Vector3(1.5f, 0f, 40f);

    private float titleT, titleYaw;
    private int camPhase = -1;
    private bool flipBurst, surpriseCue, confettiCue1, confettiCue2;
    private Vector3 jesterHome;

    private static Vector3 Patrol(float t)
    {
        var a = 0.25f * t;
        return new Vector3(26f * Mathf.Sin(a), 7f + 1.5f * Mathf.Sin(0.9f * t), 16f - 26f * Mathf.Cos(a));
    }

    private static float Ease(float s) => s * s * (3f - 2f * s);

    private void BeginTitle()
    {
        if (!onTitle) return;
        jesterHome = jester.position;
        unicorn.Cinematic = true;
        titleT = 0f;
        titleYaw = 90f;
        UpdateTitle(0f);
    }

    private void EndTitle()
    {
        unicorn.ResetTo(new Vector3(0, 0, -5f), 0f);
        jester.position = jesterHome;
        jester.localScale = Vector3.one;
        kingCheer = 0f;
        camPhase = -1;
    }

    private Vector3 TitlePosition(float t, out float targetYaw)
    {
        if (t < PatrolEnd)
        {
            var p = Patrol(t);
            var d = Patrol(t + 0.05f) - p;
            targetYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            return p;
        }
        if (t < LandEnd)
        {
            var from = Patrol(PatrolEnd);
            var s = (t - PatrolEnd) / (LandEnd - PatrolEnd);
            var e = Ease(s);
            targetYaw = 0f;
            return new Vector3(Mathf.Lerp(from.x, LandPos.x, e), from.y * (1f - s) * (1f - s), Mathf.Lerp(from.z, LandPos.z, e));
        }
        if (t < 25f)
        {
            targetYaw = 0f;
            var y = 0f;
            // A startled hop backwards-and-up when she sees them.
            var h = t - SurpriseAt;
            if (h > 0f && h < 0.5f) y = Mathf.Sin(h / 0.5f * Mathf.PI) * 0.7f;
            return LandPos + Vector3.up * y;
        }
        var u = (t - 25f) / (TitleLoop - 25f);
        var start = Patrol(0f);
        var pos = new Vector3(Mathf.Lerp(LandPos.x, start.x, Ease(u)), Mathf.Lerp(0f, start.y, u) + Mathf.Sin(u * Mathf.PI) * 3f, Mathf.Lerp(LandPos.z, start.z, Ease(u)));
        targetYaw = Mathf.Atan2(start.x - LandPos.x, start.z - LandPos.z) * Mathf.Rad2Deg;
        if (u > 0.7f) targetYaw = Mathf.LerpAngle(targetYaw, 90f, (u - 0.7f) / 0.3f);
        return pos;
    }

    private void UpdateTitle(float dt)
    {
        titleT += dt;
        if (titleT >= TitleLoop)
        {
            titleT -= TitleLoop;
            flipBurst = surpriseCue = confettiCue1 = confettiCue2 = false;
            titleYaw = 90f;
        }
        var t = titleT;

        var pos = TitlePosition(t, out var targetYaw);
        titleYaw = Mathf.LerpAngle(titleYaw, targetYaw, 1f - Mathf.Exp(-5f * dt));
        if (dt == 0f) titleYaw = targetYaw;
        unicorn.CinPlace(pos, titleYaw, dt);
        unicorn.CinTrail = (t < LandEnd - 1f) || t > 25.2f;

        // Whimsical loop-the-loop.
        unicorn.Flip = t > FlipStart && t < FlipEnd ? -360f * Ease((t - FlipStart) / (FlipEnd - FlipStart)) : 0f;
        if (!flipBurst && t > FlipStart)
        {
            flipBurst = true;
            unicorn.CinBurst();
            Sfx.Dash();
        }

        // Jester and king put on a show.
        if (t > LandEnd - 1.5f && t < 24.5f)
        {
            var hop = Mathf.Abs(Mathf.Sin((t - LandEnd) * 7f)) * 1.1f;
            var squash = Mathf.Abs(Mathf.Cos((t - LandEnd) * 7f)) * 0.1f;
            jester.position = jesterHome + Vector3.up * hop;
            jester.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
            kingCheer = Mathf.Max(kingCheer, 0.3f);
            if (!confettiCue1 && t > 14f)
            {
                confettiCue1 = true;
                ConfettiAtWall();
            }
            if (!confettiCue2 && t > 16.5f)
            {
                confettiCue2 = true;
                ConfettiAtWall();
            }
        }
        else
        {
            jester.position = jesterHome;
            jester.localScale = Vector3.one;
        }

        // Shocked face: eyebrows fly up, a few quick wiggles, then she calms down.
        float surprise = 0f;
        if (t >= SurpriseAt && t < 25f)
        {
            var s = t - SurpriseAt;
            surprise = Mathf.Clamp01(s / 0.12f);
            if (s > 1.2f && s < 3.6f) surprise = 0.8f + 0.2f * Mathf.Sin((s - 1.2f) * 16f);
            if (s > 3.6f) surprise = Mathf.Clamp01(1f - (s - 3.6f) / 1.2f);
            if (!surpriseCue)
            {
                surpriseCue = true;
                Sfx.Jump();
                effects.Text(unicorn.transform.position + Vector3.up * 3.3f, "!?", new Color(1f, 0.9f, 0.4f), 60);
                effects.Burst(unicorn.transform.position + Vector3.up * 2.4f, new Color(1f, 0.9f, 0.5f), 14, 4f);
            }
        }
        unicorn.Surprise = surprise;
    }

    private void ConfettiAtWall()
    {
        Color[] colors = { new Color(1f, 0.5f, 0.8f), new Color(1f, 0.85f, 0.3f), new Color(0.5f, 0.8f, 1f), new Color(0.6f, 1f, 0.7f) };
        for (var i = 0; i < colors.Length; i++)
        {
            effects.Burst(new Vector3(-10f + i * 6f, WallTopY + 3f, CastleZ - 4f), colors[i], 22, 7f, 1.2f);
        }
        Sfx.Golden();
    }

    private void TitleCamera(float dt)
    {
        var t = titleT;
        var phase = t >= FaceStart && t < FaceEnd ? 2 : (t >= LandEnd && t < FaceStart ? 1 : 0);
        var snap = phase != camPhase && (phase == 2 || camPhase == 2 || camPhase == -1);
        camPhase = phase;

        var u = unicorn.transform;
        var fwd = unicorn.Forward;
        var right = Quaternion.Euler(0, 90f, 0) * fwd;
        Vector3 want, look;
        float fov, rate;
        if (phase == 2)
        {
            var face = u.position + Quaternion.Euler(0, titleYaw, 0) * new Vector3(0, 1.85f, 1.1f);
            want = face + fwd * 2.5f + right * 0.3f + Vector3.up * 0.15f;
            look = face + Vector3.up * 0.12f;
            fov = 40f;
            rate = 14f;
        }
        else if (phase == 1)
        {
            want = new Vector3(5f, 3.8f, 33f + (t - LandEnd) * 0.2f);
            look = new Vector3(-1.5f, 9.6f, CastleZ - 4f);
            fov = 56f;
            rate = 1.6f;
        }
        else
        {
            var flipping = t > FlipStart - 0.3f && t < FlipEnd + 0.5f;
            var back = flipping ? 12.5f : 9f;
            want = u.position - fwd * back + right * (flipping ? 6f : 3f) + Vector3.up * 3.5f;
            look = u.position + Vector3.up * 1.2f;
            fov = 62f;
            rate = 3.5f;
        }

        var k = snap ? 1f : 1f - Mathf.Exp(-rate * dt);
        cam.transform.position = Vector3.Lerp(cam.transform.position, want, k);
        var rot = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);
        cam.transform.rotation = snap ? rot : Quaternion.Slerp(cam.transform.rotation, rot, 1f - Mathf.Exp(-Mathf.Max(rate, 4f) * dt));
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, snap ? 1f : 1f - Mathf.Exp(-4f * dt));
    }
}
