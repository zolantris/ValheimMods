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
        private static readonly HashSet<ConvexHullAPI> ActiveJobs = new(); // ✅ Tracks running jobs

        /// <summary>
        /// Schedules a job to process `PrefabColliderPointData`, generate convex hulls, and return results.
        /// </summary>
        public static void ScheduleConvexHullJob(
            ConvexHullAPI convexHullAPI,
            List<PrefabColliderPointData> colliderDataList,
            float clusterThreshold)
        {
            if (convexHullAPI == null)
            {
                Debug.LogError("❌ ConvexHullAPI reference must be provided.");
                return;
            }

            if (colliderDataList.Count == 0) return;

            // ✅ Prevent duplicate jobs for the same `ConvexHullAPI` instance
            if (ActiveJobs.Contains(convexHullAPI))
            {
                Debug.LogWarning($"⚠️ ConvexHullJob is already running for {convexHullAPI}. Skipping duplicate request.");
                return;
            }

            ActiveJobs.Add(convexHullAPI); // ✅ Mark job as running

            // ✅ Convert PrefabColliderPointData into a NativeArray for job processing
            NativeArray<PrefabColliderPointData> nativeColliderData = new(colliderDataList.ToArray(), Allocator.TempJob);
            NativeArray<ConvexHullResultData> nativeHullResults = new(colliderDataList.Count, Allocator.TempJob);

            ConvexHullJob job = new()
            {
                InputColliderData = nativeColliderData,
                ClusterThreshold = clusterThreshold,
                OutputHullData = nativeHullResults
            };

            var jobHandle = job.Schedule();
            jobHandle.Complete(); // ✅ Wait for job to complete (can be optimized later)

            // ✅ Convert results to managed arrays and call the main thread API
            for (var i = 0; i < colliderDataList.Count; i++)
            {
                var result = nativeHullResults[i];

                convexHullAPI.GenerateMeshFromConvexOutput(
                    result.Vertices.ToArray(),
                    result.Triangles.ToArray(),
                    result.Normals.ToArray(),
                    i
                );

                // ✅ Dispose after copying to managed memory
                result.Dispose();
            }

            nativeColliderData.Dispose();
            nativeHullResults.Dispose();
            ActiveJobs.Remove(convexHullAPI); // ✅ Mark job as completed
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
