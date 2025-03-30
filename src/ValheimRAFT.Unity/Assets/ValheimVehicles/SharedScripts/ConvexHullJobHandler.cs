// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{

  public class ConvexHullJobHandler : MonoBehaviour
  {
    private JobData _currentJobData;
    private Coroutine _jobMonitorCoroutine;

    private void OnDestroy()
    {
      if (_jobMonitorCoroutine != null)
        StopCoroutine(_jobMonitorCoroutine);

      if (_currentJobData.Handle.IsCompleted == false)
        _currentJobData.Handle.Complete();

      _currentJobData.Dispose();
    }

    public void ScheduleConvexHullJob(
      ConvexHullAPI convexHullAPI,
      PrefabPieceData[] prefabDataList,
      float clusterThreshold,
      Action onComplete = null)
    {
      if (_jobMonitorCoroutine != null)
      {
        Debug.LogWarning("A convex hull job is already running.");
        return;
      }

      if (prefabDataList.Length == 0) return;

      // if (ActiveJobs.ContainsKey(convexHullAPI))
      // {
      //   Debug.LogWarning($"⚠️ ConvexHullJob is already running for {convexHullAPI}. Skipping duplicate request.");
      //   return;
      // }

      var colliderDataList = new List<PrefabColliderPointData>(prefabDataList.Length);
      foreach (var prefabData in prefabDataList)
      {
        // colliderDataList.Add(prefabData.PointDataItems);
      }

      NativeArray<PrefabColliderPointData> nativeColliderData = new(colliderDataList.ToArray(), Allocator.TempJob);
      NativeArray<int> validClusterCountArray = new(1, Allocator.Persistent);
      NativeArray<ConvexHullResultData> nativeHullResults = new(prefabDataList.Length, Allocator.TempJob);

      // for (var i = 0; i < totalColliderCount; i++)
      //   colliderDataArray[i] = prefabPieceDataList[i].PrefabColliderPointData;

      var validClusterCount = new NativeArray<int>(1, Allocator.TempJob);
      var outputVertices = new NativeArray<Vector3>(10000, Allocator.TempJob);
      var outputTriangles = new NativeArray<int>(10000, Allocator.TempJob);
      var outputNormals = new NativeArray<Vector3>(10000, Allocator.TempJob);

      var job = new ConvexHullJob
      {
        // InputColliderData = colliderDataArray,
        ClusterThreshold = clusterThreshold,
        ValidClusterCount = validClusterCount,
        OutputVertices = outputVertices,
        OutputTriangles = outputTriangles,
        OutputNormals = outputNormals
      };

      var handle = job.Schedule();

      _currentJobData = new JobData
      {
        Handle = handle,
        ValidClusterCount = validClusterCount,
        OutputVertices = outputVertices,
        OutputTriangles = outputTriangles,
        OutputNormals = outputNormals,
        // ColliderDataArray = colliderDataArray,
        OnComplete = () =>
        {
          // var clusters = validClusterCount[0];

          // for (int i = 0, vOffset = 0, tOffset = 0, nOffset = 0; i < clusters; i++)
          // {
          //   // var vertexCount = prefabPieceDataList[i].PrefabColliderPointData.Points.Length;
          //
          // //   convexHullAPI.GenerateMeshFromConvexOutput(
          // //     outputVertices.GetSubArray(vOffset, vertexCount).ToArray(),
          // //     outputTriangles.GetSubArray(tOffset, vertexCount).ToArray(),
          // //     outputNormals.GetSubArray(nOffset, vertexCount).ToArray(),
          // //     i
          // //   );
          // //
          // //   vOffset += vertexCount;
          // //   tOffset += vertexCount;
          // //   nOffset += vertexCount;
          // }

          // onComplete?.Invoke();

          // Dispose after completion
          _currentJobData.Dispose();
        }
      };

      _jobMonitorCoroutine = StartCoroutine(JobMonitorRoutine());
    }

    private IEnumerator JobMonitorRoutine()
    {
      while (!_currentJobData.Handle.IsCompleted)
        yield return null;

      _currentJobData.Handle.Complete();

      _currentJobData.OnComplete?.Invoke();
      _jobMonitorCoroutine = null;
    }
  }
}