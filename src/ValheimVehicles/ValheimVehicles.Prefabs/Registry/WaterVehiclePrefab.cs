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

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var waterVehicle = new GameObject()
    {
      name = PrefabNames.WaterVehiclePrefabName,
      layer = 0,
    };
    var buildGhost = waterVehicle.AddComponent<VehicleBuildGhost>();
    buildGhost.gameObject.layer = 0;
    buildGhost.placeholderComponent =
      ShipHullPrefab.RaftHullPrefabInstance;
    buildGhost.UpdatePlaceholder();

    var _waterVehiclePrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.WaterVehiclePrefabName, waterVehicle);
    PrefabRegistryHelpers.AddNetViewWithPersistence(_waterVehiclePrefab);

    _waterVehiclePrefab.layer = 0;

    /*
     * Add all necessary colliders to the ship prefab
     * TODO make this a GameObject with a BoxCollider in it
     */
    var floatColliderComponent =
      LoadValheimAssets.vanillaRaftPrefab.transform.Find("FloatCollider");
    var blockingColliderComponent =
      LoadValheimAssets.vanillaRaftPrefab.transform.Find("ship/colliders/Cube");
    var onboardColliderComponent =
      LoadValheimAssets.vanillaRaftPrefab.transform.Find("OnboardTrigger");
    /*
     * add the colliders to the prefab
     */
    var blockingCollider = PrefabRegistryController.Instantiate(blockingColliderComponent,
      _waterVehiclePrefab.transform);
    var onboardCollider = PrefabRegistryController.Instantiate(onboardColliderComponent,
      _waterVehiclePrefab.transform);
    var floatCollider = PrefabRegistryController.Instantiate(floatColliderComponent,
      _waterVehiclePrefab.transform);

    onboardCollider.name = PrefabNames.VehicleOnboardCollider;
    floatCollider.name = PrefabNames.WaterVehicleFloatCollider;
    blockingCollider.name = PrefabNames.VehicleBlockingCollider;

    blockingCollider.transform.SetParent(_waterVehiclePrefab.transform);
    onboardCollider.transform.SetParent(_waterVehiclePrefab.transform);
    floatCollider.transform.SetParent(_waterVehiclePrefab.transform);

    floatCollider.transform.localScale = new Vector3(1f, 1f, 1f);
    blockingCollider.transform.localScale = new Vector3(1f, 1f, 1f);
    blockingCollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;

    var rigidbody = _waterVehiclePrefab.AddComponent<Rigidbody>();
    rigidbody.mass = 2000f;
    rigidbody.angularDrag = 0.1f;
    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
    rigidbody.useGravity = true;
    rigidbody.automaticInertiaTensor = true;
    rigidbody.automaticCenterOfMass = true;
    rigidbody.isKinematic = false;
    // setting to true makes the shadows not vibrate, it may be related to kinematic items triggering too many re-renders or the kinematic item needs to be moved lower
    // rigidbody.isKinematic = false;

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    _waterVehiclePrefab.AddComponent<ValheimShipControls>();
    var shipInstance = _waterVehiclePrefab.AddComponent<VVShip>();
    shipInstance.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;

    shipInstance.m_floatcollider = floatColliderComponent.GetComponentInChildren<BoxCollider>();
    shipInstance.FloatCollider = floatColliderComponent.GetComponentInChildren<BoxCollider>();

    var zSyncTransform = _waterVehiclePrefab.AddComponent<ZSyncTransform>();
    zSyncTransform.m_syncPosition = true;
    zSyncTransform.m_syncBodyVelocity = true;

    // wearntear may need to be removed or tweaked
    _waterVehiclePrefab.AddComponent<WearNTear>();
    var woodWNT = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(_waterVehiclePrefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt, WearNTear.MaterialType.HardWood);
    // wnt.m_colliders = woodWNT.m_colliders;
    // wnt.m_onDamaged += woodWNT.m_onDamaged;
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

    pieceManager.AddPiece(new CustomPiece(_waterVehiclePrefab, true, new PieceConfig
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
}