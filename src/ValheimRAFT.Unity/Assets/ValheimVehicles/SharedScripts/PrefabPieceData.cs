// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace ValheimVehicles.SharedScripts
{
    public struct PrefabPieceData
    {
        public GameObject Prefab;
        public List<Collider> AllColliders; // ✅ Tracks all colliders for IgnoreCollision logic
        public List<Collider> HullColliders; // ✅ Tracks only colliders used for convex hull generation
        public PrefabColliderPointData PointDataItems; // ✅ Stores a single struct

        public PrefabPieceData(GameObject prefab)
        {
            Prefab = prefab;
            AllColliders = new List<Collider>(); // ✅ Restored for IgnoreCollision tracking
            HullColliders = new List<Collider>(); // ✅ Tracks only convex-relevant colliders
            PointDataItems = default;

            InitializeColliders(prefab.transform.root);
        }

        public void InitializeColliders(Transform root)
        {
            // ✅ Get all colliders in the prefab hierarchy and store them
            Prefab.GetComponentsInChildren(true, AllColliders);

            foreach (var collider in AllColliders)
            {
                if (collider.gameObject.activeInHierarchy &&
                    LayerHelpers.IsContainedWithinMask(collider.gameObject.layer, LayerHelpers.PhysicalLayers))
                {
                    HullColliders.Add(collider); // ✅ Only store relevant colliders for convex processing

                    // ✅ Get Collider Points
                    var points = ConvexHullAPI.GetColliderPointsGlobal(collider).Select(root.InverseTransformPoint).ToList();
                    
                    if (points.Count > 0)
                    {
                        // ✅ Store a **single** PrefabColliderPointData instead of a list
                        PointDataItems = new PrefabColliderPointData(
                            Prefab.transform.localPosition,
                            points.ToArray(),
                            Allocator.Persistent
                        );

                        // ✅ Only track the first valid collider’s points
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Validates that all points of this `PrefabPieceData` are within the `MeshCollider`
        /// and not within the specified `thresholdDistance` from the closest convex surface.
        /// </summary>
        public bool AreAllPointsValid(
            MeshCollider convexMeshCollider,
            Transform rootTransform,
            Vector3 thresholdDistance)
        {
            if (convexMeshCollider == null)
            {
                Debug.LogError("❌ MeshCollider is null. Validation failed.");
                return false;
            }

            foreach (var localPoint in PointDataItems.Points)
            {
                // ✅ Convert local point to world space
                var worldPoint = rootTransform.TransformPoint(localPoint);

                // ✅ Get the closest surface point on the convex mesh
                var closestPoint = convexMeshCollider.ClosestPoint(worldPoint);

                // ✅ Check if the world point is inside the collider
                var isInside = Vector3.Distance(closestPoint, worldPoint) < Mathf.Epsilon;

                // ✅ Check if point is within the threshold distance from the surface
                var isTooClose = Vector3.Distance(closestPoint, worldPoint) < thresholdDistance.magnitude;

                if (!isInside || isTooClose)
                {
                    // 🚨 If any point fails validation, return false immediately
                    return false;
                }
            }

            // ✅ If all points pass the validation, return true
            return true;
        }

        /// <summary>
        /// Ensures the allocated `NativeArray<>` instance is disposed when no longer needed.
        /// </summary>
        public void Dispose()
        {
            PointDataItems.Dispose();
        }
    }
}
