#region

  using System.Collections.Generic;
  using ValheimVehicles.Interfaces;

#endregion

namespace ValheimVehicles.Helpers;

public static class VehicleSharedPropertiesUtils
{
  public static void BindControllers(IVehicleSharedProperties from, IVehicleSharedProperties to)
  {
    to.PiecesController = from.PiecesController;
    to.MovementController = from.MovementController;
    to.VehicleConfigSync = from.VehicleConfigSync;
    to.OnboardController = from.OnboardController;
    to.WheelController = from.WheelController;
    to.BaseController = from.BaseController;
    to.NetView = from.NetView;
  }

  public static void BindAllControllers(IVehicleSharedProperties from, List<IVehicleSharedProperties> toControllers)
  {
    foreach (var to in toControllers)
    {
      BindControllers(from, to);
    }
  }
}