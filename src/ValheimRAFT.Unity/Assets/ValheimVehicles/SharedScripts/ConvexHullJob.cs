#region

using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  // Job for convex hull computation
  public struct ConvexHullJob : IJob
  {
    [ReadOnly] public NativeArray<PrefabColliderPointData> InputColliderData;
    public float ClusterDistanceThreshold;

    public NativeArray<int> ValidClusterCount;
    public NativeArray<Vector3> OutputVertices;
    public NativeArray<int> OutputTriangles;
    public NativeArray<Vector3> OutputNormals;

    public void Execute()
    {
      var calculator = new ConvexHullCalculator();
      var validClusters = 0;

      var vertexOffset = 0;
      var triangleOffset = 0;
      var normalOffset = 0;

      for (var i = 0; i < InputColliderData.Length; i++)
      {
        var data = InputColliderData[i];

        var verts = new List<Vector3>(data.Points);
        var tris = new List<int>();
        var normals = new List<Vector3>();

        calculator.GenerateHull(verts, false, ref verts, ref tris, ref normals);

        if (verts.Count == 0 || tris.Count == 0)
          continue;

        NativeArray<Vector3>.Copy(verts.ToArray(), 0, OutputVertices, vertexOffset, verts.Count);
        NativeArray<int>.Copy(tris.ToArray(), 0, OutputTriangles, triangleOffset, tris.Count);
        NativeArray<Vector3>.Copy(normals.ToArray(), 0, OutputNormals, normalOffset, normals.Count);

        vertexOffset += verts.Count;
        triangleOffset += tris.Count;
        normalOffset += normals.Count;

        validClusters++;
      }

      ValidClusterCount[0] = validClusters;
    }
  }
}