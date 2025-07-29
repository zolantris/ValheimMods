using System.Linq;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Validation;
using Zolantris.Shared;
namespace ValheimVehicles.Prefabs.Registry;

public class VehicleHammerTableRegistry : GuardedRegistry<VehicleHammerTableRegistry>
{
  public static CustomPieceTable? VehicleHammerTable { get; private set; }

  // optional todo: allow re-ordering categories
  // vehicles is kept last - it will not be used unless when placing new vehicles.
  private static readonly string[] categories = [VehicleHammerTableCategories.Tools, VehicleHammerTableCategories.Structure, VehicleHammerTableCategories.Power, VehicleHammerTableCategories.Propulsion, VehicleHammerTableCategories.Vehicles];

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

  public override void OnRegister()
  {
    RegisterVehicleHammerTable();
  }
}