using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class ExperimentalPrefabRegistry : RegisterPrefab<ExperimentalPrefabRegistry>
{

  public static void OnExperimentalPrefabSettingsChange(object sender,
    EventArgs eventArgs)
  {
    if (PrefabManager.Instance == null || PieceManager.Instance == null) return;
    RegisterNautilus();
  }

  public static bool HasRegisteredNautilus;

  private static void RegisterNautilusVehicleShipPrefab()
  {
    if (HasRegisteredNautilus) return;
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

    HasRegisteredNautilus = true;
  }

  public static void RegisterNautilus()
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

  public override void OnRegister()
  {
    RegisterNautilus();
  }
}