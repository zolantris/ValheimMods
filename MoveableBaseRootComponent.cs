// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.MoveableBaseRootComponent
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ValheimRAFT.Util;

namespace ValheimRAFT
{
  public class MoveableBaseRootComponent : MonoBehaviour
  {
    public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("MBParent");
    public static readonly int MBCharacterParentHash = StringExtensionMethods.GetStableHashCode("MBCharacterParent");
    public static readonly int MBCharacterOffsetHash = StringExtensionMethods.GetStableHashCode("MBCharacterOFfset");
    public static readonly int MBParentIdHash = StringExtensionMethods.GetStableHashCode("MBParentId");
    public static readonly int MBPositionHash = StringExtensionMethods.GetStableHashCode("MBPosition");
    public static readonly int MBRotationHash = StringExtensionMethods.GetStableHashCode("MBRotation");
    public static readonly int MBRotationVecHash = StringExtensionMethods.GetStableHashCode("MBRotationVec");
    internal static Dictionary<int, List<ZNetView>> m_pendingPieces = new Dictionary<int, List<ZNetView>>();
    internal static Dictionary<int, List<ZDO>> m_allPieces = new Dictionary<int, List<ZDO>>();
    internal static Dictionary<int, List<ZDOID>> m_dynamicObjects = new Dictionary<int, List<ZDOID>>();
    internal MoveableBaseShipComponent m_moveableBaseShip;
    internal Rigidbody m_rigidbody;
    internal ZNetView m_nview;
    internal Rigidbody m_syncRigidbody;
    internal Ship m_ship;
    internal List<ZNetView> m_pieces = new List<ZNetView>();
    internal List<MastComponent> m_mastPieces = new List<MastComponent>();
    internal List<RudderComponent> m_rudderPieces = new List<RudderComponent>();
    internal List<ZNetView> m_portals = new List<ZNetView>();
    internal List<RopeLadderComponent> m_ladders = new List<RopeLadderComponent>();
    internal List<BoardingRampComponent> m_boardingRamps = new List<BoardingRampComponent>();
    private Vector2i m_sector;
    private Bounds m_bounds = new Bounds();
    internal BoxCollider m_blockingcollider;
    internal BoxCollider m_floatcollider;
    internal BoxCollider m_onboardcollider;
    internal int m_id;
    public bool m_statsOverride;
    private static bool itemsRemovedDuringWait;

    public void Awake()
    {
      this.m_rigidbody = ((Component) this).gameObject.AddComponent<Rigidbody>();
      this.m_rigidbody.isKinematic = true;
      this.m_rigidbody.interpolation = (RigidbodyInterpolation) 1;
      this.m_rigidbody.mass = 99999f;
      if (!ZNet.instance.IsServer())
        return;
      this.StartCoroutine("UpdatePieceSectors");
    }

    public void CleanUp()
    {
      this.StopCoroutine("ActivatePendingPieces");
      if (!Object.op_Implicit((Object) ZNetScene.instance) || this.m_id == 0)
        return;
      for (int index = 0; index < this.m_pieces.Count; ++index)
      {
        ZNetView piece = this.m_pieces[index];
        if (Object.op_Implicit((Object) piece))
        {
          ((Component) piece).transform.SetParent((Transform) null);
          MoveableBaseRootComponent.AddInactivePiece(this.m_id, piece);
        }
      }
      List<Player> allPlayers = Player.GetAllPlayers();
      for (int index = 0; index < allPlayers.Count; ++index)
      {
        if (Object.op_Implicit((Object) allPlayers[index]) && Object.op_Equality((Object) ((Component) allPlayers[index]).transform.parent, (Object) ((Component) this).transform))
          ((Component) allPlayers[index]).transform.SetParent((Transform) null);
      }
    }

    private void Sync()
    {
      if (!Object.op_Implicit((Object) this.m_syncRigidbody))
        return;
      this.m_rigidbody.MovePosition(((Component) this.m_syncRigidbody).transform.position);
      this.m_rigidbody.MoveRotation(((Component) this.m_syncRigidbody).transform.rotation);
    }

    public void FixedUpdate() => this.Sync();

    public void LateUpdate()
    {
      this.Sync();
      if (ZNet.instance.IsServer())
        return;
      this.UpdateAllPieces();
    }

    public void UpdateAllPieces()
    {
      Vector2i zone = ZoneSystem.instance.GetZone(((Component) this).transform.position);
      if (!Vector2i.op_Inequality(zone, this.m_sector))
        return;
      this.m_sector = zone;
      for (int index = 0; index < this.m_pieces.Count; ++index)
      {
        ZNetView piece = this.m_pieces[index];
        if (!Object.op_Implicit((Object) piece))
        {
          this.m_pieces.RemoveAt(index);
          --index;
        }
        else
          piece.m_zdo.SetPosition(((Component) this).transform.position);
      }
    }

    public IEnumerator UpdatePieceSectors()
    {
      while (true)
      {
        List<ZDO> list;
        if (!MoveableBaseRootComponent.m_allPieces.TryGetValue(this.m_id, out list))
        {
          yield return (object) new WaitForSeconds(5f);
        }
        else
        {
          Vector3 pos = ((Component) this).transform.position;
          Vector2i sector = ZoneSystem.instance.GetZone(pos);
          float time = Time.realtimeSinceStartup;
          if (Vector2i.op_Inequality(sector, this.m_sector))
          {
            this.m_sector = sector;
            for (int i = 0; i < list.Count; ++i)
            {
              ZDO zdo = list[i];
              if (Vector2i.op_Inequality(zdo.GetSector(), sector))
              {
                int id = zdo.GetInt(MoveableBaseRootComponent.MBParentIdHash, 0);
                if (id != this.m_id)
                {
                  Logger.LogWarning((object) "Invalid piece in piece list found, removing.");
                  list.FastRemoveAt<ZDO>(i);
                  --i;
                  continue;
                }
                zdo.SetPosition(pos);
                if ((double) Time.realtimeSinceStartup - (double) time > 0.10000000149011612)
                {
                  MoveableBaseRootComponent.itemsRemovedDuringWait = false;
                  yield return (object) new WaitForEndOfFrame();
                  time = Time.realtimeSinceStartup;
                  if (MoveableBaseRootComponent.itemsRemovedDuringWait)
                    i = 0;
                }
              }
              zdo = (ZDO) null;
            }
          }
          yield return (object) new WaitForEndOfFrame();
          list = (List<ZDO>) null;
          pos = new Vector3();
          sector = new Vector2i();
        }
      }
    }

    internal float GetColliderBottom() => (float) ((double) ((Component) this.m_blockingcollider).transform.position.y + (double) this.m_blockingcollider.center.y - (double) this.m_blockingcollider.size.y / 2.0);

    public static void AddInactivePiece(int id, ZNetView netview)
    {
      List<ZNetView> znetViewList;
      if (!MoveableBaseRootComponent.m_pendingPieces.TryGetValue(id, out znetViewList))
      {
        znetViewList = new List<ZNetView>();
        MoveableBaseRootComponent.m_pendingPieces.Add(id, znetViewList);
      }
      znetViewList.Add(netview);
      WearNTear component = ((Component) netview).GetComponent<WearNTear>();
      if (!Object.op_Implicit((Object) component))
        return;
      ((Behaviour) component).enabled = false;
    }

    public void RemovePiece(ZNetView netview)
    {
      if (!this.m_pieces.Remove(netview))
        return;
      MastComponent component1 = ((Component) netview).GetComponent<MastComponent>();
      if (Object.op_Implicit((Object) component1))
        this.m_mastPieces.Remove(component1);
      RudderComponent component2 = ((Component) netview).GetComponent<RudderComponent>();
      if (Object.op_Implicit((Object) component2))
        this.m_rudderPieces.Remove(component2);
      BoardingRampComponent component3 = ((Component) netview).GetComponent<BoardingRampComponent>();
      if (Object.op_Implicit((Object) component3))
        this.m_boardingRamps.Remove(component3);
      if (Object.op_Implicit((Object) ((Component) netview).GetComponent<TeleportWorld>()))
        this.m_portals.Remove(netview);
      RopeLadderComponent component4 = ((Component) netview).GetComponent<RopeLadderComponent>();
      if (Object.op_Implicit((Object) component4))
      {
        this.m_ladders.Remove(component4);
        component4.m_mbroot = (MoveableBaseRootComponent) null;
      }
      this.UpdateStats();
    }

    private void UpdateStats()
    {
    }

    public void DestroyPiece(WearNTear wnt)
    {
      this.RemovePiece(((Component) wnt).GetComponent<ZNetView>());
      this.UpdatePieceCount();
      if (this.GetPieceCount() != 0)
        return;
      ((Component) this.m_ship).GetComponent<WearNTear>().Destroy();
      Object.Destroy((Object) ((Component) this).gameObject);
    }

    public void ActivatePendingPiecesCoroutine() => this.StartCoroutine("ActivatePendingPieces");

    public IEnumerator ActivatePendingPieces()
    {
      if (!Object.op_Implicit((Object) this.m_nview) || this.m_nview.m_zdo == null)
        yield return (object) null;
      int id = ZDOPersistantID.Instance.GetOrCreatePersistantID(this.m_nview.m_zdo);
      List<ZNetView> list;
      if (MoveableBaseRootComponent.m_pendingPieces.TryGetValue(id, out list))
      {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int i = 0; i < list.Count; ++i)
        {
          ZNetView obj = list[i];
          if (Object.op_Implicit((Object) obj))
          {
            this.ActivatePiece(obj);
            if (!ZNetScene.instance.InLoadingScreen() && stopwatch.ElapsedMilliseconds >= 10L)
            {
              yield return (object) new WaitForEndOfFrame();
              stopwatch.Restart();
            }
            obj = (ZNetView) null;
          }
        }
        list.Clear();
        MoveableBaseRootComponent.m_pendingPieces.Remove(id);
        stopwatch = (Stopwatch) null;
      }
      List<ZDOID> objectList;
      if (MoveableBaseRootComponent.m_dynamicObjects.TryGetValue(this.m_id, out objectList))
      {
        for (int i = 0; i < objectList.Count; ++i)
        {
          GameObject go = ZNetScene.instance.FindInstance(objectList[i]);
          if (Object.op_Implicit((Object) go))
          {
            ZNetView nv = go.GetComponentInParent<ZNetView>();
            if (Object.op_Implicit((Object) nv) && nv.m_zdo != null)
            {
              BinarySearchDictionary<int, Vector3> dic;
              if (ZDOExtraData.s_vec3.TryGetValue(nv.m_zdo.m_uid, out dic))
              {
                Vector3 offset;
                if (dic.TryGetValue(MoveableBaseRootComponent.MBCharacterOffsetHash, ref offset))
                  ((Component) nv).transform.position = Vector3.op_Addition(offset, ((Component) this).transform.position);
                offset = new Vector3();
              }
              ZDOExtraData.RemoveInt(nv.m_zdo.m_uid, MoveableBaseRootComponent.MBCharacterParentHash);
              ZDOExtraData.RemoveVec3(nv.m_zdo.m_uid, MoveableBaseRootComponent.MBCharacterOffsetHash);
              dic = (BinarySearchDictionary<int, Vector3>) null;
            }
            nv = (ZNetView) null;
          }
          go = (GameObject) null;
        }
        MoveableBaseRootComponent.m_dynamicObjects.Remove(this.m_id);
      }
      yield return (object) null;
    }

    public static void AddDynamicParent(ZNetView source, GameObject target)
    {
      MoveableBaseRootComponent componentInParent = target.GetComponentInParent<MoveableBaseRootComponent>();
      if (!Object.op_Implicit((Object) componentInParent))
        return;
      source.m_zdo.Set(MoveableBaseRootComponent.MBCharacterParentHash, componentInParent.m_id, false);
      source.m_zdo.Set(MoveableBaseRootComponent.MBCharacterOffsetHash, Vector3.op_Subtraction(((Component) source).transform.position, ((Component) componentInParent).transform.position));
    }

    public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
    {
      MoveableBaseRootComponent componentInParent = target.GetComponentInParent<MoveableBaseRootComponent>();
      if (!Object.op_Implicit((Object) componentInParent))
        return;
      source.m_zdo.Set(MoveableBaseRootComponent.MBCharacterParentHash, componentInParent.m_id, false);
      source.m_zdo.Set(MoveableBaseRootComponent.MBCharacterOffsetHash, offset);
    }

    public static void InitZDO(ZDO zdo)
    {
      if (zdo.m_prefab != StringExtensionMethods.GetStableHashCode("MBRaft"))
        ;
      int parentId = MoveableBaseRootComponent.GetParentID(zdo);
      if (parentId != 0)
      {
        List<ZDO> zdoList;
        if (!MoveableBaseRootComponent.m_allPieces.TryGetValue(parentId, out zdoList))
        {
          zdoList = new List<ZDO>();
          MoveableBaseRootComponent.m_allPieces.Add(parentId, zdoList);
        }
        zdoList.Add(zdo);
      }
      int key = zdo.GetInt(MoveableBaseRootComponent.MBCharacterParentHash, 0);
      if (key == 0)
        return;
      List<ZDOID> zdoidList;
      if (!MoveableBaseRootComponent.m_dynamicObjects.TryGetValue(key, out zdoidList))
      {
        zdoidList = new List<ZDOID>();
        MoveableBaseRootComponent.m_dynamicObjects.Add(key, zdoidList);
      }
      zdoidList.Add(zdo.m_uid);
    }

    public static void RemoveZDO(ZDO zdo)
    {
      int parentId = MoveableBaseRootComponent.GetParentID(zdo);
      List<ZDO> list;
      if (parentId == 0 || !MoveableBaseRootComponent.m_allPieces.TryGetValue(parentId, out list))
        return;
      list.FastRemove<ZDO>(zdo);
      MoveableBaseRootComponent.itemsRemovedDuringWait = true;
    }

    private static int GetParentID(ZDO zdo)
    {
      int parentId = zdo.GetInt(MoveableBaseRootComponent.MBParentIdHash, 0);
      if (parentId == 0)
      {
        ZDOID zdoid = zdo.GetZDOID(MoveableBaseRootComponent.MBParentHash);
        if (ZDOID.op_Inequality(zdoid, ZDOID.None))
        {
          ZDO zdo1 = ZDOMan.instance.GetZDO(zdoid);
          parentId = zdo1 == null ? ZDOPersistantID.ZDOIDToId(zdoid) : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdo1);
          zdo.Set(MoveableBaseRootComponent.MBParentIdHash, parentId, false);
          ZDO zdo2 = zdo;
          int mbRotationVecHash = MoveableBaseRootComponent.MBRotationVecHash;
          Quaternion quaternion = zdo.GetQuaternion(MoveableBaseRootComponent.MBRotationHash, Quaternion.identity);
          Vector3 eulerAngles = ((Quaternion) ref quaternion).eulerAngles;
          zdo2.Set(mbRotationVecHash, eulerAngles);
          zdo.RemoveZDOID(MoveableBaseRootComponent.MBParentHash);
          ZDOHelper.Remove<Quaternion>(ZDOExtraData.s_quats, zdoid, MoveableBaseRootComponent.MBRotationHash);
        }
      }
      return parentId;
    }

    public static void InitPiece(ZNetView netview)
    {
      Rigidbody componentInChildren = ((Component) netview).GetComponentInChildren<Rigidbody>();
      if (Object.op_Implicit((Object) componentInChildren) && !componentInChildren.isKinematic)
        return;
      int parentId = MoveableBaseRootComponent.GetParentID(netview.m_zdo);
      if (parentId == 0)
        return;
      GameObject gameObject = ZDOPersistantID.Instance.GetGameObject(parentId);
      if (Object.op_Implicit((Object) gameObject))
      {
        MoveableBaseShipComponent component = gameObject.GetComponent<MoveableBaseShipComponent>();
        if (Object.op_Implicit((Object) component) && Object.op_Implicit((Object) component.m_baseRoot))
          component.m_baseRoot.ActivatePiece(netview);
      }
      else
        MoveableBaseRootComponent.AddInactivePiece(parentId, netview);
    }

    public void ActivatePiece(ZNetView netview)
    {
      if (!Object.op_Implicit((Object) netview))
        return;
      ((Component) netview).transform.SetParent(((Component) this).transform);
      ((Component) netview).transform.localPosition = netview.m_zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero);
      ((Component) netview).transform.localRotation = Quaternion.Euler(netview.m_zdo.GetVec3(MoveableBaseRootComponent.MBRotationVecHash, Vector3.zero));
      WearNTear component = ((Component) netview).GetComponent<WearNTear>();
      if (Object.op_Implicit((Object) component))
        ((Behaviour) component).enabled = true;
      this.AddPiece(netview);
    }

    public void AddTemporaryPiece(Piece piece) => ((Component) piece).transform.SetParent(((Component) this).transform);

    public void AddNewPiece(Piece piece)
    {
      if (!Object.op_Implicit((Object) piece) || !Object.op_Implicit((Object) piece.m_nview))
        return;
      this.AddNewPiece(piece.m_nview);
    }

    public void AddNewPiece(ZNetView netview)
    {
      ((Component) netview).transform.SetParent(((Component) this).transform);
      if (netview.m_zdo != null)
      {
        netview.m_zdo.Set(MoveableBaseRootComponent.MBParentIdHash, ZDOPersistantID.Instance.GetOrCreatePersistantID(this.m_nview.m_zdo), false);
        netview.m_zdo.Set(MoveableBaseRootComponent.MBPositionHash, ((Component) netview).transform.localPosition);
        ZDO zdo = netview.m_zdo;
        int mbRotationVecHash = MoveableBaseRootComponent.MBRotationVecHash;
        Quaternion localRotation = ((Component) netview).transform.localRotation;
        Vector3 eulerAngles = ((Quaternion) ref localRotation).eulerAngles;
        zdo.Set(mbRotationVecHash, eulerAngles);
      }
      this.AddPiece(netview);
      MoveableBaseRootComponent.InitZDO(netview.m_zdo);
    }

    public void AddPiece(ZNetView netview)
    {
      this.m_pieces.Add(netview);
      this.UpdatePieceCount();
      this.EncapsulateBounds(netview);
      WearNTear component1 = ((Component) netview).GetComponent<WearNTear>();
      if (Object.op_Implicit((Object) component1) && ValheimRAFT.ValheimRAFT.Instance.MakeAllPiecesWaterProof.Value)
        component1.m_noRoofWear = false;
      CultivatableComponent component2 = ((Component) netview).GetComponent<CultivatableComponent>();
      if (Object.op_Implicit((Object) component2))
        component2.UpdateMaterial();
      MastComponent component3 = ((Component) netview).GetComponent<MastComponent>();
      if (Object.op_Implicit((Object) component3))
        this.m_mastPieces.Add(component3);
      BoardingRampComponent component4 = ((Component) netview).GetComponent<BoardingRampComponent>();
      if (Object.op_Implicit((Object) component4))
      {
        component4.ForceRampUpdate();
        this.m_boardingRamps.Add(component4);
      }
      RudderComponent component5 = ((Component) netview).GetComponent<RudderComponent>();
      if (Object.op_Implicit((Object) component5))
      {
        if (!Object.op_Implicit((Object) component5.m_controls))
          component5.m_controls = ((Component) netview).GetComponentInChildren<ShipControlls>();
        if (!Object.op_Implicit((Object) component5.m_wheel))
          component5.m_wheel = ((Component) netview).transform.Find("controls/wheel");
        component5.m_controls.m_nview = this.m_nview;
        component5.m_controls.m_ship = ((Component) this.m_moveableBaseShip).GetComponent<Ship>();
        this.m_rudderPieces.Add(component5);
      }
      if (Object.op_Implicit((Object) ((Component) netview).GetComponent<TeleportWorld>()))
        this.m_portals.Add(netview);
      RopeLadderComponent component6 = ((Component) netview).GetComponent<RopeLadderComponent>();
      if (Object.op_Implicit((Object) component6))
      {
        this.m_ladders.Add(component6);
        component6.m_mbroot = this;
      }
      foreach (MeshRenderer componentsInChild in ((Component) netview).GetComponentsInChildren<MeshRenderer>(true))
      {
        if (Object.op_Implicit((Object) ((Renderer) componentsInChild).sharedMaterial))
        {
          Material[] sharedMaterials = ((Renderer) componentsInChild).sharedMaterials;
          for (int index = 0; index < sharedMaterials.Length; ++index)
          {
            Material material = new Material(sharedMaterials[index]);
            material.SetFloat("_RippleDistance", 0.0f);
            material.SetFloat("_ValueNoise", 0.0f);
            sharedMaterials[index] = material;
          }
          ((Renderer) componentsInChild).sharedMaterials = sharedMaterials;
        }
      }
      Rigidbody[] componentsInChildren = ((Component) netview).GetComponentsInChildren<Rigidbody>();
      for (int index = 0; index < componentsInChildren.Length; ++index)
      {
        if (componentsInChildren[index].isKinematic)
          Object.Destroy((Object) componentsInChildren[index]);
      }
      this.UpdateStats();
    }

    private void UpdatePieceCount()
    {
      if (!Object.op_Implicit((Object) this.m_nview) || this.m_nview.m_zdo == null)
        return;
      this.m_nview.m_zdo.Set("MBPieceCount", this.m_pieces.Count);
    }

    public void EncapsulateBounds(ZNetView netview)
    {
      Piece component1 = ((Component) netview).GetComponent<Piece>();
      List<Collider> colliderList = Object.op_Implicit((Object) component1) ? ((StaticTarget) component1).GetAllColliders() : new List<Collider>((IEnumerable<Collider>) ((Component) netview).GetComponentsInChildren<Collider>());
      Door componentInChildren = ((Component) netview).GetComponentInChildren<Door>();
      RopeLadderComponent component2 = ((Component) netview).GetComponent<RopeLadderComponent>();
      RopeAnchorComponent component3 = ((Component) netview).GetComponent<RopeAnchorComponent>();
      if (!Object.op_Implicit((Object) componentInChildren) && !Object.op_Implicit((Object) component2) && !Object.op_Implicit((Object) component3))
        ((Bounds) ref this.m_bounds).Encapsulate(((Component) netview).transform.localPosition);
      for (int index = 0; index < colliderList.Count; ++index)
      {
        Physics.IgnoreCollision(colliderList[index], (Collider) this.m_blockingcollider, true);
        Physics.IgnoreCollision(colliderList[index], (Collider) this.m_floatcollider, true);
        Physics.IgnoreCollision(colliderList[index], (Collider) this.m_onboardcollider, true);
      }
      this.m_blockingcollider.size = new Vector3(((Bounds) ref this.m_bounds).size.x, 3f, ((Bounds) ref this.m_bounds).size.z);
      this.m_blockingcollider.center = new Vector3(((Bounds) ref this.m_bounds).center.x, -0.2f, ((Bounds) ref this.m_bounds).center.z);
      this.m_floatcollider.size = new Vector3(((Bounds) ref this.m_bounds).size.x, 3f, ((Bounds) ref this.m_bounds).size.z);
      this.m_floatcollider.center = new Vector3(((Bounds) ref this.m_bounds).center.x, -0.2f, ((Bounds) ref this.m_bounds).center.z);
      this.m_onboardcollider.size = ((Bounds) ref this.m_bounds).size;
      this.m_onboardcollider.center = ((Bounds) ref this.m_bounds).center;
    }

    internal int GetPieceCount() => !Object.op_Implicit((Object) this.m_nview) || this.m_nview.m_zdo == null ? this.m_pieces.Count : this.m_nview.m_zdo.GetInt("MBPieceCount", this.m_pieces.Count);
  }
}
