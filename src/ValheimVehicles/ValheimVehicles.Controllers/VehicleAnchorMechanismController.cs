using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Controllers;

/// <summary>
/// An integration level component, meant to work with Valheim specific content / apis
/// </summary>
public class VehicleAnchorMechanismController : AnchorMechanismController
{
  public const float maxAnchorDistance = 40f;
  private static bool hasLocalizedAnchorState = false;
  public static string recoveredAnchorText = "";
  public static string reelingText = "";
  public static string anchoredText = "";
  public static string loweringText = "";
  public static string breakingText = "";
  public static string idleText = "";

  public static void setLocalizedStates()
  {
    if (hasLocalizedAnchorState) return;

    breakingText = Localization.instance.Localize("$valheim_vehicles_land_state_breaking");

    idleText = Localization.instance.Localize("$valheim_vehicles_land_state_idle");

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

  public override void Awake()
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

  public static string GetCurrentStateTextStatic(AnchorState anchorState, bool isLandVehicle)
  {
    if (isLandVehicle)
    {
      return anchorState == AnchorState.Anchored ? breakingText : idleText;
    }

    return anchorState switch
    {
      AnchorState.Idle => "Idle",
      AnchorState.Lowering => loweringText,
      AnchorState.Anchored => anchoredText,
      AnchorState.Reeling => reelingText,
      AnchorState.Recovered => recoveredAnchorText,
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