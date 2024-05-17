using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class WaterVehiclePrefab : IRegisterPrefab
{
  public static readonly WaterVehiclePrefab Instance = new();

  public static GameObject CreateClonedPrefab(string prefabName)
  {
    return PrefabManager.Instance.CreateClonedPrefab(prefabName,
      LoadValheimVehicleAssets.VehicleShipAsset);
  }

  /**
   * todo it's possible this all needs to be done in the Awake method to safely load valheim.
   * Should test this in development build of valheim
   */
  public static GameObject CreateWaterVehiclePrefab(
    GameObject prefab)
  {
    var netView = PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    netView.m_type = ZDO.ObjectType.Prioritized;

    var colliderParentObj = prefab.transform.Find("colliders");
    var floatColliderObj =
      colliderParentObj.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      colliderParentObj.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      colliderParentObj.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    var floatBoxCollider = floatColliderObj.GetComponent<BoxCollider>();

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    var vehicleRigidbody = prefab.GetComponent<Rigidbody>();
    var zSyncTransform = prefab.AddComponent<ZSyncTransform>();
    zSyncTransform.m_syncPosition = true;
    zSyncTransform.m_syncBodyVelocity = true;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_body = vehicleRigidbody;


    var shipInstance = prefab.AddComponent<VehicleShip>();
    var shipControls = prefab.AddComponent<VehicleMovementController>();
    shipInstance.ColliderParentObj = colliderParentObj.gameObject;

    shipInstance.ShipDirection =
      floatColliderObj.FindDeepChild(PrefabNames.VehicleShipMovementOrientation);
    shipInstance.m_shipControlls = shipControls;
    shipInstance.MovementController = shipControls;
    shipInstance.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    shipInstance.m_body = vehicleRigidbody;
    shipInstance.m_zsyncTransform = zSyncTransform;

    // todo fix ship water effects so they do not cause ship materials to break

    var waterEffects =
      Object.Instantiate(LoadValheimAssets.shipWaterEffects, prefab.transform);
    waterEffects.name = PrefabNames.VehicleShipEffects;
    var shipEffects = waterEffects.GetComponent<ShipEffects>();
    var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
    VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects, shipEffects);
    Object.Destroy(shipEffects);

    vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
    shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
    shipInstance.ShipEffects = vehicleShipEffects;

    shipInstance.m_floatcollider = floatBoxCollider;
    shipInstance.FloatCollider = floatBoxCollider;

    // wearntear may need to be removed or tweaked
    prefab.AddComponent<WearNTear>();
    var woodWNT = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);

    wnt.m_onDestroyed += woodWNT.m_onDestroyed;
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    prefab.AddComponent<ImpactEffect>();

    // var shipControlsGui = new GameObject
    //   { name = "ControlGui", layer = 0, transform = { parent = prefab.transform } };
    // shipControlsGui.transform.SetParent(prefab.transform);
    //
    // // todo the gui likely does not need these values
    // shipControlsGui.transform.localPosition = new Vector3(2.154f, 1.027f, -2.162f);
    // shipInstance.m_controlGuiPos = shipControlsGui.transform;

    return prefab;
  }

  private static void RegisterWaterVehicleShipPrefab()
  {
    var prefab = CreateClonedPrefab(PrefabNames.WaterVehicleShip);
    var waterVehiclePrefab = CreateWaterVehiclePrefab(prefab);

    var piece = PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.WaterVehicleShip, prefab);
    piece.m_waterPiece = true;

    PieceManager.Instance.AddPiece(new CustomPiece(waterVehiclePrefab, true, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterVehicleShipPrefab();
  }
}