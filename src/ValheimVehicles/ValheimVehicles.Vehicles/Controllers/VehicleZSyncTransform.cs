using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    this.m_nview = this.GetComponent<ZNetView>();
    this.m_body = this.GetComponent<Rigidbody>();
    this.m_projectile = this.GetComponent<Projectile>();
    this.m_character = this.GetComponent<Character>();
    if (this.m_nview.GetZDO() == null)
    {
      this.enabled = false;
    }
    else
    {
      if ((bool) (Object) this.m_body)
      {
        this.m_isKinematicBody = this.m_body.isKinematic;
        this.m_useGravity = this.m_body.useGravity;
      }
      this.m_wasOwner = this.m_nview.GetZDO().IsOwner();
    }
  }

  public void OnEnable() => ZSyncTransform.Instances.Add((IMonoUpdater) this);

  public void OnDisable() => ZSyncTransform.Instances.Remove((IMonoUpdater) this);

  public Vector3 GetVelocity()
  {
    if ((Object) this.m_body != (Object) null)
      return this.m_body.velocity;
    return (Object) this.m_projectile != (Object) null ? this.m_projectile.GetVelocity() : Vector3.zero;
  }

  public Vector3 GetPosition()
  {
    return !(bool) (Object) this.m_body ? this.transform.position : this.m_body.position;
  }

  public void OwnerSync()
  {
    ZDO zdo = this.m_nview.GetZDO();
    bool flag1 = zdo.IsOwner();
    bool flag2 = !this.m_wasOwner & flag1;
    this.m_wasOwner = flag1;
    if (!flag1)
      return;
    if (flag2)
    {
      bool flag3 = false;
      if (this.m_syncPosition)
      {
        this.transform.position = zdo.GetPosition();
        flag3 = true;
      }
      if (this.m_syncRotation)
      {
        this.transform.rotation = zdo.GetRotation();
        flag3 = true;
      }
      if (this.m_syncBodyVelocity && (bool) (Object) this.m_body)
      {
        this.m_body.velocity = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
        this.m_body.angularVelocity = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
      }
      if (flag3 && (bool) (Object) this.m_body)
        Physics.SyncTransforms();
    }
    if ((double) this.transform.position.y < -5000.0)
    {
      if ((bool) (Object) this.m_body)
        this.m_body.velocity = Vector3.zero;
      ZLog.Log((object) ("Object fell out of world:" + this.gameObject.name));
      float groundHeight = ZoneSystem.instance.GetGroundHeight(this.transform.position);
      this.transform.position = this.transform.position with
      {
        y = groundHeight + 1f
      };
      if (!(bool) (Object) this.m_body)
        return;
      Physics.SyncTransforms();
    }
    else
    {
      if (this.m_syncPosition)
      {
        Vector3 position = this.GetPosition();
        if (!this.m_positionCached.Equals(position))
          zdo.SetPosition(position);
        Vector3 velocity = this.GetVelocity();
        if (!this.m_velocityCached.Equals(velocity))
          zdo.Set(ZDOVars.s_velHash, velocity);
        this.m_positionCached = position;
        this.m_velocityCached = velocity;
        if (this.m_characterParentSync)
        {
          if (this.GetRelativePosition(zdo, out this.m_tempParent, out this.m_tempAttachJoint, out this.m_tempRelativePos, out this.m_tempRelativeRot, out this.m_tempRelativeVel))
          {
            if (this.m_tempParent != this.m_tempParentCached)
            {
              zdo.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, this.m_tempParent);
              zdo.Set(ZDOVars.s_attachJointHash, this.m_tempAttachJoint);
            }
            if (!this.m_tempRelativePos.Equals(this.m_tempRelativePosCached))
              zdo.Set(ZDOVars.s_relPosHash, this.m_tempRelativePos);
            if (!this.m_tempRelativeRot.Equals(this.m_tempRelativeRotCached))
              zdo.Set(ZDOVars.s_relRotHash, this.m_tempRelativeRot);
            if (!this.m_tempRelativeVel.Equals(this.m_tempRelativeVelCached))
              zdo.Set(ZDOVars.s_velHash, this.m_tempRelativeVel);
            this.m_tempRelativePosCached = this.m_tempRelativePos;
            this.m_tempRelativeRotCached = this.m_tempRelativeRot;
            this.m_tempRelativeVelCached = this.m_tempRelativeVel;
          }
          else if (this.m_tempParent != this.m_tempParentCached)
          {
            zdo.UpdateConnection(ZDOExtraData.ConnectionType.SyncTransform, ZDOID.None);
            zdo.Set(ZDOVars.s_attachJointHash, "");
          }
          this.m_tempParentCached = this.m_tempParent;
        }
      }
      if (this.m_syncRotation && this.transform.hasChanged)
      {
        Quaternion rot = (bool) (Object) this.m_body ? this.m_body.rotation : this.transform.rotation;
        zdo.SetRotation(rot);
      }
      if (this.m_syncScale && this.transform.hasChanged)
      {
        if (Mathf.Approximately(this.transform.localScale.x, this.transform.localScale.y) && Mathf.Approximately(this.transform.localScale.x, this.transform.localScale.z))
        {
          zdo.RemoveVec3(ZDOVars.s_scaleHash);
          zdo.Set(ZDOVars.s_scaleScalarHash, this.transform.localScale.x);
        }
        else
        {
          zdo.RemoveFloat(ZDOVars.s_scaleScalarHash);
          zdo.Set(ZDOVars.s_scaleHash, this.transform.localScale);
        }
      }
      if ((bool) (Object) this.m_body)
      {
        if (this.m_syncBodyVelocity)
        {
          this.m_nview.GetZDO().Set(ZDOVars.s_bodyVelHash, this.m_body.velocity);
          this.m_nview.GetZDO().Set(ZDOVars.s_bodyAVelHash, this.m_body.angularVelocity);
        }
        this.m_body.useGravity = this.m_useGravity;
      }
      this.transform.hasChanged = false;
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
    if ((bool) (Object) this.m_character)
      return this.m_character.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
    if ((bool) (Object) this.transform.parent)
    {
      ZNetView component = (bool) (Object) this.transform.parent ? this.transform.parent.GetComponent<ZNetView>() : (ZNetView) null;
      if ((bool) (Object) component && component.IsValid())
      {
        parent = component.GetZDO().m_uid;
        attachJoint = "";
        relativePos = this.transform.localPosition;
        relativeRot = this.transform.localRotation;
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
    if (this.m_characterParentSync && zdo.HasOwner())
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
          if ((int) zdo.DataRevision != (int) this.m_posRevision)
          {
            this.m_posRevision = zdo.DataRevision;
            this.m_targetPosTimer = 0.0f;
          }
          if (aName.Length > 0)
          {
            Transform child = Utils.FindChild(instance.transform, aName);
            if ((bool) (Object) child)
            {
              this.transform.position = child.position;
              flag = true;
            }
          }
          else
          {
            this.m_targetPosTimer += dt;
            this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
            Vector3 vector3 = vec3_1 + vec3_2 * this.m_targetPosTimer;
            if (!this.m_haveTempRelPos)
            {
              this.m_haveTempRelPos = true;
              this.m_tempRelPos = vector3;
            }
            if ((double) Vector3.Distance(this.m_tempRelPos, vector3) > 1.0 / 1000.0)
            {
              this.m_tempRelPos = Vector3.Lerp(this.m_tempRelPos, vector3, 0.2f);
              vector3 = this.m_tempRelPos;
            }
            Vector3 b = instance.transform.TransformPoint(vector3);
            if ((double) Vector3.Distance(this.transform.position, b) > 1.0 / 1000.0)
            {
              this.transform.position = b;
              flag = true;
            }
          }
          Quaternion a = Quaternion.Inverse(instance.transform.rotation) * this.transform.rotation;
          if ((double) Quaternion.Angle(a, quaternion1) > 1.0 / 1000.0)
          {
            Quaternion quaternion2 = Quaternion.Slerp(a, quaternion1, 0.5f);
            this.transform.rotation = instance.transform.rotation * quaternion2;
            flag = true;
          }
          usedLocalRotation = true;
          if (!flag || !(bool) (Object) this.m_body)
            return;
          Physics.SyncTransforms();
          return;
        }
      }
    }
    this.m_haveTempRelPos = false;
    Vector3 position = zdo.GetPosition();
    if ((int) zdo.DataRevision != (int) this.m_posRevision)
    {
      this.m_posRevision = zdo.DataRevision;
      this.m_targetPosTimer = 0.0f;
    }
    if (zdo.HasOwner())
    {
      this.m_targetPosTimer += dt;
      this.m_targetPosTimer = Mathf.Min(this.m_targetPosTimer, 2f);
      Vector3 vec3 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
      position += vec3 * this.m_targetPosTimer;
    }
    float num = Vector3.Distance(this.transform.position, position);
    if ((double) num <= 1.0 / 1000.0)
      return;
    this.transform.position = (double) num < 5.0 ? Vector3.Lerp(this.transform.position, position, 0.2f) : position;
    if (!(bool) (Object) this.m_body)
      return;
    Physics.SyncTransforms();
  }

  public void ClientSync(float dt)
  {
    ZDO zdo = this.m_nview.GetZDO();
    if (zdo.IsOwner())
      return;
    int frameCount = Time.frameCount;
    if (this.m_lastUpdateFrame == frameCount)
      return;
    this.m_lastUpdateFrame = frameCount;
    if (this.m_isKinematicBody)
    {
      if (this.m_syncPosition)
      {
        Vector3 vector3 = zdo.GetPosition();
        if ((double) Vector3.Distance(this.m_body.position, vector3) > 5.0)
        {
          this.m_body.position = vector3;
        }
        else
        {
          if ((double) Vector3.Distance(this.m_body.position, vector3) > 0.009999999776482582)
            vector3 = Vector3.Lerp(this.m_body.position, vector3, 0.2f);
          this.m_body.MovePosition(vector3);
        }
      }
      if (this.m_syncRotation)
      {
        Quaternion rotation = zdo.GetRotation();
        if ((double) Quaternion.Angle(this.m_body.rotation, rotation) > 45.0)
          this.m_body.rotation = rotation;
        else
          this.m_body.MoveRotation(rotation);
      }
    }
    else
    {
      bool usedLocalRotation = false;
      if (this.m_syncPosition)
        this.SyncPosition(zdo, dt, out usedLocalRotation);
      if (this.m_syncRotation && !usedLocalRotation)
      {
        Quaternion rotation = zdo.GetRotation();
        if ((double) Quaternion.Angle(this.transform.rotation, rotation) > 1.0 / 1000.0)
          this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rotation, 0.5f);
      }
      if ((bool) (Object) this.m_body)
      {
        this.m_body.useGravity = false;
        if (this.m_syncBodyVelocity && this.m_nview.HasOwner())
        {
          Vector3 vec3_1 = zdo.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
          Vector3 vec3_2 = zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
          if ((double) vec3_1.magnitude > 0.009999999776482582 || (double) vec3_2.magnitude > 0.009999999776482582)
          {
            this.m_body.velocity = vec3_1;
            this.m_body.angularVelocity = vec3_2;
          }
          else
            this.m_body.Sleep();
        }
        else if (!this.m_body.IsSleeping())
        {
          this.m_body.velocity = Vector3.zero;
          this.m_body.angularVelocity = Vector3.zero;
          this.m_body.Sleep();
        }
      }
    }
    if (!this.m_syncScale)
      return;
    Vector3 vec3 = zdo.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
    if (vec3 != Vector3.zero)
    {
      this.transform.localScale = vec3;
    }
    else
    {
      float num = zdo.GetFloat(ZDOVars.s_scaleScalarHash, this.transform.localScale.x);
      if (this.transform.localScale.x.Equals(num))
        return;
      this.transform.localScale = new Vector3(num, num, num);
    }
  }

  public void CustomFixedUpdate(float fixedDeltaTime)
  {
    if (!this.m_nview.IsValid())
      return;
    this.ClientSync(fixedDeltaTime);
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    // throw new System.NotImplementedException();
  }

  public void CustomLateUpdate(float deltaTime)
  {
    if (!this.m_nview.IsValid())
      return;
    this.OwnerSync();
  }

  public void SyncNow()
  {
    if (!this.m_nview.IsValid())
      return;
    this.OwnerSync();
  }
}