using UnityEngine;
namespace ValheimVehicles.Helpers;

/// <summary>
/// Mostly a Placeholder/Backup for Rigidbody Syncing when parent is a moving or syncing rigidbody and we still need accurate physics when freezing in place (relative to parent).
/// </summary>
public class FrozenRigidbodySync
{
  private Vector3 frozenLocalPos;
  private Quaternion frozenLocalRot;
  public bool isFrozen;
  public bool mustSync = true;


  public Rigidbody m_body;
  public Rigidbody m_targetBody;

  public void Init(Rigidbody current, Rigidbody target)
  {
    m_body = current;
    m_targetBody = target;
  }

  public Vector3 GetFrozenSyncPoint()
  {
    return m_targetBody.position + m_targetBody.rotation * frozenLocalPos;
  }
  public Quaternion GetFrozenSyncRotation()
  {
    return m_targetBody.rotation * frozenLocalRot;
  }

  public void SetFrozenState(bool val)
  {
    if (!m_body || !m_body.isKinematic || !m_targetBody)
    {
      isFrozen = false;
      return;
    }

    if (val)
    {
      SyncFrozenPosition();
    }
    isFrozen = val;
  }

  public void SyncFrozenPosition()
  {
    frozenLocalPos = Quaternion.Inverse(m_targetBody.rotation) * (m_body.position - m_targetBody.position);
    frozenLocalRot = Quaternion.Inverse(m_targetBody.rotation) * m_body.rotation;
  }

  public void FixedUpdate()
  {
    if (!mustSync || !m_body || !m_body.isKinematic || !m_targetBody) return;
    var worldPos = GetFrozenSyncPoint();
    var worldRot = GetFrozenSyncRotation();

    if (Vector3.Distance(worldPos + frozenLocalPos, m_body.position) > 150f)
    {
      return;
    }

    m_body.Move(worldPos, worldRot);
  }
}