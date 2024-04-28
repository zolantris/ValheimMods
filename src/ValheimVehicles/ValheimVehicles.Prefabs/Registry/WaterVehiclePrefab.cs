using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
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

  public static GameObject GetWaterVehiclePrefab =>
    PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleContainer);

  private static void RegisterWaterVehiclePrefab()
  {
    var waterVehicleObj = new GameObject()
    {
      name = PrefabNames.WaterVehicleContainer,
    };

    PrefabManager.Instance.CreateClonedPrefab(PrefabNames.WaterVehicleContainer,
      waterVehicleObj);
    Object.Destroy(waterVehicleObj);
  }

  public static GameObject CreateWaterVehiclePrefab(
    string prefabName = PrefabNames.WaterVehicleShip)
  {
    var waterVehiclePrefab =
      PrefabManager.Instance.CreateClonedPrefab(prefabName,
        LoadValheimVehicleAssets.VehicleShipAsset);

    var netView = PrefabRegistryHelpers.AddNetViewWithPersistence(waterVehiclePrefab);
    netView.m_type = ZDO.ObjectType.Prioritized;

    /*
     * Add all necessary colliders to the ship prefab
     * TODO make this a GameObject with a BoxCollider in it
     */
    var floatColliderObj =
      waterVehiclePrefab.transform.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderObj =
      waterVehiclePrefab.transform.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderObj =
      waterVehiclePrefab.transform.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    onboardColliderObj.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderObj.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderObj.name = PrefabNames.WaterVehicleBlockingCollider;

    var blockingBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();
    var floatBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();
    var onboardBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    var vehicleRigidbody = waterVehiclePrefab.GetComponent<Rigidbody>();
    var zSyncTranform = waterVehiclePrefab.AddComponent<ZSyncTransform>();
    zSyncTranform.m_syncPosition = true;
    zSyncTranform.m_syncBodyVelocity = true;
    zSyncTranform.m_syncRotation = true;
    zSyncTranform.m_body = vehicleRigidbody;

    var shipControls = waterVehiclePrefab.AddComponent<VehicleMovementController>();

    var shipInstance = waterVehiclePrefab.AddComponent<VehicleShip>();
    shipInstance.MovementController = shipControls;
    shipInstance.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    shipInstance.m_body = vehicleRigidbody;
    shipInstance.m_zsyncTransform = zSyncTranform;

    // todo fix ship water effects so they do not cause ship materials to break

    var waterEffects =
      Object.Instantiate(LoadValheimAssets.shipWaterEffects, waterVehiclePrefab.transform);
    var shipEffects = waterEffects.GetComponent<ShipEffects>();
    var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
    VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects, shipEffects);
    Object.Destroy(shipEffects);

    vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
    shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
    shipInstance.ShipEffects = vehicleShipEffects;

    shipInstance.FloatColliderObj = floatColliderObj.gameObject;
    shipInstance.FloatCollider = floatBoxCollider;

    // wearntear may need to be removed or tweaked
    waterVehiclePrefab.AddComponent<WearNTear>();
    var woodWNT = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(waterVehiclePrefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);

    wnt.m_onDestroyed += woodWNT.m_onDestroyed;
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    waterVehiclePrefab.AddComponent<ImpactEffect>();

    var shipControlsGui = new GameObject
      { name = "ControlGui", layer = 0, transform = { parent = waterVehiclePrefab.transform } };
    shipControlsGui.transform.SetParent(waterVehiclePrefab.transform);

    // todo the gui likely does not need these values
    shipControlsGui.transform.localPosition = new Vector3(2.154f, 1.027f, -2.162f);
    shipInstance.m_controlGuiPos = shipControlsGui.transform;

    return waterVehiclePrefab;
  }

  private static void RegisterWaterVehicleShipPrefab()
  {
    var waterVehiclePrefab = CreateWaterVehiclePrefab();

    var piece = waterVehiclePrefab.AddComponent<Piece>();
    piece.m_waterPiece = true;
    piece.m_description = "$valheim_vehicles_water_vehicle_desc";
    piece.m_icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon;
    piece.m_name = "$valheim_vehicles_water_vehicle";

    PieceManager.Instance.AddPiece(new CustomPiece(waterVehiclePrefab, true, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = piece.m_name,
      Description = piece.m_description,
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "FineWood",
          Recover = true
        }
      ]
    }));
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterVehicleShipPrefab();
    RegisterWaterVehiclePrefab();
  }
}