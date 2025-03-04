#region

using System.Collections.Generic;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// This is meant to be extended off of for VehiclePiecesController allowing non valheim asset replication of movement without all the other piece logic.
  /// </summary>
  public class MovementPiecesController : MonoBehaviour
  {
    public Rigidbody m_syncRigidbody;
    public Rigidbody m_localRigidbody;
    public VehicleWheelController m_vehicleWheelController;
    public VehicleCollisionManager m_vehicleCollisionManager;
    public ConvexHullJobHandler m_convexHullJobHandler;

    public bool m_shouldSync = true;
    public readonly List<PrefabPieceData> prefabPieceDataItems = new();
    public virtual void Awake()
    {
      m_localRigidbody = GetComponent<Rigidbody>();
      m_vehicleCollisionManager = gameObject.AddComponent<VehicleCollisionManager>();
      m_convexHullJobHandler = gameObject.AddComponent<ConvexHullJobHandler>();
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
      piece.transform.SetParent(transform, false);
      var prefabPieceData = new PrefabPieceData(piece);
      prefabPieceDataItems.Add(prefabPieceData);
    }
  }
}