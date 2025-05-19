using UnityEngine;
namespace ValheimVehicles.Helpers;

/// <summary>
/// Mostly a Placeholder/Backup for Rigidbody Syncing when parent is a moving or syncing rigidbody and we still need accurate physics when freezing in place (relative to parent).
/// </summary>
public class FrozenRigidbodyParentSync
{
  private Vector3 frozenLocalPos;
  private Quaternion frozenLocalRot;
  private bool isFrozen;
  public bool mustSync;


  public Rigidbody m_body;
  public MonoBehaviour monoBehaviour;

  public void OnTransformParentChanged()
  {
    var parentRigidbody = monoBehaviour.transform.GetComponentInParent<Rigidbody>();
    SetFrozenState(parentRigidbody != null);
  }

  public void SetFrozenState(bool val)
  {
    if (val)
    {
      var transform = monoBehaviour.transform;
      frozenLocalPos = transform.localPosition;
      frozenLocalRot = transform.localRotation;
    }
    isFrozen = val;
  }

  public void FixedUpdate()
  {
    if (!mustSync || !m_body) return;

    var parent = monoBehaviour.transform.parent;
    if (parent == null) return;

    var worldPos = parent.TransformPoint(frozenLocalPos);
    var worldRot = parent.rotation * frozenLocalRot;
    m_body.Move(worldPos, worldRot);
  }
}