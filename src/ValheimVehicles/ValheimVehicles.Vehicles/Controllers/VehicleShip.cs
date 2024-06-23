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
using ValheimVehicles.Vehicles.Interfaces;
using ValheimVehicles.Vehicles.Structs;
using ZdoWatcher;
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
  private int _persistentZdoId;
  public int PersistentZdoId => GetPersistentID();

  // The rudder force multiplier applied to the ship speed
  private float _rudderForce = 1f;


  public GameObject GhostContainer() =>
    VehicleShipHelpers.GetOrFindObj(_ghostContainer, gameObject,
      PrefabNames.GhostContainer);

  public GameObject PiecesContainer() =>
    VehicleShipHelpers.GetOrFindObj(_piecesContainer, transform.parent.gameObject,
      PrefabNames.PiecesContainer);

  public static readonly Dictionary<int, VehicleShip> AllVehicles = new();

  private GameObject _piecesContainer;
  private GameObject _ghostContainer;
  private ImpactEffect _impactEffect;
  public float TargetHeight => MovementController?.TargetHeight ?? 0f;

  public bool isCreative;

  public static bool HasVehicleDebugger => VehicleDebugConfig.VehicleDebugMenuEnabled.Value;

  public void SetCreativeMode(bool val)
  {
    isCreative = val;
    MovementController?.UpdateShipCreativeModeRotation();
    UpdateShipEffects();
  }

  public static bool CustomShipPhysicsEnabled = false;

  // The determines the directional movement of the ship 
  public GameObject ColliderParentObj;

  public GameObject? ShipEffectsObj;
  public VehicleShipEffects? ShipEffects;

  public WaterVehicleController PiecesController;
  public ZSyncTransform movementZSyncTransform;
  public ZSyncTransform piecesZsyncTransform;

  public ZNetView NetView { get; set; }

  public VehicleDebugHelpers? VehicleDebugHelpersInstance { get; private set; }

  public IWaterVehicleController VehiclePiecesController => PiecesController;

  private VehicleMovementController? _movementController;

  public VehicleMovementController? MovementController
  {
    get
    {
      if (!_movementController)
      {
        AwakeSetupVehicleShip();
      }

      return _movementController;
    }
    set => _movementController = value;
  }

  private GameObject _vehiclePiecesContainerInstance;
  private GUIStyle myButtonStyle;

  public Transform m_controlGuiPos { get; set; }


  public VehicleShip Instance => this;

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
      vehicle.Value?.MovementController?.OnFlightChangePolling();
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
      UpdateShipSounds(vehicleShip.Value);
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
    PiecesController.CleanUp();
    Destroy(PiecesController.gameObject);
  }

  public void OnDestroy()
  {
    UnloadAndDestroyPieceContainer();

    if (PersistentZdoId != 0 && AllVehicles.ContainsKey(PersistentZdoId))
    {
      AllVehicles.Remove(PersistentZdoId);
    }

    if (MovementController && MovementController != null)
    {
      Destroy(MovementController.gameObject);
    }
  }

  public void AwakeSetupVehicleShip()
  {
    if (!_movementController)
    {
      MovementController = GetComponent<VehicleMovementController>();
    }

    // if (!movementZSyncTransform)
    // {
    //   movementZSyncTransform = GetComponent<ZSyncTransform>();
    // }

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

  private int GetPersistentID()
  {
    if (ZdoWatchManager.Instance == null)
    {
      Logger.LogWarning("No ZdoWatchManager instance, this means something went wrong");
    }

    if (_persistentZdoId != 0)
    {
      return _persistentZdoId;
    }

    if (!NetView)
    {
      NetView = GetComponent<ZNetView>();
    }

    if (NetView == null)
    {
      return _persistentZdoId;
    }

    if (NetView.GetZDO() == null)
    {
      return _persistentZdoId;
    }

    _persistentZdoId =
      ZdoWatchManager.Instance?.GetOrCreatePersistentID(NetView.GetZDO()) ?? 0;
    return _persistentZdoId;
  }

  private void Awake()
  {
    if (ZNetView.m_forceDisableInit) return;
    NetView = GetComponent<ZNetView>();
    GetPersistentID();

    if (PersistentZdoId == 0)
    {
      Logger.LogWarning("PersistewnZdoId, did not get a zdo from the NetView");
    }

    if (AllVehicles.ContainsKey(PersistentZdoId))
    {
      Logger.LogDebug("VehicleShip somehow already registered this component");
    }
    else
    {
      AllVehicles.Add(PersistentZdoId, this);
    }

    AwakeSetupVehicleShip();

    if (!NetView)
    {
      Logger.LogWarning("No NetView but tried to set it before");
      NetView = GetComponent<ZNetView>();
    }

    FixShipRotation();
    InitializeWaterVehicleController();

    UpdateShipSounds(this);
  }

  public void Start()
  {
    if (HasVehicleDebugger && PiecesController)
    {
      InitializeVehicleDebugger();
    }
  }

  public void OnEnable()
  {
    if (!NetView)
    {
      NetView = GetComponent<ZNetView>();
    }

    GetPersistentID();

    if (PersistentZdoId != 0 && !AllVehicles.ContainsKey(PersistentZdoId))
    {
      AllVehicles.Add(PersistentZdoId, this);
    }

    InitializeWaterVehicleController();
    if (HasVehicleDebugger && PiecesController)
    {
      InitializeVehicleDebugger();
    }
  }

  public void UpdateShipZdoPosition()
  {
    if (!(bool)NetView || NetView.GetZDO() == null || NetView.m_ghost || (bool)PiecesController ||
        !isActiveAndEnabled) return;
    var sector = ZoneSystem.instance.GetZone(transform.position);
    var zdo = NetView.GetZDO();
    zdo.SetPosition(transform.position);
    zdo.SetSector(sector);
  }

  private GameObject GetStarterPiece()
  {
    string selectedPrefab;
    switch (StartingPiece?.Value)
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

    var pieceCount = PiecesController.GetPieceCount();
    if (pieceCount != 0)
    {
      return;
    }

    if (PiecesController.BaseVehicleInitState != VehiclePieceController.InitializationState.Created)
    {
      return;
    }

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
        (bool)PiecesController || PersistentZdoId == 0) return;

    var ladders = GetComponentsInChildren<Ladder>();
    foreach (var ladder in ladders)
      ladder.m_useDistance = 10f;

    var vehiclePiecesContainer = VehiclePiecesPrefab.VehiclePiecesContainer;
    if (!vehiclePiecesContainer) return;

    var prevValue = ZNetView.m_useInitZDO;
    ZNetView.m_useInitZDO = false;
    _vehiclePiecesContainerInstance =
      Instantiate(vehiclePiecesContainer, transform.position, transform.rotation, null);
    ZNetView.m_useInitZDO = prevValue;

    PiecesController = _vehiclePiecesContainerInstance.AddComponent<WaterVehicleController>();
    PiecesController.InitFromShip(Instance);

    InitStarterPiece();
  }
}