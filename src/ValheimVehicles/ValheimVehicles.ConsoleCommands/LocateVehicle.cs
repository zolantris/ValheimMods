using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.ConsoleCommands;

public abstract class LocateVehicle
{
  private static bool deprecatedSearches = true;

  private static int LocateMBRaftVehicles()
  {
    var ships = UnityEngine.Object.FindObjectsOfType<Ship>();
    // var mbRaftShips = ships.Contains()
    List<Ship> mbShips = [];
    mbShips.AddRange(from ship in ships
      let isMBRaft = ship.name.Contains(PrefabNames.MBRaft)
      select ship);
    return mbShips.Count;
  }

  private static int LocateVehicleShips()
  {
    var ships = UnityEngine.Object.FindObjectsOfType<VehicleShip>();

    return ships.ToList().Count;
  }

  public static bool LocateAllVehicles()
  {
    var isValid = false;
    if (deprecatedSearches)
    {
      if (LocateMBRaftVehicles() > 0) isValid = true;
    }

    if (LocateVehicleShips() > 0) isValid = true;

    return isValid;
  }
}