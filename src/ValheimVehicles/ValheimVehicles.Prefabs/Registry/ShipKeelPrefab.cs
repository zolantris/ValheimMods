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
      PieceTable = "Hammer",
      Description = pieceTranslations.Description,
      Icon = LoadValheimVehicleSharedAssets.SharedSprites.GetSprite(SpriteNames.ShipKeel),
      Category = PrefabNames.ValheimRaftMenuName,
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