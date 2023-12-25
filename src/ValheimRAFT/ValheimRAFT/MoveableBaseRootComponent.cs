using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jotunn;
using UnityEngine;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimRAFT;

public class MoveableBaseRootComponent : MonoBehaviour
{
  public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("MBParent");

  public static readonly int MBCharacterParentHash = "MBCharacterParent".GetStableHashCode();

  public static readonly int MBCharacterOffsetHash = "MBCharacterOFfset".GetStableHashCode();

  public static readonly int MBParentIdHash = "MBParentId".GetStableHashCode();

  public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

  public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MBRotationVecHash = "MBRotationVec".GetStableHashCode();

  internal static Dictionary<int, List<ZNetView>> m_pendingPieces =
    new Dictionary<int, List<ZNetView>>();
  
  internal static Dictionary<int, List<ZDO>> m_allPieces = new Dictionary<int, List<ZDO>>();

  internal static Dictionary<int, List<ZDOID>>
    m_dynamicObjects = new Dictionary<int, List<ZDOID>>();

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
  private Vector2i m_serverSector;

  private Bounds m_bounds = default(Bounds);

  internal BoxCollider m_blockingcollider;

  internal BoxCollider m_floatcollider;

  internal BoxCollider m_onboardcollider;

  internal int m_id;

  public bool m_statsOverride;

  private static bool itemsRemovedDuringWait;

  internal Coroutine pendingPiecesCoroutine;

  private Coroutine updateSectorCoroutine;

  public MoveableBaseShipComponent m_baseShip;

  public void Awake()
  {
    m_rigidbody = base.gameObject.AddComponent<Rigidbody>();
    m_rigidbody.isKinematic = true;
    m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    m_rigidbody.mass = 99999f;
    /*
     * This should work on both client and server, but the garbage collecting should only apply if the ZDOs are not persistent
     */
    if (ZNet.instance.IsServer())
    {
      ZLog.LogWarning("Starting UpdatePieceSectors");
      updateSectorCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    if (ZNet.instance.IsServerInstance())
      Logger.LogError("SERVER MUST CALL, Test Log to see if server calls this");
  }

  /*
   * This needs to be used to cleanup Rafts that are do not have any parents
   *
   * Alternative is advanced creativemode which allows deleting the raft object. Will need get the functionality from that and add safer logic.
   */
  public IEnumerator DeleteInvalidRafts()
  {
    yield return false;
    // yield return new WaitForSeconds(5f);
    //
    // var objects = Resources.FindObjectsOfTypeAll<MoveableBaseShipComponent>()
    //   .Where(obj => obj.name == PrefabNames.m_raft);
    //
    // ZLog.Log($"objects {objects}");
    //
    // foreach (var o in objects)
    // {
    //   var movableBaseChild = o.GetComponentInChildren<MoveableBaseShipComponent>();
    //   var movableBaseParent = o.GetComponentInParent<MoveableBaseShipComponent>();
    //   if (!movableBaseChild && !movableBaseParent)
    //   {
    //     ZLog.LogWarning(
    //       "Destroying Raft instance that has no MovableBaseChildren. This RAFT was invalid.");
    //     DestroyBoat(o.m_nview);
    //   }
    // }
  }

  public void CleanUp()
  {
    StopCoroutine("ActivatePendingPieces");
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

  /*
   * @important, server does not have access to lifecycle methods so a coroutine is required to update things
   */
  public void LateUpdate()
  {
    Sync();
    if (!ZNet.instance.IsServer())
    {
      UpdateAllPieces();
    }

    if (ZNet.instance.IsServerInstance())
    {
      Logger.LogError("Somehow server called LATE UPDATE");
    }
  }

  public void UpdateAllPieces()
  {
    Vector2i sector = ZoneSystem.instance.GetZone(base.transform.position);
    if (m_sector != m_serverSector)
    {
      if (updateSectorCoroutine != null) StopCoroutine(updateSectorCoroutine);
      updateSectorCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    if (sector == m_sector)
    {
      return;
    }

    m_sector = sector;

    for (int i = 0; i < m_pieces.Count; i++)
    {
      ZNetView netview = m_pieces[i];
      if (!netview)
      {
        Logger.LogError($"Error found with m_pieces: netview {netview}");
        m_pieces.RemoveAt(i);
        i--;
      }
      else
      {
        if (transform.position != netview.transform.position)
        {
          Logger.LogError(
            $"transform position mismatch with netview netview: {netview.transform.position} {netview.transform.localPosition} baseRaft: {transform.position}");
          netview.transform.SetParent(transform);
        }
        netview.m_zdo.SetPosition(base.transform.position);
      }
    }
  }


  public void UpdatePieces(List<ZDO> list)
  {
    var pos = transform.position;
    var sector = ZoneSystem.instance.GetZone(pos);
    if (m_serverSector != sector)
    {
      if (sector != m_sector) m_sector = sector;

      m_serverSector = sector;
      
      for (var i = 0; i < list.Count; i++)
      {
        var zdo = list[i];

        // This could also be a problem. If the zdo is created but the ship is in part of another sector it gets cut off.
        if (zdo.GetSector() == sector) continue;

        var id = zdo.GetInt(MBParentIdHash);
        if (id != m_id)
        {
          Logger.LogWarning("Invalid piece in piece list found, removing.");
          ZLog.LogWarning($"zdo uid: {zdo.m_uid} zdoId:{id} does not match id:{id}");
          list.FastRemoveAt(i);
          i--;
          continue;
        }

        zdo.SetPosition(pos);
      }
    }
  }


  /**
   * large ships need additional threads to render the ship quickly
   *
   * @todo setPosition should not need to be called unless the item is out of alignment. In theory it should be relative to parent so it never should be out of alignment.
   */
  public IEnumerator UpdatePiecesWorker(List<ZDO> list)
  {
    UpdatePieces(list);
    yield return null;
  }

  /*
   * This method IS important, but it also seems heavily related to causing the raft to disappear if it fails.
   *
   * - Apparently to get this working this method must also fire on the client & on server.
   *
   * - This method must fire when a zone loads, otherwise the items will be in a box position until they are renders.
   * - For larger ships, this can take up to 20 seconds. Yikes.
   *
   * Outside of this problem, this script repeatedly calls (but stays on a separate thread) which may be related to fps drop.
   */
  public IEnumerator UpdatePiecesInEachSectorWorker()
  {
    while (true)
    {
      /*
       * wait for the pending pieces coroutine to complete before updating
       */
      if (pendingPiecesCoroutine != null) yield return pendingPiecesCoroutine;
      
      float time = Time.realtimeSinceStartup;
      var output = m_allPieces.TryGetValue(m_id, out var list);
      if (!output)
      {
        ZLog.Log("Waiting for UpdatePieceSectors to be ready");
        yield return new WaitForSeconds(Math.Max(2f,
          ValheimRaftPlugin.Instance.ServerRaftUpdateZoneInterval
            .Value));
        continue;
      }

      UpdatePieces(list);
      yield return UpdatePiecesWorker(list);

      list = null;
      yield return new WaitForEndOfFrame();
    }
  }

  internal float GetColliderBottom()
  {
    return m_blockingcollider.transform.position.y + m_blockingcollider.center.y -
           m_blockingcollider.size.y / 2f;
  }

  public static void AddInactivePiece(int id, ZNetView netview)
  {
    Logger.LogInfo($"AddInactivePiece called, {id}");
    if (!m_pendingPieces.TryGetValue(id, out var list))
    {
      Logger.LogInfo($"Pending pieces dictionary failed");
      list = new List<ZNetView>();
      m_pendingPieces.Add(id, list);
      Logger.LogInfo(
        $"AddInactivePiece called, {m_pendingPieces} has new id {id} with list: {list}");
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
        ladder.m_mbroot = null;
      }
    }
  }

  public void DestroyPiece(WearNTear wnt)
  {
    ZNetView netview = wnt.GetComponent<ZNetView>();
    RemovePiece(netview);
    UpdatePieceCount();
    if (GetPieceCount() == 0)
    {
      m_ship.GetComponent<WearNTear>().Destroy();
      Destroy(base.gameObject);
    }
  }

  public void DestroyBoat()
  {
    var wnt_ship = m_ship.GetComponent<WearNTear>();
    if (wnt_ship)
    {
      wnt_ship.Destroy();
    }
    else if (m_ship)
    {
      Destroy(m_ship);
    }

    Destroy(base.gameObject);
  }

  public void ActivatePendingPiecesCoroutine()
  {
    pendingPiecesCoroutine = StartCoroutine("ActivatePendingPieces");
  }

  public IEnumerator ActivatePendingPieces()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      ZLog.Log(
        $"ActivatePendingPieces early exit due to m_nview: {m_nview} m_nview.m_zdo {(m_nview != null ? m_nview.m_zdo : null)}");
      yield return null;
    }

    int id = ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.m_zdo);
    m_pendingPieces.TryGetValue(id, out var list);

    Logger.LogDebug($"List count {m_dynamicObjects.Count}");
    Logger.LogDebug($"DynamicObjects count {m_dynamicObjects.Count}");

    if (list is { Count: > 0 })
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

    m_dynamicObjects.TryGetValue(m_id, out var objectList);
    var ObjectListHasNoValidItems = true;
    if (objectList is { Count: > 0 })
    {
      for (int i = 0; i < objectList.Count; i++)
      {
        GameObject go = ZNetScene.instance.FindInstance(objectList[i]);

        if (!go)
        {
          continue;
        }

        ZNetView nv = go.GetComponentInParent<ZNetView>();
        if (!nv || nv.m_zdo == null)
        {
          continue;
        }
        else
        {
          ObjectListHasNoValidItems = false;
        }

        if (ZDOExtraData.s_vec3.TryGetValue(nv.m_zdo.m_uid, out var dic))
        {
          if (dic.TryGetValue(MBCharacterOffsetHash, out var offset))
          {
            nv.transform.position = offset + base.transform.position;
          }

          offset = default(Vector3);
        }

        ZDOExtraData.RemoveInt(nv.m_zdo.m_uid, MBCharacterParentHash);
        ZDOExtraData.RemoveVec3(nv.m_zdo.m_uid, MBCharacterOffsetHash);
        dic = null;
      }

      m_dynamicObjects.Remove(m_id);
    }

    /*
     * This prevents empty Prefabs of MBRaft from existing
     */
    if (list == null || list.Count == 0 &&
        (m_dynamicObjects.Count == 0 || ObjectListHasNoValidItems)
       )
    {
      ZLog.LogError($"found boat without any items attached {m_ship} {m_nview}");
      // DestroyBoat();
    }

    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
    MoveableBaseRootComponent mbroot = target.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbroot)
    {
      source.m_zdo.Set(MBCharacterParentHash, mbroot.m_id);
      source.m_zdo.Set(MBCharacterOffsetHash,
        source.transform.position - mbroot.transform.position);
    }
  }

  public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
  {
    MoveableBaseRootComponent mbroot = target.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbroot)
    {
      source.m_zdo.Set(MBCharacterParentHash, mbroot.m_id);
      source.m_zdo.Set(MBCharacterOffsetHash, offset);
    }
  }

  public static void InitZDO(ZDO zdo)
  {
    // this codeblock was left unhandled. I wonder if there needs to be an early exit or fix for MBRaft specifically.
    if (zdo.m_prefab == "MBRaft".GetStableHashCode())
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

    int cid = zdo.GetInt(MBCharacterParentHash);
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
    int id = zdo.GetInt(MBParentIdHash);
    if (id == 0)
    {
      ZDOID zdoid = zdo.GetZDOID(MBParentHash);
      if (zdoid != ZDOID.None)
      {
        ZDO zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = ((zdoparent == null)
          ? ZDOPersistantID.ZDOIDToId(zdoid)
          : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdoparent));
        zdo.Set(MBParentIdHash, id);
        zdo.Set(MBRotationVecHash,
          zdo.GetQuaternion(MBRotationHash, Quaternion.identity).eulerAngles);
        zdo.RemoveZDOID(MBParentHash);
        ZDOExtraData.s_quats.Remove(zdoid, MBRotationHash);
      }
    }

    return id;
  }

  public static void InitPiece(ZNetView netview)
  {
    Rigidbody rb = netview.GetComponentInChildren<Rigidbody>();
    if ((bool)rb && !rb.isKinematic)
    {
      return;
    }

    int id = GetParentID(netview.m_zdo);
    if (id == 0)
    {
      return;
    }

    GameObject parentObj = ZDOPersistantID.Instance.GetGameObject(id);
    if ((bool)parentObj)
    {
      MoveableBaseShipComponent mb = parentObj.GetComponent<MoveableBaseShipComponent>();
      if ((bool)mb && (bool)mb.m_baseRoot)
      {
        mb.m_baseRoot.ActivatePiece(netview);
      }
    }
    else
    {
      AddInactivePiece(id, netview);
    }
  }

  public void ActivatePiece(ZNetView netview)
  {
    if ((bool)netview)
    {
      netview.transform.SetParent(base.transform);
      netview.transform.localPosition = netview.m_zdo.GetVec3(MBPositionHash, Vector3.zero);
      netview.transform.localRotation =
        Quaternion.Euler(netview.m_zdo.GetVec3(MBRotationVecHash, Vector3.zero));
      WearNTear wnt = netview.GetComponent<WearNTear>();
      if ((bool)wnt)
      {
        wnt.enabled = true;
      }

      AddPiece(netview);
    }
  }

  public void AddTemporaryPiece(Piece piece)
  {
    piece.transform.SetParent(base.transform);
  }

  public void AddNewPiece(Piece piece)
  {
    if ((bool)piece && (bool)piece.m_nview)
    {
      ZLog.Log("Added new piece is valid");
      AddNewPiece(piece.m_nview);
    }
  }

  public void AddNewPiece(ZNetView netview)
  {
    netview.transform.SetParent(base.transform);
    if (netview.m_zdo != null)
    {
      netview.m_zdo.Set(MBParentIdHash,
        ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.m_zdo));
      netview.m_zdo.Set(MBRotationVecHash, netview.transform.localRotation.eulerAngles);
      netview.m_zdo.Set(MBPositionHash, netview.transform.localPosition);
    }

    AddPiece(netview);
    InitZDO(netview.m_zdo);
  }

  public void AddPiece(ZNetView netview)
  {
    m_pieces.Add(netview);

    UpdatePieceCount();
    EncapsulateBounds(netview);
    WearNTear wnt = netview.GetComponent<WearNTear>();
    if ((bool)wnt && ValheimRaftPlugin.Instance.MakeAllPiecesWaterProof.Value)
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
      ladder.m_mbroot = this;
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

    /*
     * @todo investigate why this is called. Determine if it is needed
     */
    Rigidbody[] rbs = netview.GetComponentsInChildren<Rigidbody>();
    for (int i = 0; i < rbs.Length; i++)
    {
      if (rbs[i].isKinematic)
      {
        Destroy(rbs[i]);
      }
    }

    m_baseShip.UpdateStats(false);
  }

  private void UpdatePieceCount()
  {
    if ((bool)m_nview && m_nview.m_zdo != null)
    {
      m_nview.m_zdo.Set("MBPieceCount", m_pieces.Count);
    }
  }

  public void EncapsulateBounds(ZNetView netview)
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

  internal int GetPieceCount()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      return m_pieces.Count;
    }

    return m_nview.m_zdo.GetInt("MBPieceCount", m_pieces.Count);
  }
}