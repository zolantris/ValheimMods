using System.Collections.Generic;
using UnityEngine;

namespace ValheimVehicles.Helpers;

public class MeshUtils
{
  private static List<Vector3>? vertices;

  private static List<Vector3>? normals;

  private static List<Vector2>? uv;

  private static List<int>? indices;

  private static Dictionary<uint, int>? newVertices;

  private static int GetNewVertex(int i1, int i2)
  {
    if (newVertices == null || vertices == null) return 0;

    var t1 = (uint)(i1 << 16 | i2);
    var t2 = (uint)(i2 << 16 | i1);
    if (newVertices.TryGetValue(t2, out var newVertex)) return newVertex;
    if (newVertices.TryGetValue(t1, out var vertex)) return vertex;
    var newIndex = vertices.Count;
    newVertices.Add(t1, newIndex);
    vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
    if (normals != null) normals.Add((normals[i1] + normals[i2]).normalized);
    if (uv != null) uv.Add((uv[i1] + uv[i2]) * 0.5f);
    return newIndex;
  }

  public static void Subdivide(Mesh mesh)
  {
    if (mesh == null) return;
    newVertices = new Dictionary<uint, int>();
    vertices = new List<Vector3>(mesh.vertices);
    normals =
      mesh.normals != null && mesh.normals.Length == mesh.vertices.Length
        ? new List<Vector3>(mesh.normals)
        : null;
    uv = mesh.uv != null && mesh.uv.Length == mesh.vertices.Length
      ? new List<Vector2>(mesh.uv)
      : null;
    indices = new List<int>();
    var triangles = mesh.triangles;
    for (var i = 0; i < triangles.Length; i += 3)
    {
      var i2 = triangles[i];
      var i3 = triangles[i + 1];
      var i4 = triangles[i + 2];
      var a = GetNewVertex(i2, i3);
      var b = GetNewVertex(i3, i4);
      var c = GetNewVertex(i4, i2);
      indices.Add(i2);
      indices.Add(a);
      indices.Add(c);
      indices.Add(i3);
      indices.Add(b);
      indices.Add(a);
      indices.Add(i4);
      indices.Add(c);
      indices.Add(b);
      indices.Add(a);
      indices.Add(b);
      indices.Add(c);
    }

    mesh.vertices = vertices.ToArray();
    if (normals != null) mesh.normals = normals.ToArray();
    if (uv != null) mesh.uv = uv.ToArray();
    mesh.triangles = indices.ToArray();
    newVertices = null;
    vertices = null;
    normals = null;
    uv = null;
    indices = null;
  }
  
  public float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var v321 = p3.x * p2.y * p1.z;
    var v231 = p2.x * p3.y * p1.z;
    var v312 = p3.x * p1.y * p2.z;
    var v132 = p1.x * p3.y * p2.z;
    var v213 = p2.x * p1.y * p3.z;
    var v123 = p1.x * p2.y * p3.z;

    return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
  }

  public float VolumeOfMesh(Mesh mesh)
  {
    float volume = 0;

    var localVertices = mesh.vertices;
    var triangles = mesh.triangles;

    for (var i = 0; i < triangles.Length; i += 3)
    {
      var p1 = localVertices[triangles[i + 0]];
      var p2 = localVertices[triangles[i + 1]];
      var p3 = localVertices[triangles[i + 2]];
      volume += SignedVolumeOfTriangle(p1, p2, p3);
    }
    return Mathf.Abs(volume);
  }
}