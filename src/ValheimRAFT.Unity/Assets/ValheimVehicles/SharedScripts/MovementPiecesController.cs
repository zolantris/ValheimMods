#region

using System.Collections.Generic;
using System.Linq;
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
        public readonly List<PrefabPieceData> m_removedPieceData = new();
        public readonly List<PrefabPieceData> m_addedPieceData = new();

        public ConvexHullAPI m_convexHullAPI;

        public virtual void Awake()
        {
            m_convexHullAPI = gameObject.AddComponent<ConvexHullAPI>();
            m_localRigidbody = GetComponent<Rigidbody>();
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
            prefabPieceDataItems.Add(piece, prefabPieceData);

            // ✅ Ensure collision ignores are updated immediately
            UpdateCollidersIgnoresOnChange(prefabPieceData);
        }

        public void OnPieceRemoved(GameObject piece)
        {
#if UNITY_EDITOR
            piece.transform.SetParent(null);
#endif
            if (prefabPieceDataItems.TryGetValue(piece, out var prefabPieceData))
            {
                var isValidWithin = m_convexHullAPI.convexHullMeshColliders
                    .FirstOrDefault(x => prefabPieceData.AreAllPointsValid(x, transform, convexAPIThresholdDistance));

                // ✅ Only rebuild if the piece was contributing to the hull
                if (isValidWithin == null)
                {
                    OnDelayedBoundsRebuild();
                }

                prefabPieceDataItems.Remove(piece);
            }
        }

        /// <summary>
        /// For running all bounds builds. This should be invoked as the main method.
        /// This method is meant to be overridden and not extended.
        /// </summary>
        public virtual void OnDelayedBoundsRebuild()
        {
            CancelInvoke(nameof(OnDelayedBoundsRebuild));
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
            }
        }

        /// <summary>
        /// Requests convex hull generation for a tracked prefab.
        /// </summary>
        public void GenerateConvexHull(float clusterThreshold)
        {
            var prefabDataItems = prefabPieceDataItems.Values.ToList();
            ConvexHullJobHandler.ScheduleConvexHullJob(m_convexHullAPI, prefabDataItems, clusterThreshold);
        }
    }
}
