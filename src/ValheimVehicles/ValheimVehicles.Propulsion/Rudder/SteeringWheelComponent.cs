#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Patches;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Propulsion.Rudder;

public class SteeringWheelComponent : MonoBehaviour, Hoverable, Interactable, IDoodadController
{
  private VehicleMovementController _controls;
  public VehicleMovementController? Controls => _controls;
  public ShipControlls deprecatedShipControls;
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


  public static string GetAnchorHotkeyString()
  {
    return ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set"
      ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
      : ZInput.instance.GetBoundKeyString("Run");
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
  public static string GetHoverTextFromShip(float sailArea, float totalMass, float shipMass,
    float shipContainerMass, float shipPropulsion, bool isAnchored, string anchorKeyString)
  {
    var shipStatsText = "";
    if (ValheimRaftPlugin.Instance.ShowShipStats.Value)
    {
      var shipMassToPush = ValheimRaftPlugin.Instance.MassPercentageFactor.Value;
      shipStatsText += $"\nsailArea: {sailArea}";
      shipStatsText += $"\ntotalMass: {totalMass}";
      shipStatsText +=
        $"\nshipMass(no-containers): {shipMass}";
      shipStatsText += $"\nshipContainerMass: {shipContainerMass}";
      shipStatsText +=
        $"\ntotalMassToPush: {shipMassToPush}% * {totalMass} = {totalMass * shipMassToPush / 100f}";
      shipStatsText +=
        $"\nshipPropulsion: {shipPropulsion}";

      /* final formatting */
      shipStatsText = $"<color=white>{shipStatsText}</color>";
    }

    var anchoredStatus =
      isAnchored ? "[<color=red><b>$valheim_vehicles_wheel_use_anchored</b></color>]" : "";
    var anchorText =
      isAnchored
        ? "$valheim_vehicles_wheel_use_anchor_disable_detail"
        : "$valheim_vehicles_wheel_use_anchor_enable_detail";

    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>] <color=white><b>$valheim_vehicles_wheel_use</b></color> {anchoredStatus}\n[<color=yellow><b>{anchorKeyString}</b></color>] <color=white>{anchorText}</color> {shipStatsText}");
  }

  public string GetHoverText()
  {
    // deprecated MBRaft support
    if ((bool)deprecatedShipControls)
    {
      return deprecatedShipControls.GetHoverText();
    }

    var controller = ShipInstance?.VehiclePiecesController?.Instance;
    if (controller == null)
    {
      return Localization.instance.Localize(
        "<color=white><b>$valheim_vehicles_wheel_use_error</b></color>");
    }

    var isAnchored = controller?.VehicleInstance?.MovementController.IsAnchored ?? false;
    var anchorKeyString = GetAnchorHotkeyString();
    var hoverText = GetHoverTextFromShip(controller?.totalSailArea ?? 0,
      controller?.TotalMass ?? 0,
      controller?.ShipMass ?? 0,
      controller?.ShipContainerMass ?? 0, controller?.GetSailingForce() ?? 0, isAnchored,
      anchorKeyString);
#if DEBUG
    if ((bool)controller?.MovementController?.m_players?.Any())
    {
      hoverText +=
        $"\n[<color=red><b>Owner: {controller?.VehicleInstance?.NetView.GetZDO().GetOwner()} and name: {controller?.MovementController?.m_players[0].GetPlayerName()}</b></color>]";
    }
#endif
    return hoverText;
  }

  private void Awake()
  {
    AttachPoint = transform.Find("attachpoint");
    wheelTransform = transform.Find("controls/wheel");
    wheelLocalOffset = wheelTransform.position - transform.position;
    PrefabRegistryHelpers.IgnoreCameraCollisions(gameObject);
  }

  public string GetHoverName()
  {
    if ((bool)deprecatedShipControls)
    {
      return deprecatedShipControls.GetHoverName();
    }

    return Localization.instance.Localize("$valheim_vehicles_wheel");
  }

  public void SetLastUsedWheel()
  {
    if (ShipInstance != null)
    {
      ShipInstance.Instance.MovementController.lastUsedWheelComponent = this;
    }
    else if ((bool)deprecatedShipControls)
    {
      PatchSharedData.PlayerLastUsedControls = deprecatedShipControls;
    }
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!isActiveAndEnabled)
    {
      return false;
    }

    // for mbraft
    if ((bool)deprecatedShipControls)
    {
      deprecatedShipControls.Interact(user, hold, alt);
      deprecatedShipControls.m_ship.m_controlGuiPos.position = transform.position;
    }

    if (user == Player.m_localPlayer)
    {
      var baseVehicle = GetComponentInParent<VehiclePiecesController>();
      if (baseVehicle != null)
      {
        baseVehicle.ComputeAllShipContainerItemWeight();
      }
      else
      {
        var baseRoot = GetComponentInParent<MoveableBaseRootComponent>();
        if (baseRoot != null)
        {
          baseRoot.ComputeAllShipContainerItemWeight();
        }
      }
    }

    var canUse = InUseDistance(user);

    var noControls = !((bool)deprecatedShipControls || ShipInstance?.Instance);
    if (hold || noControls || !canUse)
    {
      return false;
    }

    SetLastUsedWheel();


    var player = user as Player;


    var playerOnShipViaShipInstance =
      ShipInstance?.VehiclePiecesController?.Instance.GetComponentsInChildren<Player>() ?? null;

    if (playerOnShipViaShipInstance?.Length == 0 || playerOnShipViaShipInstance == null)
    {
      playerOnShipViaShipInstance =
        ShipInstance?.Instance?.MovementController?.m_players.ToArray() ?? null;
    }

    /*
     * <note /> This logic allows for the player to just look at the Raft and see if the player is a child within it.
     */
    if (playerOnShipViaShipInstance != null)
      foreach (var playerInstance in playerOnShipViaShipInstance)
      {
        Logger.LogDebug(
          $"Interact PlayerId {playerInstance.GetPlayerID()}, currentPlayerId: {player.GetPlayerID()}");
        if (playerInstance.GetPlayerID() != player.GetPlayerID()) continue;
        ShipInstance?.Instance?.MovementController.FireRequestControl(playerInstance.GetPlayerID(),
          AttachPoint);
        return true;
      }
    else if ((bool)deprecatedShipControls)
    {
      if (player)
      {
        player.m_lastGroundBody = deprecatedShipControls.m_ship.m_body;
      }

      deprecatedShipControls.m_nview.InvokeRPC("RequestControl", (object)player.GetPlayerID());
      return false;
    }

    if (player == null || player.IsEncumbered())
    {
      return false;
    }

    var playerOnShip = VehicleShipCompat.InitFromUnknown(player.GetStandingOnShip());

    if (playerOnShip == null)
    {
      Logger.LogDebug("Player is not on Ship");
      return false;
    }

    if (!ShipInstance?.Instance?.MovementController) return false;

    ShipInstance.Instance.MovementController.FireRequestControl(player.GetPlayerID(), AttachPoint);
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

    ShipInstance.Instance.MovementController.FireReleaseControl(player);
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    if ((bool)deprecatedShipControls)
    {
      deprecatedShipControls.ApplyControlls(moveDir, lookDir, run, autoRun, block);
      return;
    }

    ShipInstance?.Instance.MovementController.ApplyControls(moveDir);
  }

  public Component GetControlledComponent()
  {
    if ((bool)deprecatedShipControls)
    {
      return transform.parent.GetComponent<MoveableBaseRootComponent>().m_ship;
    }

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


  // Run anchor controls here only if the deprecatedMbShip is used
  // Otherwise updates for anchor are handled in MovementController
  public void FixedUpdate()
  {
    if (!deprecatedShipControls) return;
    if (!VehicleMovementController.ShouldHandleControls())
    {
      return;
    }

    if (!deprecatedMBShip) return;

    VehicleMovementController.DEPRECATED_OnFlightControls(deprecatedMBShip);

    var anchorKey = VehicleMovementController.GetAnchorKey();
    if (!anchorKey) return;

    deprecatedMBShip.SetAnchor(
      !deprecatedMBShip.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored));
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
      deprecatedShipControls.m_hoverText = Localization.instance.Localize("$mb_rudder_use");
      deprecatedShipControls.m_attachPoint =
        AttachPoint ? AttachPoint : transform.Find("attachpoint");
      deprecatedShipControls.m_attachAnimation = "Standing Torch Idle right";
      deprecatedShipControls.m_detachOffset = new Vector3(0f, 0f, 0f);
      deprecatedShipControls.m_ship = mbRoot.m_ship;
    }

    if (!wheelTransform) wheelTransform = netView.transform.Find("controls/wheel");
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
    {
      _controls =
        vehicleShip.Instance.MovementController;
    }

    if (_controls != null)
    {
      _controls.InitializeWheelWithShip(this);
      ShipInstance = vehicleShip;
      _controls.enabled = true;
    }
  }

  public void UpdateSpokes()
  {
    m_spokes.Clear();
    m_spokes.AddRange(from k in wheelTransform.GetComponentsInChildren<Transform>()
      where k.gameObject.name.StartsWith("grabpoint")
      select k);
  }

  private Vector3 GetWheelHandOffset(float height)
  {
    var offset = new Vector3(0f, (height > wheelLocalOffset.y) ? -0.25f : 0.25f, 0f);
    return offset;
  }

  public void UpdateIK(Animator animator)
  {
    if (!wheelTransform)
    {
      return;
    }

    if (!m_currentLeftHand)
    {
      var playerHandTransform = transform.TransformPoint(m_leftHandPosition);
      m_currentLeftHand = GetNearestSpoke(playerHandTransform);
    }

    if (!m_currentRightHand)
    {
      var playerHandTransform = Player.m_localPlayer.transform.TransformPoint(m_leftHandPosition);
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
          GetNearestSpoke(transform.TransformPoint(m_leftHandPosition + GetWheelHandOffset(left.y) +
                                                   wheelCenter));
        m_movingLeftAlpha = Time.time;
      }
      else if (right.x < 0f)
      {
        m_targetRightHand =
          GetNearestSpoke(transform.TransformPoint(m_rightHandPosition +
                                                   GetWheelHandOffset(right.y) + wheelCenter));
        m_movingRightAlpha = Time.time;
      }
    }

    var leftHandAlpha = Mathf.Clamp01((Time.time - m_movingLeftAlpha) / m_handIKSpeed);
    var rightHandAlpha = Mathf.Clamp01((Time.time - m_movingRightAlpha) / m_handIKSpeed);
    var leftHandIKWeight = Mathf.Sin(leftHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
                           m_holdWheelTime;
    var rightHandIKWeight = Mathf.Sin(rightHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
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


    var leftHandPos = ((bool)m_targetLeftHand
      ? Vector3.Lerp(m_currentLeftHand.transform.position, m_targetLeftHand.transform.position,
        leftHandAlpha)
      : m_currentLeftHand.transform.position);
    var rightHandPos = ((bool)m_targetRightHand
      ? Vector3.Lerp(m_currentRightHand.transform.position, m_targetRightHand.transform.position,
        rightHandAlpha)
      : m_currentRightHand.transform.position);

    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);
    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandIKWeight);
    animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);
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
    return Vector3.Distance(human.transform.position, AttachPoint.position) < maxUseRange;
  }
}