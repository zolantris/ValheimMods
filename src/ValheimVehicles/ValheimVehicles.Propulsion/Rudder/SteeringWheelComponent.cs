#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Components;
using ValheimVehicles.Constants;
using ValheimVehicles.Controllers;
using ValheimVehicles.Enums;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.ValheimVehicles.Components;
using Logger = Jotunn.Logger;

#endregion

namespace ValheimVehicles.Propulsion.Rudder;

public class SteeringWheelComponent : MonoBehaviour, IAnimatorHandler, Hoverable, Interactable,
  IDoodadController, INetView
{
  private VehicleMovementController _controls;
  public VehicleMovementController? Controls => _controls;

  public IVehicleControllers ControllersInstance;
  public Transform? wheelTransform;
  private Vector3 wheelLocalOffset;

  public List<Transform> m_spokes = [];

  public Vector3 m_leftHandPosition = new(-0.5f, 0f, 0);

  public Vector3 m_rightHandPosition = new(0.5f, 0f, 0);

  public float m_holdWheelTime = 0.7f;

  public float m_wheelRotationFactor = 4f;

  public float m_handIKSpeed = 0.2f;

  private float m_movingLeftAlpha;

  private float m_movingRightAlpha;

  private Transform? m_currentLeftHand;

  private Transform? m_currentRightHand;

  private Transform? m_targetLeftHand;

  private Transform? m_targetRightHand;

  public string m_hoverText { get; set; }

  private const float maxUseRange = 10f;
  public Transform AttachPoint { get; set; }
  public Transform steeringWheelHoverTransform;
  public HoverFadeText steeringWheelHoverText;

  public const string Key_ShowTutorial = "SteeringWheel_ShowTutorial";
  public bool showTutorial = true;

  public string _cachedLocalizedWheelHoverText = "";

  /// <summary>
  /// Todo might be worth caching this.
  /// </summary>
  public static string GetAnchorHotkeyString()
  {
    return ZInput.instance.GetBoundKeyString("Run");
  }

  public static string GetAnchorMessage(bool isAnchored, string anchorKeyString)
  {
    var anchoredStatus =
      isAnchored
        ? $"[<color=red><b>{ModTranslations.AnchorPrefab_anchoredText}</b></color>]"
        : "";
    var anchorText =
      isAnchored
        ? ModTranslations.Anchor_WheelUse_DisableAnchor
        : ModTranslations.Anchor_WheelUse_EnableAnchor;

    return
      $"{anchoredStatus}\n[<color=yellow><b>{anchorKeyString}</b></color>] <color=white>{anchorText}</color>";
  }

  public bool TryGetShipStats(out string shipStatsText)
  {
    shipStatsText = "";
    if (!PropulsionConfig.ShowShipStats.Value) return false;
    if (ControllersInstance.PiecesController == null) return false;

    var piecesController = ControllersInstance.PiecesController;
    var sailArea = piecesController.cachedTotalSailArea;
    var totalMass = piecesController.TotalMass;
    var shipMass = piecesController.ShipMass;
    var shipPropulsion = piecesController.GetSailingForce();

    var shipMassToPush =
      PropulsionConfig.SailingMassPercentageFactor.Value;
    shipStatsText += $"\nsailArea: {sailArea}";
    shipStatsText += $"\ntotalMass: {totalMass}";
    shipStatsText +=
      $"\nshipMass(no-containers): {shipMass}";

    shipStatsText +=
      $"\ntotalMassToPush: {shipMassToPush}% * {totalMass} = {totalMass * shipMassToPush / 100f}";
    shipStatsText +=
      $"\nshipPropulsion: {shipPropulsion}";

    /* final formatting */
    shipStatsText = $"<color=white>{shipStatsText}</color>";

    return true;
  }

  /// <summary>
  /// Gets the hover text info for wheel
  /// </summary>
  public string GetHoverTextFromShip(bool isAnchored,
    string anchorKeyString)
  {
    var anchorMessage = GetAnchorMessage(isAnchored, anchorKeyString);

    var interactMessage = $"{ModTranslations.SharedKeys_InteractPrimary} {ModTranslations.WithBoldText(ModTranslations.Anchor_WheelUse_UseText, "white")}";

    var variant = ControllersInstance.Manager.vehicleVariant;

    var isFlightCapable = VehicleManager.IsFlightCapable(variant);
    var isBallastCapable = VehicleManager.IsBallastCapable(variant);

    interactMessage += $"\n{anchorMessage}";

    if (isFlightCapable || isBallastCapable)
    {
      interactMessage += "\n--------";
    }

    // propulsion messages
    if (isFlightCapable)
    {
      interactMessage += $"\n{ModTranslations.WheelControls_FlightActivation}";
    }
    if (isBallastCapable)
    {
      interactMessage += $"\n{ModTranslations.WheelControls_BallastActivation}";
    }

    var additionalMessages = "";

    if (this.IsNetViewValid(out var nv))
    {
      showTutorial = nv.GetZDO().GetBool(Key_ShowTutorial, showTutorial);
    }

    if (showTutorial)
    {
      additionalMessages += $"\n{ModTranslations.Cannon_TutorialShort}";

      if (variant == VehicleVariant.All || variant == VehicleVariant.Air)
      {
        additionalMessages += $"\n{ModTranslations.WheelControls_TutorialFlight}";
      }
    }

    if (TryGetShipStats(out var statsMessage))
    {
      additionalMessages += $"\n{statsMessage}";
    }

    var tutorialToggleMessage = $"{ModTranslations.SharedKeys_InteractAlt} {ModTranslations.WithBoldText(ModTranslations.SharedKeys_Tutorial, "white")}";

    return
      $"{interactMessage}\n{tutorialToggleMessage}\n{additionalMessages}";
  }


  private long? _currentOwner;
  private string? _currentPlayerName;
  private bool hasTargetControlListener;

  /// <summary>
  /// Gets the owner name
  /// </summary>
  /// TODO possibly cache this ownerName value,
  /// - listen for ZNetView owner change and fire the update then
  /// <returns>String</returns>
  private string GetOwnerHoverText()
  {
    var controller = ControllersInstance.Manager;
    if (controller == null || controller.m_nview == null || controller.m_nview.GetZDO() == null) return "";

    var ownerId = controller.m_nview.GetZDO().GetOwner();
    if (ownerId != _currentOwner || _currentPlayerName == null)
    {
      var matchingOwnerInPlayers =
        controller?.OnboardController.m_localPlayers.FirstOrDefault((player) =>
        {
          var playerOwnerId = player.GetOwner();
          return ownerId == playerOwnerId;
        });
      _currentOwner = matchingOwnerInPlayers?.GetOwner();
      _currentPlayerName = matchingOwnerInPlayers?.GetPlayerName() ?? "Server";
    }

    return
      $"\n[<color=green><b>{ModTranslations.SharedKeys_Owner}: {_currentPlayerName}</b></color>]";
  }

  private string GetBeachedHoverText()
  {
    return
      $"\n[<color=red><b>{ModTranslations.VehicleConfig_Beached}</b></color>]";
  }

  public string GetHoverText()
  {
    var piecesController = ControllersInstance.PiecesController;
    var onboardController = ControllersInstance.OnboardController;
    var movementController = ControllersInstance.MovementController;
    if (piecesController == null || onboardController == null || movementController == null)
    {
      return ModTranslations.WheelControls_Error;
    }

    var isAnchored = VehicleMovementController.GetIsAnchoredSafe(ControllersInstance);
    var anchorKeyString = GetAnchorHotkeyString();
    var hoverText = GetHoverTextFromShip(
      isAnchored,
      anchorKeyString);

    if (onboardController.m_localPlayers.Any())
      hoverText += GetOwnerHoverText();

    if (movementController.isBeached)
      hoverText += GetBeachedHoverText();

    return hoverText;
  }

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    if (!m_nview)
    {
      this.WaitForZNetView((nv) =>
      {
        m_nview = nv;
      });
    }
    AttachPoint = transform.Find("attachpoint");
    wheelTransform = transform.Find("controls/wheel");
    wheelLocalOffset = wheelTransform.position - transform.position;
    PrefabRegistryHelpers.IgnoreCameraCollisions(gameObject);
    steeringWheelHoverTransform = transform.Find("wheel_state_hover_message");
    steeringWheelHoverText = steeringWheelHoverTransform.gameObject.AddComponent<HoverFadeText>();
  }

  private void OnDestroy()
  {
    if (hasTargetControlListener && ControllersInstance.PiecesController != null)
    {
      var tgc = ControllersInstance.PiecesController.targetController;
      tgc.OnCannonGroupChange -= (val) => TargetControlsInteractive.UpdateTextFromCannonDirectionGroup(steeringWheelHoverText, val, tgc.LastGroupSize);
      hasTargetControlListener = false;
    }
  }


  public string GetHoverName()
  {
    return ModTranslations.WheelControls_Name;
  }

  public void SetLastUsedWheel()
  {
    if (ControllersInstance.MovementController != null)
      ControllersInstance.MovementController.lastUsedWheelComponent = this;
  }

  public float lastToggleTime = 0f;

  public void ToggleTutorial()
  {
    if (!this.IsNetViewValid(out var nv)) return;
    if (Time.time < lastToggleTime + 1)
      return;

    var zdo = nv.GetZDO();
    var nextValue = !showTutorial;
    zdo.Set(Key_ShowTutorial, nextValue);
    showTutorial = nextValue;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!isActiveAndEnabled) return false;
    if (ControllersInstance == null || ControllersInstance.MovementController == null || ControllersInstance.OnboardController == null || ControllersInstance.PiecesController == null) return false;

    if (alt && !hold)
    {
      ToggleTutorial();
      return true;
    }

    var canUse = InUseDistance(user);

    var HasInvalidVehicle = ControllersInstance.Manager == null;
    if (HasInvalidVehicle || !canUse) return false;

    SetLastUsedWheel();

    if (!hasTargetControlListener && ControllersInstance.PiecesController != null)
    {
      var targetController = ControllersInstance.PiecesController.targetController;
      targetController.OnCannonGroupChange += (val) => TargetControlsInteractive.UpdateTextFromCannonDirectionGroup(steeringWheelHoverText, val, targetController.LastGroupSize);
      hasTargetControlListener = true;
    }

    var player = user as Player;

    var playerOnShipViaShipInstance =
      ControllersInstance.PiecesController?.GetComponentsInChildren<Player>();
    if (player != null)
      ControllersInstance.MovementController.UpdatePlayerOnShip(player);

    if (playerOnShipViaShipInstance?.Length == 0 ||
        playerOnShipViaShipInstance == null)
      playerOnShipViaShipInstance =
        ControllersInstance.OnboardController.m_localPlayers.ToArray() ??
        null;

    if (player == null || player.IsEncumbered()) return false;
    /*
     * <note /> This logic allows for the player to just look at the Raft and see if the player is a child within it.
     */
    if (playerOnShipViaShipInstance != null)
    {
      foreach (var playerInstance in playerOnShipViaShipInstance)
      {
        Logger.LogDebug(
          $"Interact PlayerId {playerInstance.GetPlayerID()}, currentPlayerId: {player.GetPlayerID()}");
        if (playerInstance.GetPlayerID() != player.GetPlayerID()) continue;
        if (ControllersInstance == null || ControllersInstance.MovementController) continue;
        ControllersInstance.MovementController.SendRequestControl(
          playerInstance.GetPlayerID());
        return true;
      }
    }

    var playerOnShip =
      VehicleControllersCompat.InitFromUnknown(player.GetStandingOnShip());

    if (playerOnShip == null && !WaterZoneUtils.IsOnboard(player))
    {
      Logger.LogDebug("Player is not on Ship");
      return false;
    }


    ControllersInstance?.MovementController.SendRequestControl(
      player.GetPlayerID());
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void OnUseStop(Player player)
  {
    ControllersInstance.Manager?.MovementController?.SendReleaseControl(player);
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run,
    bool autoRun, bool block)
  {
    if (ControllersInstance.PiecesController == null || ControllersInstance.Manager == null || ControllersInstance.Manager.MovementController == null)
    {
      return;
    }

    if (ControllersInstance.PiecesController.targetController.HandleManualCannonControls(moveDir, lookDir, run, autoRun, block))
      return;
    ControllersInstance.Manager.MovementController.ApplyControls(moveDir);
  }

  public Component? GetControlledComponent()
  {
    return ControllersInstance.Manager;
  }

  public Vector3 GetPosition()
  {
    return transform.position;
  }

  public bool IsValid()
  {
    return this;
  }

  public void InitializeControls(ZNetView netView, IVehicleControllers? vehicleShip)
  {
    if (vehicleShip == null)
    {
      Logger.LogError("Initialized called with null vehicleShip");
      return;
    }

    ControllersInstance = vehicleShip;

    if (!(bool)_controls)
      _controls =
        vehicleShip.Manager.MovementController;

    if (_controls != null)
    {
      _controls.InitializeWheelWithShip(this);
      ControllersInstance = vehicleShip;
      _controls.enabled = true;
    }
  }

  private AnchorState lastAnchorState = AnchorState.Idle;

  public void UpdateSteeringHoverMessage(string message)
  {
    steeringWheelHoverText.currentText = message;
    steeringWheelHoverText.Show();
  }

  /// <summary>
  /// To be invoked from VehiclePiecesController when anchor updates or breaks update.
  /// </summary>
  /// <param name="message"></param>
  public void UpdateSteeringHoverMessage(AnchorState anchorState, string message)
  {
    if (anchorState == lastAnchorState) return;
    lastAnchorState = anchorState;
    steeringWheelHoverText.currentText = message;
    steeringWheelHoverText.Show();
  }

  public void UpdateSpokes()
  {
    m_spokes.Clear();
    m_spokes.AddRange(
      from k in wheelTransform.GetComponentsInChildren<Transform>()
      where k.gameObject.name.StartsWith("grabpoint")
      select k);
  }

  private Vector3 GetWheelHandOffset(float height)
  {
    var offset = new Vector3(0f, height > wheelLocalOffset.y ? -0.25f : 0.25f,
      0f);
    return offset;
  }

  public void UpdateIK(Animator animator)
  {
    if (!wheelTransform) return;

    if (!m_currentLeftHand)
    {
      var playerHandTransform = transform.TransformPoint(m_leftHandPosition);
      m_currentLeftHand = GetNearestSpoke(playerHandTransform);
    }

    if (!m_currentRightHand)
    {
      var playerHandTransform =
        transform.TransformPoint(m_rightHandPosition);
      m_currentRightHand = GetNearestSpoke(playerHandTransform);
    }

    if (!m_targetLeftHand && !m_targetRightHand && m_currentLeftHand != null && m_currentRightHand != null)
    {
      var left = transform.InverseTransformPoint(m_currentLeftHand.position);
      var right = transform.InverseTransformPoint(m_currentRightHand.position);
      var wheelCenter = wheelLocalOffset.y * Vector3.up;
      if (left.x > 0f)
      {
        m_targetLeftHand =
          GetNearestSpoke(transform.TransformPoint(
            m_leftHandPosition + GetWheelHandOffset(left.y) +
            wheelCenter));
        m_movingLeftAlpha = Time.time;
      }
      else if (right.x < 0f)
      {
        m_targetRightHand =
          GetNearestSpoke(transform.TransformPoint(m_rightHandPosition +
                                                   GetWheelHandOffset(right.y) +
                                                   wheelCenter));
        m_movingRightAlpha = Time.time;
      }
    }

    var leftHandAlpha =
      Mathf.Clamp01((Time.time - m_movingLeftAlpha) / m_handIKSpeed);
    var rightHandAlpha =
      Mathf.Clamp01((Time.time - m_movingRightAlpha) / m_handIKSpeed);
    var leftHandIKWeight =
      Mathf.Sin(leftHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
      m_holdWheelTime;
    var rightHandIKWeight = Mathf.Sin(rightHandAlpha * (float)Math.PI) *
                            (1f - m_holdWheelTime) +
                            m_holdWheelTime;
    if ((bool)m_targetLeftHand && leftHandAlpha > 0.99f)
    {
      m_currentLeftHand = m_targetLeftHand;
      m_targetLeftHand = null;
    }

    if ((bool)m_targetRightHand && rightHandAlpha > 0.99f)
    {
      m_currentRightHand = m_targetRightHand;
      m_targetRightHand = null;
    }


    var leftHandPos = (bool)m_targetLeftHand
      ? Vector3.Lerp(m_currentLeftHand.transform.position,
        m_targetLeftHand.transform.position,
        leftHandAlpha)
      : m_currentLeftHand.transform.position;
    var rightHandPos = (bool)m_targetRightHand
      ? Vector3.Lerp(m_currentRightHand.transform.position,
        m_targetRightHand.transform.position,
        rightHandAlpha)
      : m_currentRightHand.transform.position;

    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);
    // Note this might need a "SetTrigger call"
  }

  public Transform GetNearestSpoke(Vector3 position)
  {
    Transform? best = null;
    var bestDistance = 0f;
    foreach (var spoke in m_spokes)
    {
      var dist = (spoke.transform.position - position).sqrMagnitude;
      if (best != null && !(dist < bestDistance)) continue;
      best = spoke;
      bestDistance = dist;
    }

    return best;
  }

  private bool InUseDistance(Component human)
  {
    if (AttachPoint == null) return false;
    return Vector3.Distance(human.transform.position, AttachPoint.position) <
           maxUseRange;
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
}