using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class RopeAnchorComponent : MonoBehaviour, Interactable, Hoverable
{
  private class Rope
  {
    internal GameObject m_ropeTarget;

    internal RopeAttachmentTarget m_ropeAnchorTarget;

    internal GameObject m_ropeObject;

    internal LineRenderer m_rope;

    internal BoxCollider m_collider;

    internal float m_ropeAttachDistance;

    internal RopeComponent m_ropeComponent;

    internal Transform m_targetTransform;
  }

  private struct RopeAttachmentTarget
  {
    public int Id { get; }

    public byte Index { get; }

    public RopeAttachmentTarget(int id, byte index)
    {
      this = default(RopeAttachmentTarget);
      Id = id;
      Index = index;
    }

    public override int GetHashCode()
    {
      return (Id << 1) & Index;
    }

    public override bool Equals(object obj)
    {
      return obj is RopeAttachmentTarget target && target.Index == Index && target.Id == Id;
    }

    public static bool operator ==(RopeAttachmentTarget a, RopeAttachmentTarget b)
    {
      return a.Index == b.Index && a.Id == b.Id;
    }

    public static bool operator !=(RopeAttachmentTarget a, RopeAttachmentTarget b)
    {
      return a.Index != b.Index || a.Id != b.Id;
    }
  }

  public float m_maxRopeDistance = 64f;

  internal LineRenderer m_rope;

  internal ZNetView m_nview;

  internal Rigidbody m_rigidbody;

  public static RopeAnchorComponent m_draggingRopeFrom;

  private List<Rope> m_ropes = new List<Rope>();

  private List<Rope> m_updatingRopes = new List<Rope>();

  private uint m_zdoDataRevision;

  private float m_lastRopeCheckTime;

  internal static GameObject m_draggingRopeTo;

  private static readonly Dictionary<string, string[]> m_attachmentPoints = GetAttachmentPoints();

  public void Awake()
  {
    m_rigidbody = GetComponentInParent<Rigidbody>();
    m_rope = GetComponent<LineRenderer>();
    m_nview = GetComponent<ZNetView>();
    WearNTear wnt = GetComponent<WearNTear>();
    if (!wnt)
    {
      if (m_nview?.m_zdo != null) m_nview?.m_zdo.Reset();
      Destroy(this);
      return;
    }

    wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(DestroyAllRopes));
    LoadFromZDO();
  }

  private void DestroyAllRopes()
  {
    while (m_ropes.Count > 0)
    {
      RemoveRopeAt(0);
    }
  }

  public string GetHoverName()
  {
    return "";
  }

  public string GetHoverText()
  {
    if (m_draggingRopeTo != this)
    {
      return Localization.instance.Localize(
        "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach_to");
    }

    return Localization.instance.Localize(
      "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach");
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!m_draggingRopeFrom)
    {
      m_draggingRopeFrom = this;
      m_rope.enabled = true;
    }
    else if (m_draggingRopeFrom == this)
    {
      if (m_draggingRopeTo != this)
      {
        AttachRope(m_draggingRopeTo, GetIndexAtLocation(m_draggingRopeTo));
      }

      m_draggingRopeFrom = null;
      m_rope.enabled = false;
    }
    else
    {
      m_draggingRopeFrom.AttachRope(this);
      m_draggingRopeFrom.m_rope.enabled = false;
      m_draggingRopeFrom = null;
      m_rope.enabled = false;
    }

    return true;
  }

  private static Dictionary<string, string[]> GetAttachmentPoints()
  {
    Dictionary<string, string[]> attachmentPoints = new Dictionary<string, string[]>();
    attachmentPoints.Add("Karve(Clone)",
      new string[5]
      {
        "ship/ropes/mastrope", "ship/ropes/RopeAttachLeft_bottom_front",
        "ship/ropes/RopeAttachLeft_bottom_back", "ship/ropes/RopeAttachRight_bottom_front (1)",
        "ship/ropes/RopeAttachRight_bottom_back (1)"
      });
    attachmentPoints.Add("VikingShip(Clone)",
      new string[8]
      {
        "interactive/front", "interactive/mast", "interactive/ladder_right",
        "interactive/ladder_left", "ship/visual/ropes/RopeAttachLeft_bottom_front",
        "ship/visual/ropes/RopeAttachLeft_bottom_back",
        "ship/visual/ropes/RopeAttachRight_bottom_front (1)",
        "ship/visual/ropes/RopeAttachRight_bottom_back (1)"
      });
    attachmentPoints.Add("Deer(Clone)",
      new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" });
    attachmentPoints.Add("Boar(Clone)",
      new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" });
    attachmentPoints.Add("Boar_piggy(Clone)",
      new string[1] { "Visual/CG/Pelvis/Spine/Spine1/Spine2/Neck" });
    attachmentPoints.Add("Neck(Clone)", new string[1] { "Visual/Armature/Hips/Spine/Spine1/Neck" });
    return attachmentPoints;
  }

  private Transform GetAttachmentTransform(GameObject go, RopeAttachmentTarget target)
  {
    if (m_attachmentPoints.TryGetValue(go.name, out var points) && target.Index >= 0 &&
        points.Length > target.Index)
    {
      Transform t = go.transform.Find(points[target.Index]);
      if ((bool)t)
      {
        return t;
      }
    }

    return go.transform;
  }

  private byte GetIndexAtLocation(GameObject go)
  {
    ZNetView netview = go.GetComponentInParent<ZNetView>();
    if (!netview)
    {
      return 0;
    }

    if (m_attachmentPoints.TryGetValue(netview.gameObject.name, out var points))
    {
      Vector3 playerPos = Player.m_localPlayer.transform.position;
      byte index = 0;
      float distance = float.MaxValue;
      for (int i = 0; i < points.Length; i++)
      {
        string point = points[i];
        Transform t = netview.transform.Find(point);
        if ((bool)t)
        {
          float d = (playerPos - t.position).sqrMagnitude;
          if (d < distance)
          {
            index = (byte)i;
            distance = d;
          }
        }
      }

      return index;
    }

    return 0;
  }

  private void AttachRope(GameObject gameObject, byte index)
  {
    ZNetView nv = gameObject.GetComponentInParent<ZNetView>();
    if ((bool)nv && nv.m_zdo != null)
    {
      Logger.LogDebug($"AttachRope {index}");
      RopeAttachmentTarget id =
        new RopeAttachmentTarget(ZDOPersistantID.Instance.GetOrCreatePersistantID(nv.m_zdo), index);
      if (!RemoveRopeWithID(id))
      {
        CreateNewRope(id);
        SaveToZDO();
        CheckRopes();
      }
    }
  }

  private void AttachRope(RopeAnchorComponent ropeAnchorComponent)
  {
    int parentid =
      ZDOPersistantID.Instance.GetOrCreatePersistantID(ropeAnchorComponent.GetParentZDO());
    if (!RemoveRopeWithID(new RopeAttachmentTarget(parentid, 0)) &&
        !(ropeAnchorComponent == this) &&
        !ropeAnchorComponent.RemoveRopeWithID(new RopeAttachmentTarget(parentid, 0)))
    {
      CreateNewRope(new RopeAttachmentTarget(parentid, 0));
      SaveToZDO();
      CheckRopes();
    }
  }

  private void CreateNewRope(RopeAttachmentTarget target)
  {
    Rope newRope = new Rope();
    newRope.m_ropeAnchorTarget = target;
    newRope.m_ropeObject = new GameObject("MBRope");
    newRope.m_ropeObject.layer = LayerMask.NameToLayer("piece_nonsolid");
    newRope.m_collider = newRope.m_ropeObject.AddComponent<BoxCollider>();
    newRope.m_collider.size = new Vector3(0.1f, 0.1f, 0.1f);
    newRope.m_ropeComponent = newRope.m_ropeObject.AddComponent<RopeComponent>();
    newRope.m_rope = newRope.m_ropeObject.AddComponent<LineRenderer>();
    newRope.m_rope.widthMultiplier = m_rope.widthMultiplier;
    newRope.m_rope.material = m_rope.material;
    newRope.m_rope.textureMode = LineTextureMode.Tile;
    newRope.m_ropeObject.transform.SetParent(base.transform.parent);
    newRope.m_ropeObject.transform.localPosition = Vector3.zero;
    m_ropes.Add(newRope);
  }

  private ZDO GetParentZDO()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      return null;
    }

    return m_nview.m_zdo;
  }

  private bool RemoveRopeWithID(RopeAttachmentTarget target)
  {
    for (int i = 0; i < m_ropes.Count; i++)
    {
      if (m_ropes[i].m_ropeAnchorTarget == target)
      {
        RemoveRopeAt(i);
        return true;
      }
    }

    return false;
  }

  private void SaveToZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      ZPackage pkg = new ZPackage();
      byte version = 2;
      pkg.Write(version);
      for (int i = 0; i < m_ropes.Count; i++)
      {
        pkg.Write(m_ropes[i].m_ropeAnchorTarget.Id);
        pkg.Write(m_ropes[i].m_ropeAnchorTarget.Index);
      }

      m_nview.m_zdo.Set("MBRopeAnchor_Ropes", pkg.GetArray());
    }
  }

  private void LoadFromZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      List<RopeAttachmentTarget> ropeIds = new List<RopeAttachmentTarget>();
      GetRopesFromZDO(ropeIds);
      for (int i = 0; i < ropeIds.Count; i++)
      {
        CreateNewRope(ropeIds[i]);
      }
    }
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void LateUpdate()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      return;
    }

    if (!m_nview.IsOwner() && m_nview.m_zdo.DataRevision != m_zdoDataRevision)
    {
      UpdateRopesFromZDO();
    }

    if (m_rope.enabled && (bool)Player.m_localPlayer)
    {
      m_rope.SetPosition(0, base.transform.position);
      if ((bool)m_draggingRopeTo)
      {
        byte index = GetIndexAtLocation(m_draggingRopeTo);
        Transform t = GetAttachmentTransform(m_draggingRopeTo, new RopeAttachmentTarget(0, index));
        if ((bool)t)
        {
          m_rope.SetPosition(1, t.position);
        }
      }
      else
      {
        m_rope.SetPosition(1,
          ((Humanoid)Player.m_localPlayer).m_visEquipment.m_rightHand.transform.position);
      }
    }

    if (Time.time - m_lastRopeCheckTime > 2f)
    {
      CheckRopes();
      m_lastRopeCheckTime = Time.time;
    }

    for (int i = 0; i < m_updatingRopes.Count; i++)
    {
      Rope rope = m_updatingRopes[i];
      if ((bool)rope.m_ropeObject && (bool)rope.m_ropeTarget)
      {
        float ropeDistance = (base.transform.position - rope.m_targetTransform.position).magnitude;
        if (m_nview.IsOwner() && (ropeDistance > m_maxRopeDistance ||
                                  ropeDistance > rope.m_ropeAttachDistance + 8f))
        {
          RemoveUpdatingRopeAt(i);
          continue;
        }

        rope.m_rope.SetPosition(0, base.transform.position);
        rope.m_rope.SetPosition(1, rope.m_targetTransform.position);
      }
      else
      {
        m_updatingRopes.RemoveAt(i);
        i--;
      }
    }
  }

  private void CheckRopes()
  {
    for (int i = 0; i < m_ropes.Count; i++)
    {
      Rope rope = m_ropes[i];
      if ((bool)rope.m_ropeTarget)
      {
        continue;
      }

      rope.m_ropeTarget = ZDOPersistantID.Instance.GetGameObject(rope.m_ropeAnchorTarget.Id);
      if (!rope.m_ropeTarget)
      {
        if (ZNet.instance.IsServer())
        {
          ZDO zdo = ZDOPersistantID.Instance.GetZDO(rope.m_ropeAnchorTarget.Id);
          if (zdo == null)
          {
            RemoveRopeAt(i);
            i--;
          }
        }

        continue;
      }

      Transform targetTransform =
        GetAttachmentTransform(rope.m_ropeTarget, rope.m_ropeAnchorTarget);
      rope.m_ropeAttachDistance = (base.transform.position - targetTransform.position).magnitude;
      rope.m_targetTransform = targetTransform;
      Rigidbody rb = rope.m_ropeTarget.GetComponentInParent<Rigidbody>();
      if ((bool)rb)
      {
        SpringJoint spring = rope.m_ropeComponent.GetSpring();
        Rigidbody springRb = rope.m_ropeComponent.GetComponent<Rigidbody>();
        springRb.isKinematic = true;
        springRb.useGravity = false;
        spring.maxDistance = rope.m_ropeAttachDistance + 2f;
        spring.minDistance = 0f;
        spring.spring = 10000f;
        spring.connectedBody = rb;
        spring.autoConfigureConnectedAnchor = false;
        spring.anchor = springRb.transform.InverseTransformPoint(base.transform.position);
        spring.connectedAnchor = rb.transform.InverseTransformPoint(targetTransform.position);
      }

      if ((!rb && rope.m_ropeTarget.transform.parent == base.transform.parent) || rb == m_rigidbody)
      {
        rope.m_rope.useWorldSpace = false;
        rope.m_rope.SetPosition(0,
          rope.m_rope.transform.InverseTransformPoint(base.transform.position));
        rope.m_rope.SetPosition(1,
          rope.m_rope.transform.InverseTransformPoint(rope.m_targetTransform.position));
      }
      else
      {
        m_updatingRopes.Add(rope);
      }
    }
  }

  private void UpdateRopesFromZDO()
  {
    m_zdoDataRevision = m_nview.m_zdo.DataRevision;
    HashSet<RopeAttachmentTarget> ropeIds = new HashSet<RopeAttachmentTarget>();
    GetRopesFromZDO(ropeIds);
    for (int i = 0; i < m_ropes.Count; i++)
    {
      Rope rope = m_ropes[i];
      if (!ropeIds.Contains(rope.m_ropeAnchorTarget))
      {
        RemoveRopeAt(i);
        i--;
      }
    }

    foreach (RopeAttachmentTarget ropeId in ropeIds)
    {
      if (!m_ropes.Any((Rope k) => k.m_ropeAnchorTarget == ropeId))
      {
        CreateNewRope(ropeId);
      }
    }
  }

  public static int GetRopeTarget(ZDOID zdoid)
  {
    ZDO zdoparent = ZDOMan.instance.GetZDO(zdoid);
    if (zdoparent != null)
    {
      return ZDOPersistantID.Instance.GetOrCreatePersistantID(zdoparent);
    }

    return ZDOPersistantID.ZDOIDToId(zdoid);
  }

  private void GetRopesFromZDO(ICollection<RopeAttachmentTarget> ropeIds)
  {
    try
    {
      byte[] bytesPkg = m_nview.m_zdo.GetByteArray("MBRopeAnchor_Ropes");
      if (bytesPkg != null && bytesPkg.Length != 0)
      {
        ZPackage pkg = new ZPackage(bytesPkg);
        byte version = pkg.ReadByte();
        while (pkg.GetPos() < pkg.Size())
        {
          ropeIds.Add(new RopeAttachmentTarget(
            (version <= 1) ? GetRopeTarget(pkg.ReadZDOID()) : pkg.ReadInt(), pkg.ReadByte()));
        }
      }
    }
    catch (Exception)
    {
    }
  }

  private void RemoveUpdatingRopeAt(int i)
  {
    int index = m_ropes.IndexOf(m_updatingRopes[i]);
    if (index != -1)
    {
      RemoveRopeAt(index);
    }
  }

  private void RemoveRopeAt(int i)
  {
    UnityEngine.Object.Destroy(m_ropes[i].m_ropeObject);
    m_ropes.RemoveAt(i);
    SaveToZDO();
  }
}