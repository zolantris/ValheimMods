#region

  using System;
  using System.Collections.Generic;
  using Jotunn.Extensions;
  using Jotunn.Managers;
  using Registry;
  using UnityEngine;
  using ValheimVehicles.Config;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Enums;
  using ValheimVehicles.Structs;
  using static ValheimVehicles.Config.PrefabConfig;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.Components;

  public static class VehicleShipHelpers
  {
    public static GameObject GetOrFindObj(GameObject returnObj,
      GameObject searchObj,
      string objectName)
    {
      if ((bool)returnObj) return returnObj;

      var gameObjTransform = searchObj.transform.FindDeepChild(objectName);
      if (!gameObjTransform) return returnObj;

      returnObj = gameObjTransform.gameObject;
      return returnObj;
    }
  }

  /// <summary>
  /// The main initializer for all vehicle components and a way to access all properties of the vehicle.
  /// </summary>
  public class VehicleManager : MonoBehaviour, IVehicleBaseProperties, IVehicleSharedProperties
  {
    public GameObject RudderObject { get; set; }
    public const float MinimumRigibodyMass = 1000;

    // controller used to disable some behaviors when copying the vehicle to prevent spawning the base prefab piece.
    public static bool CanInitHullPiece = true;

    private int _persistentZdoId;
    public int PersistentZdoId => GetPersistentID();

    // The rudder force multiplier applied to the ship speed
    private float _rudderForce = 1f;

    public VehicleCustomZdoConfig VehicleCustomZdoConfig { get; set; }

    public GameObject GhostContainer()
    {
      return VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
        PrefabNames.GhostContainer);
    }

    public GameObject PiecesContainer()
    {
      return VehicleShipHelpers.GetOrFindObj(_piecesContainer,
        transform.parent.gameObject,
        PrefabNames.VehiclePiecesContainer);
    }

    public static readonly Dictionary<int, VehicleManager> VehicleInstances = new();

    private GameObject _piecesContainer;
    private GameObject _ghostContainer;
    private ImpactEffect _impactEffect;
    public float TargetHeight => MovementController?.TargetHeight ?? 0f;
    public bool IsLandVehicleFromPrefab = false;
    public bool IsLandVehicle { get; set; }
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

    public void SetCreativeMode(bool val)
    {
      isCreative = val;
      if (MovementController != null)
        MovementController.UpdateShipCreativeModeRotation();
      UpdateShipEffects();
    }


    public GameObject? ShipEffectsObj;
    public VehicleShipEffects? ShipEffects;

    public VehiclePiecesController? PiecesController { get; set; }

    public ZNetView NetView { get; set; }

    public Transform vehicleMovementTransform;
    public Transform vehicleMovementCollidersTransform;

    public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

    public VehicleMovementController? MovementController { get; set; }
    public VehicleConfigSyncComponent VehicleConfigSync
    {
      get;
      set;
    } = null!;

    public VehicleOnboardController? OnboardController { get; set; }
    public VehicleWheelController? WheelController { get; set; }

    public VehicleManager Manager
    {
      get => this;
      set
      {
        // do nothing
      }
    }

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

    public static void UpdateAllWheelControllers()
    {
      foreach (var instance in VehicleInstances.Values)
      {
        instance.UpdateWheelControllerProperties();
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

    public void OnDestroy()
    {
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
      return PersistentIdHelper.GetPersistentIdFrom(NetView, ref _persistentZdoId);
    }

    private void Awake()
    {
      if (ZNetView.m_forceDisableInit) return;
      NetView = GetComponent<ZNetView>();
      GetPersistentID();

      VehicleConfigSync = gameObject.AddComponent<VehicleConfigSyncComponent>();
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

      if (!NetView)
      {
        Logger.LogWarning("No NetView but tried to set it before");
        NetView = GetComponent<ZNetView>();
      }

      vehicleMovementTransform = GetVehicleMovementTransform(transform);
    }

    public void InitializeAllComponents()
    {
      var shouldRun =
        MovementController == null || PiecesController == null ||
        OnboardController == null;
      if (!shouldRun) return;

      InitializeVehiclePiecesController();
      InitializeMovementController();
      InitializeOnboardController();
      InitializeShipEffects();
      InitializeWheelController();

      if (PiecesController == null || MovementController == null || OnboardController == null)
      {
        LoggerProvider.LogError($"Component Controllers should not be null but got null controllers \nPiecesController: {PiecesController} \nMovementController: {MovementController} \nOnboardController: {OnboardController}");

        return;
      }

      var allControllers = new List<IVehicleSharedProperties>
      {
        PiecesController,
        MovementController,
        OnboardController,
        VehicleConfigSync
      };

      VehicleSharedPropertiesUtils.BindAllControllers(this, allControllers);

      // Re-attaches all the components to the initialized components (if they are valid).
      RebindAllComponents();


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

      if (MovementController != null)
      {
        MovementController.CanAnchor = IsLandVehicle;
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

    public void UpdateWheelControllerProperties()
    {
      if (!IsLandVehicle || MovementController == null || WheelController == null) return;
      if (WheelController.treadsPrefab == null)
      {
        WheelController.treadsPrefab = LoadValheimVehicleAssets.TankTreadsSingle;
      }

      if (WheelController.wheelPrefab == null)
      {
        WheelController.wheelPrefab = LoadValheimVehicleAssets.WheelSingle;
      }

      WheelController.treadWidthXScale = ExperimentalTreadScaleX.Value;


      // very important to add these. We always need a base of 30.
      var additionalTurnRate = Mathf.Lerp(VehicleWheelController.defaultTurnAccelerationMultiplier / 2, VehicleWheelController.defaultTurnAccelerationMultiplier * 2, Mathf.Clamp01(PropulsionConfig.VehicleLandTurnSpeed.Value));

      VehicleWheelController.baseTurnAccelerationMultiplier = additionalTurnRate;
      WheelController.maxTreadLength = PhysicsConfig.VehicleLandMaxTreadLength.Value;
      WheelController.maxTreadWidth = PhysicsConfig.VehicleLandMaxTreadWidth.Value;
      WheelController.forwardDirection = MovementController.ShipDirection;
      WheelController.wheelBottomOffset = PhysicsConfig.VehicleLandTreadOffset.Value;
    }

    /// <summary>
    /// For land vehicles
    /// </summary>
    public void InitializeWheelController()
    {
      if (!IsLandVehicle) return;
      if (WheelController == null)
      {
        WheelController = gameObject.GetComponent<VehicleWheelController>();
        if (WheelController == null)
        {
          WheelController = gameObject.AddComponent<VehicleWheelController>();
        }
      }
      WheelController.inputTurnForce = 0;
      WheelController.inputMovement = 0;
      UpdateWheelControllerProperties();
      if (WheelController == null)
        Logger.LogError("Error initializing WheelController");
    }

    public void Start()
    {
      if (HasVehicleDebugger && PiecesController) AddOrRemoveVehicleDebugger();

      UpdateShipSounds(this);
      UpdateShipEffects();
    }

    public void OnEnable()
    {
      if (!NetView) NetView = GetComponent<ZNetView>();

      var isValidZdo = NetView != null && NetView.GetZDO() != null;

      if (isValidZdo && !IsLandVehicleFromPrefab)
      {
        var zdo = NetView.GetZDO();
        if (zdo != null)
          IsLandVehicle = IsLandVehicleFromPrefab ||
                          zdo.GetBool(VehicleZdoVars.IsLandVehicle);
        else
          IsLandVehicle = IsLandVehicleFromPrefab;
      }
      else
      {
        IsLandVehicle = IsLandVehicleFromPrefab;
      }

      GetPersistentID();

      if (PersistentZdoId != 0 && !VehicleInstances.ContainsKey(PersistentZdoId))
        VehicleInstances.Add(PersistentZdoId, this);

      PhysicUtils.IgnoreAllCollisionsBetweenChildren(transform);

      if (isValidZdo) InitializeAllComponents();

      if (HasVehicleDebugger && PiecesController != null)
        AddOrRemoveVehicleDebugger();
    }

    public void UpdateShipZdoPosition()
    {
      if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost ||
          PiecesController == null ||
          !isActiveAndEnabled) return;
      var position = transform.position;

      var sector = ZoneSystem.GetZone(position);
      var zdo = NetView.GetZDO();

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
        NetView.GetZDO().Set(VehicleZdoVars.ZdoKeyBaseVehicleInitState, true);
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
      if (NetView == null || NetView.GetZDO() == null || NetView.m_ghost ||
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
  }