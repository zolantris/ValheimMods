using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerItemRegistry : GuardedRegistry
{

  public static void RegisterVehicleHammer()
  {
    var hammerPrefab = PrefabManager.Instance.GetPrefab("VehicleHammerPrefab");
    if (!hammerPrefab)
    {
      LoggerProvider.LogError("VehicleHammerPrefab not found!");
      return;
    }

    var customItem = new CustomItem(hammerPrefab, true);

    // Assign custom icon
    var icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.VehicleHammer);
    if (icon != null)
    {
      customItem.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
    }

    var pieceTable = VehicleHammerTableRegistry.GetPieceTable();
    // Assign empty custom PieceTable
    if (pieceTable)
    {
      customItem.ItemDrop.m_itemData.m_shared.m_buildPieces = pieceTable;
    }
    else
    {
      LoggerProvider.LogWarning("VehicleHammerTable is null. Did you register it first?");
    }

    ItemManager.Instance.AddItem(customItem);
    LoggerProvider.LogMessage("Registered VehicleHammer with empty custom build menu.");
  }

  public override void OnRegister()
  {
    RegisterVehicleHammer();
  }
}