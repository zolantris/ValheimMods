// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.RopeAnchorComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  public class RopeAnchorComponent : MonoBehaviour, Interactable, Hoverable
  {
    public float m_maxRopeDistance = 64f;
    internal LineRenderer m_rope;
    internal ZNetView m_nview;
    internal Rigidbody m_rigidbody;
    public static RopeAnchorComponent m_draggingRopeFrom;
    private List<RopeAnchorComponent.Rope> m_ropes = new List<RopeAnchorComponent.Rope>();
    private List<RopeAnchorComponent.Rope> m_updatingRopes = new List<RopeAnchorComponent.Rope>();
    private uint m_zdoDataRevision;
    private float m_lastRopeCheckTime;
    internal static GameObject m_draggingRopeTo;

    private static readonly Dictionary<string, string[]> m_attachmentPoints =
      RopeAnchorComponent.GetAttachmentPoints();

    public void Awake()
    {
      this.m_rigidbody = ((Component)this).GetComponentInParent<Rigidbody>();
      this.m_rope = ((Component)this).GetComponent<LineRenderer>();
      this.m_nview = ((Component)this).GetComponent<ZNetView>();
      ((Component)this).GetComponent<WearNTear>().m_onDestroyed += new Action(this.DestroyAllRopes);
      this.LoadFromZDO();
    }

    private void DestroyAllRopes()
    {
      while (this.m_ropes.Count > 0)
        this.RemoveRopeAt(0);
    }

    public string GetHoverName() => "";

    public string GetHoverText() =>
      m_draggingRopeTo != this
        ? Localization.instance.Localize(
          "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach_to")
        : Localization.instance.Localize(
          "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach");

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
      if (!m_draggingRopeFrom)
      {
        RopeAnchorComponent.m_draggingRopeFrom = this;
        ((Renderer)this.m_rope).enabled = true;
      }
      else if (m_draggingRopeFrom == this)
      {
        /**
         * This fix may not be accurate
         *
         * Previously it was: if (Object.op_Inequality((Object)RopeAnchorComponent.m_draggingRopeTo, (Object)this))
         */
        if (m_draggingRopeTo != this)
          this.AttachRope(RopeAnchorComponent.m_draggingRopeTo,
            this.GetIndexAtLocation(RopeAnchorComponent.m_draggingRopeTo));
        RopeAnchorComponent.m_draggingRopeFrom = (RopeAnchorComponent)null;
        ((Renderer)this.m_rope).enabled = false;
      }
      else
      {
        RopeAnchorComponent.m_draggingRopeFrom.AttachRope(this);
        ((Renderer)RopeAnchorComponent.m_draggingRopeFrom.m_rope).enabled = false;
        RopeAnchorComponent.m_draggingRopeFrom = (RopeAnchorComponent)null;
        ((Renderer)this.m_rope).enabled = false;
      }

      return true;
    }

    private static Dictionary<string, string[]> GetAttachmentPoints() =>
      new Dictionary<string, string[]>()
      {
        {
          "Karve(Clone)",
          new string[5]
          {
            "ship/ropes/mastrope",
            "ship/ropes/RopeAttachLeft_bottom_front",
            "ship/ropes/RopeAttachLeft_bottom_back",
            "ship/ropes/RopeAttachRight_bottom_front (1)",
            "ship/ropes/RopeAttachRight_bottom_back (1)"
          }
        },
        {
          "VikingShip(Clone)",
          new string[8]
          {
            "interactive/front",
            "interactive/mast",
            "interactive/ladder_right",
            "interactive/ladder_left",
            "ship/visual/ropes/RopeAttachLeft_bottom_front",
            "ship/visual/ropes/RopeAttachLeft_bottom_back",
            "ship/visual/ropes/RopeAttachRight_bottom_front (1)",
            "ship/visual/ropes/RopeAttachRight_bottom_back (1)"
          }
        },
        {
          "Deer(Clone)",
          new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" }
        },
        {
          "Boar(Clone)",
          new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" }
        },
        {
          "Boar_piggy(Clone)",
          new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" }
        },
        {
          "Neck(Clone)",
          new string[1] { "Visual/Armature/Hips/Spine/Spine1/Neck" }
        }
      };

    private Transform GetAttachmentTransform(
      GameObject go,
      RopeAnchorComponent.RopeAttachmentTarget target)
    {
      string[] strArray;
      if (RopeAnchorComponent.m_attachmentPoints.TryGetValue(((Object)go).name, out strArray) &&
          target.Index >= (byte)0 && strArray.Length > (int)target.Index)
      {
        Transform attachmentTransform = go.transform.Find(strArray[(int)target.Index]);
        if (attachmentTransform)
          return attachmentTransform;
      }

      return go.transform;
    }

    private byte GetIndexAtLocation(GameObject go)
    {
      ZNetView componentInParent = go.GetComponentInParent<ZNetView>();
      string[] strArray;
      if (!componentInParent ||
          !RopeAnchorComponent.m_attachmentPoints.TryGetValue(
            ((componentInParent).gameObject).name, out strArray))
        return 0;
      Vector3 position = ((Component)Player.m_localPlayer).transform.position;
      byte indexAtLocation = 0;
      float num = float.MaxValue;
      for (int index = 0; index < strArray.Length; ++index)
      {
        string str = strArray[index];
        Transform transform = ((Component)componentInParent).transform.Find(str);
        if (transform)
        {
          Vector3 vector3 = position - transform.position;
          float sqrMagnitude = vector3.sqrMagnitude;
          if ((double)sqrMagnitude < (double)num)
          {
            indexAtLocation = (byte)index;
            num = sqrMagnitude;
          }
        }
      }

      return indexAtLocation;
    }

    private void AttachRope(GameObject gameObject, byte index)
    {
      ZNetView componentInParent = gameObject.GetComponentInParent<ZNetView>();
      if (!componentInParent || componentInParent.m_zdo == null)
        return;
      ZLog.Log((object)string.Format("AttachRope {0}", (object)index));
      RopeAnchorComponent.RopeAttachmentTarget target =
        new RopeAnchorComponent.RopeAttachmentTarget(
          ZDOPersistantID.Instance.GetOrCreatePersistantID(componentInParent.m_zdo), index);
      if (this.RemoveRopeWithID(target))
        return;
      this.CreateNewRope(target);
      this.SaveToZDO();
      this.CheckRopes();
    }

    private void AttachRope(RopeAnchorComponent ropeAnchorComponent)
    {
      int persistantId =
        ZDOPersistantID.Instance.GetOrCreatePersistantID(ropeAnchorComponent.GetParentZDO());
      if (this.RemoveRopeWithID(
            new RopeAnchorComponent.RopeAttachmentTarget(persistantId, (byte)0)) ||
          ropeAnchorComponent == this ||
          ropeAnchorComponent.RemoveRopeWithID(
            new RopeAnchorComponent.RopeAttachmentTarget(persistantId, (byte)0)))
        return;
      this.CreateNewRope(new RopeAnchorComponent.RopeAttachmentTarget(persistantId, (byte)0));
      this.SaveToZDO();
      this.CheckRopes();
    }

    private void CreateNewRope(RopeAnchorComponent.RopeAttachmentTarget target)
    {
      RopeAnchorComponent.Rope rope = new RopeAnchorComponent.Rope()
      {
        m_ropeAnchorTarget = target,
        m_ropeObject = new GameObject("MBRope")
      };
      rope.m_ropeObject.layer = LayerMask.NameToLayer("piece_nonsolid");
      rope.m_collider = rope.m_ropeObject.AddComponent<BoxCollider>();
      rope.m_collider.size = new Vector3(0.1f, 0.1f, 0.1f);
      rope.m_ropeComponent = rope.m_ropeObject.AddComponent<RopeComponent>();
      rope.m_rope = rope.m_ropeObject.AddComponent<LineRenderer>();
      rope.m_rope.widthMultiplier = this.m_rope.widthMultiplier;
      ((Renderer)rope.m_rope).material = ((Renderer)this.m_rope).material;
      rope.m_rope.textureMode = (LineTextureMode)1;
      rope.m_ropeObject.transform.SetParent(((Component)this).transform.parent);
      rope.m_ropeObject.transform.localPosition = Vector3.zero;
      this.m_ropes.Add(rope);
    }

    private ZDO GetParentZDO() =>
      !m_nview || this.m_nview.m_zdo == null
        ? (ZDO)null
        : this.m_nview.m_zdo;

    private bool RemoveRopeWithID(RopeAnchorComponent.RopeAttachmentTarget target)
    {
      for (int index = 0; index < this.m_ropes.Count; ++index)
      {
        if (this.m_ropes[index].m_ropeAnchorTarget == target)
        {
          this.RemoveRopeAt(index);
          return true;
        }
      }

      return false;
    }

    private void SaveToZDO()
    {
      if (!m_nview || this.m_nview.m_zdo == null)
        return;
      ZPackage zpackage = new ZPackage();
      byte num = 2;
      zpackage.Write(num);
      for (int index = 0; index < this.m_ropes.Count; ++index)
      {
        zpackage.Write(this.m_ropes[index].m_ropeAnchorTarget.Id);
        zpackage.Write(this.m_ropes[index].m_ropeAnchorTarget.Index);
      }

      this.m_nview.m_zdo.Set("MBRopeAnchor_Ropes", zpackage.GetArray());
    }

    private void LoadFromZDO()
    {
      if (!Object.op_Implicit((Object)this.m_nview) || this.m_nview.m_zdo == null)
        return;
      List<RopeAnchorComponent.RopeAttachmentTarget> ropeIds =
        new List<RopeAnchorComponent.RopeAttachmentTarget>();
      this.GetRopesFromZDO((ICollection<RopeAnchorComponent.RopeAttachmentTarget>)ropeIds);
      for (int index = 0; index < ropeIds.Count; ++index)
        this.CreateNewRope(ropeIds[index]);
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public void LateUpdate()
    {
      if (!m_nview || this.m_nview.m_zdo == null)
        return;
      if (!this.m_nview.IsOwner() &&
          (int)this.m_nview.m_zdo.DataRevision != (int)this.m_zdoDataRevision)
        this.UpdateRopesFromZDO();
      if (((Renderer)this.m_rope).enabled && Object.op_Implicit((Object)Player.m_localPlayer))
      {
        this.m_rope.SetPosition(0, ((Component)this).transform.position);
        if (m_draggingRopeTo)
        {
          byte indexAtLocation = this.GetIndexAtLocation(RopeAnchorComponent.m_draggingRopeTo);
          Transform attachmentTransform = this.GetAttachmentTransform(
            RopeAnchorComponent.m_draggingRopeTo,
            new RopeAnchorComponent.RopeAttachmentTarget(0, indexAtLocation));
          if (attachmentTransform)
            this.m_rope.SetPosition(1, attachmentTransform.position);
        }
        else
          this.m_rope.SetPosition(1,
            ((Component)((Humanoid)Player.m_localPlayer).m_visEquipment.m_rightHand).transform
            .position);
      }

      if ((double)Time.time - (double)this.m_lastRopeCheckTime > 2.0)
      {
        this.CheckRopes();
        this.m_lastRopeCheckTime = Time.time;
      }

      for (int index = 0; index < this.m_updatingRopes.Count; ++index)
      {
        RopeAnchorComponent.Rope updatingRope = this.m_updatingRopes[index];
        if (updatingRope.m_ropeObject && updatingRope.m_ropeTarget)
        {
          Vector3 vector3 = transform.position - updatingRope.m_targetTransform.position;
          float magnitude = vector3.magnitude;
          if (this.m_nview.IsOwner() && ((double)magnitude > (double)this.m_maxRopeDistance ||
                                         (double)magnitude >
                                         (double)updatingRope.m_ropeAttachDistance + 8.0))
          {
            this.RemoveUpdatingRopeAt(index);
          }
          else
          {
            updatingRope.m_rope.SetPosition(0, ((Component)this).transform.position);
            updatingRope.m_rope.SetPosition(1, updatingRope.m_targetTransform.position);
          }
        }
        else
        {
          this.m_updatingRopes.RemoveAt(index);
          --index;
        }
      }
    }

    private void CheckRopes()
    {
      for (int index = 0; index < this.m_ropes.Count; ++index)
      {
        RopeAnchorComponent.Rope rope1 = this.m_ropes[index];
        if (!rope1.m_ropeTarget)
        {
          rope1.m_ropeTarget = ZDOPersistantID.Instance.GetGameObject(rope1.m_ropeAnchorTarget.Id);
          if (!rope1.m_ropeTarget)
          {
            if (ZNet.instance.IsServer() &&
                ZDOPersistantID.Instance.GetZDO(rope1.m_ropeAnchorTarget.Id) == null)
            {
              this.RemoveRopeAt(index);
              --index;
            }
          }
          else
          {
            Transform attachmentTransform =
              this.GetAttachmentTransform(rope1.m_ropeTarget, rope1.m_ropeAnchorTarget);
            RopeAnchorComponent.Rope rope2 = rope1;
            Vector3 vector3 = transform.position - attachmentTransform.position;
            double magnitude = vector3.magnitude;
            rope2.m_ropeAttachDistance = (float)magnitude;
            rope1.m_targetTransform = attachmentTransform;
            Rigidbody componentInParent = rope1.m_ropeTarget.GetComponentInParent<Rigidbody>();
            if (componentInParent)
            {
              SpringJoint spring = rope1.m_ropeComponent.GetSpring();
              Rigidbody component = ((Component)rope1.m_ropeComponent).GetComponent<Rigidbody>();
              component.isKinematic = true;
              component.useGravity = false;
              spring.maxDistance = rope1.m_ropeAttachDistance + 2f;
              spring.minDistance = 0.0f;
              spring.spring = 10000f;
              ((Joint)spring).connectedBody = componentInParent;
              ((Joint)spring).autoConfigureConnectedAnchor = false;
              ((Joint)spring).anchor =
                ((Component)component).transform.InverseTransformPoint(((Component)this).transform
                  .position);
              ((Joint)spring).connectedAnchor =
                ((Component)componentInParent).transform.InverseTransformPoint(attachmentTransform
                  .position);
            }

            if (!componentInParent && rope1.m_ropeTarget.transform.parent == transform.parent ||
                componentInParent == this.m_rigidbody)
            {
              rope1.m_rope.useWorldSpace = false;
              rope1.m_rope.SetPosition(0,
                ((Component)rope1.m_rope).transform.InverseTransformPoint(((Component)this)
                  .transform.position));
              rope1.m_rope.SetPosition(1,
                ((Component)rope1.m_rope).transform.InverseTransformPoint(rope1.m_targetTransform
                  .position));
            }
            else
              this.m_updatingRopes.Add(rope1);
          }
        }
      }
    }

    private void UpdateRopesFromZDO()
    {
      this.m_zdoDataRevision = this.m_nview.m_zdo.DataRevision;
      HashSet<RopeAnchorComponent.RopeAttachmentTarget> ropeIds =
        new HashSet<RopeAnchorComponent.RopeAttachmentTarget>();
      this.GetRopesFromZDO((ICollection<RopeAnchorComponent.RopeAttachmentTarget>)ropeIds);
      for (int index = 0; index < this.m_ropes.Count; ++index)
      {
        RopeAnchorComponent.Rope rope = this.m_ropes[index];
        if (!ropeIds.Contains(rope.m_ropeAnchorTarget))
        {
          this.RemoveRopeAt(index);
          --index;
        }
      }

      foreach (RopeAnchorComponent.RopeAttachmentTarget attachmentTarget in ropeIds)
      {
        RopeAnchorComponent.RopeAttachmentTarget ropeId = attachmentTarget;
        if (!this.m_ropes.Any<RopeAnchorComponent.Rope>(
              (Func<RopeAnchorComponent.Rope, bool>)(k => k.m_ropeAnchorTarget == ropeId)))
          this.CreateNewRope(ropeId);
      }
    }

    public static int GetRopeTarget(ZDOID zdoid)
    {
      ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
      return zdo == null
        ? ZDOPersistantID.ZDOIDToId(zdoid)
        : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdo);
    }

    private void GetRopesFromZDO(
      ICollection<RopeAnchorComponent.RopeAttachmentTarget> ropeIds)
    {
      try
      {
        byte[] byteArray = this.m_nview.m_zdo.GetByteArray("MBRopeAnchor_Ropes", (byte[])null);
        if (byteArray == null || byteArray.Length == 0)
          return;
        ZPackage zpackage = new ZPackage(byteArray);
        byte num = zpackage.ReadByte();
        while (zpackage.GetPos() < zpackage.Size())
          ropeIds.Add(new RopeAnchorComponent.RopeAttachmentTarget(
            num <= (byte)1
              ? RopeAnchorComponent.GetRopeTarget(zpackage.ReadZDOID())
              : zpackage.ReadInt(), zpackage.ReadByte()));
      }
      catch (Exception ex)
      {
      }
    }

    private void RemoveUpdatingRopeAt(int i)
    {
      int i1 = this.m_ropes.IndexOf(this.m_updatingRopes[i]);
      if (i1 == -1)
        return;
      this.RemoveRopeAt(i1);
    }

    private void RemoveRopeAt(int i)
    {
      Destroy(this.m_ropes[i].m_ropeObject);
      this.m_ropes.RemoveAt(i);
      this.SaveToZDO();
    }

    private class Rope
    {
      internal GameObject m_ropeTarget;
      internal RopeAnchorComponent.RopeAttachmentTarget m_ropeAnchorTarget;
      internal GameObject m_ropeObject;
      internal LineRenderer m_rope;
      internal BoxCollider m_collider;
      internal float m_ropeAttachDistance;
      internal RopeComponent m_ropeComponent;
      internal Transform m_targetTransform;
    }

    private struct RopeAttachmentTarget
    {
      public RopeAttachmentTarget(int id, byte index)
        : this()
      {
        this.Id = id;
        this.Index = index;
      }

      public int Id { get; }

      public byte Index { get; }

      public override int GetHashCode() => this.Id << 1 & (int)this.Index;

      public override bool Equals(object obj) =>
        obj is RopeAnchorComponent.RopeAttachmentTarget attachmentTarget &&
        (int)attachmentTarget.Index == (int)this.Index && attachmentTarget.Id == this.Id;

      public static bool operator ==(
        RopeAnchorComponent.RopeAttachmentTarget a,
        RopeAnchorComponent.RopeAttachmentTarget b)
      {
        return (int)a.Index == (int)b.Index && a.Id == b.Id;
      }

      public static bool operator !=(
        RopeAnchorComponent.RopeAttachmentTarget a,
        RopeAnchorComponent.RopeAttachmentTarget b)
      {
        return (int)a.Index != (int)b.Index || a.Id != b.Id;
      }
    }
  }
}