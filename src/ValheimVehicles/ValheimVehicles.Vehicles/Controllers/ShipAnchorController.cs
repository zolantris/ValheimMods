using System;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Vehicles.Controllers;

public class ShipAnchorController : AnchorMechanismController
{

  public override void OnAnchorStateChange(AnchorState newState)
  {
    switch (newState)
    {
      case AnchorState.Idle:
        break;
      case AnchorState.Dropping:
        var position = anchorRopeAttachStartPoint.position;
        maxDropDistance = position.y - ZoneSystem.instance.GetGroundHeight(position);
        break;
      case AnchorState.Dropped:
        break;
      case AnchorState.Reeling:
        break;
      case AnchorState.ReeledIn:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
    }
  }
  
  public void OnAnchorRaise()
  {
    
  }
  
  public void OnAnchorDrop()
  {
    
  }
}