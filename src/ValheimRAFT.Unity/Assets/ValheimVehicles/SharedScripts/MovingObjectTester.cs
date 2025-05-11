// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class MovingObjectTester : MonoBehaviour
  {
    public bool HasMovement = true;
    public bool HasRotation = true;
    public bool HasRigidbodyAnimation = true;

    public float deltaMove = 1f;
    public float RotationSpeed = 90f;

    public Vector3 RotationalVector = Vector3.forward;
    public Vector3 MovementVector = Vector3.forward;

    [SerializeField] private Vector3 movementDirection = Vector3.up;
    [SerializeField] private float amplitude = 0.2f;
    [SerializeField] private float frequency = 1f;
    private Transform _transform;

    private Vector3 initialLocalPosition;
    private Rigidbody m_rigidbody;

    private void Awake()
    {
      _transform = transform;
      initialLocalPosition = _transform.localPosition;

      if (HasRigidbodyAnimation)
      {
        m_rigidbody = GetComponent<Rigidbody>();
        if (!m_rigidbody) m_rigidbody = gameObject.AddComponent<Rigidbody>();
        m_rigidbody.isKinematic = true; // Prevent physics forces
      }
    }

    private void FixedUpdate()
    {
      if (HasRigidbodyAnimation) RigidbodyUpdate();
      else TransformUpdate();
    }

    private void TransformUpdate()
    {
      if (HasRotation)
      {
        _transform.Rotate(RotationalVector, RotationSpeed * Time.fixedDeltaTime, Space.Self);
      }

      if (HasMovement)
      {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        _transform.localPosition = initialLocalPosition + movementDirection * offset;
      }
    }

    private void RigidbodyUpdate()
    {
      if (HasRotation)
      {
        Quaternion delta = Quaternion.Euler(RotationalVector * RotationSpeed * Time.fixedDeltaTime);
        m_rigidbody.MoveRotation(m_rigidbody.rotation * delta);
      }

      if (HasMovement)
      {
        Vector3 movement = MovementVector.normalized * deltaMove * Time.fixedDeltaTime;
        m_rigidbody.MovePosition(m_rigidbody.position + movement);
      }
    }
  }
}
