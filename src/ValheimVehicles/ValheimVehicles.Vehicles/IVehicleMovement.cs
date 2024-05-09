using ValheimVehicles.Vehicles.Enums;

namespace ValheimVehicles.Vehicles;

public interface IVehicleMovement
{
  public void SendSetAnchor(bool state);
  public void Descend();
  public void Ascend();
  public VehicleMovementFlags MovementFlags { get; }
  public float TargetHeight { get; set; }
}