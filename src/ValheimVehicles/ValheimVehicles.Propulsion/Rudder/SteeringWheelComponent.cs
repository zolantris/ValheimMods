#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Controllers;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Propulsion.Rudder;

public class SteeringWheelComponent : MonoBehaviour, Hoverable, Interactable,
  IDoodadController
{
  private VehicleMovementController _controls;
  public VehicleMovementController? Controls => _controls;
  public ShipControlls deprecatedShipControls;
  public bool HasDeprecatedControls = false;
  public MoveableBaseShipComponent deprecatedMBShip;

  public IVehicleShip ShipInstance;
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
  /// <returns></returns>
  public static string GetAnchorHotkeyString()
  {
    return ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
           "Not set"
      ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
      : ZInput.instance.GetBoundKeyString("Run");
  }

  public const string AnchorUseMessage =
    "[<color=red><b>$valheim_vehicles_wheel_use_anchored</b></color>]";

  public static string GetAnchorMessage(bool isAnchored, string anchorKeyString)
  {
    var anchoredStatus =
      isAnchored
        ? AnchorUseMessage
        : "";
    var anchorText =
      isAnchored
        ? "$valheim_vehicles_wheel_use_anchor_disable_detail"
        : "$valheim_vehicles_wheel_use_anchor_enable_detail";

    return
      $"{anchoredStatus}\n[<color=yellow><b>{anchorKeyString}</b></color>] <color=white>{anchorText}</color>";
  }

  public static string GetTutorialAnchorMessage(bool isAnchored)
  {
    var anchorMessage = GetAnchorMessage(isAnchored,
      GetAnchorHotkeyString());
    anchorMessage = Regex.Replace(anchorMessage, @"[\[\]]", "");
    return $"Vehicle is {anchorMessage}";
  }

  /// <summary>
  /// Gets the hover text info for wheel
  /// </summary>
  /// todo cache the localized text.
  /// <param name="sailArea"></param>
  /// <param name="totalMass"></param>
  /// <param name="shipMass"></param>
  /// <param name="shipContainerMass"></param>
  /// <param name="shipPropulsion"></param>
  /// <param name="isAnchored"></param>
  /// <param name="anchorKeyString"></param>
  /// <returns></returns>
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

    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>]<color=white><b>$valheim_vehicles_wheel_use</b></color>\n{anchorMessage}\n{shipStatsText}");
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
    var controller = ShipInstance?.PiecesController?.VehicleInstance;
    if (controller?.NetView?.GetZDO() == null) return "";

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
      $"\n[<color=green><b>Physics Owner: {_currentPlayerName}</b></color>]";
  }

  private string GetBeachedHoverText()
  {
    return
      $"\n[<color=red><b>$valheim_vehicles_gui_vehicle_is_beached</b></color>]";
  }

  public string GetHoverText()
  {
    // deprecated MBRaft support
    if ((bool)deprecatedShipControls)
      return deprecatedShipControls.GetHoverText();

    var controller = ShipInstance?.PiecesController;
    if (controller == null)
      return Localization.instance.Localize(
        "<color=white><b>$valheim_vehicles_wheel_use_error</b></color>");

    var isAnchored =
      controller?.VehicleInstance?.MovementController?.isAnchored ?? false;
    var anchorKeyString = GetAnchorHotkeyString();
    var hoverText = GetHoverTextFromShip(controller?.totalSailArea ?? 0,
      controller?.TotalMass ?? 0,
      controller?.ShipMass ?? 0, controller?.GetSailingForce() ?? 0,
      isAnchored,
      anchorKeyString);
    if ((bool)controller?.OnboardController?.m_localPlayers?.Any())
      hoverText += GetOwnerHoverText();

    if ((bool)controller?.MovementController?.isBeached)
      hoverText += GetBeachedHoverText();

    return Localization.instance.Localize(hoverText);
  }

  private void Awake()
  {
    VehicleAnchorMechanismController.setLocalizedStates();
    AttachPoint = transform.Find("attachpoint");
    wheelTransform = transform.Find("controls/wheel");
    wheelLocalOffset = wheelTransform.position - transform.position;
    PrefabRegistryHelpers.IgnoreCameraCollisions(gameObject);
    steeringWheelHoverTransform = transform.Find("wheel_state_hover_message");
    steeringWheelHoverText = steeringWheelHoverTransform.gameObject.AddComponent<HoverFadeText>();
  }


  public string GetHoverName()
  {
    if (_cachedLocalizedWheelHoverText != string.Empty) return _cachedLocalizedWheelHoverText;

    _cachedLocalizedWheelHoverText = !HasDeprecatedControls ? Localization.instance.Localize("$valheim_vehicles_wheel") : deprecatedShipControls.GetHoverName();

    return _cachedLocalizedWheelHoverText;
  }

  public void SetLastUsedWheel()
  {
    if (ShipInstance.MovementController != null)
      ShipInstance.MovementController.lastUsedWheelComponent = this;
    else if (HasDeprecatedControls)
      PatchSharedData.PlayerLastUsedControls = deprecatedShipControls;
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!isActiveAndEnabled) return false;

    // for mbraft
    if ((bool)deprecatedShipControls)
    {
      deprecatedShipControls.Interact(user, hold, alt);
      deprecatedShipControls.m_ship.m_controlGuiPos.position =
        transform.position;
    }

    var canUse = InUseDistance(user);

    var noControls = !((bool)deprecatedShipControls || ShipInstance?.Instance);
    if (hold || noControls || !canUse) return false;

    SetLastUsedWheel();

    var player = user as Player;

    var playerOnShipViaShipInstance =
      ShipInstance?.PiecesController
        ?.GetComponentsInChildren<Player>() ?? null;

    if (player != null)
      ShipInstance?.MovementController?.UpdatePlayerOnShip(player);

    if (playerOnShipViaShipInstance?.Length == 0 ||
        playerOnShipViaShipInstance == null)
      playerOnShipViaShipInstance =
        ShipInstance?.Instance?.OnboardController?.m_localPlayers.ToArray() ??
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
        ShipInstance?.Instance?.MovementController?.SendRequestControl(
          playerInstance.GetPlayerID());
        return true;
      }
    }
    else if ((bool)deprecatedShipControls)
    {
      if (player)
        player.m_lastGroundBody = deprecatedShipControls.m_ship.m_body;

      deprecatedShipControls.m_nview.InvokeRPC("RequestControl",
        (object)player.GetPlayerID());
      return false;
    }

    if (player == null || player.IsEncumbered()) return false;

    var playerOnShip =
      VehicleShipCompat.InitFromUnknown(player.GetStandingOnShip());

    if (playerOnShip == null)
    {
      Logger.LogDebug("Player is not on Ship");
      return false;
    }

    if (!ShipInstance?.Instance?.MovementController) return false;

    ShipInstance.Instance.MovementController?.SendRequestControl(
      player.GetPlayerID());
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void OnUseStop(Player player)
  {
    if ((bool)deprecatedShipControls)
    {
      deprecatedShipControls.OnUseStop(player);
      return;
    }

    ShipInstance.Instance?.MovementController?.SendReleaseControl(player);
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run,
    bool autoRun, bool block)
  {
    if ((bool)deprecatedShipControls)
    {
      deprecatedShipControls.ApplyControlls(moveDir, lookDir, run, autoRun,
        block);
      return;
    }

    ShipInstance?.Instance.MovementController.ApplyControls(moveDir);
  }

  public Component GetControlledComponent()
  {
    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value &&
        (bool)deprecatedShipControls)
      return transform.parent.GetComponent<MoveableBaseRootComponent>().m_ship;

    return ShipInstance.Instance;
  }

  public Vector3 GetPosition()
  {
    return transform.position;
  }

  public bool IsValid()
  {
    return this;
  }

  public void FixedUpdateDeprecatedShip()
  {

    if (!deprecatedShipControls) return;
    if (!VehicleMovementController.ShouldHandleControls()) return;

    if (!deprecatedMBShip) return;

    VehicleMovementController.DEPRECATED_OnFlightControls(deprecatedMBShip);

    var anchorKey = VehicleMovementController.GetAnchorKeyDown();
    if (!anchorKey) return;

    deprecatedMBShip.SetAnchor(
      !deprecatedMBShip.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags
        .IsAnchored));
  }

  // Run anchor controls here only if the deprecatedMbShip is used
  // Otherwise updates for anchor are handled in MovementController
  public void FixedUpdate()
  {
    steeringWheelHoverText.UpdateText();

    // only for v1
    FixedUpdateDeprecatedShip();
  }

  /**
   * @Deprecated for Older ValheimRAFT.MoveableBaseRootComponent compatibility. Future updates will remove support for MBRaft after a migration script is available.
   */
  public void DEPRECATED_InitializeControls(ZNetView netView)
  {
    if (!(bool)_controls)
    {
      var mbRoot = transform.parent.GetComponent<MoveableBaseRootComponent>();
      if (mbRoot == null) return;
      deprecatedMBShip = mbRoot.shipController;
      deprecatedShipControls = mbRoot.m_ship.m_shipControlls;
      deprecatedShipControls.m_maxUseRange = 10f;
      deprecatedShipControls.m_hoverText =
        Localization.instance.Localize("$mb_rudder_use");
      deprecatedShipControls.m_attachPoint =
        AttachPoint ? AttachPoint : transform.Find("attachpoint");
      deprecatedShipControls.m_attachAnimation = "Standing Torch Idle right";
      deprecatedShipControls.m_detachOffset = new Vector3(0f, 0f, 0f);
      deprecatedShipControls.m_ship = mbRoot.m_ship;
      HasDeprecatedControls = true;
    }

    if (!wheelTransform)
      wheelTransform = netView.transform.Find("controls/wheel");
  }

  public void InitializeControls(ZNetView netView, IVehicleShip? vehicleShip)
  {
    if (vehicleShip == null)
    {
      Logger.LogError("Initialized called with null vehicleShip");
      return;
    }

    ShipInstance = vehicleShip;

    if (!(bool)_controls)
      _controls =
        vehicleShip.Instance.MovementController;

    if (_controls != null)
    {
      _controls.InitializeWheelWithShip(this);
      ShipInstance = vehicleShip;
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