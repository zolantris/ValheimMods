// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Controllers
{
  /// <summary>
  /// A custom sync component for child transform data under a shared ZNetView.
  /// Applies local interpolation and smoothing of synced transforms.
  /// </summary>
  public class ChildZSyncTransform : MonoBehaviour, IMonoUpdater
  {
    [Header("Sync Settings")]
    public bool m_syncPosition = true;
    public bool m_syncRotation = true;
    public bool m_syncBodyVelocity = true;
    public bool m_useExtrapolation = true;

    [Header("Smoothing Config")]
    public float m_positionSmoothTime = 0.2f;
    public float m_rotationSmoothTime = 0.5f;
    public float m_snapDistanceThreshold = 1.0f;
    public float m_positionEpsilon = 0.01f;
    public float m_rotationEpsilon = 0.1f;
    public float m_extrapolationTime = 0.1f;

    public ZNetView m_nview;
    public Rigidbody m_body;

    private Vector3 m_cachedPosition = Vector3.negativeInfinity;
    private Quaternion m_cachedRotation = Quaternion.identity;
    private Vector3 m_cachedVelocity = Vector3.negativeInfinity;
    private Vector3 m_cachedAngularVelocity = Vector3.negativeInfinity;

    private Vector3 m_smoothVelocity;
    private float m_smoothAngularVelocity;

    private bool m_isKinematic;

    private void Awake()
    {
      m_nview = GetComponentInParent<ZNetView>();
      m_body = GetComponent<Rigidbody>();

      if (!m_nview || !m_nview.IsValid())
      {
        enabled = false;
        return;
      }

      m_isKinematic = m_body && m_body.isKinematic;
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
      // Force run even if paused – use unscaled delta time
      CustomLateUpdate(Time.unscaledDeltaTime);
    }

    public void CustomLateUpdate(float deltaTime)
    {
      if (!m_nview.IsValid()) return;

      if (!m_nview.IsOwner())
      {
        ApplyRemoteSync(deltaTime);
      }
      else
      {
        PushOwnerSync();
      }
    }

    private void ApplyRemoteSync(float deltaTime)
    {
      var zdo = m_nview.GetZDO();

      if (m_syncPosition)
      {
        var syncedPos = zdo.GetVec3(VehicleZdoVars.SwivelSyncPosition, transform.localPosition);
        if (m_useExtrapolation)
        {
          var vel = zdo.GetVec3(VehicleZdoVars.SwivelSyncVelocity, Vector3.zero);
          syncedPos += vel * m_extrapolationTime;
        }

        var distance = Vector3.Distance(transform.localPosition, syncedPos);
        if (distance > m_snapDistanceThreshold)
        {
          transform.localPosition = syncedPos;
        }
        else if (distance > m_positionEpsilon)
        {
          transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            syncedPos,
            ref m_smoothVelocity,
            m_positionSmoothTime,
            float.PositiveInfinity,
            deltaTime);
        }
      }

      if (m_syncRotation)
      {
        var syncedRot = zdo.GetQuaternion(VehicleZdoVars.SwivelSyncRotation, transform.localRotation);
        var angle = Quaternion.Angle(transform.localRotation, syncedRot);

        if (angle > m_snapDistanceThreshold)
        {
          transform.localRotation = syncedRot;
        }
        else if (angle > m_rotationEpsilon)
        {
          transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            syncedRot,
            deltaTime / m_rotationSmoothTime);
        }
      }

      if (m_syncBodyVelocity && m_body && !m_isKinematic)
      {
        m_body.linearVelocity = zdo.GetVec3(VehicleZdoVars.SwivelSyncVelocity, Vector3.zero);
        m_body.angularVelocity = zdo.GetVec3(VehicleZdoVars.SwivelSyncAngularVelocity, Vector3.zero);
      }
    }

    private void PushOwnerSync()
    {
      var zdo = m_nview.GetZDO();

      if (m_syncPosition)
      {
        var localPos = transform.localPosition;
        if (localPos != m_cachedPosition)
        {
          zdo.Set(VehicleZdoVars.SwivelSyncPosition, localPos);
          m_cachedPosition = localPos;
        }
      }

      if (m_syncRotation)
      {
        var localRot = transform.localRotation;
        if (localRot != m_cachedRotation)
        {
          zdo.Set(VehicleZdoVars.SwivelSyncRotation, localRot);
          m_cachedRotation = localRot;
        }
      }

      if (m_syncBodyVelocity && m_body && !m_isKinematic)
      {
        var vel = m_body.linearVelocity;
        var angVel = m_body.angularVelocity;

        if (vel != m_cachedVelocity)
        {
          zdo.Set(VehicleZdoVars.SwivelSyncVelocity, vel);
          m_cachedVelocity = vel;
        }

        if (angVel != m_cachedAngularVelocity)
        {
          zdo.Set(VehicleZdoVars.SwivelSyncAngularVelocity, angVel);
          m_cachedAngularVelocity = angVel;
        }
      }
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