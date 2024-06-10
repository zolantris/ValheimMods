using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IBaseVehicleController
{
  // this may need to be omitted
  public ZNetView m_nview { get; }
  public void CleanUp();
  public VehicleShip VehicleInstance { get; }
  public int PersistentZdoId { get; }
}