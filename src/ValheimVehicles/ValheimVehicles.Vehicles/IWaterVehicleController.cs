using UnityEngine.UIElements;

namespace ValheimVehicles.Vehicles;

public interface IWaterVehicleController : IBaseVehicleController
{
  public WaterVehicleFlags VehicleFlags { get; }
  public ZSyncTransform m_zsync { get; set; }
  public float m_targetHeight { get; set; }
  public void SendSetAnchor(bool state);
  public void Descent();
  public void Ascend();
  public WaterVehicleController Instance { get; }
}