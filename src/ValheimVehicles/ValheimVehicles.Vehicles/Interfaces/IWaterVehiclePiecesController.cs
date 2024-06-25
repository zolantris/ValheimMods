using UnityEngine.UIElements;

namespace ValheimVehicles.Vehicles.Interfaces;

public interface IWaterVehiclePiecesController : IBaseVehicleController
{
  public WaterVehiclePiecesController Instance { get; }
}