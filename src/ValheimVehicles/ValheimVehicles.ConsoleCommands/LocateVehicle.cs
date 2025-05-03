using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.ConsoleCommands;

public abstract class LocateVehicle
{
  private static int LocateVehicleShips()
  {
    var ships = UnityEngine.Object.FindObjectsOfType<VehicleManager>();

    return ships.ToList().Count;
  }

  public static bool LocateAllVehicles()
  {
    var isValid = false;

    if (LocateVehicleShips() > 0) isValid = true;

    return isValid;
  }
}