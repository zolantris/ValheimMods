using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ValheimRAFT.Util;

using ZdoWatcher;
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
      this = default;
      Id = id;
      Index = index;
    }

    public override int GetHashCode()
    {
      return (Id << 1) & Index;
    }

    public override bool Equals(object obj)
    {
      return obj is RopeAttachmentTarget target && target.Index == Index &&
             target.Id == Id;
    }

    public static bool operator ==(RopeAttachmentTarget a,
      RopeAttachmentTarget b)
    {
      return a.Index == b.Index && a.Id == b.Id;
    }

    public static bool operator !=(RopeAttachmentTarget a,
      RopeAttachmentTarget b)
    {
      return a.Index != b.Index || a.Id != b.Id;
    }
  }

  public float m_maxRopeDistance = 64f;

  internal LineRenderer m_rope;

  internal ZNetView m_nview;

  internal Rigidbody m_rigidbody;

  public static RopeAnchorComponent? m_draggingRopeFrom;

  private List<Rope> m_ropes = new();

  private List<Rope> m_updatingRopes = new();

  private uint m_zdoDataRevision;

  private float m_lastRopeCheckTime;
  
  internal static GameObject m_draggingRopeTo;

  public bool isHauling = false;
  private static string AttachToText = "";
  private static string StartAttachText = "";
  private static string HaulingStartText = "";
  private static string HaulingStopText = ""; 
  

  private static readonly Dictionary<string, string[]> m_attachmentPoints =
    GetAttachmentPoints();

  public void Awake()
  {
    m_rigidbody = GetComponentInParent<Rigidbody>();
    m_rope = GetComponent<LineRenderer>();
    m_nview = GetComponent<ZNetView>();
    var wnt = GetComponent<WearNTear>();
    if (!wnt)
    {
      if (m_nview?.m_zdo != null) m_nview?.m_zdo.Reset();
      Destroy(this);
      return;
    }

    wnt.m_onDestroyed =
      (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(DestroyAllRopes));
    LoadFromZDO();

    SetupLocalization();
    Localization.OnLanguageChange += SetupLocalization;
  }

  public void OnDestroy()
  {
    Localization.OnLanguageChange -= SetupLocalization;
  }

  private static void SetupLocalization()
  {
    if (Localization.instance == null) return;

    // alt key for hauling vehicles
    if (HaulingStartText == string.Empty)
    {
      HaulingStartText = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $valheim_vehicles_haul_vehicle_start");
    }

    // stop does not need a alt key
    if (HaulingStopText == string.Empty)
    {
      HaulingStopText = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_haul_vehicle_stop");
    }

    if (AttachToText == string.Empty)
    {
      AttachToText = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach_to");
    }


    if (StartAttachText == string.Empty)
    {
      StartAttachText = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_Use</b></color>] $mb_rope_anchor_attach");
    }
  }

  private void DestroyAllRopes()
  {
    while (m_ropes.Count > 0) RemoveRopeAt(0);
  }

  public string GetHoverName()
  {
    return "";
  }
  

  public string GetHoverText()
  {
    if (isHauling)
    {
      return HaulingStopText;
    }
    
    if (m_draggingRopeTo != this)
      return $"{StartAttachText}\n{HaulingStartText}";

    return AttachToText;
  }

  public void StopHauling()
  {
    m_rope.enabled = false;
    m_draggingRopeFrom = null;
    isHauling = false;
  }

  public void ToggleHaulingVehicle(Humanoid user)
  {
    isHauling = !isHauling;
    if (!isHauling)
    {
      StopHauling();
    }
    m_draggingRopeFrom = isHauling ? this : null;
    m_rope.enabled = isHauling;

    var player = user.GetComponent<Player>();
    if (player == null) return;
    var piecesController = GetComponentInParent<VehiclePiecesController>();
    if (piecesController == null || piecesController.MovementController == null) return;
    piecesController.MovementController.SetHaulingVehicle(player, this, isHauling);
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (hold && m_draggingRopeFrom)
    {
      m_draggingRopeFrom = null;
      m_rope.enabled = false;
      return false;
    }

    if (alt || isHauling)
    {
      ToggleHaulingVehicle(user);
      return true;
    }

    if (!m_draggingRopeFrom)
    {
      m_draggingRopeFrom = this;
      m_rope.enabled = true;
    }
    else if (m_draggingRopeFrom == this)
    {
      if (m_draggingRopeTo != this)
        AttachRope(m_draggingRopeTo, GetIndexAtLocation(m_draggingRopeTo));

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
    Dictionary<string, string[]> attachmentPoints = new();
    attachmentPoints.Add("Karve(Clone)",
      new string[5]
      {
        "ship/ropes/mastrope", "ship/ropes/RopeAttachLeft_bottom_front",
        "ship/ropes/RopeAttachLeft_bottom_back",
        "ship/ropes/RopeAttachRight_bottom_front (1)",
        "ship/ropes/RopeAttachRight_bottom_back (1)"
      });
    attachmentPoints.Add("VikingShip(Clone)",
      new string[8]
      {
        "interactive/front", "interactive/mast", "interactive/ladder_right",
        "interactive/ladder_left",
        "ship/visual/ropes/RopeAttachLeft_bottom_front",
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
    attachmentPoints.Add("Neck(Clone)",
      new string[1] { "Visual/Armature/Hips/Spine/Spine1/Neck" });
    return attachmentPoints;
  }

  private Transform GetAttachmentTransform(GameObject go,
    RopeAttachmentTarget target)
  {
    if (m_attachmentPoints.TryGetValue(go.name, out var points) &&
        target.Index >= 0 &&
        points.Length > target.Index)
    {
      var t = go.transform.Find(points[target.Index]);
      if ((bool)t) return t;
    }

    return go.transform;
  }

  private byte GetIndexAtLocation(GameObject go)
  {
    var netview = go.GetComponentInParent<ZNetView>();
    if (!netview) return 0;

    if (m_attachmentPoints.TryGetValue(netview.gameObject.name, out var points))
    {
      var playerPos = Player.m_localPlayer.transform.position;
      byte index = 0;
      var distance = float.MaxValue;
      for (var i = 0; i < points.Length; i++)
      {
        var point = points[i];
        var t = netview.transform.Find(point);
        if ((bool)t)
        {
          var d = (playerPos - t.position).sqrMagnitude;
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
    var nv = gameObject.GetComponentInParent<ZNetView>();
    if ((bool)nv && nv.m_zdo != null)
    {
      Logger.LogDebug($"AttachRope {index}");
      var id =
        new RopeAttachmentTarget(
          ZdoWatchController.Instance.GetOrCreatePersistentID(nv.m_zdo),
          index);
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
    var parentid =
      ZdoWatchController.Instance.GetOrCreatePersistentID(ropeAnchorComponent
        .GetParentZDO());
    if (!RemoveRopeWithID(new RopeAttachmentTarget(parentid, 0)) &&
        !(ropeAnchorComponent == this) &&
        !ropeAnchorComponent.RemoveRopeWithID(
          new RopeAttachmentTarget(parentid, 0)))
    {
      CreateNewRope(new RopeAttachmentTarget(parentid, 0));
      SaveToZDO();
      CheckRopes();
    }
  }

  private void CreateNewRope(RopeAttachmentTarget target)
  {
    var newRope = new Rope();
    newRope.m_ropeAnchorTarget = target;
    newRope.m_ropeObject = new GameObject("MBRope");
    newRope.m_ropeObject.layer = LayerMask.NameToLayer("piece_nonsolid");
    newRope.m_collider = newRope.m_ropeObject.AddComponent<BoxCollider>();
    newRope.m_collider.size = new Vector3(0.1f, 0.1f, 0.1f);
    newRope.m_ropeComponent =
      newRope.m_ropeObject.AddComponent<RopeComponent>();
    newRope.m_rope = newRope.m_ropeObject.AddComponent<LineRenderer>();
    newRope.m_rope.widthMultiplier = m_rope.widthMultiplier;
    newRope.m_rope.material = m_rope.material;
    newRope.m_rope.textureMode = LineTextureMode.Tile;
    newRope.m_ropeObject.transform.SetParent(transform.parent);
    newRope.m_ropeObject.transform.localPosition = Vector3.zero;
    m_ropes.Add(newRope);
  }

  private ZDO GetParentZDO()
  {
    if (!m_nview || m_nview.m_zdo == null) return null;

    return m_nview.m_zdo;
  }

  private bool RemoveRopeWithID(RopeAttachmentTarget target)
  {
    for (var i = 0; i < m_ropes.Count; i++)
      if (m_ropes[i].m_ropeAnchorTarget == target)
      {
        RemoveRopeAt(i);
        return true;
      }

    return false;
  }

  private void SaveToZDO()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      var pkg = new ZPackage();
      byte version = 2;
      pkg.Write(version);
      for (var i = 0; i < m_ropes.Count; i++)
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
      var ropeIds = new List<RopeAttachmentTarget>();
      GetRopesFromZDO(ropeIds);
      for (var i = 0; i < ropeIds.Count; i++) CreateNewRope(ropeIds[i]);
    }
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void LateUpdate()
  {
    if (!m_nview || m_nview.m_zdo == null) return;

    if (!m_nview.IsOwner() && m_nview.m_zdo.DataRevision != m_zdoDataRevision)
      UpdateRopesFromZDO();

    if (m_rope.enabled && (bool)Player.m_localPlayer)
    {
      m_rope.SetPosition(0, transform.position);
      if (!isHauling && (bool)m_draggingRopeTo)
      {
        var index = GetIndexAtLocation(m_draggingRopeTo);
        var t = GetAttachmentTransform(m_draggingRopeTo,
          new RopeAttachmentTarget(0, index));
        if ((bool)t) m_rope.SetPosition(1, t.position);
      }
      else
      {
        m_rope.SetPosition(1,
          ((Humanoid)Player.m_localPlayer).m_visEquipment.m_rightHand.transform
          .position);
      }
    }

    if (Time.time - m_lastRopeCheckTime > 2f)
    {
      CheckRopes();
      m_lastRopeCheckTime = Time.time;
    }

    for (var i = 0; i < m_updatingRopes.Count; i++)
    {
      var rope = m_updatingRopes[i];
      if ((bool)rope.m_ropeObject && (bool)rope.m_ropeTarget)
      {
        var ropeDistance =
          (transform.position - rope.m_targetTransform.position).magnitude;
        if (m_nview.IsOwner() && (ropeDistance > m_maxRopeDistance ||
                                  ropeDistance >
                                  rope.m_ropeAttachDistance + 8f))
        {
          RemoveUpdatingRopeAt(i);
          continue;
        }

        rope.m_rope.SetPosition(0, transform.position);
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
    for (var i = 0; i < m_ropes.Count; i++)
    {
      var rope = m_ropes[i];
      if ((bool)rope.m_ropeTarget) continue;

      rope.m_ropeTarget =
        ZdoWatchController.Instance.GetGameObject(rope.m_ropeAnchorTarget.Id);
      if (!rope.m_ropeTarget)
      {
        if (ZNet.instance.IsServer())
        {
          var zdo =
            ZdoWatchController.Instance.GetZdo(rope.m_ropeAnchorTarget.Id);
          if (zdo == null)
          {
            RemoveRopeAt(i);
            i--;
          }
        }

        continue;
      }

      var targetTransform =
        GetAttachmentTransform(rope.m_ropeTarget, rope.m_ropeAnchorTarget);
      rope.m_ropeAttachDistance =
        (transform.position - targetTransform.position).magnitude;
      rope.m_targetTransform = targetTransform;
      var rb = rope.m_ropeTarget.GetComponentInParent<Rigidbody>();
      if ((bool)rb)
      {
        var spring = rope.m_ropeComponent.GetSpring();
        var springRb = rope.m_ropeComponent.GetComponent<Rigidbody>();
        springRb.isKinematic = true;
        springRb.useGravity = false;
        spring.maxDistance = rope.m_ropeAttachDistance + 2f;
        spring.minDistance = 0f;
        spring.spring = 10000f;
        spring.connectedBody = rb;
        spring.autoConfigureConnectedAnchor = false;
        spring.anchor =
          springRb.transform.InverseTransformPoint(transform.position);
        spring.connectedAnchor =
          rb.transform.InverseTransformPoint(targetTransform.position);
      }

      if ((!rb && rope.m_ropeTarget.transform.parent == transform.parent) ||
          rb == m_rigidbody)
      {
        rope.m_rope.useWorldSpace = false;
        rope.m_rope.SetPosition(0,
          rope.m_rope.transform.InverseTransformPoint(transform.position));
        rope.m_rope.SetPosition(1,
          rope.m_rope.transform.InverseTransformPoint(rope.m_targetTransform
            .position));
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
    var ropeIds = new HashSet<RopeAttachmentTarget>();
    GetRopesFromZDO(ropeIds);
    for (var i = 0; i < m_ropes.Count; i++)
    {
      var rope = m_ropes[i];
      if (!ropeIds.Contains(rope.m_ropeAnchorTarget))
      {
        RemoveRopeAt(i);
        i--;
      }
    }

    foreach (var ropeId in ropeIds)
      if (!m_ropes.Any((Rope k) => k.m_ropeAnchorTarget == ropeId))
        CreateNewRope(ropeId);
  }

  public static int GetRopeTarget(ZDOID zdoid)
  {
    var zdoparent = ZDOMan.instance.GetZDO(zdoid);
    if (zdoparent != null)
      return ZdoWatchController.Instance.GetOrCreatePersistentID(zdoparent);

    return ZdoWatchController.ZdoIdToId(zdoid);
  }

  private void GetRopesFromZDO(ICollection<RopeAttachmentTarget> ropeIds)
  {
    try
    {
      var bytesPkg = m_nview.m_zdo.GetByteArray("MBRopeAnchor_Ropes");
      if (bytesPkg != null && bytesPkg.Length != 0)
      {
        var pkg = new ZPackage(bytesPkg);
        var version = pkg.ReadByte();
        while (pkg.GetPos() < pkg.Size())
          ropeIds.Add(new RopeAttachmentTarget(
            version <= 1 ? GetRopeTarget(pkg.ReadZDOID()) : pkg.ReadInt(),
            pkg.ReadByte()));
      }
    }
    catch (Exception)
    {
    }
  }

  private void RemoveUpdatingRopeAt(int i)
  {
    var index = m_ropes.IndexOf(m_updatingRopes[i]);
    if (index != -1) RemoveRopeAt(index);
  }

  private void RemoveRopeAt(int i)
  {
    Destroy(m_ropes[i].m_ropeObject);
    m_ropes.RemoveAt(i);
    SaveToZDO();
  }
}