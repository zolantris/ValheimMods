#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using Jotunn.Managers;
  using Registry;
  using UnityEngine;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Enums;
  using ValheimVehicles.SharedScripts.Helpers;
  using ValheimVehicles.SharedScripts.PowerSystem.Compute;
  using static ValheimVehicles.BepInExConfig.PrefabConfig;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.Components;

  /// <summary>
  /// The main initializer for all vehicle components and a way to access all properties of the vehicle.
  /// </summary>
  ///
  public class VehicleManager : MonoBehaviour, IVehicleBaseProperties, IVehicleSharedProperties, IVehicleConfig
  {
    public GameObject RudderObject { get; set; }
    public const float MinimumRigibodyMass = 1000;

    // controller used to disable some behaviors when copying the vehicle to prevent spawning the base prefab piece.
    public static bool CanInitHullPiece = true;

    private int _persistentZdoId;
    public int PersistentZdoId => GetPersistentID();

    public PowerConsumerData? PowerConsumerData { get; set; }

    // The rudder force multiplier applied to the ship speed
    private float _rudderForce = 1f;

    public VehicleCustomConfig Config => VehicleConfigSync.Config;

    public GameObject GhostContainer()
    {
      return TransformUtils.GetOrFindObj(_ghostContainer, gameObject,
        PrefabNames.GhostContainer);
    }


    public VehicleVariant vehicleVariant = VehicleVariant.All;

    public GameObject PiecesContainer()
    {
      return TransformUtils.GetOrFindObj(_piecesContainer,
        transform.parent.gameObject,
        PrefabNames.VehiclePiecesContainer);
    }

    public static readonly Dictionary<int, VehicleManager> VehicleInstances = new();

    private GameObject _piecesContainer;
    private GameObject _ghostContainer;
    private ImpactEffect _impactEffect;
    public float TargetHeight => MovementController?.TargetHeight ?? 0f;
    private bool m_isLandVehicle = false;
    public bool IsLandVehicle => m_isLandVehicle;
    public bool isCreative;

    private BoxCollider m_floatCollider;
    private BoxCollider m_onboardCollider;

    public BoxCollider? FloatCollider
    {
      get => m_floatCollider;
      set => m_floatCollider = value;
    }

    public BoxCollider? OnboardCollider
    {
      get => m_onboardCollider;
      set => m_onboardCollider = value;
    }

    public bool HasVehicleDebugger = false;
    private Coroutine? _validateVehicleCoroutineInstance;

    public void SetCreativeMode(bool val)
    {
      isCreative = val;
      if (MovementController != null)
        MovementController.UpdateShipCreativeModeRotation();
      UpdateShipEffects();
    }


    public GameObject? ShipEffectsObj;
    public VehicleShipEffects? ShipEffects;

  #region IVehicleSharedProperties

    // setters are added here for VehicleManager only
    public bool IsInitialized { get; private set; }
    public bool IsDestroying { get; private set; }
    public bool IsControllerValid { get; private set; }

    public VehicleManager? VehicleParent;
    public int ParentManagerId = 0;

    // 1:many
    public static Dictionary<int, HashSet<int>> VehicleParentToChildrenMap = new();
    // 1:1
    public static Dictionary<int, int> VehicleChildToParentMap = new();

    public static void AddVehicleChildToParentMap(int childManagerId, int parentManagerId)
    {
      if (!VehicleParentToChildrenMap.TryGetValue(parentManagerId, out var childrenIds))
      {
        VehicleParentToChildrenMap[childManagerId] = new HashSet<int> { childManagerId };
      }
      else
      {
        if (!childrenIds.Contains(childManagerId))
        {
          childrenIds.Add(childManagerId);
        }
      }

      // 1:1 always.
      VehicleChildToParentMap[childManagerId] = parentManagerId;
    }

    public static void RemoveVehicleChildToParentMap(int childManagerId, int parentManagerId)
    {
      if (VehicleParentToChildrenMap.TryGetValue(parentManagerId, out var childrenIds))
      {
        if (childrenIds.Contains(childManagerId))
          childrenIds.Remove(childManagerId);
      }
      VehicleChildToParentMap.Remove(parentManagerId);
    }

    public static void AddVehicleParent(VehicleManager parentManager, VehicleManager childManager, bool isForceDocked = false)
    {
      if (parentManager.PiecesController == null || childManager.PiecesController == null) return;

      parentManager.PiecesController.AddNewPiece(childManager.Manager.m_nview);

      var parentId = childManager.m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentId, 0);

      // fails to attach somehow. Bail on failure.
      if (parentId == 0)
      {
        return;
      }

      AddVehicleChildToParentMap(childManager.PersistentZdoId, parentId);

      if (isForceDocked)
      {
        childManager.Config.ForceDocked = true;
        childManager.VehicleConfigSync.Save(childManager.m_nview.GetZDO());
      }
      childManager.ParentManagerId = parentId;

      if (childManager.MovementController != null)
      {
        childManager.MovementController.TryAddOrRemoveFrozenSync();
      }

      if (childManager.PiecesController.m_dockAnchor != null && parentManager.PiecesController.m_dockAnchor != null)
      {
        childManager.PiecesController.m_dockAnchor.AddDockPoint(parentManager.PiecesController.m_dockAnchor);
      }
    }

    public static void RemoveVehicleParent(VehicleManager childManager)
    {
      if (childManager == null || childManager.MovementController == null || childManager.PiecesController == null) return;
      var childZdo = childManager.m_nview.GetZDO();
      if (childZdo == null) return;

      var parentId = childZdo.GetInt(VehicleZdoVars.MBParentId, 0);
      VehiclePiecesController.RemoveVehicleDataFromZdo(childZdo);
      RemoveVehicleChildToParentMap(childManager.PersistentZdoId, parentId);

      childManager.Config.ForceDocked = false;
      childManager.MovementController.m_frozenSync = null;
      childManager.VehicleParent = null;

      childManager.MovementController.isWaitingForParentVehicleToBeReady = false;
      childManager.MovementController.TryAddOrRemoveFrozenSync();

      if (childManager.PiecesController.m_dockAnchor != null)
      {
        childManager.PiecesController.m_dockAnchor.RemoveAllRopes();
      }

      childManager.VehicleConfigSync.Save(childZdo);
    }

    public VehicleMovementController? MovementController { get; set; }

    private VehicleConfigSyncComponent _vehicleConfigSync = null!;
    private bool _hasInitPrefabSync = false;

    public VehicleConfigSyncComponent VehicleConfigSync
    {
      get => this.GetOrCache(ref _vehicleConfigSync, ref _hasInitPrefabSync);
      set => _vehicleConfigSync = value;
    }

    public VehicleOnboardController? OnboardController { get; set; }
    public VehicleLandMovementController? LandMovementController { get; set; }

    public VehicleManager Manager
    {
      get => this;
      set
      {
        // do nothing
      }
    }

  #endregion

    public VehiclePiecesController? PiecesController { get; set; }

    public ZNetView m_nview { get; set; }

    public Transform vehicleMovementTransform;
    public Transform vehicleMovementCollidersTransform;

    public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

    public VehicleManager Instance => this;

    private GameObject? _vehiclePiecesContainerInstance;
    private GUIStyle myButtonStyle;

    public Transform m_controlGuiPos { get; set; }

    public Transform ControlGuiPosition
    {
      get => m_controlGuiPos;
      set => m_controlGuiPos = value;
    }

    public Rigidbody? MovementControllerRigidbody => MovementController?.m_body;

    public static GameObject GetVehicleMovingPiecesObj(Transform prefabRoot)
    {
      var obj = prefabRoot.Find("vehicle_moving_pieces");
      return obj.gameObject;
    }

    public static GameObject GetVehiclePiecesObj(Transform prefabRoot)
    {
      var obj = prefabRoot.Find("vehicle_pieces");
      return obj.gameObject;
    }

    public static Transform GetVehicleMovementCollidersTransform(
      Transform prefabRoot)
    {
      return prefabRoot.Find("vehicle_movement/colliders");
    }


    public static Transform GetVehicleMovementTransform(Transform prefabRoot)
    {
      return prefabRoot.Find("vehicle_movement");
    }

    public void UpdateIsControllerValid()
    {
      var isValid = isActiveAndEnabled && !IsDestroying &&
                    IsInitialized && !IsDestroying &&
                    PiecesController != null &&
                    MovementController != null &&
                    OnboardController != null &&
                    VehicleConfigSync != null;

      if (IsLandVehicle && LandMovementController == null)
      {
        isValid = false;
      }

      IsControllerValid = isValid;
    }

    public static Transform GetVehicleMovementDamageColliders(
      Transform prefabRoot)
    {
      return prefabRoot.Find("vehicle_movement/damage_colliders");
    }

    public static void OnAllowFlight(object sender, EventArgs eventArgs)
    {
      foreach (var vehicle in VehicleInstances)
        vehicle.Value?.MovementController?.OnFlightChangePolling();
    }


    private static void UpdateShipSounds(VehicleManager vehicleManager)
    {
      if (vehicleManager == null) return;
      if (vehicleManager.ShipEffects == null) return;
      vehicleManager.ShipEffects.m_inWaterSoundRoot.SetActive(VehicleGlobalConfig.EnableShipInWaterSounds.Value);
      vehicleManager.ShipEffects.m_wakeSoundRoot.SetActive(VehicleGlobalConfig.EnableShipWakeSounds.Value);
      vehicleManager.ShipEffects.m_sailSound.gameObject.SetActive(VehicleGlobalConfig.EnableShipSailSounds.Value);
    }

    private static void UpdateAllShipSounds()
    {
      foreach (var vehicleShip in VehicleInstances)
        UpdateShipSounds(vehicleShip.Value);
    }

    public static void UpdateAllLandMovementControllers()
    {
      foreach (var instance in VehicleInstances.Values)
      {
        instance.UpdateLandMovementControllerProperties();
      }
    }

    public static void UpdateAllShipSounds(object sender, EventArgs eventArgs)
    {
      UpdateAllShipSounds();
    }

    /// <summary>
    /// For Hiding the ghost container after the vehicle has initialized.
    /// </summary>
    public void HideGhostContainer()
    {
      var ghostContainer = GhostContainer();
      if (ghostContainer == null) return;
      ghostContainer.SetActive(false);
    }

    /// <summary>
    /// Unloads the Boat Pieces properly
    /// </summary>
    ///
    /// <description>calling cleanup must be done before Unity starts garbage collecting otherwise positions, ZNetViews and other items may be destroyed</description>
    /// 
    public void UnloadAndDestroyPieceContainer()
    {
      if (_vehiclePiecesContainerInstance == null) return;
      if (PiecesController != null)
      {
        PiecesController.CleanUp();
        PiecesController = null;

        if (_vehiclePiecesContainerInstance != null)
        {
          Destroy(_vehiclePiecesContainerInstance);
          _vehiclePiecesContainerInstance = null;
        }
      }
    }

    private void OnDisable()
    {
      if (_validateVehicleCoroutineInstance != null)
      {
        StopCoroutine(_validateVehicleCoroutineInstance);
        _validateVehicleCoroutineInstance = null;
      }
      UpdateIsControllerValid();
      IsControllerValid = false;
    }

    public void OnDestroy()
    {
      IsDestroying = true;
      UnloadAndDestroyPieceContainer();

      if (PersistentZdoId != 0 && VehicleInstances.ContainsKey(PersistentZdoId))
        VehicleInstances.Remove(PersistentZdoId);

      if (MovementController && MovementController != null)
        Destroy(MovementController.gameObject);
    }

    // updates the vehicle water effects if flying/not flying
    public void UpdateShipEffects()
    {
      if (ShipEffectsObj == null) return;
      ShipEffectsObj.SetActive(!(TargetHeight > 0f || isCreative));
    }

    private int GetPersistentID()
    {
      return PersistentIdHelper.GetPersistentIdFrom(m_nview, ref _persistentZdoId);
    }

    public void OnVehicleConfigChange()
    {
      var shouldForceRebuild = false;
      if (LandMovementController != null)
      {
        if (MovementController != null)
        {
          LandMovementController.forwardDirection = MovementController.ShipDirection;
        }
        LandMovementController.wheelBottomOffset = PhysicsConfig.VehicleLandTreadVerticalOffset.Value;

        if (!Mathf.Approximately(LandMovementController.treadWidthXScale, Config.TreadScaleX) || !Mathf.Approximately(LandMovementController.maxTreadLength, Config.TreadLength) || !Mathf.Approximately(LandMovementController.maxTreadWidth, Config.TreadDistance))
        {
          shouldForceRebuild = true;
          LandMovementController.treadWidthXScale = Config.TreadScaleX;
          LandMovementController.maxTreadLength = Config.TreadLength;
          LandMovementController.maxTreadWidth = Config.TreadDistance;
        }
      }

      // other config here.

      if (Config.HasCustomFloatationHeight && FloatCollider != null)
      {
        if (FloatCollider.transform.localPosition.y != Config.CustomFloatationHeight)
        {
          shouldForceRebuild = true;
          var newPosition = FloatCollider.transform.localPosition;
          newPosition.y = Config.CustomFloatationHeight;

          // for updates floatcollider position.
          FloatCollider.transform.localPosition = newPosition;
        }
      }

      if (PiecesController != null && shouldForceRebuild)
      {
        PiecesController.ForceRebuildBounds();
      }
    }

    private void Awake()
    {
      if (ZNetView.m_forceDisableInit) return;
      m_nview = GetComponent<ZNetView>();
      GetPersistentID();

      // todo figure out why when setting this value on the prefab directly it is inaccurate.
      // has to set itself. Otherwise it is mismatched unless saved as a ZDO value.
      SetVehicleVariant(vehicleVariant);

      VehicleConfigSync = gameObject.GetOrAddComponent<VehicleConfigSyncComponent>();
      VehicleConfigSync.OnLoadSubscriptions += OnVehicleConfigChange;
      // this flag can be updated manually via VehicleCommands.
      HasVehicleDebugger = VehicleDebugConfig.VehicleDebugMenuEnabled.Value;

      vehicleMovementCollidersTransform =
        GetVehicleMovementCollidersTransform(transform);
      vehicleMovementTransform = GetVehicleMovementTransform(transform);

      if (PersistentZdoId == 0)
        Logger.LogWarning("PersistewnZdoId, did not get a zdo from the NetView");

      if (VehicleInstances.ContainsKey(PersistentZdoId))
        Logger.LogDebug("VehicleShip somehow already registered this component");
      else
        VehicleInstances.Add(PersistentZdoId, this);

      if (!m_nview)
      {
        Logger.LogWarning("No NetView but tried to set it before");
        m_nview = GetComponent<ZNetView>();
      }

      vehicleMovementTransform = GetVehicleMovementTransform(transform);
    }

    private bool ShouldRunInitialization()
    {
      return !IsDestroying && MovementController == null || PiecesController == null ||
             OnboardController == null || VehicleConfigSync == null;
    }

    private bool TryGetControllersToBind([NotNullWhen(true)] out List<IVehicleSharedProperties>? controllersToBind)
    {
      var allControllers = new List<IVehicleSharedProperties?>
      {
        PiecesController,
        MovementController,
        OnboardController,
        VehicleConfigSync
      };

      controllersToBind = null;

      var validControllers = new List<IVehicleSharedProperties>();
      foreach (var controller in allControllers)
      {
        if (controller == null)
        {
          return false;
        }
        validControllers.Add(controller);
      }

      controllersToBind = validControllers;

      return true;
    }

    private List<IVehicleSharedProperties> GetControllersToBind()
    {
      var allControllers = new List<IVehicleSharedProperties>
      {
        PiecesController,
        MovementController,
        OnboardController,
        VehicleConfigSync
      };
      return allControllers;
    }

    public bool InitializeData()
    {
      if (!this.IsNetViewValid()) return false;
      Config.Load(m_nview.GetZDO(), Config);
      return true;
    }

    public void InitializeAllComponents()
    {
      if (!this.IsNetViewValid()) return;
      // must have latest data loaded into the Vehicle otherwise config can be inaccurate.
      if (!InitializeData()) return;

      InitializeVehiclePiecesController();
      InitializeMovementController();
      InitializeOnboardController();
      InitializeShipEffects();
      InitializeLandMovementController();
      InitializePowerConsumerData();

      if (!TryGetControllersToBind(out var controllersToBind))
      {
        LoggerProvider.LogError("Error while Initializing components. This likely means ValheimVehicles Mod is broken for this Vehicle Type.");
        return;
      }

      BindAllControllersAndData(controllersToBind);

      if (!IsInitialized) return;

      // For starting the vehicle pieces.
      if (PiecesController != null)
      {
        PiecesController.InitFromShip();
        InitStarterPiece();
      }
      else
      {
        Logger.LogError(
          "InitializeAllComponents somehow failed, PiecesController does not exist");
      }
    }

    public void BindAllControllersAndData(List<IVehicleSharedProperties> controllersToBind)
    {
      // todo get vehicle variant from config before running this so init is not wrong.
      // does not affect any prefabs with hardcoded variants.
      if (!VehicleSharedPropertiesUtils.BindAllControllers(this, controllersToBind, vehicleVariant))
      {
        IsInitialized = false;
        return;
      }

      RebindAllComponents();
      IsDestroying = false;
      IsInitialized = true;
    }

    /// <summary>
    /// TODO might use this instead of getter/setters from VehicleShip shared instance.
    /// </summary>
    public void RebindAllComponents()
    {
      // Init colliders
      if (PiecesController != null)
      {
        FloatCollider = PiecesController.FloatCollider;
        OnboardCollider = PiecesController.OnboardCollider;
      }

      if (MovementController != null && FloatCollider != null)
        MovementController.m_floatcollider = FloatCollider;
    }

    public void InitializeMovementController()
    {
      if (MovementController == null)
      {
        var movementController = GetComponent<VehicleMovementController>();
        if (movementController == null)
          movementController =
            gameObject.AddComponent<VehicleMovementController>();
        MovementController = movementController;
      }

      if (MovementController != null && PiecesController != null)
      {
        MovementController.UpdateAnchorCapabilities();
      }
    }

    public void InitializeShipEffects()
    {
      if (ShipEffectsObj == null)
      {
        ShipEffects = GetComponent<VehicleShipEffects>();
        if (ShipEffects != null) ShipEffectsObj = ShipEffects.gameObject;
      }
    }

    /// <summary>
    /// @Requires MovementController
    /// </summary>
    public void InitializeOnboardController()
    {
      if (PiecesController == null)
      {
        Logger.LogError(
          $"PiecesController {PiecesController} not initialized. We cannot initialize OnboardController without it. The mod is likely unstable. Report this bug.");
        return;
      }

      OnboardController = PiecesController.OnboardCollider
        .gameObject
        .AddComponent<VehicleOnboardController>();
      OnboardController.Manager = this;
    }

    public void UpdateLandMovementControllerProperties(bool shouldSkipLoad = false)
    {
      if (!IsLandVehicle || MovementController == null || LandMovementController == null) return;
      if (LandMovementController.treadsPrefab == null)
      {
        LandMovementController.treadsPrefab = LoadValheimVehicleAssets.TankTreadsSingle;
      }

      if (LandMovementController.wheelPrefab == null)
      {
        LandMovementController.wheelPrefab = LoadValheimVehicleAssets.WheelSingle;
      }

      // very important to add these. We always need a base of 30.
      var additionalTurnRate = Mathf.Lerp(VehicleLandMovementController.defaultTurnAccelerationMultiplier / 4, VehicleLandMovementController.defaultTurnAccelerationMultiplier * 4, Mathf.Clamp01(PropulsionConfig.VehicleLandTurnSpeed.Value));
      VehicleLandMovementController.baseTurnAccelerationMultiplier = additionalTurnRate;

      // sync's forward dir.
      LandMovementController.forwardDirection = MovementController.ShipDirection;

      // protect against infinity loads.
      if (!shouldSkipLoad)
      {
        // config controlled. We must skip the load event otherwise this will infinity loop.
        VehicleConfigSync.Load(null, true);
      }

      // todo move to config control
      LandMovementController.wheelBottomOffset = PhysicsConfig.VehicleLandTreadVerticalOffset.Value;
    }

    public void InitializePowerConsumerData()
    {
      if (!IsLandVehicle) return;
      this.WaitForZNetView(netView =>
      {
        PowerConsumerData = new PowerConsumerData(netView.GetZDO());
      });
    }

    /// <summary>
    /// For land vehicles
    /// </summary>
    public void InitializeLandMovementController()
    {
      if (!IsLandVehicle) return;
      if (LandMovementController == null)
      {
        LandMovementController = gameObject.GetComponent<VehicleLandMovementController>();
        if (LandMovementController == null)
        {
          LandMovementController = gameObject.AddComponent<VehicleLandMovementController>();
        }
      }
      LandMovementController.inputTurnForce = 0;
      LandMovementController.inputMovement = 0;
      UpdateLandMovementControllerProperties();
      if (LandMovementController == null)
        Logger.LogError("Error initializing WheelController");
    }

    public void Start()
    {
      if (HasVehicleDebugger && PiecesController) AddOrRemoveVehicleDebugger();

      // Required for vehicle to update well.
      SetVehicleVariant(vehicleVariant);

      UpdateShipSounds(this);
      UpdateShipEffects();
    }

    public void SetVehicleVariant(VehicleVariant variant)
    {
      vehicleVariant = variant;
      m_isLandVehicle = variant == VehicleVariant.Land;
    }

    private IEnumerator ValidateVehicleCoroutineRoutine()
    {
      while (isActiveAndEnabled)
      {
        if (ShouldRunInitialization())
        {
          InitializeAllComponents();
          yield return null;
        }

        UpdateIsControllerValid();
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForFixedUpdate();
      }

      IsControllerValid = false;
    }

    public void OnEnable()
    {
      // OnEnable should always reset this value.
      IsDestroying = false;
      IsInitialized = false;

      if (!this.IsNetViewValid(out var netView))
      {
        m_nview = GetComponent<ZNetView>();
        netView = m_nview;
      }

      GetPersistentID();

      if (PersistentZdoId != 0 && !VehicleInstances.ContainsKey(PersistentZdoId))
        VehicleInstances.Add(PersistentZdoId, this);

      PhysicUtils.IgnoreAllCollisionsBetweenChildren(transform);

      if (netView)
      {
        InitializeAllComponents();
      }

      if (HasVehicleDebugger && PiecesController != null)
        AddOrRemoveVehicleDebugger();

      _validateVehicleCoroutineInstance = StartCoroutine(ValidateVehicleCoroutineRoutine());
    }

    public void UpdateShipZdoPosition()
    {
      if (!(bool)m_nview || m_nview.GetZDO() == null || m_nview.m_ghost ||
          PiecesController == null ||
          !isActiveAndEnabled) return;
      var position = transform.position;

      var sector = ZoneSystem.GetZone(position);
      var zdo = m_nview.GetZDO();

      zdo.SetPosition(PiecesController.m_localRigidbody.worldCenterOfMass);
      zdo.SetSector(sector);
    }

    private GameObject GetStarterPiece()
    {
      string selectedPrefab;
      if (IsLandVehicle)
        return PrefabManager.Instance.GetPrefab(PrefabNames.GetHullSlabName(
          HullMaterial.Wood,
          PrefabNames.PrefabSizeVariant.FourByFour));

      switch (StartingPiece?.Value)
      {
        case VehicleShipInitPiece.HullFloor2X2:
          selectedPrefab = PrefabNames.GetHullSlabName(
            HullMaterial.Wood,
            PrefabNames.PrefabSizeVariant.TwoByTwo);
          break;
        case VehicleShipInitPiece.HullFloor4X4:
          selectedPrefab = PrefabNames.GetHullSlabName(
            HullMaterial.Wood,
            PrefabNames.PrefabSizeVariant.FourByFour);
          break;
        case VehicleShipInitPiece.Nautilus:
          selectedPrefab = PrefabNames.Nautilus;
          break;
        case VehicleShipInitPiece.WoodFloor2X2:
          selectedPrefab = "wood_floor";
          break;
        case VehicleShipInitPiece.Hull4X8:
        default:
          selectedPrefab = PrefabNames.ShipHullCenterWoodPrefabName;
          break;
      }

      return PrefabManager.Instance.GetPrefab(selectedPrefab);
    }

    /**
     * toggle VehicleShip ability to init pieces
     * todo swap out prefab for a prefab type check similar to vehicle storage api.
     */
    public static VehicleManager? InitWithoutStarterPiece(Transform obj)
    {
      CanInitHullPiece = false;
      try
      {
        var shipPrefab =
          PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);
        var ship = Instantiate(shipPrefab, obj.position,
          obj.rotation, null);

        CanInitHullPiece = true;
        return ship.GetComponent<VehicleManager>();
      }
      catch
      {
        CanInitHullPiece = true;
      }

      return null;
    }

    private void InitStarterPiece()
    {
      if (PiecesController == null) return;
      if (!CanInitHullPiece)
      {
        m_nview.GetZDO().Set(
          VehicleZdoVars.ZdoKeyBaseVehicleInitState, true);
        return;
      }

      var pieceCount = PiecesController.GetPieceCount();
      if (pieceCount != 0) return;

      // Having this value sooner is better
      GetPersistentID();

      if (PiecesController.BaseVehicleInitState !=
          InitializationState.Created)
        return;

      var prefab = GetStarterPiece();
      if (!prefab) return;
      var localTransform = transform;
      if (IsLandVehicle)
      {
        // we use the same alignments of the slabs in the ghost preview
        var slabTransform = transform.Find("ghostContainer/preview_slabs");
        if (slabTransform != null)
        {
          for (var i = 0; i < slabTransform.childCount; i++)
          {
            var slabTopLevelChild = slabTransform.GetChild(i);
            if (slabTopLevelChild == null) continue;
            var hull =
              Instantiate(prefab, slabTopLevelChild.position,
                slabTopLevelChild.rotation, null);
            if (hull == null) return;
            var hullNetView = hull.GetComponent<ZNetView>();
            PiecesController.AddNewPiece(hullNetView);
          }
        }
        else
        {
          var hull = Instantiate(prefab, localTransform.position,
            localTransform.rotation, null);
          if (hull == null) return;
          var hullNetView = hull.GetComponent<ZNetView>();
          PiecesController.AddNewPiece(hullNetView);
        }
      }
      else
      {
        var hull = Instantiate(prefab, localTransform.position,
          localTransform.rotation, null);
        if (hull == null) return;
        var hullNetView = hull.GetComponent<ZNetView>();
        PiecesController.AddNewPiece(hullNetView);
      }

      PiecesController.SetInitComplete();
    }

    public void AddOrRemoveVehicleDebugger()
    {
      if (!isActiveAndEnabled) return;
      // early exit if this should not be added. only need to remove this for performance reasons if the player has specifically flagged the debugger off.
      if (!HasVehicleDebugger)
      {
        Destroy(VehicleDebugHelpersInstance);
        VehicleDebugHelpersInstance = null;
        return;
      }

      if (VehicleDebugHelpersInstance != null) return;
      if (MovementController == null || !MovementController.FloatCollider ||
          !MovementController.OnboardCollider)
      {
        CancelInvoke(nameof(AddOrRemoveVehicleDebugger));
        Invoke(nameof(AddOrRemoveVehicleDebugger), 1);
        return;
      }

      VehicleDebugHelpersInstance =
        gameObject.AddComponent<VehicleDebugHelpers>();

      VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders
      {
        collider = MovementController.FloatCollider,
        lineColor = Color.green,
        parent = transform
      });

      VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders
      {
        collider = MovementController.OnboardCollider,
        lineColor = Color.yellow,
        parent = transform
      });
      VehicleDebugHelpersInstance.VehicleObj = gameObject;
      VehicleDebugHelpersInstance.vehicleManagerInstance = this;
    }

    /// <summary>
    /// Initializes the WaterVehicleController on the PiecePrefabGameObject
    /// </summary>
    /// <note>
    /// this must be added instead of on the prefab otherwise PlacedPiece cannot get the data in time
    ///
    /// This does not call InitFromShip due to init from ship require other values to be set on VehicleShip before running the command.
    /// </note>
    public void InitializeVehiclePiecesController()
    {
      if (ZNetView.m_forceDisableInit) return;
      if (m_nview == null || m_nview.GetZDO() == null || m_nview.m_ghost ||
          PiecesController != null || PersistentZdoId == 0 || _vehiclePiecesContainerInstance != null) return;

      var ladders = GetComponentsInChildren<Ladder>();
      foreach (var ladder in ladders)
        ladder.m_useDistance = 10f;

      var vehiclePiecesContainer = VehiclePiecesPrefab.VehiclePiecesContainer;
      if (!vehiclePiecesContainer) return;

      var localTransform = transform;
      _vehiclePiecesContainerInstance =
        Instantiate(vehiclePiecesContainer, localTransform.position,
          localTransform.rotation);

      PiecesController = _vehiclePiecesContainerInstance
        .AddComponent<VehiclePiecesController>();
    }

  #region IVehicleConfig

    public string Version
    {
      get => Config.Version;
      set
      {
      }
    }

    public float TreadDistance
    {
      get => Config.TreadDistance;
      set {}
    }

    public float TreadLength
    {
      get => Config.TreadLength;
      set {}
    }

    public float TreadHeight
    {
      get => Config.TreadHeight;
      set {}
    }

    public float TreadScaleX
    {
      get => Config.TreadScaleX;
      set {}
    }

    public bool HasCustomFloatationHeight
    {
      get => Config.HasCustomFloatationHeight;
      set {}
    }

    public float CustomFloatationHeight
    {
      get => Config.CustomFloatationHeight;
      set {}
    }

    public float CenterOfMassOffset
    {
      get => Config.CenterOfMassOffset;
      set {}
    }

    public bool ForceDocked
    {
      get => Config.ForceDocked;
      set {}
    }

  #endregion

  }