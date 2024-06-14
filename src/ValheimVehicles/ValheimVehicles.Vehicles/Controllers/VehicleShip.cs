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
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using static ValheimVehicles.Config.PrefabConfig;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Components;

public static class VehicleShipHelpers
{
  public static GameObject GetOrFindObj(GameObject returnObj, GameObject searchObj,
    string objectName)
  {
    if ((bool)returnObj)
    {
      return returnObj;
    }

    var gameObjTransform = searchObj.transform.FindDeepChild(objectName);
    if (!gameObjTransform)
    {
      return returnObj;
    }

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

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;

  public GameObject GhostContainer() =>
    VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
      PrefabNames.GhostContainer);

  public GameObject PiecesContainer() =>
    VehicleShipHelpers.GetOrFindObj(_piecesContainer, transform.parent.gameObject,
      PrefabNames.PiecesContainer);

  public static readonly List<VehicleShip> AllVehicles = [];

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;
  public float TargetHeight => MovementController.TargetHeight;

  public bool isCreative;

  public static bool HasVehicleDebugger = false;

  public void SetCreativeMode(bool val)
  {
    isCreative = val;
    UpdateShipEffects();
    MovementController.UpdateShipCreativeModeRotation();
  }

  public static bool CustomShipPhysicsEnabled = false;

  // The determines the directional movement of the ship 
  public GameObject ColliderParentObj;

  public IWaterVehicleController VehiclePiecesController => _piecesController;

  public GameObject? ShipEffectsObj;
  public VehicleShipEffects? ShipEffects;

  private WaterVehicleController _piecesController;
  public ZSyncTransform movementZSyncTransform;
  public ZSyncTransform piecesZsyncTransform;

  public ZNetView NetView { get; set; }

  public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

  public VehicleMovementController? MovementController { get; set; }

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

  public Transform m_controlGuiPos { get; set; }


  public VehicleShip Instance => this;

  public Transform ControlGuiPosition
  {
    get => m_controlGuiPos;
    set => m_controlGuiPos = value;
  }

  private Rigidbody? MovementControllerRigidbody
  {
    get
    {
      if (!MovementController)
      {
        MovementController = GetComponent<VehicleMovementController>();
      }

      return MovementController?.m_body;
    }
  }

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

  public static GameObject GetVehicleMovementCollidersObj(Transform prefabRoot)
  {
    var obj = prefabRoot.Find("vehicle_movement/colliders");
    return obj.gameObject;
  }


  public static GameObject GetVehicleMovementObj(Transform prefabRoot)
  {
    var obj = prefabRoot.Find("vehicle_movement");
    return obj.gameObject;
  }

  public static void OnAllowFlight(object sender, EventArgs eventArgs)
  {
    foreach (var vehicle in AllVehicles)
    {
      vehicle.MovementController.OnFlightChangePolling();
    }
  }


  private static void UpdateShipSounds(VehicleShip vehicleShip)
  {
    if (vehicleShip?.ShipEffects == null) return;
    vehicleShip.ShipEffects.m_inWaterSoundRoot.SetActive(ValheimRaftPlugin.Instance
      .EnableShipInWaterSounds.Value);
    vehicleShip.ShipEffects.m_wakeSoundRoot.SetActive(ValheimRaftPlugin.Instance
      .EnableShipWakeSounds.Value);
    // this one is not a gameobject so have to select the gameobject
    vehicleShip.ShipEffects.m_sailSound.gameObject.SetActive(ValheimRaftPlugin.Instance
      .EnableShipInWaterSounds.Value);
  }

  private static void UpdateAllShipSounds()
  {
    foreach (var vehicleShip in AllVehicles)
    {
      UpdateShipSounds(vehicleShip);
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
    _piecesController.CleanUp();
    Destroy(_piecesController.gameObject);
  }

  public void OnDestroy()
  {
    AllVehicles.Remove(this);
    UnloadAndDestroyPieceContainer();

    if (MovementController && MovementController != null)
    {
      Destroy(MovementController.gameObject);
    }
  }

  public void AwakeSetupVehicleShip()
  {
    var vehicleMovementObj = GetVehicleMovementObj(transform);

    if (!MovementController)
    {
      MovementController = vehicleMovementObj.GetComponent<VehicleMovementController>();
    }

    if ((bool)vehicleMovementObj && !(bool)movementZSyncTransform)
    {
      movementZSyncTransform = vehicleMovementObj.GetComponent<ZSyncTransform>();
    }

    if (!(bool)ShipEffectsObj)
    {
      ShipEffects = MovementController?.GetComponent<VehicleShipEffects>();
      ShipEffectsObj = ShipEffects?.gameObject;
    }
  }

  public void UpdateShipEffects()
  {
    ShipEffectsObj?.SetActive(!(TargetHeight > 0f || isCreative));
  }

  public void FixShipRotation()
  {
    var eulerAngles = transform.rotation.eulerAngles;
    var eulerX = eulerAngles.x;
    var eulerY = eulerAngles.y;
    var eulerZ = eulerAngles.z;

    var transformedX = eulerX;
    var transformedZ = eulerZ;
    var shouldUpdate = false;

    if (eulerX is > 60 and < 300)
    {
      transformedX = 0;
      shouldUpdate = true;
    }

    if (eulerZ is > 60 and < 300)
    {
      transformedZ = 0;
      shouldUpdate = true;
    }

    if (shouldUpdate)
    {
      transform.rotation = Quaternion.Euler(transformedX, eulerY, transformedZ);
    }
  }

  private void Awake()
  {
    NetView = GetComponent<ZNetView>();

    if (AllVehicles.Contains(this))
    {
      Logger.LogDebug("VehicleShip somehow already registered this component");
    }
    else
    {
      AllVehicles.Add(this);
    }

    AwakeSetupVehicleShip();

    Logger.LogDebug(
      $"called Awake in {name}, movementControllerRigidbody {MovementControllerRigidbody}");
    if (!NetView)
    {
      NetView = GetComponent<ZNetView>();
    }

    FixShipRotation();
    InitializeWaterVehicleController();

    UpdateShipSounds(this);
  }

  public void Start()
  {
    if (HasVehicleDebugger && _piecesController)
    {
      InitializeVehicleDebugger();
    }
  }

  public void OnEnable()
  {
    InitializeWaterVehicleController();
    if (HasVehicleDebugger && _piecesController)
    {
      InitializeVehicleDebugger();
    }
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost || (bool)_piecesController ||
        !isActiveAndEnabled) return;
    var sector = ZoneSystem.instance.GetZone(transform.position);
    var zdo = NetView.GetZDO();
    zdo.SetPosition(transform.position);
    zdo.SetSector(sector);
  }

  private GameObject GetStarterPiece()
  {
    string selectedPrefab;
    switch (StartingPiece.Value)
    {
      case VehicleShipInitPiece.HullFloor2X2:
        selectedPrefab = PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Wood,
          PrefabNames.PrefabSizeVariant.Two);
        break;
      case VehicleShipInitPiece.HullFloor4X4:
        selectedPrefab = PrefabNames.GetHullSlabVariants(ShipHulls.HullMaterial.Wood,
          PrefabNames.PrefabSizeVariant.Four);
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
      var shipPrefab = PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);
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

    var pieceCount = _piecesController.GetPieceCount();
    if (pieceCount != 0)
    {
      return;
    }

    if (_piecesController.BaseVehicleInitState != BaseVehicleController.InitializationState.Created)
    {
      return;
    }

    var prefab = GetStarterPiece();
    if (!prefab) return;

    var hull = Instantiate(prefab, transform.position, transform.rotation);
    if (hull == null) return;

    var hullNetView = hull.GetComponent<ZNetView>();
    _piecesController.AddNewPiece(hullNetView);
    _piecesController.SetInitComplete();
  }

  public void InitializeVehicleDebugger()
  {
    if (VehicleDebugHelpersInstance != null) return;
    if (MovementController == null || !MovementController.FloatCollider ||
        !MovementController.BlockingCollider || !MovementController.OnboardCollider)
    {
      CancelInvoke(nameof(InitializeVehicleDebugger));
      Invoke(nameof(InitializeVehicleDebugger), 1);
      return;
    }

    VehicleDebugHelpersInstance = gameObject.AddComponent<VehicleDebugHelpers>();


    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = MovementController.FloatCollider,
      lineColor = Color.green,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = MovementController.BlockingCollider,
      lineColor = Color.blue,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = MovementController.OnboardCollider,
      lineColor = Color.yellow,
      parent = gameObject
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
  private void InitializeWaterVehicleController()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost ||
        (bool)_piecesController) return;

    var ladders = GetComponentsInChildren<Ladder>();
    foreach (var ladder in ladders)
      ladder.m_useDistance = 10f;

    var colliderParentBoxCollider =
      ColliderParentObj.gameObject.AddComponent<BoxCollider>();
    colliderParentBoxCollider.enabled = false;

    var vehiclePiecesContainer = VehiclePiecesPrefab.VehiclePiecesContainer;
    if (!vehiclePiecesContainer) return;

    _vehiclePiecesContainerInstance =
      Instantiate(vehiclePiecesContainer, transform.position, transform.rotation);

    _piecesController = _vehiclePiecesContainerInstance.AddComponent<WaterVehicleController>();
    _piecesController.InitFromShip(Instance);

    InitStarterPiece();
  }

  /**
    * Compatibility method remappings
    * delegates a bunch of required ship logic to MovementController
    */
  // public Ship.Speed GetSpeedSetting() => VehicleSpeed;
  //
  // public float GetRudder() => MovementController.GetRudder();
  // public float GetRudderValue() => MovementController.GetRudderValue();
  //
  // public void ApplyControlls(Vector3 dir) => MovementController.ApplyControls(dir);
  //
  // public void ApplyControls(Vector3 dir) => MovementController.ApplyControls(dir);
  //
  // public void Forward()
  // {
  //   MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Forward);
  // }
  //
  // public void Backward() =>
  //   MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Backward);
  //
  // public void Stop() =>
  //   MovementController.SendSpeedChange(VehicleMovementController.DirectionChange.Stop);
  //
  // public void UpdateControls(float dt) => MovementController.UpdateControls(dt);
  //
  // public void UpdateControlls(float dt) => MovementController.UpdateControls(dt);
}