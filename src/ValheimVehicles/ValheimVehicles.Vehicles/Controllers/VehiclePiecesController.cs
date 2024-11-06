using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Enums;
using ValheimVehicles.Vehicles.Interfaces;
using ZdoWatcher;
using static ValheimVehicles.Propulsion.Sail.SailAreaForce;
using Component = UnityEngine.Component;
using Logger = Jotunn.Logger;
using Object = System.Object;
using PrefabNames = ValheimVehicles.Prefabs.PrefabNames;

namespace ValheimVehicles.Vehicles;

/// <summary>controller used for all vehicles</summary>
/// <description> This is a controller used for all vehicles, Currently it must be initialized within a vehicle view IE VehicleShip or upcoming VehicleWheeled, and VehicleFlying instances.</description>
public class VehiclePiecesController : MonoBehaviour, IMonoUpdater
{
  /*
   * Get all the instances statically
   */
  public static Dictionary<int, VehiclePiecesController>
    ActiveInstances = new();

  public static Dictionary<int, List<ZNetView>> m_pendingPieces = new();

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

  public bool isInitialActivationComplete = false;

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

  // /// <summary>
  // /// For water access. This will accurately set the lowest relative netview on the ship. This netview position will then be computed in LowestPointOnVehicle.
  // /// </summary>
  // public void UpdateLowestAveragePoint(ZNetView pieceNetView)
  // {
  //   if (IgnoredAveragePointRegexp.IsMatch(pieceNetView.name)) return;
  //
  //   if (LowestPiece == null)
  //   {
  //     LowestPiece = pieceNetView;
  //     return;
  //   }
  //
  //   if (LowestPiece.transform.localPosition.y >
  //       pieceNetView.transform.localPosition.y)
  //   {
  //     LowestPiece = pieceNetView;
  //   }
  // }


  public static bool DEBUGAllowActivatePendingPieces
  {
    get => _allowPendingPiecesToActivate;
    set
    {
      if (value)
      {
        ActivateAllPendingPieces();
      }

      _allowPendingPiecesToActivate = value;
    }
  }

  // rigidbody for all pieces within the ship. Does not directly contribute to floatation, floatation controlled by m_syncRigidbody and synced to this m_rigidbody

  // for the ship physics without item piece colliders or alternatively access via VehicleInstance.m_body
  /// <summary>
  /// Future todo to enable zsync transform for objects within the synced raft which is done on clients only.
  /// </summary>
  internal ZSyncTransform? zsyncTransform;

  public Rigidbody m_body;
  internal FixedJoint m_fixedJoint;

  public List<ZNetView> m_pieces = [];
  internal List<ZNetView> m_ramPieces = [];
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

  public enum InitializationState
  {
    Pending, // when the ship has a pending state
    Complete, // when the ship loads as an existing ship and has pieces.
    Created, // when the ship is created with 0 pieces
  }

  public enum PendingPieceStateEnum
  {
    Idle, // not started
    Scheduled, // called but not started
    Running, // running
    Failure, // failed
    Complete, // completed successfully
    ForceReset, // forced to exit IE teleport or despawn or logout or command to destroy it.
  }

  /// <summary>
  /// For usage in debugging
  /// </summary>
  /// <returns></returns>
  public PendingPieceStateEnum PendingPiecesState => _pendingPiecesState;

  private Coroutine? _rebuildBoundsTimer = null;
  public float ShipContainerMass = 0f;
  public float ShipMass = 0f;
  public static bool hasDebug => ValheimRaftPlugin.Instance.HasDebugBase.Value;

  public float TotalMass => ShipContainerMass + ShipMass;

  /*
   * sail calcs
   */
  public int numberOfTier1Sails = 0;
  public int numberOfTier2Sails = 0;
  public int numberOfTier3Sails = 0;
  public int numberOfTier4Sails = 0;
  public float customSailsArea = 0f;

  public float totalSailArea = 0f;

  public virtual IVehicleShip? VehicleInstance { set; get; }
  public int PersistentZdoId => VehicleInstance?.PersistentZdoId ?? 0;

  public VehicleMovementController? MovementController =>
    VehicleInstance?.MovementController;

/* end sail calcs  */
  private Vector2i m_sector;
  private Vector2i m_serverSector;
  private Bounds _vehicleBounds;
  private Bounds _hullBounds;

  private BoxCollider m_blockingcollider =>
    MovementController?.BlockingCollider ?? new();

  private BoxCollider m_floatcollider =>
    MovementController?.FloatCollider ?? new();

  private BoxCollider m_onboardcollider =>
    MovementController?.OnboardCollider ?? new();

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

  private int rebuildBoundsPromise;

  public List<Bed> GetBedPieces()
  {
    return m_bedPieces;
  }

  public Bounds GetVehicleBounds()
  {
    return _vehicleBounds;
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
  }


  public void FireErrorOnNull(Collider obj, string name)
  {
    if (!(bool)obj)
    {
      Logger.LogError(
        $"BaseVehicleError: collider not initialized for <{name}>");
    }
  }

  public void ValidateInitialization()
  {
    // colliders that must be valid
    FireErrorOnNull(m_floatcollider, PrefabNames.WaterVehicleFloatCollider);
    FireErrorOnNull(m_blockingcollider,
      PrefabNames.WaterVehicleBlockingCollider);
    FireErrorOnNull(m_onboardcollider, PrefabNames.WaterVehicleOnboardCollider);
  }

  private void HideGhostContainer()
  {
    VehicleInstance?.Instance?.GhostContainer()?.SetActive(false);
  }

  /// <summary>
  /// Coroutine to init vehicle just in case things get delayed or desync. This allows for it to wait until things are ready without skipping critical initialization
  /// </summary>
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
      {
        ActiveInstances.Add(vehicleShip.PersistentZdoId, this);
      }

      yield return new WaitForFixedUpdate();
    }
  }

  /// <summary>
  /// Method to be called from the Parent Component that adds this VehiclePiecesController
  /// </summary>
  public void InitFromShip(VehicleShip vehicleShip)
  {
    VehicleInstance = vehicleShip;

    if (!InitializeBaseVehicleValuesWhenReady())
    {
      StartCoroutine(InitVehicle(vehicleShip));
    }
  }

  private IEnumerator ZdoReadyStart()
  {
    InitializeBaseVehicleValuesWhenReady();

    if (VehicleInstance == null)
    {
      Logger.LogError(
        "No ShipInstance detected");
    }

    yield return null;
  }

  public static VehiclePiecesController? GetPieceControllerFromPlayer(
    GameObject playerGo)
  {
    var controller =
      playerGo.transform.root.GetComponent<VehiclePiecesController>();
    if (!controller)
    {
      controller = playerGo.GetComponentInParent<VehiclePiecesController>();
    }

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

  public void Awake()
  {
    if (ZNetView.m_forceDisableInit)
    {
      return;
    }

    _piecesContainer = GetPiecesContainer();
    _movingPiecesContainer = CreateMovingPiecesContainer();
    m_body = _piecesContainer.GetComponent<Rigidbody>();
    m_fixedJoint = _piecesContainer.GetComponent<FixedJoint>();
    InitializationTimer.Start();
  }

  private void LinkFixedJoint()
  {
    if (!MovementController) return;
    if (!m_fixedJoint)
    {
      m_fixedJoint = GetComponent<FixedJoint>();
    }

    if (!m_fixedJoint)
    {
      Logger.LogError(
        "No FixedJoint found. This means the vehicle is not syncing positions");
    }

    m_fixedJoint.connectedBody = MovementController!.GetRigidbody();
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
    yield return new WaitForSeconds(3);
  }

  /*
   * Possible alternatives to this approach:
   * - Add a setter that triggers initializeBaseVehicleValues when the zdo is falsy -> truthy
   */
  private bool InitializeBaseVehicleValuesWhenReady()
  {
    if (ZNetView.m_forceDisableInit) return false;
    // vehicleInstance is the persistent ID, the pieceContainer only has a netView for syncing ship position
    if (VehicleInstance?.NetView == null)
    {
      Logger.LogWarning(
        "Warning netview not detected on vehicle, this means any netview attached events will not bind correctly");
      return false;
    }

    LoadInitState();
    HideGhostContainer();
    LinkFixedJoint();
    return true;
  }

  public virtual void Start()
  {
    ValidateInitialization();

    if (!(bool)ZNet.instance)
    {
      return;
    }

    if (hasDebug)
    {
      Logger.LogInfo($"pieces {m_pieces.Count}");
      Logger.LogInfo($"pendingPieces {m_pendingPieces.Count}");
      Logger.LogInfo($"allPieces {m_allPieces.Count}");
    }

    if (VehicleInstance != null)
    {
      ActiveInstances.Add(VehicleInstance.PersistentZdoId, this);
    }

    /*
     * This should work on both client and server, but the garbage collecting should only apply if the ZDOs are not persistent
     */
    if (ZNet.instance.IsDedicated())
    {
      _serverUpdatePiecesCoroutine =
        StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }

    StartActivatePendingPieces();
  }

  private void OnDisable()
  {
    if (MonoUpdaterInstances.Contains(this))
    {
      MonoUpdaterInstances.Remove(this);
    }

    if (VehicleInstance != null &&
        ActiveInstances.GetValueSafe(VehicleInstance.PersistentZdoId))
    {
      ActiveInstances.Remove(VehicleInstance.PersistentZdoId);
    }

    InitializationTimer.Stop();
    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }

    CleanUp();
  }

  private void OnEnable()
  {
    HasRunCleanup = false;
    MonoUpdaterInstances.Add(this);
    InitializationTimer.Restart();

    StartActivatePendingPieces();
    if (!(bool)ZNet.instance)
    {
      return;
    }

    if (ZNet.instance.IsDedicated() && _serverUpdatePiecesCoroutine == null)
    {
      _serverUpdatePiecesCoroutine =
        StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
    }
  }


  private void OnDestroy()
  {
    if (!HasRunCleanup)
    {
      CleanUp();
    }
  }

  public void CleanUp()
  {
    if (HasRunCleanup) return;
    HasRunCleanup = true;
    RemovePlayersFromBoat();

    if (_pendingPiecesCoroutine != null)
    {
      StopCoroutine(_pendingPiecesCoroutine);
      OnActivatePendingPiecesComplete(PendingPieceStateEnum.ForceReset);
    }

    if (_serverUpdatePiecesCoroutine != null)
    {
      StopCoroutine(_serverUpdatePiecesCoroutine);
    }

    if (VehicleInstance?.Instance == null)
    {
      Logger.LogError("Cleanup called but there is no valid VehicleInstance");
    }

    if (!ZNetScene.instance || VehicleInstance?.PersistentZdoId == null ||
        VehicleInstance?.PersistentZdoId == 0) return;


    foreach (var piece in m_pieces.ToList())
    {
      if (!piece)
      {
        m_pieces.Remove(piece);
        continue;
      }

      piece.transform.SetParent(null);
      AddInactivePiece(VehicleInstance!.PersistentZdoId, piece, null, true);
    }
  }

  public virtual void SyncRigidbodyStats(float drag, float angularDrag,
    bool flight)
  {
    if (!isActiveAndEnabled) return;
    if (MovementController?.m_body == null || m_statsOverride ||
        !VehicleInstance?.Instance || !m_body)
    {
      return;
    }

    if (VehiclePhysicsMode.ForceSyncedRigidbody !=
        localVehiclePhysicsMode)
    {
      SetVehiclePhysicsType(VehiclePhysicsMode.ForceSyncedRigidbody);
    }

    if (flight && localVehiclePhysicsMode ==
        VehiclePhysicsMode.SyncedRigidbody)
    {
      SetVehiclePhysicsType(VehiclePhysicsMode
        .DesyncedJointRigidbodyBody);
    }

    if (!flight &&
        localVehiclePhysicsMode ==
        VehiclePhysicsMode.DesyncedJointRigidbodyBody)
    {
      SetVehiclePhysicsType(VehiclePhysicsMode.SyncedRigidbody);
    }

    var mass = Math.Max(VehicleShip.MinimumRigibodyMass, TotalMass);

    m_body.angularDrag = angularDrag;
    m_body.drag = drag;
    m_body.mass = mass;

    MovementController.m_body.angularDrag = angularDrag;
    MovementController.m_body.drag = drag;
    MovementController.m_body.mass = mass;
  }

  public VehiclePhysicsMode localVehiclePhysicsMode =
    VehiclePhysicsMode.ForceSyncedRigidbody;

  /// <summary>
  /// Deprecated, for now. Will not be used unless this can be leveraged as a fix for some physics objects that need a kinematic rigidbody
  /// </summary>
  private void SyncMovingPiecesContainer()
  {
    if (_movingPiecesContainer == null) return;
    if (_movingPiecesContainer.position != transform.position)
    {
      _movingPiecesContainer.position = transform.position;
    }

    if (_movingPiecesContainer.rotation != transform.rotation)
    {
      _movingPiecesContainer.rotation = transform.rotation;
    }
  }

  public void SetVehiclePhysicsType(
    VehiclePhysicsMode physicsMode)
  {
    localVehiclePhysicsMode = physicsMode;
  }

  public void KinematicSync()
  {
    if (VehicleInstance?.MovementController == null) return;
    if (!m_body.isKinematic)
    {
      m_body.isKinematic = true;
    }

    if (m_body.collisionDetectionMode !=
        CollisionDetectionMode.ContinuousDynamic)
    {
      m_body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    if (m_fixedJoint.connectedBody)
    {
      m_fixedJoint.connectedBody = null;
    }

    m_body.MovePosition(VehicleInstance.MovementController.m_body.position);
    m_body.MoveRotation(
      VehicleInstance.MovementController.m_body.rotation);
  }

  public void JointSync()
  {
    if (m_body.isKinematic)
    {
      m_body.isKinematic = false;
    }

    if (m_fixedJoint.connectedBody == null)
    {
      LinkFixedJoint();
    }
  }

  private bool IsNotFlying =>
    Mathf.Approximately(VehicleInstance?.Instance?.TargetHeight ?? 0f, 0f) ||
    ValheimRaftPlugin.Instance.AllowFlight.Value == false;

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
    if (!(bool)m_body || !(bool)VehicleInstance?.MovementControllerRigidbody ||
        VehicleInstance?.MovementController == null ||
        VehicleInstance.NetView == null ||
        VehicleInstance.Instance == null)
    {
      return;
    }

    if (IsPhysicsForceSynced)
    {
      KinematicSync();
      return;
    }

    if (VehicleMovementController.HasPieceSyncTarget && IsNotFlying)
    {
      var vehiclePhysicsOwner = VehicleInstance.NetView.IsOwner();
      if (vehiclePhysicsOwner)
      {
        KinematicSync();
      }
      else
      {
        JointSync();
      }

      return;
    }

    if (localVehiclePhysicsMode ==
        VehiclePhysicsMode.DesyncedJointRigidbodyBody ||
        !IsNotFlying)
    {
      JointSync();
      return;
    }

    KinematicSync();
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    Client_UpdateAllPieces();
    Sync();
  }

  private void UpdateBedPiece(Bed mBedPiece)
  {
    mBedPiece.m_nview.m_zdo.SetPosition(mBedPiece.m_nview.transform.position);
  }

  public void UpdateBedPieces()
  {
    foreach (var mBedPiece in m_bedPieces)
    {
      UpdateBedPiece(mBedPiece);
    }
  }

  public void CustomFixedUpdate(float deltaTime)
  {
    UpdateBedPieces();
    Sync();
  }

  public void CustomLateUpdate(float deltaTime)
  {
    Sync();
    if (!ZNet.instance.IsServer())
    {
      Client_UpdateAllPieces();
    }
  }


  /// <summary>
  /// This should only be called directly in cases like teleporting or respawning
  /// </summary>
  public void ForceUpdateAllPiecePositions()
  {
    Physics.SyncTransforms();
    var currentPieceControllerSector =
      ZoneSystem.GetZone(transform.position);

    VehicleInstance?.NetView?.m_zdo?.SetPosition(transform.position);


    foreach (var nv in m_pieces.ToList())
    {
      if (!nv)
      {
        Logger.LogError(
          $"Error found with m_pieces: netview {nv}, save removing the piece");
        m_pieces.Remove(nv);
        continue;
      }

      var bedComponent = nv.GetComponent<Bed>();
      if (bedComponent)
      {
        UpdateBedPiece(bedComponent);
        continue;
      }

      nv.m_zdo?.SetPosition(transform.position);
    }
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
      {
        ForceUpdateAllPiecePositions();
      }

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

    if (_serverUpdatePiecesCoroutine != null)
    {
      return;
    }

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

      zdo.SetPosition(pos);
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
    while (isActiveAndEnabled)
    {
      if (!VehicleInstance?.NetView)
      {
        yield return new WaitUntil(() => (bool)VehicleInstance.NetView);
      }

      var output =
        m_allPieces.TryGetValue(VehicleInstance.PersistentZdoId, out var list);
      if (list == null || !output)
      {
        yield return new WaitForSeconds(Math.Max(2f,
          ValheimRaftPlugin.Instance.ServerRaftUpdateZoneInterval
            .Value));
        continue;
      }

      yield return UpdatePiecesWorker(list);
      yield return new WaitForFixedUpdate();
    }
  }

  // this needs to be connected to ropeladder too.
  internal float GetColliderBottom()
  {
    return m_blockingcollider.transform.position.y +
           m_blockingcollider.center.y -
           m_blockingcollider.size.y / 2f;
  }

  public static void AddInactivePiece(int id, ZNetView netView,
    VehiclePiecesController? instance, bool skipActivation = false)
  {
    if (hasDebug)
      Logger.LogDebug($"addInactivePiece called with {id} for {netView.name}");

    instance?.CancelInvoke(nameof(StartActivatePendingPieces));

    if (instance != null && instance.isActiveAndEnabled)
    {
      if (ActiveInstances.TryGetValue(id, out var activeInstance))
      {
        activeInstance.ActivatePiece(netView);
        return;
      }
    }

    AddPendingPiece(id, netView);

    var wnt = netView.GetComponent<WearNTear>();
    if ((bool)wnt) wnt.enabled = false;

    if (!skipActivation && instance != null && instance.isActiveAndEnabled)
    {
      // This will queue up a re-run of ActivatePendingPieces if there are any
      instance?.Invoke(nameof(StartActivatePendingPieces), 1f);
    }
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
  }

  public void DebouncedRebuildBounds()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (_rebuildBoundsTimer != null)
    {
      return;
    }

    _rebuildBoundsTimer = StartCoroutine(DebounceRebuildBoundRoutine());
  }

  private IEnumerator DebounceRebuildBoundRoutine()
  {
    yield return new WaitForSeconds(1f);
    RebuildBounds();
    _rebuildBoundsTimer = null;
  }

  public void RemovePiece(ZNetView netView)
  {
    if (netView.name.Contains(PrefabNames.WaterVehicleShip)) return;
    if (!m_pieces.Remove(netView)) return;

    UpdateMass(netView, true);
    DebouncedRebuildBounds();

    if (PrefabNames.IsHull(netView.gameObject))
    {
      m_hullPieces.Remove(netView);
    }

    var isRam = RamPrefabs.IsRam(netView.name);
    if (isRam)
    {
      m_ramPieces.Remove(netView);
    }

    var components = netView.GetComponents<Component>();

    foreach (var component in components)
    {
      switch (component)
      {
        case SailComponent sail:
          m_sailPieces.Remove(sail);
          break;

        case Fireplace fireplace:
          if (netView.m_zdo != null)
          {
            cachedVehicleBurningEffectAreas.Remove(netView.m_zdo.m_uid);
          }

          break;
        case MastComponent mast:
          m_mastPieces.Remove(mast);
          break;

        case RudderComponent rudder:
          m_rudderPieces.Remove(rudder);
          if (VehicleInstance?.Instance && m_rudderPieces.Count > 0)
          {
            SetShipWakeBounds();
          }

          break;

        case SteeringWheelComponent wheel:
          _steeringWheelPieces.Remove(wheel);
          break;

        case Bed bed:
          m_bedPieces.Remove(bed);
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

    //
    // var effectsArea = netView.GetComponent<EffectArea>();
    // if (effectsArea != null)
    // {
    //   cachedVehicleEffectAreas.Add(effectsArea);
    // }
    //
    // var sail = netView.GetComponent<SailComponent>();
    // if ((bool)sail)
    // {
    //   m_sailPieces.Remove(sail);
    // }
    //
    // var mast = netView.GetComponent<MastComponent>();
    // if ((bool)mast)
    // {
    //   m_mastPieces.Remove(mast);
    // }
    //
    // var rudder = netView.GetComponent<RudderComponent>();
    // if ((bool)rudder)
    // {
    //   m_rudderPieces.Remove(rudder);
    //
    //   if (VehicleInstance?.Instance && m_rudderPieces.Count > 0)
    //   {
    //     SetShipWakeBounds();
    //   }
    // }
    //
    // var wheel = netView.GetComponent<SteeringWheelComponent>();
    // if ((bool)wheel)
    // {
    //   _steeringWheelPieces.Remove(wheel);
    // }
    //
    // var isRam = RamPrefabs.IsRam(netView.name);
    // if (isRam)
    // {
    //   m_ramPieces.Remove(netView);
    // }
    //
    //
    // var bed = netView.GetComponent<Bed>();
    // if ((bool)bed) m_bedPieces.Remove(bed);
    //
    // var ramp = netView.GetComponent<BoardingRampComponent>();
    // if ((bool)ramp) m_boardingRamps.Remove(ramp);
    //
    // var portal = netView.GetComponent<TeleportWorld>();
    // if ((bool)portal) m_portals.Remove(netView);
    //
    // var ladder = netView.GetComponent<RopeLadderComponent>();
    // if ((bool)ladder)
    // {
    //   m_ladders.Remove(ladder);
    //   ladder.m_mbroot = null;
    //   ladder.vehiclePiecesController = null;
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


  private float ComputeContainerWeight(Container container,
    bool isRemoving = false)
  {
    var inventory = container.GetInventory();
    if (inventory == null) return 0f;

    var containerWeight = inventory.GetTotalWeight();
    if (hasDebug)
      Logger.LogDebug(
        $"containerWeight {containerWeight} name: {container.name}");
    if (isRemoving)
    {
      return -containerWeight;
    }

    return containerWeight;
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
    {
      return 20f;
    }

    if (pieceName ==
        PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames
          .ShipHullCenterIronPrefabName))
    {
      return 80f;
    }

    if (pieceName.StartsWith(PrefabNames.HullRib))
    {
      if (pieceName.Contains(ShipHulls.HullMaterial.Iron))
      {
        return 720f;
      }

      return 180f;
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

    if (pieceName.Contains("blastfurnace") ||
        pieceName.Contains("charcoal_kiln") ||
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
      if (wnt.name.Contains(PrefabNames.WaterVehicleShip))
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
    {
      t.transform.SetParent(null);
    }
  }

  public void DestroyVehicle()
  {
    var wntVehicle = GetComponent<WearNTear>();

    RemovePlayersFromBoat();

    if (!CanDestroyVehicle(VehicleInstance?.NetView))
    {
      return;
    }

    if ((bool)wntVehicle)
      wntVehicle.Destroy();
    else if (gameObject)
    {
      Destroy(gameObject);
    }
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
    {
      // delegates to coroutine activation logic
      StartActivatePendingPieces();
    }
  }

  private static void AddPendingPiece(int vehicleId, ZNetView piece)
  {
    ActiveInstances.TryGetValue(vehicleId, out var vehicleInstance);

    if (!m_pendingPieces.ContainsKey(vehicleId))
    {
      m_pendingPieces.Add(vehicleId, []);
    }

    if (vehicleInstance == null)
    {
      m_pendingPieces[vehicleId].Add(piece);
      return;
    }

    vehicleInstance.AddPendingPieceToActiveVehicle(vehicleId, piece);
  }

  public static void ActivateAllPendingPieces()
  {
    foreach (var pieceController in ActiveInstances)
    {
      pieceController.Value.StartActivatePendingPieces();
    }
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
    if (!CanActivatePendingPieces)
    {
      return;
    }

    // do not run if in a Pending or Created state or if no pending pieces
    if (BaseVehicleInitState != InitializationState.Complete &&
        GetCurrentPendingPieces()?.Count == 0) return;

    _pendingPiecesCoroutine =
      StartCoroutine(nameof(ActivatePendingPiecesCoroutine));
  }

  public void OnActivatePendingPiecesComplete(
    PendingPieceStateEnum pieceStateEnum,
    string message = "")
  {
    _pendingPiecesCoroutine = null;
    _pendingPiecesState = pieceStateEnum;
    PendingPiecesTimer.Reset();

    if (!isInitialActivationComplete)
    {
      isInitialActivationComplete = true;
    }

    if (pieceStateEnum == PendingPieceStateEnum.ForceReset)
    {
      InitializationTimer.Reset();
    }
    else
    {
      InitializationTimer.Stop();
    }


    if (pieceStateEnum == PendingPieceStateEnum.Failure)
    {
      Logger.LogWarning(
        $"ActivatePendingPieces did not complete correctly. Reason: {message}");
    }
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
    if (hasDebug)
      Logger.LogDebug(
        $"Ship Size calc is: m_bounds {_vehicleBounds} bounds size {_vehicleBounds.size}");

    m_dynamicObjects.TryGetValue(VehicleInstance?.PersistentZdoId ?? 0,
      out var objectList);
    var objectListHasNoValidItems = true;
    if (objectList is { Count: > 0 })
    {
      if (hasDebug)
        Logger.LogDebug($"m_dynamicObjects is valid: {objectList.Count}");

      foreach (var t in objectList.ToList())
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
          if (dic.TryGetValue(VehicleZdoVars.MBCharacterOffsetHash,
                out var offset))
            nv.transform.position = offset + transform.position;

          offset = default;
        }

        ZDOExtraData.RemoveInt(nv.m_zdo.m_uid,
          VehicleZdoVars.MBCharacterParentHash);
        ZDOExtraData.RemoveVec3(nv.m_zdo.m_uid,
          VehicleZdoVars.MBCharacterOffsetHash);
        dic = null;
      }

      if (VehicleInstance != null)
      {
        m_dynamicObjects.Remove(VehicleInstance.PersistentZdoId);
      }

      yield return null;
    }
  }

  public IEnumerator ActivatePendingPiecesCoroutine()
  {
    if (_baseVehicleInitializationState !=
        InitializationState.Complete) yield break;

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
        yield break;
      }

      _pendingPiecesDirty = false;

      if (currentPieces == null && _newPendingPiecesQueue.Count == 0) continue;
      // Process each pending piece, yielding periodically to avoid frame spikes
      foreach (var piece in currentPieces.ToList())
      {
        // Activate each piece (e.g., instantiate or enable)
        ActivatePiece(piece);

        // Yield after each piece or batch for smoother frame rates
        yield return null;
      }

      // Clear processed items and add any newly queued items
      currentPieces?.Clear();
      if (_newPendingPiecesQueue.Count <= 0) continue;
      currentPieces ??= [];
      currentPieces.AddRange(_newPendingPiecesQueue);
      _newPendingPiecesQueue.Clear();
      _pendingPiecesDirty = true; // Mark dirty to re-run coroutine
    } while
      (_pendingPiecesDirty); // Loop if new items were added during this run

    OnActivatePendingPiecesComplete(PendingPieceStateEnum.Complete);
  }

  // private IEnumerator DEPRECATED_activatePendingPiecesCoroutine()
  // {
  //   // It will wait for the vehicle to be initialized before attempting to run.
  //
  //
  //   // yield return new WaitUntil(() =>
  //   //   (bool)VehicleInstance?.NetView ||
  //   //   BaseVehicleInitState != InitializationState.Complete ||
  //   //   PendingPiecesTimer.ElapsedMilliseconds > 50000);
  //   //
  //   // if (VehicleInstance?.NetView?.GetZDO() == null)
  //   // {
  //   //   yield return new WaitUntil(() =>
  //   //     VehicleInstance?.NetView?.GetZDO() != null);
  //   // }
  //
  //   var persistentZdoId = VehicleInstance?.PersistentZdoId;
  //   if (!persistentZdoId.HasValue)
  //   {
  //     OnActivatePendingPiecesComplete(PendingPieceStateEnum.Failure,
  //       "No persistentID found on Vehicle instance");
  //     yield break;
  //   }
  //
  //   var currentPieces = GetShipActiveInstances(persistentZdoId);
  //
  //   if (currentPieces is { Count: > 0 })
  //   {
  //     var stopwatch = Stopwatch.StartNew();
  //     foreach (var obj in currentPieces.ToList())
  //     {
  //       if ((bool)obj)
  //       {
  //         if (hasDebug)
  //         {
  //           Logger.LogDebug($"ActivatePendingPieces obj: {obj} {obj.name}");
  //         }
  //
  //         ActivatePiece(obj);
  //         if (ZNetScene.instance.InLoadingScreen() ||
  //             stopwatch.ElapsedMilliseconds < 10)
  //           continue;
  //         yield return new WaitForEndOfFrame();
  //         stopwatch.Restart();
  //       }
  //       else
  //       {
  //         currentPieces.FastRemove(obj);
  //         if (hasDebug)
  //         {
  //           Logger.LogDebug($"ActivatePendingPieces obj is not valid {obj}");
  //         }
  //       }
  //     }
  //
  //     // this is commented out b/c it may be triggering the destroy method guard at the bottom.
  //     currentPieces.Clear();
  //     m_pendingPieces.Remove(persistentZdoId.Value);
  //   }
  //
  //   OnActivatePendingPiecesComplete(PendingPieceStateEnum.Complete);
  // }


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
    {
      if (instance.m_onboardcollider.bounds.Contains(p))
      {
        controller = instance;
        return true;
      }
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

    source.m_zdo.RemoveInt(VehicleZdoVars.MBCharacterParentHash);
    source.m_zdo.RemoveVec3(VehicleZdoVars.MBCharacterOffsetHash);
  }

  /// <summary>
  /// Makes the player associated with the vehicle ship, if they spawn they are teleported here.
  /// </summary>
  /// <param name="source"></param>
  /// <param name="bvc"></param>
  /// <returns></returns>
  public static bool AddDynamicParentForVehicle(ZNetView source,
    VehiclePiecesController bvc)
  {
    if (source == null) return false;
    if (!source.isActiveAndEnabled)
    {
      Logger.LogDebug("Player source Not active");
      return false;
    }

    // source.m_zdo.Set(VehicleZdoVars.MBCharacterParentHash, bvc.PersistentZdoId);
    // source.m_zdo.Set(VehicleZdoVars.MBCharacterOffsetHash,
    //   source.transform.position - bvc.transform.position);
    return true;
  }

  public static bool AddDynamicParent(ZNetView source, GameObject target)
  {
    var bvc = target.GetComponentInParent<VehiclePiecesController>();
    if (!(bool)bvc) return false;
    return AddDynamicParentForVehicle(source, bvc);
  }

  public static int GetParentVehicleId(ZNetView netView)
  {
    if (!netView) return 0;

    return netView.GetZDO()?.GetInt(VehicleZdoVars.MBCharacterParentHash, 0) ??
           0;
  }

  public static Vector3 GetDynamicParentOffset(ZNetView netView)
  {
    if (!netView) return Vector3.zero;

    return netView.m_zdo?.GetVec3(VehicleZdoVars.MBCharacterOffsetHash,
             Vector3.zero) ??
           Vector3.zero;
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
    numberOfTier4Sails = 0;

    var hasConfigOverride =
      ValheimRaftPlugin.Instance.EnableCustomPropulsionConfig.Value;

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
          ? ValheimRaftPlugin.Instance.SailTier1Area.Value
          : Tier1;
        totalSailArea += numberOfTier1Sails * multiplier;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier2RaftMastName))
      {
        ++numberOfTier2Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier2Area.Value
          : Tier2;
        totalSailArea += numberOfTier2Sails * multiplier;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier3RaftMastName))
      {
        ++numberOfTier3Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier3Area.Value
          : Tier3;
        totalSailArea += numberOfTier3Sails * multiplier;
        ;
      }
      else if (mMastPiece.name.StartsWith(PrefabNames.Tier4RaftMastName))
      {
        ++numberOfTier4Sails;
        var multiplier = hasConfigOverride
          ? ValheimRaftPlugin.Instance.SailTier4Area.Value
          : Tier4;
        totalSailArea += numberOfTier4Sails * multiplier;
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
    if (totalSailArea != 0 &&
        !ValheimRaftPlugin.Instance.HasShipWeightCalculations.Value)
    {
      totalSailArea = Math.Min(ValheimRaftPlugin.Instance.MaxSailSpeed.Value,
        totalSailArea);
    }

    return totalSailArea;
  }

  public float GetSailingForce()
  {
    var area = GetTotalSailArea();
    if (!ValheimRaftPlugin.Instance.HasShipWeightCalculations.Value)
      return area;

    var mpFactor = ValheimRaftPlugin.Instance.MassPercentageFactor.Value;
    var speedCapMultiplier =
      ValheimRaftPlugin.Instance.SpeedCapMultiplier.Value;

    var sailForce = speedCapMultiplier * area /
                    (TotalMass / mpFactor);

    var maxSailForce = Math.Min(ValheimRaftPlugin.Instance.MaxSailSpeed.Value,
      sailForce);
    var maxPropulsion =
      Math.Min(ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value,
        maxSailForce);
    return maxPropulsion;
  }

  public static void InitZdo(ZDO zdo)
  {
    if (zdo.m_prefab == PrefabNames.WaterVehicleShip.GetStableHashCode())
    {
      return;
    }

    var id = GetParentID(zdo);
    if (id != 0)
    {
      if (!m_allPieces.TryGetValue(id, out var list))
      {
        list = [];
        m_allPieces.Add(id, list);
      }

      // important for preventing a list error if the zdo has already been added
      if (list.Contains(zdo))
      {
        return;
      }

      list.Add(zdo);
    }

    var cid = zdo.GetInt(VehicleZdoVars.MBCharacterParentHash);
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
    if (zdo.m_prefab == PrefabNames.WaterVehicleShip.GetStableHashCode())
    {
      return;
    }

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
    if (netView.name.StartsWith(PrefabNames.WaterVehicleShip) ||
        netView.name.StartsWith(PrefabNames.VehiclePiecesContainer))
    {
      return true;
    }

    return false;
  }

  public static void InitPiece(ZNetView netView)
  {
    if (!netView) return;

    var isPiecesOrWaterVehicle = IsExcludedPrefab(netView.gameObject);

    if (isPiecesOrWaterVehicle)
    {
      return;
    }

    var rb = netView.GetComponentInChildren<Rigidbody>();
    if ((bool)rb && !rb.isKinematic && !RamPrefabs.IsRam((netView.name)))
    {
      return;
    }

    var id = GetParentID(netView.m_zdo);
    if (id == 0) return;

    var parentObj = ZdoWatchController.Instance.GetGameObject(id);
    if (parentObj != null)
    {
      var vehicleShip = parentObj.GetComponent<VehicleShip>();
      if (vehicleShip != null || vehicleShip?.PiecesController != null)
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
  /// Rams are rigidbodies and thus must not be controlled by a RigidBody
  /// </summary>
  /// <param name="targetTransform"></param>
  public void SetPieceToParent(Transform targetTransform)
  {
    if (RamPrefabs.IsRam(targetTransform.name))
    {
      targetTransform.SetParent(VehicleInstance?.Instance?.transform);
      return;
    }

    targetTransform.SetParent(_piecesContainer);
  }

  public void AddTemporaryPiece(Piece piece)
  {
    SetPieceToParent(piece.transform);
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
    {
      return false;
    }

    return !hasPieces;
  }

  /// <summary>
  /// Must return a new material
  /// </summary>
  /// <param name="material"></param>
  /// <returns></returns>
  public static Material FixMaterial(Material material)
  {
    var isBlackMarble = material.name.Contains("blackmarble");
    if (isBlackMarble)
    {
      material.SetFloat(TriplanarLocalPos, 1f);
    }

    material.SetFloat(RippleDistance, 0f);
    material.SetFloat(ValueNoise, 0f);
    return new Material(material);
  }

  public static void FixPieceMeshes(ZNetView netView)
  {
    /*
     * It fixes shadow flicker on all of valheim's prefabs with boats
     * If this is removed, the raft is seizure inducing.
     */
    var meshes = netView.GetComponentsInChildren<MeshRenderer>(true);
    foreach (var meshRenderer in meshes)
    {
      foreach (var meshRendererMaterial in meshRenderer.materials)
      {
        FixMaterial(meshRendererMaterial);
      }

      if ((bool)meshRenderer.sharedMaterial)
      {
        // todo disable triplanar shader which causes shader to move on black marble
        var sharedMaterials = meshRenderer.sharedMaterials;

        for (var j = 0; j < sharedMaterials.Length; j++)
        {
          sharedMaterials[j] = FixMaterial(sharedMaterials[j]);
        }

        meshRenderer.sharedMaterials = sharedMaterials;
      }
    }
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

    SetPieceToParent(netView.transform);

    if (netView.m_zdo != null)
    {
      if (VehicleInstance?.PersistentZdoId != null)
      {
        netView.m_zdo.Set(VehicleZdoVars.MBParentIdHash,
          VehicleInstance.PersistentZdoId);
      }
      else
      {
        // We should not reach this, but this would be a critical issue and should be tracked.
        Logger.LogError(
          "Potential update error detected: Ship parent ZDO is invalid but added a Piece to the ship");
      }

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

    if (previousCount == 0 && GetPieceCount() == 1)
    {
      SetInitComplete();
    }
  }

// must call wnt destroy otherwise the piece is removed but not destroyed like a player destroying an item.
// must create a new array to prevent a collection modify error
  public void OnAddSteeringWheelDestroyPrevious(ZNetView netView,
    SteeringWheelComponent steeringWheelComponent)
  {
    var wheelPieces = _steeringWheelPieces;
    if (wheelPieces.Count <= 0) return;

    foreach (var wheelPiece in wheelPieces)
    {
      if (wheelPiece == null) return;
      var wnt = wheelPiece.GetComponent<WearNTear>();
      if (wnt == null) return;
      wnt.Destroy();
    }
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

  public void AddPiece(ZNetView netView, bool isNew = false)
  {
    if (!(bool)netView)
    {
      Logger.LogError("netView does not exist but somehow called AddPiece()");
      return;
    }

    FixPieceMeshes(netView);
    IgnoreCollidersForAllRamPieces(netView);

    var shouldRebuildBounds = false;
    totalSailArea = 0;
    m_pieces.Add(netView);
    UpdatePieceCount();
    // UpdateLowestAveragePoint(netView);

    // Cache components
    var components = netView.GetComponents<Component>();

    foreach (var component in components)
    {
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

        case RudderComponent rudder:
          m_rudderPieces.Add(rudder);
          SetShipWakeBounds();
          break;

        case SteeringWheelComponent wheel:
          OnAddSteeringWheelDestroyPrevious(netView, wheel);
          _steeringWheelPieces.Add(wheel);
          wheel.InitializeControls(netView, VehicleInstance);
          shouldRebuildBounds = true;
          break;

        case TeleportWorld portal:
          m_portals.Add(netView);
          break;

        case RopeLadderComponent ladder:
          m_ladders.Add(ladder);
          ladder.vehiclePiecesController = this;
          break;
      }
    }

    if (RamPrefabs.IsRam(netView.name))
    {
      m_ramPieces.Add(netView);
      var vehicleRamAoe = netView.GetComponentInChildren<VehicleRamAoe>();
      if ((bool)vehicleRamAoe)
      {
        vehicleRamAoe.vehicle = VehicleInstance.Instance;
      }
    }

    // Remove non-kinematic rigidbodies if not a ram
    if (!RamPrefabs.IsRam(netView.name))
    {
      var rbs = netView.GetComponentsInChildren<Rigidbody>();
      foreach (var rbsItem in rbs)
      {
        if (!rbsItem.isKinematic)
          Destroy(rbsItem);
      }
    }

    UpdateMass(netView);

    // Handle bounds rebuilding
    switch (isNew)
    {
      case true when shouldRebuildBounds:
        RebuildBounds();
        break;
      case true:
        EncapsulateBounds(netView.gameObject);
        break;
      case false:
        DebouncedRebuildBounds();
        break;
    }

    if (hasDebug)
    {
      Logger.LogDebug(
        $"After Adding Piece: {netView.name}, Ship Size calc is: m_bounds {_vehicleBounds} bounds size {_vehicleBounds.size}");
    }
  }


  private void UpdatePieceCount()
  {
    if ((bool)VehicleInstance.NetView &&
        VehicleInstance?.NetView?.m_zdo != null)
      VehicleInstance.NetView.m_zdo.Set(VehicleZdoVars.MBPieceCount,
        m_pieces.Count);
  }

  private void UpdateShipBounds()
  {
    OnBoundsChangeUpdateShipColliders();
  }

  /// <summary>
  /// Used to update stats efficiently when the updater reaches the end re-renders or deletions of piece will spam this, but the last item will then invoke it
  /// </summary>
  private void OnShipBoundsChange()
  {
    CancelInvoke(nameof(UpdateShipBounds));
    Invoke(nameof(UpdateShipBounds), 0.1f);
  }


// for increasing ship wake size.
  private void SetShipWakeBounds()
  {
    if (VehicleInstance?.Instance?.ShipEffectsObj == null) return;

    var firstRudder = m_rudderPieces.First();
    if (firstRudder == null)
    {
      VehicleInstance.Instance.ShipEffectsObj.transform.localPosition =
        new Vector3(m_floatcollider.transform.localPosition.x,
          m_floatcollider.bounds.center.y,
          m_floatcollider.bounds.min.z);
      return;
    }

    VehicleInstance.Instance.ShipEffectsObj.transform.localPosition =
      new Vector3(
        firstRudder.transform.localPosition.x,
        m_floatcollider.bounds.center.y,
        firstRudder.transform.localPosition.z);
  }

  private float GetAverageFloatHeightFromHulls()
  {
    _hullBounds = new Bounds();

    if (m_hullPieces.Count <= 0 ||
        !ValheimRaftPlugin.Instance.HullCollisionOnly.Value)
    {
      return ValheimRaftPlugin.Instance.HullFloatationCustomColliderOffset
        .Value;
    }

    var totalHeight = 0f;
    foreach (var hullPiece in m_hullPieces)
    {
      var newBounds = EncapsulateColliders(_hullBounds.center, _hullBounds.size,
        hullPiece.gameObject);
      totalHeight += hullPiece.transform.localPosition.y;
      if (newBounds == null) continue;
      _hullBounds = newBounds.Value;
    }

    switch (ValheimRaftPlugin.Instance.HullFloatationColliderLocation.Value)
    {
      case ValheimRaftPlugin.HullFloatation.Average:
        return totalHeight / m_hullPieces.Count;
      case ValheimRaftPlugin.HullFloatation.Bottom:
        return _hullBounds.min.y;
      case ValheimRaftPlugin.HullFloatation.Top:
        return _hullBounds.max.y;
      case ValheimRaftPlugin.HullFloatation.Custom:
        return ValheimRaftPlugin.Instance.HullFloatationCustomColliderOffset
          .Value;
      case ValheimRaftPlugin.HullFloatation.Center:
      default:
        return _hullBounds.center.y;
    }
  }

  /**
   * Must fire RebuildBounds after doing this otherwise colliders will not have the correct x z axis when rotating the y
   */
  private void RotateVehicleForwardPosition()
  {
    if (VehicleInstance.MovementController == null)
    {
      return;
    }

    if (_steeringWheelPieces.Count <= 0) return;
    var firstPiece = _steeringWheelPieces.First();
    if (!firstPiece.enabled)
    {
      return;
    }

    VehicleInstance?.Instance?.MovementController?.UpdateShipDirection(
      firstPiece.transform
        .localRotation);
  }

  /**
   * Must be wrapped in an Invoke delay to prevent spamming on unmounting
   * bounds cannot be de-encapsulated by default so regenerating it seems prudent on piece removal
   */
  public void RebuildBounds()
  {
    if (!(bool)m_floatcollider || !(bool)m_onboardcollider ||
        !(bool)m_blockingcollider)
    {
      return;
    }

    RotateVehicleForwardPosition();
    Physics.SyncTransforms();

    _vehicleBounds = new Bounds();

    foreach (var netView in m_pieces.ToList())
    {
      if (!netView)
      {
        m_pieces.Remove(netView);
        continue;
      }

      // This may need to be called within the encapsulation section to validate the bounds.min.y + transform.position is the true lowest section.
      // UpdateLowestAveragePoint(netView);
      EncapsulateBounds(netView.gameObject);
    }

    OnBoundsChangeUpdateShipColliders();
  }


// todo move this logic to a file that can be tested
// todo compute the float colliderY transform so it aligns with bounds if player builds underneath boat
  public void OnBoundsChangeUpdateShipColliders()
  {
    var minColliderSize = 0.1f;
    if (!(bool)m_blockingcollider || !(bool)m_floatcollider ||
        !(bool)m_onboardcollider)
    {
      Logger.LogWarning(
        "Ship colliders updated but the ship was unable to access colliders on ship object. Likely cause is ZoneSystem destroying the ship");
      return;
    }

    /*
     * @description float collider logic
     * - should match all ship colliders at surface level
     * - surface level eventually will change based on weight of ship and if it is sinking
     */
    var averageFloatHeight = GetAverageFloatHeightFromHulls();
    var floatColliderCenterOffset =
      new Vector3(_vehicleBounds.center.x, averageFloatHeight,
        _vehicleBounds.center.z);
    // var floatColliderSize = new Vector3(Mathf.Max(minColliderSize, _vehicleBounds.size.x),
    // m_floatcollider.size.y, Mathf.Max(minColliderSize, _vehicleBounds.size.z));

    var floatColliderSize = new Vector3(
      Mathf.Max(minColliderSize, _vehicleBounds.size.x),
      m_floatcollider.size.y,
      Mathf.Max(minColliderSize, _vehicleBounds.size.z));

    /*
     * onboard colliders
     * need to be higher than the items placed on the ship.
     *
     * todo make this logic exact.
     * - Have a minimum "deck" position and determine height based on the deck. For now this do not need to be done
     */
    const float characterTriggerMaxAddedHeight = 10;
    const float characterTriggerMinHeight = 4;
    const float characterHeightScalar = 1.15f;
    // var computedOnboardTriggerHeight =
    //   Math.Min(
    //     Math.Max(_vehicleBounds.size.y * characterHeightScalar,
    //       _vehicleBounds.size.y + characterTriggerMinHeight),
    //     _vehicleBounds.size.y + characterTriggerMaxAddedHeight) -
    //   m_floatcollider.size.y;
    var onboardColliderCenter =
      new Vector3(_vehicleBounds.center.x,
        _vehicleBounds.center.y,
        _vehicleBounds.center.z);
    var onboardColliderSize = new Vector3(
      Mathf.Max(minColliderSize, _vehicleBounds.size.x),
      Mathf.Max(minColliderSize, _vehicleBounds.size.y),
      Mathf.Max(minColliderSize, _vehicleBounds.size.z));

    /*
     * blocking collider is the collider that prevents the ship from going through objects.
     * - must start at the float collider and encapsulate only up to the ship deck otherwise the ship becomes unstable due to the collider being used for gravity
     * This collider could likely share most of floatcollider settings and sync them
     * - may need an additional size
     * - may need more logic for water masks (hiding water on boat) and other boat magic that has not been added yet.
     */
    var blockingColliderCenterY = floatColliderCenterOffset.y + 0.2f;
    var blockingColliderCenterOffset = new Vector3(_vehicleBounds.center.x,
      blockingColliderCenterY, _vehicleBounds.center.z);
    var blockingColliderSize = new Vector3(
      Mathf.Max(minColliderSize, _vehicleBounds.size.x),
      floatColliderSize.y,
      Mathf.Max(minColliderSize, _vehicleBounds.size.z));

    if (ValheimRaftPlugin.Instance.HullCollisionOnly.Value &&
        _hullBounds.size != Vector3.zero)
    {
      blockingColliderCenterOffset.x = _hullBounds.center.x;
      blockingColliderCenterOffset.z = _hullBounds.center.z;
      blockingColliderSize.x = _hullBounds.size.x;
      blockingColliderSize.z = _hullBounds.size.z;

      floatColliderCenterOffset.x = _hullBounds.center.x;
      floatColliderCenterOffset.z = _hullBounds.center.z;
      floatColliderSize.x = _hullBounds.size.x;
      floatColliderSize.z = _hullBounds.size.z;
    }

    // Assign all the colliders
    m_blockingcollider.size = blockingColliderSize;
    m_blockingcollider.transform.localPosition = blockingColliderCenterOffset;

    m_floatcollider.size = floatColliderSize;
    m_floatcollider.transform.localPosition = floatColliderCenterOffset;

    m_onboardcollider.size = onboardColliderSize;
    m_onboardcollider.transform.localPosition = onboardColliderCenter;

    FloatColliderDefaultPosition = m_floatcollider.transform.localPosition;
    BlockingColliderDefaultPosition =
      m_blockingcollider.transform.localPosition;
  }

  public void IgnoreCollidersForAllRamPieces(ZNetView netView)
  {
    if (m_ramPieces.Count <= 0) return;
    var colliders = netView.GetComponentsInChildren<Collider>();
    foreach (var mRamPiece in m_ramPieces)
    {
      var ramColliders = mRamPiece.GetComponentsInChildren<Collider>();

      foreach (var collider in colliders)
      {
        foreach (var ramCollider in ramColliders)
        {
          Physics.IgnoreCollision(collider, ramCollider, true);
        }
      }
    }
  }


  /// <summary>
  /// Ignore all colliders on the vehicleShip for a placed ram so it doesn't make the Vehicle Freak out
  /// </summary>
  /// <param name="ramColliders">Colliders from a Ram</param>
  public void IgnoreCollidersOnShipForRams(List<Collider> ramColliders)
  {
    foreach (var t in ramColliders)
    {
      if (t == null) continue;
      if (m_floatcollider) Physics.IgnoreCollision(t, m_floatcollider, true);
      if (m_blockingcollider)
        Physics.IgnoreCollision(t, m_blockingcollider, true);
      if (m_onboardcollider)
        Physics.IgnoreCollision(t, m_onboardcollider, true);
      foreach (var nv in m_pieces.ToList())
      {
        if (!nv)
        {
          m_pieces.Remove(nv);
          continue;
        }

        var nvColliders = nv.GetComponentsInChildren<Collider>();
        foreach (var nvCollider in nvColliders)
        {
          Physics.IgnoreCollision(t, nvCollider, true);
        }
      }
    }
  }

  public void IgnoreShipColliders(List<Collider> colliders)
  {
    foreach (var t in colliders.ToList())
    {
      if (t == null) continue;
      if (m_floatcollider) Physics.IgnoreCollision(t, m_floatcollider, true);
      if (m_blockingcollider)
        Physics.IgnoreCollision(t, m_blockingcollider, true);
      if (m_onboardcollider)
        Physics.IgnoreCollision(t, m_onboardcollider, true);
    }
  }

  /// <summary>
  /// Should ignore camera jitter, however it does not work yet
  /// </summary>
  /// TODO fix this or add a patch to ignore camera collision in roofed boats or when paning and colliding with wheel
  /// <param name="colliders"></param>
  public void IgnoreCameraCollision(List<Collider> colliders)
  {
    var cameraMask = GameCamera.instance.m_blockCameraMask;
    foreach (var t in colliders.ToList())
    {
      if (t == null) continue;
      t.excludeLayers |= cameraMask;
    }
  }

  ///
  /// <summary>Parses Collider.bounds and confirm if it's local/ or out of ship bounds</summary>
  /// - Collider.bounds should be global, but it may not be returning the correct value when instantiated
  /// - world position bounds will desync when the vehicle moves
  /// - Using global position bounds with a local bounds will cause the center to extend to the global position and break the raft
  ///
  /// Looks like the solution is Physics.SyncTransforms() because on first render before Physics it does not update transforms.
  ///
  public static Bounds? TransformColliderGlobalBoundsToLocal(Collider collider)
  {
    var colliderCenterMagnitude = collider.bounds.center.magnitude;
    var worldPositionMagnitude = collider.transform.position.magnitude;

    Vector3 center;

    /*
     * <summary>
     * confirms that the magnitude is near zero when subtracting a guaranteed world-position coordinate with a bounds.center coordinate that could be local or global.
     * </summary>
     *
     * - if magnitude is above 5f (or probably even 1f) it is very likely a local position subtracted against a global position.
     *
     * - Limitations: Near world center 0,0,0 this calc likely will not be accurate, but won't really matter
     */
    var isOutOfBounds =
      Mathf.Abs(colliderCenterMagnitude - worldPositionMagnitude) > 5f;
    if (isOutOfBounds)
    {
      return new Bounds(
        collider.transform.root.transform.InverseTransformPoint(collider
          .transform.position),
        collider.bounds.size);
    }

    center =
      collider.transform.root.transform.InverseTransformPoint(collider.bounds
        .center);
    var size = collider.bounds.size;
    var outputBounds = new Bounds(center, size);
    return outputBounds;
  }

  private Bounds? EncapsulateColliders(Vector3 boundsCenter,
    Vector3 boundsSize,
    GameObject netView)
  {
    if (!(bool)m_floatcollider) return null;
    var outputBounds = new Bounds(boundsCenter, boundsSize);
    var colliders = netView.GetComponentsInChildren<Collider>();
    foreach (var collider in colliders)
    {
      if (collider.gameObject.layer != PrefabRegistryHelpers.PieceLayer ||
          collider.gameObject.name.StartsWith(PrefabNames.KeelColliderPrefix))
      {
        continue;
      }

      var rendererGlobalBounds = TransformColliderGlobalBoundsToLocal(collider);
      if (rendererGlobalBounds == null) continue;
      outputBounds.Encapsulate(rendererGlobalBounds.Value);
    }

    return outputBounds;
  }

  /// <summary>
  /// Gets all colliders even inactive ones, so they can ignore the vehicles colliders that should not interact with pieces aboard a vehicle
  /// </summary>
  /// If only including active colliders, this would cause a problem if a WearNTear Piece updated its object and the collider began interacting with the vehicle 
  /// <param name="netView"></param>
  /// <returns></returns>
  public static List<Collider> GetCollidersInPiece(GameObject netView)
  {
    // var piece = netView.GetComponent<Piece>();
    // return piece
    //   ? piece.GetAllColliders()
    return [..netView.GetComponentsInChildren<Collider>(true)];
  }

/*
 * Functional that updates targetBounds, useful for updating with new items or running off large lists and updating the newBounds value without mutating rigidbody values
 */
  public void EncapsulateBounds(GameObject go)
  {
    var colliders = GetCollidersInPiece(go);

    if (RamPrefabs.IsRam(go.name))
    {
      IgnoreCollidersOnShipForRams(colliders);
    }

    IgnoreShipColliders(colliders);

    var door = go.GetComponentInChildren<Door>();
    var ladder = go.GetComponent<RopeLadderComponent>();
    var isRope = go.name.Equals(PrefabNames.MBRopeLadder);

    if (!door && !ladder && !isRope && !SailPrefabs.IsSail(go.name)
        && !RamPrefabs.IsRam(go.name))
    {
      if (ValheimRaftPlugin.Instance.EnableExactVehicleBounds.Value ||
          PrefabNames.IsHull(go))
      {
        var newBounds =
          EncapsulateColliders(_vehicleBounds.center, _vehicleBounds.size, go);
        if (newBounds == null) return;
        _vehicleBounds = newBounds.Value;
        OnShipBoundsChange();
        return;
      }

      _vehicleBounds.Encapsulate(go.transform.localPosition);
    }
  }

  internal int GetPieceCount()
  {
    if (!VehicleInstance.NetView || VehicleInstance.NetView.m_zdo == null)
    {
      return m_pieces.Count;
    }

    var count =
      VehicleInstance.NetView.m_zdo.GetInt(VehicleZdoVars.MBPieceCount,
        m_pieces.Count);
    return count;
  }
}