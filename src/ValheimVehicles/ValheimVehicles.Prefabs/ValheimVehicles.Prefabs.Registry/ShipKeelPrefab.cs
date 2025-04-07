using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Prefabs;

namespace Registry;

public class ShipKeelPrefab : IRegisterPrefab
{
  public static readonly ShipKeelPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(PrefabNames.ShipKeel,
        LoadValheimVehicleAssets.ShipKeelAsset);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab, prefab.transform);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.ShipKeel, prefab);

    var pieceTranslations =
      PrefabRegistryHelpers.PieceDataDictionary.GetValueSafe(PrefabNames.ShipKeel);


    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
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