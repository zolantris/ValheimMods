#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Text.RegularExpressions;
  using HarmonyLib;
  using Jotunn;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Integrations;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Patches;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.Prefabs.Registry;
  using ValheimVehicles.Propulsion.Rudder;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Enums;
  using ValheimVehicles.SharedScripts.Helpers;
  using ValheimVehicles.Structs;
  using ValheimVehicles.ValheimVehicles.Components;
  using ZdoWatcher;
  using ZdoWatcher.ZdoWatcher.Utils;
  using static ValheimVehicles.Propulsion.Sail.SailAreaForce;
  using Component = UnityEngine.Component;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.Controllers;

  public class VehiclePieceActivator : BasePieceActivatorComponent
  {
    [SerializeField] private VehiclePiecesController _host;

    public override IPieceActivatorHost Host => _host;

    public void Init(VehiclePiecesController host)
    {
      _host = host;
    }

    protected override void TrySetPieceToParent(ZNetView netView)
    {
      if (netView == null || PrefabNames.IsVehicle(netView.name)) return;
      // Classic vehicle-specific logic
      netView.transform.SetParent(_host.GetPiecesContainer(), false);
    }
    protected override void AddPiece(ZNetView netView, bool isNewPiece = false)
    {
      _host.AddPiece(netView, isNewPiece);
    }
  }

  /// <summary>controller used for all vehicles</summary>
  /// <description> This is a controller used for all vehicles, Currently it must be initialized within a vehicle view IE VehicleShip or upcoming VehicleWheeled, and VehicleFlying instances.</description>
  public sealed class VehiclePiecesController : BasePiecesController, IMonoUpdater, IVehicleSharedProperties, IPieceActivatorHost, IPieceController, IRaycastPieceActivator
  {

    // pushes the collider down a bit to have the boat spawn above water.
    private const float HullFloatationColliderAlignmentOffset = -1.5f;
    private const float originalFloatColliderSize = 0.5f;
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
    public static readonly Dictionary<Collider, int> m_prefabPieceColliderToIdMap = new();


    public static Dictionary<int, List<ZDO>> m_allPieces = new();

    /// <summary>
    /// These are materials or players that will not be persisted via ZDOs but are "on" the ship and will be added as a child of the ship
    /// </summary>
    public static Dictionary<int, List<ZDOID>>
      m_dynamicObjects = new();

    // public static Dictionary<ZDOID, ZNetView> temporaryMaterials = new();

    public static List<IMonoUpdater> MonoUpdaterInstances = [];
    private static string _ComponentName => PrefabNames.VehiclePiecesContainer;
    public string ComponentName => _ComponentName;

    private static bool _allowPendingPiecesToActivate = true;

    private static readonly string[] IgnoredPrefabNames =
    [
      PrefabNames.CustomWaterMask,
      PrefabNames.CustomWaterMaskCreator
    ];

    private static readonly Regex IgnoredAveragePointRegexp = new(
      $"^({string.Join("|", IgnoredPrefabNames.Select(Regex.Escape))})",
      RegexOptions.Compiled);

    public static bool UseManualSync = true;
    private static bool itemsRemovedDuringWait;


    public static bool HasSetupMeshClusterController;

    // for adding additional multiplier within lower ranges so angle gets to expected value quicker.
    public static float rotationLeanMultiplier = 1.5f;

    public static bool CanUseActualPiecePosition = RenderingConfig.UNSTABLE_VehiclePositionSync_AllowVehiclePiecesToUseWorldPosition.Value;
    public static bool CanBedsUseActualWorldPosition = RenderingConfig.VehiclePositionSync_AllowBedsToSyncToWorldPosition.Value;

    public static float floatColliderSizeMultiplier = 1.5f;
    public static float minColliderSize = 1f;

    public bool HasRunCleanup;

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


    public bool useWorldColliderPosition;

    // abstraction from convexHullAPI overridess it.
    public ConvexHullComponent convexHullComponent = null!;

    public List<MeshCollider> convexHullTriggerMeshColliders = [];
    public List<Collider> convexHullTriggerColliders = [];

    public List<ZNetView> m_pieces = [];
    public List<ZNetView> m_tempPieces = [];

    public float ShipMass;

    /*
     * sail calcs
     */
    public int numberOfTier1Sails;
    public int numberOfTier2Sails;
    public int numberOfTier3Sails;
    public int numberOfTier4Sails;
    public float customSailsArea;

    public float cachedTotalSailArea;
    public float cachedSailForce;

    public bool m_statsOverride;

    public GameObject vehicleCenter;

    public HoverFadeText m_hoverFadeText;

    public VehiclePhysicsMode localVehiclePhysicsMode =
      VehiclePhysicsMode.ForceSyncedRigidbody;

    public float sailLeanAngle;
    public float sailDirectionDamping = 0.05f;

    public bool _isInvalid = true;

    public List<Collider> tempVehicleColliders = new();
    public List<Collider> tempPieceColliders = new();

    private readonly List<ZNetView> _newPendingPiecesQueue = [];

    /// <summary>
    /// ZDOID of the fire component per netview. Since these are nested it's much heavier to find it.
    /// </summary>
    /// Premise: 1 Unique EffectArea per NetView. (this could be wrong in future updates)
    public readonly Dictionary<ZDOID, EffectArea>
      m_vehicleBurningEffectAreas = new();


    private Coroutine? _bedUpdateCoroutine;

    private GameObject _movingPiecesContainerObj;
    private Transform? _movingPiecesContainerTransform;
    private Bounds _pendingHullBounds;


    private Coroutine? _pendingPiecesCoroutine;
    private bool _pendingPiecesDirty;

    // private Bounds BaseControllerHullBounds;
    private Bounds _pendingVehicleBounds;

    private VehiclePieceActivator _pieceActivator;

    private Transform? _piecesContainerTransform;
    private Coroutine? _serverUpdatePiecesCoroutine;

    // only one is allowed
    internal SteeringWheelComponent? _steeringWheelPiece;

    // only one is allowed
    internal RopeAnchorComponent? m_dockAnchor;

    private Bounds BaseControllerHullBounds;

    private Bounds BaseControllerPieceBounds;
    private Transform floatColliderTransform;

    internal Stopwatch InitializationTimer = new();

    private bool IsPhysicsForceSynced =
      PropulsionConfig.DefaultPhysicsMode.Value ==
      VehiclePhysicsMode.ForceSyncedRigidbody;


    private AnchorState lastAnchorState = AnchorState.Idle;

    internal List<VehicleAnchorMechanismController> m_anchorMechanismComponents =
      [];
    internal List<ZNetView> m_anchorPieces = [];


    // todo make a patch to fix coordinates on death to send player to the correct zdo location.
    // bed component
    internal List<Bed> m_bedPieces = [];

    internal List<BoardingRampComponent> m_boardingRamps = [];

    internal List<ZNetView> m_hullPieces = [];

    internal List<RopeLadderComponent> m_ladders = [];
    internal Vector3 m_localShipBack = Vector3.back;
    internal Vector3 m_localShipForward = Vector3.forward;

    internal Vector3 m_localShipLeft = Vector3.left;
    internal Vector3 m_localShipRight = Vector3.right;

    internal List<MastComponent> m_mastPieces = [];

    internal List<ZNetView> m_portals = [];
    internal List<ZNetView> m_ramPieces = [];
    internal List<ShieldGenerator> m_shieldGenerators = [];


    // ship rudders
    internal List<RudderComponent> m_rudderPieces = [];

    internal List<SailComponent> m_sailPieces = [];

/* end sail calcs  */
    private Vector2i m_sector;
    private Vector2i m_serverSector;

    private Transform onboardColliderTransform;
    internal Stopwatch PendingPiecesTimer = new();

    private Transform piecesCollidersTransform;

    public InitializationState BaseVehicleInitState
    {
      get;
      private set;
    } = InitializationState.Pending;

    public bool IsActivationComplete =>
      PendingPiecesState is not PendingPieceStateEnum.Failure
        and not PendingPieceStateEnum.Running;

    public static bool DEBUGAllowActivatePendingPieces
    {
      get => _allowPendingPiecesToActivate;
      set
      {
        if (value) ActivateAllPendingPieces();

        _allowPendingPiecesToActivate = value;
      }
    }

    /// <summary>
    /// For usage in debugging
    /// </summary>
    /// <returns></returns>
    public PendingPieceStateEnum PendingPiecesState
    {
      get;
      private set;
    } = PendingPieceStateEnum.Idle;

    public static bool hasDebug => VehicleGuiMenuConfig.HasDebugPieces.Value;

    public float TotalMass => ShipMass;

    public int PersistentZdoId => Manager?.PersistentZdoId ?? 0;

    public BoxCollider? FloatCollider { get; set; }

    public BoxCollider? OnboardCollider { get; set; }

    private bool IsNotFlying =>
      !MovementController?.IsFlying() ?? false ||
      PropulsionConfig.AllowFlight.Value == false;

    public bool CanActivatePendingPieces => _pendingPiecesCoroutine == null;

    public TargetController targetController;

    public override void Awake()
    {
      SetupMeshClusterController();
      if (ZNetView.m_forceDisableInit) return;

      // must be called before base.awake()
      SetupJobHandlerOnZNetScene();

      base.Awake();

      AddTargetController();

      if (vehicleCenter == null)
      {
        CreatePieceCenter();
      }

      m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);

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

      // mBaseControllerCollisionManager.AddObjectToVehicle(FloatCollider.gameObject);
      // mBaseControllerCollisionManager.AddObjectToVehicle(OnboardCollider.gameObject);
      InitConvexHullGenerator();
      _piecesContainerTransform = GetPiecesContainer();
      _movingPiecesContainerTransform = CreateMovingPiecesContainer();
      m_localRigidbody = _piecesContainerTransform.GetComponent<Rigidbody>();
      InitializationTimer.Start();
    }

    public void AddTargetController()
    {
      targetController = gameObject.AddComponent<TargetController>();
      targetController.targetingMode = TargetController.TargetingMode.DefendPlayer;
      targetController.autoFire = true;
    }


    public void Start()
    {
      ValidateInitialization();
      if (!(bool)ZNet.instance) return;
      if (hasDebug)
      {
        LoggerProvider.LogInfo($"pieces {m_pieces.Count}");
        LoggerProvider.LogInfo($"pendingPieces {m_pendingPieces.Count}");
        LoggerProvider.LogInfo($"allPieces {m_allPieces.Count}");
      }

      InitializeBasePiecesControllerOverrides();

      if (Manager != null &&
          !ActiveInstances.ContainsKey(Manager.PersistentZdoId))
        ActiveInstances.Add(Manager.PersistentZdoId, this);

      StartClientServerUpdaters();
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

    public override void OnDisable()
    {
      // hopefully it can be run otherwise it needs to be patched so it can run just before objects are being cleaned up in a zone.
      ForceUpdateAllPiecePositions();

      base.OnDisable();

      _isInvalid = true;

      if (MonoUpdaterInstances.Contains(this)) MonoUpdaterInstances.Remove(this);

      if (Manager != null &&
          ActiveInstances.GetValueSafe(Manager.PersistentZdoId))
        ActiveInstances.Remove(Manager.PersistentZdoId);

      InitializationTimer.Stop();
      if (_serverUpdatePiecesCoroutine != null)
        StopCoroutine(_serverUpdatePiecesCoroutine);


      // probably safer to just do this.
      StopAllCoroutines();

      CleanUp();
    }

    public void CustomUpdate(float deltaTime, float time)
    {
      Client_UpdateAllPieces();
    }

    public override void CustomFixedUpdate(float deltaTime)
    {
      if (m_nview == null) return;
      Sync();
    }

    public void CustomLateUpdate(float deltaTime)
    {
      if (ZNet.instance == null || ZNet.instance.IsServer()) return;
      Client_UpdateAllPieces();
      UpdateBedPieces();
    }

    public ZNetView? GetNetView()
    {
      return m_nview;
    }

    public Transform GetPieceContainer()
    {
      return _piecesContainerTransform != null ? _piecesContainerTransform : transform;
    }

    public int GetPersistentId()
    {
      return PersistentZdoId;
    }

    public void RemoveEffectAreaFromVehicle(ZNetView? netView)
    {
      if (netView == null || netView.m_zdo == null) return;
      m_vehicleBurningEffectAreas.Remove(netView.m_zdo.m_uid);
    }

    public void InitSwivelController(SwivelComponentBridge swivelComponentBridge)
    {
      swivelComponentBridge.m_vehiclePiecesController = this;
      swivelComponentBridge.StartActivatePendingSwivelPieces();
    }

    public void RemovePiece(ZNetView netView)
    {
      if (PrefabNames.IsVehicle(netView.name)) return;
      if (!m_pieces.Remove(netView)) return;

      IncrementPieceRevision();
      UpdateMass(netView, true);

      if (m_prefabPieceDataItems.TryGetValue(netView.gameObject, out var pieceData))
      {
        foreach (var pieceDataAllCollider in pieceData.AllColliders)
        {
          m_prefabPieceColliderToIdMap.Remove(pieceDataAllCollider);
        }
      }

      m_prefabPieceColliderToIdMap.RemoveNullKeys();
      VehicleRamAoe.ColliderVehicleIdMap.RemoveNullKeys();

      OnPieceRemoved(netView.gameObject);

      if (PrefabNames.IsHull(netView.gameObject)) m_hullPieces.Remove(netView);

      var isRam = RamPrefabs.IsRam(netView.name);
      if (isRam) m_ramPieces.Remove(netView);
    }

    public static CannonballVariant AmmoVariantDefault = CannonballVariant.Solid;


    public void AddPieceDataForComponents(ZNetView netView)
    {
      var components = netView.GetComponents<Component>();
      if (components == null) return;
      if (components.Length == 0) return;
      foreach (var component in components)
      {
        if (component == null) continue;
        switch (component)
        {
          case VehicleManager vehicleManager:
            LoggerProvider.LogDev("Detected VehicleManager, setting parent to PiecesController.Manager");
            vehicleManager.MovementController.OnParentReady(PiecesController.Manager);
            break;
          case ShieldGenerator shieldGenerator:
            m_shieldGenerators.Add(shieldGenerator);
            break;
          case TargetControlsInteractive prefabTargetControls:
            prefabTargetControls.targetController = targetController;
            break;
          case TargetController prefabTargetController:
            Destroy(prefabTargetController);
            break;
          case CannonController cannonController:
            targetController.AddCannon(cannonController);
            cannonController.AddIgnoredTransforms([transform, Manager!.transform]);
            break;
          case SwivelComponentBridge swivelController:
            InitSwivelController(swivelController);
            break;
          case WearNTear wnt when PrefabConfig.MakeAllPiecesWaterProof.Value:
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
          {
            m_anchorMechanismComponents.Add(anchorMechanismController);
            InitAnchorComponent(anchorMechanismController);
            m_anchorPieces.Add(netView);
            if (MovementController != null)
            {
              MovementController.UpdateAnchorCapabilities();
              anchorMechanismController.UpdateAnchorState(MovementController
                .vehicleAnchorState, VehicleAnchorMechanismController.GetCurrentStateTextStatic(MovementController.vehicleAnchorState, Manager != null && Manager.IsLandVehicle));
            }
            break;
          }
          case RudderComponent rudder:
            m_rudderPieces.Add(rudder);
            SetShipWakeBounds();
            break;
          case RopeAnchorComponent ropeAnchor:
            if (ropeAnchor.IsDockAnchor())
            {
              OnAddUniquePieceDestroyPrevious(m_dockAnchor);
              m_dockAnchor = ropeAnchor;
            }
            break;
          case SteeringWheelComponent wheel:
            OnAddUniquePieceDestroyPrevious(_steeringWheelPiece);
            _steeringWheelPiece = wheel;
            RotateVehicleForwardPosition();

            wheel.InitializeControls(netView, Manager);
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
    }

    public void RemovePieceDataForComponents(ZNetView netView)
    {
      // Avoid invalid memory access
      if (!netView || !netView.gameObject || !netView.gameObject.activeInHierarchy)
      {
        return;
      }

      var components = netView.GetComponents<Component>();

      if (components == null) return;
      foreach (var component in components)
      {
        if (component == null) continue;
        switch (component)
        {
          case SailComponent sail:
            m_sailPieces.Remove(sail);
            break;
          case Fireplace fireplace:
            RemoveEffectAreaFromVehicle(netView);
            break;
          case ShieldGenerator shieldGenerator:
            m_shieldGenerators.Add(shieldGenerator);
            break;
          case CannonController cannonController:
            targetController.RemoveCannon(cannonController);
            break;
          case MastComponent mast:
            m_mastPieces.Remove(mast);
            break;
          case RudderComponent rudder:
          {
            m_rudderPieces.Remove(rudder);
            if (Manager && m_rudderPieces.Count > 0)
              SetShipWakeBounds();
            break;
          }
          case RopeAnchorComponent ropeAnchor:
            if (ropeAnchor.IsDockAnchor())
            {
              m_dockAnchor = null;
            }
            break;
          case SteeringWheelComponent wheel:
            _steeringWheelPiece = null;
            if (Manager)
            {
              VehicleMovementController.RemoveAllShipControls(Manager.MovementController);
            }
            break;
          case Bed bed:
            m_bedPieces.Remove(bed);
            break;
          case VehicleAnchorMechanismController anchorMechanismController:
          {
            m_anchorMechanismComponents.Remove(anchorMechanismController);
            m_anchorPieces.Remove(netView);

            if (MovementController != null)
            {
              MovementController.UpdateAnchorCapabilities();
            }
            break;
          }
          case BoardingRampComponent ramp:
            m_boardingRamps.Remove(ramp);
            break;
          case TeleportWorld portal:
            m_portals.Remove(netView);
            break;
          case RopeLadderComponent ladder:
            m_ladders.Remove(ladder);
            ladder.vehiclePiecesController = null;
            break;
        }
      }
    }


    /**
     * prevent ship destruction on m_nview null
     * - if null it would prevent getting the ZDO information for the ship pieces
     */
    public void DestroyPiece(WearNTear wnt)
    {
      if (IsInvalid()) return;
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

      if (pieceCount > 0 || m_nview == null) return;
      if (Manager == null) return;

      var wntShip = Manager.GetComponent<WearNTear>();
      if ((bool)wntShip) wntShip.Destroy();
    }

    public static bool CanRemoveRigidbodyFromChild(string name)
    {
      return !RamPrefabs.IsRam(name) &&
             !name.Contains(PrefabNames.ShipAnchorWood) && !PrefabNames.IsVehicle(name) && !PrefabNames.IsVehiclePiecesContainer(name) && !name.StartsWith(PrefabNames.SwivelPrefabName);
    }

    public void AddPiece(ZNetView netView, bool isNew = false)
    {
      if (IsInvalid()) return;
      if (!(bool)netView)
      {
        LoggerProvider.LogError("netView does not exist but somehow called AddPiece()");
        return;
      }

      // incrementRevision
      IncrementPieceRevision();
      PieceActivatorHelpers.FixPieceMeshes(netView);
      ResetSailCachedValues();
      OnPieceAdded(netView.gameObject);

      // todo onPieceAdded SHOULD DO this.
      OnAddPieceIgnoreColliders(netView);

      m_pieces.Add(netView);
      UpdatePieceCount();

      AddPieceDataForComponents(netView);


      if (RamPrefabs.IsRam(netView.name))
      {
        m_ramPieces.Add(netView);
        var vehicleRamAoe = netView.GetComponentInChildren<VehicleRamAoe>();
        if (vehicleRamAoe != null)
          vehicleRamAoe.m_vehicle = Manager;
      }

      // Remove non-kinematic rigidbodies if not a ram
      if (CanRemoveRigidbodyFromChild(netView.name))
      {
        var rbs = netView.GetComponentsInChildren<Rigidbody>();
        foreach (var rbsItem in rbs)
          if (!rbsItem.isKinematic && rbsItem != m_localRigidbody || rbsItem != m_syncRigidbody)
          {
            LoggerProvider.LogWarning(
              $"Destroying Rigidbody on netview <{netView.name}> for root object <{rbsItem.transform.root?.name ?? rbsItem.transform.name}>");
            Destroy(rbsItem);
          }
      }


      UpdateMass(netView);

      // Handle bounds rebuilding
      RequestBoundsRebuild();

      if (hasDebug)
        LoggerProvider.LogDebug(
          $"After Adding Piece: {netView.name}, Ship Size calc is: m_bounds {BaseControllerPieceBounds} bounds size {BaseControllerPieceBounds.size}");
    }

    public List<Bed> GetBedPieces()
    {
      return m_bedPieces;
    }

    public Bounds GetVehicleBounds()
    {
      return BaseControllerPieceBounds;
    }

    public List<ZNetView>? GetCurrentPendingPieces()
    {
      var persistentId = GetPersistentId();
      m_pendingPieces.TryGetValue(persistentId,
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
      if (!m_nview || !Manager ||
          !MovementController)
      {
        LoggerProvider.LogDebug(
          $"Vehicle setting state to Pending as it is not ready, must have netview: {Manager.m_nview}, VehicleInstance {Manager}, MovementController {MovementController}");
        BaseVehicleInitState = InitializationState.Pending;
        return;
      }

      var initialized = m_nview?.GetZDO()
        .GetBool(VehicleZdoVars.ZdoKeyBaseVehicleInitState) ?? false;

      BaseVehicleInitState = initialized
        ? InitializationState.Complete
        : InitializationState.Created;
    }

    public void SetInitComplete()
    {
      m_nview?.GetZDO()
        .Set(VehicleZdoVars.ZdoKeyBaseVehicleInitState, true);
      BaseVehicleInitState = InitializationState.Complete;
      IgnoreAllVehicleColliders();
    }


    public void FireErrorOnNull(Collider obj, string name)
    {
      if (obj == null)
        LoggerProvider.LogError(
          $"BaseVehicleError: collider not initialized for <{name}>");
    }

    public void ValidateInitialization()
    {
      // colliders that must be valid
      FireErrorOnNull(FloatCollider, PrefabNames.WaterVehicleFloatCollider);
      FireErrorOnNull(OnboardCollider, PrefabNames.WaterVehicleOnboardCollider);
    }

    /// <summary>
    /// Coroutine to init vehicle just in case things get delayed or desync. This allows for it to wait until things are ready without skipping critical initialization
    /// </summary>
    /// TODO this might need to be removed or more guards need to be added.
    /// <param name="vehicleShip"></param>
    /// <returns></returns>
    private IEnumerator InitVehicle(VehicleManager vehicleShip)
    {
      while (!(m_nview || !MovementController) &&
             InitializationTimer.ElapsedMilliseconds < 5000)
      {
        if (!m_nview || !MovementController)
        {
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
    public void InitFromShip()
    {
      if (Manager == null)
      {
        LoggerProvider.LogError("InitFromShip should have a valid BaseController");
        return;
      }

      var hasInitializedSuccessfully = InitializeBaseVehicleValuesWhenReady(Manager);

      // wait more time if the vehicle is somehow taking forever to init
      if (!hasInitializedSuccessfully)
        StartCoroutine(InitVehicle(Manager));
    }

    private IEnumerator ZdoReadyStart()
    {
      if (Manager != null)
      {
        InitializeBaseVehicleValuesWhenReady(Manager);
      }

      if (Manager == null)
        LoggerProvider.LogError(
          "No ShipInstance detected");

      yield return null;
    }

    /// <summary>
    /// Gets the RaycastPieceActivator which is used for Swivels and VehiclePiecesController components. These components are responsible for activation and parenting of vehicle pieces and will always exist above the current piece in transform hierarchy.
    /// </summary>
    public static VehiclePiecesController? GetVehiclePiecesController(
      GameObject obj)
    {
      var controller = obj.GetComponentInParent<VehiclePiecesController>();
      return controller;
    }

    /// <summary>
    /// Currently only exist within the top level transform but this may change so there is a getter
    /// </summary>
    /// <returns></returns>
    public Transform GetPiecesContainer()
    {
      return transform;
    }

    private Transform CreateMovingPiecesContainer()
    {
      if (_movingPiecesContainerTransform) return _movingPiecesContainerObj.transform;

      var mpc = new GameObject
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
          (m) => LoggerProvider.LogDebug(m));

        // instance
        convexHullComponent.PreviewParent = transform;
        convexHullComponent.transformPreviewOffset =
          PhysicsConfig.convexHullPreviewOffset.Value;
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
    private bool InitializeBaseVehicleValuesWhenReady(VehicleManager vehicle)
    {
      if (ZNetView.m_forceDisableInit) return false;
      // vehicleInstance is the persistent ID, the pieceContainer only has a netView for syncing ship position
      if (vehicle.m_nview == null)
      {
        LoggerProvider.LogWarning(
          "Warning netview not detected on vehicle, this means any netview attached events will not bind correctly");
        return false;
      }

      if (vehicle.vehicleMovementCollidersTransform)
      {
        // must set parent to be the vehicle colliders
        convexHullComponent.parentTransform = vehicle.vehicleMovementCollidersTransform;
      }

      LoadInitState();

      if (Manager != null)
      {
        Manager.HideGhostContainer();
      }
      return true;
    }

    public void InitializeBasePiecesControllerOverrides()
    {
      if (m_meshClusterComponent != null)
      {
        m_meshClusterComponent.IgnoreAllVehicleCollidersCallback = IgnoreAllVehicleColliders;
      }
    }

    private void StartClientServerUpdaters()
    {
      if (!(bool)ZNet.instance) return;

      LoggerProvider.LogDebug($"IsDedicated : {ZNet.instance.IsDedicated()}");
      if (ZNet.instance.IsDedicated() && _serverUpdatePiecesCoroutine == null)
      {
        LoggerProvider.LogDebug("Calling UpdatePiecesInEachSectorWorker");
        _serverUpdatePiecesCoroutine =
          StartCoroutine(nameof(UpdatePiecesInEachSectorWorker));
      }

      StartActivatePendingPieces();
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

      if (Manager == null)
        LoggerProvider.LogError("Cleanup called but there is no valid VehicleInstance");

      if (!ZNetScene.instance || PersistentZdoId == null ||
          PersistentZdoId == 0) return;


      for (var index = 0; index < m_pieces.Count; index++)
      {
        if (!m_pieces.TryGetValidElement(ref index, ["m_zdo"], out var piece))
        {
          continue;
        }

        piece.transform.SetParent(null);
        AddInactivePiece(Manager!.PersistentZdoId, piece, null, true);
      }

      // todo might need to do some freezing of positions if these pieces are rigidbodies/physics related such as animals and npcs.
      for (var index = 0; index < m_tempPieces.Count; index++)
      {
        if (!m_tempPieces.TryGetValidElement(ref index, ["m_zdo"], out var tempPiece))
        {
          continue;
        }
        // we must update the position as these pieces/characters can move while on Vehicles.
        // todo we may wan to not update any values for temp pieces as this removeall process could be inaccurate and set a location way outside expected range.
        // tempPiece.m_zdo.Set(VehicleZdoVars.MBPositionHash, tempPiece.transform.localPosition);
        tempPiece.transform.SetParent(null);
      }
    }

    public void SyncRigidbodyStats(float drag, float angularDrag,
      bool flight)
    {
      if (!isActiveAndEnabled) return;
      if (MovementController?.m_body == null || m_statsOverride ||
          !Manager || !m_localRigidbody)
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

    public void SetVehiclePhysicsType(
      VehiclePhysicsMode physicsMode)
    {
      localVehiclePhysicsMode = physicsMode;
    }

    /// <summary>
    /// Adds a leaning effect similar to sailing when wind is starboard/port pushing upwards/downwards. Cosmetic only to the ship. SailPower controls lean effect. Zero sails will not influence it.
    /// </summary>
    /// <returns></returns>
    public Quaternion GetRotationWithLean()
    {
      if (MovementController == null || m_nview == null)
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

      var rotation = GetRotationWithLean();
      var position = MovementController!.m_body.position;
      m_localRigidbody.Move(
        position,
        rotation
      );

    }

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

    /// <summary>
    /// Todo add a Coroutine to evalutate this. If there is an error in FixedUpdate set this flag to false. Then do a full evaluation similar to MovementController
    /// </summary>
    /// <returns></returns>
    public bool IsInvalid()
    {
      var isInvalid = !isActiveAndEnabled || m_localRigidbody == null ||
                      Manager == null ||
                      Manager.m_nview == null ||
                      MovementController == null ||
                      MovementController.rigidbody == null || !this.IsNetViewValid();
      _isInvalid = isInvalid;

      return _isInvalid;
    }

    private void UpdateBedPiece(Bed mBedPiece)
    {
      var bedNetView = mBedPiece.m_nview;
      if (!bedNetView) return;
      bedNetView.GetZDO()?.SetPosition(mBedPiece.m_nview.transform.position);
    }

    /// <summary>
    /// BedPieces are not kept in the raft ball, so that a bed is always placed in the correct area if a player must spawn in it.
    /// </summary>
    public void UpdateBedPieces()
    {
      if (!CanBedsUseActualWorldPosition) return;
      foreach (var mBedPiece in m_bedPieces) UpdateBedPiece(mBedPiece);
    }

    /// <summary>
    /// For local updates use position of current transform.
    /// </summary>
    public void ForceUpdateAllPiecePositions()
    {
      ForceUpdateAllPiecePositions(transform.position);
    }

    /// <summary>
    /// This should only be called directly in cases of force moving the vehicle with a command
    /// </summary>
    public void ForceUpdateAllPiecePositions(Vector3 vehiclePosition)
    {
      if (!Manager.IsInitialized) return;
      if (Manager.isCreative) return;

      Physics.SyncTransforms();

      if (!isActiveAndEnabled || !this.IsNetViewValid(out var netView)) return;
      // use center of rigidbody to set position.
      var rootNvZdo = netView.GetZDO();
      rootNvZdo.SetPosition(vehiclePosition);

      for (var index = 0; index < m_pieces.Count; index++)
      {
        var nv = m_pieces[index];
        if (!nv || !nv.IsValid())
        {
          LoggerProvider.LogDebugDebounced(
            $"Null netview found with m_pieces: netview, safe removing the piece");
          m_pieces.FastRemoveAt(ref index);
          continue;
        }

        var hasPrefabPieceData = m_prefabPieceDataItems.TryGetValue(nv.gameObject, out var prefabPieceData);

        if (hasPrefabPieceData)
        {
          if (prefabPieceData.IsBed)
          {
            UpdatePieceZdoPosition(nv.GetZDO(), vehiclePosition, prefabPieceData.IsBed);
            continue;
          }
          if (prefabPieceData.IsSwivelChild && TrySetSwivelPiecePosition(nv))
          {
            continue;
          }
        }

        // updates the zdo for the current location of the piece.
        nv.GetZDO()?.SetPosition(CanUseActualPiecePosition ? nv.transform.position : vehiclePosition);
      }
      var convexHullBounds = convexHullComponent.GetConvexHullBounds(false);

      // Removes the temp collider from the parent if not within the parent.
      for (var index = 0; index < m_tempPieces.Count; index++)
      {
        var nv = m_tempPieces[index];
        if (!nv)
        {
          m_tempPieces.FastRemoveAt(ref index);
          continue;
        }

        var combinedBounds = GetCombinedColliderBoundsInPiece(nv.gameObject);

        // todo handle edge cases like if the temp piece is very close the vehicle.
        if (!convexHullBounds.Intersects(combinedBounds))
        {
          // do not remove the piece otherwise it mutates the current list.
          RemoveTempPiece(nv, false);
          m_tempPieces.FastRemoveAt(ref index);
          continue;
        }

        // should always use actual position as this is a moving object that might have a ZsyncTransform.
        nv.m_zdo?.SetPosition(nv.transform.position);
      }
    }

    public bool TrySetSwivelPiecePosition(ZNetView nv)
    {
      var swivel = nv.GetComponentInParent<SwivelComponentBridge>();
      if (swivel != null)
      {
        var zdo = nv.GetZDO();
        if (zdo != null)
        {

          UpdatePieceZdoPosition(zdo, swivel.transform.position);
        }
        return true;
      }
      return false;
    }

    /**
     * @warning this must only be called on the client
     */
    public void Client_UpdateAllPieces()
    {
      if (IsInvalid()) return;
      var sector = ZoneSystem.GetZone(transform.position);

      if (sector == m_sector)
      {
        if (VehicleGlobalConfig.ForceShipOwnerUpdatePerFrame.Value)
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
      return m_nview?.GetZDO()?.IsOwner() ?? false;
    }

    /// <summary>
    /// Ran locally in singleplayer or on the machine that owns the netview or on the server.
    /// </summary>
    public void Server_SyncAllPieces()
    {
      if (!ZNet.instance) return;

      // jotunn extension for Server + Dedicated check. May not be required as IsDedicated may not be true on clients.
      var isDedicatedServer = ZNet.instance.IsServerInstance();

      LoggerProvider.LogDev($"IsDedicatedServer : {isDedicatedServer}, isServer: {ZNet.instance.IsServer()} isDedicated {ZNet.instance.IsDedicated()}");

      if (!isDedicatedServer) return;

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

        var id = zdo.GetInt(VehicleZdoVars.MBParentId);
        if (id != PersistentZdoId)
        {
          list.FastRemoveAt(ref i);
          continue;
        }

        UpdatePieceZdoPosition(zdo, pos);
      }
    }

    /// <summary>
    /// Sets position based on a few flags.
    /// - CanUseActualPiecePosition is experimental will likely cause problems, but it aligns with other piece setting values. Allows for compatibility with mods like Planbuild https://github.com/sirskunkalot/PlanBuild
    /// </summary>
    /// <param name="zdo"></param>
    /// <param name="vehiclePosition"></param>
    private void UpdatePieceZdoPosition(ZDO zdo, Vector3 vehiclePosition, bool isBed = false)
    {
      if (zdo.m_prefab == PrefabNameHashes.LandVehicle)
      {
        // do not set position for vehicle. Instead keep in sync the relative position of it's parent.
        var newOffset = vehiclePosition - zdo.GetPosition();
        zdo.Set(VehicleZdoVars.MBPositionHash, newOffset);
      }
      else if (isBed && CanBedsUseActualWorldPosition || CanUseActualPiecePosition)
      {
        var pieceOffset = zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        zdo.SetPosition(vehiclePosition + pieceOffset);
      }
      else
      {
        zdo.SetPosition(vehiclePosition);
      }
    }

    private void UpdatePlayers()
    {
      // if (BaseController?.m_players == null) return;
      // var vehiclePlayers = BaseController.m_players;
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
      LoggerProvider.LogDebug("called UpdatePiecesWorker");
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
      LoggerProvider.LogMessage("UpdatePiecesInEachSectorWorker started");
      while (isActiveAndEnabled)
      {
        if (!m_nview)
          yield return new WaitUntil(() => Manager != null && Manager.m_nview != null);

        var output =
          m_allPieces.TryGetValue(Manager.PersistentZdoId, out var list);
        if (list == null || !output)
        {
          yield return new WaitForSeconds(Math.Max(
            ModEnvironment.IsDebug ? 0.05f : 0.1f,
            VehicleGlobalConfig.ServerRaftUpdateZoneInterval
              .Value));
          continue;
        }

        yield return UpdatePiecesWorker(list);
        yield return new WaitForFixedUpdate();
      }

      LoggerProvider.LogMessage("UpdatePiecesInEachSectorWorker finished");
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
        if (activeInstance && !activeInstance.IsInvalid())
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
      if (!ValheimExtensions.IsCurrentGameHealthy())
      {
        return;
      }

      if (hasDebug)
        LoggerProvider.LogDebug($"addInactivePiece called with {id} for {netView.name}");

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

      if (!skipActivation && instance != null && ZNet.instance != null && !instance.IsInvalid())
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
        if (hasDebug) LoggerProvider.LogDebug("NetView is invalid skipping mass update");
        return;
      }

      var piece = netView.GetComponent<Piece>();
      if (!piece)
      {
        if (hasDebug)
          LoggerProvider.LogDebug(
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
          LoggerProvider.LogDebug(
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

      if (pieceName.StartsWith(PrefabNames.HullRib_BaseName))
      {
        if (pieceName.Contains(HullMaterial.Iron)) return 720f;

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

    public void RemovePlayersFromBoat()
    {
      try
      {
        var players = Player.GetAllPlayers();
        foreach (var t in players.Where(t =>
                   (bool)t && t.transform.parent == transform))
          t.transform.SetParent(null);
      }
      catch (Exception e)
      {
        LoggerProvider.LogError("Error while removing player from boat.");
      }
    }

    public void DestroyVehicle()
    {
      var wntVehicle = GetComponent<WearNTear>();

      RemovePlayersFromBoat();

      if (!CanDestroyVehicle(m_nview)) return;

      if ((bool)wntVehicle)
        wntVehicle.Destroy();
      else if (gameObject) Destroy(gameObject);
    }

    public void AddPendingPieceToActiveVehicle(int vehicleId, ZNetView piece,
      bool skipActivation = false)
    {
      PendingPiecesState = PendingPieceStateEnum.Scheduled;
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
        LoggerProvider.LogDebug(
          $"ActivatePendingPiecesCoroutine(): pendingPieces count: {m_pendingPieces.Count}");
      if (!CanActivatePendingPieces) return;

      if (Manager == null) return;

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
      PendingPiecesState = pieceStateEnum;
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
        LoggerProvider.LogWarning(
          $"ActivatePendingPieces did not complete correctly. Reason: {message}");
    }

    public void OnStartActivatePendingPieces()
    {
      PendingPiecesState = PendingPieceStateEnum.Running;
      PendingPiecesTimer.Restart();
    }

    public IEnumerator ActivatePendingPiecesCoroutine()
    {
      if (BaseVehicleInitState !=
          InitializationState.Complete)
      {
        _pendingPiecesCoroutine = null;
        yield break;
      }

      OnStartActivatePendingPieces();

      var persistentZdoId = PersistentZdoId;
      if (persistentZdoId == 0)
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

        yield return null;

        if (Manager?.m_nview == null)
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

      ActivateTempPieces();

      OnActivatePendingPiecesComplete(PendingPieceStateEnum.Complete);
    }

    /// <summary>
    /// A bit heavy for iteration, likely better than raycast logic, allows for accurately detecting if in vehicle area. But it could be inaccurate since the point is not technically a part of the pieces list.
    /// </summary>
    /// - Used for fires and other Effects Area logic which requires movement support for static struct of bounds.
    public static bool IsPointWithin(Vector3 p,
      out VehiclePiecesController? controller)
    {
      controller = null;
      foreach (var instance in ActiveInstances.Values)
        if (instance != null && instance.OnboardCollider != null && instance.OnboardCollider.bounds.Contains(p))
        {
          controller = instance;
          return true;
        }

      return false;
    }

    public static bool IsPointWithinEffectsArea(Vector3 p, out EffectArea? effectArea)
    {
      effectArea = null;
      if (!IsPointWithin(p, out var piecesController))
      {
        return false;
      }

      if (piecesController == null) return false;

      var matchingInstance =
        piecesController.m_vehicleBurningEffectAreas.Values.FirstOrDefault(
          x =>
          {
            if (x != null && x.m_collider != null && x.m_collider.bounds.Contains(p)) return true;
            return false;
          });

      if (matchingInstance != null)
      {
        effectArea = matchingInstance;
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
        LoggerProvider.LogDebug("Player source Not active");
        return;
      }

      // todo confirm calling this will do nothing if they do not exist.
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
        LoggerProvider.LogDebug("Player source Not active");
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

      return netView.GetZDO()?.GetInt(VehicleZdoVars.TempPieceParentId) ??
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

      var isLandVehicle = MovementController != null && MovementController.Manager is
      {
        IsLandVehicle: true
      };

      lastAnchorState = anchorState;

      var currentWheelStateText = VehicleAnchorMechanismController.GetCurrentStateTextStatic(anchorState, isLandVehicle);
      foreach (var anchorComponent in m_anchorMechanismComponents)
        if (anchorState != anchorComponent.currentState)
          anchorComponent.UpdateAnchorState(anchorState, currentWheelStateText);

      if (_steeringWheelPiece)
      {
        _steeringWheelPiece.UpdateSteeringHoverMessage(anchorState, currentWheelStateText);
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
          var multiplier = PropulsionConfig.SailTier1Area.Value;
          cachedTotalSailArea += numberOfTier1Sails * multiplier;
        }
        else if (mMastPiece.name.StartsWith(PrefabNames.Tier2RaftMastName))
        {
          ++numberOfTier2Sails;
          var multiplier = PropulsionConfig.SailTier2Area.Value;
          cachedTotalSailArea += numberOfTier2Sails * multiplier;
        }
        else if (mMastPiece.name.StartsWith(PrefabNames.Tier3RaftMastName))
        {
          ++numberOfTier3Sails;
          var multiplier = PropulsionConfig.SailTier3Area.Value;
          cachedTotalSailArea += numberOfTier3Sails * multiplier;
        }
        else if (mMastPiece.name.StartsWith(PrefabNames.Tier4RaftMastName))
        {
          ++numberOfTier4Sails;
          var multiplier = PropulsionConfig.SailTier4Area.Value;
          cachedTotalSailArea += numberOfTier4Sails * multiplier;
        }
      }

      var sailComponents = GetComponentsInChildren<SailComponent>();
      if (sailComponents.Length != 0)
      {
        foreach (var sailComponent in sailComponents)
          if ((bool)sailComponent)
            customSailsArea += sailComponent.GetSailArea();

        if (hasDebug) LoggerProvider.LogDebug($"CustomSailsArea {customSailsArea}");
        var multiplier = PropulsionConfig.SailCustomAreaTier1Multiplier.Value;
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

      // fast remove might not be threadsafe
      list.Remove(zdo);

      itemsRemovedDuringWait = true;
    }

    public static int GetParentID(ZDO zdo)
    {
      var id = zdo.GetInt(VehicleZdoVars.MBParentId);
      if (id == 0)
      {
        var zdoid = zdo.GetZDOID(VehicleZdoVars.MBParentHash);
        if (zdoid != ZDOID.None)
        {
          var zdoparent = ZDOMan.instance.GetZDO(zdoid);
          id = zdoparent == null
            ? ZdoUtils.ZdoIdToId(zdoid)
            : ZdoWatchController.Instance.GetOrCreatePersistentID(zdoparent);
          zdo.Set(VehicleZdoVars.MBParentId, id);
        }
      }

      return id;
    }

    public static bool TryInitTempPiece(ZNetView netView)
    {
      if (netView == null) return false;
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
    /// A Method meant to be called in ActivatePendingPieceCoroutine. This must always be run after all pending ship pieces are activated to allow the vehicle to align properly.
    /// </summary>
    public void ActivateTempPieces()
    {
      if (Manager == null) return;
      if (!m_pendingTempPieces.TryGetValue(Manager.PersistentZdoId, out var pendingTempPiecesList))
      {
        LoggerProvider.LogDebug($"No temp pieces found for vehicle {Manager.PersistentZdoId}");
        return;
      }

      foreach (var activationPieceData in pendingTempPiecesList)
      {
        if (activationPieceData.netView == null) continue;
        if (activationPieceData.vehicleId != Manager.PersistentZdoId)
        {
          LoggerProvider.LogError($"VehicleId of temp piece {activationPieceData.gameObject.name} vehicleId: {activationPieceData.vehicleId} is not equal to currently activating vehicle {Manager.PersistentZdoId}");
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

    public static void RemoveVehicleDataFromZdo(ZDO zdo)
    {
      // main parenting logic.
      zdo.RemoveInt(VehicleZdoVars.MBParentId);

      // this is likely not being used
      zdo.RemoveQuaternion(VehicleZdoVars.MBRotationHash);

      // important for relative position.
      zdo.RemoveVec3(VehicleZdoVars.MBPositionHash);
      zdo.RemoveVec3(VehicleZdoVars.MBRotationVecHash);
    }

    public void ActivatePiece(ZNetView netView)
    {
      if (netView == null) return;
      var zdo = netView.GetZDO();
      if (zdo == null) return;

      if (TryBailOnSameObject(netView.gameObject))
      {
        RemoveVehicleDataFromZdo(netView.m_zdo);
        return;
      }

      TrySetPieceToParent(netView);

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
    /// Overload of AddTemporaryPiece(NetView, bool)
    /// </summary>
    public void AddTemporaryPiece(ActivationPieceData activationPieceData, bool shouldSkipIgnoreColliders = false)
    {
      AddTemporaryPiece(activationPieceData.netView, shouldSkipIgnoreColliders);
    }

    public bool TryBailOnSameObject(GameObject obj)
    {
      if (obj == gameObject || obj == Manager.gameObject) return true;
      return false;
    }

    /// <summary>
    /// For Carts and other rigidbody moveable objects.
    /// </summary>
    public void AddTemporaryPiece(ZNetView netView, bool shouldSkipIgnoreColliders = false)
    {
      if (netView == null) return;
      if (TryBailOnSameObject(netView.gameObject)) return;
      // do not allow adding a piece to itself
      if (netView.transform == transform || netView.transform == Manager.transform) return;

      var zdo = netView.GetZDO();
      if (zdo == null) return;

      var isSwivelParent = SwivelComponentBridge.IsSwivelParent(zdo);

      var character = netView.GetComponent<Character>();
      var shouldSkipAddingProperties = !isSwivelParent && character != null && character.IsPlayer();

      // Guard for cases where we should not add a tempPiece parent or properties but want to run the rest of the logic.
      if (!shouldSkipAddingProperties)
      {
        AddTempPieceProperties(netView, this);
        TrySetPieceToParent(netView);
      }

      OnPieceAdded(netView.gameObject);

      // still need this for flickering but it may cause problems when the object leaves the area.
      // PieceActivatorHelpers.FixPieceMeshes(netView);

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

    public void RemoveTempPiece(ZNetView netView, bool shouldRemoveFromList = true)
    {
      RemoveDynamicParentForVehicle(netView);
      netView.transform.SetParent(null);

      if (shouldRemoveFromList)
      {
        m_tempPieces.Remove(netView);
      }
    }

    public void TrySetPieceToParent(ZNetView? nv)
    {
      // validate current parent
      if (IsInvalid()) return;
      // validate provided netview.
      if (!ValheimExtensions.Internal_IsNetViewValid(nv, out var netView)) return;
      var netViewName = netView.name;
      if (PrefabNames.IsVehicle(netViewName) && netViewName.Contains(PrefabNames.LandVehicle)) return;
      // no overriding parent if sail already has a custom parent EG a mast.
      if (netViewName.StartsWith(PrefabNames.Tier1CustomSailName) && netView.GetZDO().GetInt(SailComponent.SailParentIdHash) != 0)
      {
        if (netView.transform.parent == null)
        {
          var sailComponent = netView.GetComponent<SailComponent>();
          if (!sailComponent) return;
          sailComponent.UpdateSailParent();
        }
        return;
      }
      TrySetPieceToParent(netView.gameObject);
    }

    /// <summary>
    /// Allows directly adding pieces but is by default guarded so only valheim vehicles prefixes are allowed. This is provided the piece has no valid netview otherwise TrySetPieceToParent can be used with a netview and Zdo.
    /// </summary>
    /// <param name="prefab"></param>
    public void TrySetPieceToParent(GameObject prefab, bool isForced = false)
    {
      if (prefab.name.Contains(PrefabNames.ValheimVehiclesPrefix) || isForced)
      {
        prefab.transform.SetParent(_piecesContainerTransform);
      }

      if (prefab.name.StartsWith(PrefabNames.SailCreator) && PatchSharedData.PlayerLastRayPiece != null && PatchSharedData.PlayerLastRayPiece.name.StartsWith(PrefabNames.CustomMast))
      {
        var parentMaskComponent = PatchSharedData.PlayerLastRayPiece.GetComponent<MastComponent>();
        if (!parentMaskComponent || !parentMaskComponent.m_rotationTransform) return;
        prefab.transform.SetParent(parentMaskComponent.m_rotationTransform);
        return;
      }

      if (RamPrefabs.IsRam(prefab.name))
      {
        if (Manager != null && Manager != null)
        {
          prefab.transform.SetParent(Manager.transform);
        }
        return;
      }
      prefab.transform.SetParent(_piecesContainerTransform);
    }

    /**
     * True let's WearNTear destroy this vehicle
     *
     * this could also be used to force a re-render if the user attempts to destroy a raft with pending pieces, might as well run activate pending pieces.
     */
    public static bool CanDestroyVehicle(ZNetView? netView)
    {
      try
      {
        if (netView == null) return false;
        var vehicleController = netView.GetComponent<VehicleManager>();
        if (vehicleController == null || vehicleController.PiecesController == null || vehicleController.PiecesController.IsInvalid())
        {
          LoggerProvider.LogDebug("Bailing vehicle deletion attempt: Valheim Attempted to delete a vehicle that matched the ValheimVehicle prefab instance but there was no VehicleBaseController or VehiclePiecesController found. This could mean there is a mod breaking the vehicle registration of ValheimRAFT.");
          return false;
        }

        var hasPendingPieces =
          m_pendingPieces.TryGetValue(vehicleController.PersistentZdoId,
            out var pendingPieces);
        var hasPieces = vehicleController.PiecesController.GetPieceCount() != 0;

        // if there are pending pieces, do not let vehicle be destroyed
        if (pendingPieces != null && hasPendingPieces && pendingPieces.Count > 0)
          return false;

        return !hasPieces;
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning("A error was thrown in CanDestroyVehicle. This likely means the vehicle is corrupt. This check will protect the vehicle from being deleted.");
        return false;
      }
    }

    public void AddCustomPiece(GameObject prefab, bool isNew = false)
    {
      if (prefab.name.StartsWith(PrefabNames.CustomWaterFloatation))
      {
        AddCustomFloatationPrefab(prefab);
        return;
      }
    }

    public void AddCustomPiece(ZNetView prefab, bool isNew = false)
    {
      if (prefab.name.StartsWith(PrefabNames.CustomWaterFloatation))
      {
        AddCustomFloatationPrefab(prefab.gameObject);
        return;
      }
    }

    /// <summary>
    /// For custom config cubes that are deleted near instantly.
    /// </summary>
    /// <param name="prefab"></param>
    private void AddCustomFloatationPrefab(GameObject prefab)
    {
      if (IsInvalid()) return;
      if (!prefab.name.StartsWith(PrefabNames.CustomWaterFloatation)) return;
      prefab.transform.SetParent(_piecesContainerTransform);

      var isVehicleUsingCustomFloatation = Manager.VehicleConfigSync.GetWaterFloatationHeightMode() == VehicleFloatationMode.Custom;
      var nextState = !isVehicleUsingCustomFloatation;

      Manager.VehicleConfigSync.Request_SyncFloatationMode(nextState, prefab.transform.localPosition.y);

      var stateText = nextState ? ModTranslations.EnabledText : ModTranslations.DisabledText;

      m_hoverFadeText.currentText = $"{ModTranslations.VehicleConfig_CustomFloatationHeight} ({stateText})";
      m_hoverFadeText.transform.position = prefab.transform.position;
      m_hoverFadeText.ResetHoverTimer();
      m_hoverFadeText.Show();
      IgnoreAllVehicleCollidersForGameObjectChildren(prefab);

      // destroy the prefab. It has no use after this call.
      Destroy(prefab);
    }

    public void AddNewPiece(Piece piece)
    {
      if (IsInvalid()) return;
      if (!(bool)piece)
      {
        LoggerProvider.LogError("piece does not exist");
        return;
      }
      if (TryBailOnSameObject(piece.gameObject)) return;
      if (!(bool)piece.m_nview)
      {
        LoggerProvider.LogError("m_nview does not exist on piece");
        return;
      }

      var pieceGo = piece.gameObject;

      if (hasDebug) LoggerProvider.LogDebug("Added new piece is valid");
      AddNewPiece(piece.m_nview);
    }

    public void AddNewPiece(GameObject obj)
    {
      if (TryBailOnSameObject(obj)) return;
      var nv = obj.GetComponent<ZNetView>();
      if (nv == null) return;
      AddNewPiece(nv);
    }

    public void AddNewPiece(ZNetView netView)
    {
      if (netView == null)
      {
        LoggerProvider.LogError("netView does not exist");
        return;
      }
      if (TryBailOnSameObject(netView.gameObject)) return;
      var zdo = netView.GetZDO();
      if (zdo == null)
      {
        LoggerProvider.LogError($"NetView <{netView.name}> has no valid ZDO returning");
        return;
      }

      if (zdo.m_prefab == PrefabNameHashes.WaterVehicleShip)
      {
        LoggerProvider.LogWarning($"Attempted to add a piece to a water vehicle. This is not allowed. NetView <{netView.name}>");
        return;
      }

      if (zdo.m_prefab == PrefabNameHashes.LandVehicle)
      {
        if (PiecesController.Manager.IsLandVehicle)
        {
          LoggerProvider.LogWarning($"Attempted to add a nested landvehicle to another land vehicle. This is not allowed. NetView <{netView.name}>");
          return;
        }
#if DEBUG
        LoggerProvider.LogDev("Detected a nested landvehicle within a WaterVehicle. This is supported but could be unstable.");
#endif
      }

      var previousCount = GetPieceCount();

      if (m_pieces.Contains(netView))
      {
        LoggerProvider.LogDev($"NetView already is added. name: {netView.name}");
      }
      else
      {
        TrySetPieceToParent(netView);
      }

      if (netView.m_zdo != null && netView.m_persistent)
      {
        if (PersistentZdoId != null)
          netView.m_zdo.Set(VehicleZdoVars.MBParentId,
            Manager.PersistentZdoId);
        else
          // We should not reach this, but this would be a critical issue and should be tracked.
          LoggerProvider.LogError(
            "Potential update error detected: Ship parent ZDO is invalid but added a Piece to the ship");

        netView.m_zdo.Set(VehicleZdoVars.MBRotationVecHash,
          netView.transform.localRotation.eulerAngles);
        netView.m_zdo.Set(VehicleZdoVars.MBPositionHash,
          transform.InverseTransformPoint(netView.transform.position));
      }

      PieceOverlapUtils.TryResolveCoplanarityOnPlacement(
        netView.gameObject,
        go =>
        {
          var goPiece = go.GetComponentInParent<Piece>();
          if (goPiece) return goPiece.gameObject;
          var rootGo = go.transform.root.gameObject;
          LoggerProvider.LogWarning($"Could not find root piece for go {go.name} using root transform {rootGo.name}");
          return rootGo;
        },
        0.001f,
        16,
        LayerHelpers.PieceLayerMask,
        (prefabRoot, pos) =>
        {
          if (!prefabRoot || !prefabRoot.GetZDO(out var zdo)) return;
          zdo.SetPosition(pos);

          // the assumption is this will be true. Fire an error when it's not.
          var hasMBParent = zdo.GetInt(VehicleZdoVars.MBParentId, 0) != 0;
          if (hasMBParent)
          {
            var positionOffset = transform.InverseTransformPoint(pos);
            zdo.Set(VehicleZdoVars.MBPositionHash, positionOffset);
          }
          else
          {
            LoggerProvider.LogError($"Could not find parent ZDOID when fixing overlap of piece {prefabRoot.name}");
          }
        }
      );

      AddPiece(netView, true);
      InitZdo(zdo);

      if (previousCount == 0 && GetPieceCount() == 1) SetInitComplete();
    }

    public void OnAddUniquePieceDestroyPrevious(MonoBehaviour? component)
    {
      if (component == null) return;
      var netView = component.GetComponent<ZNetView>();
      if (netView == null) return;

      var wnt = netView.GetComponent<WearNTear>();
      if (wnt != null)
      {
        wnt.Destroy();
      }
    }

    /// <summary>
    /// We can actually assume all "fireplace" components will have the EffectArea nested in them. So doing a query on them is actually pretty efficient. Name checks are more prone to breaks or mod incompatibility, so skipping this.
    /// </summary>
    /// <param name="netView"></param>
    public void AddFireEffectAreaComponent(ZNetView netView)
    {
      var effectAreaItems = netView.GetComponentsInChildren<EffectArea>();
      foreach (var effectAreaItem in effectAreaItems)
      {
        // player base burning type is for all fireplace fires. At least in >=0.219
        if (effectAreaItem.m_type != EffectArea.Type.PlayerBase) continue;
        m_vehicleBurningEffectAreas.Add(netView.m_zdo.m_uid,
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

#if DEBUG
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
#endif
    }

    public void IncrementPieceRevision()
    {
      _lastPieceRevision += 1;
    }


    private void UpdatePieceCount()
    {
      if (IsInvalid()) return;
      if (m_nview != null &&
          m_nview.m_zdo != null)
        m_nview.m_zdo.Set(VehicleZdoVars.MBPieceCount,
          m_pieces.Count);
    }


// for increasing ship wake size.
    private void SetShipWakeBounds()
    {
      if (Manager?.ShipEffectsObj == null) return;

      var firstRudder = m_rudderPieces.First();
      if (firstRudder == null)
      {
        var bounds = FloatCollider.bounds;
        Manager.ShipEffectsObj.transform.localPosition =
          new Vector3(FloatCollider.transform.localPosition.x,
            bounds.center.y,
            bounds.min.z);
        return;
      }

      var localPosition = firstRudder.transform.localPosition;
      Manager.ShipEffectsObj.transform.localPosition =
        new Vector3(
          localPosition.x,
          FloatCollider.bounds.center.y,
          localPosition.z);
    }

    private float GetVehicleFloatHeight()
    {
      if (Manager == null) return 0f;
      _pendingHullBounds = new Bounds();

      var totalHeight = 0f;

      Manager.VehicleConfigSync.Load([VehicleCustomConfig.Key_CustomFloatationHeight, VehicleCustomConfig.Key_HasCustomFloatationHeight]);
      var hullFloatationMode = Manager.VehicleConfigSync.GetWaterFloatationHeightMode();

      var isAverageOfPieces = hullFloatationMode ==
                              VehicleFloatationMode.AverageOfHullPieces;
      var items = isAverageOfPieces ? m_hullPieces : m_pieces;

      foreach (var piece in items)
      {
        if (piece == null)
        {
          LoggerProvider.LogDev("Piece is null during GetVehicleFloatHeight calc.");
          continue;
        }
        var newBounds = EncapsulateColliders(BaseControllerHullBounds.center,
          BaseControllerHullBounds.size,
          piece.gameObject);
        totalHeight += piece.transform.localPosition.y;
        if (newBounds == null) continue;
        _pendingHullBounds = newBounds.Value;
      }

      BaseControllerHullBounds = _pendingHullBounds;

      switch (hullFloatationMode)
      {
        case VehicleFloatationMode.AverageOfHullPieces:
        case VehicleFloatationMode.Average:
          var hullPieceCount =
            isAverageOfPieces
              ? m_hullPieces.Count
              : m_pieces.Count;

          if (Mathf.Approximately(totalHeight, 0f) ||
              Mathf.Approximately(hullPieceCount, 0f))
            return BaseControllerHullBounds.center.y;

          return totalHeight / hullPieceCount;
        case VehicleFloatationMode.Fixed:
          return HullFloatationColliderAlignmentOffset;
        case VehicleFloatationMode.Custom:
          return Manager.VehicleConfigSync.Config.CustomFloatationHeight;
        case VehicleFloatationMode.Center:
        default:
          return BaseControllerHullBounds.center.y;
      }
    }

    /**
     * Must fire RebuildBounds after doing this otherwise colliders will not have the correct x z axis when rotating the y
     */
    private void RotateVehicleForwardPosition()
    {
      if (MovementController == null) return;

      if (_steeringWheelPiece == null || !_steeringWheelPiece.enabled) return;

      // forces wheel to always be direction of landvehicle.
      if (Manager.IsLandVehicle)
      {
        _steeringWheelPiece.transform.localRotation = Quaternion.identity;
        var nv = _steeringWheelPiece.GetComponent<ZNetView>();
        if (!nv || nv.GetZDO() == null) return;
        _steeringWheelPiece.GetComponent<ZNetView>().GetZDO().SetRotation(Quaternion.identity);
        return;
      }

      MovementController.UpdateShipDirection(
        _steeringWheelPiece.transform
          .localRotation);

      if (targetController)
      {
        targetController.RecalculateCannonGroups();
      }
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
      if (Manager == null || MovementController == null) return;

      // ✅ Clear before refilling to avoid stale data

      // ✅ Fetch colliders using `List<>` (HashSet not allowed in GetComponentsInChildren)
      // if (_shouldUpdateVehicleColliders)
      // {
      //   tempVehicleColliders.Clear();
      //   BaseController.GetComponentsInChildren(true, tempVehicleColliders);
      // }
      //
      // if (_shouldUpdatePieceColliders)
      // {
      //   tempPieceColliders.Clear();
      //   GetComponentsInChildren(true, tempPieceColliders);
      // }

      var vehicleColliders = Manager.GetComponentsInChildren<Collider>(true);
      if (vehicleColliders == null) return;

      foreach (var prefabPieceDataItem in m_prefabPieceDataItems.Values)
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
        if (vehicleCollider == null) continue;
        // must ignore all vehicle colliders
        foreach (var vehicleCollider2 in vehicleColliders)
        {
          if (vehicleCollider2 == null) continue;
          if (vehicleCollider == vehicleCollider2) continue;
          Physics.IgnoreCollision(vehicleCollider, vehicleCollider2, true);
        }
      }
    }
    public List<Collider> allVehicleColliders = new();

    // For when pieces are added
    // extremely unoptimized and allocates. But should work without complex logic managing removals.
    public void IgnoreAllVehicleCollidersForGameObjectChildren(GameObject gameObject)
    {
      var colliders = gameObject.GetComponentsInChildren<Collider>(true);

      // characters should not skip hitting treads. If this happens we would have to track them so they do not fall on the treads and go through them when exiting vehicle after first time on vehicle.
      var character = gameObject.GetComponentInParent<Character>();
      var isCharacter = character != null;
      if (!colliders.Any()) return;
      if (Manager == null) return;

      Manager.GetComponentsInChildren<Collider>(true, allVehicleColliders);
      foreach (var collider in colliders)
      {
        m_prefabPieceColliderToIdMap[collider] = PersistentZdoId;
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

      allVehicleColliders.Clear();
    }

    public void TryAddRamToVehicle()
    {
      if (MovementController == null) return;
      MovementController.TryAddRamAoeToVehicle();
    }

    public void ForceRebuildBounds()
    {
      if (_rebuildBoundsRoutineInstance != null)
      {
        StopCoroutine(_rebuildBoundsRoutineInstance);
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
        LoggerProvider.LogError($"{e}");
      }
    }


    public void UpdateTrackedColliders()
    {
      // Update tracked VehicleColliders to ignore.
      Manager.GetComponentsInChildren(vehicleCollidersToIgnore);
    }

    /// <summary>
    /// A complete override of OnConvexHullGenerated.
    /// </summary>
    public override void OnConvexHullGenerated(bool hasSucceeded)
    {
      if (!hasSucceeded)
      {
        RequestBoundsRebuild();
        return;
      }

      UpdateTrackedColliders();

      BaseControllerPieceBounds = convexHullComponent.GetConvexHullBounds(true);

      try
      {
        if (LandMovementController != null)
        {
          LandMovementController.Initialize(BaseControllerPieceBounds);
        }
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"{e}");
      }

      if (RenderingConfig.EnableVehicleClusterMeshRendering.Value && m_pieces.Count >= RenderingConfig.ClusterRenderingPieceThreshold.Value)
      {
        try
        {
          var objects = m_pieces.Where(x => x != null).Select(x => x.gameObject).ToArray();
          m_meshClusterComponent.GenerateCombinedMeshes(objects);
        }
        catch (Exception e)
        {
          LoggerProvider.LogError($"{e}");
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

      // ensures the ram colliders have up to data collision maps.
      if (MovementController != null && MovementController.vehicleRam != null)
      {
        VehicleRamAoe.RegisterVehicleColliders(Manager);
        MovementController.vehicleRam.UpdateColliderCache();
      }
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

// todo move this logic to a file that can be tested
// todo compute the float colliderY transform so it aligns with bounds if player builds underneath boat
    public void OnBoundsChangeUpdateShipColliders()
    {
      if (FloatCollider == null || OnboardCollider == null)
      {
        LoggerProvider.LogWarning(
          "Ship colliders updated but the ship was unable to access colliders on ship object. Likely cause is ZoneSystem destroying the ship");
        return;
      }
      var convexHullBounds = convexHullComponent.GetConvexHullBounds(false, transform.position);

      if (convexHullBounds == null)
      {
        LoggerProvider.LogWarning(
          "Cached convexHullBounds is null this is like a problem with collider setup. Make sure to use custom colliders if other settings are not working");
        return;
      }

      /*
       * @description float collider logic
       * - should match all ship colliders at surface level
       * - surface level eventually will change based on weight of ship and if it is sinking
       */
      var vehicleFloatHeight = GetVehicleFloatHeight();
      var floatColliderCenterOffset =
        new Vector3(BaseControllerPieceBounds.center.x, vehicleFloatHeight,
          BaseControllerPieceBounds.center.z);

      var floatColliderSize = new Vector3(
        Mathf.Max(minColliderSize,
          convexHullBounds.size.x),
        originalFloatColliderSize,
        Mathf.Max(minColliderSize,
          convexHullBounds.size.z));

      var onboardColliderCenter =
        new Vector3(BaseControllerPieceBounds.center.x,
          BaseControllerPieceBounds.center.y,
          BaseControllerPieceBounds.center.z);
      const float additionalHeight = 3f;
      onboardColliderCenter.y += additionalHeight / 2f;

      var onboardColliderSize = new Vector3(
        Mathf.Max(minColliderSize, BaseControllerPieceBounds.size.x),
        Mathf.Max(minColliderSize, BaseControllerPieceBounds.size.y),
        Mathf.Max(minColliderSize, BaseControllerPieceBounds.size.z));
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

    public Bounds GetCombinedColliderBoundsInPiece(GameObject netView,
      bool includeInactive = true)
    {
      if (m_prefabPieceDataItems.TryGetValue(netView.gameObject, out var data) && data.ColliderPointData.HasValue)
      {
        return data.ColliderPointData.Value.LocalBounds;
      }
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
        var newBounds =
          EncapsulateColliders(tempBounds.center, tempBounds.size,
            go);
        if (newBounds != null)
          return new Bounds(newBounds.Value.center, newBounds.Value.size);
      }

      return new Bounds(tempBounds.center, tempBounds.size);
    }

  #region IPieceController

    public override int GetPieceCount()
    {
      if (Manager == null || Manager.m_nview == null ||
          Manager.m_nview.m_zdo == null)
        return base.GetPieceCount();

      var count =
        Manager.m_nview.m_zdo.GetInt(VehicleZdoVars.MBPieceCount,
          m_pieces.Count);
      return count;
    }

    public bool CanRaycastHitPiece()
    {
      return true;
    }

    public bool CanDestroy()
    {
      return GetPieceCount() == 0;
    }

  #endregion

  #region IVehicleSharedProperties

    public VehiclePiecesController? PiecesController
    {
      get => this;
      set
      {
        // do nothing.
      }
    }

    public VehicleMovementController? MovementController
    {
      get;
      set;
    }
    public VehicleConfigSyncComponent? VehicleConfigSync
    {
      get;
      set;
    }
    public VehicleOnboardController? OnboardController
    {
      get;
      set;
    }
    public VehicleManager Manager
    {
      get;
      set;
    } = null!;
    public ZNetView? m_nview
    {
      get;
      set;
    }
    public bool IsControllerValid => Manager.IsControllerValid;
    public bool IsInitialized => Manager.IsInitialized;
    public bool IsDestroying => Manager.IsDestroying;

  #endregion

  }