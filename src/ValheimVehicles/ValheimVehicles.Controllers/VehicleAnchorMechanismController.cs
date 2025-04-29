using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Controllers;

/// <summary>
/// An integration level component, meant to work with Valheim specific content / apis
/// </summary>
public class VehicleAnchorMechanismController : AnchorMechanismController
{
  public const float maxAnchorDistance = 40f;

  public static void SyncHudAnchorValues()
  {
    HideAnchorTimer = HudConfig.HudAnchorMessageTimer.Value;
    HasAnchorTextHud = HudConfig.HudAnchorTextAboveAnchors.Value;
    foreach (var anchorMechanismController in Instances)
      anchorMechanismController.anchorTextSize =
        HudConfig.HudAnchorTextSize.Value;
  }

  public override void Awake()
  {
    base.Awake();
    CanUseHotkeys = false;
  }

  public VehicleMovementController? MovementController;

  public override void FixedUpdate()
  {
    base.FixedUpdate();

    if (currentState == AnchorState.Lowering) UpdateDistanceToGround();
  }

  public void UpdateDistanceToGround()
  {
    var position = anchorRopeAttachStartPoint.position;
    var distanceFromAnchorToGround =
      position.y - ZoneSystem.instance.GetGroundHeight(position);
    anchorDropDistance = Mathf.Clamp(distanceFromAnchorToGround, 1f,
      maxAnchorDistance);
  }

  public static string GetCurrentStateTextStatic(AnchorState anchorState, bool isLandVehicle)
  {
    if (isLandVehicle)
    {
      return anchorState == AnchorState.Anchored ? ModTranslations.breakingText : ModTranslations.idleText;
    }

    return anchorState switch
    {
      AnchorState.Idle => "Idle",
      AnchorState.Lowering => ModTranslations.loweringText,
      AnchorState.Anchored => ModTranslations.anchoredText,
      AnchorState.Reeling => ModTranslations.reelingText,
      AnchorState.Recovered => ModTranslations.Anchor_RecoveredAnchorText,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public override string GetCurrentStateText()
  {
    var isLandVehicle = MovementController != null && MovementController.VehicleInstance is
    {
      IsLandVehicle: true
    };
    return GetCurrentStateTextStatic(currentState, isLandVehicle);
  }

  public override void OnAnchorStateChange(AnchorState newState)
  {
    // No callbacks for anchor when flying. You can only Reel-in, or reel upwards.
    if (MovementController != null && MovementController.IsFlying())
    {
      if (currentState != AnchorState.Recovered)
        UpdateAnchorState(AnchorState.Reeling, GetCurrentStateText());
      else
        return;
    }

    switch (newState)
    {
      case AnchorState.Idle:
        break;
      case AnchorState.Lowering:
        break;
      case AnchorState.Anchored:
        break;
      case AnchorState.Reeling:
        break;
      case AnchorState.Recovered:
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
    }

    if (MovementController != null && MovementController.m_nview.IsOwner())
      MovementController.SendSetAnchor(newState);
  }
}