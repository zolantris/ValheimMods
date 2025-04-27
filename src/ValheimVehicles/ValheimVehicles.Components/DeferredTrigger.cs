namespace ValheimVehicles.Components;

public interface IDeferredTrigger
{
  internal bool IsReady();
  internal bool isReadyForCollisions { get; set; }
  internal bool isRebuildingCollisions { get; set; }
}