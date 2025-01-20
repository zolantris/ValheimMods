using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles.Controllers;

/// <summary>
/// An integration level component, meant to work with Valheim specific content / apis
/// </summary>
public class VehicleAnchorMechanismController : AnchorMechanismController
{
  public const float maxAnchorDistance = 40f;
  private static bool hasLocalizedAnchorState = false;
  private static string recoveredAnchorText;
  private static string reelingText;
  private static string anchoredText;
  private static string loweringText;

  public static void setLocalizedStates()
  {
    if (hasLocalizedAnchorState) return;

    reelingText =
      Localization.instance.Localize("$valheim_vehicles_anchor_state_reeling");

    recoveredAnchorText =
      Localization.instance.Localize(
        "$valheim_vehicles_anchor_state_recovered");

    anchoredText =
      Localization.instance.Localize("$valheim_vehicles_anchor_state_anchored");

    loweringText =
      Localization.instance.Localize("$valheim_vehicles_anchor_state_lowering");
    hasLocalizedAnchorState = true;
  }

  public static void SyncHudAnchorValues()
  {
    HideAnchorTimer = HudConfig.HudAnchorMessageTimer.Value;
    HasAnchorTextHud = HudConfig.HudAnchorTextAboveAnchors.Value;
    foreach (var anchorMechanismController in Instances)
      anchorMechanismController.anchorTextSize =
        HudConfig.HudAnchorTextSize.Value;
  }

  public new void Awake()
  {
    base.Awake();
    setLocalizedStates();
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


  public override string GetCurrentStateText()
  {
    return currentState switch
    {
      AnchorState.Idle => "Idle",
      AnchorState.Lowering => loweringText,
      AnchorState.Anchored => anchoredText,
      AnchorState.Reeling => reelingText,
      AnchorState.Recovered => recoveredAnchorText,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public override void OnAnchorStateChange(AnchorState newState)
  {
    // No callbacks for anchor when flying. You can only Reel-in, or reel upwards.
    if (MovementController != null && MovementController.IsFlying())
    {
      if (currentState != AnchorState.Recovered)
        UpdateAnchorState(AnchorState.Reeling);
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