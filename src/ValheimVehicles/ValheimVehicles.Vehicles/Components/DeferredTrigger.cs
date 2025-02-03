namespace ValheimVehicles.Vehicles;

public interface IDeferredTrigger
{
  internal bool IsReady();
  internal bool _isReadyForCollisions { get; set; }
  internal bool _isRebuildingCollisions { get; set; }
}