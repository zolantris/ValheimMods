using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jotunn;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Serialization;
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

  internal static Dictionary<int, List<ZNetView>> m_pendingPieces = new();

  internal static Dictionary<int, List<ZDO>> m_allPieces = new();

  internal static Dictionary<int, List<ZDOID>>
    m_dynamicObjects = new();

  internal MoveableBaseShipComponent m_moveableBaseShip;

  internal Rigidbody m_rigidbody;

  internal ZNetView m_nview;

  internal Rigidbody m_syncRigidbody;

  internal Ship m_ship;

  internal List<ZNetView> m_pieces = new();

  internal List<MastComponent> m_mastPieces = new();
  internal List<SailComponent> m_sailPiece = new();

  internal List<RudderComponent> m_rudderPieces = new();

  internal List<ZNetView> m_portals = new();

  internal List<RopeLadderComponent> m_ladders = new();

  internal List<BoardingRampComponent> m_boardingRamps = new();

  /*
   * sail calcs
   */
  public int numberOfTier1Sails = 0;
  public int numberOfTier2Sails = 0;
  public int numberOfTier3Sails = 0;
  public float customSailsArea = 0f;
  public float totalSailArea = 0f;
  /* end sail calcs  */

  private Vector2i m_sector;
  private Vector2i m_serverSector;

  private Bounds m_bounds = default;

  internal BoxCollider m_blockingcollider;

  internal BoxCollider m_floatcollider;

  internal BoxCollider m_onboardcollider;

  internal int m_id;

  public bool m_statsOverride;

  private static bool itemsRemovedDuringWait;

  internal Coroutine pendingPiecesCoroutine;

  private Coroutine server_UpdatePiecesCoroutine;

  public void Awake()
  {
    m_rigidbody = gameObject.AddComponent<Rigidbody>();
    m_rigidbody.isKinematic = true;
    m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    m_rigidbody.mass = 99999f;
    /*
     * This should work on both client and server, but the garbage collecting should only apply if the ZDOs are not persistent
     */
    if (ZNet.instance.IsServer())
    {
      ZLog.LogWarning("IS SERVER");
      server_UpdatePiecesCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    ZLog.LogWarning(
      $"znet instance: {ZNet.instance} isServer {ZNet.instance.IsServer()} isDedicated: {ZNet.instance.IsDedicated()}");

    if (ZNet.instance.IsServer())
      Logger.LogError("SERVER MUST CALL, Test Log to see if server calls this");
  }

  public void CleanUp()
  {
    StopCoroutine("ActivatePendingPieces");
    if (!ZNetScene.instance || m_id == 0) return;

    for (var i = 0; i < m_pieces.Count; i++)
    {
      var piece = m_pieces[i];
      if ((bool)piece)
      {
        piece.transform.SetParent(null);
        AddInactivePiece(m_id, piece);
      }
    }

    var players = Player.GetAllPlayers();
    for (var j = 0; j < players.Count; j++)
      if ((bool)players[j] && players[j].transform.parent == transform)
        players[j].transform.SetParent(null);
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
    if (!ZNet.instance.IsServer()) Client_UpdateAllPieces();
  }

  /**
   * @warning this must only be called on the client
   */
  public void Client_UpdateAllPieces()
  {
    var sector = ZoneSystem.instance.GetZone(transform.position);

    if (sector == m_sector) return;

    if (m_sector != m_serverSector) ServerSyncAllPieces();

    m_sector = sector;

    for (var i = 0; i < m_pieces.Count; i++)
    {
      var netview = m_pieces[i];
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
          ZLog.Log(
            $"Transform position {transform.position} {netview.transform.position} {netview.transform.localPosition}");
          netview.m_zdo.SetPosition(transform.position);
        }
      }
    }
  }

  public void ServerSyncAllPieces()
  {
    if (server_UpdatePiecesCoroutine != null) StopCoroutine(server_UpdatePiecesCoroutine);
    StartCoroutine(UpdatePiecesInEachSectorWorker());
  }


  public void UpdatePieces(List<ZDO> list)
  {
    var pos = transform.position;
    var sector = ZoneSystem.instance.GetZone(pos);

    if (m_serverSector == sector) return;
    if (!sector.Equals(m_sector)) m_sector = sector;

    m_serverSector = sector;

    for (var i = 0; i < list.Count; i++)
    {
      var zdo = list[i];

      // This could also be a problem. If the zdo is created but the ship is in part of another sector it gets cut off.
      if (zdo.GetSector() == sector) continue;

      var id = zdo.GetInt(MBParentIdHash);
      if (id != m_id)
      {
        list.FastRemoveAt(i);
        i--;
        continue;
      }

      zdo.SetPosition(pos);
    }
  }


  /**
   * large ships need additional threads to render the ship quickly
   *
   * @todo setPosition should not need to be called unless the item is out of alignment. In theory it should be relative to parent so it never should be out of alignment.
   */
  private IEnumerator UpdatePiecesWorker(List<ZDO> list)
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

      var time = Time.realtimeSinceStartup;
      var output = m_allPieces.TryGetValue(m_id, out var list);
      if (!output)
      {
        yield return new WaitForSeconds(Math.Max(2f,
          ValheimRaftPlugin.Instance.ServerRaftUpdateZoneInterval
            .Value));
        continue;
      }

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
    if (!m_pendingPieces.TryGetValue(id, out var list))
    {
      list = new List<ZNetView>();
      m_pendingPieces.Add(id, list);
    }

    list.Add(netview);
    var wnt = netview.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = false;
  }

  public void RemovePiece(ZNetView netview)
  {
    if (m_pieces.Remove(netview))
    {
      var sail = netview.GetComponent<SailComponent>();
      if ((bool)sail)
      {
        m_sailPiece.Remove(sail);
      }

      var mast = netview.GetComponent<MastComponent>();
      if ((bool)mast)
      {
        m_mastPieces.Remove(mast);
      }

      var rudder = netview.GetComponent<RudderComponent>();
      if ((bool)rudder) m_rudderPieces.Remove(rudder);

      var ramp = netview.GetComponent<BoardingRampComponent>();
      if ((bool)ramp) m_boardingRamps.Remove(ramp);

      var portal = netview.GetComponent<TeleportWorld>();
      if ((bool)portal) m_portals.Remove(netview);

      var ladder = netview.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        m_ladders.Remove(ladder);
        ladder.m_mbroot = null;
      }
    }

    UpdateStats();
  }

  private void UpdateStats()
  {
  }

  public void DestroyPiece(WearNTear wnt)
  {
    var netview = wnt.GetComponent<ZNetView>();
    RemovePiece(netview);
    UpdatePieceCount();
    totalSailArea = 0f;
    if (GetPieceCount() == 0)
    {
      m_ship.GetComponent<WearNTear>().Destroy();
      Destroy(gameObject);
    }
  }

  public void DestroyBoat()
  {
    var wnt_ship = m_ship.GetComponent<WearNTear>();
    if (wnt_ship)
      wnt_ship.Destroy();
    else if (m_ship) Destroy(m_ship);

    Destroy(gameObject);
  }

  public void ActivatePendingPiecesCoroutine()
  {
    if (pendingPiecesCoroutine != null) StopCoroutine(pendingPiecesCoroutine);

    pendingPiecesCoroutine = StartCoroutine(nameof(ActivatePendingPieces));
  }

  public IEnumerator ActivatePendingPieces()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      ZLog.Log(
        $"ActivatePendingPieces early exit due to m_nview: {m_nview} m_nview.m_zdo {(m_nview != null ? m_nview.m_zdo : null)}");
      yield return null;
    }

    var id = ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.m_zdo);
    m_pendingPieces.TryGetValue(id, out var list);

    if (list is { Count: > 0 })
    {
      var stopwatch = new Stopwatch();
      stopwatch.Start();
      for (var j = 0; j < list.Count; j++)
      {
        var obj = list[j];
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

    ZLog.Log($"Ship Size calc is: m_bounds {m_bounds} bounds size {m_bounds.size}");

    m_dynamicObjects.TryGetValue(m_id, out var objectList);
    var ObjectListHasNoValidItems = true;
    if (objectList is { Count: > 0 })
    {
      for (var i = 0; i < objectList.Count; i++)
      {
        var go = ZNetScene.instance.FindInstance(objectList[i]);

        if (!go) continue;

        var nv = go.GetComponentInParent<ZNetView>();
        if (!nv || nv.m_zdo == null)
          continue;
        else
          ObjectListHasNoValidItems = false;

        if (ZDOExtraData.s_vec3.TryGetValue(nv.m_zdo.m_uid, out var dic))
        {
          if (dic.TryGetValue(MBCharacterOffsetHash, out var offset))
            nv.transform.position = offset + transform.position;

          offset = default;
        }

        ZDOExtraData.RemoveInt(nv.m_zdo.m_uid, MBCharacterParentHash);
        ZDOExtraData.RemoveVec3(nv.m_zdo.m_uid, MBCharacterOffsetHash);
        dic = null;
      }

      m_dynamicObjects.Remove(m_id);
    }

    /*
     * This prevents empty Prefabs of MBRaft from existing
     * @todo make this only apply for boats with no objects in any list
     */
    if (list == null || (list.Count == 0 &&
                         (m_dynamicObjects.Count == 0 || ObjectListHasNoValidItems))
       )
    {
      // ZLog.LogError($"found boat without any items attached {m_ship} {m_nview}");
      // DestroyBoat();
    }

    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
    var mbroot = target.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbroot)
    {
      source.m_zdo.Set(MBCharacterParentHash, mbroot.m_id);
      source.m_zdo.Set(MBCharacterOffsetHash,
        source.transform.position - mbroot.transform.position);
    }
  }

  /**
   * A cached getter for sail size. Cache invalidates when a piece is added or removed
   *
   * This method calls so frequently outside of the scope of ValheimRaftPlugin.Instance so the Config values cannot be fetched for some reason.
   */
  public float GetShipSailArea()
  {
    if (totalSailArea != 0f || !ValheimRaftPlugin.Instance ||
        m_mastPieces.Count == 0 && m_sailPiece.Count == 0)
    {
      return totalSailArea;
    }

    totalSailArea = 0;
    customSailsArea = 0;
    numberOfTier1Sails = 0;
    numberOfTier2Sails = 0;
    numberOfTier3Sails = 0;

    foreach (var mMastPiece in m_mastPieces)
    {
      if (mMastPiece.name.Contains("MBRaftMast"))
      {
        ++numberOfTier1Sails;
        totalSailArea += numberOfTier1Sails * SailAreaForce.Tier1;
      }

      else if (mMastPiece.name.Contains("MBKarveMast"))
      {
        ++numberOfTier2Sails;
        totalSailArea += numberOfTier2Sails * SailAreaForce.Tier2;
      }

      else if (mMastPiece.name.Contains("MBVikingShipMast"))
      {
        ++numberOfTier3Sails;
        totalSailArea += numberOfTier3Sails * SailAreaForce.Tier3;
        ;
      }
    }

    Logger.LogDebug(
      $"numberOfTieredSails {numberOfTier1Sails} {numberOfTier2Sails} {numberOfTier3Sails}");

    var sailComponents = GetComponentsInChildren<SailComponent>();
    if (sailComponents.Length != 0)
    {
      foreach (var sailComponent in sailComponents)
      {
        if ((bool)sailComponent)
        {
          customSailsArea += sailComponent.GetSailArea();
        }
      }

      totalSailArea +=
        (customSailsArea * Math.Max(0.1f, SailAreaForce.CustomTier1AreaForceMultiplier));
    }

    /*
     * Clamps everything by base 10
     *
     *  divide by 10 b/c all the enums were set with a 10 multiplier to keep them whole numbers
     */
    if (totalSailArea != 0)
    {
      totalSailArea /= Math.Max(1f, SailAreaForce.SailAreaThrottle);
    }

    Logger.LogDebug($"totalSailArea: {totalSailArea}");

    return totalSailArea;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
  {
    var mbroot = target.GetComponentInParent<MoveableBaseRootComponent>();
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

    var id = GetParentID(zdo);
    if (id != 0)
    {
      if (!m_allPieces.TryGetValue(id, out var list))
      {
        list = new List<ZDO>();
        m_allPieces.Add(id, list);
      }

      list.Add(zdo);
    }

    var cid = zdo.GetInt(MBCharacterParentHash);
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
    var id = GetParentID(zdo);
    if (id != 0 && m_allPieces.TryGetValue(id, out var list))
    {
      list.FastRemove(zdo);
      itemsRemovedDuringWait = true;
    }
  }

  private static int GetParentID(ZDO zdo)
  {
    var id = zdo.GetInt(MBParentIdHash);
    if (id == 0)
    {
      var zdoid = zdo.GetZDOID(MBParentHash);
      if (zdoid != ZDOID.None)
      {
        var zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = zdoparent == null
          ? ZDOPersistantID.ZDOIDToId(zdoid)
          : ZDOPersistantID.Instance.GetOrCreatePersistantID(zdoparent);
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
    var rb = netview.GetComponentInChildren<Rigidbody>();
    if ((bool)rb && !rb.isKinematic) return;

    var id = GetParentID(netview.m_zdo);
    if (id == 0) return;

    var parentObj = ZDOPersistantID.Instance.GetGameObject(id);
    if ((bool)parentObj)
    {
      var mb = parentObj.GetComponent<MoveableBaseShipComponent>();
      if ((bool)mb && (bool)mb.m_baseRoot) mb.m_baseRoot.ActivatePiece(netview);
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
      netview.transform.SetParent(transform);
      netview.transform.localPosition = netview.m_zdo.GetVec3(MBPositionHash, Vector3.zero);
      netview.transform.localRotation =
        Quaternion.Euler(netview.m_zdo.GetVec3(MBRotationVecHash, Vector3.zero));
      var wnt = netview.GetComponent<WearNTear>();
      if ((bool)wnt) wnt.enabled = true;

      AddPiece(netview);
    }
  }

  public void AddTemporaryPiece(Piece piece)
  {
    piece.transform.SetParent(transform);
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
    netview.transform.SetParent(transform);
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
    totalSailArea = 0;
    m_pieces.Add(netview);
    m_moveableBaseShip.GetShipStats().GetShipFloatation(m_pieces);

    UpdatePieceCount();
    EncapsulateBounds(netview);
    var wnt = netview.GetComponent<WearNTear>();
    if ((bool)wnt && ValheimRaftPlugin.Instance.MakeAllPiecesWaterProof.Value)
      wnt.m_noRoofWear = false;

    var cultivatable = netview.GetComponent<CultivatableComponent>();
    if ((bool)cultivatable) cultivatable.UpdateMaterial();

    var mast = netview.GetComponent<MastComponent>();
    if ((bool)mast)
    {
      m_mastPieces.Add(mast);
    }

    var sail = netview.GetComponent<SailComponent>();
    if ((bool)sail)
    {
      m_sailPiece.Add(sail);
    }

    var ramp = netview.GetComponent<BoardingRampComponent>();
    if ((bool)ramp)
    {
      ramp.ForceRampUpdate();
      m_boardingRamps.Add(ramp);
    }

    var rudder = netview.GetComponent<RudderComponent>();
    if ((bool)rudder)
    {
      if (!rudder.m_controls) rudder.m_controls = netview.GetComponentInChildren<ShipControlls>();

      if (!rudder.m_wheel) rudder.m_wheel = netview.transform.Find("controls/wheel");

      rudder.m_controls.m_nview = m_nview;
      rudder.m_controls.m_ship = m_moveableBaseShip.GetComponent<Ship>();
      m_rudderPieces.Add(rudder);
    }

    var portal = netview.GetComponent<TeleportWorld>();
    if ((bool)portal) m_portals.Add(netview);

    var ladder = netview.GetComponent<RopeLadderComponent>();
    if ((bool)ladder)
    {
      m_ladders.Add(ladder);
      ladder.m_mbroot = this;
    }

    var meshes = netview.GetComponentsInChildren<MeshRenderer>(true);
    foreach (var meshRenderer in meshes)
      if ((bool)meshRenderer.sharedMaterial)
      {
        var sharedMaterials = meshRenderer.sharedMaterials;
        for (var j = 0; j < sharedMaterials.Length; j++)
        {
          var material = new Material(sharedMaterials[j]);
          material.SetFloat("_RippleDistance", 0f);
          material.SetFloat("_ValueNoise", 0f);
          sharedMaterials[j] = material;
        }

        meshRenderer.sharedMaterials = sharedMaterials;
      }

    /*
     * @todo investigate why this is called. Determine if it is needed
     */
    var rbs = netview.GetComponentsInChildren<Rigidbody>();
    for (var i = 0; i < rbs.Length; i++)
      if (rbs[i].isKinematic)
        Destroy(rbs[i]);
  }

  private void UpdatePieceCount()
  {
    if ((bool)m_nview && m_nview.m_zdo != null) m_nview.m_zdo.Set("MBPieceCount", m_pieces.Count);
  }

  public void EncapsulateBounds(ZNetView netview)
  {
    var piece = netview.GetComponent<Piece>();
    var colliders = piece
      ? piece.GetAllColliders()
      : new List<Collider>(netview.GetComponentsInChildren<Collider>());
    var door = netview.GetComponentInChildren<Door>();
    var ladder = netview.GetComponent<RopeLadderComponent>();
    var rope = netview.GetComponent<RopeAnchorComponent>();
    if (!door && !ladder && !rope) m_bounds.Encapsulate(netview.transform.localPosition);

    for (var i = 0; i < colliders.Count; i++)
    {
      Physics.IgnoreCollision(colliders[i], m_blockingcollider, true);
      Physics.IgnoreCollision(colliders[i], m_floatcollider, true);
      Physics.IgnoreCollision(colliders[i], m_onboardcollider, true);
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
    if (!m_nview || m_nview.m_zdo == null) return m_pieces.Count;

    return m_nview.m_zdo.GetInt("MBPieceCount", m_pieces.Count);
  }
}