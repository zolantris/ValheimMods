using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Interfaces;

/// <summary>
/// All vehicle controllers should exist here.
/// </summary>
///
/// Todo: Sharing this interface
/// - This interface should exist on all Controllers so that each controller can access properties easily.
/// - Limitation: It may require adding interfaces to all Controllers so there is no reference cycle.
///
/// Todo: Non-Nullable Refactor
/// - We should wrap accessors of any GetComponent with a validation check to assert all values are not null.
/// - If this is false we do not return the component and instead return null.
/// - Then any deep accessors of the component are always truthy.
/// 
public interface IVehicleControllers
{
  public VehiclePiecesController? PiecesController { get; }
  public VehicleMovementController? MovementController { get; }
  public VehicleConfigSyncComponent? VehicleConfigSync { get; }
  public VehicleOnboardController? OnboardController { get; }
  public VehicleWheelController? WheelController { get; }
  public VehicleBaseController? BaseController { get; }
}