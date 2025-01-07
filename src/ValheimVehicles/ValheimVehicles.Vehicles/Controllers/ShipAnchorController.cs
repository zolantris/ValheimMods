using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Vehicles.Controllers;

public class ShipAnchorController : AnchorMechanismController
{
  public static float maxAnchorDistance = 40f;

  public override void OnAnchorStateChange(AnchorState newState)
  {
    switch (newState)
    {
      case AnchorState.Idle:
        break;
      case AnchorState.Dropping:
        var position = anchorRopeAttachStartPoint.position;
        var distanceFromAnchorToGround =  position.y - ZoneSystem.instance.GetGroundHeight(position);
        anchorDropDistance = Mathf.Clamp(distanceFromAnchorToGround, 1f,
          maxAnchorDistance);
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