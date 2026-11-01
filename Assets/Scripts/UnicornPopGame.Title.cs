using UnityEngine;

// Title-screen cinematic, looping until the player starts: the unicorn patrols the sky and does a loop,
// lands to watch the jester and king celebrate, then gets a comically shocked face, and takes off again.
public sealed partial class UnicornPopGame
{
    private const float TitleLoop = 26f;
    private const float FlipStart = 1.6f, FlipEnd = 3.3f;
    private const float PatrolEnd = 3.4f, LandEnd = 7f;
    private const float FaceStart = 7f, SurpriseAt = 7.4f, FaceEnd = 12.2f;
    private const float KingEnd = 15.7f, JesterEnd = 19.2f, TakeoffStart = 19.2f;
    private static readonly Vector3 LandPos = new Vector3(1.5f, 0f, 40f);

    private float titleT, titleYaw;
    private int camPhase = -1;
    private bool flipBurst, surpriseCue, confettiCue1, confettiCue2;
    private float shotT;
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
        AnimateCharacterFaces(0f);
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
        if (t < TakeoffStart)
        {
            targetYaw = 0f;
            var y = 0f;
            // A startled hop backwards-and-up when she sees them.
            var h = t - SurpriseAt;
            if (h > 0f && h < 0.5f) y = Mathf.Sin(h / 0.5f * Mathf.PI) * 0.7f;
            return LandPos + Vector3.up * y;
        }
        var u = (t - TakeoffStart) / (TitleLoop - TakeoffStart);
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
        unicorn.CinTrail = (t < LandEnd - 1f) || t > TakeoffStart + 0.2f;

        // Whimsical loop-the-loop.
        unicorn.Flip = t > FlipStart && t < FlipEnd ? -360f * Ease((t - FlipStart) / (FlipEnd - FlipStart)) : 0f;
        if (!flipBurst && t > FlipStart)
        {
            flipBurst = true;
            unicorn.CinBurst();
            Sfx.Dash();
        }

        // Jester and king put on a show.
        if (t >= FaceEnd && t < JesterEnd)
        {
            var hop = (t > KingEnd ? Mathf.Abs(Mathf.Sin((t - KingEnd) * 9f)) * 0.7f : 0f);
            var squash = (t > KingEnd ? Mathf.Abs(Mathf.Cos((t - KingEnd) * 9f)) * 0.08f : 0f);
            jester.position = jesterHome + Vector3.up * hop;
            jester.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
            if (t > KingEnd) kingCheer = Mathf.Max(kingCheer, 0.3f);
            if (!confettiCue1 && t > FaceEnd + 0.2f)
            {
                confettiCue1 = true;
                ConfettiAtWall();
            }
            if (!confettiCue2 && t > KingEnd + 0.2f)
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
        if (t >= SurpriseAt && t < FaceEnd)
        {
            var s = t - SurpriseAt;
            surprise = Mathf.Clamp01(s / 0.12f);
            if (s > 1.0f && s < 3.0f) surprise = 0.8f + 0.2f * Mathf.Sin((s - 1.0f) * 16f);
            if (s > 3.0f) surprise = Mathf.Clamp01(1f - (s - 3.0f) / 1.4f);
            if (!surpriseCue)
            {
                surpriseCue = true;
                Sfx.Jump();
                effects.Text(unicorn.transform.position + Vector3.up * 3.3f, "!?", new Color(1f, 0.9f, 0.4f), 60);
                effects.Burst(unicorn.transform.position + Vector3.up * 2.4f, new Color(1f, 0.9f, 0.5f), 14, 4f);
            }
        }
        unicorn.Surprise = surprise;
        AnimateCharacterFaces(t);
    }

    // King: a slow, subtle, scheming grimace. Jester: a goofy cackle with wobbly buck teeth.
    private void AnimateCharacterFaces(float t)
    {
        var kingOn = t >= FaceEnd - 0.4f && t < JesterEnd;
        var kg = kingOn ? Mathf.Clamp01((t - (FaceEnd - 0.4f)) / 0.8f) : 0f;
        var tw = 0.5f + 0.5f * Mathf.Sin(Time.time * 3.2f);
        kingMouth.localScale = Vector3.Lerp(new Vector3(0.12f, 0.02f, 0.06f), new Vector3(0.36f + 0.03f * tw, 0.055f, 0.08f), kg);
        kingTeeth.localScale = new Vector3(0.3f * kg, 0.04f * kg, 0.03f * kg);
        for (var i = 0; i < 2; i++)
        {
            var sd = i == 0 ? -1f : 1f;
            // Brows slant down toward the nose, with a slow single raise.
            var raise = Mathf.Max(0f, Mathf.Sin(Time.time * 1.3f)) * 0.03f * kg;
            kingBrows[i].localPosition = new Vector3(sd * 0.2f, 2.79f + raise, 0.4f);
            kingBrows[i].localRotation = Quaternion.Euler(0, 0, 90f + sd * 16f * kg);
        }

        var jOn = t >= KingEnd - 0.2f && t < JesterEnd;
        var jg = jOn ? Mathf.Clamp01((t - (KingEnd - 0.2f)) / 0.25f) : 0f;
        var cackle = jg * (0.5f + 0.5f * Mathf.Sin(Time.time * 26f));
        jesterMouth.localScale = Vector3.Lerp(new Vector3(0.1f, 0.02f, 0.06f), new Vector3(0.52f, 0.1f + 0.14f * cackle, 0.09f), jg);
        jesterMouth.localPosition = new Vector3(0, 1.86f - 0.03f * cackle * jg, 0.33f);
        jesterTongue.localScale = new Vector3(0.22f, 0.07f, 0.05f) * jg;
        jesterTeeth.localScale = new Vector3(0.17f * jg, 0.1f * jg, 0.03f * jg);
        jesterTeeth.localPosition = new Vector3(0, 1.94f, 0.39f);
        for (var i = 0; i < 2; i++)
        {
            var sd = i == 0 ? -1f : 1f;
            jesterEyes[i].localScale = new Vector3(0.12f, 0.12f - 0.07f * jg, 0.12f);
            jesterBrows[i].localPosition = new Vector3(sd * 0.18f, 2.36f + 0.05f * jg + 0.03f * cackle, 0.36f);
            jesterBrows[i].localRotation = Quaternion.Euler(0, 0, 90f - sd * 22f * jg);
        }
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
        var phase = t >= JesterEnd ? 0 : t >= KingEnd ? 4 : t >= FaceEnd ? 3 : t >= FaceStart ? 2 : 0;
        var changed = phase != camPhase;
        if (changed) shotT = 0f;
        shotT += dt;
        var snap = changed && (phase >= 2 || camPhase >= 2 || camPhase == -1);
        camPhase = phase;

        var u = unicorn.transform;
        var fwd = unicorn.Forward;
        var right = Quaternion.Euler(0, 90f, 0) * fwd;
        // Quick whip-zoom: each close-up starts a little far away and pushes in.
        var push = Mathf.Lerp(1.9f, 1f, Ease(Mathf.Clamp01(shotT / 0.45f)));
        Vector3 want, look;
        float fov, rate;
        if (phase == 2)
        {
            var face = u.position + Quaternion.Euler(0, titleYaw, 0) * new Vector3(0, 1.85f, 1.1f);
            want = face + fwd * 2.5f * push + right * 0.3f + Vector3.up * 0.15f;
            look = face + Vector3.up * 0.12f;
            fov = 40f;
            rate = 14f;
        }
        else if (phase == 3)
        {
            var face = king.position + Vector3.up * 2.55f;
            var toward = unicorn.transform.position - king.position;
            toward.y = 0f;
            toward.Normalize();
            want = face + toward * 3.3f * push + Vector3.up * 0.1f;
            look = face - Vector3.up * 0.1f;
            fov = 38f;
            rate = 14f;
        }
        else if (phase == 4)
        {
            var face = jesterHome + Vector3.up * 2.2f;
            want = face + new Vector3(0.3f, 0.15f, -3.0f * push);
            look = jesterHome + Vector3.up * 2.1f;
            fov = 38f;
            rate = 14f;
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
