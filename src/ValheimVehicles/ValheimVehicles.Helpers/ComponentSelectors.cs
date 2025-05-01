using System.Diagnostics.CodeAnalysis;
using ValheimVehicles.Controllers;
using ValheimVehicles.Components;

namespace ValheimVehicles.Helpers;

/// <summary>
/// For cleaning up logic and selectors
/// </summary>
public static class ComponentSelectors
{
  public static VehiclePiecesController? GetVehiclePiecesController(VehicleBaseController? m_vehicle)
  {
    if (m_vehicle != null && m_vehicle.PiecesController != null)
    {
      return m_vehicle.PiecesController;
    }

    return null;
  }

  public static bool TryGetVehiclePiecesController(
    VehicleBaseController? vehicle,
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