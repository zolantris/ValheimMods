using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jotunn.Extensions;
using Jotunn.Managers;
using Registry;
using UnityEngine;
using UnityEngine.UI;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles.Controllers;
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using ZdoWatcher;
using static ValheimVehicles.Config.PrefabConfig;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

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

/*
 * Acts as a Delegate component between the ship physics and the controller
 */
public class VehicleShip : MonoBehaviour, IVehicleShip
{
  public GameObject RudderObject { get; set; }
  public const float MinimumRigibodyMass = 1000;

  public static bool CanInitHullPiece = true;
  private int _persistentZdoId;
  public int PersistentZdoId => GetPersistentID();

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;


  public GameObject GhostContainer()
  {
    return VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
      PrefabNames.GhostContainer);
  }

  public GameObject PiecesContainer()
  {
    return VehicleShipHelpers.GetOrFindObj(_piecesContainer,
      transform.parent.gameObject,
      PrefabNames.PiecesContainer);
  }

  public static readonly Dictionary<int, VehicleShip> VehicleInstances = new();

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

  public static bool HasVehicleDebugger =>
    VehicleDebugConfig.VehicleDebugMenuEnabled.Value;

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

  public VehicleOnboardController? OnboardController { get; set; }
  public VehicleWheelController? WheelController { get; set; }

  public VehicleShip Instance => this;

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


  private static void UpdateShipSounds(VehicleShip vehicleShip)
  {
    if (vehicleShip?.ShipEffects == null) return;
    vehicleShip.ShipEffects.m_inWaterSoundRoot.SetActive(ValheimRaftPlugin
      .Instance
      .EnableShipInWaterSounds.Value);
    vehicleShip.ShipEffects.m_wakeSoundRoot.SetActive(ValheimRaftPlugin.Instance
      .EnableShipWakeSounds.Value);
    // this one is not a gameobject so have to select the gameobject
    vehicleShip.ShipEffects.m_sailSound.gameObject.SetActive(ValheimRaftPlugin
      .Instance
      .EnableShipInWaterSounds.Value);
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
  /// Unloads the Boat Pieces properly
  /// </summary>
  ///
  /// <description>calling cleanup must be done before Unity starts garbage collecting otherwise positions, ZNetViews and other items may be destroyed</description>
  /// 
  public void UnloadAndDestroyPieceContainer()
  {
    if (!(bool)_vehiclePiecesContainerInstance) return;
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
    if (ZNetView.m_forceDisableInit || ZNetScene.instance == null) return 0;
    if (ZdoWatchController.Instance == null)
    {
      Logger.LogWarning(
        "No ZdoWatchManager instance, this means something went wrong");
      _persistentZdoId = 0;
      return _persistentZdoId;
    }

    if (_persistentZdoId != 0) return _persistentZdoId;

    if (!NetView) NetView = GetComponent<ZNetView>();

    if (NetView == null) return _persistentZdoId;

    if (NetView.GetZDO() == null) return _persistentZdoId;

    _persistentZdoId =
      ZdoWatchController.Instance.GetOrCreatePersistentID(NetView.GetZDO());
    return _persistentZdoId;
  }

  private void Awake()
  {
    if (ZNetView.m_forceDisableInit) return;
    NetView = GetComponent<ZNetView>();
    GetPersistentID();

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


    // Re-attaches all the components to the initialized components (if they are valid).
    RebindAllComponents();


    // For starting the vehicle pieces.
    if (PiecesController != null)
    {
      PiecesController.InitFromShip(Instance);
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
    OnboardController.vehicleShip = this;
  }

  public void UpdateWheelControllerProperties()
  {
    if (!IsLandVehicle || MovementController == null || WheelController == null) return;
    if (WheelController.treadsPrefab == null)
    {
      WheelController.treadsPrefab = LoadValheimVehicleAssets.TankTreadsSingle;
    }

    WheelController.wheelPrefab = LoadValheimVehicleAssets.WheelSingle;
    WheelController.UseManualControls = true;
    WheelController.magicTurnRate = PhysicsConfig.VehicleLandTurnSpeed.Value;
    WheelController.forwardDirection = MovementController.ShipDirection;
    WheelController.m_steeringType = VehicleWheelController.SteeringType.Magic;
    WheelController.wheelSuspensionDistance = PhysicsConfig.VehicleLandSuspensionDistance.Value;
    WheelController.wheelRadius = PhysicsConfig.VehicleLandWheelRadius.Value;
    WheelController.wheelSuspensionSpring = PhysicsConfig.VehicleLandWheelSuspensionSpring.Value;
    WheelController.wheelBottomOffset = PhysicsConfig.VehicleLandWheelOffset.Value;
    WheelController.wheelMass = PhysicsConfig.VehicleLandWheelMass.Value;
  }

  /// <summary>
  /// For land vehicles
  /// </summary>
  public void InitializeWheelController()
  {
    if (WheelController == null)
    {
      WheelController = gameObject.GetComponent<VehicleWheelController>();
      if (WheelController == null)
      {
        WheelController = gameObject.AddComponent<VehicleWheelController>();
      }
    }
    WheelController.inputTurnForce = 0;
    WheelController.inputForwardForce = 0;
    UpdateWheelControllerProperties();
    if (WheelController == null)
      Logger.LogError("Error initializing WheelController");
  }

  public void Start()
  {
    if (HasVehicleDebugger && PiecesController) InitializeVehicleDebugger();

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

    if (isValidZdo) InitializeAllComponents();

    if (HasVehicleDebugger && PiecesController != null)
      InitializeVehicleDebugger();
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost ||
        PiecesController == null ||
        !isActiveAndEnabled) return;
    var position = transform.position;

    var sector = ZoneSystem.GetZone(position);
    var zdo = NetView.GetZDO();

    zdo.SetPosition(PiecesController.m_body.worldCenterOfMass);
    zdo.SetSector(sector);
  }

  private GameObject GetStarterPiece()
  {
    string selectedPrefab;
    if (IsLandVehicle)
      return PrefabManager.Instance.GetPrefab(PrefabNames.GetHullSlabName(
        ShipHulls.HullMaterial.Wood,
        PrefabNames.PrefabSizeVariant.FourByFour));

    switch (StartingPiece?.Value)
    {
      case VehicleShipInitPiece.HullFloor2X2:
        selectedPrefab = PrefabNames.GetHullSlabName(
          ShipHulls.HullMaterial.Wood,
          PrefabNames.PrefabSizeVariant.TwoByTwo);
        break;
      case VehicleShipInitPiece.HullFloor4X4:
        selectedPrefab = PrefabNames.GetHullSlabName(
          ShipHulls.HullMaterial.Wood,
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
   */
  public static VehicleShip? InitWithoutStarterPiece(Transform obj)
  {
    CanInitHullPiece = false;
    try
    {
      var shipPrefab =
        PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);
      var ship = Instantiate(shipPrefab, obj.position,
        obj.rotation, null);

      CanInitHullPiece = true;
      return ship.GetComponent<VehicleShip>();
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
        VehiclePiecesController.InitializationState.Created)
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

  public void InitializeVehicleDebugger()
  {
    if (VehicleDebugHelpersInstance != null) return;
    if (MovementController == null || !MovementController.FloatCollider ||
        !MovementController.OnboardCollider)
    {
      CancelInvoke(nameof(InitializeVehicleDebugger));
      Invoke(nameof(InitializeVehicleDebugger), 1);
      return;
    }

    VehicleDebugHelpersInstance =
      gameObject.AddComponent<VehicleDebugHelpers>();

    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = MovementController.FloatCollider,
      lineColor = Color.green,
      parent = transform
    });

    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = MovementController.OnboardCollider,
      lineColor = Color.yellow,
      parent = transform
    });
    VehicleDebugHelpersInstance.VehicleObj = gameObject;
    VehicleDebugHelpersInstance.VehicleShipInstance = this;
  }

  /// <summary>
  /// Initializes the WaterVehicleController on the PiecePrefabGameObject
  /// </summary>
  /// <note>
  /// this must be added instead of on the prefab otherwise PlacedPiece cannot get the data in time
  /// </note>
  public void InitializeVehiclePiecesController()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost ||
        (bool)PiecesController || PersistentZdoId == 0) return;

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