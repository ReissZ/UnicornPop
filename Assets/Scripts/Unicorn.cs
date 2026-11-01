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
    private readonly Transform[] tail = new Transform[6];
    private readonly Transform[] wings = new Transform[2];
    private readonly Transform[][] feathers = new Transform[2][];
    private readonly Transform[] mane = new Transform[6];
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
    private float wingAngle = 20f;
    private float wingFold = 1f;
    private float pitch;
    private float roll;

    // Off while the title or game-over screen is up.
    public bool Controllable { get; set; } = true;

    // Title-screen cinematic: the game places the unicorn and this only animates the body.
    public bool Cinematic;
    public bool CinTrail;
    public float Flip;
    public float Surprise;
    private readonly Transform[] brows = new Transform[2];
    private readonly Transform[] eyes = new Transform[2];
    private Transform mouth;
    private Vector3 mouthBase;
    private static readonly Vector3 EyeBase = new Vector3(0.12f, 0.15f, 0.1f);
    private float cinFlapTimer;

    public void CinPlace(Vector3 pos, float yawDegrees, float dt)
    {
        if (dt > 0f)
        {
            hv = new Vector3(pos.x - transform.position.x, 0f, pos.z - transform.position.z) / dt;
            vy = (pos.y - transform.position.y) / dt;
        }
        cinTurn = dt > 0f ? Mathf.Clamp(Mathf.DeltaAngle(yaw, yawDegrees) / dt / 60f, -1f, 1f) : 0f;
        yaw = yawDegrees;
        transform.position = pos;
        cinFlapTimer -= dt;
        if (!Grounded && cinFlapTimer <= 0f && flapT >= 1f)
        {
            flapT = 0f;
            cinFlapTimer = Mathf.Abs(Flip) > 1f ? 0.3f : 0.65f;
        }
    }
    private float cinTurn;

    public void CinBurst() => RainbowBlast(Forward, 50, true);

    public void ResetTo(Vector3 pos, float yawDegrees)
    {
        Cinematic = false;
        CinTrail = false;
        Flip = 0f;
        Surprise = 0f;
        hv = Vector3.zero;
        vy = 0f;
        pitch = 0f;
        roll = 0f;
        yaw = yawDegrees;
        transform.position = pos;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }
    private static readonly float[] FeatherLen = { 1.75f, 1.65f, 1.5f, 1.3f, 1.1f, 0.9f };
    private static readonly float[] FeatherFan = { 0f, 12f, 25f, 38f, 52f, 66f };

    public Transform HornTip { get; private set; }
    public Transform HornBase { get; private set; }
    public float Stamina01 => stamina / MaxStamina;
    public float DashReady01 => 1f - Mathf.Clamp01(dashCd / DashCooldown);
    // Power-ups, switched on and off by the game.
    public bool WingBoost;
    public bool RainbowTrail;
    private readonly List<Vector3> trail = new List<Vector3>();
    private float trailTimer;
    public IReadOnlyList<Vector3> TrailPoints => trail;

    public void StarBoost()
    {
        stamina = MaxStamina;
        dashCd = 0f;
    }

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
        var lilac = new Color(0.72f, 0.62f, 1f);
        var pastel = new[]
        {
            new Color(1f, 0.5f, 0.6f), new Color(1f, 0.72f, 0.4f), new Color(1f, 0.93f, 0.5f),
            new Color(0.55f, 0.88f, 0.6f), new Color(0.5f, 0.75f, 1f), new Color(0.75f, 0.6f, 1f),
        };

        model = new GameObject("Model").transform;
        model.SetParent(transform, false);

        Prim.Make(PrimitiveType.Sphere, "Torso", model, new Vector3(0, 1.0f, 0), new Vector3(0.75f, 0.75f, 1.4f), white);
        Prim.Make(PrimitiveType.Sphere, "Chest", model, new Vector3(0, 1.1f, 0.45f), new Vector3(0.62f, 0.68f, 0.6f), white);
        Prim.Make(PrimitiveType.Sphere, "Rump", model, new Vector3(0, 1.02f, -0.5f), new Vector3(0.7f, 0.72f, 0.7f), white);
        Prim.Make(PrimitiveType.Cylinder, "Neck", model, new Vector3(0, 1.45f, 0.6f), new Vector3(0.32f, 0.38f, 0.32f), white, new Vector3(35, 0, 0));
        Prim.Make(PrimitiveType.Sphere, "Head", model, new Vector3(0, 1.8f, 0.95f), new Vector3(0.44f, 0.44f, 0.55f), white);
        Prim.Make(PrimitiveType.Sphere, "Snout", model, new Vector3(0, 1.7f, 1.25f), new Vector3(0.3f, 0.27f, 0.32f), new Color(1f, 0.92f, 0.96f));
        Prim.Make(PrimitiveType.Sphere, "Nose", model, new Vector3(0, 1.72f, 1.4f), new Vector3(0.12f, 0.09f, 0.08f), pink);
        mouthBase = new Vector3(0.14f, 0.025f, 0.08f);
        mouth = Prim.Make(PrimitiveType.Sphere, "Mouth", model, new Vector3(0, 1.6f, 1.34f), mouthBase, new Color(0.85f, 0.4f, 0.55f));
        foreach (var sd in new[] { -1f, 1f })
        {
            var browColor = new Color(0.42f, 0.28f, 0.6f);
            eyes[sd > 0 ? 0 : 1] = Prim.Make(PrimitiveType.Sphere, "Eye", model, new Vector3(0.18f * sd, 1.87f, 1.1f), EyeBase, new Color(0.2f, 0.1f, 0.35f));
            brows[sd > 0 ? 0 : 1] = Prim.Make(PrimitiveType.Cylinder, "Brow", model, new Vector3(0.18f * sd, 1.99f, 1.07f), new Vector3(0.035f, 0.075f, 0.035f), browColor, new Vector3(0, 0, 90f));
            Prim.Make(PrimitiveType.Sphere, "Shine", model, new Vector3(0.205f * sd, 1.915f, 1.15f), Vector3.one * 0.045f, Color.white);
            Prim.Make(PrimitiveType.Sphere, "Shine2", model, new Vector3(0.2f * sd, 1.83f, 1.14f), Vector3.one * 0.02f, Color.white);
            Prim.Make(PrimitiveType.Cylinder, "Lash", model, new Vector3(0.21f * sd, 1.98f, 1.06f), new Vector3(0.015f, 0.05f, 0.015f), new Color(0.2f, 0.1f, 0.35f), new Vector3(0, 0, -35f * sd));
            Prim.Make(PrimitiveType.Sphere, "Blush", model, new Vector3(0.19f * sd, 1.74f, 1.12f), new Vector3(0.08f, 0.06f, 0.05f), new Color(1f, 0.68f, 0.78f));
            Prim.Cone("Ear", model, new Vector3(0.13f * sd, 2.06f, 0.8f), new Vector3(0.12f, 0.26f, 0.1f), white, new Vector3(-8f, 0, -12f * sd));
            Prim.Cone("EarInner", model, new Vector3(0.13f * sd, 2.05f, 0.82f), new Vector3(0.07f, 0.18f, 0.05f), pink, new Vector3(-8f, 0, -12f * sd));
        }

        // Golden horn with spiral rings, angled up and forward.
        var horn = Prim.Cone("Horn", model, new Vector3(0, 2.18f, 1.26f), new Vector3(0.13f, 0.6f, 0.13f), gold, new Vector3(50, 0, 0));
        foreach (var ry in new[] { -0.25f, -0.02f, 0.2f })
        {
            Prim.Make(PrimitiveType.Cylinder, "Ring", horn, new Vector3(0, ry, 0), new Vector3((0.5f - ry) * 1.15f, 0.02f, (0.5f - ry) * 1.15f), new Color(0.9f, 0.62f, 0.12f));
        }
        HornTip = new GameObject("HornTip").transform;
        HornTip.SetParent(horn, false);
        HornTip.localPosition = new Vector3(0, 0.5f, 0);
        HornBase = new GameObject("HornBase").transform;
        HornBase.SetParent(horn, false);
        HornBase.localPosition = new Vector3(0, -0.5f, 0);

        // Forelock between the ears, then a mane that flows down the back of the neck.
        Prim.Make(PrimitiveType.Sphere, "Forelock", model, new Vector3(0, 2.0f, 0.9f), new Vector3(0.2f, 0.2f, 0.34f), pastel[0], new Vector3(-20, 0, 0));
        for (var i = 0; i < mane.Length; i++)
        {
            var t = i / (mane.Length - 1f);
            var piv = new GameObject("Mane").transform;
            piv.SetParent(model, false);
            piv.localPosition = new Vector3(0, Mathf.Lerp(1.98f, 1.4f, t), Mathf.Lerp(0.66f, 0.2f, t));
            Prim.Make(PrimitiveType.Sphere, "ManeBlob", piv, Vector3.zero, new Vector3(0.2f, 0.32f, 0.3f), pastel[i]);
            mane[i] = piv;
        }

        var hips = new[] { new Vector3(-0.24f, 0.85f, 0.45f), new Vector3(0.24f, 0.85f, 0.45f), new Vector3(-0.24f, 0.85f, -0.45f), new Vector3(0.24f, 0.85f, -0.45f) };
        for (var i = 0; i < 4; i++)
        {
            var pivot = new GameObject("Leg").transform;
            pivot.SetParent(model, false);
            pivot.localPosition = hips[i];
            var coat = i % 2 == 0 ? white : shade;
            Prim.Make(PrimitiveType.Cylinder, "Thigh", pivot, new Vector3(0, -0.2f, 0), new Vector3(0.2f, 0.25f, 0.2f), coat);
            Prim.Make(PrimitiveType.Cylinder, "Shin", pivot, new Vector3(0, -0.5f, 0), new Vector3(0.13f, 0.3f, 0.13f), coat);
            Prim.Make(PrimitiveType.Sphere, "Fetlock", pivot, new Vector3(0, -0.74f, 0), Vector3.one * 0.17f, coat);
            Prim.Make(PrimitiveType.Cylinder, "Hoof", pivot, new Vector3(0, -0.82f, 0.01f), new Vector3(0.19f, 0.05f, 0.21f), gold);
            legs[i] = pivot;
        }

        for (var i = 0; i < tail.Length; i++)
        {
            var piv = new GameObject("Tail").transform;
            piv.SetParent(model, false);
            piv.localPosition = new Vector3(0, 1.1f, -0.8f);
            var w = 0.26f - i * 0.02f;
            Prim.Make(PrimitiveType.Sphere, "TailPiece", piv, new Vector3(0, -0.04f - i * 0.04f, -0.18f - i * 0.2f), new Vector3(w, w, 0.38f), pastel[i]);
            tail[i] = piv;
        }

        // Wings: a leading-edge bone and a fan of feathers. Each feather sits at its own height so they never z-fight.
        for (var i = 0; i < 2; i++)
        {
            var sd = i == 0 ? 1f : -1f;
            var wing = new GameObject("Wing").transform;
            wing.SetParent(model, false);
            wing.localPosition = new Vector3(0.28f * sd, 1.3f, 0.1f);
            Prim.Make(PrimitiveType.Sphere, "Shoulder", wing, Vector3.zero, Vector3.one * 0.28f, white);
            Prim.Make(PrimitiveType.Cylinder, "Bone", wing, new Vector3(sd * 0.6f, 0.04f, 0.05f), new Vector3(0.09f, 0.6f, 0.09f), white, new Vector3(0, 0, 90));

            feathers[i] = new Transform[FeatherLen.Length];
            for (var f = 0; f < FeatherLen.Length; f++)
            {
                var t = f / (FeatherLen.Length - 1f);
                var color = t < 0.5f ? Color.Lerp(white, pink, t * 2f) : Color.Lerp(pink, lilac, (t - 0.5f) * 2f);
                var pivot = new GameObject("Feather").transform;
                pivot.SetParent(wing, false);
                pivot.localPosition = new Vector3(sd * (0.1f + f * 0.04f), 0, -0.03f * f);
                var len = FeatherLen[f];
                Prim.Make(PrimitiveType.Sphere, "Vane", pivot, new Vector3(sd * len * 0.5f, f * 0.022f, 0), new Vector3(len, 0.045f, 0.34f - f * 0.02f), color);
                feathers[i][f] = pivot;
            }
            wings[i] = wing;
        }

        Pose(0f, 0f);
    }

    private void Update()
    {
        // A script recompile during Play wipes the cached references; rebuild the model instead of throwing every frame.
        if (model == null || legs[0] == null)
        {
            for (var c = transform.childCount - 1; c >= 0; c--) Destroy(transform.GetChild(c).gameObject);
            Build(transform.position, yaw);
            return;
        }

        var dt = Time.deltaTime;
        if (Cinematic)
        {
            if (CinTrail) RainbowBlast(Forward, 2, false);
            Pose(cinTurn, dt);
            UpdateSparks(dt);
            return;
        }
        var grounded = Grounded;

        var turn = 0f;
        var throttle = 0f;
        var strafe = 0f;
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

            // Gamepad: left stick walks and strafes, right stick turns.
            turn = Mathf.Clamp(turn + Pad.LookX, -1f, 1f);
            var padY = Pad.MoveY;
            if (padY > 0f) throttle = Mathf.Max(throttle, padY);
            else if (padY < 0f && throttle <= 0f) throttle = padY * 0.4f;
            strafe = Pad.MoveX;
            spaceDown |= Pad.JumpDown;
            spaceHeld |= Pad.JumpHeld;
            dive |= Pad.DiveHeld;
            dashPressed |= Pad.DashDown;
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
            targetSpeed = throttle * (grounded ? GroundSpeed : AirSpeed) * (WingBoost ? 1.35f : 1f);
            response = grounded ? 10f : 3f;
        }
        var wanted = fwd * targetSpeed;
        if (dashT <= 0f) wanted += Quaternion.Euler(0, 90f, 0) * fwd * (strafe * (grounded ? GroundSpeed : AirSpeed) * 0.7f);
        hv = Vector3.Lerp(hv, wanted, 1f - Mathf.Exp(-response * dt));

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
        if (WingBoost)
        {
            stamina = MaxStamina;
            if (Random.value < dt * 14f) Emit(transform.position + Vector3.up * 1.4f + Random.insideUnitSphere * 0.8f, gold: true, 1, 0.6f);
        }

        if (RainbowTrail)
        {
            RainbowBlast(fwd, 2, false);
            trailTimer -= dt;
            if (trailTimer <= 0f)
            {
                trailTimer = 0.06f;
                trail.Add(transform.position + Vector3.up * 1.2f);
                if (trail.Count > 12) trail.RemoveAt(0);
            }
        }
        else if (trail.Count > 0)
        {
            trail.Clear();
        }

        Pose(turn, dt);
        UpdateSparks(dt);
    }

    private void TryFlap()
    {
        if (!WingBoost)
        {
            if (stamina < FlapCost) return;
            stamina -= FlapCost;
        }
        vy = Mathf.Min(Mathf.Max(vy, 0f) + FlapSpeed * (WingBoost ? 1.25f : 1f), 10f);
        flapT = 0f;
        flapCool = WingBoost ? 0.25f : 0.4f;
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
        model.localRotation = Quaternion.Euler(pitch + Flip, 0, roll);

        var planar = new Vector2(hv.x, hv.z).magnitude;
        var stepBefore = Mathf.FloorToInt(walkCycle / Mathf.PI);
        walkCycle += (airborne ? 3f : 3f + planar * 1.6f) * dt;
        // A hoofbeat every half cycle while trotting; the pitch alternates and varies a little.
        if (!airborne && planar > 1.5f && Mathf.FloorToInt(walkCycle / Mathf.PI) != stepBefore)
        {
            Sfx.Hoof((stepBefore % 2 == 0 ? 1f : 0.85f) * Random.Range(0.95f, 1.05f));
        }
        var bob = airborne ? 0f : Mathf.Abs(Mathf.Sin(walkCycle)) * Mathf.Min(planar / GroundSpeed, 1f) * 0.1f;
        model.localPosition = new Vector3(0, bob, 0);
        if (Mathf.Abs(Flip) > 0.01f)
        {
            // Tumble around the middle of the body instead of the hooves.
            var pivot = new Vector3(0, 1.1f, 0.2f);
            model.localPosition += pivot - model.localRotation * pivot;
        }
        PoseFace();

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
            var lift = airborne ? 8f : 22f;
            var sway = Mathf.Sin(Time.time * 4f - i * 0.5f) * (4f + i * 2.5f);
            tail[i].localRotation = Quaternion.Euler(-lift + i * 3f, sway, 0);
        }
        for (var i = 0; i < mane.Length; i++)
        {
            var lift = airborne ? 10f : 0f;
            mane[i].localRotation = Quaternion.Euler(-lift * i / 5f, 0, Mathf.Sin(Time.time * 4f - i * 0.6f) * 6f);
        }

        PoseWings(airborne, dt);
    }

    // Surprise raises and arches the brows, widens the eyes and opens the mouth into an O.
    private void PoseFace()
    {
        if (brows[0] == null || mouth == null) return;
        var s = Surprise;
        for (var i = 0; i < 2; i++)
        {
            var sd = i == 0 ? 1f : -1f;
            brows[i].localPosition = new Vector3(0.18f * sd, 1.99f + s * 0.1f, 1.07f);
            brows[i].localRotation = Quaternion.Euler(0, 0, 90f - sd * s * 24f);
            eyes[i].localScale = EyeBase * (1f + s * 0.55f);
        }
        mouth.localScale = Vector3.Lerp(mouthBase, new Vector3(0.1f, 0.1f, 0.09f), s);
    }

    // Wings fold back along the body on the ground, beat on a flap (feather tips lag the bone) and stay open in a glide.
    private void PoseWings(bool airborne, float dt)
    {
        if (flapT < 1f) flapT += dt / 0.35f;
        float angle;
        if (!airborne) angle = 18f;
        else if (flapT < 1f) angle = 55f * Mathf.Cos(flapT * Mathf.PI * 2f) - 5f;
        else angle = 10f + Mathf.Sin(Time.time * 3f) * 6f;

        var previous = wingAngle;
        wingAngle = Mathf.Lerp(wingAngle, angle, 1f - Mathf.Exp(-(flapT < 1f ? 30f : 10f) * dt));
        wingFold = Mathf.Lerp(wingFold, airborne ? 0f : 1f, 1f - Mathf.Exp(-12f * dt));

        // Angular speed in degrees per second; the feather tips bend against the motion.
        var speed = dt > 0f ? (wingAngle - previous) / dt : 0f;
        var drag = Mathf.Clamp(-speed * 0.06f, -35f, 35f);

        for (var w = 0; w < 2; w++)
        {
            var sd = w == 0 ? 1f : -1f;
            wings[w].localRotation = Quaternion.Euler(0, sd * wingFold * 78f, 0) * Quaternion.Euler(0, 0, sd * wingAngle);
            wings[w].localScale = new Vector3(Mathf.Lerp(1f, 0.55f, wingFold), 1f, 1f);
            for (var f = 0; f < feathers[w].Length; f++)
            {
                var fan = FeatherFan[f] * (1f - 0.7f * wingFold);
                var bend = drag * (f + 1f) / feathers[w].Length;
                feathers[w][f].localRotation = Quaternion.Euler(0, 0, sd * bend) * Quaternion.Euler(0, sd * fan, 0);
            }
        }
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
