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
      PrefabNames.Mechanism_CoalEngine,
      LoadValheimVehicleAssets.Mechanism_Engine_Coal);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_CoalEngine, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_engine_coal",
      Description = "$valheim_vehicles_mechanism_engine_coal_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .CoalEngine)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_CoalEngine, prefab);

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

  /// <summary>
  /// Todo an alternative way to add any command via placement. The item must not have a ZDO and must be a Temp netview.
  /// </summary>
  private static void CreateCommandPrefabAction()
  {
    var prefab = PrefabManager.Instance.CreateEmptyPrefab(
      "ValheimVehicles_CommandsMenuToggleOnPlace", false);

    PrefabRegistryHelpers.AddTempNetView(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_ElectricPylon, prefab);
  }

  private void RegisterElectricPylonPrefab()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(
      PrefabNames.Mechanism_ElectricPylon,
      LoadValheimVehicleAssets.Mechanism_ElectricPylon);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.Mechanism_ElectricPylon, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_electric_pylon",
      Description = "$valheim_vehicles_mechanism_electric_pylon_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .ElectricPylon)
    });
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.Mechanism_ElectricPylon, prefab);

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