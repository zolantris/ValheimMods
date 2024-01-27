namespace ValheimVehicles.Vehicles;

public interface IBaseVehicleController
{
  // this may need to be omitted
  public ZNetView m_nview { get; set; }
  public void CleanUp();
}