using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Vehicles.Structs;
using ValheimVehicles.Vehicles.Controllers;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using ZdoWatcher;
using static ValheimVehicles.Propulsion.Sail.SailAreaForce;
using Component = UnityEngine.Component;
using Logger = Jotunn.Logger;
using Object = object;
using PrefabNames = ValheimVehicles.Prefabs.PrefabNames;

namespace ValheimVehicles.Vehicles;

/// <summary>controller used for all vehicles</summary>
/// <description> This is a controller used for all vehicles, Currently it must be initialized within a vehicle view IE VehicleShip or upcoming VehicleWheeled, and VehicleFlying instances.</description>
public class VehiclePiecesController : BasePiecesController, IMonoUpdater
{
  /*
   * Get all the instances statically
   */
  public static Dictionary<int, VehiclePiecesController>
    ActiveInstances = new();

  /// <summary>
  /// pieceDictionary with a key (vehicleId) and value as a list of netviews 
  /// </summary>
  public static Dictionary<int, List<ZNetView>> m_pendingPieces = new();
  public static Dictionary<int, List<ActivationPieceData>> m_pendingTempPieces = new();

  private List<ZNetView> _newPendingPiecesQueue = [];

  /// <summary>
  /// ZDOID of the fire component. Since these are nested it's much heavier to find it.
  /// </summary>
  public readonly Dictionary<ZDOID, EffectArea>
    cachedVehicleBurningEffectAreas = new();

  public static Dictionary<int, List<ZDO>> m_allPieces = new();

  /// <summary>
  /// These are materials or players that will not be persisted via ZDOs but are "on" the ship and will be added as a child of the ship
  /// </summary>
  public static Dictionary<int, List<ZDOID>>
    m_dynamicObjects = new();

  // public static Dictionary<ZDOID, ZNetView> temporaryMaterials = new();

  public static List<IMonoUpdater> MonoUpdaterInstances = [];

  private static bool _allowPendingPiecesToActivate = true;
  private bool _pendingPiecesDirty = false;

  private PendingPieceStateEnum
    _pendingPiecesState = PendingPieceStateEnum.Idle;


  private InitializationState _baseVehicleInitializationState =
    InitializationState.Pending;

  public InitializationState BaseVehicleInitState =>
    _baseVehicleInitializationState;
  
  public bool IsActivationComplete =>
    _pendingPiecesState is not PendingPieceStateEnum.Failure
      and not PendingPieceStateEnum.Running;

  public bool HasRunCleanup = false;

  // public ZNetView? LowestPiece = null;

  // public Vector3? GetLowestPiecePoint()
  // {
  //   if (!isActiveAndEnabled) return null;
  //   return transform.position;
  //   // if (LowestPiece == null) return null;
  //   // if (!LowestPiece.isActiveAndEnabled) return null;
  // }
  //
  // /// <summary>
  // /// May need to make this nullable and just exit.
  // /// </summary>
  // public Vector3 LowestPiecePoint =>
  //   LowestPiece?.transform?.position ?? transform?.position ?? Vector3.zero;

  /// <summary>
  /// Persists the collider after it has been set to prevent auto-ballast feature from losing the origin point.
  /// todo might be optional.
  /// </summary>
  public Vector3 BlockingColliderDefaultPosition = Vector3.zero;

  public Vector3 FloatColliderDefaultPosition = Vector3.zero;

  private static readonly string[] IgnoredPrefabNames =
  [
    PrefabNames.CustomWaterMask,
    PrefabNames.CustomWaterMaskCreator
  ];

  private static readonly Regex IgnoredAveragePointRegexp = new(
    $"^({string.Join("|", IgnoredPrefabNames.Select(Regex.Escape))})",
    RegexOptions.Compiled);


  public bool useWorldColliderPosition = false;

  // abstraction from convexHullAPI overridess it.
  public ConvexHullComponent convexHullComponent = null!;

  public List<MeshCollider> convexHullTriggerMeshColliders = [];
  public List<Collider> convexHullTriggerColliders = [];

  public static bool DEBUGAllowActivatePendingPieces
  {
    get => _allowPendingPiecesToActivate;
    set
    {
      if (value) ActivateAllPendingPieces();

      _allowPendingPiecesToActivate = value;
    }
  }

  internal FixedJoint m_fixedJoint;

  public List<ZNetView> m_nviewPieces = [];
  public List<ZNetView> m_tempPieces = [];
  internal List<ZNetView> m_ramPieces = [];
  internal List<ZNetView> m_anchorPieces = [];

  internal List<VehicleAnchorMechanismController> m_anchorMechanismComponents =
    [];

  internal List<ZNetView> m_hullPieces = [];

  internal List<MastComponent> m_mastPieces = [];

  internal List<SailComponent> m_sailPieces = [];


  // todo make a patch to fix coordinates on death to send player to the correct zdo location.
  // bed component
  internal List<Bed> m_bedPieces = [];


  // ship rudders
  internal List<RudderComponent> m_rudderPieces = [];

  // wheels for rudders
  internal List<SteeringWheelComponent> _steeringWheelPieces = [];

  internal List<ZNetView> m_portals = [];

  internal List<RopeLadderComponent> m_ladders = [];

  internal List<BoardingRampComponent> m_boardingRamps = [];

  private Transform? _piecesContainer;
  private Transform? _movingPiecesContainer;

  public static bool UseManualSync = true;

  public enum InitializationState
  {
    Pending, // when the ship has a pending state
    Complete, // when the ship loads as an existing ship and has pieces.
    Created // when the ship is created with 0 pieces
  }

  public enum PendingPieceStateEnum
  {
    Idle, // not started
    Scheduled, // called but not started
    Running, // running
    Failure, // failed
    Complete, // completed successfully
    ForceReset // forced to exit IE teleport or despawn or logout or command to destroy it.
  }

  /// <summary>
  /// For usage in debugging
  /// </summary>
  /// <returns></returns>
  public PendingPieceStateEnum PendingPiecesState => _pendingPiecesState;

  public float ShipMass = 0f;
  public static bool hasDebug => ValheimRaftPlugin.Instance.HasDebugBase.Value;

  public float TotalMass => ShipMass;

  private Transform piecesCollidersTransform;

  private Transform onboardColliderTransform;
  private Transform floatColliderTransform;

  /*
   * sail calcs
   */
  public int numberOfTier1Sails = 0;
  public int numberOfTier2Sails = 0;
  public int numberOfTier3Sails = 0;
  public int numberOfTier4Sails = 0;
  public float customSailsArea = 0f;

  public float cachedTotalSailArea = 0f;
  public float cachedSailForce = 0f;

  public virtual IVehicleShip? VehicleInstance { set; get; }
  private VehicleShip _vehicle;
  public int PersistentZdoId => VehicleInstance?.PersistentZdoId ?? 0;

  public VehicleMovementController? MovementController =>
    VehicleInstance?.MovementController;

  public VehicleWheelController? WheelController =>
    VehicleInstance?.WheelController;

  public VehicleOnboardController? OnboardController =>
    VehicleInstance?.OnboardController;

/* end sail calcs  */
  private Vector2i m_sector;
  private Vector2i m_serverSector;

  private Bounds _vehiclePieceBounds;
  private Bounds _vehicleHullBounds;

  // private Bounds _vehicleHullBounds;
  private Bounds _pendingVehicleBounds;
  private Bounds _pendingHullBounds;

  public BoxCollider? FloatCollider { get; set; }

  public BoxCollider? OnboardCollider { get; set; }

  internal Stopwatch InitializationTimer = new();
  internal Stopwatch PendingPiecesTimer = new();

  public bool m_statsOverride;
  private static bool itemsRemovedDuringWait;

  private static readonly int TriplanarLocalPos =
    Shader.PropertyToID("_TriplanarLocalPos");

  private static readonly int RippleDistance =
    Shader.PropertyToID("_RippleDistance");

  private static readonly int ValueNoise = Shader.PropertyToID("_ValueNoise");
  private Coroutine? _pendingPiecesCoroutine;
  private Coroutine? _serverUpdatePiecesCoroutine;
  private Coroutine? _bedUpdateCoroutine;

  public GameObject vehicleCenter;

  public List<Bed> GetBedPieces()
  {
    return m_bedPieces;
  }

  public Bounds GetVehicleBounds()
  {
    return _vehiclePieceBounds;
  }

  public List<ZNetView>? GetCurrentPendingPieces()
  {
    var persistentId = VehicleInstance?.PersistentZdoId;
    if (persistentId == null) return null;
    m_pendingPieces.TryGetValue(persistentId.Value,
      out var pendingPiecesList);
    return pendingPiecesList ?? null;
  }

  /**
   * Side Effect to be used when initialization state changes. This allows for starting the ActivatePendingPiecesCoroutine
   */
  private void OnBaseVehicleInitializationStateChange(InitializationState state)
  {
    if (state != InitializationState.Complete) return;

    StartActivatePendingPieces();
  }

  public static bool GetIsActivationComplete(
    VehiclePiecesController? piecesController)
  {
    return piecesController != null && piecesController.IsActivationComplete;
  }

  public void LoadInitState()
  {
    if (!VehicleInstance?.NetView || !VehicleInstance?.Instance ||
        !MovementController)
    {
      Logger.LogDebug(
        $"Vehicle setting state to Pending as it is not ready, must have netview: {VehicleInstance.NetView}, VehicleInstance {VehicleInstance?.Instance}, MovementController {MovementController}");
      _baseVehicleInitializationState = InitializationState.Pending;
      return;
    }

    var initialized = VehicleInstance?.NetView?.GetZDO()
      .GetBool(VehicleZdoVars.ZdoKeyBaseVehicleInitState) ?? false;

    _baseVehicleInitializationState = initialized
      ? InitializationState.Complete
      : InitializationState.Created;
  }

  public void SetInitComplete()
  {
    VehicleInstance?.NetView?.GetZDO()
      .Set(VehicleZdoVars.ZdoKeyBaseVehicleInitState, true);
    _baseVehicleInitializationState = InitializationState.Complete;
    IgnoreAllVehicleColliders();
  }


  public void FireErrorOnNull(Collider obj, string name)
  {
    if (!(bool)obj)
      Logger.LogError(
        $"BaseVehicleError: collider not initialized for <{name}>");
  }

  public void ValidateInitialization()
  {
    // colliders that must be valid
    FireErrorOnNull(FloatCollider, PrefabNames.WaterVehicleFloatCollider);
    FireErrorOnNull(OnboardCollider, PrefabNames.WaterVehicleOnboardCollider);
  }

  private void HideGhostContainer()
  {
    VehicleInstance?.Instance?.GhostContainer()?.SetActive(false);
  }

  /// <summary>
  /// Coroutine to init vehicle just in case things get delayed or desync. This allows for it to wait until things are ready without skipping critical initialization
  /// </summary>
  /// TODO this might need to be removed or more guards need to be added.
  /// <param name="vehicleShip"></param>
  /// <returns></returns>
  private IEnumerator InitVehicle(VehicleShip vehicleShip)
  {
    while (!(VehicleInstance?.NetView || !MovementController) &&
           InitializationTimer.ElapsedMilliseconds < 5000)
    {
      if (!VehicleInstance.NetView || !MovementController)
      {
        VehicleInstance = vehicleShip;
        yield return ZdoReadyStart();
      }

      if (!ActiveInstances.GetValueSafe(vehicleShip.PersistentZdoId))
        ActiveInstances.Add(vehicleShip.PersistentZdoId, this);

      yield return new WaitForFixedUpdate();
    }
  }

  /// <summary>
  /// Method to be called from the Parent Component that adds this VehiclePiecesController
  /// </summary>
  public void InitFromShip(VehicleShip vehicleShip)
  {
    _vehicle = vehicleShip;
    VehicleInstance = vehicleShip;

    var hasInitializedSuccessfully = InitializeBaseVehicleValuesWhenReady(vehicleShip);

    // wait more time if the vehicle is somehow taking forever to init
    if (!hasInitializedSuccessfully)
      StartCoroutine(InitVehicle(vehicleShip));
  }

  private IEnumerator ZdoReadyStart()
  {
    if (VehicleInstance?.Instance != null)
    {
      InitializeBaseVehicleValuesWhenReady(VehicleInstance.Instance);
    }

    if (VehicleInstance == null)
      Logger.LogError(
        "No ShipInstance detected");

    yield return null;
  }

  public static VehiclePiecesController? GetPieceControllerFromPlayer(
    GameObject playerGo)
  {
    var controller =
      playerGo.transform.root.GetComponent<VehiclePiecesController>();
    if (!controller)
      controller = playerGo.GetComponentInParent<VehiclePiecesController>();

    return controller != null ? controller : null;
  }

  /// <summary>
  /// Currently only exist within the top level transform but this may change so there is a getter
  /// </summary>
  /// <returns></returns>
  public Transform GetPiecesContainer()
  {
    // return transform.Find("pieces");
    return transform;
  }

  private GameObject _movingPiecesContainerObj;

  private Transform CreateMovingPiecesContainer()
  {
    if (_movingPiecesContainer) return _movingPiecesContainerObj.transform;

    var mpc = new GameObject()
    {
      name = $"{PrefabNames.VehicleMovingPiecesContainer}",
      transform =
        { position = transform.position, rotation = transform.rotation }
    };
    _movingPiecesContainerObj = mpc;

    return mpc.transform;
  }

  // private ZNetView tempZNetView;

  /// <summary>
  /// Todo add to prefab. Even test this in the VehiclePieceMovement controller would be worth it.
  /// </summary>
  public void CreatePieceCenter()
  {
    var controllerTransform = transform;
    vehicleCenter = new GameObject
    {
      name = "VehiclePiece_Center",
      transform = { position = controllerTransform.position, rotation = controllerTransform.rotation, parent = controllerTransform }
    };
  }

  /// <summary>
  /// ZNetScene must have the jobhandler so that when the scene is destroyed it can unload
  /// </summary>
  public void SetupJobHandlerOnZNetScene()
  {
    if (!m_convexHullJobHandlerObj)
    {
      m_convexHullJobHandlerObj = ZNetScene.instance.gameObject;
    }
  }


  public static bool HasSetupMeshClusterController = false;

  /// <summary>
  /// A setup function related to adding PrefabNames which is outside the sharedscripts scope in Unity. We need this for valheim game only.
  ///
  /// Must be called only 1 time.
  /// </summary>
  public void SetupMeshClusterController()
  {
    if (HasSetupMeshClusterController) return;

    MeshClusterController.PrefabExcludeNames.AddRange([
      PrefabNames.MBRopeLadder,
      PrefabNames.MBRopeAnchor,
      PrefabNames.ShipSteeringWheel
    ]);
    clusterThreshold = RenderingConfig.ClusterRenderingPieceThreshold.Value;
    HasSetupMeshClusterController = true;
  }

  public override void Awake()
  {
    SetupMeshClusterController();
    if (ZNetView.m_forceDisableInit) return;

    // must be called before base.awake()
    SetupJobHandlerOnZNetScene();
    
    base.Awake();
    
    
    if (vehicleCenter == null)
    {
      CreatePieceCenter();
    }

    if (piecesCollidersTransform == null)
      piecesCollidersTransform = transform.Find("colliders");

    // todo might not need float collider soon too.
    if (floatColliderTransform == null)
      floatColliderTransform =
        piecesCollidersTransform.Find(PrefabNames.WaterVehicleFloatCollider);

    if (onboardColliderTransform == null)
      onboardColliderTransform =
        piecesCollidersTransform.Find(PrefabNames.WaterVehicleOnboardCollider);

    
    IgnoreAllVehicleColliders();

    if (FloatCollider == null)
      FloatCollider = floatColliderTransform.GetComponent<BoxCollider>();

    if (OnboardCollider == null)
      OnboardCollider = onboardColliderTransform.GetComponent<BoxCollider>();

    // m_vehicleCollisionManager.AddObjectToVehicle(FloatCollider.gameObject);
    // m_vehicleCollisionManager.AddObjectToVehicle(OnboardCollider.gameObject);
    InitConvexHullGenerator();
    _piecesContainer = GetPiecesContainer();
    _movingPiecesContainer = CreateMovingPiecesContainer();
    m_localRigidbody = _piecesContainer.GetComponent<Rigidbody>();
    m_fixedJoint = _piecesContainer.GetComponent<FixedJoint>();
    InitializationTimer.Start();
  }

  private void InitConvexHullGenerator()
  {
    if (convexHullComponent == null)
      convexHullComponent = GetComponent<ConvexHullComponent>();

    if (convexHullComponent == null)
      convexHullComponent =
        gameObject.AddComponent<ConvexHullComponent>();
    
    convexHullComponent.MovementController = MovementController;
    // safety check, this will run after game-world loads especially if settings have changed.
    m_convexHullAPI = convexHullComponent;
    if (!ConvexHullAPI.HasInitialized)
    {
      // static
      convexHullComponent.InitializeConvexMeshGeneratorApi(
        ConvexHullComponent.GetConvexHullModeFromFlags(),
        LoadValheimVehicleAssets.DoubleSidedTransparentMat,
        LoadValheimVehicleAssets.WaterHeightMaterial,
        PhysicsConfig.convexHullDebuggerColor.Value,
        WaterConfig.UnderwaterBubbleEffectColor.Value, PrefabNames.ConvexHull,
        Logger.LogMessage);

      // instance
      convexHullComponent.PreviewParent = transform;
      convexHullComponent.transformPreviewOffset =
        PhysicsConfig.convexHullPreviewOffset.Value;
    }
  }

  private void LinkFixedJoint()
  {
    if (MovementController == null) return;
    if (!m_fixedJoint) m_fixedJoint = GetComponent<FixedJoint>();
    if (!m_fixedJoint)
    {
      m_fixedJoint = gameObject.AddComponent<FixedJoint>();
    }

    if (m_fixedJoint == null)
    {
      Logger.LogError(
        "No FixedJoint found. This means the vehicle is not syncing positions");
      return;
    }

    if (m_fixedJoint.connectedBody == null)
    {
      var movementPosition = MovementController.transform.position;
      var movementRotation = MovementController.transform.rotation;
      if (movementPosition != transform.position)
      {
        transform.position = movementPosition;
      }
      if (movementRotation != transform.rotation)
      {
        transform.rotation = movementRotation;
      }

    m_fixedJoint.connectedBody = MovementController!.GetRigidbody();
    }
  }


  public void UpdateBedSpawn()
  {
    foreach (var mBedPiece in m_bedPieces)
    {
      if (mBedPiece.m_nview == null) continue;
      if (mBedPiece.m_nview.m_zdo == null) continue;
      var zdoPosition = mBedPiece.m_nview.m_zdo.GetPosition();
      if (zdoPosition == mBedPiece.m_spawnPoint.position) continue;
      mBedPiece.m_spawnPoint.position = zdoPosition;
    }
  }

  private IEnumerable UpdateBedSpawnWorker()
  {
    yield return new WaitForSeconds(3);
  }

  /*
   * Possible alternatives to this approach:
   * - Add a setter that triggers initializeBaseVehicleValues when the zdo is falsy -> truthy
   */
  private bool InitializeBaseVehicleValuesWhenReady(VehicleShip vehicleShip)
  {
    if (ZNetView.m_forceDisableInit) return false;
    // vehicleInstance is the persistent ID, the pieceContainer only has a netView for syncing ship position
    if (vehicleShip.NetView == null)
    {
      Logger.LogWarning(
        "Warning netview not detected on vehicle, this means any netview attached events will not bind correctly");
      return false;
    }

    if (vehicleShip.vehicleMovementCollidersTransform)
    {
      // must set parent to be the vehicle colliders
      convexHullComponent.parentTransform = vehicleShip.vehicleMovementCollidersTransform;
    }
    
    LoadInitState();
    HideGhostContainer();
    LinkFixedJoint();
    return true;
  }

  public void InitializeBasePiecesControllerOverrides()
  {
    if (m_meshClusterComponent != null)
    {
      m_meshClusterComponent.IgnoreAllVehicleCollidersCallback = IgnoreAllVehicleColliders;
    }
  }

  public void Start()
  {
    ValidateInitialization();
    if (!(bool)ZNet.instance) return;
    if (hasDebug)
    {
      Logger.LogInfo($"pieces {m_nviewPieces.Count}");
      Logger.LogInfo($"pendingPieces {m_pendingPieces.Count}");
      Logger.LogInfo($"allPieces {m_allPieces.Count}");
    }

    InitializeBasePiecesControllerOverrides();

    if (VehicleInstance != null &&
        !ActiveInstances.ContainsKey(VehicleInstance.PersistentZdoId))
      ActiveInstances.Add(VehicleInstance.PersistentZdoId, this);

    StartClientServerUpdaters();
  }

  public override void OnDisable()
  {
    base.OnDisable();

    _isInvalid = true;
    
    if (MonoUpdaterInstances.Contains(this)) MonoUpdaterInstances.Remove(this);

    if (VehicleInstance != null &&
        ActiveInstances.GetValueSafe(VehicleInstance.PersistentZdoId))
      ActiveInstances.Remove(VehicleInstance.PersistentZdoId);

    InitializationTimer.Stop();
    if (_serverUpdatePiecesCoroutine != null)
      StopCoroutine(_serverUpdatePiecesCoroutine);

    CleanUp();
  }

  private void StartClientServerUpdaters()
  {
    if (!(bool)ZNet.instance) return;

    Logger.LogDebug($"IsDedicated : {ZNet.instance.IsDedicated()}");
    if (ZNet.instance.IsDedicated() && _serverUpdatePiecesCoroutine == null)
    {
      Logger.LogDebug("Calling UpdatePiecesInEachSectorWorker");
      _serverUpdatePiecesCoroutine =
        StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    StartActivatePendingPieces();
  }

  private void OnEnable()
  {
    HasRunCleanup = false;
    MonoUpdaterInstances.Add(this);
    InitializationTimer.Restart();

    if (vehicleCenter == null)
    {
      CreatePieceCenter();
    }

    StartClientServerUpdaters();
  }

  public void CleanUp()
  {
    if (HasRunCleanup) return;
    HasRunCleanup = true;
    RemovePlayersFromBoat();

    if (vehicleCenter)
    {
      Destroy(vehicleCenter);
    }

    if (_pendingPiecesCoroutine != null)
    {
      StopCoroutine(_pendingPiecesCoroutine);
      OnActivatePendingPiecesComplete(PendingPieceStateEnum.ForceReset);
    }

    if (_serverUpdatePiecesCoroutine != null)
      StopCoroutine(_serverUpdatePiecesCoroutine);

    if (VehicleInstance?.Instance == null)
      Logger.LogError("Cleanup called but there is no valid VehicleInstance");

    if (!ZNetScene.instance || VehicleInstance?.PersistentZdoId == null ||
        VehicleInstance?.PersistentZdoId == 0) return;


    for (var index = 0; index < m_nviewPieces.Count; index++)
    {
      var piece = m_nviewPieces[index];
      if (!piece)
      {
        m_nviewPieces.Remove(piece);
        index--;
        continue;
      }

      piece.transform.SetParent(null);
      AddInactivePiece(VehicleInstance!.PersistentZdoId, piece, null, true);
    }

    // todo might need to do some freezing of positions if these pieces are rigidbodies/physics related such as animals and npcs.
    for (var index = 0; index < m_tempPieces.Count; index++)
    {
      var tempPiece = m_tempPieces[index];
      if (!tempPiece)
      {
        m_tempPieces.Remove(tempPiece);
        index--;
        continue;
      }
      var zdo = tempPiece.GetZDO();
      if (zdo == null)
      {
        m_tempPieces.Remove(tempPiece);
        index--;
        continue;
      }
      // we must update position as these pieces/characters can move while on Vehicles.
      tempPiece.m_zdo.Set(VehicleZdoVars.MBPositionHash, tempPiece.transform.localPosition);
      tempPiece.transform.SetParent(null);
    }
  }

  public virtual void SyncRigidbodyStats(float drag, float angularDrag,
    bool flight)
  {
    if (!isActiveAndEnabled) return;
    if (MovementController?.m_body == null || m_statsOverride ||
        !VehicleInstance?.Instance || !m_localRigidbody)
      return;

    if (VehiclePhysicsMode.ForceSyncedRigidbody !=
        localVehiclePhysicsMode)
      SetVehiclePhysicsType(VehiclePhysicsMode.ForceSyncedRigidbody);

    m_localRigidbody.angularDrag = angularDrag;
    MovementController.m_body.angularDrag = angularDrag;

    m_localRigidbody.drag = drag;
    MovementController.m_body.drag = drag;

    // temp disable mass sync.
    // m_body.mass = mass;
    // MovementController.m_body.mass = mass;
  }

  public VehiclePhysicsMode localVehiclePhysicsMode =
    VehiclePhysicsMode.ForceSyncedRigidbody;

  public void SetVehiclePhysicsType(
    VehiclePhysicsMode physicsMode)
  {
    localVehiclePhysicsMode = physicsMode;
  }

  public float sailLeanAngle = 0f;
  public float sailDirectionDamping = 0.05f;

  // for adding additional multiplier within lower ranges so angle gets to expected value quicker.
  public static float rotationLeanMultiplier = 1.5f;
  
  /// <summary>
  /// Adds a leaning effect similar to sailing when wind is starboard/port pushing upwards/downwards. Cosmetic only to the ship. SailPower controls lean effect. Zero sails will not influence it.
  /// </summary>
  /// <returns></returns>
  public Quaternion GetRotationWithLean()
  {
    if (MovementController == null || VehicleInstance?.NetView == null)
      return Quaternion.identity;
    
    var baseRotation = MovementController.m_body.rotation;
    if (!PropulsionConfig.EXPERIMENTAL_LeanTowardsWindSailDirection.Value)
      return baseRotation;

    // no leaning when no sails.
    if (m_sailPieces.Count == 0) return baseRotation;

    // Normalize wind direction to [0, 360]
    var windDirection = Mathf.Repeat(MovementController.GetWindAngle(), 360f);

    var toValue = 0f;
    // Determine lean direction: port (-1) or starboard (1)
    float leanDirection;
    if (windDirection > 180f) // Port side (aft to left)
      leanDirection = -1f;
    else // Starboard side (aft to right)
      leanDirection = 1f;

    // Check if wind direction falls within the specified ranges
    var isWithinRange =
      windDirection is >= 60f and <= 120f || // Starboard range
      windDirection is >= 240f and <= 310f; // Port range

    if (isWithinRange)
    {
      var forwardVelocity = MovementController.GetForwardVelocity();
      if (forwardVelocity < 0.5f)
        toValue = 0f;
      else
        // Wind affects sail force in this range
        toValue = forwardVelocity *
                  rotationLeanMultiplier;
    }

    // Smoothly interpolate sail lean angle based on wind and direction
    sailLeanAngle = Mathf.Lerp(sailLeanAngle,
      toValue * leanDirection,
      Time.fixedDeltaTime * sailDirectionDamping);

    // Clamp lean angle to configured maximum values
    sailLeanAngle = Mathf.Clamp(sailLeanAngle,
      -PropulsionConfig.EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle.Value,
      PropulsionConfig.EXPERIMENTAL_LeanTowardsWindSailDirectionMaxAngle.Value);

    // Apply the rotation based on the computed sail lean angle
    return baseRotation *
           Quaternion.Euler(0f, 0f, sailLeanAngle);
  }

  public void KinematicSync()
  {
    if (IsInvalid()) return;
    
    if (m_fixedJoint && m_fixedJoint.connectedBody)
      m_fixedJoint.connectedBody = null;

    var rotation = GetRotationWithLean();
    var position = MovementController!.m_body.position;
    m_localRigidbody.Move(
      position,
      rotation
    );
  
  }

  public void JointSync()
  {
    if (m_localRigidbody.isKinematic) m_localRigidbody.isKinematic = false;

    if (MovementController != null &&
        MovementController.m_body.rotation != m_localRigidbody.rotation)
      m_localRigidbody.rotation = MovementController.m_body.rotation;
    
    if (m_fixedJoint.connectedBody == null) LinkFixedJoint();
  }

  private bool IsNotFlying =>
    !VehicleInstance?.MovementController?.IsFlying() ?? false ||
    PropulsionConfig.AllowFlight.Value == false;

  private bool IsPhysicsForceSynced =
    PropulsionConfig.DefaultPhysicsMode.Value ==
    VehiclePhysicsMode.ForceSyncedRigidbody;

  /// <summary>
  /// Client should use the Synced rigidbody by default.
  ///
  /// If pieceSync is enabled it runs only that logic
  ///
  /// Physics.SyncRigidbody is a performant way to sync the position of the pieces with the parent container that is applying physics
  /// Cons
  /// - The client(s) that do not own the boat physics suffer from mild-extreme shaking based on boat speed and turning velocity
  ///
  /// PhysicsMode.DesyncedJointRigidbodyBody -> A high quality way to do physics syncing by using a joint for the pieces container. Downsides are no concav meshes are allowed for physics. Will not work with nautilus (until there are custom colliders created).
  /// Cons
  /// - Clients all using their own calcs can have the pieces container desync from the parent boat sync they do not own the physics.
  ///
  /// HasPieceSyncTarget
  /// - Client will use synced rigibody only if they are owner PhysicsMode.SyncedRigidbody
  /// - Clients who are not the owner use the PhysicsMode.DesyncedJointRigidbodyBody
  /// </summary>
  public void Sync()
  {
    KinematicSync();
  }

  public void ForceEnableGPUInstancing(GameObject prefab)
  {
    var renderer = prefab.GetComponent<MeshRenderer>();
    if (renderer)
    {
      renderer.sharedMaterial.enableInstancing = true;
    }

    var meshRenderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
    if (meshRenderers == null) return;
    foreach (var meshRenderer in meshRenderers)
    {
      meshRenderer.sharedMaterial.enableInstancing = true;
    }
  }

  public bool _isInvalid = true;
  public bool IsInvalid()
  {
    if (isActiveAndEnabled && !_isInvalid)
    {
      return false;
    }

    var isInvalid = m_localRigidbody == null || VehicleInstance?.MovementControllerRigidbody == null ||
                    VehicleInstance?.MovementController == null ||
                    VehicleInstance.NetView == null ||
                    VehicleInstance.Instance == null;
    _isInvalid = isInvalid;

    return _isInvalid;
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    Client_UpdateAllPieces();
  }

  private void UpdateBedPiece(Bed mBedPiece)
  {
    var bedNetView = mBedPiece.m_nview;
    if (bedNetView == null) return;
    if (bedNetView.m_zdo == null) return;
    bedNetView.m_zdo.SetPosition(mBedPiece.m_nview.transform.position);
  }

  /// <summary>
  /// BedPieces are not kept in the raft ball, so that a bed is always placed in the correct area if a player must spawn in it.
  /// </summary>
  public void UpdateBedPieces()
  {
    foreach (var mBedPiece in m_bedPieces) UpdateBedPiece(mBedPiece);
  }

  public override void CustomFixedUpdate(float deltaTime)
  {
    if (VehicleInstance?.NetView == null) return;
    Sync();
  }

  public void CustomLateUpdate(float deltaTime)
  {
    if (ZNet.instance == null || ZNet.instance.IsServer()) return;
    Client_UpdateAllPieces();
    UpdateBedPieces();
  }

  public static bool CanUseActualPiecePosition = true;

  /// <summary>
  /// This should only be called directly in cases of force moving the vehicle with a command
  /// </summary>
  public static void ForceUpdateAllPiecePositions(
    VehiclePiecesController controller, Vector3 position)
  {
    Physics.SyncTransforms();
    var currentPieceControllerSector =
      ZoneSystem.GetZone(position);

    // todo might have to set this to the current position not the sent in position
    controller.VehicleInstance?.NetView?.m_zdo?.SetPosition(position);


    for (var index = 0; index < controller.m_nviewPieces.Count; index++)
    {
      var nv = controller.m_nviewPieces[index];
      if (!nv)
      {
        Logger.LogError(
          $"Error found with m_pieces: netview {nv}, save removing the piece");
        controller.m_nviewPieces.Remove(nv);
        index--;
        continue;
      }

      // todo convert this getComponent to a Dictionary lookup
      var bedComponent = nv.GetComponent<Bed>();
      if (bedComponent)
      {
        controller.UpdateBedPiece(bedComponent);
        continue;
      }

      nv.m_zdo?.SetPosition(CanUseActualPiecePosition ? nv.transform.position : position);
    }
    var convexHullBounds = controller.convexHullComponent.GetConvexHullBounds(false);

    // Removes the temp collider from the parent if not within the parent.
    for (var index = 0; index < controller.m_tempPieces.Count; index++)
    {
      var nv = controller.m_tempPieces[index];
      if (nv == null)
      {
        controller.m_tempPieces.Remove(nv);
        index--;
        continue;
      }
      
      var combinedBounds = GetCombinedColliderBoundsInPiece(nv.gameObject);
      if (!convexHullBounds.Intersects(combinedBounds))
      {
        nv.transform.SetParent(null);
        controller.m_tempPieces.Remove(nv);
        index--;
        continue;
      }

      // should always use actual position as this is a moving object that might have a ZsyncTransform.
      nv.m_zdo?.SetPosition(nv.transform.position);
    }
  }


  /// <summary>
  /// This should only be called directly in cases like teleporting or respawning
  /// </summary>
  public void ForceUpdateAllPiecePositions()
  {
    ForceUpdateAllPiecePositions(this,
      m_localRigidbody.worldCenterOfMass);
  }

  /**
   * @warning this must only be called on the client
   */
  public void Client_UpdateAllPieces()
  {
    var sector = ZoneSystem.GetZone(transform.position);

    if (sector == m_sector)
    {
      if (ValheimRaftPlugin.Instance.ForceShipOwnerUpdatePerFrame.Value)
        ForceUpdateAllPiecePositions();

      return;
    }

    m_sector = sector;
    ForceUpdateAllPiecePositions();
  }

  /// <summary>
  /// Abstract for this getter, coherces to false
  /// </summary>
  /// <returns></returns>
  private bool IsPlayerOwnerOfNetview()
  {
    return VehicleInstance?.NetView?.GetZDO()?.IsOwner() ?? false;
  }

  /// <summary>
  /// Ran locally in singleplayer or on the machine that owns the netview or on the server.
  /// </summary>
  public void ServerSyncAllPieces()
  {
    var isDedicated = ZNet.instance?.IsDedicated();
    if (isDedicated != true) return;

    if (_serverUpdatePiecesCoroutine != null) return;

    _serverUpdatePiecesCoroutine =
      StartCoroutine(UpdatePiecesInEachSectorWorker());
  }


  public void UpdatePieces(List<ZDO> list)
  {
    var pos = transform.position;
    var sector = ZoneSystem.GetZone(pos);

    if (m_serverSector == sector) return;
    if (!sector.Equals(m_sector)) m_sector = sector;

    m_serverSector = sector;

    for (var i = 0; i < list.Count; i++)
    {
      var zdo = list[i];

      // This could also be a problem. If the zdo is created but the ship is in part of another sector it gets cut off.
      if (zdo.GetSector() == sector) continue;

      var id = zdo.GetInt(VehicleZdoVars.MBParentIdHash);
      if (id != VehicleInstance?.PersistentZdoId)
      {
        list.FastRemoveAt(i);
        i--;
        continue;
      }

      // this is experimental might cause problems here, but it aligns with other piece setting values.
      if (CanUseActualPiecePosition)
      {
        var pieceOffset = zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        zdo.SetPosition(pos + pieceOffset);
      }
      else
      {
        zdo.SetPosition(pos);
      }
    }
  }

  private void UpdatePlayers()
  {
    // if (VehicleInstance?.Instance?.m_players == null) return;
    // var vehiclePlayers = VehicleInstance.Instance.m_players;
    // foreach (var vehiclePlayer in vehiclePlayers)
    // {
    //   AddDynamicParentForVehicle(vehiclePlayer.m_nview, this);
    // }
  }

  /**
   * large ships need additional threads to render the ship quickly
   *
   * @todo setPosition should not need to be called unless the item is out of alignment. In theory it should be relative to parent so it never should be out of alignment.
   */
  private IEnumerator UpdatePiecesWorker(List<ZDO> list)
  {
    Logger.LogDebug("called UpdatePiecesWorker");
    UpdatePieces(list);
    yield return new WaitForFixedUpdate();
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
    Logger.LogMessage("UpdatePiecesInEachSectorWorker started");
    while (isActiveAndEnabled)
    {
      if (!VehicleInstance?.NetView)
        yield return new WaitUntil(() => (bool)VehicleInstance.NetView);

      var output =
        m_allPieces.TryGetValue(VehicleInstance.PersistentZdoId, out var list);
      if (list == null || !output)
      {
        yield return new WaitForSeconds(Math.Max(
          ModEnvironment.IsDebug ? 0.05f : 2f,
          ValheimRaftPlugin.Instance.ServerRaftUpdateZoneInterval
            .Value));
        continue;
      }

      yield return UpdatePiecesWorker(list);
      yield return new WaitForFixedUpdate();
    }

    Logger.LogMessage("UpdatePiecesInEachSectorWorker finished");
    // if we get here we need to restart this updater and this requires the coroutine to be null
    _serverUpdatePiecesCoroutine = null;
  }

  /// <summary>
  /// Should be called during physics update
  /// </summary>
  /// <returns></returns>
  internal float GetColliderBottom()
  {
    return OnboardCollider.bounds.min.y;
  }

  // freeze only if instance dne
  public static void AddInactiveTempPiece(ActivationPieceData activationPieceData)
  {
    if (ActiveInstances.TryGetValue(activationPieceData.vehicleId, out var activeInstance))
    {
      if (activeInstance != null && !activeInstance.IsInvalid())
      {
        activeInstance.AddTemporaryPiece(activationPieceData);
      }
      return;
    }

    AddPendingTempPiece(activationPieceData);
  }

  public static void AddInactivePiece(int id, ZNetView netView,
    VehiclePiecesController? instance, bool skipActivation = false)
  {
    if (hasDebug)
      Logger.LogDebug($"addInactivePiece called with {id} for {netView.name}");

    if (instance != null)
    {
      instance.CancelInvoke(nameof(StartActivatePendingPieces));
    }

    if (instance != null && instance.isActiveAndEnabled)
    {
      if (ActiveInstances.TryGetValue(id, out var activeInstance))
      {
        activeInstance.ActivatePiece(netView);
        return;
      }
    }

    AddPendingPiece(id, netView, skipActivation);

    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = false;

    if (!skipActivation && instance != null && instance.isActiveAndEnabled)
      // This will queue up a re-run of ActivatePendingPieces if there are any
      instance.Invoke(nameof(StartActivatePendingPieces), 0.1f);
  }

/*
 * deltaMass can be positive or negative number
 */
  public void UpdateMass(ZNetView netView, bool isRemoving = false)
  {
    if (!netView)
    {
      if (hasDebug) Logger.LogDebug("NetView is invalid skipping mass update");
      return;
    }

    var piece = netView.GetComponent<Piece>();
    if (!piece)
    {
      if (hasDebug)
        Logger.LogDebug(
          "unable to fetch piece data from netViewPiece this could be a raft piece erroring.");
      return;
    }

    var pieceWeight = ComputePieceWeight(piece, isRemoving);

    if (isRemoving)
      ShipMass -= pieceWeight;
    else
      ShipMass += pieceWeight;
  }

  /// <summary>
  /// Small override for addition guards when rebuilding bounds
  /// </summary>
  public override void RequestBoundsRebuild()
  {
    if (!isActiveAndEnabled || ZNetView.m_forceDisableInit || !isInitialPieceActivationComplete) return;
    base.RequestBoundsRebuild();
  }

#if DEBUG
  // todo experiment to send the generated mesh collider across the network bridge so all clients do not have to compute this.
  public void RPC_GenerateMeshCollider()
  {

  }
#endif

  public void RemovePiece(ZNetView netView)
  {
    if (PrefabNames.IsVehicle(netView.name)) return;
    if (!m_nviewPieces.Remove(netView)) return;

    IncrementPieceRevision();
    UpdateMass(netView, true);
    OnPieceRemoved(netView.gameObject);

    if (PrefabNames.IsHull(netView.gameObject)) m_hullPieces.Remove(netView);

    var isRam = RamPrefabs.IsRam(netView.name);
    if (isRam) m_ramPieces.Remove(netView);

    var components = netView.GetComponents<Component>();

    foreach (var component in components)
      switch (component)
      {
        case SailComponent sail:
          m_sailPieces.Remove(sail);
          break;

        case Fireplace fireplace:
          if (netView.m_zdo != null)
            cachedVehicleBurningEffectAreas.Remove(netView.m_zdo.m_uid);

          break;
        case MastComponent mast:
          m_mastPieces.Remove(mast);
          break;

        case RudderComponent rudder:
          m_rudderPieces.Remove(rudder);
          if (VehicleInstance?.Instance && m_rudderPieces.Count > 0)
            SetShipWakeBounds();

          break;

        case SteeringWheelComponent wheel:
          _steeringWheelPieces.Remove(wheel);
          VehicleMovementController.RemoveAllShipControls(VehicleInstance
            ?.MovementController);
          break;

        case Bed bed:
          m_bedPieces.Remove(bed);
          break;

        case VehicleAnchorMechanismController anchorMechanismController:
          m_anchorMechanismComponents.Remove(anchorMechanismController);
          m_anchorPieces.Remove(netView);

          if (MovementController != null)
            MovementController.CanAnchor = m_anchorPieces.Count > 0 || VehicleInstance!.IsLandVehicle;
          break;

        case BoardingRampComponent ramp:
          m_boardingRamps.Remove(ramp);
          break;

        case TeleportWorld portal:
          m_portals.Remove(netView);
          break;

        case RopeLadderComponent ladder:
          m_ladders.Remove(ladder);
          ladder.m_mbroot = null;
          ladder.vehiclePiecesController = null;
          break;
        default:
          break;
      }
  }

/*
 * this function must be used on additional and removal of items to avoid retaining item weight
 */
  private float ComputePieceWeight(Piece piece, bool isRemoving)
  {
    if (!(bool)piece) return 0f;

    var pieceName = piece.name;
    
    var baseMultiplier = 1f;
    /*
     * locally scaled pieces should have a mass multiplier.
     *
     * For now assuming everything is a rectangular prism L*W*H
     */
    if (piece.transform.localScale != new Vector3(1, 1, 1))
    {
      baseMultiplier = piece.transform.localScale.x *
                       piece.transform.localScale.y *
                       piece.transform.localScale.z;
      if (hasDebug)
        Logger.LogDebug(
          $"ValheimRAFT ComputeShipItemWeight() found piece that does not have a 1,1,1 local scale piece: {pieceName} scale: {piece.transform.localScale}, the 3d localScale will be multiplied by the area of this vector instead of 1x1x1");
    }

    // todo figure out hull weight like 20 wood per hull. Also calculate buoyancy from hull wood
    if (pieceName ==
        PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames
          .ShipHullCenterWoodPrefabName))
      return 20f;

    if (pieceName ==
        PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames
          .ShipHullCenterIronPrefabName))
      return 80f;

    if (pieceName.StartsWith(PrefabNames.HullRib))
    {
      if (pieceName.Contains(ShipHulls.HullMaterial.Iron)) return 720f;

      return 180f;
    }

    if (pieceName == "wood_floor_1x1") return 1f * baseMultiplier;

    /*
     * wood_log/wood_core may be split out to a lower ratio
     */
    if (pieceName.Contains("wood")) return MaterialWeight.Wood * baseMultiplier;

    if (pieceName.Contains("stone_"))
      return MaterialWeight.Stone * baseMultiplier;

    if (pieceName.Contains("blackmarble"))
      return MaterialWeight.BlackMarble * baseMultiplier;

    if (pieceName.Contains("blastfurnace") ||
        pieceName.Contains("charcoal_kiln") ||
        pieceName.Contains("forge") || pieceName.Contains("smelter"))
      return 20f * baseMultiplier;

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
      if (PrefabNames.IsVehicle(wnt.name))
        // prevents a loop of DestroyPiece being called from WearNTear_Patch
        return;

      var netview = wnt.GetComponent<ZNetView>();
      RemovePiece(netview);
      UpdatePieceCount();
      cachedTotalSailArea = 0f;
    }


    var pieceCount = GetPieceCount();

    if (pieceCount > 0 || VehicleInstance?.NetView == null) return;
    if (VehicleInstance?.Instance == null) return;

    var wntShip = VehicleInstance.Instance.GetComponent<WearNTear>();
    if ((bool)wntShip) wntShip.Destroy();
  }

  public void RemovePlayersFromBoat()
  {
    var players = Player.GetAllPlayers();
    foreach (var t in players.Where(t =>
               (bool)t && t.transform.parent == transform))
      t.transform.SetParent(null);
  }

  public void DestroyVehicle()
  {
    var wntVehicle = GetComponent<WearNTear>();

    RemovePlayersFromBoat();

    if (!CanDestroyVehicle(VehicleInstance?.NetView)) return;

    if ((bool)wntVehicle)
      wntVehicle.Destroy();
    else if (gameObject) Destroy(gameObject);
  }

  public void AddTempPieceToActiveVehicle(int vehicleId, ZNetView piece,
    bool skipActivation = false)
  {

  }

  public void AddPendingPieceToActiveVehicle(int vehicleId, ZNetView piece,
    bool skipActivation = false)
  {
    _pendingPiecesState = PendingPieceStateEnum.Scheduled;
    if (_pendingPiecesCoroutine != null)
    {
      _newPendingPiecesQueue.Add(piece);
      _pendingPiecesDirty = true;
      return;
    }

    if (!m_pendingPieces.TryGetValue(vehicleId, out var list))
    {
      list = [];
      m_pendingPieces.Add(vehicleId, list);
    }

    list.Add(piece);
    _pendingPiecesDirty = true;


    if (!skipActivation && isActiveAndEnabled && piece != null &&
        piece.isActiveAndEnabled)
      // delegates to coroutine activation logic
      StartActivatePendingPieces();
  }

  private static void AddPendingTempPiece(ActivationPieceData activationPieceData)
  {
    ActiveInstances.TryGetValue(activationPieceData.vehicleId, out var activeVehicleInstance);

    if (!m_pendingTempPieces.ContainsKey(activationPieceData.vehicleId))
      m_pendingTempPieces.Add(activationPieceData.vehicleId, []);

    if (activeVehicleInstance == null)
    {
      m_pendingTempPieces[activationPieceData.vehicleId].Add(activationPieceData);
      return;
    }

    activeVehicleInstance.AddTemporaryPiece(activationPieceData);
  }

  private static void AddPendingPiece(int vehicleId, ZNetView piece, bool skipActivation = false)
  {
    ActiveInstances.TryGetValue(vehicleId, out var vehicleInstance);

    if (!m_pendingPieces.ContainsKey(vehicleId))
      m_pendingPieces.Add(vehicleId, []);

    if (vehicleInstance == null)
    {
      m_pendingPieces[vehicleId].Add(piece);
      return;
    }

    vehicleInstance.AddPendingPieceToActiveVehicle(vehicleId, piece, skipActivation);
  }

  public static void ActivateAllPendingPieces()
  {
    foreach (var pieceController in ActiveInstances)
      pieceController.Value.StartActivatePendingPieces();
  }

  internal static List<ZNetView>? GetShipActiveInstances(int? persistentId)
  {
    if (persistentId == null) return null;
    var hasSucceeded =
      m_pendingPieces.TryGetValue(persistentId.Value, out var list);

    return !hasSucceeded ? null : list;
  }

  public bool CanActivatePendingPieces => _pendingPiecesCoroutine == null;

  /// <summary>
  /// Main method for starting the pending piece activation.
  /// - It will only allow 1 instance of itself to run
  /// - It will only run on Initialization state complete
  /// - It will only run if there are valid pieces to activate
  /// </summary>
  public void StartActivatePendingPieces()
  {
    // For debugging activation of raft
#if DEBUG
    if (!DEBUGAllowActivatePendingPieces) return;
#endif
    if (hasDebug)
      Logger.LogDebug(
        $"ActivatePendingPiecesCoroutine(): pendingPieces count: {m_pendingPieces.Count}");
    if (!CanActivatePendingPieces) return;

    if (VehicleInstance == null) return;

    // do not run if in a Pending or Created state or if no pending pieces
    if (BaseVehicleInitState != InitializationState.Complete &&
        GetCurrentPendingPieces()?.Count == 0) return;

    if (_pendingPiecesCoroutine == null)
    {
      _pendingPiecesCoroutine =
        StartCoroutine(nameof(ActivatePendingPiecesCoroutine));
    }
  }

  public void OnActivatePendingPiecesComplete(
    PendingPieceStateEnum pieceStateEnum,
    string message = "")
  {
    _pendingPiecesCoroutine = null;
    _pendingPiecesState = pieceStateEnum;
    PendingPiecesTimer.Reset();

    if (pieceStateEnum == PendingPieceStateEnum.Complete)
    {
      if (!isInitialPieceActivationComplete)
      {
        isInitialPieceActivationComplete = true;
        // as a safety measure calling this will prevent collisions if any piece was delayed in activation.
        ForceRebuildBounds();
      }
      else
      {
        // Should be called after activation completes provided there is no reset
        RequestBoundsRebuild();
      }
    }

    if (pieceStateEnum == PendingPieceStateEnum.ForceReset)
      InitializationTimer.Reset();
    else
      InitializationTimer.Stop();


    if (pieceStateEnum == PendingPieceStateEnum.Failure)
      Logger.LogWarning(
        $"ActivatePendingPieces did not complete correctly. Reason: {message}");
  }

  public void OnStartActivatePendingPieces()
  {
    _pendingPiecesState = PendingPieceStateEnum.Running;
    PendingPiecesTimer.Restart();
  }

  /// <summary>
  /// This may be optional now.
  /// </summary>
  /// <returns></returns>
  public IEnumerator ActivateDynamicPendingPieces()
  {
    m_dynamicObjects.TryGetValue(VehicleInstance?.PersistentZdoId ?? 0,
      out var objectList);
    var objectListHasNoValidItems = true;
    if (objectList is { Count: > 0 })
    {
      if (hasDebug)
        Logger.LogDebug($"m_dynamicObjects is valid: {objectList.Count}");

      foreach (var t in objectList)
      {
        var go = ZNetScene.instance.FindInstance(t);
        if (!go) continue;

        var nv = go.GetComponentInParent<ZNetView>();
        if (!nv || nv.m_zdo == null)
          continue;
        else
          objectListHasNoValidItems = false;

        if (ZDOExtraData.s_vec3.TryGetValue(nv.m_zdo.m_uid, out var dic))
        {
          if (dic.TryGetValue(VehicleZdoVars.MBPositionHash,
                out var offset))
            nv.transform.position = offset + transform.position;

          offset = default;
        }

        ZDOExtraData.RemoveInt(nv.m_zdo.m_uid,
          VehicleZdoVars.TempPieceParentId);
        ZDOExtraData.RemoveVec3(nv.m_zdo.m_uid,
          VehicleZdoVars.MBPositionHash);
        dic = null;
      }

      if (VehicleInstance != null)
        m_dynamicObjects.Remove(VehicleInstance.PersistentZdoId);

      yield return null;
    }
  }

  public IEnumerator ActivatePendingPiecesCoroutine()
  {
    if (_baseVehicleInitializationState !=
        InitializationState.Complete)
    {
      _pendingPiecesCoroutine = null;
      yield break;
    }

    OnStartActivatePendingPieces();

    var persistentZdoId = VehicleInstance?.PersistentZdoId;
    if (!persistentZdoId.HasValue)
    {
      OnActivatePendingPiecesComplete(PendingPieceStateEnum.Failure,
        "No persistentID found on Vehicle instance");
      yield break;
    }

    var currentPieces = GetShipActiveInstances(persistentZdoId);

    if (currentPieces == null || currentPieces.Count == 0)
    {
      OnActivatePendingPiecesComplete(PendingPieceStateEnum.Complete);
      yield break;
    }

    // Does not care about conditionals are first run
    do
    {
      if (ZNetScene.instance.InLoadingScreen())
        yield return new WaitForFixedUpdate();

      if (VehicleInstance?.Instance?.NetView == null)
      {
        // NetView somehow unmounted;
        OnActivatePendingPiecesComplete(PendingPieceStateEnum.ForceReset);
        _pendingPiecesCoroutine = null;
        yield break;
      }

      _pendingPiecesDirty = false;

      if (currentPieces == null && _newPendingPiecesQueue.Count == 0) continue;
      // Process each pending piece, yielding periodically to avoid frame spikes
      foreach (var piece in currentPieces.ToList())
        // Activate each piece (e.g., instantiate or enable)
        ActivatePiece(piece);

      // Clear processed items and add any newly queued items
      currentPieces?.Clear();
      if (_newPendingPiecesQueue.Count <= 0) continue;
      currentPieces ??= [];
      currentPieces.AddRange(_newPendingPiecesQueue);
      _newPendingPiecesQueue.Clear();
      _pendingPiecesDirty = true; // Mark dirty to re-run coroutine
    } while
      (_pendingPiecesDirty); // Loop if new items were added during this run


    // todo this might be functional but it is older logic. See if we need it for anything.
    // yield return ActivateDynamicPendingPieces();

    ActivateTempPieces();

    OnActivatePendingPiecesComplete(PendingPieceStateEnum.Complete);
  }

  /// <summary>
  /// A bit heavy for iteration, likely better than raycast logic, allows for accurately detecting if in vehicle area. But it could be inaccurate since the point is not technically a part of the pieces list.
  /// </summary>
  /// - Used for fires and other Effects Area logic which requires movement support for static struct of bounds.
  /// <param name="p"></param>
  /// <returns></returns>
  public static bool IsPointWithin(Vector3 p,
    out VehiclePiecesController? controller)
  {
    controller = null;
    foreach (var instance in ActiveInstances.Values)
      if (instance.OnboardCollider.bounds.Contains(p))
      {
        controller = instance;
        return true;
      }

    return false;
  }

  /// <summary>
  /// Way to check if within PieceController
  /// </summary>
  /// <summary>May be cleaner than an out var approach. This is experimental syntax. Does not match other patterns yet. </summary>
  /// <param name="objTransform"></param>
  /// <param name="controller"></param>
  /// <returns></returns>
  public static bool
    IsWithin(Transform objTransform, out VehiclePiecesController controller)
  {
    controller = objTransform.transform.root
      .GetComponent<VehiclePiecesController>();
    return controller != null;
  }

  /// <summary>
  /// Makes the player no longer attached to the vehicleship
  /// </summary>
  /// <param name="source"></param>
  /// <param name="bvc"></param>
  public static void RemoveDynamicParentForVehicle(ZNetView source)
  {
    if (!source.isActiveAndEnabled)
    {
      Logger.LogDebug("Player source Not active");
      return;
    }

    source.m_zdo.RemoveInt(VehicleZdoVars.TempPieceParentId);
    source.m_zdo.RemoveVec3(VehicleZdoVars.MBPositionHash);
  }

  /// <summary>
  /// Makes the Character/Player/Vehicle/Cart associated with the parent vehicle ship, if they spawn near the active vehicle they are move there during initial activation spot.
  /// </summary>
  /// <param name="netView"></param>
  /// <param name="vehiclePiecesController"></param>
  /// <returns></returns>
  public static bool AddTempPieceProperties(ZNetView netView,
    VehiclePiecesController vehiclePiecesController)
  {
    if (netView == null) return false;
    if (!netView.isActiveAndEnabled)
    {
      Logger.LogDebug("Player source Not active");
      return false;
    }

    netView.m_zdo.Set(VehicleZdoVars.TempPieceParentId, vehiclePiecesController.PersistentZdoId);
    netView.m_zdo.Set(VehicleZdoVars.MBPositionHash,
      netView.transform.position - vehiclePiecesController.m_localRigidbody.worldCenterOfMass);
    return true;
  }

  public static bool AddTempParent(ZNetView netView, GameObject target)
  {
    var bvc = target.GetComponentInParent<VehiclePiecesController>();
    if (bvc == null) return false;
    return AddTempPieceProperties(netView, bvc);
  }

  public static int GetParentVehicleId(ZNetView netView)
  {
    if (!netView) return 0;

    return netView.GetZDO()?.GetInt(VehicleZdoVars.TempPieceParentId, 0) ??
           0;
  }

  public static Vector3 GetDynamicParentOffset(ZNetView netView)
  {
    if (!netView) return Vector3.zero;

    return netView.m_zdo?.GetVec3(VehicleZdoVars.MBPositionHash,
             Vector3.zero) ??
           Vector3.zero;
  }

  public void InitAnchorComponent(
    VehicleAnchorMechanismController anchorComponent)
  {
    if (anchorComponent.MovementController == null &&
        MovementController != null)
      anchorComponent.MovementController = MovementController;
  }


  private AnchorState lastAnchorState = AnchorState.Idle;
  /// <summary>
  /// Binds the Movement to the anchor components and allows for the anchor hotkeys to toggle all anchors on the ship
  /// </summary>
  /// <param name="anchorState"></param>
  public void UpdateAnchorState(AnchorState anchorState)
  {
    if (lastAnchorState == anchorState)
    {
      return;
    }

    var isLandVehicle = MovementController != null && MovementController.VehicleInstance is
    {
      IsLandVehicle: true
    };

    lastAnchorState = anchorState;

    var currentWheelStateText = VehicleAnchorMechanismController.GetCurrentStateTextStatic(anchorState, isLandVehicle);
    foreach (var anchorComponent in m_anchorMechanismComponents)
      if (anchorState != anchorComponent.currentState)
        anchorComponent.UpdateAnchorState(anchorState, currentWheelStateText);
    foreach (var steeringWheel in _steeringWheelPieces)
    {
      steeringWheel.UpdateSteeringHoverMessage(anchorState, currentWheelStateText);
    }
  }

  /**
   * A cached getter for sail size. Cache invalidates when a piece is added or removed
   *
   * This method calls so frequently outside of the scope of ValheimRaftPlugin.Instance so the Config values cannot be fetched for some reason.
   */
  public float GetTotalSailArea(bool forceUpdate = false)
  {
    if (!forceUpdate)
    {
      if (cachedTotalSailArea > -1 ||
          m_mastPieces.Count == 0 && m_sailPieces.Count == 0)
      return cachedTotalSailArea;
    }

    cachedTotalSailArea = 0;
    customSailsArea = 0;
    numberOfTier1Sails = 0;
    numberOfTier2Sails = 0;
    numberOfTier3Sails = 0;
    numberOfTier4Sails = 0;

    var hasConfigOverride =
      PropulsionConfig.EnableCustomPropulsionConfig.Value;

    foreach (var mMastPiece in m_mastPieces.ToList())
    {
      // prevent NRE from occuring if destroying the mastPiece but it still exists
      if (!mMastPiece)
      {
        m_mastPieces.Remove(mMastPiece);
        continue;
      }

      if (mMastPiece.name.StartsWith(PrefabNames.Tier1RaftMastName))
      {
        ++numberOfTier1Sails;
        var multiplier = hasConfigOverride
          ? PropulsionConfig.SailTier1Area.Value
          : Tier1;
        cachedTotalSailArea += numberOfTier1Sails * multiplier;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier2RaftMastName))
      {
        ++numberOfTier2Sails;
        var multiplier = hasConfigOverride
          ? PropulsionConfig.SailTier2Area.Value
          : Tier2;
        cachedTotalSailArea += numberOfTier2Sails * multiplier;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier3RaftMastName))
      {
        ++numberOfTier3Sails;
        var multiplier = hasConfigOverride
          ? PropulsionConfig.SailTier3Area.Value
          : Tier3;
        cachedTotalSailArea += numberOfTier3Sails * multiplier;
        ;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier4RaftMastName))
      {
        ++numberOfTier4Sails;
        var multiplier = hasConfigOverride
          ? PropulsionConfig.SailTier4Area.Value
          : Tier4;
        cachedTotalSailArea += numberOfTier4Sails * multiplier;
        ;
      }
    }

    var sailComponents = GetComponentsInChildren<SailComponent>();
    if (sailComponents.Length != 0)
    {
      foreach (var sailComponent in sailComponents)
        if ((bool)sailComponent)
          customSailsArea += sailComponent.GetSailArea();

      if (hasDebug) Logger.LogDebug($"CustomSailsArea {customSailsArea}");
      var multiplier = hasConfigOverride
        ? PropulsionConfig.SailCustomAreaTier1Multiplier.Value
        : CustomTier1AreaForceMultiplier;

      cachedTotalSailArea +=
        customSailsArea * Mathf.Max(0.1f,
          multiplier);
    }

    return cachedTotalSailArea;
  }

  public float GetSailingForce()
  {
    if (cachedSailForce >= 0f)
    {
      return cachedSailForce;
    }

    var area = Mathf.Max(GetTotalSailArea(), 0f);
    var mpFactor = Mathf.Clamp01(PropulsionConfig.SailingMassPercentageFactor.Value);
    var speedCapMultiplier =
      PropulsionConfig.SpeedCapMultiplier.Value;
    var surfaceArea = speedCapMultiplier * area;
    var maxSpeed = Mathf.Min(PhysicsConfig.MaxLinearVelocity.Value, PropulsionConfig.MaxSailSpeed.Value);
    var massToPush = Mathf.Max(1f, TotalMass * mpFactor);
    var lerpedSailForce = Mathf.Lerp(0f, maxSpeed, Mathf.Clamp01(surfaceArea / massToPush));
    return lerpedSailForce;
  }

  public static void InitZdo(ZDO zdo)
  {
    if (zdo.m_prefab ==
        PrefabNames.WaterVehicleShip.GetStableHashCode()) return;

    var id = GetParentID(zdo);
    if (id != 0)
    {
      if (!m_allPieces.TryGetValue(id, out var list))
      {
        list = [];
        m_allPieces.Add(id, list);
      }

      // important for preventing a list error if the zdo has already been added
      if (list.Contains(zdo)) return;

      list.Add(zdo);
    }

    var cid = zdo.GetInt(VehicleZdoVars.TempPieceParentId);
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
    if (zdo.m_prefab ==
        PrefabNames.WaterVehicleShip.GetStableHashCode() || zdo.m_prefab == PrefabNames.LandVehicle.GetStableHashCode()) return;

    var id = GetParentID(zdo);
    if (id == 0 || !m_allPieces.TryGetValue(id, out var list)) return;
    list.FastRemove(zdo);
    itemsRemovedDuringWait = true;
  }

  private static int GetParentID(ZDO zdo)
  {
    var id = zdo.GetInt(VehicleZdoVars.MBParentIdHash);
    if (id == 0)
    {
      var zdoid = zdo.GetZDOID(VehicleZdoVars.MBParentHash);
      if (zdoid != ZDOID.None)
      {
        var zdoparent = ZDOMan.instance.GetZDO(zdoid);
        id = zdoparent == null
          ? ZdoWatchController.ZdoIdToId(zdoid)
          : ZdoWatchController.Instance.GetOrCreatePersistentID(zdoparent);
        zdo.Set(VehicleZdoVars.MBParentIdHash, id);
        zdo.Set(VehicleZdoVars.MBRotationVecHash,
          zdo.GetQuaternion(VehicleZdoVars.MBRotationHash, Quaternion.identity)
            .eulerAngles);
        zdo.RemoveZDOID(VehicleZdoVars.MBParentHash);
        ZDOExtraData.s_quats.Remove(zdoid, VehicleZdoVars.MBRotationHash);
      }
    }

    return id;
  }

  public static bool IsExcludedPrefab(GameObject netView)
  {
    if (PrefabNames.IsVehicle(netView.name) ||
        netView.name.StartsWith(PrefabNames.VehiclePiecesContainer))
      return true;

    return false;
  }

  private static bool TryInitTempPiece(ZNetView netView)
  {
    if (netView != null) return false;
    var zdo = netView.GetZDO();
    if (zdo == null) return false;

    var tempPieceParentId = zdo.GetInt(VehicleZdoVars.TempPieceParentId);
    if (tempPieceParentId == 0)
    {
      return false;
    }
    var tempPieceLocalPosition = zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    if (tempPieceLocalPosition == Vector3.zero)
    {
      LoggerProvider.LogDebug($"Possible issue with temp piece {netView.name} It has no local position. Meaning it likely was not saved correctly.");
    }

    var tempPiece = new ActivationPieceData(netView, tempPieceParentId, tempPieceLocalPosition);
    AddInactiveTempPiece(tempPiece);

    return true;
  }

  /// <summary>
  /// A Method meant to be called in ActivatePendingPieceCoroutine. This must always be run after all pending ship pieces are activated in order to allow the vehicle to align properly.
  /// </summary>
  public void ActivateTempPieces()
  {
    if (_vehicle == null) return;
    if (!m_pendingTempPieces.TryGetValue(_vehicle.PersistentZdoId, out var pendingTempPiecesList))
    {
      LoggerProvider.LogDebug($"No temp pieces found for vehicle {_vehicle.PersistentZdoId}");
      return;
    }

    foreach (var activationPieceData in pendingTempPiecesList)
    {
      if (activationPieceData.netView == null) continue;
      if (activationPieceData.vehicleId != _vehicle.PersistentZdoId)
      {
        LoggerProvider.LogError($"VehicleId of temp piece {activationPieceData.gameObject.name} vehicleId: {activationPieceData.vehicleId} is not equal to currently activating vehicle {_vehicle.PersistentZdoId}");
        continue;
      }
      if (hasDebug)
      {
        LoggerProvider.LogDebug($"VehicleId of temp piece {activationPieceData.gameObject.name} activated");
      }
      AddTemporaryPiece(activationPieceData);
    }

    pendingTempPiecesList.Clear();
  }

  public static void InitPiece(ZNetView netView)
  {
    if (!netView) return;
    if (TryInitTempPiece(netView)) return;
    
    var isPiecesOrWaterVehicle = IsExcludedPrefab(netView.gameObject);
    
    if (isPiecesOrWaterVehicle) return;

    var rb = netView.GetComponentInChildren<Rigidbody>();
    if ((bool)rb && !rb.isKinematic && !RamPrefabs.IsRam(netView.name)) return;

    var zdo = netView.GetZDO();
    if (zdo == null) return;

    var id = GetParentID(zdo);
    if (id == 0) return;

    var parentObj = ZdoWatchController.Instance.GetGameObject(id);
    if (parentObj != null)
    {
      var vehicleShip = parentObj.GetComponent<VehicleShip>();
      if (vehicleShip != null && vehicleShip.PiecesController != null)
      {
        vehicleShip.PiecesController.ActivatePiece(netView);
        return;
      }
    }

    AddInactivePiece(id, netView, null);
  }

  public void ActivatePiece(ZNetView netView)
  {
    if (!(bool)netView) return;
    if (netView.m_zdo == null) return;

    SetPieceToParent(netView.transform);
    netView.transform.localPosition =
      netView.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
    netView.transform.localRotation =
      Quaternion.Euler(netView.m_zdo.GetVec3(VehicleZdoVars.MBRotationVecHash,
        Vector3.zero));

    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = true;

    AddPiece(netView);
  }


  /// <summary>
  /// For Carts and other rigidbody moveable objects.
  /// </summary>
  public void AddTemporaryPiece(ZNetView netView, bool shouldSkipIgnoreColliders = false)
  {
    if (netView == null) return;

    var zdo = netView.GetZDO();
    if (zdo == null) return;

    AddTempPieceProperties(netView, this);
    SetPieceToParent(netView.transform);

    // Likely do not want to call this.
    FixPieceMeshes(netView);

    if (!shouldSkipIgnoreColliders)
    {
      OnAddPieceIgnoreColliders(netView);
    }

    // in case of spam or other in progress apis add a guard to prevent duplication.
    if (!m_tempPieces.Contains(netView))
    {
      m_tempPieces.Add(netView);
    }
  }

  /// <summary>
  /// For Carts and other rigidbody moveable objects.
  /// </summary>
  public void AddTemporaryPiece(ActivationPieceData activationPieceData)
  {
    var netView = activationPieceData.netView;
    if (netView == null) return;
    
    var zdo = netView.GetZDO();
    if (zdo == null) return;

    AddTempPieceProperties(netView, this);
    SetPieceToParent(netView.transform);

    // Likely do not want to call this.
    // FixPieceMeshes(netView);
    
    OnAddPieceIgnoreColliders(netView);

    if (!isInitialPieceActivationComplete)
    {
      activationPieceData.FreezeRigidbodies();
    }
    else
    {
      activationPieceData.UnFreezeRigidbodies();
    }
    

    // in case of spam or other in progress apis add a guard to prevent duplication.
    if (!m_tempPieces.Contains(netView))
    {
      m_tempPieces.Add(netView);
    }
  }

  public void RemoveTempPiece(ZNetView netView)
  {
    RemoveDynamicParentForVehicle(netView);
    netView.transform.SetParent(null);
    m_tempPieces.Remove(netView);
  }

  /// <summary>
  /// Adds the piece depending on type to the correct container
  /// - Rams are rigidbodies and thus must not be controlled by a RigidBody
  /// </summary>
  /// <param name="targetTransform"></param>
  public void SetPieceToParent(Transform targetTransform)
  {
    if (RamPrefabs.IsRam(targetTransform.name))
    {
      if (VehicleInstance != null && VehicleInstance.Instance != null)
      {
        targetTransform.SetParent(VehicleInstance.Instance.transform);
      }
      return;
    }

    targetTransform.SetParent(_piecesContainer);
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
  public static bool CanDestroyVehicle(ZNetView? netView)
  {
    if (!netView || netView == null) return false;
    var vehicleShip = netView.GetComponent<VehicleShip>();
    var hasPendingPieces =
      m_pendingPieces.TryGetValue(vehicleShip.PersistentZdoId,
        out var pendingPieces);
    var hasPieces = vehicleShip.PiecesController.GetPieceCount() != 0;

    // if there are pending pieces, do not let vehicle be destroyed
    if (pendingPieces != null && hasPendingPieces && pendingPieces.Count > 0)
      return false;

    return !hasPieces;
  }

  public static Dictionary<Material, Material> FixMaterialUniqueInstances = new();
  /// <summary>
  /// Must return a new material
  /// </summary>
  /// <param name="material"></param>
  /// <returns></returns>
  public static Material FixMaterial(Material material)
  {
    if (!material) return null;

    // Check if material has any of the target properties
    if (!material.HasFloat(RippleDistance) && !material.HasFloat(ValueNoise) && !material.HasFloat(TriplanarLocalPos))
    {
      return material; // No need to fix
    }

    // If already fixed, return the cached instance
    if (FixMaterialUniqueInstances.TryGetValue(material, out var fixedMaterialInstance))
    {
      return fixedMaterialInstance;
    }

    // 🔹 Fix: Create a NEW material BEFORE modifying it
    var newMaterial = new Material(material);

    if (material.name.Contains("blackmarble"))
    {
      newMaterial.SetFloat(TriplanarLocalPos, 1f);
    }

    newMaterial.SetFloat(RippleDistance, 0f);
    newMaterial.SetFloat(ValueNoise, 0f);

    // Cache the fixed material
    FixMaterialUniqueInstances[material] = newMaterial;

    return newMaterial;
  }
  // public static Material FixMaterial(Material material)
  // {
  //   var isBlackMarble = material.name.Contains("blackmarble");
  //   if (!material.HasFloat(RippleDistance) && !material.HasFloat(ValueNoise) && !material.HasFloat(TriplanarLocalPos)) return material;
  //   if (!FixMaterialUniqueInstances.TryGetValue(material, out var fixedMaterialInstance))
  //   {
  //     if (isBlackMarble) material.SetFloat(TriplanarLocalPos, 1f);
  //     material.SetFloat(RippleDistance, 0f);
  //     material.SetFloat(ValueNoise, 0f);
  //     var newMaterial = new Material(material);
  //     FixMaterialUniqueInstances.Add(material, newMaterial);
  //     fixedMaterialInstance = newMaterial;
  //   }
  //   return fixedMaterialInstance;
  // }
  
  public static void FixPieceMeshes(ZNetView netView)
  {
    /*
     * It fixes shadow flicker on all of valheim's prefabs with boats
     * If this is removed, the raft is seizure inducing.
     */
    var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
    foreach (var meshRenderer in meshes)
    {
      // foreach (var meshRendererMaterial in meshRenderer.materials)
      //   FixMaterial(meshRendererMaterial);

      if (meshRenderer.sharedMaterials.Length > 0)
      {
        var sharedMaterials = meshRenderer.sharedMaterials;
        for (var j = 0; j < sharedMaterials.Length; j++)
          sharedMaterials[j] = FixMaterial(sharedMaterials[j]);

        meshRenderer.sharedMaterials = sharedMaterials;
      }
      else if (meshRenderer.materials.Length > 0)
      {
        var materials = meshRenderer.materials;

        for (var j = 0; j < materials.Length; j++)
          materials[j] = FixMaterial(materials[j]);
        meshRenderer.materials = materials;
      }
    }
  }

  // public static void FixPieceMeshes(ZNetView netView)
  // {
  //   /*
  //    * It fixes shadow flicker on all of valheim's prefabs with boats
  //    * If this is removed, the raft is seizure inducing.
  //    */
  //   var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
  //   foreach (var meshRenderer in meshes)
  //   {
  //     // for (var index = 0; index < meshRenderer.sharedMaterials.Length; index++)
  //     // {
  //     //   var meshRendererMaterial = meshRenderer.sharedMaterials[index];
  //     //   meshRenderer.materials[index] = FixMaterial(meshRendererMaterial);
  //     // }
  //     // for (var index = 0; index < meshRenderer.materials.Length; index++)
  //     // {
  //     //   var meshRendererMaterial = meshRenderer.materials[index];
  //     //   meshRenderer.materials[index] = FixMaterial(meshRendererMaterial);
  //     // }
  //     //
  //     if (meshRenderer.sharedMaterial)
  //     {
  //       // todo disable triplanar shader which causes shader to move on black marble
  //       var sharedMaterials = meshRenderer.sharedMaterials;
  //     
  //       for (var j = 0; j < sharedMaterials.Length; j++)
  //         sharedMaterials[j] = FixMaterial(sharedMaterials[j]);
  //     
  //       meshRenderer.sharedMaterials = sharedMaterials;
  //     }
  //   }
  // }

  public void AddNewPiece(ZNetView netView)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist");
      return;
    }

    if (m_nviewPieces.Contains(netView))
    {
      Logger.LogWarning($"NetView already is added. name: {netView.name}");
      return;
    }

    var previousCount = GetPieceCount();

    SetPieceToParent(netView.transform);

    if (netView.m_zdo != null)
    {
      if (VehicleInstance?.PersistentZdoId != null)
        netView.m_zdo.Set(VehicleZdoVars.MBParentIdHash,
          VehicleInstance.PersistentZdoId);
      else
        // We should not reach this, but this would be a critical issue and should be tracked.
        Logger.LogError(
          "Potential update error detected: Ship parent ZDO is invalid but added a Piece to the ship");

      netView.m_zdo.Set(VehicleZdoVars.MBRotationVecHash,
        netView.transform.localRotation.eulerAngles);
      netView.m_zdo.Set(VehicleZdoVars.MBPositionHash,
        netView.transform.localPosition);
    }

    if (netView.GetZDO() == null)
    {
      Logger.LogError("NetView has no valid ZDO returning");
      return;
    }

    AddPiece(netView, true);
    InitZdo(netView.GetZDO());

    if (previousCount == 0 && GetPieceCount() == 1) SetInitComplete();
  }

// must call wnt destroy otherwise the piece is removed but not destroyed like a player destroying an item.
// must create a new array to prevent a collection modify error
  public void OnAddSteeringWheelDestroyPrevious(ZNetView netView,
    SteeringWheelComponent steeringWheelComponent)
  {
    var wheelPieces = _steeringWheelPieces;
    if (wheelPieces.Count <= 0) return;

    foreach (var wheelPiece in wheelPieces.ToList())
    {
      if (wheelPiece == null) return;
      var wnt = wheelPiece.GetComponent<WearNTear>();
      if (wnt == null) return;
      wnt.Destroy();
    }

    RotateVehicleForwardPosition();
  }

  /// <summary>
  /// Likely not needed
  /// </summary>
  /// <deprecated>Use fireplace component check</deprecated>
  /// <param name="obj"></param>
  /// <returns></returns>
  public bool IsBurningEffectAreaComponent(ZNetView obj)
  {
    return obj.name == "fire_pit(Clone)" ||
           obj.name == "hearth_clone";
  }

  /// <summary>
  /// We can actually assume all "fireplace" components will have the EffectArea nested in them. So doing a query on them is actually pretty efficient. Name checks are more prone to breaks or mod incompatibility so skipping this.
  /// </summary>
  /// <param name="netView"></param>
  public void AddFireEffectAreaComponent(ZNetView netView)
  {
    var effectAreaItems = netView.GetComponentsInChildren<EffectArea>();
    foreach (var effectAreaItem in effectAreaItems)
    {
      if (effectAreaItem.m_type != EffectArea.Type.Burning) continue;
      cachedVehicleBurningEffectAreas.Add(netView.m_zdo.m_uid,
        effectAreaItem);
      break;
    }

  }

  /// <summary>
  /// The main event for adding collider ignores when pieces are added.
  /// </summary>
  public void OnAddPieceIgnoreColliders(ZNetView netView)
  {
    IgnoreAllVehicleCollidersForGameObjectChildren(netView.gameObject);
    // OnPieceAddedIgnoreAllColliders(netView.gameObject);


    // todo the code below is inefficient. Need to abstract this all to another component that is meant for hashing gameobjects with colliders and iterating through their slices in batches.

    // var nvName = netView.name;
    // var nvColliders = netView.GetComponentsInChildren<Collider>(true).ToList();
    // convexHullTriggerColliders = MovementController.DamageColliders
    //   .GetComponents<Collider>().ToList();
    //
    // if (nvColliders.Count == 0) return;


    // main ship colliders like the generated meshes and onboard collider
    // IgnoreShipColliders(nvColliders);
    // IgnoreWheelColliders(nvColliders);

    // all pieces
    // if (RamPrefabs.IsRam(nvName) || nvName.Contains(PrefabNames.ShipAnchorWood))
    //   IgnoreCollidersForList(nvColliders, m_nviewPieces);

    // rams must always have new pieces added to their list ignored. So that the new piece does not hit the ram.
    // IgnoreCollidersForRamPieces(netView);
    // IgnoreCollidersForAnchorPieces(netView);
  }

  public void IncrementPieceRevision()
  {
    _lastPieceRevision += 1;
  }

  public void AddPiece(ZNetView netView, bool isNew = false)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist but somehow called AddPiece()");
      return;
    }
    // incrementRevision
    IncrementPieceRevision();    
    FixPieceMeshes(netView);
    ResetSailCachedValues();
    OnPieceAdded(netView.gameObject);


    // todo onPieceAdded SHOULD DO this.
    OnAddPieceIgnoreColliders(netView);
    
    m_nviewPieces.Add(netView);
    UpdatePieceCount();

    // Cache components
    var components = netView.GetComponents<Component>();

    foreach (var component in components)
      switch (component)
      {
        case WearNTear wnt
          when ValheimRaftPlugin.Instance.MakeAllPiecesWaterProof.Value:
          wnt.m_noRoofWear = false;
          break;
        case Fireplace fireplace:
          AddFireEffectAreaComponent(netView);
          break;
        case CultivatableComponent cultivatable:
          cultivatable.UpdateMaterial();
          break;

        case MastComponent mast:
          m_mastPieces.Add(mast);
          break;

        case SailComponent sail:
          m_sailPieces.Add(sail);
          break;

        case Bed bed:
          m_bedPieces.Add(bed);
          break;

        case BoardingRampComponent ramp:
          ramp.ForceRampUpdate();
          m_boardingRamps.Add(ramp);
          break;

        case VehicleAnchorMechanismController anchorMechanismController:
          m_anchorMechanismComponents.Add(anchorMechanismController);
          InitAnchorComponent(anchorMechanismController);
          m_anchorPieces.Add(netView);
          if (MovementController != null)
          {
            MovementController.CanAnchor = m_anchorPieces.Count > 0 || VehicleInstance!.IsLandVehicle;
            anchorMechanismController.UpdateAnchorState(MovementController
              .vehicleAnchorState, VehicleAnchorMechanismController.GetCurrentStateTextStatic(MovementController.vehicleAnchorState, VehicleInstance != null && VehicleInstance.IsLandVehicle));
          }

          break;

        case RudderComponent rudder:
          m_rudderPieces.Add(rudder);
          SetShipWakeBounds();
          break;

        case SteeringWheelComponent wheel:
          OnAddSteeringWheelDestroyPrevious(netView, wheel);
          _steeringWheelPieces.Add(wheel);
          wheel.InitializeControls(netView, VehicleInstance);
          break;

        case TeleportWorld portal:
          m_portals.Add(netView);
          break;

        case RopeLadderComponent ladder:
          m_ladders.Add(ladder);
          ladder.vehiclePiecesController = this;
          break;
      }

    if (RamPrefabs.IsRam(netView.name))
    {
      m_ramPieces.Add(netView);
      var vehicleRamAoe = netView.GetComponentInChildren<VehicleRamAoe>();
      if ((bool)vehicleRamAoe)
        vehicleRamAoe.m_vehicle = VehicleInstance.Instance;
    }

    // Remove non-kinematic rigidbodies if not a ram
    if (!RamPrefabs.IsRam(netView.name) &&
        !netView.name.Contains(PrefabNames.ShipAnchorWood))
    {
      var rbs = netView.GetComponentsInChildren<Rigidbody>();
      foreach (var rbsItem in rbs)
        if (!rbsItem.isKinematic)
        {
          Logger.LogWarning(
            $"Destroying Rigidbody for root object {rbsItem.transform.root?.name ?? rbsItem.transform.name}");
          Destroy(rbsItem);
        }
    }

    UpdateMass(netView);

    // Handle bounds rebuilding
    RequestBoundsRebuild();

    if (hasDebug)
      Logger.LogDebug(
        $"After Adding Piece: {netView.name}, Ship Size calc is: m_bounds {_vehiclePieceBounds} bounds size {_vehiclePieceBounds.size}");
  }


  private void UpdatePieceCount()
  {
    if ((bool)VehicleInstance.NetView &&
        VehicleInstance?.NetView?.m_zdo != null)
      VehicleInstance.NetView.m_zdo.Set(VehicleZdoVars.MBPieceCount,
        m_nviewPieces.Count);
  }


// for increasing ship wake size.
  private void SetShipWakeBounds()
  {
    if (VehicleInstance?.Instance?.ShipEffectsObj == null) return;

    var firstRudder = m_rudderPieces.First();
    if (firstRudder == null)
    {
      var bounds = FloatCollider.bounds;
      VehicleInstance.Instance.ShipEffectsObj.transform.localPosition =
        new Vector3(FloatCollider.transform.localPosition.x,
          bounds.center.y,
          bounds.min.z);
      return;
    }

    var localPosition = firstRudder.transform.localPosition;
    VehicleInstance.Instance.ShipEffectsObj.transform.localPosition =
      new Vector3(
        localPosition.x,
        FloatCollider.bounds.center.y,
        localPosition.z);
  }

  // pushes the collider down a bit to have the boat spawn above water.
  private const float HullFloatationColliderAlignmentOffset = -1.5f;

  private float GetAverageFloatHeightFromHulls()
  {
    _pendingHullBounds = new Bounds();

    var totalHeight = 0f;

    var isAverageCollider = PhysicsConfig.HullFloatationColliderLocation.Value ==
                            PhysicsConfig.HullFloatation.AverageOfHullPieces;
    var items = isAverageCollider ? m_hullPieces : m_nviewPieces;

    foreach (var piece in items)
    {
      var newBounds = EncapsulateColliders(_vehicleHullBounds.center,
        _vehicleHullBounds.size,
        piece.gameObject);
      totalHeight += piece.transform.localPosition.y;
      if (newBounds == null) continue;
      _pendingHullBounds = newBounds.Value;
    }

    _vehicleHullBounds = _pendingHullBounds;


    switch (PhysicsConfig.HullFloatationColliderLocation.Value)
    {
      case PhysicsConfig.HullFloatation.AverageOfHullPieces:
      case PhysicsConfig.HullFloatation.Average:
        var hullPieceCount =
          PhysicsConfig.EnableExactVehicleBounds.Value
            ? m_hullPieces.Count
            : m_nviewPieces.Count;

        if (Mathf.Approximately(totalHeight, 0f) ||
            Mathf.Approximately(hullPieceCount, 0f))
          return _vehicleHullBounds.center.y;

        return totalHeight / hullPieceCount;

      case PhysicsConfig.HullFloatation.Bottom:
        return _vehicleHullBounds.min.y;
      case PhysicsConfig.HullFloatation.Top:
        return _vehicleHullBounds.max.y;
      case PhysicsConfig.HullFloatation.Custom:
        return PhysicsConfig.HullFloatationCustomColliderOffset
          .Value + HullFloatationColliderAlignmentOffset;
      case PhysicsConfig.HullFloatation.Center:
      default:
        return _vehicleHullBounds.center.y;
    }
  }

  /**
   * Must fire RebuildBounds after doing this otherwise colliders will not have the correct x z axis when rotating the y
   */
  private void RotateVehicleForwardPosition()
  {
    if (VehicleInstance?.MovementController == null) return;

    if (_steeringWheelPieces.Count <= 0) return;
    var firstWheel = _steeringWheelPieces.First();
    if (firstWheel == null || !firstWheel.enabled) return;

    VehicleInstance.MovementController.UpdateShipDirection(
      firstWheel.transform
        .localRotation);
  }

  /// <summary>
  /// For items that cannot be included within bounds without causing problems. E.G movable items that still need to be within the PiecesController
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  public bool IsExcludedBoundsItem(string name)
  {
    if (name.StartsWith(PrefabNames.ShipAnchorWood)) return true;
    return false;
  }

  internal Vector3 m_localShipLeft = Vector3.left;
  internal Vector3 m_localShipRight = Vector3.right;
  internal Vector3 m_localShipForward = Vector3.forward;
  internal Vector3 m_localShipBack = Vector3.back;

  // meant for calculating vehicle's positiona and water height.
  // public Vector3 shipLeft => transform.position + m_localShipLeft;
  // public Vector3 shipRight => transform.position + m_localShipRight;
  // public Vector3 shipForward => transform.position + m_localShipForward;
  // public Vector3 shipBack => transform.position + m_localShipBack;

  public void CalculateFurthestPointsOnMeshes()
  {
    // if (convexHullMeshColliders.Count == 0 ||
    //     MovementController == null || FloatCollider == null) return;
    //
    // var furthestLeft = Vector3.left;
    // var furthestRight = Vector3.right;
    // var furthestFront = Vector3.forward;
    // var furthestBack = Vector3.back;
    //
    // var position = MovementController.transform.position;
    // var forward = MovementController.ShipDirection.forward;
    // var right = MovementController.ShipDirection.right;
    //
    //
    // var shipForward = position +
    //                   forward *
    //                   MovementController.GetFloatSizeFromDirection(
    //                     Vector3.forward);
    // var shipBack = position -
    //                forward *
    //                MovementController.GetFloatSizeFromDirection(
    //                  Vector3.forward);
    // var shipLeft = position -
    //                right *
    //                MovementController
    //                  .GetFloatSizeFromDirection(Vector3.right);
    // var shipRight = position +
    //                 right *
    //                 MovementController.GetFloatSizeFromDirection(
    //                   Vector3.right);
    //
    // foreach (var meshCollider in convexHullMeshColliders)
    // {
    //   // Loop through all vertices of the mesh
    //   // foreach (var vertex in mesh.vertices)
    //   // {
    //   //   // Convert the vertex from local space to world space
    //   //   var worldVertex = meshCollider.transform.TransformPoint(vertex);
    //   //
    //   //   // todo might  have to get offset of meshCollider.position and transform.position and use meshCollider.Inverse instead
    //   //   var relativeVertex = transform.InverseTransformPoint(worldVertex);
    //
    //   var nearestForward =
    //     meshCollider.ClosestPoint(shipForward) - position;
    //   var nearestBack =
    //     meshCollider.ClosestPoint(shipBack) - position;
    //   var nearestLeft =
    //     meshCollider.ClosestPoint(shipLeft) - position;
    //   var nearestRight =
    //     meshCollider.ClosestPoint(shipRight) - position;
    //
    //   if (nearestForward.z > furthestFront.z)
    //     furthestFront = new Vector3(nearestForward.x, FloatCollider.center.y,
    //       nearestForward.z);
    //
    //   if (nearestBack.z < furthestBack.z)
    //     furthestBack = new Vector3(nearestBack.x, FloatCollider.center.y,
    //       nearestBack.z);
    //
    //   if (nearestLeft.x < furthestLeft.x)
    //     furthestLeft = new Vector3(nearestLeft.x, FloatCollider.center.y,
    //       nearestLeft.z);
    //
    //   if (nearestRight.x > furthestRight.x)
    //     furthestRight = new Vector3(nearestRight.x, FloatCollider.center.y,
    //       nearestRight.z);
    // }
    //
    // m_localShipLeft = furthestLeft;
    // m_localShipRight = furthestRight;
    // m_localShipForward = furthestFront;
    // m_localShipBack = furthestBack;
  }

  // public void RebuildConvexHull()
  // {
  //   if (VehicleInstance?.Instance == null || MovementController == null) return;
  //   var vehicleMovementCollidersTransform =
  //     VehicleInstance.Instance.vehicleMovementCollidersTransform;
  //   var nvChildGameObjects = m_nviewPieces.Select(x => x.gameObject)
  //     .Where(x => !IsExcludedBoundsItem(x.gameObject.name)).ToList();
  //   if (WaterConfig.HasUnderwaterHullBubbleEffect.Value)
  //     // Makes it slightly larger extended out from the ship
  //     convexHullComponent.previewScale = new Vector3(1.05f, 1.05f, 1.05f);
  //
  //   convexHullComponent
  //     .GenerateMeshesFromChildColliders(
  //       vehicleMovementCollidersTransform.gameObject, Vector3.zero,
  //       PhysicsConfig.convexHullJoinDistanceThreshold.Value,
  //       nvChildGameObjects, MovementController.DamageColliders);
  //
  //   vehicleCenter.transform.localPosition = convexHullComponent.GetConvexHullBounds(true).center;
  //
  //   // convexHullColliders.Clear();
  //   // convexHullMeshColliders.Clear();
  //   //
  //   // convexHullMeshes.ForEach((x) =>
  //   // {
  //   //   var meshCollider = x.GetComponent<MeshCollider>();
  //   //   var collider = x.GetComponent<Collider>();
  //   //
  //   //   if (meshCollider != null) convexHullMeshColliders.Add(meshCollider);
  //   //   if (collider != null) convexHullColliders.Add(collider);
  //   // });
  //
  //   // convexHullTriggerColliders = MovementController.DamageColliders
  //   //   .GetComponentsInChildren<Collider>(true).ToList();
  //   // convexHullTriggerMeshColliders = MovementController.DamageColliders
  //   //   .GetComponentsInChildren<MeshCollider>(true).ToList();
  // }

  public List<Collider> tempVehicleColliders = new();
  public List<Collider> tempPieceColliders = new();


  public void DebouncedIgnoreAllVehicleColliders()
  {
    CancelInvoke(nameof(IgnoreAllVehicleColliders));
    Invoke(nameof(IgnoreAllVehicleColliders), 0.01f);
  }

  /**
   * Ignores absolutely all vehicle colliders.
   * This is optimized to prevent duplicate colliders and unnecessary allocations.
   */
  public void IgnoreAllVehicleColliders()
  {
    if (VehicleInstance?.Instance == null || MovementController == null) return;

    // ✅ Clear before refilling to avoid stale data

    // ✅ Fetch colliders using `List<>` (HashSet not allowed in GetComponentsInChildren)
    // if (_shouldUpdateVehicleColliders)
    // {
    //   tempVehicleColliders.Clear();
    //   VehicleInstance.Instance.GetComponentsInChildren(true, tempVehicleColliders);
    // }
    //
    // if (_shouldUpdatePieceColliders)
    // {
    //   tempPieceColliders.Clear();
    //   GetComponentsInChildren(true, tempPieceColliders);
    // }

    var vehicleColliders = VehicleInstance.Instance.GetComponentsInChildren<Collider>(true);
    if (vehicleColliders == null) return;

    foreach (var prefabPieceDataItem in prefabPieceDataItems.Values)
    {
      // a removed reference means we should skip this as the colliders will be shortly removed.
      if (prefabPieceDataItem.Prefab == null) continue;
      foreach (var allCollider in prefabPieceDataItem.AllColliders)
      {
        foreach (var vehicleCollider in vehicleColliders)
        {
          if (allCollider == null || vehicleCollider == null) continue;
          Physics.IgnoreCollision(allCollider, vehicleCollider);
        }
      }
    }

    foreach (var vehicleCollider in vehicleColliders)
    {
      // must ignore all vehicle colliders
      foreach (var vehicleCollider2 in vehicleColliders)
      {
        if (vehicleCollider == vehicleCollider2) continue;
        Physics.IgnoreCollision(vehicleCollider, vehicleCollider2, true);
      }
    }
    // var pieceColliders = transform.GetComponentsInChildren<Collider>(true);

    // heavy but simple ignore all colliders.
    // foreach (var vehicleCollider in vehicleColliders)
    // {
    //   // must ignore all vehicle colliders
    //   foreach (var vehicleCollider2 in vehicleColliders)
    //   {
    //     if (vehicleCollider == vehicleCollider2) continue;
    //     Physics.IgnoreCollision(vehicleCollider, vehicleCollider2, true);
    //   }
    //
    //   // vehicle colliders must ignore pieces. But pieces should likely not ignore eachother and it won't matter with how piece controller ignores physics engine.
    //   foreach (var allPieceCollider in pieceColliders)
    //   {
    //     Physics.IgnoreCollision(vehicleCollider, allPieceCollider, true);
    //   }
    // }
  }

  // For when pieces are added
  // extremely unoptimized and allocates. But should work without complex logic managing removals.
  public void IgnoreAllVehicleCollidersForGameObjectChildren(GameObject gameObject)
  {
    var colliders = gameObject.GetComponentsInChildren<Collider>(true);
    // characters should not skip hitting treads. If this happens we would have to track them so they do not fall on the treads and go through them when exiting vehicle after first time on vehicle.
    var character = gameObject.GetComponentInParent<Character>();
    var isCharacter = character != null;
    if (!colliders.Any()) return;
    if (VehicleInstance == null || VehicleInstance.Instance == null) return;
    var allVehicleColliders = VehicleInstance.Instance.GetComponentsInChildren<Collider>(true);
    foreach (var collider in colliders)
    {
      foreach (var allVehicleCollider in allVehicleColliders)
      {
        if (collider.name.StartsWith("tread") && isCharacter)
        {
          // skip character collider ignore.
          continue;
        }
        Physics.IgnoreCollision(collider, allVehicleCollider, true);
      }
    }
  }

  // public void IgnoreColliderForWheelColliders(Collider collider)
  // {
  //   if (collider == null || WheelController == null) return;
  //   foreach (var wheelCollider in WheelController.wheelColliders)
  //   {
  //     if (wheelCollider == null)
  //     {
  //       continue;
  //     }
  //
  //     Physics.IgnoreCollision(collider, wheelCollider, true);
  //   }
  // }

  // public void IgnoreWheelColliders(List<Collider> colliders)
  // {
  //   if (WheelController == null) return;
  //   colliders.ForEach(IgnoreColliderForWheelColliders);
  // }

  // public void IgnoreAllWheelColliders()
  // {
  //   if (WheelController == null || WheelController.wheelColliders.Count == 0) return;
  //   //
  //   // var colliders = new List<Collider>();
  //   // foreach (var wheelCollider in WheelController.wheelColliders.ToList())
  //   // {
  //   //   if (wheelCollider == null)
  //   //   {
  //   //     continue;
  //   //   }
  //   // OnPieceAddedIgnoreAllColliders(wheelCollider);
  //   //
  //   //   var collider = wheelCollider.GetComponent<Collider>();
  //   //   if (collider == null) continue;
  //   //
  //   //   IgnoreShipColliderForCollider(collider, true);
  //   //   colliders.Add(collider);
  //   // }
  //   //
  //   // IgnoreCollidersForList(colliders, m_nviewPieces);
  // }

  public void TryAddRamToVehicle()
  {
    if (VehicleInstance?.MovementController == null) return;
    VehicleInstance.MovementController.TryAddRamAoeToVehicle();
  }

  public void ForceRebuildBounds()
  {
    if (_rebuildBoundsTimer != null)
    {
      StopCoroutine(_rebuildBoundsTimer);
    }

    RebuildBounds(true);
  }

  public void ResetSailCachedValues()
  {
    cachedSailForce = -1;
    cachedTotalSailArea = -1;
  }

  /// <summary>
  /// A override of RebuildBounds scoped towards valheim integration instead of unity-only.
  /// - Must be wrapped in a delay/coroutine to prevent spamming on unmounting bounds
  /// - cannot be de-encapsulated by default so regenerating it seems prudent on piece removal
  /// </summary>
  /// 
  /// <param name="isForced"></param>
  public override void RebuildBounds(bool isForced = false)
  {
    if (!isActiveAndEnabled || ZNetView.m_forceDisableInit || !isInitialPieceActivationComplete) return;
    if (FloatCollider == null || OnboardCollider == null)
      return;

    // methods related to VehiclePiecesController
    // todo Physics sync might not be required, but it ensures accuracy.
    Physics.SyncTransforms();

    ResetSailCachedValues();

    TryAddRamToVehicle();
    TempDisableRamsDuringRebuild();
    
    RotateVehicleForwardPosition();

    if (!convexHullComponent.TriggerParent && MovementController != null)
    {
      convexHullComponent.TriggerParent = MovementController.DamageColliders;
    }

    try
    {
      // internal parent class must still be called.
      base.RebuildBounds(isForced);
    }
    catch (Exception e)
    {
      Logger.LogError(e);
    }
  }

  /// <summary>
  /// A complete override of OnConvexHullGenerated.
  /// </summary>
  public override void OnConvexHullGenerated()
  {
    _vehiclePieceBounds = convexHullComponent.GetConvexHullBounds(true);

    try
    {
      if (WheelController != null)
      {
        WheelController.Initialize(_vehiclePieceBounds);
      }
    }
    catch (Exception e)
    {
      Logger.LogError(e);
    }

    if (RenderingConfig.EnableVehicleClusterMeshRendering.Value && m_nviewPieces.Count >= RenderingConfig.ClusterRenderingPieceThreshold.Value)
    {
      try
      {
        var objects = m_nviewPieces.Where(x => x != null).Select(x => x.gameObject).ToArray();
        m_meshClusterComponent.GenerateCombinedMeshes(objects);
      }
      catch (Exception e)
      {
        Logger.LogError(e);
      }
    }

    // Critical for vehicle stability otherwise it will blast off in a random direction to due colliders internally colliding.
    IgnoreAllVehicleColliders();

    // to accurately place player onboard after rebuild of bounds.
    if (OnboardController != null)
    {
      OnboardController.OnBoundsRebuild();
    }

    OnBoundsChangeUpdateShipColliders();
  }

  /// <summary>
  /// Prevents accidents like the ram hitting when the vehicle is rebuilding bounds
  /// </summary>
  public void TempDisableRamsDuringRebuild()
  {
    if (MovementController == null) return;
    var vehicleRamAoeComponents = MovementController.GetComponentsInChildren<VehicleRamAoe>();

    foreach (var vehicleRamAoeComponent in vehicleRamAoeComponents)
    {
      vehicleRamAoeComponent.OnBoundsRebuildStart();
    }
  }

  public static float floatColliderSizeMultiplier = 1.5f;
  public static float minColliderSize = 1f;
  private const float originalFloatColliderSize = 0.5f;

// todo move this logic to a file that can be tested
// todo compute the float colliderY transform so it aligns with bounds if player builds underneath boat
  public void OnBoundsChangeUpdateShipColliders()
  {
    if (FloatCollider == null || OnboardCollider == null)
    {
      Logger.LogWarning(
        "Ship colliders updated but the ship was unable to access colliders on ship object. Likely cause is ZoneSystem destroying the ship");
      return;
    }
    var convexHullBounds = convexHullComponent.GetConvexHullBounds(false, transform.position);

    if (convexHullBounds == null)
    {
      Logger.LogWarning(
        "Cached convexHullBounds is null this is like a problem with collider setup. Make sure to use custom colliders if other settings are not working");
      return;
    }


    /*
     * @description float collider logic
     * - should match all ship colliders at surface level
     * - surface level eventually will change based on weight of ship and if it is sinking
     */
    var averageFloatHeight = GetAverageFloatHeightFromHulls();
    var floatColliderCenterOffset =
      new Vector3(_vehiclePieceBounds.center.x, averageFloatHeight,
        _vehiclePieceBounds.center.z);

    var floatColliderSize = new Vector3(
      Mathf.Max(minColliderSize,
        convexHullBounds.size.x),
      originalFloatColliderSize,
      Mathf.Max(minColliderSize,
        convexHullBounds.size.z));

    var onboardColliderCenter =
      new Vector3(_vehiclePieceBounds.center.x,
        _vehiclePieceBounds.center.y,
        _vehiclePieceBounds.center.z);
    const float additionalHeight = 3f;
    onboardColliderCenter.y += additionalHeight / 2f;

    var onboardColliderSize = new Vector3(
      Mathf.Max(minColliderSize, _vehiclePieceBounds.size.x),
      Mathf.Max(minColliderSize, _vehiclePieceBounds.size.y),
      Mathf.Max(minColliderSize, _vehiclePieceBounds.size.z));
    onboardColliderSize.y += additionalHeight;

    FloatCollider.size = floatColliderSize;
    FloatCollider.transform.localPosition =
      floatColliderCenterOffset;

    OnboardCollider.size =
      onboardColliderSize;
    OnboardCollider.transform.localPosition = onboardColliderCenter;
    Physics.SyncTransforms();
  }

  public void IgnoreNetViewCollidersForList(ZNetView netView,
    List<ZNetView> list)
  {
    IgnoreCollidersForList(
      netView.GetComponentsInChildren<Collider>(true).ToList(), list);
  }

  public void IgnoreCollidersForList(List<Collider> colliders,
    List<ZNetView> list)
  {
    if (list.Count <= 0) return;
    foreach (var listItem in list.ToList())
    {
      if (listItem == null)
      {
        list.Remove(listItem);
        continue;
      }

      var listItemColliders = listItem.GetComponentsInChildren<Collider>();

      foreach (var collider in colliders)
      foreach (var pieceCollider in listItemColliders)
        Physics.IgnoreCollision(collider, pieceCollider, true);
    }
  }

  public void IgnoreCollidersForAnchorPieces(ZNetView netView)
  {
    IgnoreNetViewCollidersForList(netView, m_anchorPieces);
  }

  public void IgnoreCollidersForRamPieces(ZNetView netView)
  {
    IgnoreNetViewCollidersForList(netView, m_ramPieces);
  }

  // public void IgnoreShipColliderForCollider(Collider collider, bool skipWheelIgnore = false)
  // {
  //   if (collider == null) return;
  //   foreach (var triggerCollider in convexHullTriggerColliders)
  //     Physics.IgnoreCollision(collider, triggerCollider, true);
  //   foreach (var triggerMeshCollider in convexHullMeshColliders)
  //   {
  //     Physics.IgnoreCollision(collider, triggerMeshCollider, true);
  //   }
  //   foreach (var convexHullMesh in convexHullMeshes)
  //     Physics.IgnoreCollision(collider,
  //       convexHullMesh.GetComponent<MeshCollider>(),
  //       true);
  //
  //   if (!skipWheelIgnore)
  //   {
  //     IgnoreColliderForWheelColliders(collider);
  //   }
  //
  //   if (FloatCollider)
  //     Physics.IgnoreCollision(collider, FloatCollider, true);
  //   if (OnboardCollider)
  //     Physics.IgnoreCollision(collider, OnboardCollider, true);
  // }

  // public void IgnoreShipColliders(List<Collider> colliders)
  // {
  //   foreach (var t in colliders) IgnoreShipColliderForCollider(t);
  // }

  public void IgnoreCameraCollision(List<Collider> colliders)
  {
    if (Camera.main == null) return;
    var cameraCollider = Camera.main.GetComponent<Collider>();
    foreach (var t in colliders)
    {
      if (t == null) continue;
      Physics.IgnoreCollision(t, cameraCollider, true);
    }
  }

  public static Bounds TransformColliderGlobalBoundsToLocal(Collider collider)
  {
    if (collider == null)
      throw new ArgumentNullException(nameof(collider));

    // Transform global center into local space of the root transform
    var rootTransform = collider.transform.root;
    var localCenter =
      rootTransform.InverseTransformPoint(collider.bounds.center);

    // Adjust the size for scaling (bounds.size is always in world space)
    var localSize = Vector3.Scale(collider.bounds.size,
      Reciprocal(rootTransform.lossyScale));

    // Return the calculated local bounds
    return new Bounds(localCenter, localSize);
  }

  // Helper extension method to get reciprocal scale
  public static Vector3 Reciprocal(Vector3 v)
  {
    return new Vector3(
      v.x != 0 ? 1f / v.x : 0f,
      v.y != 0 ? 1f / v.y : 0f,
      v.z != 0 ? 1f / v.z : 0f
    );
  }

  /// <summary>
  /// todo this would need to rotate a point if the collider is rotated. Or rotate based on ship direction if the collider is not rotated.
  /// </summary>
  /// <param name="boundsCenter"></param>
  /// <param name="boundsSize"></param>
  /// <param name="netView"></param>
  /// <returns></returns>
  private Bounds? EncapsulateColliders(Vector3 boundsCenter,
    Vector3 boundsSize,
    GameObject netView)
  {
    if (netView.gameObject.name.StartsWith(PrefabNames.KeelColliderPrefix))
      return null;

    var outputBounds = new Bounds(boundsCenter, boundsSize);
    var colliders = netView.GetComponentsInChildren<Collider>(false);

    // filters only physical layers
    var filteredColliders =
      ConvexHullAPI.FilterColliders(colliders.ToList());

    foreach (var collider in filteredColliders)
    {
      var rendererGlobalBounds = TransformColliderGlobalBoundsToLocal(collider);
      outputBounds.Encapsulate(rendererGlobalBounds);
    }

    return outputBounds;
  }

  /// <summary>
  /// Gets all colliders even inactive ones, so they can ignore the vehicles colliders that should not interact with pieces aboard a vehicle
  /// </summary>
  /// If only including active colliders, this would cause a problem if a WearNTear Piece updated its object and the collider began interacting with the vehicle 
  /// <param name="netView"></param>
  /// <returns></returns>
  public static List<Collider> GetCollidersInPiece(GameObject netView,
    bool includeInactive = true)
  {
    return [..netView.GetComponentsInChildren<Collider>(includeInactive)];
  }

  public static Bounds GetCombinedColliderBoundsInPiece(GameObject netView,
    bool includeInactive = true)
  {
    var colliders = GetCollidersInPiece(netView, includeInactive);
    var bounds = new Bounds(Vector3.zero, Vector3.zero);

    foreach (var collider in colliders)
    {
      if (collider == null) continue;
      if (bounds.size == Vector3.zero)
      {
        bounds = collider.bounds;
      }
      bounds.Encapsulate(collider.bounds);
    }

    return bounds;
  }

/*
 * Functional that updates targetBounds, useful for updating with new items or running off large lists and updating the newBounds value without mutating rigidbody values
 * As a safety measure it will never update the vehicle bounds directly.
 */
  public Bounds EncapsulateBounds(GameObject go, Bounds tempBounds)
  {
    var door = go.GetComponentInChildren<Door>();
    var ladder = go.GetComponent<RopeLadderComponent>();
    var isRope = go.name.Equals(PrefabNames.MBRopeLadder);

    if (!door && !ladder && !isRope && !SailPrefabs.IsSail(go.name)
        && !RamPrefabs.IsRam(go.name))
    {
      if (PhysicsConfig.EnableExactVehicleBounds.Value ||
          PrefabNames.IsHull(go))
      {
        var newBounds =
          EncapsulateColliders(tempBounds.center, tempBounds.size,
            go);
        if (newBounds != null)
          return new Bounds(newBounds.Value.center, newBounds.Value.size);
      }
      else
      {
        var relativePosition = transform.InverseTransformPoint(go.transform.position);
        tempBounds.Encapsulate(relativePosition);
      }
    }
    
    return new Bounds(tempBounds.center, tempBounds.size);
  }

  internal override int GetPieceCount()
  {
    if (VehicleInstance == null || VehicleInstance.NetView == null ||
        VehicleInstance.NetView.m_zdo == null)
      return base.GetPieceCount();

    var count =
      VehicleInstance.NetView.m_zdo.GetInt(VehicleZdoVars.MBPieceCount,
        m_nviewPieces.Count);
    return count;
  }
}