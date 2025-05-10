#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class RBRotationTester : MonoBehaviour
  {
    public bool HasMovement = true;
    public bool HasRotation = true;
    public float deltaMove = 1f;
    private Rigidbody m_rigidbody;
    public void Awake()
    {
      m_rigidbody = GetComponent<Rigidbody>();
      if (m_rigidbody == null)
      {
        m_rigidbody = gameObject.AddComponent<Rigidbody>();
      }
    }
    public void FixedUpdate()
    {
      if (HasRotation)
      {
        m_rigidbody.MoveRotation(m_rigidbody.rotation * Quaternion.Euler(0, 15f * Time.fixedDeltaTime, 0));
      }
      if (HasMovement)
      {
        m_rigidbody.MovePosition(transform.position + transform.forward * Time.fixedDeltaTime * deltaMove);
      }
    }
  }
}