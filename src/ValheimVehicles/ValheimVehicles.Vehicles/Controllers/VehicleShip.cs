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

  public static readonly Dictionary<int, VehicleShip> AllVehicles = new();

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;
  public float TargetHeight => MovementController?.TargetHeight ?? 0f;

  public bool isCreative;

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

  public VehicleShip Instance => this;

  private GameObject _vehiclePiecesContainerInstance;
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
    foreach (var vehicle in AllVehicles)
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
    foreach (var vehicleShip in AllVehicles)
      UpdateShipSounds(vehicleShip.Value);
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
    PiecesController.CleanUp();
    Destroy(PiecesController.gameObject);
  }

  public void OnDestroy()
  {
    UnloadAndDestroyPieceContainer();

    if (PersistentZdoId != 0 && AllVehicles.ContainsKey(PersistentZdoId))
      AllVehicles.Remove(PersistentZdoId);

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

    if (AllVehicles.ContainsKey(PersistentZdoId))
      Logger.LogDebug("VehicleShip somehow already registered this component");
    else
      AllVehicles.Add(PersistentZdoId, this);

    if (!NetView)
    {
      Logger.LogWarning("No NetView but tried to set it before");
      NetView = GetComponent<ZNetView>();
    }

    vehicleMovementTransform = GetVehicleMovementTransform(transform);

    InitializeVehiclePiecesController();
    InitializeMovementController();
    InitializeOnboardController();

    // For starting the vehicle pieces.
    PiecesController.InitFromShip(Instance);
    InitStarterPiece();
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

    if (PiecesController != null)
      PiecesController.MovementController = MovementController;

    if (OnboardController != null)
      OnboardController.MovementController = MovementController;

    if (ShipEffectsObj == null)
    {
      ShipEffects = MovementController.GetComponent<VehicleShipEffects>();
      ShipEffectsObj = ShipEffects.gameObject;
    }
  }

  /// <summary>
  /// @Requires MovementController
  /// </summary>
  public void InitializeOnboardController()
  {
    if (MovementController == null || PiecesController == null)
    {
      Logger.LogError(
        $"MovementController: {MovementController}, PiecesController {PiecesController} not initialized this likely means the mod is unstable");
      return;
    }

    PiecesController.OnboardController = PiecesController.OnboardCollider
      .gameObject
      .AddComponent<VehicleOnboardController>();
    OnboardController = PiecesController.OnboardController;
    MovementController.OnboardController = PiecesController.OnboardController;
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

    GetPersistentID();

    if (PersistentZdoId != 0 && !AllVehicles.ContainsKey(PersistentZdoId))
      AllVehicles.Add(PersistentZdoId, this);

    InitializeVehiclePiecesController();
    if (HasVehicleDebugger && PiecesController) InitializeVehicleDebugger();
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost ||
        (bool)PiecesController ||
        !isActiveAndEnabled) return;
    var position = transform.position;

    var sector = ZoneSystem.GetZone(position);
    var zdo = NetView.GetZDO();

    zdo.SetPosition(position);
    zdo.SetSector(sector);
  }

  private GameObject GetStarterPiece()
  {
    string selectedPrefab;
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

    var hull = Instantiate(prefab, transform.position, transform.rotation);
    if (hull == null) return;

    var hullNetView = hull.GetComponent<ZNetView>();

    PiecesController.AddNewPiece(hullNetView);
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

    // foreach (var piecesControllerConvexHullMesh in PiecesController
    //            .convexHullMeshes)
    //   VehicleDebugHelpersInstance.AddColliderToRerender(
    //     new DrawTargetColliders()
    //     {
    //       collider =
    //         piecesControllerConvexHullMesh.GetComponent<MeshCollider>(),
    //       lineColor = Color.blue,
    //       parent = gameObject
    //     });

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

    // var prevValue = ZNetView.m_useInitZDO;
    // ZNetView.m_useInitZDO = false;
    _vehiclePiecesContainerInstance =
      Instantiate(vehiclePiecesContainer, transform.position,
        transform.rotation);
    // ZNetView.m_useInitZDO = prevValue;

    PiecesController = _vehiclePiecesContainerInstance
      .AddComponent<VehiclePiecesController>();
  }
}