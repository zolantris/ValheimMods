#region

  using System;
  using UnityEngine;
  using ValheimVehicles.Config;
  using ValheimVehicles.Constants;
  using ValheimVehicles.SharedScripts;

#endregion

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
      return anchorState == AnchorState.Anchored ? ModTranslations.AnchorPrefab_breakingText : ModTranslations.AnchorPrefab_idleText;
    }

    return anchorState switch
    {
      AnchorState.Idle => "Idle",
      AnchorState.Lowering => ModTranslations.AnchorPrefab_loweringText,
      AnchorState.Anchored => ModTranslations.AnchorPrefab_anchoredText,
      AnchorState.Reeling => ModTranslations.AnchorPrefab_reelingText,
      AnchorState.Recovered => ModTranslations.AnchorPrefab_RecoveredAnchorText,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public bool IsLandVehicle()
  {
    return MovementController != null && MovementController.BaseController != null && MovementController.BaseController.IsLandVehicle;
  }

  public override string GetCurrentStateText()
  {
    return GetCurrentStateTextStatic(currentState, IsLandVehicle());
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