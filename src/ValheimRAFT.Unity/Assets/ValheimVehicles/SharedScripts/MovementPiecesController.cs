#region

using System.Collections.Generic;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
    /// <summary>
    /// This is meant to be extended for VehiclePiecesController, allowing non-Valheim asset replication of movement without all other piece logic.
    /// </summary>
    public class MovementPiecesController : MonoBehaviour
    {
        public Rigidbody m_syncRigidbody;
        public Rigidbody m_localRigidbody;

        public bool m_shouldSync = true;
        public Vector3 convexAPIThresholdDistance = Vector3.one;

        public List<Collider> vehicleCollidersToIgnore = new();
        public readonly Dictionary<GameObject, PrefabPieceData> prefabPieceDataItems = new();
        public List<GameObject> convexHullMeshes =>
            m_convexHullAPI.convexHullMeshes;
        public List<Collider> convexHullColliders = [];
        public List<MeshCollider> convexHullMeshColliders = [];
        public List<GameObject> convexHullTriggerMeshes =>
            m_convexHullAPI.convexHullTriggerMeshes;
        public ConvexHullAPI m_convexHullAPI;

        public virtual void Awake()
        {
            m_localRigidbody = GetComponent<Rigidbody>();
        }

        public virtual void Start()
        {
#if UNITY_EDITOR
            if (!m_convexHullAPI)
            {
                m_convexHullAPI = gameObject.AddComponent<ConvexHullAPI>();
            }
#endif
        }

#if UNITY_EDITOR
        public void FixedUpdate()
        {
            CustomFixedUpdate(Time.fixedDeltaTime);
        }
#endif
        public virtual void CustomFixedUpdate(float deltaTime)
        {
            if (m_shouldSync)
            {
                m_localRigidbody.Move(m_syncRigidbody.position, m_syncRigidbody.rotation);
            }
        }

        public void OnPieceAdded(GameObject piece)
        {
#if UNITY_EDITOR
            piece.transform.SetParent(transform, false);
#endif
            var prefabPieceData = new PrefabPieceData(piece);
            prefabPieceDataItems[piece] = prefabPieceData; // ✅ Direct assignment instead of `.Add()`

            // ✅ Ensure collision ignores are updated immediately
            UpdateCollidersIgnoresOnChange(prefabPieceData);
        }

        public void OnPieceRemoved(GameObject piece)
        {
#if UNITY_EDITOR
            piece.transform.SetParent(null);
#endif
            if (!prefabPieceDataItems.TryGetValue(piece, out var prefabPieceData)) return;

            // ✅ Avoid LINQ allocation by using `foreach`
            var shouldRebuild = true;
            foreach (var meshCollider in m_convexHullAPI.convexHullMeshColliders)
            {
                if (prefabPieceData.AreAllPointsValid(meshCollider, transform, convexAPIThresholdDistance))
                {
                    shouldRebuild = false;
                    break;
                }
            }

            if (shouldRebuild)
            {
                RequestBoundsRebuild();
            }

            prefabPieceDataItems.Remove(piece);
        }

        /// <summary>
        /// Requests a delayed convex hull rebuild.
        /// </summary>
        public virtual void RequestBoundsRebuild()
        {
            CancelInvoke(nameof(RequestBoundsRebuild));
            Invoke(nameof(GenerateConvexHull), 1f);
        }

        /// <summary>
        /// Updates collision ignores for a specific `PrefabPieceData` instance.
        /// </summary>
        public void UpdateCollidersIgnoresOnChange(PrefabPieceData prefabPieceData)
        {
            foreach (var prefabCollider in prefabPieceData.AllColliders)
            {
                if (prefabCollider == null) continue;

                foreach (var vehicleCollider in vehicleCollidersToIgnore)
                {
                    if (vehicleCollider == null) continue;
                    Physics.IgnoreCollision(prefabCollider, vehicleCollider, true);
                }

                foreach (var convexCollider in m_convexHullAPI.convexHullMeshColliders)
                {
                    if (convexCollider == null) continue;
                    Physics.IgnoreCollision(prefabCollider, convexCollider, true);
                }
            }
        }

        /// <summary>
        /// Ignores all collisions between convex colliders and tracked vehicle colliders.
        /// </summary>
        public void IgnoreAllCollisionsFromConvexColliders()
        {
            // ✅ Precompute `AllColliders` to avoid reallocation
            var allColliders = new HashSet<Collider>();
            foreach (var prefabPieceData in prefabPieceDataItems.Values)
            {
                allColliders.UnionWith(prefabPieceData.AllColliders);
            }

            foreach (var convexCollider in m_convexHullAPI.convexHullMeshColliders)
            {
                if (convexCollider == null) continue;

                foreach (var prefabCollider in allColliders)
                {
                    if (prefabCollider == null) continue;
                    Physics.IgnoreCollision(convexCollider, prefabCollider, true);
                }

                foreach (var vehicleCollider in vehicleCollidersToIgnore)
                {
                    if (vehicleCollider == null) continue;
                    Physics.IgnoreCollision(convexCollider, vehicleCollider, true);
                }
            }
        }

        /// <summary>
        /// Requests convex hull generation for a tracked prefab.
        /// </summary>
        public void GenerateConvexHull(float clusterThreshold)
        {
            // ✅ Avoid extra allocations by passing `IEnumerable` instead of `.ToList()`
            var prefabDataItems = prefabPieceDataItems.Values;
            ConvexHullJobHandler.ScheduleConvexHullJob(m_convexHullAPI, new List<PrefabPieceData>(prefabDataItems), clusterThreshold, () =>
            {
                Debug.Log("✅ Convex Hull Generation Complete!");
                IgnoreAllCollisionsFromConvexColliders();
            });
        }
    }
}
