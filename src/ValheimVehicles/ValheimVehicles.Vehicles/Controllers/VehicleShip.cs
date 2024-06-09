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

  public IWaterVehicleController VehicleController => _controller;

  public GameObject? ShipEffectsObj;
  public VehicleShipEffects? ShipEffects;

  private WaterVehicleController _controller;
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

  private Rigidbody _movementControllerRigidbody => MovementController.rigidbody;

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
    _controller.CleanUp();
    Destroy(_controller.gameObject);
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
    var vehiclePiecesObj = GetVehiclePiecesObj(transform);

    if (!MovementController)
    {
      MovementController = vehicleMovementObj.GetComponent<VehicleMovementController>();
    }

    if (MovementController)
    {
      MovementController.ShipInstance = this;
    }

    if ((bool)vehicleMovementObj && !(bool)movementZSyncTransform)
    {
      movementZSyncTransform = vehicleMovementObj.GetComponent<ZSyncTransform>();
    }

    if ((bool)vehiclePiecesObj && !(bool)piecesZsyncTransform)
    {
      piecesZsyncTransform = vehiclePiecesObj.GetComponent<ZSyncTransform>();
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
      $"called Awake in {name}, movementControllerRigidbody {_movementControllerRigidbody}");
    if (!NetView)
    {
      NetView = GetComponent<ZNetView>();
    }

    FixShipRotation();


    InitializeWaterVehicleController();

    if (HasVehicleDebugger && _controller)
    {
      InitializeVehicleDebugger();
    }

    UpdateShipSounds(this);
  }

  public void OnEnable()
  {
    InitializeWaterVehicleController();
    if (HasVehicleDebugger && _controller)
    {
      InitializeVehicleDebugger();
    }
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost || (bool)_controller ||
        !isActiveAndEnabled) return;
    var sector = ZoneSystem.instance.GetZone(transform.position);
    var zdo = NetView.GetZDO();
    zdo.SetPosition(transform.position);
    zdo.SetSector(sector);
  }

  private void InitHull()
  {
    var pieceCount = _controller.GetPieceCount();
    if (pieceCount != 0 || !_controller.m_nview)
    {
      return;
    }

    if (_controller.BaseVehicleInitState != BaseVehicleController.InitializationState.Created)
    {
      return;
    }

    var prefab = PrefabManager.Instance.GetPrefab(PrefabNames.ShipHullCenterWoodPrefabName);
    if (!prefab) return;

    var hull = Instantiate(prefab, transform.position, transform.rotation);
    if (hull == null) return;

    var hullNetView = hull.GetComponent<ZNetView>();
    _controller.AddNewPiece(hullNetView);
    _controller.SetInitComplete();
  }

  public void InitializeVehicleDebugger()
  {
    if (VehicleDebugHelpersInstance != null) return;
    VehicleDebugHelpersInstance = gameObject.AddComponent<VehicleDebugHelpers>();

    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_floatcollider,
      lineColor = Color.green,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_blockingcollider,
      lineColor = Color.blue,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.AddColliderToRerender(new DrawTargetColliders()
    {
      collider = _controller.m_onboardcollider,
      lineColor = Color.yellow,
      parent = gameObject
    });
    VehicleDebugHelpersInstance.VehicleObj = gameObject;
    VehicleDebugHelpersInstance.VehicleShipInstance = this;
  }

  /*
   * Only initializes the controller if the prefab is enabled (when zdo is initialized this happens)
   */
  private void InitializeWaterVehicleController()
  {
    if (ZNetView.m_forceDisableInit) return;
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost || (bool)_controller) return;

    var ladders = GetComponentsInChildren<Ladder>();
    foreach (var ladder in ladders)
      ladder.m_useDistance = 10f;

    var colliderParentBoxCollider =
      ColliderParentObj.gameObject.AddComponent<BoxCollider>();
    colliderParentBoxCollider.enabled = false;

    var vehiclePiecesContainer = VehiclePiecesPrefab.VehiclePiecesContainer;
    if (!vehiclePiecesContainer) return;

    _vehiclePiecesContainerInstance = GetVehiclePiecesObj(transform);
    // _vehiclePiecesContainerInstance = Instantiate(vehiclePiecesContainer, transform);
    // _vehiclePiecesContainerInstance.transform.position = transform.position;
    // _vehiclePiecesContainerInstance.transform.rotation = transform.rotation;

    _controller = _vehiclePiecesContainerInstance.AddComponent<WaterVehicleController>();
    _controller.InitializeShipValues(Instance);

    // sets back to unparented to prevent rigidbody from controlling parent physics

    InitHull();
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