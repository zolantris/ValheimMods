#region

  using System;
  using System.Collections.Generic;
  using System.Text.RegularExpressions;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Enums;
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

    private static readonly Regex NonLandVehicleAllowedNullKeys = StringValidatorExtensions.GenerateRegexFromList([nameof(IVehicleSharedProperties.WheelController)]);

    private static Regex? GetSkipRegexpForVehicleType(VehicleVariant vehicleVariant)
    {
      switch (vehicleVariant)
      {
        case VehicleVariant.Land:
          return null;
        case VehicleVariant.Water:
        case VehicleVariant.Sub:
        case VehicleVariant.Air:
        case VehicleVariant.All:
        default:
          return NonLandVehicleAllowedNullKeys;
      }
    }

    /// <summary>
    /// The main method that binds to all controllers. These controller values are then considered not-null.
    /// </summary>
    public static bool BindAllControllers(IVehicleSharedProperties fromController, List<IVehicleSharedProperties> toControllers, VehicleVariant vehicleVariant)
    {
      // todo would require adding a validator ignore for most of VehicleManager.
      // if (!ClassValidator.ValidateRequiredNonNullFields<IVehicleSharedProperties>(fromController, null, GetSkipRegexpForVehicleType(vehicleVariant))) return false;
      foreach (var to in toControllers)
      {
        BindControllers(fromController, to);
      }

      return true;
    }
  }