using UnityEngine;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IVehicleShip
{
  public IWaterVehicleController VehicleController { get; }
  public VehicleMovementController MovementController { get; }
  public Transform ControlGuiPosition { get; set; }
  public VehicleShip? Instance { get; }
  public ZNetView NetView { get; }
}