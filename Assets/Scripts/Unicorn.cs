using System.Collections.Generic;
using UnityEngine;

// The player: a winged unicorn that runs, jumps, flaps, glides and dashes. Hits are made with HornTip.
public sealed class Unicorn : MonoBehaviour
{
    private const float GroundSpeed = 7f;
    private const float AirSpeed = 9f;
    private const float DashSpeed = 26f;
    private const float DashTime = 0.28f;
    private const float DashCooldown = 1f;
    private const float JumpSpeed = 8.5f;
    private const float FlapSpeed = 6.5f;
    private const float FlapCost = 12f;
    private const float Gravity = -18f;
    private const float GlideFall = -2.5f;
    private const float MaxHeight = 28f;

    public const float MaxStamina = 100f;

    private sealed class Spark
    {
        public Transform T;
        public Vector3 Vel;
        public float Life;
        public float MaxLife;
        public float Size = 0.22f;
    }

    private Transform model;
    private readonly Transform[] legs = new Transform[4];
    private readonly Transform[] tail = new Transform[4];
    private readonly Transform[] wings = new Transform[2];
    private readonly List<Spark> sparks = new List<Spark>();

    private float yaw;
    private float vy;
    private Vector3 hv;
    private float stamina = MaxStamina;
    private float dashT;
    private float dashCd;
    private float flapT = 1f;
    private float flapCool;
    private float walkCycle;
    private float wingAngle = 80f;
    private float wingSpread = 0.45f;
    private float pitch;
    private float roll;

    // Off while the title or game-over screen is up.
    public bool Controllable { get; set; } = true;
    public Transform HornTip { get; private set; }
    public Transform HornBase { get; private set; }
    public float Stamina01 => stamina / MaxStamina;
    public float DashReady01 => 1f - Mathf.Clamp01(dashCd / DashCooldown);
    public bool Dashing => dashT > 0f;
    public Vector3 Forward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
    public bool Grounded => transform.position.y <= 0.01f;

    public void Build(Vector3 start, float startYaw)
    {
        transform.position = start;
        yaw = startYaw;

        var white = new Color(0.98f, 0.96f, 1f);
        var shade = new Color(0.86f, 0.84f, 0.94f);
        var pink = new Color(1f, 0.55f, 0.8f);
        var gold = new Color(1f, 0.82f, 0.25f);
        var rainbow = new[]
        {
            new Color(1f, 0.45f, 0.6f), new Color(1f, 0.75f, 0.35f),
            new Color(0.5f, 0.85f, 0.6f), new Color(0.5f, 0.65f, 1f),
        };

        model = new GameObject("Model").transform;
        model.SetParent(transform, false);

        Prim.Make(PrimitiveType.Sphere, "Torso", model, new Vector3(0, 1.0f, 0), new Vector3(0.75f, 0.75f, 1.4f), white);
        Prim.Make(PrimitiveType.Cylinder, "Neck", model, new Vector3(0, 1.45f, 0.6f), new Vector3(0.32f, 0.38f, 0.32f), white, new Vector3(35, 0, 0));
        Prim.Make(PrimitiveType.Sphere, "Head", model, new Vector3(0, 1.8f, 0.95f), new Vector3(0.42f, 0.42f, 0.55f), white);
        Prim.Make(PrimitiveType.Sphere, "Snout", model, new Vector3(0, 1.7f, 1.25f), new Vector3(0.3f, 0.27f, 0.32f), new Color(1f, 0.9f, 0.95f));
        Prim.Make(PrimitiveType.Sphere, "Nose", model, new Vector3(0, 1.72f, 1.4f), new Vector3(0.1f, 0.08f, 0.08f), pink);
        foreach (var s in new[] { -1f, 1f })
        {
            Prim.Make(PrimitiveType.Sphere, "Eye", model, new Vector3(0.17f * s, 1.88f, 1.12f), Vector3.one * 0.1f, new Color(0.15f, 0.08f, 0.25f));
            Prim.Make(PrimitiveType.Sphere, "Shine", model, new Vector3(0.19f * s, 1.91f, 1.16f), Vector3.one * 0.035f, Color.white);
            Prim.Cone("Ear", model, new Vector3(0.14f * s, 2.07f, 0.85f), new Vector3(0.11f, 0.22f, 0.11f), pink, new Vector3(0, 0, -15f * s));
        }

        // Golden horn, angled up and forward.
        var horn = Prim.Cone("Horn", model, new Vector3(0, 2.18f, 1.26f), new Vector3(0.13f, 0.6f, 0.13f), gold, new Vector3(50, 0, 0));
        HornTip = new GameObject("HornTip").transform;
        HornTip.SetParent(horn, false);
        HornTip.localPosition = new Vector3(0, 0.5f, 0);
        HornBase = new GameObject("HornBase").transform;
        HornBase.SetParent(horn, false);
        HornBase.localPosition = new Vector3(0, -0.5f, 0);

        for (var i = 0; i < 4; i++)
        {
            Prim.Make(PrimitiveType.Sphere, "Mane", model, new Vector3(0, 2.0f - i * 0.2f, 0.55f - i * 0.12f), new Vector3(0.2f, 0.3f, 0.34f), rainbow[i]);
        }

        var hips = new[] { new Vector3(-0.24f, 0.85f, 0.45f), new Vector3(0.24f, 0.85f, 0.45f), new Vector3(-0.24f, 0.85f, -0.45f), new Vector3(0.24f, 0.85f, -0.45f) };
        for (var i = 0; i < 4; i++)
        {
            var pivot = new GameObject("Leg").transform;
            pivot.SetParent(model, false);
            pivot.localPosition = hips[i];
            Prim.Make(PrimitiveType.Cylinder, "Shin", pivot, new Vector3(0, -0.4f, 0), new Vector3(0.14f, 0.4f, 0.14f), i % 2 == 0 ? white : shade);
            Prim.Make(PrimitiveType.Sphere, "Hoof", pivot, new Vector3(0, -0.8f, 0), new Vector3(0.17f, 0.12f, 0.2f), new Color(0.75f, 0.65f, 0.9f));
            legs[i] = pivot;
        }

        for (var i = 0; i < tail.Length; i++)
        {
            var piv = new GameObject("Tail").transform;
            piv.SetParent(model, false);
            piv.localPosition = new Vector3(0, 1.1f, -0.65f);
            Prim.Make(PrimitiveType.Sphere, "TailPiece", piv, new Vector3(0, -0.1f - i * 0.05f, -0.2f - i * 0.2f), new Vector3(0.22f, 0.22f, 0.34f), rainbow[i]);
            tail[i] = piv;
        }

        for (var i = 0; i < 2; i++)
        {
            var s = i == 0 ? 1f : -1f;
            var wing = new GameObject("Wing").transform;
            wing.SetParent(model, false);
            wing.localPosition = new Vector3(0.3f * s, 1.3f, 0.15f);
            Prim.Make(PrimitiveType.Cube, "Feather", wing, new Vector3(0.45f * s, 0, 0), new Vector3(0.9f, 0.05f, 0.5f), white);
            Prim.Make(PrimitiveType.Cube, "Feather", wing, new Vector3(0.65f * s, 0, -0.3f), new Vector3(0.9f, 0.05f, 0.4f), pink);
            Prim.Make(PrimitiveType.Cube, "Feather", wing, new Vector3(0.85f * s, 0, -0.55f), new Vector3(0.8f, 0.05f, 0.3f), new Color(0.75f, 0.65f, 1f));
            wings[i] = wing;
        }

        Pose(0f, 0f);
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        var grounded = Grounded;

        var turn = 0f;
        var throttle = 0f;
        var spaceDown = false;
        var spaceHeld = false;
        var dive = false;
        var dashPressed = false;
        if (Controllable)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttle = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttle = -0.4f;
            spaceDown = Input.GetKeyDown(KeyCode.Space);
            spaceHeld = Input.GetKey(KeyCode.Space);
            dive = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
            dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }

        yaw += turn * (grounded ? 130f : 150f) * dt;
        var fwd = Forward;

        dashCd -= dt;
        flapCool -= dt;
        if (dashPressed && dashCd <= 0f)
        {
            dashT = DashTime;
            dashCd = DashCooldown;
            RainbowBlast(fwd, 60, true);
            Sfx.Dash();
        }

        float targetSpeed;
        float response;
        if (dashT > 0f)
        {
            dashT -= dt;
            targetSpeed = DashSpeed;
            response = 20f;
            vy *= Mathf.Exp(-8f * dt);
            RainbowBlast(fwd, 3, false);
        }
        else
        {
            targetSpeed = throttle * (grounded ? GroundSpeed : AirSpeed);
            response = grounded ? 10f : 3f;
        }
        hv = Vector3.Lerp(hv, fwd * targetSpeed, 1f - Mathf.Exp(-response * dt));

        if (spaceDown)
        {
            if (grounded)
            {
                vy = JumpSpeed;
                Sfx.Jump();
                Emit(transform.position + Vector3.up * 0.1f, gold: false, 6, 2f);
            }
            else
            {
                TryFlap();
            }
        }
        else if (spaceHeld && !grounded && vy < -1f && flapCool <= 0f)
        {
            TryFlap();
        }

        if (!grounded || vy > 0f)
        {
            vy += Gravity * dt;
            if (dive) vy = Mathf.Max(vy - 30f * dt, -16f);
            else if (spaceHeld && vy < GlideFall && dashT <= 0f) vy = Mathf.Lerp(vy, GlideFall, 1f - Mathf.Exp(-8f * dt));
        }

        var pos = transform.position + (hv + Vector3.up * vy) * dt;
        pos.x = Mathf.Clamp(pos.x, -45f, 45f);
        pos.z = Mathf.Clamp(pos.z, -25f, 50f);
        if (pos.y >= MaxHeight)
        {
            pos.y = MaxHeight;
            vy = Mathf.Min(vy, 0f);
        }
        if (pos.y <= 0f)
        {
            if (vy < -6f) Emit(pos, gold: false, 8, 2.5f);
            pos.y = 0f;
            vy = Mathf.Max(vy, 0f);
        }
        transform.position = pos;

        stamina = Mathf.Min(MaxStamina, stamina + (Grounded ? 50f : 14f) * dt);

        Pose(turn, dt);
        UpdateSparks(dt);
    }

    private void TryFlap()
    {
        if (stamina < FlapCost) return;
        stamina -= FlapCost;
        vy = Mathf.Min(Mathf.Max(vy, 0f) + FlapSpeed, 10f);
        flapT = 0f;
        flapCool = 0.4f;
        Emit(transform.position + Vector3.up * 0.8f, gold: false, 5, 1.5f);
        Sfx.Flap();
    }

    private void Pose(float turn, float dt)
    {
        var airborne = !Grounded;
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        var targetPitch = dashT > 0f ? 18f : Mathf.Clamp(-vy * 2.5f, -25f, 25f);
        var targetRoll = airborne ? -turn * 20f : 0f;
        var k = 1f - Mathf.Exp(-8f * dt);
        pitch = Mathf.Lerp(pitch, airborne ? targetPitch : 0f, k);
        roll = Mathf.Lerp(roll, targetRoll, k);
        model.localRotation = Quaternion.Euler(pitch, 0, roll);

        var planar = new Vector2(hv.x, hv.z).magnitude;
        walkCycle += (airborne ? 3f : 3f + planar * 1.6f) * dt;
        var bob = airborne ? 0f : Mathf.Abs(Mathf.Sin(walkCycle)) * Mathf.Min(planar / GroundSpeed, 1f) * 0.1f;
        model.localPosition = new Vector3(0, bob, 0);

        for (var i = 0; i < legs.Length; i++)
        {
            float swing;
            if (airborne)
            {
                swing = i < 2 ? -35f : 40f;
            }
            else
            {
                var phase = (i == 0 || i == 3) ? 0f : Mathf.PI;
                swing = Mathf.Sin(walkCycle + phase) * Mathf.Min(planar / GroundSpeed, 1.2f) * 35f;
            }
            legs[i].localRotation = Quaternion.Euler(swing, 0, 0);
        }
        for (var i = 0; i < tail.Length; i++)
        {
            var lift = airborne ? 10f : 25f;
            tail[i].localRotation = Quaternion.Euler(-lift + i * 4f, Mathf.Sin(Time.time * 5f + i * 0.6f) * 10f, 0);
        }

        // Wings stay folded on the ground, beat on a flap and hold open while gliding.
        if (flapT < 1f) flapT += dt / 0.35f;
        float angle;
        float spread;
        if (!airborne)
        {
            angle = 80f;
            spread = 0.45f;
        }
        else if (flapT < 1f)
        {
            angle = 55f * Mathf.Cos(flapT * Mathf.PI * 2f) - 5f;
            spread = 1f;
        }
        else
        {
            angle = 12f + Mathf.Sin(Time.time * 4f) * 8f;
            spread = 1f;
        }
        var wk = 1f - Mathf.Exp(-(flapT < 1f ? 30f : 10f) * dt);
        wingAngle = Mathf.Lerp(wingAngle, angle, wk);
        wingSpread = Mathf.Lerp(wingSpread, spread, wk);
        wings[0].localRotation = Quaternion.Euler(0, 0, wingAngle);
        wings[1].localRotation = Quaternion.Euler(0, 0, -wingAngle);
        wings[0].localScale = new Vector3(wingSpread, 1f, 1f);
        wings[1].localScale = new Vector3(wingSpread, 1f, 1f);
    }

    private static readonly Color[] Rainbow =
    {
        new Color(1f, 0.25f, 0.3f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.95f, 0.3f),
        new Color(0.3f, 0.95f, 0.45f), new Color(0.3f, 0.7f, 1f), new Color(0.7f, 0.4f, 1f),
    };

    // Sparkles shot backwards from the tail in a cone; the opening burst is bigger and faster.
    private void RainbowBlast(Vector3 fwd, int count, bool burst)
    {
        var origin = transform.position + Vector3.up * 1.1f - fwd * 1.2f;
        for (var i = 0; i < count; i++)
        {
            var size = Random.Range(0.18f, burst ? 0.5f : 0.38f);
            var color = Rainbow[Random.Range(0, Rainbow.Length)];
            var t = Prim.Make(PrimitiveType.Sphere, "Sparkle", null, origin + Random.insideUnitSphere * 0.4f, Vector3.one * size, color);
            var dir = (-fwd + Random.insideUnitSphere * (burst ? 0.7f : 0.35f)).normalized;
            var speed = Random.Range(6f, burst ? 22f : 12f);
            var life = Random.Range(0.5f, burst ? 1.1f : 0.8f);
            sparks.Add(new Spark { T = t, Vel = dir * speed, Life = life, MaxLife = life, Size = size });
        }
    }

    private void Emit(Vector3 pos, bool gold, int count, float speed)
    {
        for (var i = 0; i < count; i++)
        {
            var color = gold ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 1f, 1f);
            var t = Prim.Make(PrimitiveType.Sphere, "Spark", null, pos, Vector3.one * Random.Range(0.1f, 0.22f), color);
            var life = Random.Range(0.35f, 0.7f);
            sparks.Add(new Spark { T = t, Vel = Random.onUnitSphere * speed, Life = life, MaxLife = life });
        }
    }

    private void UpdateSparks(float dt)
    {
        for (var i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i];
            s.Life -= dt;
            if (s.Life <= 0f)
            {
                Destroy(s.T.gameObject);
                sparks.RemoveAt(i);
                continue;
            }
            s.Vel *= Mathf.Exp(-2.5f * dt);
            s.T.position += s.Vel * dt;
            s.T.localScale = Vector3.one * (s.Size * s.Life / s.MaxLife);
        }
    }
}
