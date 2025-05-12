// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Controllers
{
  /// <summary>
  /// A custom sync component for child transform data under a shared ZNetView.
  /// </summary>
  public class ChildZSyncTransform : MonoBehaviour, IMonoUpdater
  {
    public bool m_syncPosition = true;
    public bool m_syncRotation = true;
    public bool m_syncBodyVelocity = true;

    public const float m_smoothnessPos = 0.2f;
    public const float m_smoothnessRot = 0.5f;

    public ZNetView m_nview;
    public Rigidbody m_body;

    private Vector3 m_cachedPosition = Vector3.negativeInfinity;
    private Quaternion m_cachedRotation = Quaternion.identity;
    private Vector3 m_cachedVelocity = Vector3.negativeInfinity;
    private Vector3 m_cachedAngularVelocity = Vector3.negativeInfinity;

    private int m_lastUpdateFrame = -1;
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
      if (!m_nview.IsValid() || m_nview.IsOwner()) return;

      var zdo = m_nview.GetZDO();

      if (m_syncPosition)
      {
        var syncedPos = zdo.GetVec3(VehicleZdoVars.SwivelSyncPosition, transform.localPosition);
        if (Vector3.Distance(transform.localPosition, syncedPos) > 0.001f)
        {
          if (m_body && m_isKinematic)
          {
            var worldTarget = transform.parent ? transform.parent.TransformPoint(syncedPos) : syncedPos;
            m_body.MovePosition(worldTarget);
          }
          else
          {
            transform.localPosition = Vector3.Lerp(transform.localPosition, syncedPos, fixedDeltaTime / m_smoothnessPos);
          }
        }
      }

      if (m_syncRotation)
      {
        var syncedRot = zdo.GetQuaternion(VehicleZdoVars.SwivelSyncRotation, transform.localRotation);
        if (Quaternion.Angle(transform.localRotation, syncedRot) > 0.1f)
        {
          if (m_body && m_isKinematic)
          {
            var worldRot = transform.parent ? transform.parent.rotation * syncedRot : syncedRot;
            m_body.MoveRotation(worldRot);
          }
          else
          {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, syncedRot, fixedDeltaTime / m_smoothnessRot);
          }
        }
      }

      if (m_syncBodyVelocity && m_body && !m_isKinematic)
      {
        m_body.velocity = zdo.GetVec3(VehicleZdoVars.SwivelSyncVelocity, Vector3.zero);
        m_body.angularVelocity = zdo.GetVec3(VehicleZdoVars.SwivelSyncAngularVelocity, Vector3.zero);
      }
    }

    public void CustomLateUpdate(float deltaTime)
    {
      if (!m_nview.IsOwner()) return;

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
        var vel = m_body.velocity;
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
      CustomLateUpdate(Time.deltaTime);
    }
  }
}