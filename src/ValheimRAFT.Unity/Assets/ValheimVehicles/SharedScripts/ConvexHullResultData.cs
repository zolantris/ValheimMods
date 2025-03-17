// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using Unity.Collections;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
  public struct ConvexHullResultData
  {
    public NativeArray<Vector3> Vertices;
    public NativeArray<int> Triangles;
    public NativeArray<Vector3> Normals;

    public ConvexHullResultData(Vector3[] verts, int[] tris, Vector3[] normals, Allocator allocator)
    {
      Vertices = new NativeArray<Vector3>(verts, allocator);
      Triangles = new NativeArray<int>(tris, allocator);
      Normals = new NativeArray<Vector3>(normals, allocator);
    }

    public void Dispose()
    {
      if (Vertices.IsCreated) Vertices.Dispose();
      if (Triangles.IsCreated) Triangles.Dispose();
      if (Normals.IsCreated) Normals.Dispose();
    }
  }
}