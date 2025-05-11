using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class CustomVehicleMastRegistry : RegisterPrefab<CustomVehicleMastRegistry>
{
  private static void RegisterMast(string mastTier)
  {
    var mastAsset = LoadValheimVehicleAssets.GetMastVariant(mastTier);
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.GetMastByLevelName(mastTier), mastAsset);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.GetMastByLevelName(mastTier), prefab);
    PrefabRegistryHelpers.SetWearNTear(prefab);

    prefab.AddComponent<MastComponent>();

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 20,
            Item = "RoundLog",
            Recover = true
          }
        ]
      }));
  }

  public override void OnRegister()
  {
    RegisterMast(PrefabNames.MastLevels.ONE);
    RegisterMast(PrefabNames.MastLevels.TWO);
    RegisterMast(PrefabNames.MastLevels.THREE);
  }
}