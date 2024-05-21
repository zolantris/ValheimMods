using UnityEngine.UIElements;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IWaterVehicleController : IBaseVehicleController
{
  public WaterVehicleController Instance { get; }
}