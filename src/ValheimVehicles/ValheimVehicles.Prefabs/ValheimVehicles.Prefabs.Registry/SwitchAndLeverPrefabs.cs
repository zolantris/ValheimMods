using Components;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class SwitchAndLeverPrefabs : IRegisterPrefab
{
  public static readonly SwitchAndLeverPrefabs Instance = new();

  // ToggleSwitch and possibly a register multi-level switches
  private void RegisterToggleSwitch(PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab = prefabManager.CreateClonedPrefab(
      PrefabNames.ToggleSwitch,
      LoadValheimVehicleAssets.MechanicalSwitch);
    var pieceTranslations =
      PrefabRegistryHelpers.PieceDataDictionary.GetValueSafe(PrefabNames
        .ToggleSwitch);

    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var piece = prefab.AddComponent<Piece>();
    piece.name = prefab.name;
    piece.m_description =
      "Toggle Switch - allows additional controls on vehicles";

    // main toggle switch.
    prefab.AddComponent<CustomToggleSwitch>();

    pieceManager.AddPiece(new CustomPiece(prefab, true, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Name = pieceTranslations.Name,
      Description = pieceTranslations.Description,
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames
        .VehicleSwitch),
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

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterToggleSwitch(prefabManager, pieceManager);
  }
}