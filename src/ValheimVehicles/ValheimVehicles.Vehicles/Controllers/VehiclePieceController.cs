using System.Collections.Generic;

namespace ValheimVehicles.Vehicles.Controllers;

public class VehiclePieceController
{
  // public ActiveZonesWithVehicles

  // Active Vehicles
  public List<ZDO> activeVehicles;
  public static Dictionary<int, List<ZDO>> m_allPieces = new();
}