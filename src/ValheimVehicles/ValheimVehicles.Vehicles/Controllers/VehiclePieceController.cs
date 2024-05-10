using System.Collections.Generic;

namespace ValheimVehicles.Vehicles.Controllers;

public class VehiclePieceController
{
  // public ActiveZonesWithVehicles

  // Active Vehicles
  public static List<ZDO> activeVehicles;
  public static Dictionary<int, List<ZDO>> AllPieces = new();
  public static Dictionary<int, List<ZDO>> PendingPieces = new();
  public static Dictionary<int, List<ZDO>> ActivePieces = new();

  private Dictionary<int, List<ZDO>> pieces = new();
}