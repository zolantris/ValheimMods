using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class MechanismPrefabs : RegisterPrefab<MechanismPrefabs>
{
  private void RegisterPowerStorageEitr()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_Power_Storage_Eitr,
      LoadValheimVehicleAssets.Mechanism_Power_Storage_Eitr);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Storage_Eitr, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_storage_eitr",
      Description = "$valheim_vehicles_mechanism_power_storage_eitr_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Storage_Icon)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Storage_Eitr, prefab);

    // main toggle switch.
    prefab.AddComponent<PowerStorageComponentIntegration>();
    prefab.AddComponent<PowerHoverComponent>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMetal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Eitr"
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMarble",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterPowerSourceEitr()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_Power_Source_Eitr,
      LoadValheimVehicleAssets.Mechanism_Power_Source_Eitr);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Source_Eitr, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_source_eitr",
      Description = "$valheim_vehicles_mechanism_power_source_eitr_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .Power_Source_Icon)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Source_Eitr, prefab);

    // todo set values based on a config for these prefabs.
    var powerSource = prefab.AddComponent<PowerSourceComponentIntegration>();
    var powerStorage = prefab.AddComponent<PowerStorageComponentIntegration>();
    prefab.AddComponent<PowerHoverComponent>();

    powerSource.Logic.IsStorage = true;
    powerStorage.Logic.IsSource = true;

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 12,
          Item = "BlackMetal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "Eitr",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMarble",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterPowerChargePlate()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_Power_Consumer_Charge_Plate,
      LoadValheimVehicleAssets.Mechanism_Power_Activator_Plate);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Consumer_Charge_Plate, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_charge_plate",
      Description = "$valheim_vehicles_mechanism_power_charge_plate_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .MechanismActivatorPlate)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Consumer_Charge_Plate, prefab);
    prefab.AddComponent<PowerConduitPlateChargeComponentIntegration>();
    prefab.AddComponent<PowerConduitHover>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMetal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Eitr",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterPowerDrainPlate()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_Power_Consumer_Drain_Plate,
      LoadValheimVehicleAssets.Mechanism_Power_Activator_Plate);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Consumer_Drain_Plate, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_drain_plate",
      Description = "$valheim_vehicles_mechanism_power_drain_plate_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .MechanismActivatorPlate)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Consumer_Drain_Plate, prefab);

    prefab.AddComponent<PowerConduitPlateDrainComponentIntegration>();
    prefab.AddComponent<PowerConduitHover>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMetal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Eitr",
          Recover = true
        }
      ]
    }));
  }

  // private void RegisterCoalEngine()
  // {
  //   var prefab = PrefabManager.Instance.CreateClonedPrefab(
  //     PrefabNames.Mechanism_Power_Source_Coal,
  //     LoadValheimVehicleAssets.Mechanism_Power_Source_Eitr);
  //
  //   PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
  //   PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Source_Coal, new PrefabRegistryHelpers.PieceData
  //   {
  //     Name = "$valheim_vehicles_mechanism_engine_coal",
  //     Description = "$valheim_vehicles_mechanism_engine_coal_desc",
  //     Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
  //       .PowerSourceCoal)
  //   });
  //   PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Source_Coal, prefab);
  //
  //   // main toggle switch.
  //   prefab.AddComponent<VehicleEngineIntegration>();
  //
  //   PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
  //   {
  //     PieceTable = PrefabRegistryController.GetPieceTableName(),
  //     Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
  //     Enabled = true,
  //     Requirements =
  //     [
  //       new RequirementConfig
  //       {
  //         Amount = 2,
  //         Item = "Iron",
  //         Recover = true
  //       },
  //       new RequirementConfig
  //       {
  //         Amount = 4,
  //         Item = "Wood",
  //         Recover = true
  //       }
  //     ]
  //   }));
  // }


  /// <summary>
  /// Todo an alternative way to add any command via placement. The item must not have a ZDO and must be a Temp netview.
  /// </summary>
  private static void CreateCommandPrefabAction()
  {
    var prefab = PrefabManager.Instance.CreateEmptyPrefab(
      "ValheimVehicles_CommandsMenuToggleOnPlace", false);

    PrefabRegistryHelpers.AddTempNetView(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Pylon, prefab);
  }

  private void RegisterPowerPylonPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_Power_Pylon,
      LoadValheimVehicleAssets.Mechanism_PowerPylon);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_Power_Pylon, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_power_pylon",
      Description = "$valheim_vehicles_mechanism_power_pylon_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .PowerPylon)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_Power_Pylon, prefab);

    // main toggle switch.
    prefab.AddComponent<PowerPylonComponentIntegration>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 2,
          Item = "BlackMetal",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "BlackMarble",
          Recover = true
        }
      ]
    }));
  }


  // ToggleSwitch and possibly a register multi-level switches
  private void RegisterToggleSwitch()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_ToggleSwitch,
      LoadValheimVehicleAssets.Mechanism_Switch);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab, AnimatedLeverMechanism.GetSnappointsContainer(prefab));
    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_ToggleSwitch, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_toggle_switch",
      Description = "$valheim_vehicles_mechanism_toggle_switch_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .MechanismSwitch)
    });

    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_ToggleSwitch, prefab);

    // main toggle switch.
    prefab.AddComponent<MechanismSwitch>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Power),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  public override void OnRegister()
  {
    RegisterToggleSwitch();
    RegisterPowerPylonPrefab();
    // RegisterCoalEngine();
    RegisterPowerSourceEitr();
    RegisterPowerStorageEitr();

    RegisterPowerDrainPlate();
#if DEBUG
    RegisterPowerChargePlate();
#endif
  }
}