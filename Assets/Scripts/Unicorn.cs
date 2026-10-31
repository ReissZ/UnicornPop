using UnityEngine;

// Batch 2: the unicorn. Runs along the castle walkway, jumps and double jumps.
public sealed class Unicorn : MonoBehaviour
{
    private const float MoveSpeed = 4.5f;
    private const float JumpSpeed = 8.5f;
    private const float Gravity = -24f;
    private const float MinX = -4.2f;
    private const float MaxX = 1.8f;

    private Camera cam;
    private Transform body;
    private Transform[] legs;
    private Transform[] tail;
    private float x = -1f;
    private float height;
    private float vy;
    private int jumpsUsed;
    private int facing = 1;
    private float walkCycle;
    private bool walking;

    // Pointer state: hold to run toward the pointer, quick tap to jump.
    private bool pressing;
    private float pressTime;
    private float pressStartX;
    private float moveTarget = float.NaN;

    public Transform HornTip { get; private set; }

    public void Build(Camera camera)
    {
        cam = camera;
        var white = new Color(0.98f, 0.96f, 1f);
        var pink = new Color(1f, 0.55f, 0.8f);
        var purple = new Color(0.7f, 0.5f, 0.95f);
        var gold = new Color(1f, 0.85f, 0.3f);
        var hoof = new Color(0.8f, 0.7f, 0.9f);

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);

        legs = new Transform[4];
        var legX = new[] { -0.3f, -0.15f, 0.2f, 0.35f };
        for (var i = 0; i < 4; i++)
        {
            // Pivot at the hip so rotating swings the leg.
            var pivot = new GameObject("Leg").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector2(legX[i], 0.05f);
            Art.Spr("Shin", pivot, Art.Square, new Vector2(0, -0.2f), new Vector2(0.12f, 0.45f), i % 2 == 0 ? white : new Color(0.88f, 0.86f, 0.95f), 20);
            Art.Spr("Hoof", pivot, Art.Square, new Vector2(0, -0.42f), new Vector2(0.13f, 0.1f), hoof, 21);
            legs[i] = pivot;
        }

        Art.Spr("Torso", body, Art.Circle, new Vector2(0, 0.15f), new Vector2(1.1f, 0.6f), white, 22);
        Art.Spr("Neck", body, Art.Square, new Vector2(0.4f, 0.45f), new Vector2(0.28f, 0.55f), white, 22, -20f);
        Art.Spr("Head", body, Art.Circle, new Vector2(0.55f, 0.75f), new Vector2(0.5f, 0.42f), white, 24);
        Art.Spr("Snout", body, Art.Circle, new Vector2(0.78f, 0.68f), new Vector2(0.26f, 0.22f), new Color(1f, 0.9f, 0.95f), 25);
        Art.Spr("Nostril", body, Art.Circle, new Vector2(0.86f, 0.68f), new Vector2(0.04f, 0.04f), pink, 26);
        Art.Spr("Eye", body, Art.Circle, new Vector2(0.62f, 0.8f), new Vector2(0.09f, 0.11f), new Color(0.2f, 0.1f, 0.3f), 26);
        Art.Spr("EyeShine", body, Art.Circle, new Vector2(0.64f, 0.83f), new Vector2(0.03f, 0.03f), Color.white, 27);
        Art.Spr("Ear", body, Art.Triangle, new Vector2(0.43f, 0.98f), new Vector2(0.14f, 0.22f), pink, 23, 15f);
        Art.Spr("Cheek", body, Art.Circle, new Vector2(0.66f, 0.7f), new Vector2(0.1f, 0.07f), new Color(1f, 0.7f, 0.8f), 26);

        // Golden spiral-ish horn pointing up and forward.
        var horn = new GameObject("Horn").transform;
        horn.SetParent(body, false);
        horn.localPosition = new Vector2(0.62f, 0.94f);
        horn.localRotation = Quaternion.Euler(0, 0, -25f);
        Art.Spr("HornShape", horn, Art.Triangle, new Vector2(0, 0.28f), new Vector2(0.17f, 0.6f), gold, 25);
        for (var i = 0; i < 3; i++)
        {
            Art.Spr("Stripe", horn, Art.Square, new Vector2(0, 0.14f + i * 0.14f), new Vector2(0.12f - i * 0.03f, 0.03f), new Color(0.9f, 0.6f, 0.1f), 26, 15f);
        }
        HornTip = new GameObject("HornTip").transform;
        HornTip.SetParent(horn, false);
        HornTip.localPosition = new Vector2(0, 0.58f);

        // Rainbow mane down the neck.
        var mane = new[] { pink, purple, new Color(0.5f, 0.8f, 1f), new Color(0.6f, 0.95f, 0.7f) };
        for (var i = 0; i < mane.Length; i++)
        {
            Art.Spr("Mane", body, Art.Circle, new Vector2(0.28f - i * 0.04f, 0.9f - i * 0.17f), new Vector2(0.3f, 0.24f), mane[i], 21);
        }

        // Rainbow tail, animated in Pose.
        tail = new Transform[4];
        for (var i = 0; i < tail.Length; i++)
        {
            var piv = new GameObject("Tail").transform;
            piv.SetParent(body, false);
            piv.localPosition = new Vector2(-0.5f, 0.2f);
            Art.Spr("TailPiece", piv, Art.Circle, new Vector2(-0.25f, 0), new Vector2(0.5f - i * 0.05f, 0.14f), mane[i], 19 - i);
            tail[i] = piv;
        }

        Pose();
    }

    private void Update()
    {
        if (cam == null) return;
        var dt = Time.deltaTime;

        var dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;
        var jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);

        var pointerX = cam.ScreenToWorldPoint(Input.mousePosition).x;
        if (Input.GetMouseButtonDown(0))
        {
            pressing = true;
            pressTime = 0f;
            pressStartX = pointerX;
        }
        if (pressing)
        {
            pressTime += dt;
            if (Input.GetMouseButton(0) && (pressTime > 0.12f || Mathf.Abs(pointerX - pressStartX) > 0.4f))
            {
                moveTarget = pointerX;
            }
            if (Input.GetMouseButtonUp(0))
            {
                pressing = false;
                if (pressTime < 0.18f && Mathf.Abs(pointerX - pressStartX) < 0.4f) jump = true;
                moveTarget = float.NaN;
            }
        }
        if (dir == 0f && !float.IsNaN(moveTarget))
        {
            var delta = moveTarget - x;
            if (Mathf.Abs(delta) > 0.15f) dir = Mathf.Sign(delta);
        }

        if (dir != 0f) facing = dir > 0f ? 1 : -1;
        x = Mathf.Clamp(x + dir * MoveSpeed * dt, MinX, MaxX);
        walking = dir != 0f;
        walkCycle += (walking && height <= 0.001f ? 14f : 4f) * dt;

        if (jump && jumpsUsed < 2)
        {
            vy = JumpSpeed * (jumpsUsed == 0 ? 1f : 0.85f);
            jumpsUsed++;
        }

        vy += Gravity * dt;
        height += vy * dt;
        if (height <= 0f)
        {
            height = 0f;
            vy = 0f;
            jumpsUsed = 0;
        }

        Pose();
    }

    private void Pose()
    {
        transform.position = new Vector3(x, UnicornPopGame.WalkwayY + 0.5f + height, 0);
        var sx = Mathf.Lerp(body.localScale.x, facing, 0.35f);
        var airborne = height > 0.001f;
        var bob = airborne ? 0f : Mathf.Abs(Mathf.Sin(walkCycle)) * (walking ? 0.07f : 0.02f);
        body.localScale = new Vector3(sx, 1f, 1f);
        body.localPosition = new Vector3(0, bob, 0);
        var tilt = airborne ? Mathf.Clamp(vy * -0.6f, -12f, 12f) : 0f;
        body.localRotation = Quaternion.Euler(0, 0, tilt * Mathf.Sign(sx) * 0.5f);

        for (var i = 0; i < legs.Length; i++)
        {
            var phase = i % 2 == 0 ? 0f : Mathf.PI;
            var swing = airborne ? (i < 2 ? -35f : 35f) : Mathf.Sin(walkCycle + phase) * (walking ? 32f : 0f);
            legs[i].localRotation = Quaternion.Euler(0, 0, swing);
        }
        for (var i = 0; i < tail.Length; i++)
        {
            tail[i].localRotation = Quaternion.Euler(0, 0, 20f - i * 8f + Mathf.Sin(Time.time * 6f + i * 0.5f) * 8f);
        }
    }
}
