using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// An integration level component, meant to work with Valheim specific content / apis
/// </summary>
public class VehicleAnchorMechanismController : AnchorMechanismController
{
  public static float maxAnchorDistance = 40f;

  public void Awake()
  {
    CanUseHotkeys = false;
  }

  public VehicleMovementController? MovementController;

  public override void FixedUpdate()
  {
    base.FixedUpdate();
    
    if (currentState == AnchorState.Dropping)
    {
      UpdateDistanceToGround();
    }
  }

  public void UpdateDistanceToGround()
  {
    var position = anchorRopeAttachStartPoint.position;
    var distanceFromAnchorToGround =  position.y - ZoneSystem.instance.GetGroundHeight(position);
    anchorDropDistance = Mathf.Clamp(distanceFromAnchorToGround, 1f,
      maxAnchorDistance);
  }

  public override void OnAnchorStateChange(AnchorState newState)
  {
    
    // No callbacks for anchor when flying. You can only Reel-in, or reel upwards.
    if (MovementController != null && MovementController.IsFlying())
    {
      if (currentState != AnchorState.ReeledIn)
      {
        UpdateAnchorState(AnchorState.Reeling);
      }
      else
      {
        return;
      }
    }
    
    switch (newState)
    {
      case AnchorState.Idle:
        break;
      case AnchorState.Dropping:
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

    if (MovementController != null)
    {
      MovementController.SendSetAnchor(newState);
    }
  }
}