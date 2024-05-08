using UnityEngine.UIElements;

namespace ValheimVehicles.Vehicles;

public interface IWaterVehicleController : IBaseVehicleController
{
  public WaterVehicleController Instance { get; }
}