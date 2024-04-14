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

  public static GameObject GetVehiclePieces =>
    PrefabManager.Instance.GetPrefab(PrefabNames.VehiclePieces);

  private static void RegisterVehicleShipPiecesContainer()
  {
    PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VehiclePieces,
      LoadValheimVehicleAssets.VehiclePiecesAsset);
  }

  private static void RegisterWaterVehiclePrefab()
  {
    var _waterVehiclePrefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.WaterVehiclePrefabName,
        LoadValheimVehicleAssets.VehicleShipAsset);
    PrefabRegistryHelpers.AddNetViewWithPersistence(_waterVehiclePrefab);

    var buildGhost = _waterVehiclePrefab.AddComponent<VehicleBuildGhost>();
    buildGhost.gameObject.layer = 0;
    /*
     * Add all necessary colliders to the ship prefab
     * TODO make this a GameObject with a BoxCollider in it
     */
    var floatColliderComponent =
      LoadValheimVehicleAssets.VehicleShipAsset.transform.Find(
        PrefabNames.WaterVehicleFloatCollider);
    var blockingColliderComponent =
      LoadValheimVehicleAssets.VehicleShipAsset.transform.Find(PrefabNames
        .WaterVehicleBlockingCollider);
    var onboardColliderComponent =
      LoadValheimVehicleAssets.VehicleShipAsset.transform.Find(PrefabNames
        .WaterVehicleOnboardCollider);

    // EXPERIMENTAL. MAY CAUSE EXTREME LAG or annoying noises
    // will need to verify if the effects also expand across raft as it gets larger
    // var waterEffects =
    // Object.Instantiate(LoadValheimAssets.shipWaterEffects, _waterVehiclePrefab.transform);

    onboardColliderComponent.name = PrefabNames.WaterVehicleOnboardCollider;
    floatColliderComponent.name = PrefabNames.WaterVehicleFloatCollider;
    blockingColliderComponent.name = PrefabNames.WaterVehicleBlockingCollider;

    var zSyncTranform = _waterVehiclePrefab.AddComponent<ZSyncTransform>();

    // setting to true makes the shadows not vibrate, it may be related to kinematic items triggering too many re-renders or the kinematic item needs to be moved lower
    // rigidbody.isKinematic = false;

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    var vehicleRigidbody = _waterVehiclePrefab.GetComponent<Rigidbody>();
    _waterVehiclePrefab.AddComponent<ValheimShipControls>();
    var shipInstance = _waterVehiclePrefab.AddComponent<VehicleShip>();
    shipInstance.m_zsyncTransform = zSyncTranform;
    shipInstance.m_zsyncTransform.m_syncPosition = true;
    shipInstance.m_zsyncTransform.m_syncBodyVelocity = true;
    shipInstance.m_body = vehicleRigidbody;
    shipInstance.m_zsyncTransform.m_body = vehicleRigidbody;
    shipInstance.previewComponent =
      _waterVehiclePrefab.transform.Find("vehicle_ship_hull").gameObject;

    // todo fix ship water effects so they do not cause ship materials to break
    // shipInstance.waterEffects = waterEffects;
    shipInstance.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    shipInstance.m_zsyncTransform.m_body = vehicleRigidbody;

    shipInstance.FloatCollider = floatColliderComponent.GetComponentInChildren<BoxCollider>();

    // wearntear may need to be removed or tweaked
    _waterVehiclePrefab.AddComponent<WearNTear>();
    var woodWNT = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(_waterVehiclePrefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);
    // wnt.m_colliders = woodWNT.m_colliders;
    // wnt.m_onDamaged += woodWNT.m_onDamaged;
    // wnt.m_oldMaterials = null;
    wnt.m_onDestroyed += woodWNT.m_onDestroyed;
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    _waterVehiclePrefab.AddComponent<ImpactEffect>();
    // AddVehicleLODs(waterVehiclePrefab);

    var shipControlsGui = new GameObject
      { name = "ControlGui", layer = 0 };
    var shipControlsGuiInstance = Object.Instantiate(shipControlsGui,
      _waterVehiclePrefab.transform);
    shipControlsGui.transform.SetParent(_waterVehiclePrefab.transform);

    // todo the gui likely does not need these values
    shipControlsGui.transform.localPosition = new Vector3(2.154f, 1.027f, -2.162f);
    shipInstance.m_controlGuiPos = shipControlsGuiInstance.transform;

    var piece = _waterVehiclePrefab.AddComponent<Piece>();
    piece.m_waterPiece = true;
    piece.m_description = "$valheim_vehicles_raft_desc";
    piece.m_icon = LoadValheimAssets.vanillaRaftPrefab.GetComponent<Piece>().m_icon;
    piece.m_name = "$valheim_vehicles_raft";

    PieceManager.Instance.AddPiece(new CustomPiece(_waterVehiclePrefab, true, new PieceConfig
    {
      PieceTable = "Hammer",
      Name = piece.m_name,
      Description = piece.m_description,
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[3]
      {
        new()
        {
          Amount = 10,
          Item = "FineWood",
          Recover = true
        },
        new()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        },
        new()
        {
          Amount = 6,
          Item = "WolfPelt",
          Recover = true
        }
      }
    }));
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    // RegisterVehicleShipPiecesContainer();
    RegisterWaterVehiclePrefab();
  }
}