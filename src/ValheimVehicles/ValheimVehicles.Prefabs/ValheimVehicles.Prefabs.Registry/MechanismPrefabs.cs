using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class MechanismPrefabs : RegisterPrefab<MechanismPrefabs>
{
  private void RegisterCoalEngine()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.CoalEngine,
      LoadValheimVehicleAssets.CoalEngine);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.CoalEngine, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_coal_engine",
      Description = "$valheim_vehicles_coal_engine_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .CoalEngine)
    });
    PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames.CoalEngine);

    // main toggle switch.
    prefab.AddComponent<VehicleEngineIntegration>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 2,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  private void RegisterElectricPylonPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.ElectricPylon,
      LoadValheimVehicleAssets.ElectricPylon);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.ElectricPylon, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_electric_pylon",
      Description = "$valheim_vehicles_electric_pylon_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ElectricPylon)
    });
    PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames.ElectricPylon);

    // main toggle switch.
    prefab.AddComponent<ElectricityPylonIntegration>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 2,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 4,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  // ToggleSwitch and possibly a register multi-level switches
  private void RegisterToggleSwitch()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.ToggleSwitch,
      LoadValheimVehicleAssets.MechanicalSwitch);
    var pieceTranslations =
      PrefabRegistryHelpers.PieceDataDictionary.GetValueSafe(PrefabNames
        .ToggleSwitch);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.GetPieceNameFromPrefab(PrefabNames.ToggleSwitch);


    // main toggle switch.
    prefab.AddComponent<CustomToggleSwitch>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
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
    RegisterElectricPylonPrefab();
    RegisterCoalEngine();
  }
}