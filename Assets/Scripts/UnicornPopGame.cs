using System.Collections.Generic;
using UnityEngine;

// Batch 1: camera, sky, castle, cannon and a jester. No gameplay yet.
public sealed class UnicornPopGame : MonoBehaviour
{
    public const float WalkwayY = -1.6f;

    private Camera cam;
    private Transform jester;
    private Transform cannonBarrel;
    private readonly List<Transform> clouds = new List<Transform>();
    private float lastAspect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<UnicornPopGame>() == null)
        {
            new GameObject("UnicornPop").AddComponent<UnicornPopGame>();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Art.Init();
        BuildCamera();
        BuildWorld();
    }

    private void BuildCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.8f, 1f);
        FitCamera();
    }

    private void FitCamera()
    {
        lastAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        cam.orthographicSize = Mathf.Max(5f, 5.4f / lastAspect);
    }

    private void BuildWorld()
    {
        var world = new GameObject("World").transform;
        world.SetParent(transform, false);

        Art.Spr("SkyHigh", world, Art.Square, new Vector2(0, 8), new Vector2(80, 14), new Color(0.42f, 0.7f, 1f), -100);
        Art.Spr("SkyLow", world, Art.Square, new Vector2(0, -2), new Vector2(80, 8), new Color(0.78f, 0.92f, 1f), -99);
        Art.Spr("Sun", world, Art.Circle, new Vector2(-5.5f, 3.4f), new Vector2(1.8f, 1.8f), new Color(1f, 0.95f, 0.6f), -98);

        for (var i = 0; i < 6; i++)
        {
            var c = new GameObject("Cloud").transform;
            c.SetParent(world, false);
            c.position = new Vector3(Random.Range(-9f, 9f), Random.Range(0.5f, 4.2f), 0);
            var s = Random.Range(0.8f, 1.4f);
            for (var k = 0; k < 3; k++)
            {
                Art.Spr("Puff", c, Art.Circle, new Vector2((k - 1) * 0.7f * s, k == 1 ? 0.25f * s : 0f), new Vector2(1.1f * s, 0.8f * s), new Color(1f, 1f, 1f, 0.9f), -90);
            }
            clouds.Add(c);
        }

        Art.Spr("Hill1", world, Art.Circle, new Vector2(-6f, -5.2f), new Vector2(11f, 7f), new Color(0.55f, 0.82f, 0.55f), -50);
        Art.Spr("Hill2", world, Art.Circle, new Vector2(6.5f, -5.6f), new Vector2(13f, 8f), new Color(0.45f, 0.75f, 0.5f), -49);

        BuildCastle(world);
        BuildCannon(world);
        BuildJester(world);
    }

    private static void BuildCastle(Transform world)
    {
        var stone = new Color(0.78f, 0.74f, 0.85f);
        var dark = new Color(0.6f, 0.56f, 0.7f);
        var door = new Color(0.45f, 0.3f, 0.35f);

        Art.Spr("Wall", world, Art.Square, new Vector2(0, WalkwayY - 15f), new Vector2(9.6f, 30f), stone, 0);
        Art.Spr("Walkway", world, Art.Square, new Vector2(0, WalkwayY - 0.12f), new Vector2(9.6f, 0.24f), dark, 2);
        for (var i = 0; i < 5; i++)
        {
            Art.Spr("Brick", world, Art.Square, new Vector2(-3.8f + i * 1.9f, WalkwayY - 1.4f), new Vector2(1.5f, 0.5f), dark, 1);
            Art.Spr("Brick", world, Art.Square, new Vector2(-2.85f + i * 1.9f, WalkwayY - 2.4f), new Vector2(1.5f, 0.5f), dark, 1);
        }
        Art.Spr("DoorTop", world, Art.Circle, new Vector2(0, WalkwayY - 3.4f), new Vector2(1.6f, 1.6f), door, 1);
        Art.Spr("DoorBase", world, Art.Square, new Vector2(0, WalkwayY - 4.2f), new Vector2(1.6f, 1.6f), door, 1);

        // Crenellations along the walkway edges.
        for (var i = 0; i < 4; i++)
        {
            Art.Spr("Merlon", world, Art.Square, new Vector2(-4.5f + i * 0.45f, WalkwayY + 0.2f), new Vector2(0.3f, 0.4f), dark, 3);
            Art.Spr("Merlon", world, Art.Square, new Vector2(3.15f + i * 0.45f, WalkwayY + 0.2f), new Vector2(0.3f, 0.4f), dark, 3);
        }

        // A flag on the left end.
        Art.Spr("Pole", world, Art.Square, new Vector2(-4.4f, WalkwayY + 1.1f), new Vector2(0.08f, 2f), new Color(0.35f, 0.3f, 0.3f), 3);
        Art.Spr("Flag", world, Art.Triangle, new Vector2(-4.1f, WalkwayY + 1.8f), new Vector2(0.6f, 0.6f), new Color(0.95f, 0.4f, 0.7f), 3, -90f);
    }

    private void BuildCannon(Transform world)
    {
        var root = new GameObject("Cannon").transform;
        root.SetParent(world, false);
        root.position = new Vector3(3.6f, WalkwayY, 0);
        Art.Spr("Wheel", root, Art.Circle, new Vector2(0, 0.35f), new Vector2(0.7f, 0.7f), new Color(0.35f, 0.22f, 0.15f), 6);
        Art.Spr("Hub", root, Art.Circle, new Vector2(0, 0.35f), new Vector2(0.22f, 0.22f), new Color(0.85f, 0.7f, 0.3f), 8);

        var pivot = new GameObject("BarrelPivot").transform;
        pivot.SetParent(root, false);
        pivot.localPosition = new Vector2(0, 0.4f);
        pivot.localRotation = Quaternion.Euler(0, 0, 8f);
        cannonBarrel = pivot;
        Art.Spr("Barrel", pivot, Art.Square, new Vector2(0, 0.45f), new Vector2(0.42f, 1.0f), new Color(0.25f, 0.27f, 0.33f), 7);
        Art.Spr("Muzzle", pivot, Art.Square, new Vector2(0, 0.98f), new Vector2(0.54f, 0.16f), new Color(0.18f, 0.19f, 0.24f), 8);
    }

    private void BuildJester(Transform world)
    {
        jester = new GameObject("Jester").transform;
        jester.SetParent(world, false);
        jester.position = new Vector3(2.5f, WalkwayY, 0);

        var skin = new Color(1f, 0.85f, 0.72f);
        var purple = new Color(0.6f, 0.3f, 0.8f);
        var gold = new Color(1f, 0.8f, 0.2f);

        Art.Spr("LegL", jester, Art.Square, new Vector2(-0.1f, 0.2f), new Vector2(0.14f, 0.4f), purple, 10);
        Art.Spr("LegR", jester, Art.Square, new Vector2(0.1f, 0.2f), new Vector2(0.14f, 0.4f), gold, 10);
        Art.Spr("Body", jester, Art.Square, new Vector2(0, 0.62f), new Vector2(0.42f, 0.5f), purple, 11);
        Art.Spr("Belt", jester, Art.Square, new Vector2(0, 0.5f), new Vector2(0.42f, 0.1f), gold, 12);
        Art.Spr("Head", jester, Art.Circle, new Vector2(0, 1.07f), new Vector2(0.45f, 0.45f), skin, 12);
        Art.Spr("EyeL", jester, Art.Circle, new Vector2(-0.08f, 1.1f), new Vector2(0.07f, 0.07f), Color.black, 13);
        Art.Spr("EyeR", jester, Art.Circle, new Vector2(0.08f, 1.1f), new Vector2(0.07f, 0.07f), Color.black, 13);
        Art.Spr("Smile", jester, Art.Circle, new Vector2(0, 1.0f), new Vector2(0.2f, 0.1f), new Color(0.8f, 0.2f, 0.3f), 13);

        // Two-pronged hat with bells.
        Art.Spr("HatL", jester, Art.Triangle, new Vector2(-0.22f, 1.42f), new Vector2(0.3f, 0.5f), purple, 11, 35f);
        Art.Spr("HatR", jester, Art.Triangle, new Vector2(0.22f, 1.42f), new Vector2(0.3f, 0.5f), gold, 11, -35f);
        Art.Spr("BellL", jester, Art.Circle, new Vector2(-0.4f, 1.6f), new Vector2(0.12f, 0.12f), gold, 14);
        Art.Spr("BellR", jester, Art.Circle, new Vector2(0.4f, 1.6f), new Vector2(0.12f, 0.12f), purple, 14);
    }

    private void Update()
    {
        if (Mathf.Abs((float)Screen.width / Mathf.Max(1, Screen.height) - lastAspect) > 0.001f)
        {
            FitCamera();
        }

        foreach (var c in clouds)
        {
            var p = c.position;
            p.x += Time.deltaTime * 0.15f;
            if (p.x > 11f) p.x = -11f;
            c.position = p;
        }

        // Idle jester hop and a slight cannon wobble so the scene feels alive.
        jester.position = new Vector3(2.5f, WalkwayY + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.12f, 0);
        cannonBarrel.localRotation = Quaternion.Euler(0, 0, 8f + Mathf.Sin(Time.time * 1.5f) * 2f);
    }
}
