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

  public static GameObject CreateClonedPrefab(string prefabName)
  {
    return PrefabManager.Instance.CreateClonedPrefab(prefabName,
      LoadValheimVehicleAssets.VehicleShipAsset);
  }

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

    var blockingBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();
    var floatBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();
    var onboardBoxCollider = blockingColliderObj.GetComponent<BoxCollider>();

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    var vehicleRigidbody = prefab.GetComponent<Rigidbody>();
    var zSyncTranform = prefab.AddComponent<ZSyncTransform>();
    zSyncTranform.m_syncPosition = true;
    zSyncTranform.m_syncBodyVelocity = true;
    zSyncTranform.m_syncRotation = true;
    zSyncTranform.m_body = vehicleRigidbody;

    var shipControls = prefab.AddComponent<VehicleMovementController>();

    var shipInstance = prefab.AddComponent<VehicleShip>();
    shipInstance.ColliderParentObj = colliderParentObj.gameObject;
    shipInstance.ColliderParentObj.gameObject.AddComponent<BoxCollider>();
    shipInstance.MovementController = shipControls;
    shipInstance.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    shipInstance.m_body = vehicleRigidbody;
    shipInstance.m_zsyncTransform = zSyncTranform;

    // todo fix ship water effects so they do not cause ship materials to break

    var waterEffects =
      Object.Instantiate(LoadValheimAssets.shipWaterEffects, prefab.transform);
    var shipEffects = waterEffects.GetComponent<ShipEffects>();
    var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
    VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects, shipEffects);
    Object.Destroy(shipEffects);

    vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
    shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
    shipInstance.ShipEffects = vehicleShipEffects;

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
  }
}