// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.MoveableBaseRootComponent

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;
using Main = ValheimRAFT.Main;

namespace ValheimRAFT.MoveableBaseRootComponent;

public class Server : MoveBaseRoot
{
  public override List<MastComponent> GetMastPieces()
  {
    return m_mastPieces;
  }

  public static readonly KeyValuePair<int, int> MbParentHash = ZDO.GetHashZDOID("MBParent");

  public static readonly int MbCharacterParentHash = "MBCharacterParent".GetStableHashCode();

  public static readonly int MbCharacterOffsetHash = "MBCharacterOFfset".GetStableHashCode();

  public static readonly int MbParentIdHash = "MBParentId".GetStableHashCode();

  public static readonly int MbPositionHash = "MBPosition".GetStableHashCode();

  public static readonly int MbRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MbRotationVecHash = "MBRotationVec".GetStableHashCode();

  public static CustomRPC SyncBuildSectorsRPC;

  public static CustomRPC DelegateToServerRPC;


  public void Awake()
  {
    m_rigidbody = base.gameObject.AddComponent<Rigidbody>();
    m_rigidbody.isKinematic = true;
    m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    m_rigidbody.mass = 99999f;

    // Create your RPC as early as possible so it gets registered with the game
    SyncBuildSectorsRPC = NetworkManager.Instance.AddRPC(
      "UselessRPC", SyncBuildSectorsRPCServerReceive, SyncBuildSectorsRPCClientReceive);

    if (ZNet.instance.IsServer())
    {
      StartCoroutine(nameof(UpdatePieceSectors));
    }
  }

  public override bool GetStatsOverride()
  {
    return m_statsOverride;
  }

  public override void InitializeShipComponent(MoveableBaseShipComponent moveableBaseShipComponent,
    ZNetView nView, Ship ship, Rigidbody rigidbody)
  {
    m_moveableBaseShip = moveableBaseShipComponent;
    m_nview = nView;
    m_ship = ship;
    m_id = ZDOPersistantID.Instance.GetOrCreatePersistantID(nView.m_zdo);
    m_syncRigidbody = rigidbody;
  }

  public override BoxCollider GetFloatCollider()
  {
    return m_floatcollider;
  }

  public override void InitializeShipColliders(BoxCollider[] colliders)
  {
    m_onboardcollider =
      colliders.FirstOrDefault((BoxCollider k) => k.gameObject.name == "OnboardTrigger");
    if (m_onboardcollider != null)
      m_onboardcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    else
    {
      ZLog.LogError("ValheimRAFT MovableBaseShipComponent m_baseRoot.m_onboardcollider is null");
    }

    m_floatcollider = m_ship.m_floatCollider;
    m_floatcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    m_blockingcollider = m_ship.transform.Find("ship/colliders/Cube")
      .GetComponentInChildren<BoxCollider>();
    m_blockingcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    m_blockingcollider.gameObject.layer =
      Main.CustomRaftLayer;
    m_blockingcollider.transform.parent.gameObject.layer =
      Main.CustomRaftLayer;
    ZLog.Log($"Activating MBRoot: {m_id}");
    ActivatePendingPiecesCoroutine();
    FirstTimeCreation();
  }

  private void FirstTimeCreation()
  {
    if (GetPieceCount() != 0)
    {
      return;
    }

    GameObject floor = ZNetScene.instance.GetPrefab("wood_floor");
    for (float x = -1f; x < 1.01f; x += 2f)
    {
      for (float z = -2f; z < 2.01f; z += 2f)
      {
        Vector3 pt = base.transform.TransformPoint(new Vector3(x, 0.45f, z));
        GameObject obj = UnityEngine.Object.Instantiate(floor, pt, base.transform.rotation);
        ZNetView netview = obj.GetComponent<ZNetView>();
        AddNewPiece(netview);
      }
    }
  }

  public void CleanUp()
  {
    StopCoroutine(nameof(ActivatePendingPieces));
    if (!ZNetScene.instance || m_id == 0)
    {
      return;
    }

    for (int i = 0; i < m_pieces.Count; i++)
    {
      ZNetView piece = m_pieces[i];
      if ((bool)piece)
      {
        piece.transform.SetParent(null);
        AddInactivePiece(m_id, piece);
      }
    }

    List<Player> players = Player.GetAllPlayers();
    for (int j = 0; j < players.Count; j++)
    {
      if ((bool)players[j] && players[j].transform.parent == base.transform)
      {
        players[j].transform.SetParent(null);
      }
    }
  }

  private void Sync()
  {
    if ((bool)m_syncRigidbody)
    {
      m_rigidbody.MovePosition(m_syncRigidbody.transform.position);
      m_rigidbody.MoveRotation(m_syncRigidbody.transform.rotation);
    }
  }

  public void FixedUpdate()
  {
    Sync();
  }

  public void LateUpdate()
  {
    Sync();
    if (!ZNet.instance.IsServer())
    {
      UpdateAllPieces();
    }
  }

  public void UpdateAllPieces()
  {
    Vector2i sector = ZoneSystem.instance.GetZone(base.transform.position);
    if (!(sector != m_sector))
    {
      return;
    }

    m_sector = sector;
    for (int i = 0; i < m_pieces.Count; i++)
    {
      ZNetView netview = m_pieces[i];
      if (!netview)
      {
        m_pieces.RemoveAt(i);
        i--;
      }
      else
      {
        netview.GetZDO().SetPosition(base.transform.position);
      }
    }
  }


  public static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);


  enum BuildSectorRequest
  {
    GET_ALL_SECTORS,
    POST_SECTOR,
    EDIT_SECTOR,
    DELETE_SECTOR,
  }

  public static void RunSyncBuildSectors()
  {
    ZPackage package = new ZPackage();
    // byte[] array = new byte[int.Parse(args[0]) * 1024 * 1024];
    // random.NextBytes(array);
    string[] array = { "BuildSectorRequest", BuildSectorRequest.GET_ALL_SECTORS.ToString() };
    package.Write(string.Join("_", array));

    // Invoke the RPC with the server as the target and our random data package as the payload
    Jotunn.Logger.LogMessage($"RunSyncBuildSectors() called");
    SyncBuildSectorsRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
  }

// React to the RPC call on a server
  private IEnumerator SyncBuildSectorsRPCServerReceive(long sender, ZPackage package)
  {
    Jotunn.Logger.LogMessage($"Broadcasting to all clients");
    SyncBuildSectorsRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
    yield return true;
  }

  public static readonly WaitForSeconds HalfSecondWait = new WaitForSeconds(0.5f);

  public static string Base64Decode(string base64EncodedData)
  {
    var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
    return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
  }

// React to the RPC call on a client
  private IEnumerator SyncBuildSectorsRPCClientReceive(long sender, ZPackage package)
  {
    Jotunn.Logger.LogMessage($"Received blob, processing");
    string packageItems = Base64Decode(package.GetBase64());
    Jotunn.Logger.LogMessage($"SyncBuildSectorsRPCClientReceive(): {packageItems}");
    // yield return null;
    //
    // string dot = string.Empty;
    // for (int i = 0; i < 10; ++i)
    // {
    //   dot += ".";
    //   Jotunn.Logger.LogMessage(dot);
    //   yield return HalfSecondWait;
    // }
    yield return true;
  }

  public override IEnumerator UpdatePieceSectors()
  {
    while (true)
    {
      if (!m_allPieces.TryGetValue(m_id, out var list))
      {
        yield return new WaitForSeconds(5f);
        continue;
      }

      Vector3 pos = base.transform.position;
      Vector2i sector = ZoneSystem.instance.GetZone(pos);
      float time = Time.realtimeSinceStartup;
      if (sector != m_sector)
      {
        m_sector = sector;
        for (int i = 0; i < list.Count; i++)
        {
          ZDO zdo = list[i];
          if (!(zdo.GetSector() != sector))
          {
            continue;
          }

          int id = zdo.GetInt(MbParentIdHash);
          if (id != m_id)
          {
            ZLog.LogWarning("Invalid piece in piece list found, removing.");
            list.FastRemoveAt(i);
            i--;
            continue;
          }

          zdo.SetPosition(pos);
          if (Time.realtimeSinceStartup - time > 0.1f)
          {
            itemsRemovedDuringWait = false;
            yield return new WaitForEndOfFrame();
            time = Time.realtimeSinceStartup;
            if (itemsRemovedDuringWait)
            {
              i = 0;
            }
          }
        }
      }

      yield return new WaitForEndOfFrame();
      list = null;
    }
  }

  public override List<RudderComponent> GetRudderPieces()
  {
    return m_rudderPieces;
  }

  public override float GetColliderBottom()
  {
    // this had IL transformation issues
    return m_blockingcollider.transform.position.y + m_blockingcollider.center.y -
           m_blockingcollider.size.y / 2f;
  }

  public static void AddInactivePiece(int id, ZNetView netview)
  {
    if (!m_pendingPieces.TryGetValue(id, out var list))
    {
      list = new List<ZNetView>();
      m_pendingPieces.Add(id, list);
    }

    list.Add(netview);
    WearNTear wnt = netview.GetComponent<WearNTear>();
    if ((bool)wnt)
    {
      wnt.enabled = false;
    }
  }

  public void RemovePiece(ZNetView netview)
  {
    if (m_pieces.Remove(netview))
    {
      MastComponent mast = netview.GetComponent<MastComponent>();
      if ((bool)mast)
      {
        m_mastPieces.Remove(mast);
      }

      RudderComponent rudder = netview.GetComponent<RudderComponent>();
      if ((bool)rudder)
      {
        m_rudderPieces.Remove(rudder);
      }

      BoardingRampComponent ramp = netview.GetComponent<BoardingRampComponent>();
      if ((bool)ramp)
      {
        m_boardingRamps.Remove(ramp);
      }

      TeleportWorld portal = netview.GetComponent<TeleportWorld>();
      if ((bool)portal)
      {
        m_portals.Remove(netview);
      }

      RopeLadderComponent ladder = netview.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        m_ladders.Remove(ladder);
        ladder.m_mbRootDelegate = null;
      }

      UpdateStats();
    }
  }

  private void UpdateStats()
  {
  }

  public void DestroyPiece(WearNTear wnt)
  {
    ZNetView netview = wnt.GetComponent<ZNetView>();
    ZLog.Log($"DestroyPiece: {netview.GetZDO()}");
    RemovePiece(netview);
    UpdatePieceCount();
    if (GetPieceCount() == 0)
    {
      m_ship.GetComponent<WearNTear>().Destroy();
      Object.Destroy(base.gameObject);
    }
  }

  public void ActivatePendingPiecesCoroutine()
  {
    StartCoroutine("ActivatePendingPieces");
  }

  public IEnumerator ActivatePendingPieces()
  {
    if (!m_nview || m_nview.GetZDO() == null)
    {
      yield return null;
    }

    int id = ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.GetZDO());
    if (m_pendingPieces.TryGetValue(id, out var list))
    {
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      for (int j = 0; j < list.Count; j++)
      {
        ZNetView obj = list[j];
        if ((bool)obj)
        {
          ActivatePiece(obj);
          if (!ZNetScene.instance.InLoadingScreen() && stopwatch.ElapsedMilliseconds >= 10)
          {
            yield return new WaitForEndOfFrame();
            stopwatch.Restart();
          }
        }
      }

      list.Clear();
      m_pendingPieces.Remove(id);
    }

    if (m_dynamicObjects.TryGetValue(m_id, out var objectList))
    {
      for (int i = 0; i < objectList.Count; i++)
      {
        GameObject go = ZNetScene.instance.FindInstance(objectList[i]);
        if (!go)
        {
          continue;
        }

        ZNetView nv = go.GetComponentInParent<ZNetView>();
        if (!nv || nv.GetZDO() == null)
        {
          continue;
        }


        /*
         * possible workaround code for non-public access of s_vec3 dictionary
         */
        // if (nv.GetZDO().GetVec3(nv.GetZDO().GetHashCode(), out var dic))
        // {
        //   nv.transform.position = dic + base.transform.position;
        // }

        if (ZDOExtraData.s_vec3.TryGetValue(nv.GetZDO().m_uid, out var dic))
        {
          if (dic.TryGetValue(MbCharacterOffsetHash, out var offset))
          {
            nv.transform.position = offset + base.transform.position;
          }

          offset = default(Vector3);
        }

        ZDOExtraData.RemoveInt(nv.GetZDO().m_uid, MbCharacterParentHash);
        ZDOExtraData.RemoveVec3(nv.GetZDO().m_uid, MbCharacterOffsetHash);
        dic = null;
      }

      m_dynamicObjects.Remove(m_id);
    }

    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
    Server mbroot = target.GetComponentInParent<Server>();
    if ((bool)mbroot)
    {
      source.GetZDO().Set(MbCharacterParentHash, mbroot.m_id);
      source.GetZDO().Set(MbCharacterOffsetHash,
        source.transform.position - mbroot.transform.position);
    }
  }

  public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
  {
    Server mbroot = target.GetComponentInParent<Server>();
    if ((bool)mbroot)
    {
      source.GetZDO().Set(MbCharacterParentHash, mbroot.m_id);
      source.GetZDO().Set(MbCharacterOffsetHash, offset);
    }
  }

  public static void InitZDO(ZDO zdo)
  {
    if (ZNet.instance.IsClientInstance())
    {
    }

    if (zdo.GetPrefab() == StringExtensionMethods.GetStableHashCode("MBRaft"))
    {
    }

    int id = GetParentID(zdo);
    if (id != 0)
    {
      if (!m_allPieces.TryGetValue(id, out var list))
      {
        list = new List<ZDO>();
        m_allPieces.Add(id, list);
      }

      list.Add(zdo);
    }

    int cid = zdo.GetInt(MbCharacterParentHash);
    if (cid != 0)
    {
      if (!m_dynamicObjects.TryGetValue(cid, out var objectList))
      {
        objectList = new List<ZDOID>();
        m_dynamicObjects.Add(cid, objectList);
      }

      objectList.Add(zdo.m_uid);
    }
  }

  public static void RemoveZDO(ZDO zdo)
  {
    int id = GetParentID(zdo);
    if (id != 0 && m_allPieces.TryGetValue(id, out var list))
    {
      list.FastRemove(zdo);
      itemsRemovedDuringWait = true;
    }
  }

  private static int GetParentID(ZDO zdo)
  {
    int id = zdo.GetInt(MbParentIdHash);
    if (id == 0)
    {
      ZDOID zdoid = zdo.GetZDOID(MbParentHash);
      if (zdoid != ZDOID.None)
      {
        ZDO zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = ((zdoparent == null)
          ? ZDOPersistantID.ZDOIDToId(zdoid)
          : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdoparent));
        zdo.Set(MbParentIdHash, id);
        zdo.Set(MbRotationVecHash,
          zdo.GetQuaternion(MbRotationHash, Quaternion.identity).eulerAngles);
        zdo.RemoveZDOID(MbParentHash);
        ZDOExtraData.RemoveQuaternion(zdoid, MbRotationHash);
      }
    }

    return id;
  }

  public bool GetAnchorHeight(float pointY)
  {
    return m_moveableBaseShip && m_moveableBaseShip.m_targetHeight > 0.0 &&
           m_moveableBaseShip.m_flags.HasFlag(MoveableBaseShipComponent
             .MBFlags
             .IsAnchored) && pointY < GetColliderBottom();
  }

  public override void ActivatePiece(ZNetView netview)
  {
    if ((bool)netview)
    {
      netview.transform.SetParent(base.transform);
      netview.transform.localPosition = netview.GetZDO().GetVec3(MbPositionHash, Vector3.zero);
      netview.transform.localRotation =
        Quaternion.Euler(netview.GetZDO().GetVec3(MbRotationVecHash, Vector3.zero));
      WearNTear wnt = netview.GetComponent<WearNTear>();
      if ((bool)wnt)
      {
        wnt.enabled = true;
      }

      AddPiece(netview);
    }
  }

  public override void AddTemporaryPiece(Piece piece)
  {
    piece.transform.SetParent(base.transform);
  }

  public override void AddNewPiece(Piece piece)
  {
    if ((bool)piece && (bool)piece.m_nview)
    {
      AddNewPiece(piece.m_nview);
    }
  }

  public override void AddNewPiece(ZNetView netview)
  {
    ZLog.Log($"netview piece update, {netview.GetZDO()}");
    netview.transform.SetParent(base.transform);
    ZLog.Log(
      $"netview transformed parent to, localPosition:{transform.localPosition} {netview.transform.localRotation.eulerAngles}");
    if (netview.GetZDO() != null)
    {
      netview.GetZDO().Set(MbParentIdHash,
        ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.GetZDO()));
      netview.GetZDO().Set(MbPositionHash, netview.transform.localPosition);
      netview.GetZDO().Set(MbRotationVecHash, netview.transform.localRotation.eulerAngles);
    }

    AddPiece(netview);
    InitZDO(netview.GetZDO());
  }

  public static void InitPiece(ZNetView netview)
  {
    Rigidbody rb = ((Component)netview).GetComponentInChildren<Rigidbody>();
    if (rb && !rb.isKinematic)
    {
      return;
    }

    int id = GetParentID(netview.m_zdo);
    if (id == 0)
    {
      return;
    }

    GameObject parentObj = ZDOPersistantID.Instance.GetGameObject(id);
    if (parentObj)
    {
      MoveableBaseShipComponent mb = parentObj.GetComponent<MoveableBaseShipComponent>();
      if (mb && mb.m_baseRootDelegate)
      {
        mb.m_baseRootDelegate.Instance.ActivatePiece(netview);
      }
    }
    else
    {
      AddInactivePiece(id, netview);
    }
  }

  public override void AddPiece(ZNetView netview)
  {
    m_pieces.Add(netview);
    UpdatePieceCount();
    EncapsulateBounds(netview);
    WearNTear wnt = netview.GetComponent<WearNTear>();
    if ((bool)wnt && global::ValheimRAFT.Main.Instance.MakeAllPiecesWaterProof
          .Value)
    {
      wnt.m_noRoofWear = false;
    }

    CultivatableComponent cultivatable = netview.GetComponent<CultivatableComponent>();
    if ((bool)cultivatable)
    {
      cultivatable.UpdateMaterial();
    }

    MastComponent mast = netview.GetComponent<MastComponent>();
    if ((bool)mast)
    {
      m_mastPieces.Add(mast);
    }

    BoardingRampComponent ramp = netview.GetComponent<BoardingRampComponent>();
    if ((bool)ramp)
    {
      ramp.ForceRampUpdate();
      m_boardingRamps.Add(ramp);
    }

    RudderComponent rudder = netview.GetComponent<RudderComponent>();
    if ((bool)rudder)
    {
      if (!rudder.m_controls)
      {
        rudder.m_controls = netview.GetComponentInChildren<ShipControlls>();
      }

      if (!rudder.m_wheel)
      {
        rudder.m_wheel = netview.transform.Find("controls/wheel");
      }

      rudder.m_controls.m_nview = m_nview;
      rudder.m_controls.m_ship = m_moveableBaseShip.GetComponent<Ship>();
      m_rudderPieces.Add(rudder);
    }

    TeleportWorld portal = netview.GetComponent<TeleportWorld>();
    if ((bool)portal)
    {
      m_portals.Add(netview);
    }

    RopeLadderComponent ladder = netview.GetComponent<RopeLadderComponent>();
    if ((bool)ladder)
    {
      m_ladders.Add(ladder);
      // was "this" before
      ladder.m_mbRootDelegate = GetComponentInParent<Delegate>();
    }

    MeshRenderer[] meshes = netview.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
    MeshRenderer[] array = meshes;
    foreach (MeshRenderer meshRenderer in array)
    {
      if ((bool)meshRenderer.sharedMaterial)
      {
        Material[] sharedMaterials = meshRenderer.sharedMaterials;
        for (int j = 0; j < sharedMaterials.Length; j++)
        {
          Material material = new Material(sharedMaterials[j]);
          material.SetFloat("_RippleDistance", 0f);
          material.SetFloat("_ValueNoise", 0f);
          sharedMaterials[j] = material;
        }

        meshRenderer.sharedMaterials = sharedMaterials;
      }
    }

    Rigidbody[] rbs = netview.GetComponentsInChildren<Rigidbody>();
    for (int i = 0; i < rbs.Length; i++)
    {
      if (rbs[i].isKinematic)
      {
        Object.Destroy(rbs[i]);
      }
    }

    UpdateStats();
  }

  internal override void UpdatePieceCount()
  {
    if ((bool)m_nview && m_nview.GetZDO() != null)
    {
      ZLog.Log($"nview piece update, {m_nview.GetZDO()} to {m_pieces.Count}");
      m_nview.GetZDO().Set("MBPieceCount", m_pieces.Count);
    }
  }

  public override void EncapsulateBounds(ZNetView netview)
  {
    Piece piece = netview.GetComponent<Piece>();
    List<Collider> colliders = (piece
      ? piece.GetAllColliders()
      : new List<Collider>(netview.GetComponentsInChildren<Collider>()));
    Door door = netview.GetComponentInChildren<Door>();
    RopeLadderComponent ladder = netview.GetComponent<RopeLadderComponent>();
    RopeAnchorComponent rope = netview.GetComponent<RopeAnchorComponent>();
    if (!door && !ladder && !rope)
    {
      m_bounds.Encapsulate(netview.transform.localPosition);
    }

    for (int i = 0; i < colliders.Count; i++)
    {
      Physics.IgnoreCollision(colliders[i], m_blockingcollider, ignore: true);
      Physics.IgnoreCollision(colliders[i], m_floatcollider, ignore: true);
      Physics.IgnoreCollision(colliders[i], m_onboardcollider, ignore: true);
    }

    m_blockingcollider.size = new Vector3(m_bounds.size.x, 3f, m_bounds.size.z);
    m_blockingcollider.center = new Vector3(m_bounds.center.x, -0.2f, m_bounds.center.z);
    m_floatcollider.size = new Vector3(m_bounds.size.x, 3f, m_bounds.size.z);
    m_floatcollider.center = new Vector3(m_bounds.center.x, -0.2f, m_bounds.center.z);
    m_onboardcollider.size = m_bounds.size;
    m_onboardcollider.center = m_bounds.center;
  }

  internal override int GetPieceCount()
  {
    if (!m_nview || m_nview.GetZDO() == null)
    {
      return m_pieces.Count;
    }

    return m_nview.GetZDO().GetInt("MBPieceCount", m_pieces.Count);
  }
}