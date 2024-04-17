using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using ValheimVehicles.Vehicles;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Components;
using static ValheimVehicles.Propulsion.Sail.SailAreaForce;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;
using PrefabNames = ValheimVehicles.Prefabs.PrefabNames;

namespace ValheimVehicles.Vehicles;

using Vehicles_BaseVehicleController = Vehicles.BaseVehicleController;

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

  public static readonly int MBPieceCount = "MBPieceCount".GetStableHashCode();

  public static readonly string ZdoKeyBaseVehicleInitState =
    "ValheimVehicles_BaseVehicle_Initialized";

  /*
   * Get all the instances statically
   */
  public static Dictionary<int, BaseVehicleController> ActiveInstances = new();

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

  // rigidbody for all pieces within the ship. Does not directly contribute to floatation, floatation controlled by m_syncRigidbody and synced to this m_rigidbody
  internal Rigidbody? m_rigidbody;

  // for the ship physics without item piece colliders or alternatively access via VehicleInstance.m_body
  internal Rigidbody? m_syncRigidbody;

  internal List<ZNetView> m_pieces = [];
  internal List<ShipHullComponent> m_hullPieces = [];

  internal List<MastComponent> m_mastPieces = [];

  internal List<SailComponent> m_sailPieces = [];


  // todo make a patch to fix coordinates on death to send player to the correct zdo location.
  // bed component
  internal List<Bed> m_bedPieces = [];


  // ship rudders
  internal List<RudderComponent> m_rudderPieces = [];

  // wheels for rudders
  internal List<RudderWheelComponent> m_rudderWheelPieces = [];

  internal List<ZNetView> m_portals = [];

  internal List<RopeLadderComponent> m_ladders = [];

  internal List<BoardingRampComponent> m_boardingRamps = [];

  internal enum InitializationState
  {
    Pending, // when the ship has a pending state
    Complete, // when the ship loads as an existing ship and has pieces.
    Created, // when the ship is created with 0 pieces
  }

  private InitializationState _baseVehicleInitializationState = InitializationState.Pending;

  internal InitializationState BaseVehicleInitState
  {
    get => _baseVehicleInitializationState;
    set
    {
      if (!Enum.IsDefined(typeof(InitializationState), value))
        throw new InvalidEnumArgumentException(nameof(value), (int)value,
          typeof(InitializationState));
      _baseVehicleInitializationState = value;
      OnBaseVehicleInitializationStateChange(value);
    }
  }

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
  internal BoxCollider m_floatcollider = new();
  internal BoxCollider m_onboardcollider = new();

  private int _persistentZdoId;

  public int PersistentZdoId => GetPersistentID();

  public bool m_statsOverride;
  private static bool itemsRemovedDuringWait;
  private Coroutine? _pendingPiecesCoroutine;
  private Coroutine? _serverUpdatePiecesCoroutine;
  private Coroutine? _bedUpdateCoroutine;
  GUIStyle myButtonStyle;

  private void OnGUI()
  {
    if (myButtonStyle == null)
    {
      myButtonStyle = new GUIStyle(GUI.skin.button);
      myButtonStyle.fontSize = 50;
    }

    GUILayout.BeginArea(new Rect(150, 10, 100, 100), myButtonStyle);
    if (GUILayout.Button("activatePendingPieces"))
    {
      ActivatePendingPiecesCoroutine();
    }

    if (GUILayout.Button("rebuild bounds"))
    {
      RebuildBounds();
    }

    GUILayout.EndArea();
  }

  /**
   * Side Effect to be used when initialization state changes. This allows for starting the ActivatePendingPiecesCoroutine
   */
  private void OnBaseVehicleInitializationStateChange(InitializationState state)
  {
    if (state != InitializationState.Complete) return;

    ActivatePendingPiecesCoroutine();
  }

  public void LoadInitState()
  {
    if (!m_nview)
    {
      BaseVehicleInitState = InitializationState.Pending;
    }

    var initialized = m_nview.GetZDO().GetBool(ZdoKeyBaseVehicleInitState);

    BaseVehicleInitState = initialized ? InitializationState.Complete : InitializationState.Created;
  }

  public void SetInitComplete()
  {
    m_nview.GetZDO().Set(ZdoKeyBaseVehicleInitState, true);
    BaseVehicleInitState = InitializationState.Complete;
  }

  public void SetColliders(VehicleShip vehicleInstance)
  {
    var colliders = vehicleInstance.transform.GetComponentsInChildren<BoxCollider>();

    // defaults to a new boxcollider if somehow things are not detected
    m_onboardcollider =
      colliders.FirstOrDefault(
        (k) => k.gameObject.name.Contains(PrefabNames.WaterVehicleOnboardCollider)) ??
      new BoxCollider();
    m_floatcollider = vehicleInstance.FloatCollider;
    m_blockingcollider =
      colliders.FirstOrDefault((k) =>
        k.gameObject.name.Contains(PrefabNames.WaterVehicleBlockingCollider)) ??
      new BoxCollider();

    // todo the local scales cause issues with floating with new ships until an item is manually placed by the player
    if (m_onboardcollider != null)
    {
      m_onboardcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    if (m_floatcollider != null)
    {
      m_floatcollider.transform.localScale = new Vector3(1f, 1f, 1f);
      // m_floatcollider.size = new Vector3(4f, 1f, 2f); // size of raft hull
    }

    if (m_blockingcollider != null)
    {
      m_blockingcollider.transform.localScale = new Vector3(1f, 1f, 2f);
      m_blockingcollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
      m_blockingcollider.transform.parent.gameObject.layer =
        ValheimRaftPlugin.CustomRaftLayer;
    }
  }


  public void FireErrorOnNull(Collider obj, string name)
  {
    if (!(bool)obj)
    {
      Logger.LogError($"BaseVehicleError: collider not initialized for <{name}>");
    }
  }

  public void ValidateInitialization()
  {
    // colliders that must be valid
    FireErrorOnNull(m_floatcollider, PrefabNames.WaterVehicleFloatCollider);
    FireErrorOnNull(m_blockingcollider, PrefabNames.WaterVehicleBlockingCollider);
    FireErrorOnNull(m_onboardcollider, PrefabNames.WaterVehicleOnboardCollider);
  }

  private void HideGhostContainer()
  {
    VehicleInstance?.Instance.GhostContainer.SetActive(false);
  }

  public void Awake()
  {
    instance = this;
    hasDebug = ValheimRaftPlugin.Instance.HasDebugBase.Value;

    if (!(bool)m_rigidbody)
    {
      m_rigidbody = GetComponent<Rigidbody>();
    }

    Debug.Log("Captured Log"); // Breadcrumb
    Debug.LogWarning("Captured Warning"); // Breadcrumb
    Debug.LogError("This is a Test error called within BaseVehicleController.Awake");
  }

  public void UpdateBedSpawn()
  {
    foreach (var mBedPiece in m_bedPieces)
    {
      if (!(bool)mBedPiece.m_nview) continue;

      var zdoPosition = mBedPiece.m_nview.m_zdo.GetPosition();
      if (zdoPosition == mBedPiece.m_spawnPoint.position)
      {
        continue;
      }

      mBedPiece.m_spawnPoint.position = zdoPosition;
    }
  }

  IEnumerable UpdateBedSpawnWorker()
  {
    UpdateBedSpawn();

    yield return new WaitForSeconds(3);
  }

  /*
   * Possible alternatives to this approach:
   * - Add a setter that triggers initializeBaseVehicleValues when the zdo is falsy -> truthy
   */
  public void InitializeBaseVehicleValuesWhenReady()
  {
    if (m_nview == null)
    {
      return;
    }

    if (_persistentZdoId == 0)
    {
      _persistentZdoId = GetPersistentID();
    }

    LoadInitState();

    HideGhostContainer();
    // encapsulate ensures that the float collider will never be smaller than the boat hull size IE the initial objects
    // m_bounds.Encapsulate(m_nview.transform.localPosition);


    // Instances allows getting the instance from a ZDO
    // OR something queryable on a ZDO making it much easier to debug and eventually update items
    if (!ActiveInstances.GetValueSafe(PersistentZdoId))
    {
      ActiveInstances.Add(PersistentZdoId, this);
    }
  }

  public virtual void Start()
  {
    ValidateInitialization();

    if (!(bool)ZNet.instance)
    {
      return;
    }

    Logger.LogInfo($"BaseVehicleController pieces {m_pieces.Count}");
    Logger.LogInfo($"BaseVehicleController pendingPieces {m_pendingPieces.Count}");
    Logger.LogInfo($"BaseVehicleController allPieces {m_allPieces.Count}");

    /*
     * This should work on both client and server, but the garbage collecting should only apply if the ZDOs are not persistent
     */
    if (ZNet.instance.IsDedicated())
    {
      _serverUpdatePiecesCoroutine = StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    // _bedUpdateCoroutine = StartCoroutine(nameof(UpdateBedSpawnWorker));
  }

  protected int GetPersistentID()
  {
    Logger.LogDebug($"GetPersistentID, called {name}");
    if (!(bool)m_nview)
    {
      _persistentZdoId = 0;
      return _persistentZdoId;
    }

    if (m_nview.GetZDO() == null)
    {
      _persistentZdoId = 0;
      return _persistentZdoId;
    }

    _persistentZdoId = ZDOPersistentID.Instance.GetOrCreatePersistentID(m_nview.GetZDO());
    Logger.LogInfo($"BaseVehicleController _persistentZdoId: {_persistentZdoId}");
    return _persistentZdoId;
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

    var nv = GetComponent<ZNetView>();

    if (nv)
    {
      m_nview = nv;
    }

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
    RemovePlayerFromBoat();

    if (ActiveInstances.GetValueSafe(_persistentZdoId))
    {
      ActiveInstances.Remove(_persistentZdoId);
    }

    if (_pendingPiecesCoroutine != null)
    {
      StopCoroutine(_pendingPiecesCoroutine);
    }

    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }

    if (!ZNetScene.instance || _persistentZdoId == 0) return;

    foreach (var piece in m_pieces)
    {
      if ((bool)piece)
      {
        piece.transform.SetParent(null);
        AddInactivePiece(_persistentZdoId, piece);
      }
    }
  }

  private void Sync()
  {
    if (!(bool)m_syncRigidbody || !(bool)m_rigidbody) return;
    m_rigidbody.MovePosition(m_syncRigidbody.transform.position);
    m_rigidbody.MoveRotation(m_syncRigidbody.transform.rotation);
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

    if (m_sector != m_serverSector) ServerSyncAllPieces();

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
      if (id != _persistentZdoId)
      {
        list.FastRemoveAt(i);
        i--;
        continue;
      }

      zdo.SetPosition(pos);
    }

    UpdateBedSpawn();
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
      // if (_pendingPiecesCoroutine != null)
      // {
      //   
      // };
      if (!m_nview)
      {
        yield return new WaitUntil(() => (bool)m_nview);
      }

      var time = Time.realtimeSinceStartup;
      var output = m_allPieces.TryGetValue(_persistentZdoId, out var list);
      if (!output)
      {
        yield return new WaitForSeconds(Math.Max(2f,
          ValheimRaftPlugin.Instance.ServerRaftUpdateZoneInterval
            .Value));
        continue;
      }

      yield return UpdatePiecesWorker(list);
      yield return new WaitForEndOfFrame();
    }
    // ReSharper disable once IteratorNeverReturns
  }

  // this needs to be connected to ropeladder too.
  internal float GetColliderBottom()
  {
    return m_blockingcollider.transform.position.y + m_blockingcollider.center.y -
           m_blockingcollider.size.y / 2f;
  }

  public static void AddInactivePiece(int id, ZNetView netView)
  {
    if (hasDebug) Logger.LogDebug($"addInactivePiece called with {id} for {netView.name}");

    if (ActiveInstances.TryGetValue(id, out var activeInstance))
    {
      activeInstance.ActivatePiece(netView);
      return;
    }

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

    if ((bool)m_rigidbody)
    {
      m_rigidbody.mass = 1000f + TotalMass;
    }

    if ((bool)m_syncRigidbody)
    {
      m_syncRigidbody.mass = 1000f + TotalMass;
    }
  }

  public void RemovePiece(ZNetView netView)
  {
    if (netView.name.Contains(PrefabNames.WaterVehicleContainer)) return;
    if (m_pieces.Remove(netView))
    {
      UpdateMass(netView, true);
      RebuildBounds();

      var hull = netView.GetComponent<ShipHullComponent>();
      if ((bool)hull)
      {
        // todo
        // only rebuilds bounds for hull pieces. If the player does not use a hull it will not increase boat size.
        // RebuildBounds();
        m_hullPieces.Remove(hull);
      }

      var sail = netView.GetComponent<SailComponent>();
      if ((bool)sail)
      {
        m_sailPieces.Remove(sail);
      }

      var mast = netView.GetComponent<MastComponent>();
      if ((bool)mast)
      {
        m_mastPieces.Remove(mast);
      }

      var rudder = netView.GetComponent<RudderComponent>();
      if ((bool)rudder) m_rudderPieces.Remove(rudder);

      var wheel = netView.GetComponent<RudderWheelComponent>();
      if ((bool)wheel) m_rudderWheelPieces.Remove(wheel);


      var bed = netView.GetComponent<Bed>();
      if ((bool)bed) m_bedPieces.Remove(bed);

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

    // if (GetPieceCount() == 0)
    // {
    //   DestroyVehicle();
    // }
    // else
    // {
    //   UpdateStats();
    // }
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

    // todo figure out hull weight like 20 woood per hull. Also calculate buoyancy from hull wood
    if (pieceName == PrefabNames.ShipHullCoreWoodHorizontal)
    {
      return 20f;
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


  /**
   * prevent ship destruction on m_nview null
   * - if null it would prevent getting the ZDO information for the ship pieces
   */
  public void DestroyPiece(WearNTear wnt)
  {
    if ((bool)wnt)
    {
      if (wnt.name.Contains(PrefabNames.WaterVehicleContainer))
      {
        // prevents a loop of DestroyPiece being called from WearNTear_Patch
        return;
      }

      var netview = wnt.GetComponent<ZNetView>();
      RemovePiece(netview);
      UpdatePieceCount();
      totalSailArea = 0f;
    }


    var pieceCount = GetPieceCount();

    if (pieceCount > 0 || m_nview == null) return;
    if (VehicleInstance?.Instance != null)
    {
      var wntShip = VehicleInstance.Instance.GetComponent<WearNTear>();
      if ((bool)wntShip) wntShip.Destroy();
    }
  }

  public void RemovePlayerFromBoat()
  {
    var players = Player.GetAllPlayers();
    foreach (var t in players.Where(t => (bool)t && t.transform.parent == transform))
      t.transform.SetParent(null);
  }

  /*
   * TODO figure out why this is exiting too early when there are items
   */
  public void DestroyVehicle()
  {
    var wntVehicle = instance.GetComponent<WearNTear>();

    RemovePlayerFromBoat();

    if ((bool)wntVehicle)
      wntVehicle.Destroy();
    else if (instance)
    {
      Destroy(instance);
    }

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

    // do not run if in a Pending or Created state
    if (BaseVehicleInitState != InitializationState.Complete && m_pendingPieces.Count == 0) return;

    _pendingPiecesCoroutine = StartCoroutine(nameof(ActivatePendingPieces));
  }

  public IEnumerator ActivatePendingPieces()
  {
    if (!(bool)m_nview || m_nview == null)
    {
      yield return new WaitUntil(() => (bool)m_nview);
    }

    if (BaseVehicleInitState != InitializationState.Complete)
    {
      yield return null;
    }

    if (m_nview.GetZDO() == null)
    {
      Logger.LogDebug("m_zdo is null for activate pending pieces");
      yield return new WaitUntil(() => m_nview.m_zdo != null);
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

      // this is commented out b/c it may be triggering the destroy method guard at the bottom.
      list.Clear();
      m_pendingPieces.Remove(id);
    }

    if (hasDebug)
      Logger.LogDebug($"Ship Size calc is: m_bounds {m_bounds} bounds size {m_bounds.size}");

    m_dynamicObjects.TryGetValue(_persistentZdoId, out var objectList);
    var objectListHasNoValidItems = true;
    if (objectList is { Count: > 0 })
    {
      Logger.LogDebug($"m_dynamicObjects is valid: {objectList.Count}");
      for (var i = 0; i < objectList.Count; i++)
      {
        var go = ZNetScene.instance.FindInstance(objectList[i]);

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

      m_dynamicObjects.Remove(_persistentZdoId);
    }

    /*
     * This prevents empty Prefabs of MBRaft from existing
     * @todo make this only apply for boats with no objects in any list
     */
    if (list is { Count: 0 } &&
        (m_dynamicObjects.Count == 0 || objectListHasNoValidItems)
       )
    {
      Logger.LogError(
        $"found boat with _persistentZdoId {_persistentZdoId}, without any items attached");
    }

    yield return null;
  }

  public static void AddDynamicParent(ZNetView source, GameObject target)
  {
    var bvc = target.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
    {
      source.m_zdo.Set(MBCharacterParentHash, bvc.PersistentZdoId);
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
        m_mastPieces.Count == 0 && m_sailPieces.Count == 0)
    {
      return totalSailArea;
    }

    totalSailArea = 0;
    customSailsArea = 0;
    numberOfTier1Sails = 0;
    numberOfTier2Sails = 0;
    numberOfTier3Sails = 0;

    var hasConfigOverride = ValheimRaftPlugin.Instance.EnableCustomPropulsionConfig.Value;

    foreach (var mMastPiece in m_mastPieces.ToList())
    {
      // prevent NRE from occuring if destroying the mastPiece but it still exists
      if (!mMastPiece)
      {
        m_mastPieces.Remove(mMastPiece);
        continue;
      }

      if (mMastPiece.name.Contains("MBRaftMast"))
      {
        ++numberOfTier1Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier1Area.Value
          : Tier1;
        totalSailArea += numberOfTier1Sails * multiplier;
      }
      else if (mMastPiece.name.Contains("MBKarveMast"))
      {
        ++numberOfTier2Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier2Area.Value
          : Tier2;
        totalSailArea += numberOfTier2Sails * multiplier;
      }
      else if (mMastPiece.name.Contains("MBVikingShipMast"))
      {
        ++numberOfTier3Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier3Area.Value
          : Tier3;
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
        : CustomTier1AreaForceMultiplier;

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

  public static void InitZdo(ZDO zdo)
  {
    var id = GetParentID(zdo);
    if (id != 0)
    {
      Logger.LogInfo($"InitZDO for id: {id}");
      if (!m_allPieces.TryGetValue(id, out var list))
      {
        list = [];
        m_allPieces.Add(id, list);
      }

      if (list.Contains(zdo))
      {
        Logger.LogWarning(
          $"ValheimVehicles.BaseVehicleController: The zdo {zdo.m_uid}, tried to be added when it already exists within the list. Please submit a bug if this issue shows up frequently.");
        return;
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
    if (id == 0 || !m_allPieces.TryGetValue(id, out var list)) return;
    list.FastRemove(zdo);
    itemsRemovedDuringWait = true;
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
        Logger.LogDebug($"zdoParent {zdoparent}, id: {id}");
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
    if (netView.name == $"{PrefabNames.WaterVehicleContainer}(Clone)")
    {
      return;
    }

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
      var vehicleShip = parentObj.GetComponent<VehicleShip>();
      Logger.LogDebug($"ParentObj {parentObj}");
      if (vehicleShip.Controller == null) return;
      Logger.LogDebug("ActivatingBaseVehicle piece");
      vehicleShip.Controller.Instance.ActivatePiece(netView);
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

  /**
   * True let's WearNTear destroy this vehicle
   *
   * this could also be used to force a re-render if the user attempts to destroy a raft with pending pieces, might as well run activate pending pieces.
   */
  public static bool CanDestroyVehicle(ZNetView netView)
  {
    if (!netView) return false;

    var bvc = netView.GetComponent<Vehicles_BaseVehicleController>();
    if (!(bool)bvc) return true;

    var hasPendingPieces =
      m_pendingPieces.TryGetValue(bvc.GetPersistentID(), out var pendingPieces);
    var hasPieces = bvc.GetPieceCount() != 0;

    // if there are pending pieces, do not let vehicle be destroyed
    if (pendingPieces != null && hasPendingPieces && pendingPieces.Count > 0)
    {
      return false;
    }

    return !hasPieces;
  }

  public void AddNewPiece(ZNetView netView)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist");
      return;
    }

    if (m_pieces.Contains(netView))
    {
      Logger.LogWarning($"NetView already is added. name: {netView.name}");
      return;
    }

    var previousCount = GetPieceCount();

    Logger.LogDebug($"netView exists {netView.name}");
    netView.transform.SetParent(transform);
    Logger.LogDebug($"netView set parent");
    Logger.LogDebug($"ZDOPersistentID instance {ZDOPersistentID.Instance}");
    if (netView.m_zdo != null)
    {
      netView.m_zdo.Set(MBParentIdHash, PersistentZdoId);
      Logger.LogDebug(
        $"netView.transform.localRotation.eulerAngles {netView.transform.localRotation.eulerAngles}");

      netView.m_zdo.Set(MBRotationVecHash, netView.transform.localRotation.eulerAngles);
      Logger.LogDebug(
        $"netView.transform.localPosition {netView.transform.localPosition}");
      netView.m_zdo.Set(MBPositionHash, netView.transform.localPosition);
    }

    Logger.LogDebug($"made it end, about to call addpiece and init zdo");


    if (netView.GetZDO() == null)
    {
      Logger.LogError("NetView has no valid ZDO returning");
      return;
    }

    AddPiece(netView);
    InitZdo(netView.GetZDO());

    if (previousCount == 0 && GetPieceCount() == 1)
    {
      SetInitComplete();
    }
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
      hull.SetParentZdoId(PersistentZdoId);
      m_hullPieces.Add(hull);
    }

    var cultivatable = netView.GetComponent<CultivatableComponent>();
    if ((bool)cultivatable) cultivatable.UpdateMaterial();

    var mast = netView.GetComponent<MastComponent>();
    if ((bool)mast)
    {
      // ReSharper disable once MergeIntoPattern
      if (VehicleInstance?.Instance && mast.m_allowSailShrinking && mast.m_allowSailShrinking)
      {
        // mast.transform.SetParent(VehicleInstance.Instance.m_mastObject.transform);
      }

      m_mastPieces.Add(mast);
    }

    var sail = netView.GetComponent<SailComponent>();
    if ((bool)sail)
    {
      m_sailPieces.Add(sail);
    }

    var bed = netView.GetComponent<Bed>();
    if ((bool)bed)
    {
      m_bedPieces.Add(bed);
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
      m_rudderPieces.Add(rudder);
    }


    var rudderWheel = netView.GetComponent<RudderWheelComponent>();
    if ((bool)rudderWheel)
    {
      rudderWheel.InitializeControls(netView, VehicleInstance);
      m_rudderWheelPieces.Add(rudderWheel);
    }

    var portal = netView.GetComponent<TeleportWorld>();
    if ((bool)portal) m_portals.Add(netView);

    var ladder = netView.GetComponent<RopeLadderComponent>();
    if ((bool)ladder)
    {
      m_ladders.Add(ladder);
      ladder.baseVehicleController = instance;
    }

    /*
     * Very very important. It fixes shadow flicker on all of valheim's prefabs with boats. If this is removed, the raft is seizure inducing.
     */
    var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
    foreach (var meshRenderer in meshes)
    {
      foreach (var meshRendererMaterial in meshRenderer.materials)
      {
        var isBlackMarble = meshRendererMaterial.name.Contains("blackmarble");
        if (isBlackMarble)
        {
          meshRendererMaterial.SetFloat("_TriplanarLocalPos", 1f);
        }
      }

      if ((bool)meshRenderer.sharedMaterial)
      {
        // todo disable triplanar shader which causes shader to move on black marble
        var sharedMaterials = meshRenderer.sharedMaterials;

        for (var j = 0; j < sharedMaterials.Length; j++)
        {
          var material = new Material(sharedMaterials[j]);
          var isBlackMarble = sharedMaterials[j].name.Contains("blackmarble");
          if (isBlackMarble)
          {
            material.SetFloat("_TriplanarLocalPos", 1f);
          }

          material.SetFloat("_RippleDistance", 0f);
          material.SetFloat("_ValueNoise", 0f);
          sharedMaterials[j] = material;
        }

        meshRenderer.sharedMaterials = sharedMaterials;
      }
    }

    UpdateMass(netView);

    /*
     * @todo investigate why this is called. Determine if it is needed
     *
     * - most likely this is to prevent other rigidbody nodes from interacting with unity world physics within the ship
     */
    var rbs = netView.GetComponentsInChildren<Rigidbody>();
    for (var i = 0; i < rbs.Length; i++)
    {
      var rbsItem = rbs[i];
      if (rbsItem.isKinematic)
      {
        Logger.LogDebug($"destroying kinematic rbs");
        Destroy(rbsItem);
      }
    }

    if (hasDebug)
      Logger.LogDebug(
        $"After Adding Piece: {netView.name}, Ship Size calc is: m_bounds {m_bounds} bounds size {m_bounds.size}");
  }

  private void UpdatePieceCount()
  {
    if ((bool)m_nview && m_nview.m_zdo != null) m_nview.m_zdo.Set(MBPieceCount, m_pieces.Count);
  }

  /**
   * bounds cannot be decapsulated by default so regenerating it seems prudent on piece removal
   */
  /**
   * bounds cannot be decapsulated by default so regenerating it seems prudent on piece removal
   */
  public void RebuildBounds()
  {
    if (!(bool)m_floatcollider || !(bool)m_onboardcollider || !(bool)m_blockingcollider)
    {
      return;
    }

    m_bounds = new Bounds();

    foreach (var netView in m_pieces)
    {
      EncapsulateBounds(netView);
    }

    OnBoundsChangeUpdateShipColliders();
  }

  public void OnBoundsChangeUpdateShipColliders()
  {
    if (!(bool)m_blockingcollider || !(bool)m_floatcollider || !(bool)m_onboardcollider)
    {
      Logger.LogWarning(
        "Ship colliders updated but the ship was unable to access colliders on ship object. Likely cause is ZoneSystem destroying the ship");
      return;
    }

    if (hasDebug)
    {
      Logger.LogDebug($"floatcollider size before {m_floatcollider.size}");
      Logger.LogDebug($"floatcollider size after {m_floatcollider.size}");
    }

    // old approach
    // m_blockingcollider.size = new Vector3(m_bounds.size.x,
    //   ValheimRaftPlugin.Instance.BlockingColliderVerticalSize.Value, m_bounds.size.z);
    // m_blockingcollider.center = new Vector3(m_bounds.center.x,
    //   ValheimRaftPlugin.Instance.BlockingColliderVerticalCenterOffset.Value, m_bounds.center.z);
    // m_floatcollider.size = new Vector3(m_bounds.size.x,
    //   ValheimRaftPlugin.Instance.FloatingColliderVerticalSize.Value, m_bounds.size.z);
    // m_floatcollider.center = new Vector3(m_bounds.center.x,
    //   ValheimRaftPlugin.Instance.FloatingColliderVerticalCenterOffset.Value, m_bounds.center.z);
    // m_onboardcollider.size = m_bounds.size;
    // m_onboardcollider.center = m_bounds.center;
    // end of OLD APPROACH!

    m_blockingcollider.size = new Vector3(m_bounds.size.x,
      ValheimRaftPlugin.Instance.BlockingColliderVerticalSize.Value, m_bounds.size.z);
    m_blockingcollider.center = new Vector3(m_bounds.center.x,
      ValheimRaftPlugin.Instance.BlockingColliderVerticalSize.Value / 2, m_bounds.center.z);

    // VehicleShip_FloatCollider
    // center -1.5992 -2.0976 1.3841
    // extents 3.6007 0.2688 6.6194
    // size 7.2014 0.5376 13.2389
    // bounds -1.5992 -2.0976 1.3841


    // watervehicle controller bounds
    // size 7.2014 4.1465 13.2389
    // extents 3.6007 2.0733 6.6194
    // center -1.5992 -1.0518 1.3841
    // min -5.1999 -3.1251 -5.2354
    // max 2.0015 1.0215 8.0035

    // VehicleSHip rb
    // position -190.9417 26.5681 4415.641
    // world center -188.0447 28.0174 4418
    //
    // vehicle pieces rigidbody
    // world center -191.3834 23.238 4416.917
    // position -191.2324 28.0026 4415.408

    m_floatcollider.size = new Vector3(m_bounds.size.x,
      m_floatcollider.size.y, m_bounds.size.z);
    m_floatcollider.center = new Vector3(m_bounds.center.x,
      -0.2f, m_bounds.center.z);
    m_floatcollider.transform.localPosition =
      new Vector3(m_bounds.center.x, -2f, m_bounds.center.y);
    /*?
     * onboard colliders need to be higher than the items placed on the ship.
     *
     * todo make this logic exact.
     * - Have a minimum "deck" position and determine height based on the deck. For now this do not need to be done
     */
    m_onboardcollider.size = new Vector3(m_bounds.size.x, m_bounds.size.y, m_bounds.size.z);
    m_onboardcollider.center = new Vector3(m_bounds.size.x, Mathf.Max(3f, m_bounds.size.y),
      m_bounds.size.z);

    if (hasDebug)
    {
      Logger.LogDebug($"floatcollider center before {m_floatcollider.center}");
      Logger.LogDebug($"floatcollider center after {m_floatcollider.center}");
    }
  }

  public void IgnoreShipColliders(List<Collider> colliders)
  {
    foreach (var t in colliders)
    {
      if (m_floatcollider) Physics.IgnoreCollision(t, m_floatcollider, true);
      if (m_blockingcollider) Physics.IgnoreCollision(t, m_blockingcollider, true);
      if (m_onboardcollider)
        Physics.IgnoreCollision(t, m_onboardcollider, true);
    }
  }

  /**
   * updating rotation point of object is important as it will properly fix how rotation works at the helm
   *
   * Using rigidbody center of mass could also work, but it then breaks physics for objects that are now outside the rigidbody center of mass (since it shifts all colliders)
   */
  public void OnBoundsChangeUpdateShipRotationPoint()
  {
    // todo add a way to toggle this for a specific wheelPiece or rudderPiece based on a saved ZDO flag
    if (m_rudderPieces.Count > 0)
    {
      var firstPiece = m_rudderPieces.First();
      VehicleInstance.Instance.shipRotationObj = firstPiece.gameObject;
    }
    else if (m_rudderWheelPieces.Count > 0)
    {
      var firstPiece = m_rudderWheelPieces.First();
      VehicleInstance.Instance.shipRotationObj = firstPiece.gameObject;
      // m_rigidbody.centerOfMass = firstPiece.transform.localPosition;
    }
    else
    {
      // m_rigidbody.ResetCenterOfMass();
      // m_syncRigidbody.ResetCenterOfMass();
      // m_rigidbody.centerOfMass = m_bounds.center;
    }
  }

  /**
   * @description alternative heavier approach to detecting bounds
   * - likely will cause issues if the object is invisible or not supposed to be in a specific layer
   * - check for layer piece
   * - check for enabled
   */
  public void EncapsulateAllChildrenWithinBounds(ZNetView netView)
  {
    if (hasDebug)
    {
      Logger.LogDebug(
        $"called EncapsulateAllChildrenWithinBounds, due to no colliders detected within netview named: {netView.name}");
    }

    var renderers = netView.GetComponentsInChildren<Renderer>();
    foreach (var renderer in renderers)
    {
      if (renderer == null) continue;
      if (!renderer.enabled && renderer.gameObject.layer == LayerMask.GetMask("piece"))
        m_bounds.Encapsulate(renderer.bounds);
    }
  }

  public static Bounds TransformBounds(Transform _transform, Bounds _localBounds)
  {
    var center = _transform.TransformPoint(_localBounds.center);

    // transform the local extents' axes
    var extents = _localBounds.extents;
    var axisX = _transform.TransformVector(extents.x, 0, 0);
    var axisY = _transform.TransformVector(0, extents.y, 0);
    var axisZ = _transform.TransformVector(0, 0, extents.z);

    // sum their absolute value to get the world extents
    extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
    extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
    extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

    return new Bounds { center = center, extents = extents };
  }

  public void EncapsulateColliders(List<Collider> colliders)
  {
    if (!(bool)m_floatcollider) return;

    if (m_bounds.size == Vector3.zero)
    {
      m_bounds = new Bounds();
      // m_bounds.Encapsulate(m_floatcollider.bounds);
    }

    foreach (var collider in colliders)
    {
      // very important to use local bounds otherwise it grabs world position point as center and causes the bounds to extend massively
      // var isGlobalPosition = collider.bounds. collider.bounds.center
      // var localCenterOffset = collider.bounds.center - netView.transform.position;
      // var localCenter = collider.transform.TransformPoint(collider.bounds.center);

      // first: Center(0.1,0.2,0.2)
      // Second: Center: (-471.940887, 20.7657166, 4952.95605), Extents: (0.50050354, 0.227736473, 0.512207031)

      var isLocalCenter = collider.transform.localPosition != collider.transform.position &&
                          collider.bounds.Contains(collider.transform.localPosition);

      var localCenter = collider.bounds.center;
      if (!isLocalCenter)
      {
        localCenter = collider.transform.position - collider.bounds.center;
      }

      // var localCenter = collider.transform.localPosition + localCenterOffset;
      var colliderWorldBounds = new Bounds(localCenter, collider.bounds.extents);
      // var localBounds = TransformBounds(collider.transform, collider.bounds);

      // first time initialization bounds will be zeroed
      m_bounds.Encapsulate(colliderWorldBounds);
    }
  }

  /**
   * Functional that updates targetBounds, useful for updating with new items or running off large lists and updating the newBounds value without mutating rigidbody values
   */
  public void EncapsulateBounds(ZNetView netView)
  {
    var piece = netView.GetComponent<Piece>();
    var colliders = piece
      ? piece.GetAllColliders()
      : new List<Collider>(netView.GetComponentsInChildren<Collider>());
    var door = netView.GetComponentInChildren<Door>();
    var ladder = netView.GetComponent<RopeLadderComponent>();
    var rope = netView.GetComponent<RopeAnchorComponent>();

    Logger.LogDebug($"previous m_bounds extents: {m_bounds.extents}");
    if (!door && !ladder && !rope)
    {
      // local position is in accurate but better
      // todo possibly use the heavier approach here if there are no colliders
      // if (colliders.Count == 0)
      // {
      //   var enableExactVehicleBounds = ValheimRaftPlugin.Instance.EnableExactVehicleBounds.Value;
      //   if (hasDebug)
      //   {
      //     Logger.LogWarning(
      //       "No colliders detected for piece, using centerpoint as encapsulation, FYI the raft could be inaccurately sized, consider enabling the <EnableExactVehicleBounds=true> which will likely fix this object");
      //   }
      //
      //   if (enableExactVehicleBounds)
      //   {
      //     EncapsulateAllChildrenWithinBounds(netView);
      //   }
      //   else
      //   {
      //     m_bounds.Encapsulate(netView.transform.localPosition);
      //   }
      // }
      m_bounds.Encapsulate(netView.transform.localPosition);

      // m_bounds.Encapsulate(netView.transform.localPosition);
      // EncapsulateColliders(colliders);
    }

    IgnoreShipColliders(colliders);
    OnBoundsChangeUpdateShipColliders();
    OnBoundsChangeUpdateShipRotationPoint();

    Logger.LogDebug($"current m_bounds extents (after Encapsulate): {m_bounds.extents}");
    Logger.LogDebug($"m_floatcollider: {m_floatcollider.bounds}");
  }

  // public void EncapsulateBounds(ZNetView netView)
  // {
  //   var piece = netView.GetComponent<Piece>();
  //   var colliders = piece
  //     ? piece.GetAllColliders()
  //     : new List<Collider>(netView.GetComponentsInChildren<Collider>());
  //   var door = netView.GetComponentInChildren<Door>();
  //   var ladder = netView.GetComponent<RopeLadderComponent>();
  //   var rope = netView.GetComponent<RopeAnchorComponent>();
  //
  //   Logger.LogDebug($"previous m_bounds extents: {m_bounds.extents}");
  //   if (!door && !ladder && !rope)
  //   {
  //     m_bounds.Encapsulate(netView.transform.localPosition);
  //   }
  //
  //   Logger.LogDebug($"current m_bounds extents (after Encapsulate): {m_bounds.extents}");
  //
  //   Logger.LogDebug($"m_floatcollider: {m_floatcollider.bounds}");
  //
  //   for (var i = 0; i < colliders.Count; i++)
  //   {
  //     if (m_floatcollider) Physics.IgnoreCollision(colliders[i], m_floatcollider, true);
  //     if (m_blockingcollider) Physics.IgnoreCollision(colliders[i], m_blockingcollider, true);
  //     if (m_onboardcollider)
  //       Physics.IgnoreCollision(colliders[i], m_onboardcollider, true);
  //   }
  //
  //   m_blockingcollider.size = new Vector3(m_bounds.size.x,
  //     ValheimRaftPlugin.Instance.BlockingColliderVerticalSize.Value, m_bounds.size.z);
  //   m_blockingcollider.center = new Vector3(m_bounds.center.x,
  //     ValheimRaftPlugin.Instance.BlockingColliderVerticalCenterOffset.Value, m_bounds.center.z);
  //   m_floatcollider.size = new Vector3(m_bounds.size.x,
  //     m_floatcollider.size.y, m_bounds.size.z);
  //   m_floatcollider.center = new Vector3(m_bounds.center.x,
  //     m_floatcollider.center.y, m_bounds.center.z);
  //   m_onboardcollider.size = m_bounds.size;
  //   m_onboardcollider.center = m_bounds.center;
  // }

  internal int GetPieceCount()
  {
    if (!m_nview || m_nview.m_zdo == null)
    {
      Logger.LogDebug(
        $"GetPieceCount ZNetView or ZDO null {m_pieces.Count}, this could cause boat to self destruct if it returns 0 at destroy piece");
      return m_pieces.Count;
    }

    var count = m_nview.m_zdo.GetInt(MBPieceCount, m_pieces.Count);

    Logger.LogDebug($"GetPieceCount() {count}");
    return count;
  }
}