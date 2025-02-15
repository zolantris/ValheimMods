#region

using UnityEngine;

#endregion

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

    public bool m_shouldSync = true;
    public virtual void Awake()
    {
      m_localRigidbody = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
      if (m_shouldSync)
      {
        m_localRigidbody.Move(m_syncRigidbody.position, m_syncRigidbody.rotation);
      }
    }
  }
}