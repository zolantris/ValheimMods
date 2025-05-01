#nullable enable

#region

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Compat;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Controllers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

#endregion

namespace ValheimVehicles.Propulsion.Rudder;

public class SteeringWheelComponent : MonoBehaviour, Hoverable, Interactable,
  IDoodadController
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

  /// <summary>
  /// Gets the hover text info for wheel
  /// </summary>
  public static string GetHoverTextFromShip(float sailArea, float totalMass,
    float shipMass, float shipPropulsion, bool isAnchored,
    string anchorKeyString)
  {
    var shipStatsText = "";
    if (PropulsionConfig.ShowShipStats.Value)
    {
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
    }

    var anchorMessage = GetAnchorMessage(isAnchored, anchorKeyString);

    return 
      $"[<color=yellow><b>{ModTranslations.ValheimInput_KeyUse}</b></color>]<color=white><b>{ModTranslations.Anchor_WheelUse_UseText}</b></color>\n{anchorMessage}\n{shipStatsText}";
  }


  private long? _currentOwner;
  private string? _currentPlayerName;

  /// <summary>
  /// Gets the owner name
  /// </summary>
  /// TODO possibly cache this ownerName value,
  /// - listen for ZNetView owner change and fire the update then
  /// <returns>String</returns>
  private string GetOwnerHoverText()
  {
    var controller = ControllersInstance.BaseController;
    if (controller == null || controller.NetView == null || controller.NetView.GetZDO() == null) return "";

    var ownerId = controller.NetView.GetZDO().GetOwner();
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
    var hoverText = GetHoverTextFromShip(piecesController.cachedTotalSailArea,
      piecesController.TotalMass,
      piecesController.ShipMass, piecesController.GetSailingForce(),
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
    AttachPoint = transform.Find("attachpoint");
    wheelTransform = transform.Find("controls/wheel");
    wheelLocalOffset = wheelTransform.position - transform.position;
    PrefabRegistryHelpers.IgnoreCameraCollisions(gameObject);
    steeringWheelHoverTransform = transform.Find("wheel_state_hover_message");
    steeringWheelHoverText = steeringWheelHoverTransform.gameObject.AddComponent<HoverFadeText>();
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

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!isActiveAndEnabled) return false;
    
    var canUse = InUseDistance(user);

    var HasInvalidVehicle = ControllersInstance.BaseController == null;
    if (hold || HasInvalidVehicle || !canUse) return false;

    SetLastUsedWheel();

    var player = user as Player;

    var playerOnShipViaShipInstance =
      ControllersInstance?.PiecesController
        ?.GetComponentsInChildren<Player>() ?? null;

    if (player != null)
      ControllersInstance?.MovementController?.UpdatePlayerOnShip(player);

    if (playerOnShipViaShipInstance?.Length == 0 ||
        playerOnShipViaShipInstance == null)
      playerOnShipViaShipInstance =
        ControllersInstance?.BaseController?.OnboardController?.m_localPlayers.ToArray() ??
        null;

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
        ControllersInstance?.BaseController?.MovementController?.SendRequestControl(
          playerInstance.GetPlayerID());
        return true;
      }
    }

    if (player == null || player.IsEncumbered()) return false;

    var playerOnShip =
      VehicleControllersCompat.InitFromUnknown(player.GetStandingOnShip());

    if (playerOnShip == null)
    {
      Logger.LogDebug("Player is not on Ship");
      return false;
    }

    if (!ControllersInstance?.BaseController?.MovementController) return false;

    ControllersInstance.BaseController.MovementController?.SendRequestControl(
      player.GetPlayerID());
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void OnUseStop(Player player)
  {
    ControllersInstance.BaseController?.MovementController?.SendReleaseControl(player);
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run,
    bool autoRun, bool block)
  {
    if (ControllersInstance.BaseController == null || ControllersInstance.BaseController.MovementController == null)
    {
      return;
    }

    ControllersInstance?.BaseController.MovementController.ApplyControls(moveDir);
  }

  public Component? GetControlledComponent()
  {
    return ControllersInstance.BaseController;
  }

  public Vector3 GetPosition()
  {
    return transform.position;
  }

  public bool IsValid()
  {
    return this;
  }

  // Run anchor controls here only if the deprecatedMbShip is used
  // Otherwise updates for anchor are handled in MovementController
  public void FixedUpdate()
  {
    steeringWheelHoverText.FixedUpdate_UpdateText();
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
        vehicleShip.BaseController.MovementController;

    if (_controls != null)
    {
      _controls.InitializeWheelWithShip(this);
      ControllersInstance = vehicleShip;
      _controls.enabled = true;
    }
  }

  private AnchorState lastAnchorState = AnchorState.Idle;
  /// <summary>
  /// To be invoked from VehiclePiecesController when anchor updates or breaks update.
  /// </summary>
  /// <param name="message"></param>
  public void UpdateSteeringHoverMessage(AnchorState anchorState, string message)
  {
    if (anchorState == lastAnchorState) return;
    lastAnchorState = anchorState;
    steeringWheelHoverText.ResetHoverTimer();
    steeringWheelHoverText.currentText = message;
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
        Player.m_localPlayer.transform.TransformPoint(m_leftHandPosition);
      m_currentRightHand = GetNearestSpoke(playerHandTransform);
    }

    if (!m_targetLeftHand && !m_targetRightHand)
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
}