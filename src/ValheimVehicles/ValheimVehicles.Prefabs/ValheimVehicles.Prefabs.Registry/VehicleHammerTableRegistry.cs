using System.Linq;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Validation;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerTableRegistry : GuardedRegistry<VehicleHammerTableRegistry>
{
  public static CustomPieceTable? VehicleHammerTable { get; private set; }

  // optional todo: allow re-ordering categories
  // vehicles is kept last - it will not be used unless when placing new vehicles.
  private static readonly string[] categories = [VehicleHammerTableCategories.Structure, VehicleHammerTableCategories.Tools, VehicleHammerTableCategories.Propulsion, VehicleHammerTableCategories.Vehicles];

  public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

  private static void RegisterVehicleHammerTable()
  {
    var vehicleHammerTableConfig = new PieceTableConfig
    {
      CanRemovePieces = true,
      UseCategories = false,
      UseCustomCategories = true,
      CustomCategories = categories
    };

    VehicleHammerTable = new CustomPieceTable(VehicleHammerTableName, vehicleHammerTableConfig);

    var success = PieceManager.Instance.AddPieceTable(VehicleHammerTable);

    if (!success)
    {
      LoggerProvider.LogError("VehicleHammerTable failed to be added. Falling back with original hammer table for all items. This is a bug. This could break your game. Please report this bug.");
      VehicleHammerTable = null;
    }
  }

  public static void AddRepairToolToPieceTable(PieceTable table)
  {
    var repairToolPrefab = PrefabManager.Instance.GetPrefab("RepairTool");
    if (repairToolPrefab == null)
    {
      Jotunn.Logger.LogError("RepairTool prefab not found in ObjectDB!");
      return;
    }

    // Add to each defined category in the table
    foreach (var category in table.m_categories)
    {
      // Avoid duplicates
      var alreadyExists = table.m_pieces.Any(p =>
      {
        var piece = p.GetComponent<Piece>();
        return piece && piece.m_name == "RepairTool" && piece.m_category == category;
      });

      if (alreadyExists)
        continue;

      // Clone the RepairTool prefab so each can have its own category
      var repairClone = Object.Instantiate(repairToolPrefab);
      var repairPiece = repairClone.GetComponent<Piece>();
      repairPiece.m_category = category;

      // Optional: Give each one a unique name so it's not overridden
      repairPiece.m_name = $"RepairTool_{category}";

      // Add it to the piece table
      table.m_pieces.Insert(0, repairClone);
      Jotunn.Logger.LogInfo($"Added RepairTool to VehicleHammerTable category {category}");
    }
  }

  public override void OnRegister()
  {
    RegisterVehicleHammerTable();

    if (VehicleHammerTable != null)
    {
      AddRepairToolToPieceTable(VehicleHammerTable.PieceTable);
    }
  }
}