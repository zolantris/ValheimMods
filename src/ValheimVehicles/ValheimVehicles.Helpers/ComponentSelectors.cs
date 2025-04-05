using System.Diagnostics.CodeAnalysis;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Helpers;

/// <summary>
/// For cleaning up logic and selectors
/// </summary>
public static class ComponentSelectors
{
  public static VehiclePiecesController? GetVehiclePiecesController(VehicleShip? m_vehicle)
  {
    if (m_vehicle != null && m_vehicle.PiecesController != null)
    {
      return m_vehicle.PiecesController;
    }

    return null;
  }

  public static bool TryGetVehiclePiecesController(
    VehicleShip? vehicle,
    [NotNullWhen(true)] out VehiclePiecesController? controller)
  {
    if (vehicle != null && vehicle.PiecesController != null)
    {
      controller = vehicle.PiecesController;
      return true;
    }

    controller = null;
    return false;
  }
}