// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Integrations;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Controllers
{
  /// <summary>
  /// A custom sync component for child transform data under a shared ZNetView.
  /// Applies deterministic interpolation using SwivelMotionStateTracker from the integration layer.
  /// </summary>
  public class ChildZSyncTransform : MonoBehaviour, IMonoUpdater
  {
    [Header("Sync Settings")]
    public bool m_syncPosition = true;
    public bool m_syncRotation = true;
    public bool m_syncBodyVelocity = true;

    public ZNetView m_nview;
    public Rigidbody m_body;

    private bool m_isKinematic;

    private void Awake()
    {
      m_nview = GetComponentInParent<ZNetView>();
      m_body = GetComponent<Rigidbody>();

      if (m_nview == null || !m_nview.IsValid())
      {
        enabled = false;
        return;
      }

      m_isKinematic = m_body != null && m_body.isKinematic;
    }

    private void OnEnable()
    {
      ZSyncTransform.Instances.Add(this);
    }

    private void OnDisable()
    {
      ZSyncTransform.Instances.Remove(this);
    }

    public void CustomFixedUpdate(float fixedDeltaTime)
    {
      // No-op: handled in LateUpdate with unscaled time
    }

    public void CustomLateUpdate(float deltaTime)
    {
      if (m_nview == null || !m_nview.IsValid()) return;

      if (!m_nview.IsOwner())
      {
        ApplyRemoteSync();
      }
      else
      {
        PushOwnerSync();
      }
    }

    private void ApplyRemoteSync()
    {
      var integration = GetComponentInParent<SwivelComponentIntegration>();
      if (integration == null) return;

      var tracker = integration.motionTracker;
      if (tracker == null || !tracker.IsInitialized) return;

      if (m_syncPosition)
      {
        transform.localPosition = tracker.GetPredictedPosition();
      }

      if (m_syncRotation)
      {
        transform.localRotation = tracker.GetPredictedRotation();
      }

      if (m_syncBodyVelocity && m_body != null && !m_isKinematic)
      {
        // Velocity sync logic here (not needed for transform-only updates)
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
      }
    }

    private void PushOwnerSync()
    {
      // Optional: Owner-side push logic, if needed
    }

    public void CustomUpdate(float deltaTime, float time) {}

    public void ClientSync(float dt) {}

    public void SyncNow()
    {
      if (m_nview != null && m_nview.IsOwner())
      {
        PushOwnerSync();
      }
    }
  }
}