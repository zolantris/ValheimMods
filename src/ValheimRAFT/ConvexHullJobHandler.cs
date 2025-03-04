// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
    using System;
    using System.Collections.Generic;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    public static class ConvexHullJobHandler
    {
        private static readonly HashSet<ConvexHullAPI> ActiveJobs = new();
        private static readonly Dictionary<JobHandle, Action> JobCompletionCallbacks = new(); // ✅ Track job handles & their callbacks

        /// <summary>
        /// Schedules a job to process `PrefabPieceData`, generate convex hulls, and execute a callback when complete.
        /// </summary>
        public static void ScheduleConvexHullJob(
            ConvexHullAPI convexHullAPI,
            List<PrefabPieceData> prefabDataList,
            float clusterThreshold,
            Action onComplete = null)
        {
            if (convexHullAPI == null)
            {
                Debug.LogError("❌ ConvexHullAPI reference must be provided.");
                return;
            }

            if (prefabDataList.Count == 0) return;

            if (ActiveJobs.Contains(convexHullAPI))
            {
                Debug.LogWarning($"⚠️ ConvexHullJob is already running for {convexHullAPI}. Skipping duplicate request.");
                return;
            }

            ActiveJobs.Add(convexHullAPI);

            // ✅ Extract `PrefabColliderPointData` from `PrefabPieceData`
            var colliderDataList = new List<PrefabColliderPointData>();
            foreach (var prefabData in prefabDataList)
            {
                colliderDataList.Add(prefabData.PointDataItems);
            }

            NativeArray<PrefabColliderPointData> nativeColliderData = new(colliderDataList.ToArray(), Allocator.TempJob);
            NativeArray<ConvexHullResultData> nativeHullResults = new(colliderDataList.Count, Allocator.TempJob);

            ConvexHullJob job = new()
            {
                InputColliderData = nativeColliderData,
                ClusterThreshold = clusterThreshold,
                OutputHullData = nativeHullResults
            };

            var jobHandle = job.Schedule(); // ✅ Schedule job asynchronously (DO NOT CALL `.Complete()`)

            // ✅ Store the job and its callback
            JobCompletionCallbacks[jobHandle] = () =>
            {
                for (var i = 0; i < colliderDataList.Count; i++)
                {
                    var result = nativeHullResults[i];

                    convexHullAPI.GenerateMeshFromConvexOutput(
                        result.Vertices.ToArray(),
                        result.Triangles.ToArray(),
                        result.Normals.ToArray(),
                        i
                    );

                    result.Dispose();
                }

                nativeColliderData.Dispose();
                nativeHullResults.Dispose();
                ActiveJobs.Remove(convexHullAPI);

                // ✅ Invoke the callback after the job completes
                onComplete?.Invoke();
            };
        }

        /// <summary>
        /// ✅ This must be called from a `MonoBehaviour` (e.g., `VehicleManager`) in `LateUpdate()`.
        /// </summary>
        public static void ProcessCompletedJobs()
        {
            var completedJobs = new List<JobHandle>();

            foreach (var job in JobCompletionCallbacks.Keys)
            {
                if (job.IsCompleted)
                {
                    job.Complete(); // ✅ Now we safely complete without blocking the main thread
                    JobCompletionCallbacks[job]?.Invoke();
                    completedJobs.Add(job);
                }
            }

            // ✅ Remove completed jobs from the tracking dictionary
            foreach (var completedJob in completedJobs)
            {
                JobCompletionCallbacks.Remove(completedJob);
            }
        }

        /// <summary>
        /// Job for processing `PrefabColliderPointData`, clustering points, and generating convex hulls.
        /// </summary>
        private struct ConvexHullJob : IJob
        {
            [ReadOnly] public NativeArray<PrefabColliderPointData> InputColliderData;
            public float ClusterThreshold;
            public NativeArray<ConvexHullResultData> OutputHullData;

            public void Execute()
            {
                var calculator = new ConvexHullCalculator();
                var clusteredPoints = ClusterMeshData();

                for (var clusterIndex = 0; clusterIndex < clusteredPoints.Count; clusterIndex++)
                {
                    var verts = new List<Vector3>();
                    var normals = new List<Vector3>();
                    var triangles = new List<int>();

                    calculator.GenerateHull(clusteredPoints[clusterIndex], false, ref verts, ref triangles, ref normals);

                    OutputHullData[clusterIndex] = new ConvexHullResultData(verts.ToArray(), triangles.ToArray(), normals.ToArray(), Allocator.TempJob);
                }
            }

            /// <summary>
            /// Clusters collider points into groups for convex hull generation based on `ClusterThreshold`.
            /// </summary>
            private List<List<Vector3>> ClusterMeshData()
            {
                List<List<Vector3>> clusters = new();
                HashSet<int> processed = new();

                for (var i = 0; i < InputColliderData.Length; i++)
                {
                    if (processed.Contains(i)) continue;

                    List<Vector3> cluster = new(InputColliderData[i].Points.ToArray());
                    processed.Add(i);

                    for (var j = i + 1; j < InputColliderData.Length; j++)
                    {
                        if (processed.Contains(j)) continue;

                        if (Vector3.Distance(InputColliderData[i].LocalPosition, InputColliderData[j].LocalPosition) < ClusterThreshold)
                        {
                            cluster.AddRange(InputColliderData[j].Points.ToArray());
                            processed.Add(j);
                        }
                    }

                    clusters.Add(cluster);
                }

                return clusters;
            }
        }
    }
}
