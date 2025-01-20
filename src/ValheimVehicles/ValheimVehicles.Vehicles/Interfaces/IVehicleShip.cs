using UnityEngine;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IVehicleShip
{
  public VehiclePiecesController? PiecesController { get; }
  public VehicleMovementController? MovementController { get; }
  public VehicleOnboardController? OnboardController { get; }
  public Rigidbody? MovementControllerRigidbody { get; }
  public Transform ControlGuiPosition { get; set; }
  public VehicleShip? Instance { get; }
  public ZNetView? NetView { get; }
  public int PersistentZdoId { get; }
}