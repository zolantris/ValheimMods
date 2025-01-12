using UnityEngine;

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
  public bool m_characterParentSync;
  public const float m_smoothnessPos = 0.2f;
  public const float m_smoothnessRot = 0.5f;
  public bool m_isKinematicBody;
  public bool m_useGravity = true;
  public Vector3 m_tempRelPos;
  public bool m_haveTempRelPos;
  public float m_targetPosTimer;
  public uint m_posRevision = uint.MaxValue;
  public int m_lastUpdateFrame = -1;
  public bool m_wasOwner;
  public ZNetView m_nview;
  public Rigidbody m_body;
  public Projectile m_projectile;
  public Character m_character;
  public ZDOID m_tempParent = ZDOID.None;
  public ZDOID m_tempParentCached;
  public string m_tempAttachJoint;
  public Vector3 m_tempRelativePos;
  public Quaternion m_tempRelativeRot;
  public Vector3 m_tempRelativeVel;
  public Vector3 m_tempRelativePosCached;
  public Quaternion m_tempRelativeRotCached;
  public Vector3 m_tempRelativeVelCached;
  public Vector3 m_positionCached = Vector3.negativeInfinity;
  public Vector3 m_velocityCached = Vector3.negativeInfinity;

  public void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    m_body = GetComponent<Rigidbody>();
    m_projectile = GetComponent<Projectile>();
    m_character = GetComponent<Character>();
    if (m_nview.GetZDO() == null)
    {
      enabled = false;
    }
    else
    {
      if ((bool) (Object) m_body)
      {
        m_isKinematicBody = m_body.isKinematic;
        m_useGravity = m_body.useGravity;
      }
      m_wasOwner = m_nview.GetZDO().IsOwner();
    }
  }

  public void OnEnable() => ZSyncTransform.Instances.Add(this);

  public void OnDisable() => ZSyncTransform.Instances.Remove(this);

  public Vector3 GetVelocity()
  {
    if (m_body != null)
      return m_body.velocity;
    return m_projectile != null ? m_projectile.GetVelocity() : Vector3.zero;
  }

  public Vector3 GetPosition()
  {
    return !(bool) (Object) m_body ? transform.position : m_body.position;
  }

  public void OwnerSync()
  {
    ZDO zdo = m_nview.GetZDO();
    bool flag1 = zdo.IsOwner();
    bool flag2 = !m_wasOwner & flag1;
    m_wasOwner = flag1;
    if (!flag1)
      return;
    if (flag2)
    {
      bool flag3 = false;
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
      if (m_syncBodyVelocity && (bool) (Object) m_body)
      {
        m_body.velocity = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
        m_body.angularVelocity = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
      }
      if (flag3 && (bool) (Object) m_body)
        Physics.SyncTransforms();
    }
    if (transform.position.y < -5000.0)
    {
      if ((bool) (Object) m_body)
        m_body.velocity = Vector3.zero;
      ZLog.Log("Object fell out of world:" + gameObject.name);
      float groundHeight = ZoneSystem.instance.GetGroundHeight(transform.position);
      transform.position = transform.position with
      {
        y = groundHeight + 1f
      };
      if (!(bool) (Object) m_body)
        return;
      Physics.SyncTransforms();
    }
    else
    {
      if (m_syncPosition)
      {
        Vector3 position = GetPosition();
        if (!m_positionCached.Equals(position))
          zdo.SetPosition(position);
        Vector3 velocity = GetVelocity();
        if (!m_velocityCached.Equals(velocity))
          zdo.Set(ZDOVars.s_velHash, velocity);
        m_positionCached = position;
        m_velocityCached = velocity;
        if (m_characterParentSync)
        {
          if (GetRelativePosition(zdo, out m_tempParent, out m_tempAttachJoint, out m_tempRelativePos, out m_tempRelativeRot, out m_tempRelativeVel))
          {
            if (m_tempParent != m_tempParentCached)
            {
              zdo.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, m_tempParent);
              zdo.Set(ZDOVars.s_attachJointHash, m_tempAttachJoint);
            }
            if (!m_tempRelativePos.Equals(m_tempRelativePosCached))
              zdo.Set(ZDOVars.s_relPosHash, m_tempRelativePos);
            if (!m_tempRelativeRot.Equals(m_tempRelativeRotCached))
              zdo.Set(ZDOVars.s_relRotHash, m_tempRelativeRot);
            if (!m_tempRelativeVel.Equals(m_tempRelativeVelCached))
              zdo.Set(ZDOVars.s_velHash, m_tempRelativeVel);
            m_tempRelativePosCached = m_tempRelativePos;
            m_tempRelativeRotCached = m_tempRelativeRot;
            m_tempRelativeVelCached = m_tempRelativeVel;
          }
          else if (m_tempParent != m_tempParentCached)
          {
            zdo.UpdateConnection(ZDOExtraData.ConnectionType.SyncTransform, ZDOID.None);
            zdo.Set(ZDOVars.s_attachJointHash, "");
          }
          m_tempParentCached = m_tempParent;
        }
      }
      if (m_syncRotation && transform.hasChanged)
      {
        Quaternion rot = (bool) (Object) m_body ? m_body.rotation : transform.rotation;
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
      if ((bool) (Object) m_body)
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
    if ((bool) (Object) m_character)
      return m_character.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
    if ((bool) (Object) transform.parent)
    {
      ZNetView component = (bool) (Object) transform.parent ? transform.parent.GetComponent<ZNetView>() : null;
      if ((bool) (Object) component && component.IsValid())
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
    if (m_characterParentSync && zdo.HasOwner())
    {
      ZDOID connectionZdoid = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.SyncTransform);
      if (!connectionZdoid.IsNone())
      {
        GameObject instance = ZNetScene.instance.FindInstance(connectionZdoid);
        if ((bool) (Object) instance)
        {
          ZSyncTransform component = instance.GetComponent<ZSyncTransform>();
          if ((bool) (Object) component)
            component.ClientSync(dt);
          string aName = zdo.GetString(ZDOVars.s_attachJointHash);
          Vector3 vec3_1 = zdo.GetVec3(ZDOVars.s_relPosHash, Vector3.zero);
          Quaternion quaternion1 = zdo.GetQuaternion(ZDOVars.s_relRotHash, Quaternion.identity);
          Vector3 vec3_2 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
          bool flag = false;
          if ((int) zdo.DataRevision != (int) m_posRevision)
          {
            m_posRevision = zdo.DataRevision;
            m_targetPosTimer = 0.0f;
          }
          if (aName.Length > 0)
          {
            Transform child = Utils.FindChild(instance.transform, aName);
            if ((bool) (Object) child)
            {
              transform.position = child.position;
              flag = true;
            }
          }
          else
          {
            m_targetPosTimer += dt;
            m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
            Vector3 vector3 = vec3_1 + vec3_2 * m_targetPosTimer;
            if (!m_haveTempRelPos)
            {
              m_haveTempRelPos = true;
              m_tempRelPos = vector3;
            }
            if (Vector3.Distance(m_tempRelPos, vector3) > 1.0 / 1000.0)
            {
              m_tempRelPos = Vector3.Lerp(m_tempRelPos, vector3, 0.2f);
              vector3 = m_tempRelPos;
            }
            Vector3 b = instance.transform.TransformPoint(vector3);
            if (Vector3.Distance(transform.position, b) > 1.0 / 1000.0)
            {
              transform.position = b;
              flag = true;
            }
          }
          Quaternion a = Quaternion.Inverse(instance.transform.rotation) * transform.rotation;
          if (Quaternion.Angle(a, quaternion1) > 1.0 / 1000.0)
          {
            Quaternion quaternion2 = Quaternion.Slerp(a, quaternion1, 0.5f);
            transform.rotation = instance.transform.rotation * quaternion2;
            flag = true;
          }
          usedLocalRotation = true;
          if (!flag || !(bool) (Object) m_body)
            return;
          Physics.SyncTransforms();
          return;
        }
      }
    }
    m_haveTempRelPos = false;
    Vector3 position = zdo.GetPosition();
    if ((int) zdo.DataRevision != (int) m_posRevision)
    {
      m_posRevision = zdo.DataRevision;
      m_targetPosTimer = 0.0f;
    }
    if (zdo.HasOwner())
    {
      m_targetPosTimer += dt;
      m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
      Vector3 vec3 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
      position += vec3 * m_targetPosTimer;
    }
    float num = Vector3.Distance(transform.position, position);
    if (num <= 1.0 / 1000.0)
      return;
    transform.position = num < 5.0 ? Vector3.Lerp(transform.position, position, Mathf.Max(dt, m_targetPosTimer)) : position;
    if (!(bool) (Object) m_body)
      return;
    Physics.SyncTransforms();
  }
  
  public static float smoothTime = 0.5f;

  public void KinematicClientSync(ZDO zdo, float dt)
  {
    if (m_syncPosition)
    {
      Vector3 vector3 = zdo.GetPosition();
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
      Quaternion rotation = zdo.GetRotation();
      if (Quaternion.Angle(m_body.rotation, rotation) > 45.0)
        m_body.rotation = rotation;
      else
        m_body.MoveRotation(rotation);
    }
  }

  public void NonKinematicSync(ZDO zdo, float dt)
  {
    bool usedLocalRotation = false;
    if (m_syncPosition)
      SyncPosition(zdo, dt, out usedLocalRotation);
    if (m_syncRotation && !usedLocalRotation)
    {
      Quaternion rotation = zdo.GetRotation();
      if (Quaternion.Angle(transform.rotation, rotation) > 1.0 / 1000.0)
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, dt * smoothTime);
    }
    if ((bool) (Object) m_body)
    {
      m_body.useGravity = false;
      if (m_syncBodyVelocity && m_nview.HasOwner())
      {
        Vector3 vec3_1 = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
        Vector3 vec3_2 = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
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
    Vector3 vec3 = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
    if (vec3 != Vector3.zero)
    {
      transform.localScale = vec3;
    }
    else
    {
      float num = zdo.GetFloat(ZDOVars.s_scaleScalarHash, transform.localScale.x);
      if (transform.localScale.x.Equals(num))
        return;
      transform.localScale = new Vector3(num, num, num);
    }
  }

  public void ClientSync(float dt)
  {
    ZDO zdo = m_nview.GetZDO();
    if (zdo.IsOwner())
      return;
    int frameCount = Time.frameCount;
    if (m_lastUpdateFrame == frameCount)
      return;
    m_lastUpdateFrame = frameCount;

    if (m_isKinematicBody)
    {
      KinematicClientSync(zdo, dt);
      return;
    }

    NonKinematicSync(zdo, dt);
  }

  public void CustomFixedUpdate(float fixedDeltaTime)
  {
    if (!m_nview.IsValid())
      return;
    ClientSync(fixedDeltaTime);
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    // throw new System.NotImplementedException();
  }

  public void CustomLateUpdate(float deltaTime)
  {
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