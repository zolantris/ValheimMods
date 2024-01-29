using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.Vehicles;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Vehicles;

/**
 * @description This is a controller used for all vehicles
 *
 * Currently it must be initialized within a vehicle view IE VVShip or upcoming landvehicle instances.
 */
public class BaseVehicleController : MonoBehaviour, IBaseVehicleController
{
  public ZNetView m_nview { get; set; }

  public static readonly KeyValuePair<int, int> MBParentHash = ZDO.GetHashZDOID("MBParent");

  public static readonly int MBCharacterParentHash = "MBCharacterParent".GetStableHashCode();

  public static readonly int MBCharacterOffsetHash = "MBCharacterOffset".GetStableHashCode();

  public static readonly int MBParentIdHash = "MBParentId".GetStableHashCode();

  public static readonly int MBPositionHash = "MBPosition".GetStableHashCode();

  public static readonly int MBRotationHash = "MBRotation".GetStableHashCode();

  public static readonly int MBRotationVecHash = "MBRotationVec".GetStableHashCode();

  public static Dictionary<int, List<ZNetView>> m_pendingPieces = new();

  public static Dictionary<int, List<ZDO>> m_allPieces = new();

  public static Dictionary<int, List<ZDOID>>
    m_dynamicObjects = new();

  /*
   * @todo make this a generic most likely this all should be in a shared extension api
   * IE: VehicleInstance getter
   */
  public WaterVehicleController waterVehicleController;
  public BaseVehicleController instance;

  internal Rigidbody m_rigidbody;
  // internal Rigidbody m_syncRigidbody;

  internal List<ZNetView> m_pieces = new();
  internal List<ShipHullComponent> m_hullPieces = new();

  internal List<MastComponent> m_mastPieces = new();
  internal List<SailComponent> m_sailPiece = new();

  internal List<RudderComponent> m_rudderPieces = new();

  internal List<ZNetView> m_portals = new();

  internal List<RopeLadderComponent> m_ladders = new();

  internal List<BoardingRampComponent> m_boardingRamps = new();

  internal float ShipContainerMass = 0f;
  internal float ShipMass = 0f;
  public static bool hasDebug = false;

  internal float TotalMass => ShipContainerMass + ShipMass;

  /*
   * sail calcs
   */
  public int numberOfTier1Sails = 0;
  public int numberOfTier2Sails = 0;
  public int numberOfTier3Sails = 0;
  public float customSailsArea = 0f;

  public float totalSailArea = 0f;

  public virtual IVehicleShip? VehicleInstance { set; get; }

/* end sail calcs  */
  private Vector2i m_sector;
  private Vector2i m_serverSector;
  private Bounds m_bounds = new();
  public BoxCollider m_blockingcollider = new();
  internal BoxCollider m_floatcollider;
  internal BoxCollider m_onboardcollider = new();
  public int m_id;
  public bool m_statsOverride;
  private static bool itemsRemovedDuringWait;
  private Coroutine? _pendingPiecesCoroutine;
  private Coroutine? _serverUpdatePiecesCoroutine;

  public void SetColliders(GameObject vehicleInstance)
  {
    var colliders = vehicleInstance.transform.GetComponentsInChildren<BoxCollider>();

    // defaults to a new boxcollider if somehow things are not detected
    m_onboardcollider =
      colliders.FirstOrDefault((k) => k.gameObject.name.Contains("VVOnboardTrigger")) ??
      new BoxCollider();
    m_floatcollider =
      colliders.FirstOrDefault((k) => k.gameObject.name.Contains("VVFloatCollider")) ??
      new BoxCollider();
    m_blockingcollider =
      colliders.FirstOrDefault((k) => k.gameObject.name.Contains("VVBlockingCollider")) ??
      new BoxCollider();

    if (m_onboardcollider != null)
    {
      m_onboardcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    if (m_floatcollider != null) m_floatcollider.transform.localScale = new Vector3(1f, 1f, 1f);

    if (m_blockingcollider != null)
    {
      m_blockingcollider.transform.localScale = new Vector3(1f, 1f, 1f);
      m_blockingcollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
      m_blockingcollider.transform.parent.gameObject.layer =
        ValheimRaftPlugin.CustomRaftLayer;
    }
  }


  public void FireErrorOnNull(Collider obj, String name)
  {
    if (!(bool)obj)
    {
      Logger.LogError($"BaseVehicleError: collider not initialized for <{name}>");
    }
  }

  public void ValidateInitialization()
  {
    // colliders that must be valid
    FireErrorOnNull(m_floatcollider, "VVFloatCollider");
    FireErrorOnNull(m_blockingcollider, "VVBlockingCollider");
    FireErrorOnNull(m_onboardcollider, "VVOnboardCollider");
  }

  public void Awake()
  {
    instance = this;
    hasDebug = ValheimRaftPlugin.Instance.HasDebugBase.Value;

    m_rigidbody = GetComponent<Rigidbody>();
    // m_rigidbody.isKinematic = true;
    // m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    // m_rigidbody.mass = 99999f;
    Debug.Log("Captured Log"); // Breadcrumb
    Debug.LogWarning("Captured Warning"); // Breadcrumb
    Debug.LogError("This is a Test error called within BaseVehicleController.Awake");
  }

  public void Start()
  {
    ValidateInitialization();

    if (!(bool)ZNet.instance)
    {
      return;
    }

    /*
     * This should work on both client and server, but the garbage collecting should only apply if the ZDOs are not persistent
     */
    if (ZNet.instance.IsDedicated())
    {
      _serverUpdatePiecesCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }
  }

  public void GetPersistentID()
  {
    Logger.LogDebug($"GetPersistentID, called {name}");
    if (!(bool)m_nview)
    {
      return;
    }

    if (m_nview.GetZDO() == null)
    {
      return;
    }

    m_id = ZDOPersistentID.Instance.GetOrCreatePersistentID(m_nview.GetZDO());
  }

  private void OnDisable()
  {
    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }
  }

  private void OnEnable()
  {
    GetPersistentID();

    if (!(bool)ZNet.instance)
    {
      return;
    }

    if (ZNet.instance.IsDedicated() && _serverUpdatePiecesCoroutine == null)
    {
      _serverUpdatePiecesCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }
  }

  public void OnDestroy()
  {
    CleanUp();
  }

  public void CleanUp()
  {
    if (_pendingPiecesCoroutine != null)
    {
      StopCoroutine(_pendingPiecesCoroutine);
    }

    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }

    if (!ZNetScene.instance || m_id == 0) return;

    foreach (var piece in m_pieces)
    {
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
    // if ((bool)m_syncRigidbody && (bool)m_rigidbody)
    // {
    //   m_syncRigidbody.mass = Math.Max(TotalMass, 2000f);
    //   m_rigidbody.MovePosition(m_syncRigidbody.transform.position);
    //   m_rigidbody.MoveRotation(m_syncRigidbody.transform.rotation);
    // }
  }

  // public void FixedUpdate()
  // {
  //   Sync();
  // }

/*
 * @important, server does not have access to lifecycle methods so a coroutine is required to update things
 */
  public void LateUpdate()
  {
    // Sync();

    if (!(bool)ZNet.instance)
    {
      // prevents NRE from next command
      Client_UpdateAllPieces();
      return;
    }

    if (ZNet.instance.IsDedicated() == false) Client_UpdateAllPieces();
  }

  /**
   * @warning this must only be called on the client
   */
  public void Client_UpdateAllPieces()
  {
    var sector = ZoneSystem.instance.GetZone(transform.position);

    if (sector == m_sector) return;

    // if (m_sector != m_serverSector) ServerSyncAllPieces();

    m_sector = sector;

    for (var i = 0; i < m_pieces.Count; i++)
    {
      var nv = m_pieces[i];
      if (!nv)
      {
        Logger.LogError($"Error found with m_pieces: netview {nv}");
        m_pieces.RemoveAt(i);
        i--;
      }
      else
      {
        if (transform.position != nv.transform.position)
        {
          nv.m_zdo.SetPosition(transform.position);
        }
      }
    }
  }

  public void ServerSyncAllPieces()
  {
    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }

    _serverUpdatePiecesCoroutine = StartCoroutine(UpdatePiecesInEachSectorWorker());
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
      if (_pendingPiecesCoroutine != null) yield return _pendingPiecesCoroutine;

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

      // todo remove this if it does nothing
      list = null;
      yield return new WaitForEndOfFrame();
    }
    // ReSharper disable once IteratorNeverReturns
  }

  internal float GetColliderBottom()
  {
    return m_blockingcollider.transform.position.y + m_blockingcollider.center.y -
           m_blockingcollider.size.y / 2f;
  }

  public static void AddInactivePiece(int id, ZNetView netView)
  {
    if (hasDebug) Logger.LogDebug($"addInactivePiece called with {id} for {netView.name}");
    if (!m_pendingPieces.TryGetValue(id, out var list))
    {
      list = new List<ZNetView>();
      m_pendingPieces.Add(id, list);
    }

    list.Add(netView);
    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = false;
  }

/*
 * deltaMass can be positive or negative number
 */
  public void UpdateMass(ZNetView netView, bool isRemoving = false)
  {
    if (!(bool)netView)
    {
      if (hasDebug)
      {
        Logger.LogDebug("NetView is invalid skipping mass update");
      }

      return;
    }

    var piece = netView.GetComponent<Piece>();
    if (!(bool)piece)
    {
      if (hasDebug)
        Logger.LogDebug(
          "unable to fetch piece data from netViewPiece this could be a raft piece erroring.");
      return;
    }

    var pieceWeight = ComputePieceWeight(piece, isRemoving);

    if (isRemoving)
    {
      ShipMass -= pieceWeight;
    }
    else
    {
      ShipMass += pieceWeight;
    }

    /*
     * TODO figure out if this should updated
     */
    // m_rigidbody.mass = TotalMass;
  }

  public void RemovePiece(ZNetView netView)
  {
    if (m_pieces.Remove(netView))
    {
      UpdateMass(netView, true);

      var hull = netView.GetComponent<ShipHullComponent>();
      if ((bool)hull)
      {
        m_hullPieces.Remove(hull);
      }

      var sail = netView.GetComponent<SailComponent>();
      if ((bool)sail)
      {
        m_sailPiece.Remove(sail);
      }

      var mast = netView.GetComponent<MastComponent>();
      if ((bool)mast)
      {
        m_mastPieces.Remove(mast);
      }

      var rudder = netView.GetComponent<RudderComponent>();
      if ((bool)rudder) m_rudderPieces.Remove(rudder);

      var ramp = netView.GetComponent<BoardingRampComponent>();
      if ((bool)ramp) m_boardingRamps.Remove(ramp);

      var portal = netView.GetComponent<TeleportWorld>();
      if ((bool)portal) m_portals.Remove(netView);

      var ladder = netView.GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        m_ladders.Remove(ladder);
        ladder.m_mbroot = null;
        ladder.baseVehicleController = null;
      }
    }

    UpdateStats();
  }

  private void UpdateStats()
  {
  }

  /**
   * this will recalculate only when the ship speed changes.
   */
  public void ComputeAllShipContainerItemWeight()
  {
    if (!ValheimRaftPlugin.Instance.HasShipContainerWeightCalculations.Value &&
        ShipContainerMass != 0f)
    {
      ShipContainerMass = 0f;
      return;
    }

    var containers = GetComponentsInChildren<Container>();
    float totalContainerMass = 0f;
    foreach (var container in containers)
    {
      totalContainerMass += ComputeContainerWeight(container);
    }

    ShipContainerMass = totalContainerMass;
  }


  private float ComputeContainerWeight(Container container, bool isRemoving = false)
  {
    var inventory = container.GetInventory();
    if (inventory != null)
    {
      var containerWeight = inventory.GetTotalWeight();
      if (hasDebug) Logger.LogDebug($"containerWeight {containerWeight} name: {container.name}");
      if (isRemoving)
      {
        return -containerWeight;
      }

      return containerWeight;
    }

    return 0f;
  }

/*
 * this function must be used on additional and removal of items to avoid retaining item weight
 */
  private float ComputePieceWeight(Piece piece, bool isRemoving)
  {
    if (!(bool)piece)
    {
      return 0f;
    }

    var pieceName = piece.name;

    if (ValheimRaftPlugin.Instance.HasShipContainerWeightCalculations.Value)
    {
      var container = piece.GetComponent<Container>();
      if ((bool)container)
      {
        ShipContainerMass += ComputeContainerWeight(container, isRemoving);
      }
    }

    var baseMultiplier = 1f;
    /*
     * locally scaled pieces should have a mass multiplier.
     *
     * For now assuming everything is a rectangular prism L*W*H
     */
    if (piece.transform.localScale != new Vector3(1, 1, 1))
    {
      baseMultiplier = piece.transform.localScale.x * piece.transform.localScale.y *
                       piece.transform.localScale.z;
      if (hasDebug)
        Logger.LogDebug(
          $"ValheimRAFT ComputeShipItemWeight() found piece that does not have a 1,1,1 local scale piece: {pieceName} scale: {piece.transform.localScale}, the 3d localScale will be multiplied by the area of this vector instead of 1x1x1");
    }

    if (pieceName == "wood_floor_1x1")
    {
      return 1f * baseMultiplier;
    }

    /*
     * wood_log/wood_core may be split out to a lower ratio
     */
    if (pieceName.Contains("wood"))
    {
      return MaterialWeight.Wood * baseMultiplier;
    }

    if (pieceName.Contains("stone_"))
    {
      return MaterialWeight.Stone * baseMultiplier;
    }

    if (pieceName.Contains("blackmarble"))
    {
      return MaterialWeight.BlackMarble * baseMultiplier;
    }

    if (pieceName.Contains("blastfurnace") || pieceName.Contains("charcoal_kiln") ||
        pieceName.Contains("forge") || pieceName.Contains("smelter"))
    {
      return 20f * baseMultiplier;
    }

    // default return is the weight of wood 1x1
    return 2f * baseMultiplier;
  }


  public void DestroyPiece(WearNTear wnt)
  {
    if ((bool)wnt)
    {
      if (wnt.name.Contains(PrefabController.WaterVehiclePrefabName))
      {
        // prevents a loop of DestroyPiece being called from WearNTear_Patch.
        return;
      }

      var netview = wnt.GetComponent<ZNetView>();
      RemovePiece(netview);
      UpdatePieceCount();
      totalSailArea = 0f;
    }

    var pieceCount = GetPieceCount();
    if (pieceCount > 0) return;
    if (VehicleInstance?.Instance != null)
    {
      var wntShip = VehicleInstance.Instance.GetComponent<WearNTear>();
      if ((bool)wntShip) wntShip.Destroy();
    }
  }

  public void DestroyBoat()
  {
    var wntShip = instance.GetComponent<WearNTear>();
    if ((bool)wntShip)
      wntShip.Destroy();
    else if (instance) Destroy(instance);

    if (instance.gameObject != gameObject)
    {
      Destroy(gameObject);
    }
  }

  public void ActivatePendingPiecesCoroutine()
  {
    if (hasDebug)
      Logger.LogDebug(
        $"ActivatePendingPiecesCoroutine(): pendingPieces count: {m_pendingPieces.Count}");
    if (_pendingPiecesCoroutine != null)
    {
      StopCoroutine(_pendingPiecesCoroutine);
    }

    _pendingPiecesCoroutine = StartCoroutine(nameof(ActivatePendingPieces));
  }

  public IEnumerator ActivatePendingPieces()
  {
    if (!(bool)m_nview || m_nview == null)
    {
      yield return null;
    }

    if (m_nview.GetZDO() == null)
    {
      Logger.LogDebug("m_zdo is null for activate pending pieces");
      yield return null;
    }

    Logger.LogDebug("ActivatePendingPieces before ID getter");

    var id = ZDOPersistentID.Instance.GetOrCreatePersistentID(m_nview.GetZDO());
    Logger.LogDebug("ActivatePendingPieces after ID getter");
    m_pendingPieces.TryGetValue(id, out var list);

    // Logger.LogDebug($"mpending pieces list {list.Count}");
    if (list is { Count: > 0 })
    {
      // var stopwatch = new Stopwatch();
      // stopwatch.Start();
      for (var j = 0; j < list.Count; j++)
      {
        var obj = list[j];
        if ((bool)obj)
        {
          if (hasDebug)
          {
            Logger.LogDebug($"ActivatePendingPieces obj: {obj} {obj.name}");
          }

          ActivatePiece(obj);
        }
        else
        {
          if (hasDebug)
          {
            Logger.LogDebug($"ActivatePendingPieces obj is not valid {obj}");
          }
        }
      }

      list.Clear();
      m_pendingPieces.Remove(id);
    }

    Logger.LogDebug($"after list 589");


    if (hasDebug)
      Logger.LogDebug($"Ship Size calc is: m_bounds {m_bounds} bounds size {m_bounds.size}");

    m_dynamicObjects.TryGetValue(m_id, out var objectList);
    var objectListHasNoValidItems = true;
    Logger.LogDebug($"after list 598");
    if (objectList is { Count: > 0 })
    {
      for (var i = 0; i < objectList.Count; i++)
      {
        var go = ZNetScene.instance.FindInstance(objectList[i]);
        Logger.LogDebug($"after list 604");

        if (!go) continue;

        var nv = go.GetComponentInParent<ZNetView>();
        if (!nv || nv.m_zdo == null)
          continue;
        else
          objectListHasNoValidItems = false;

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

    Logger.LogDebug($"after list 630");

    /*
     * This prevents empty Prefabs of MBRaft from existing
     * @todo make this only apply for boats with no objects in any list
     */
    if (list is { Count: 0 } &&
        (m_dynamicObjects.Count == 0 || objectListHasNoValidItems)
       )
    {
      Logger.LogError($"found boat without any items attached {m_nview}");
      DestroyBoat();
    }

    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
    var bvc = target.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
    {
      source.m_zdo.Set(MBCharacterParentHash, bvc.m_id);
      source.m_zdo.Set(MBCharacterOffsetHash,
        source.transform.position - bvc.transform.position);
    }
  }

  /**
   * A cached getter for sail size. Cache invalidates when a piece is added or removed
   *
   * This method calls so frequently outside of the scope of ValheimRaftPlugin.Instance so the Config values cannot be fetched for some reason.
   */
  public float GetTotalSailArea()
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

    var hasConfigOverride = ValheimRaftPlugin.Instance.EnableCustomPropulsionConfig.Value;

    foreach (var mMastPiece in m_mastPieces)
    {
      if (mMastPiece.name.Contains("MBRaftMast"))
      {
        ++numberOfTier1Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier1Area.Value
          : SailAreaForce.Tier1;
        totalSailArea += numberOfTier1Sails * multiplier;
      }
      else if (mMastPiece.name.Contains("MBKarveMast"))
      {
        ++numberOfTier2Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier2Area.Value
          : SailAreaForce.Tier2;
        totalSailArea += numberOfTier2Sails * multiplier;
      }
      else if (mMastPiece.name.Contains("MBVikingShipMast"))
      {
        ++numberOfTier3Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier3Area.Value
          : SailAreaForce.Tier3;
        totalSailArea += numberOfTier3Sails * multiplier;
        ;
      }
    }

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

      if (hasDebug) Logger.LogDebug($"CustomSailsArea {customSailsArea}");
      var multiplier = hasConfigOverride
        ? ValheimRaftPlugin.Instance.SailCustomAreaTier1Multiplier.Value
        : SailAreaForce.CustomTier1AreaForceMultiplier;

      totalSailArea +=
        (customSailsArea * Math.Max(0.1f,
          multiplier));
    }

    /*
     * Clamps everything by the maxSailSpeed
     */
    if (totalSailArea != 0 && !ValheimRaftPlugin.Instance.HasShipWeightCalculations.Value)
    {
      totalSailArea = Math.Min(ValheimRaftPlugin.Instance.MaxSailSpeed.Value, totalSailArea);
    }

    return totalSailArea;
  }

  public float GetSailingForce()
  {
    var area = GetTotalSailArea();
    if (!ValheimRaftPlugin.Instance.HasShipWeightCalculations.Value) return area;

    var mpFactor = ValheimRaftPlugin.Instance.MassPercentageFactor.Value;
    var speedCapMultiplier = ValheimRaftPlugin.Instance.SpeedCapMultiplier.Value;

    var sailForce = speedCapMultiplier * area /
                    (TotalMass / mpFactor);

    if (ValheimRaftPlugin.Instance.HasDebugSails.Value)
    {
      Logger.LogDebug(
        $"GetSailingForce() = speedCapMultiplier * area /(totalMass / mpFactor); {speedCapMultiplier} * ({area}/({TotalMass}/{mpFactor})) = {sailForce}");
    }

    var maxSailForce = Math.Min(ValheimRaftPlugin.Instance.MaxSailSpeed.Value, sailForce);
    var maxPropulsion = Math.Min(ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value, maxSailForce);
    return maxPropulsion;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target, Vector3 offset)
  {
    var mbroot = target.GetComponentInParent<BaseVehicleController>();
    if ((bool)mbroot)
    {
      source.m_zdo.Set(MBCharacterParentHash, mbroot.m_id);
      source.m_zdo.Set(MBCharacterOffsetHash, offset);
    }
  }

  public static void InitZDO(ZDO zdo)
  {
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
          ? ZDOPersistentID.ZDOIDToId(zdoid)
          : ZDOPersistentID.Instance.GetOrCreatePersistentID(zdoparent);
        zdo.Set(MBParentIdHash, id);
        zdo.Set(MBRotationVecHash,
          zdo.GetQuaternion(MBRotationHash, Quaternion.identity).eulerAngles);
        zdo.RemoveZDOID(MBParentHash);
        ZDOExtraData.s_quats.Remove(zdoid, MBRotationHash);
      }
    }

    return id;
  }

  public static void InitPiece(ZNetView netView)
  {
    var rb = netView.GetComponentInChildren<Rigidbody>();
    if ((bool)rb && !rb.isKinematic)
    {
      return;
    }

    var id = GetParentID(netView.m_zdo);
    if (id == 0) return;

    var parentObj = ZDOPersistentID.Instance.GetGameObject(id);
    if ((bool)parentObj)
    {
      var controller = parentObj.GetComponent<BaseVehicleController>();
      Logger.LogDebug($"ParentObj {parentObj}");
      if ((bool)controller)
      {
        Logger.LogDebug("ActivatingBaseVehicle piece");
        controller.ActivatePiece(netView);
      }
    }
    else
    {
      Logger.LogDebug($"adding inactive piece, {id} {netView.m_zdo}");
      AddInactivePiece(id, netView);
    }
  }

  public void ActivatePiece(ZNetView netView)
  {
    if ((bool)netView)
    {
      netView.transform.SetParent(transform);
      netView.transform.localPosition = netView.m_zdo.GetVec3(MBPositionHash, Vector3.zero);
      netView.transform.localRotation =
        Quaternion.Euler(netView.m_zdo.GetVec3(MBRotationVecHash, Vector3.zero));
      var wnt = netView.GetComponent<WearNTear>();
      if ((bool)wnt) wnt.enabled = true;

      AddPiece(netView);
    }
  }

  public void AddTemporaryPiece(Piece piece)
  {
    piece.transform.SetParent(transform);
  }

  public void AddNewPiece(Piece piece)
  {
    if (!(bool)piece)
    {
      Logger.LogError("piece does not exist");
      return;
    }

    if (!(bool)piece.m_nview)
    {
      Logger.LogError("m_nview does not exist on piece");
      return;
    }

    if (hasDebug) Logger.LogDebug("Added new piece is valid");
    AddNewPiece(piece.m_nview);
  }

  public void AddNewPiece(ZNetView netView)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist");
      return;
    }

    Logger.LogDebug($"netView exists {netView.name}");
    netView.transform.SetParent(transform);
    Logger.LogDebug($"netView set parent");
    Logger.LogDebug($"ZDOPersistentID instance {ZDOPersistentID.Instance}");
    if (netView.GetZDO() != null)
    {
      var newId = ZDOPersistentID.Instance.GetOrCreatePersistentID(netView.GetZDO());
      Logger.LogDebug(
        $"persistent id {newId}");
      netView.m_zdo.Set(MBParentIdHash, newId);
      Logger.LogDebug(
        $"netView.transform.localRotation.eulerAngles {netView.transform.localRotation.eulerAngles}");

      netView.m_zdo.Set(MBRotationVecHash, netView.transform.localRotation.eulerAngles);
      Logger.LogDebug(
        $"netView.transform.localRotation.eulerAngles {netView.transform.localPosition}");
      netView.m_zdo.Set(MBPositionHash, netView.transform.localPosition);
    }

    Logger.LogDebug($"made it end, about to call addpiece and init zdo");


    if (netView.GetZDO() == null)
    {
      Logger.LogError("NetView has no valid ZDO returning");
      return;
    }

    AddPiece(netView);
    InitZDO(netView.GetZDO());
  }

  public void AddPiece(ZNetView netView)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist but somehow called AddPiece()");
      return;
    }

    totalSailArea = 0;
    m_pieces.Add(netView);

    UpdatePieceCount();
    EncapsulateBounds(netView);
    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt && ValheimRaftPlugin.Instance.MakeAllPiecesWaterProof.Value)
      wnt.m_noRoofWear = false;

    var hull = netView.GetComponent<ShipHullComponent>();
    if ((bool)hull)
    {
      m_hullPieces.Add(hull);
    }

    var cultivatable = netView.GetComponent<CultivatableComponent>();
    if ((bool)cultivatable) cultivatable.UpdateMaterial();

    var mast = netView.GetComponent<MastComponent>();
    if ((bool)mast)
    {
      m_mastPieces.Add(mast);
    }

    var sail = netView.GetComponent<SailComponent>();
    if ((bool)sail)
    {
      m_sailPiece.Add(sail);
    }

    var ramp = netView.GetComponent<BoardingRampComponent>();
    if ((bool)ramp)
    {
      ramp.ForceRampUpdate();
      m_boardingRamps.Add(ramp);
    }

    var rudder = netView.GetComponent<RudderComponent>();
    if ((bool)rudder)
    {
      rudder.InitializeControls(netView, VehicleInstance);
      m_rudderPieces.Add(rudder);
    }

    var portal = netView.GetComponent<TeleportWorld>();
    if ((bool)portal) m_portals.Add(netView);

    var ladder = netView.GetComponent<RopeLadderComponent>();
    if ((bool)ladder)
    {
      m_ladders.Add(ladder);
      ladder.baseVehicleController = instance;
    }

    var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
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

    UpdateMass(netView);

    /*
     * @todo investigate why this is called. Determine if it is needed
     *
     * - most likely this is to prevent other rigidbody nodes from interacting with unity world physics within the ship
     */
    var rbs = netView.GetComponentsInChildren<Rigidbody>();
    for (var i = 0; i < rbs.Length; i++)
      if (rbs[i].isKinematic)
      {
        Logger.LogDebug($"destroying kinematic rbs");
        Destroy(rbs[i]);
      }
  }

  private void UpdatePieceCount()
  {
    if ((bool)m_nview && m_nview.m_zdo != null) m_nview.m_zdo.Set("MBPieceCount", m_pieces.Count);
  }

  public void EncapsulateBounds(ZNetView netView)
  {
    var piece = netView.GetComponent<Piece>();
    var colliders = piece
      ? piece.GetAllColliders()
      : new List<Collider>(netView.GetComponentsInChildren<Collider>());
    var door = netView.GetComponentInChildren<Door>();
    var ladder = netView.GetComponent<RopeLadderComponent>();
    var rope = netView.GetComponent<RopeAnchorComponent>();
    if (!door && !ladder && !rope) m_bounds.Encapsulate(netView.transform.localPosition);

    Logger.LogDebug(
      $"Colliders set, m_floatcollider: {m_floatcollider}, m_onboardcollider: {m_onboardcollider}, m_blockingcollider: {m_blockingcollider}");

    for (var i = 0; i < colliders.Count; i++)
    {
      if (m_floatcollider) Physics.IgnoreCollision(colliders[i], m_floatcollider, true);
      if (m_blockingcollider) Physics.IgnoreCollision(colliders[i], m_blockingcollider, true);
      if (m_onboardcollider)
        Physics.IgnoreCollision(colliders[i], m_onboardcollider, true);
    }

    m_blockingcollider.size = new Vector3(m_bounds.size.x,
      ValheimRaftPlugin.Instance.BlockingColliderVerticalSize.Value, m_bounds.size.z);
    m_blockingcollider.center = new Vector3(m_bounds.center.x,
      ValheimRaftPlugin.Instance.BlockingColliderVerticalCenterOffset.Value, m_bounds.center.z);
    m_floatcollider.size = new Vector3(m_bounds.size.x,
      ValheimRaftPlugin.Instance.FloatingColliderVerticalSize.Value, m_bounds.size.z);
    m_floatcollider.center = new Vector3(m_bounds.center.x,
      ValheimRaftPlugin.Instance.FloatingColliderVerticalCenterOffset.Value, m_bounds.center.z);
    m_onboardcollider.size = m_bounds.size;
    m_onboardcollider.center = m_bounds.center;
  }

  internal int GetPieceCount()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      Logger.LogDebug($"GetPieceCount netview or ZDO null {m_pieces.Count}");
      return m_pieces.Count;
    }

    var count = m_nview.m_zdo.GetInt("MBPieceCount", m_pieces.Count);

    Logger.LogDebug($"GetPieceCount() {count}");
    return count;
  }
}