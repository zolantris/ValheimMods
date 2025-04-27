using ValheimVehicles.SharedScripts;
using ValheimVehicles.Enums;

namespace ValheimVehicles.Interfaces;

public interface IVehicleMovement
{
  public void SendSetAnchor(AnchorState state);
  public void Descend();
  public void Ascend();
  // public VehicleMovementFlags MovementFlags { get; }
  public float TargetHeight { get; }
}