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
  public static CustomPieceTable? VehicleHammerTable { get; set; }

  // optional todo: allow re-ordering categories
  // vehicles is kept last - it will not be used unless when placing new vehicles.
  public static readonly string[] categories = [VehicleHammerTableCategories.Tools, VehicleHammerTableCategories.Structure, VehicleHammerTableCategories.Propulsion, VehicleHammerTableCategories.Vehicles];

  public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

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