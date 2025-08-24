using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;

namespace Registry;

public class ShipKeelPrefab : RegisterPrefab<ShipKeelPrefab>
{
  public override void OnRegister()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(PrefabNames.ShipKeel,
        LoadValheimVehicleAssets.ShipKeelAsset);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab, prefab.transform);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipKeel, prefab);

    var pieceTranslations =
      PrefabRegistryHelpers.PieceDataDictionary.GetValueSafe(PrefabNames.ShipKeel);


    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Description = pieceTranslations.Description,
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.ShipKeel),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Structure),
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
}