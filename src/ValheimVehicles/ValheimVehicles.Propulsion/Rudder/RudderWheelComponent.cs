#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Propulsion.Rudder;

public class RudderWheelComponent : MonoBehaviour, Hoverable, Interactable, IDoodadController
{
  private VehicleMovementController _controls;
  public VehicleMovementController? Controls => _controls;
  public ShipControlls deprecatedShipControls;

  public IVehicleShip ShipInstance;

  public Transform? wheelTransform;

  public List<Transform> m_spokes = [];

  public Vector3 m_leftHandPosition = new(0f, 0f, 2f);

  public Vector3 m_rightHandPosition = new(0f, 0f, -2f);

  public float m_holdWheelTime = 0.7f;

  public float m_wheelRotationFactor = 4f;

  public float m_handIKSpeed = 0.2f;

  private float m_movingLeftAlpha;

  private float m_movingRightAlpha;

  private Transform m_currentLeftHand;

  private Transform m_currentRightHand;

  private Transform m_targetLeftHand;

  private Transform m_targetRightHand;

  public string m_hoverText { get; set; }

  private const float maxUseRange = 10f;
  public Transform AttachPoint { get; set; }

  public string GetHoverText()
  {
    var controller = ShipInstance?.VehicleController?.Instance;
    if (controller == null)
    {
      return Localization.instance.Localize("$valheim_vehicles_ship_controls");
    }

    var shipStatsText = "";

    if (ValheimRaftPlugin.Instance.ShowShipStats.Value)
    {
      var shipMassToPush = ValheimRaftPlugin.Instance.MassPercentageFactor.Value;
      shipStatsText += $"\nsailArea: {controller.GetTotalSailArea()}";
      shipStatsText += $"\ntotalMass: {controller.TotalMass}";
      shipStatsText +=
        $"\nshipMass(no-containers): {controller.ShipMass}";
      shipStatsText += $"\nshipContainerMass: {controller.ShipContainerMass}";
      shipStatsText +=
        $"\ntotalMassToPush: {shipMassToPush}% * {controller.TotalMass} = {controller.TotalMass * shipMassToPush / 100f}";
      shipStatsText +=
        $"\nshipPropulsion: {controller.GetSailingForce()}";

      /* final formatting */
      shipStatsText = $"<color=white>{shipStatsText}</color>";
    }

    var isAnchored =
      controller.VehicleFlags.HasFlag(WaterVehicleFlags
        .IsAnchored);

    var anchoredStatus =
      isAnchored ? "[<color=red><b>$valheim_vehicles_wheel_use_anchored</b></color>]" : "";
    var anchorText =
      isAnchored
        ? "$valheim_vehicles_wheel_use_anchor_disable_detail"
        : "$valheim_vehicles_wheel_use_anchor_enable_detail";
    var anchorKey =
      ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set"
        ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
        : ZInput.instance.GetBoundKeyString("Run");
    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>] <color=white><b>$valheim_vehicles_wheel_use</b></color> {anchoredStatus}\n[<color=yellow><b>{anchorKey}</b></color>] <color=white>{anchorText}</color> {shipStatsText}");
  }

  private void Awake()
  {
    AttachPoint = transform.Find("attachpoint");
    wheelTransform = transform.Find("controls/wheel");
  }

  public string GetHoverName()
  {
    return Localization.instance.Localize("$valheim_vehicles_wheel");
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (user == Player.m_localPlayer && isActiveAndEnabled)
    {
      var baseVehicle = GetComponentInParent<BaseVehicleController>();
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

      ShipInstance.Instance.MovementController.lastUsedWheelComponent = this;
    }

    var canUse = InUseDistance(user);

    if (hold || !ShipInstance?.Instance || !canUse)
    {
      return false;
    }

    var player = user as Player;


    var playerOnShipViaShipInstance =
      ShipInstance?.Instance?.GetComponentsInChildren<Player>() ?? null;

    /*
     * <note /> This logic allows for the player to just look at the Raft and see if the player is a child within it.
     */
    if (playerOnShipViaShipInstance != null)
      foreach (var playerInstance in playerOnShipViaShipInstance)
      {
        Logger.LogDebug(
          $"Interact PlayerId {playerInstance.GetPlayerID()}, currentPlayerId: {player.GetPlayerID()}");
        if (playerInstance.GetPlayerID() != player.GetPlayerID()) continue;
        ShipInstance.Instance.MovementController.FireRequestControl(playerInstance.GetPlayerID(),
          AttachPoint);
        return true;
      }

    if (player == null || player.IsEncumbered())
    {
      return false;
    }

    var playerOnShip = player.GetStandingOnShip();

    if (playerOnShip == null)
    {
      Logger.LogDebug("Player is not on Ship");
      return false;
    }

    ShipInstance.Instance.MovementController.FireRequestControl(player.GetPlayerID(), AttachPoint);
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public void OnUseStop(Player player)
  {
    ShipInstance.Instance.MovementController.FireReleaseControl(player);
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    ShipInstance?.Instance.ApplyControls(moveDir);
  }

  public Component GetControlledComponent()
  {
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


  /// <summary>
  /// pass-through for the single instance of VehicleMovementController on a vehicle
  /// </summary>
  /// Updates attachpoint of parent when Interacted with
  // public bool Interact(Humanoid user, bool hold, bool alt)
  // {
  //   if (_controls != null)
  //   {
  //     _controls.AttachPoint = AttachPoint;
  //
  //     if (ShipInstance?.Instance != null)
  //     {
  //       ShipInstance.Instance.ControlGuiPosition = transform;
  //     }
  //   }
  //
  //   return _controls.Interact(user, hold, alt);
  // }

  // public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  // {
  //   return false;
  // }

  /**
   * @Deprecated for Older ValheimRAFT.MoveableBaseRootComponent compatibility. Likely will just remove the ship controller from the MBRaft.
   */
  public void DEPRECATED_InitializeControls(ZNetView netView)
  {
    if (!(bool)_controls)
    {
      deprecatedShipControls = netView.gameObject.AddComponent<ShipControlls>();
      deprecatedShipControls.m_hoverText = "$mb_rudder_use";
      deprecatedShipControls.m_attachPoint =
        AttachPoint ? AttachPoint : transform.Find("attachpoint");
      deprecatedShipControls.m_attachAnimation = "Standing Torch Idle right";
      deprecatedShipControls.m_detachOffset = new Vector3(0f, 0f, 0f);
    }

    if (!wheelTransform) wheelTransform = netView.transform.Find("controls/wheel");

    Logger.LogDebug("added rudder to BaseVehicle");
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
      _controls.InitializeRudderWithShip(vehicleShip,
        this);
      ShipInstance = vehicleShip;
      _controls.enabled = true;
    }

    Logger.LogDebug("added rudder to BaseVehicle");
  }

  public void UpdateSpokes()
  {
    m_spokes.Clear();
    m_spokes.AddRange(from k in wheelTransform.GetComponentsInChildren<Transform>()
      where k.gameObject.name.StartsWith("grabpoint")
      select k);
  }

  public void UpdateIK(Animator animator)
  {
    if (!wheelTransform)
    {
      return;
    }

    if (!m_currentLeftHand)
    {
      m_currentLeftHand = GetNearestSpoke(base.transform.TransformPoint(m_leftHandPosition));
    }

    if (!m_currentRightHand)
    {
      m_currentRightHand = GetNearestSpoke(base.transform.TransformPoint(m_rightHandPosition));
    }

    if (!m_targetLeftHand && !m_targetRightHand)
    {
      Vector3 left = base.transform.InverseTransformPoint(m_currentLeftHand.position);
      Vector3 right = base.transform.InverseTransformPoint(m_currentRightHand.position);
      if (left.z < 0.2f)
      {
        Vector3 offsetY2 = new Vector3(0f, (left.y > 0.5f) ? (-2f) : 2f, 0f);
        m_targetLeftHand =
          GetNearestSpoke(base.transform.TransformPoint(m_leftHandPosition + offsetY2));
        m_movingLeftAlpha = Time.time;
      }
      else if (right.z > -0.2f)
      {
        Vector3 offsetY = new Vector3(0f, (right.y > 0.5f) ? (-2f) : 2f, 0f);
        m_targetRightHand =
          GetNearestSpoke(base.transform.TransformPoint(m_rightHandPosition + offsetY));
        m_movingRightAlpha = Time.time;
      }
    }

    float leftHandAlpha = Mathf.Clamp01((Time.time - m_movingLeftAlpha) / m_handIKSpeed);
    float rightHandAlpha = Mathf.Clamp01((Time.time - m_movingRightAlpha) / m_handIKSpeed);
    float leftHandIKWeight = Mathf.Sin(leftHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
                             m_holdWheelTime;
    float rightHandIKWeight = Mathf.Sin(rightHandAlpha * (float)Math.PI) * (1f - m_holdWheelTime) +
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

    Vector3 leftHandPos = (m_targetLeftHand
      ? Vector3.Lerp(m_currentLeftHand.transform.position, m_targetLeftHand.transform.position,
        leftHandAlpha)
      : m_currentLeftHand.transform.position);
    Vector3 rightHandPos = (m_targetRightHand
      ? Vector3.Lerp(m_currentRightHand.transform.position, m_targetRightHand.transform.position,
        rightHandAlpha)
      : m_currentRightHand.transform.position);
    Vector3 rightHandRot = (m_targetLeftHand
      ? Vector3.Slerp(m_currentLeftHand.transform.rotation.eulerAngles,
        m_targetLeftHand.transform.rotation.eulerAngles, leftHandAlpha)
      : m_currentLeftHand.transform.rotation.eulerAngles);
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