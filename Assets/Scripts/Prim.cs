using System.Collections.Generic;
using UnityEngine;

// Helpers for building everything out of Unity primitives, so the game needs no imported assets.
public static class Prim
{
    private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
    private static Mesh coneMesh;

    public static Material Mat(Color color)
    {
        if (Materials.TryGetValue(color, out var m)) return m;
        var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        m = new Material(shader) { color = color };
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.25f);
        Materials[color] = m;
        return m;
    }

    // Spheres, cubes and cylinders keep their usual proportions (a cylinder is 2 tall, radius 0.5).
    public static Transform Make(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(color);
        return go.transform;
    }

    // Cone with a 1 x 1 x 1 bounding box, pivot in the middle, tip pointing +Y.
    public static Transform Cone(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, Vector3 euler = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
        go.AddComponent<MeshRenderer>().sharedMaterial = Mat(color);
        return go.transform;
    }

    private static Mesh ConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        const int seg = 24;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var tip = new Vector3(0, 0.5f, 0);
        for (var i = 0; i < seg; i++)
        {
            var a0 = i / (float)seg * Mathf.PI * 2f;
            var a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
            var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
            var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);
            var b = verts.Count;
            verts.AddRange(new[] { tip, p1, p0 });
            tris.AddRange(new[] { b, b + 1, b + 2 });
            b = verts.Count;
            verts.AddRange(new[] { new Vector3(0, -0.5f, 0), p0, p1 });
            tris.AddRange(new[] { b, b + 1, b + 2 });
        }
        coneMesh = new Mesh { name = "Cone" };
        coneMesh.SetVertices(verts);
        coneMesh.SetTriangles(tris, 0);
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
        return coneMesh;
    }
}
