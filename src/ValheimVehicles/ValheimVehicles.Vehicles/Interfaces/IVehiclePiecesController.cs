using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IVehiclePiecesController
{
  // this may need to be omitted
  public void CleanUp();
  public VehicleShip VehicleInstance { get; }
}