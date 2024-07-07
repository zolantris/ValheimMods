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
public class VehicleSwitchPrefab : IRegisterPrefab
{
  public static readonly VehicleSwitchPrefab Instance = new();

  // ToggleSwitch and possibly a register multi-level switches
  private void RegisterLeverSwitch(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var prefab = prefabManager.CreateClonedPrefab(PrefabNames.VehicleLeverSwitch,
      LoadValheimVehicleAssets.VehicleSwitchAsset);

    prefab.AddComponent<LeverComponent>();
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.VehicleLeverSwitch, prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
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

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterLeverSwitch(prefabManager, pieceManager);
  }
}