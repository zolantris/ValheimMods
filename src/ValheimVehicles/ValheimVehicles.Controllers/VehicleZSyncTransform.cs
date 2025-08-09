using System;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Controllers;

/// <summary>
/// A replacement class for ZSyncTransform which causes a bunch of jitters
/// </summary>
public class VehicleZSyncTransform : MonoBehaviour, IMonoUpdater
{
  public bool m_syncPosition = true;
  public bool m_syncRotation = true;
  public bool m_syncScale;
  public bool m_syncBodyVelocity;
  public bool m_isKinematicBody;
  public bool m_useGravity = true;
  public float m_targetPosTimer;
  public uint m_posRevision = uint.MaxValue;
  public int m_lastUpdateFrame = -1;
  public bool m_wasOwner;
  public ZNetView m_nview;
  public Rigidbody m_body;
  public Projectile m_projectile;
  public Vector3 m_positionCached = Vector3.zero;
  public Vector3 m_velocityCached = Vector3.zero;
  private bool _isReady = false;

  public void Awake()
  {
    TryInit();
  }

  public bool TryInit()
  {
    if (_isReady) return true;
    try
    {
      m_nview = GetComponentInParent<ZNetView>();
      m_body = GetComponentInParent<Rigidbody>();
      m_projectile = GetComponent<Projectile>();
      if (m_nview == null || m_nview.GetZDO() == null || !m_nview.IsValid() || m_body == null)
      {
        _isReady = false;
        return false;
      }

      m_isKinematicBody = m_body.isKinematic;
      m_useGravity = m_body.useGravity;

      m_wasOwner = m_nview.GetZDO().IsOwner();
      _isReady = true;

      return _isReady;
    }
    catch (Exception e)
    {
      LoggerProvider.LogDebugDebounced($"Exception in TryInit \n{e}");
      return false;
    }
  }

  public void OnEnable()
  {
    this.WaitForZNetView(() =>
    {
      TryInit();
    });
    ZSyncTransform.Instances.Add(this);
  }

  public void OnDisable()
  {
    _isReady = false;
    ZSyncTransform.Instances.Remove(this);
  }

  public Vector3 GetVelocity()
  {
    if (m_body != null)
      return m_body.velocity;
    return m_projectile != null ? m_projectile.GetVelocity() : Vector3.zero;
  }

  public Vector3 GetPosition()
  {
    return !(bool)(Object)m_body ? transform.position : m_body.position;
  }

  public void OwnerSync()
  {
    if (!_isReady) return;
    // force updates kinematic body.
    m_isKinematicBody = m_body.isKinematic;

    var zdo = m_nview.GetZDO();
    var flag1 = zdo.IsOwner();
    var flag2 = !m_wasOwner & flag1;
    m_wasOwner = flag1;
    if (!flag1)
      return;
    if (flag2)
    {
      var flag3 = false;
      if (m_syncPosition)
      {
        transform.position = zdo.GetPosition();
        flag3 = true;
      }
      if (m_syncRotation)
      {
        transform.rotation = zdo.GetRotation();
        flag3 = true;
      }
      if (!m_isKinematicBody && m_syncBodyVelocity && (bool)(Object)m_body)
      {
        m_body.velocity = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
        m_body.angularVelocity = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
      }
      if (flag3 && (bool)(Object)m_body)
        Physics.SyncTransforms();
    }
    if (transform.position.y < -5000.0)
    {
      if (!m_isKinematicBody && (bool)(Object)m_body)
        m_body.velocity = Vector3.zero;
      LoggerProvider.LogInfo("Object fell out of world:" + gameObject.name);
      var groundHeight = ZoneSystem.instance.GetGroundHeight(transform.position);
      transform.position = transform.position with
      {
        y = groundHeight + 1f
      };
      if (!(bool)(Object)m_body)
        return;
      Physics.SyncTransforms();
    }
    else
    {
      if (m_syncPosition)
      {
        var position = GetPosition();
        if (!m_positionCached.Equals(position))
          zdo.SetPosition(position);
        var velocity = GetVelocity();
        if (!m_velocityCached.Equals(velocity))
          zdo.Set(ZDOVars.s_velHash, velocity);
        m_positionCached = position;
        m_velocityCached = velocity;
      }
      if (m_syncRotation && transform.hasChanged)
      {
        var rot = (bool)(Object)m_body ? m_body.rotation : transform.rotation;
        zdo.SetRotation(rot);
      }
      if (m_syncScale && transform.hasChanged)
      {
        if (Mathf.Approximately(transform.localScale.x, transform.localScale.y) && Mathf.Approximately(transform.localScale.x, transform.localScale.z))
        {
          zdo.RemoveVec3(ZDOVars.s_scaleHash);
          zdo.Set(ZDOVars.s_scaleScalarHash, transform.localScale.x);
        }
        else
        {
          zdo.RemoveFloat(ZDOVars.s_scaleScalarHash);
          zdo.Set(ZDOVars.s_scaleHash, transform.localScale);
        }
      }
      if ((bool)(Object)m_body)
      {
        if (m_syncBodyVelocity)
        {
          m_nview.GetZDO().Set(ZDOVars.s_bodyVelHash, m_body.velocity);
          m_nview.GetZDO().Set(ZDOVars.s_bodyAVelHash, m_body.angularVelocity);
        }
        m_body.useGravity = m_useGravity;
      }
      transform.hasChanged = false;
    }
  }

  public bool GetRelativePosition(
    ZDO zdo,
    out ZDOID parent,
    out string attachJoint,
    out Vector3 relativePos,
    out Quaternion relativeRot,
    out Vector3 relativeVel)
  {
    if ((bool)(Object)transform.parent)
    {
      var component = (bool)(Object)transform.parent ? transform.parent.GetComponent<ZNetView>() : null;
      if ((bool)(Object)component && component.IsValid())
      {
        parent = component.GetZDO().m_uid;
        attachJoint = "";
        relativePos = transform.localPosition;
        relativeRot = transform.localRotation;
        relativeVel = Vector3.zero;
        return true;
      }
    }
    parent = ZDOID.None;
    attachJoint = "";
    relativePos = Vector3.zero;
    relativeRot = Quaternion.identity;
    relativeVel = Vector3.zero;
    return false;
  }

  public void SyncPosition(ZDO zdo, float dt, out bool usedLocalRotation)
  {
    usedLocalRotation = false;
    var position = zdo.GetPosition();
    if ((int)zdo.DataRevision != (int)m_posRevision)
    {
      m_posRevision = zdo.DataRevision;
      m_targetPosTimer = 0.0f;
    }
    if (zdo.HasOwner())
    {
      m_targetPosTimer += dt;
      m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
      var vec3 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
      position += vec3 * m_targetPosTimer;
    }
    var num = Vector3.Distance(transform.position, position);
    if (num <= 1.0 / 1000.0)
      return;
    transform.position = num < 5.0 ? Vector3.Lerp(transform.position, position, Mathf.Max(dt, m_targetPosTimer)) : position;
    if (!(bool)(Object)m_body)
      return;
    Physics.SyncTransforms();
  }

  public static float smoothTime = 0.5f;

  public void KinematicClientSync(ZDO zdo, float dt)
  {
    if (m_syncPosition)
    {
      var vector3 = zdo.GetPosition();
      if (Vector3.Distance(m_body.position, vector3) > 5.0)
      {
        m_body.position = vector3;
      }
      else
      {
        if (Vector3.Distance(m_body.position, vector3) > 0.009999999776482582)
          vector3 = Vector3.Lerp(m_body.position, vector3, dt);
        m_body.MovePosition(vector3);
      }
    }
    if (m_syncRotation)
    {
      var rotation = zdo.GetRotation();
      if (Quaternion.Angle(m_body.rotation, rotation) > 45.0)
        m_body.rotation = rotation;
      else
        m_body.MoveRotation(rotation);
    }
  }

  public void NonKinematicSync(ZDO zdo, float dt)
  {
    if (m_isKinematicBody)
    {
      LoggerProvider.LogDebugDebounced("Somehow called nonkinematic sync when rigidbody is kinematic. Bailing...");
      return;
    }

    var usedLocalRotation = false;
    if (m_syncPosition)
      SyncPosition(zdo, dt, out usedLocalRotation);
    if (m_syncRotation && !usedLocalRotation)
    {
      var rotation = zdo.GetRotation();
      if (Quaternion.Angle(transform.rotation, rotation) > 1.0 / 1000.0)
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, dt * smoothTime);
    }
    if ((bool)(Object)m_body)
    {
      m_body.useGravity = false;
      if (m_syncBodyVelocity && m_nview.HasOwner())
      {
        var vec3_1 = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
        var vec3_2 = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
        if (vec3_1.magnitude > 0.009999999776482582 || vec3_2.magnitude > 0.009999999776482582)
        {
          m_body.velocity = vec3_1;
          m_body.angularVelocity = vec3_2;
        }
        else
          m_body.Sleep();
      }
      else if (!m_body.IsSleeping())
      {
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
        m_body.Sleep();
      }
    }
    if (!m_syncScale)
      return;
    var vec3 = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
    if (vec3 != Vector3.zero)
    {
      transform.localScale = vec3;
    }
    else
    {
      var num = zdo.GetFloat(ZDOVars.s_scaleScalarHash, transform.localScale.x);
      if (transform.localScale.x.Equals(num))
        return;
      transform.localScale = new Vector3(num, num, num);
    }
  }

  public void SetKinematic(bool val, bool shouldSetDirectly = true)
  {
    if (m_body == null) return;
    m_isKinematicBody = val;

    if (m_nview != null && m_nview.IsOwner())
    {
      m_body.isKinematic = val;
    }
    else
    {
      m_body.isKinematic = false;
    }
  }

  public void SetGravity(bool val, bool shouldSetDirectly = true)
  {
    if (m_body == null) return;
    m_useGravity = val;
    if (m_nview != null && m_nview.IsOwner())
    {
      m_body.useGravity = val;
    }
    else
    {
      m_body.useGravity = false;
    }
  }

  public void ClientSync(float dt)
  {
    if (!_isReady) return;
    var zdo = m_nview.GetZDO();
    if (zdo.IsOwner())
      return;
    var frameCount = Time.frameCount;
    if (m_lastUpdateFrame == frameCount)
      return;
    m_lastUpdateFrame = frameCount;
    m_isKinematicBody = m_body.isKinematic;

    if (m_isKinematicBody)
    {
      KinematicClientSync(zdo, dt);
      return;
    }

    NonKinematicSync(zdo, dt);
  }

  public void CustomFixedUpdate(float fixedDeltaTime)
  {
    if (!TryInit()) return;
    if (!m_nview.IsValid())
      return;
    ClientSync(fixedDeltaTime);
  }

  public void CustomUpdate(float deltaTime, float time)
  {
  }

  public void CustomLateUpdate(float deltaTime)
  {
    if (!TryInit()) return;
    if (!m_nview.IsValid())
      return;
    OwnerSync();
  }

  public void SyncNow()
  {
    if (!m_nview.IsValid())
      return;
    OwnerSync();
  }
}