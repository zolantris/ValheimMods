#region

  using System.Collections.Generic;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.SharedScripts.Validation;

#endregion

  namespace ValheimVehicles.Helpers;

  public static class VehicleSharedPropertiesUtils
  {
    private static void BindControllers(IVehicleSharedProperties from, IVehicleSharedProperties to)
    {
      to.PiecesController = from.PiecesController;
      to.MovementController = from.MovementController;
      to.VehicleConfigSync = from.VehicleConfigSync;
      to.OnboardController = from.OnboardController;
      to.WheelController = from.WheelController;
      to.Manager = from.Manager;
      to.m_nview = from.m_nview;
    }

    /// <summary>
    /// The main method that binds to all controllers. These controllers values are then considered not-null.
    /// </summary>
    /// <param name="fromController"></param>
    /// <param name="toControllers"></param>
    /// <returns></returns>
    public static bool BindAllControllers(IVehicleSharedProperties fromController, List<IVehicleSharedProperties> toControllers)
    {
      if (!ClassValidator.ValidateRequiredNonNullFields(fromController)) return false;
      foreach (var to in toControllers)
      {
        BindControllers(fromController, to);
      }

      return true;
    }
  }