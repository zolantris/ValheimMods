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

    public bool m_shouldSync = true;
    public readonly List<GameObject> vehiclePieces = new();
    public virtual void Awake()
    {
      m_localRigidbody = GetComponent<Rigidbody>();
      m_vehicleCollisionManager = gameObject.AddComponent<VehicleCollisionManager>();
      AddAllChildrenToIgnores(transform);
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

    public void AddAllChildrenToIgnores(Transform targetTransform)
    {
      foreach (Transform child in targetTransform)
      {
        m_vehicleCollisionManager.AddObjectToVehicle(child.gameObject);
      }
    }

    public void OnPieceAdded(GameObject piece)
    {
      piece.transform.SetParent(transform, false);
      vehiclePieces.Add(piece);
      // OnPieceAddedIgnoreAllColliders(piece);
    }

    internal void OnPieceAddedIgnoreAllColliders(GameObject piece)
    {
      m_vehicleCollisionManager.AddObjectToVehicle(piece);
    }
  }
}