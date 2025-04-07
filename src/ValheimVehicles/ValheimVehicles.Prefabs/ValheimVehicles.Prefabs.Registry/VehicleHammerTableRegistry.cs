using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Validation;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerTableRegistry : GuardedRegistry
{
  private static CustomPieceTable? VehicleHammerTable { get; set; }

  public static class VehicleHammerTableCategories
  {
    public const string Vehicles = "Vehicles";
    public const string Tools = "Tools";
    public const string Propulsion = "Propulsion";
    public const string Structure = "Structure";
  }

  // optional todo: allow re-ordering categories
  // vehicles is kept last - it will not be used unless when placing new vehicles.
  public static readonly string[] categories = [VehicleHammerTableCategories.Tools, VehicleHammerTableCategories.Structure, VehicleHammerTableCategories.Propulsion, VehicleHammerTableCategories.Vehicles];

  [UsedImplicitly]
  public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

  public static PieceTable GetPieceTable()
  {
    if (VehicleHammerTable != null) return VehicleHammerTable.PieceTable;

    var allTables = PieceManager.Instance.GetPieceTables();

    // for debugging names.
    foreach (var pieceTable in allTables)
    {
      if (pieceTable == null) continue;
      LoggerProvider.LogDebug(pieceTable.name);
    }

    return PieceManager.Instance.GetPieceTable("Valheim");
  }

  private static void RegisterVehicleHammerTable()
  {
    var runeTable = new PieceTableConfig
    {
      CanRemovePieces = false,
      UseCategories = false,
      UseCustomCategories = true,
      CustomCategories = categories
    };

    VehicleHammerTable = new CustomPieceTable(VehicleHammerTableName, runeTable);

    var success = PieceManager.Instance.AddPieceTable(VehicleHammerTable);

    if (!success)
    {
      VehicleHammerTable = null;
      LoggerProvider.LogError("VehicleHammerTable failed to be added. Falling back with original hammer table for all items. This is a bug. This could break your game. Please report this bug.");
    }
  }

  public override void OnRegister()
  {
    RegisterVehicleHammerTable();
  }
}