using UnityEngine;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IVehicleShip
{
  public IWaterVehicleController? VehiclePiecesController { get; }
  public VehicleMovementController? MovementController { get; }
  public Transform ControlGuiPosition { get; set; }
  public VehicleShip? Instance { get; }
  public ZNetView? NetView { get; }
  public int PersistentZdoId { get; }
}