// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
// Struct to track job-related native arrays and cleanup
  public struct JobData
  {
    public JobHandle Handle;
    public NativeArray<int> ValidClusterCount;
    public NativeArray<Vector3> OutputVertices;
    public NativeArray<int> OutputTriangles;
    public NativeArray<Vector3> OutputNormals;
    public NativeArray<PrefabColliderPointData> ColliderDataArray;
    public Action OnComplete;

    public void Dispose()
    {
      if (ValidClusterCount.IsCreated) ValidClusterCount.Dispose();
      if (OutputVertices.IsCreated) OutputVertices.Dispose();
      if (OutputTriangles.IsCreated) OutputTriangles.Dispose();
      if (OutputNormals.IsCreated) OutputNormals.Dispose();
      if (ColliderDataArray.IsCreated) ColliderDataArray.Dispose();
    }
  }
}