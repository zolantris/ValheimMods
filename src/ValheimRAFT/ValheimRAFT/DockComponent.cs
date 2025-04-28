using UnityEngine;
using ZdoWatcher;

namespace ValheimRAFT;

public class DockComponent : MonoBehaviour
{
  private enum DockState
  {
    None,
    EnteringDock,
    Docked,
    LeavingDock
  }

  private DockState m_dockState = DockState.None;

  private float m_dockingStrength = 1f;

  private GameObject m_dockedObject;

  private Rigidbody m_dockedRigidbody;

  private ZNetView m_nview;

  public Transform m_dockLocation;

  public Transform m_dockExit;

  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
  }

  public void FixedUpdate()
  {
    if ((bool)m_dockedRigidbody)
    {
      if (m_dockState == DockState.EnteringDock)
        PushToward(m_dockLocation);
      else if (m_dockState == DockState.LeavingDock) PushToward(m_dockExit);
    }
  }

  private void PushToward(Transform target)
  {
    var direction = target.transform.position -
                    m_dockedRigidbody.transform.position;
    m_dockedRigidbody.AddForce(direction.normalized * m_dockingStrength,
      ForceMode.VelocityChange);
  }

  public void OnTriggerEnter(Collider other)
  {
    if ((bool)m_dockedObject && CanDock(other)) Dock(other);
  }

  private void Dock(Collider other)
  {
    var nv = other.GetComponentInParent<ZNetView>();
    if ((bool)nv && nv.IsOwner())
    {
      var rb = nv.GetComponent<Rigidbody>();
      if ((bool)rb)
      {
        var id = ZdoWatchController.Instance.GetOrCreatePersistentID(nv.m_zdo);
        m_dockedObject = nv.gameObject;
        m_dockedRigidbody = rb;
        m_nview.m_zdo.Set("MBDock_dockedObject", id);
        m_dockState = DockState.EnteringDock;
      }
    }
  }

  private bool CanDock(Collider other)
  {
    if (other.name.StartsWith("Karve")) return true;

    if (other.name.StartsWith("VikingShip")) return true;

    return false;
  }
}