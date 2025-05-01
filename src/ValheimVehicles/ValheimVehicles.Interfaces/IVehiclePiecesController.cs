using ValheimVehicles.Components;

namespace ValheimVehicles.Interfaces;

public interface IVehiclePiecesController
{
  // this may need to be omitted
  public void CleanUp();
  public VehicleBaseController VehicleInstance { get; }
}