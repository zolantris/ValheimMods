using System;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;

using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class VehiclePrefabs : IRegisterPrefab
{
  public static readonly VehiclePrefabs Instance = new();

  /**
   * todo it's possible this all needs to be done in the Awake method to safely load valheim.
   * Should test this in development build of valheim
   */
  private static GameObject CreateWaterVehiclePrefab(
    GameObject prefab)
  {
    // top level netview must be passed along to other components from VehicleShip
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab, true);
    PrefabRegistryHelpers.GetOrAddMovementZSyncTransform(prefab);

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */

    var shipInstance = prefab.AddComponent<VehicleShip>();
    shipInstance.gameObject.layer = LayerHelpers.CustomRaftLayer;

    // todo fix ship water effects so they do not cause ship materials to break

    var waterEffects =
      Object.Instantiate(LoadValheimAssets.shipWaterEffects, prefab.transform);
    waterEffects.name = PrefabNames.VehicleShipEffects;
    var shipEffects = waterEffects.GetComponent<ShipEffects>();
    var vehicleShipEffects = waterEffects.AddComponent<VehicleShipEffects>();
    VehicleShipEffects.CloneShipEffectsToInstance(vehicleShipEffects,
      shipEffects);
    Object.Destroy(shipEffects);

    vehicleShipEffects.transform.localPosition = new Vector3(0, -2, 0);
    shipInstance.ShipEffectsObj = vehicleShipEffects.gameObject;
    shipInstance.ShipEffects = vehicleShipEffects;

    // WearNTear may need to be removed or tweaked
    prefab.AddComponent<WearNTear>();
    var woodWnt = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt,
      WearNTear.MaterialType.HardWood);

    wnt.m_onDestroyed += woodWnt.m_onDestroyed;
    // triggerPrivateArea will damage enemies/pieces when within it
    wnt.m_triggerPrivateArea = true;

    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    return prefab;
  }

  private static void RegisterWaterVehicleShipPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.WaterVehicleShip,
      LoadValheimVehicleAssets.VehicleShipAsset);
    var waterVehiclePrefab = CreateWaterVehiclePrefab(prefab);
    // top level netview must be passed along to other components from VehicleShip
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab, true);
    PrefabRegistryHelpers.GetOrAddMovementZSyncTransform(prefab);

    var woodWnt = LoadValheimAssets.woodFloorPiece.GetComponent<WearNTear>();
    var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 1, true);
    PrefabRegistryHelpers.SetWearNTearSupport(wnt,
      WearNTear.MaterialType.HardWood);

    wnt.m_onDestroyed += woodWnt.m_onDestroyed;
    // triggerPrivateArea will damage enemies/pieces when within it
    wnt.m_triggerPrivateArea = true;

    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    wnt.enabled = false;

    /*
     * ShipControls were a gameObject with a script attached to them. This approach directly attaches the script instead of having the rudder show.
     */
    
    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.WaterVehicleShip,
        prefab);
    piece.m_primaryTarget = true;
    piece.m_randomTarget = true;
    piece.m_targetNonPlayerBuilt = true;
    piece.m_waterPiece = true;
    piece.m_noClipping = true;
    piece.m_canRotate = true;

    PieceManager.Instance.AddPiece(new CustomPiece(waterVehiclePrefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Vehicles),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 72,
            Item = "Wood",
            Recover = false
          }
        ]
      }));
  }

  public static void RegisterLandVehiclePrefab()
  {
    var landVehiclePrefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.LandVehicle,
      LoadValheimVehicleAssets.VehicleLand);

    CreateWaterVehiclePrefab(landVehiclePrefab);
    var vehicleShip = landVehiclePrefab.GetComponent<VehicleShip>();
    vehicleShip.IsLandVehicleFromPrefab = true;
    vehicleShip.IsLandVehicle = true;

    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.LandVehicle,
        landVehiclePrefab);
    piece.m_primaryTarget = true;
    piece.m_randomTarget = true;
    piece.m_targetNonPlayerBuilt = true;
    piece.m_waterPiece = false;
    piece.m_noClipping = true;
    piece.m_canRotate = true;


    PieceManager.Instance.AddPiece(new CustomPiece(landVehiclePrefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Vehicles),
        Enabled = PrefabConfig.EnableLandVehicles.Value,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 12,
            Item = "BlackMetal",
            Recover = false
          },
          new RequirementConfig
          {
            Amount = 32,
            Item = "Wood",
            Recover = false
          },
          new RequirementConfig
          {
            Amount = 12,
            Item = "Tar",
            Recover = false
          }
        ]
      }));
  }

  private static void RegisterNautilusVehicleShipPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.Nautilus,
      LoadValheimVehicleAssets.ShipNautilus);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var piece =
      PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Nautilus, prefab);
    // var wnt = PrefabRegistryHelpers.SetWearNTear(prefab, 3);
    // wnt.m_health = 7000f;
    // wnt.m_burnable = false;
    // wnt.m_supports = true;
    // wnt.m_damages = new HitData.DamageModifiers()
    // {
    //   m_slash = HitData.DamageModifier.Resistant,
    //   m_blunt = HitData.DamageModifier.Resistant,
    //   m_pierce = HitData.DamageModifier.VeryWeak,
    //   m_lightning = HitData.DamageModifier.VeryWeak,
    //   m_poison = HitData.DamageModifier.Immune,
    //   m_frost = HitData.DamageModifier.Immune,
    //   m_fire = HitData.DamageModifier.Resistant,
    // };

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 200,
          Item = "Bronze",
          Recover = true
        }
      ]
    }));
  }

  public void OnExperimentalPrefabSettingsChange(object sender,
    EventArgs eventArgs)
  {
    if (PrefabManager.Instance == null || PieceManager.Instance == null) return;
    RegisterNautilus();
  }

  public void RegisterNautilus()
  {
    var shouldEnable =
      PrefabConfig.AllowExperimentalPrefabs.Value;

    var nautilus = PrefabManager.Instance?.GetPrefab(PrefabNames.Nautilus);

    if (shouldEnable && !nautilus) RegisterNautilusVehicleShipPrefab();

    if (!shouldEnable && nautilus)
    {
      var piece = PieceManager.Instance?.GetPiece(PrefabNames.Nautilus);
      if (piece != null) piece.Piece.enabled = false;
    }
  }

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterWaterVehicleShipPrefab();

    // registers only for v3 releases.
    // if (ValheimRaftPlugin.Version.StartsWith("3")) 

    RegisterLandVehiclePrefab();

    RegisterNautilus();
  }
}