using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Propulsion.Rudder;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class ValheimShipControls : MonoBehaviour, Interactable, Hoverable, IDoodadController,
  IRudderControls
{
  public IVehicleShip ShipInstance { get; set; }
  // might be safer to directly make this a getter
  // public Transform m_attachPoint;

  public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

  public string m_hoverText { get; set; }
  [SerializeField] private float maxUseRange = 10f;

  public float MaxUseRange
  {
    get => maxUseRange;
    set => maxUseRange = value;
  }

  public Transform AttachPoint { get; set; }

  public string m_attachAnimation = "Standing Torch Idle right";
  public ValheimShipControls m_lastUsedControls;
  public ZNetView m_nview;

  private bool hasRegister = false;

  public void InitializeRudderWithShip(IVehicleShip ship, RudderComponent rudder)
  {
    ShipInstance = ship;
    ShipInstance.Instance.m_controlGuiPos = transform;

    var rudderAttachPoint = rudder.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }

    m_nview = ship.Instance.GetComponent<ZNetView>();
    if (m_nview != null && !hasRegister)
    {
      m_nview.Register<long>("RequestControl", RPC_RequestControl);
      m_nview.Register<long>("ReleaseControl", RPC_ReleaseControl);
      m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
      hasRegister = true;
    }
  }

  private void OnDestroy()
  {
    if (!hasRegister) return;
    m_nview.Unregister("RequestControl");
    m_nview.Unregister("ReleaseControl");
    m_nview.Unregister("RequestRespons");
  }

  public bool IsValid()
  {
    return this;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public bool Interact(Humanoid character, bool repeat, bool alt)
  {
    if (character == Player.m_localPlayer && isActiveAndEnabled)
    {
      var baseRoot = GetComponentInParent<MoveableBaseRootComponent>();
      var baseVehicle = GetComponentInParent<BaseVehicleController>();
      if (baseRoot != null)
      {
        baseRoot.ComputeAllShipContainerItemWeight();
      }

      if (baseVehicle != null)
      {
        baseVehicle.ComputeAllShipContainerItemWeight();
      }

      m_lastUsedControls = this;
      if (ShipInstance.Instance != null)
      {
        ShipInstance.Instance.m_controlGuiPos = transform;
        ShipInstance.Instance.ControlGuiPosition = transform;
      }
    }

    if (repeat)
    {
      return false;
    }

    if (!m_nview.IsValid())
    {
      return false;
    }

    if (!InUseDistance(character))
    {
      return false;
    }

    Player player = character as Player;
    if (player == null || player.IsEncumbered())
    {
      return false;
    }

    var playerOnShip = player.GetStandingOnShip();
    if (playerOnShip == null ||
        !(playerOnShip).name.Contains(PrefabController.WaterVehiclePrefabName))
    {
      Logger.LogDebug("Player is not on VVShip");
      return false;
    }

    m_nview.InvokeRPC("RequestControl", player.GetPlayerID());
    return false;
  }

  public Component GetControlledComponent()
  {
    return ShipInstance.Instance;
  }

  public Vector3 GetPosition()
  {
    return base.transform.position;
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    ShipInstance.Instance.ApplyControls(moveDir);
  }

  public string GetHoverText()
  {
    var controller = ShipInstance?.Controller?.Instance;
    if (controller == null)
    {
      return "";
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

    var waterVehicleController = GetComponentInParent<WaterVehicleController>();

    var isAnchored =
      waterVehicleController.VehicleFlags.HasFlag(WaterVehicleFlags
        .IsAnchored);
    var anchoredStatus = isAnchored ? "[<color=red><b>$mb_rudder_use_anchored</b></color>]" : "";
    var anchorText =
      isAnchored
        ? "$mb_rudder_use_anchor_disable_detail"
        : "$mb_rudder_use_anchor_enable_detail";
    var anchorKey =
      ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set"
        ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString()
        : ZInput.instance.GetBoundKeyString("Run");
    return Localization.instance.Localize(
      $"[<color=yellow><b>$KEY_Use</b></color>] <color=white><b>$mb_rudder_use</b></color> {anchoredStatus}\n[<color=yellow><b>{anchorKey}</b></color>] <color=white>{anchorText}</color> {shipStatsText}");
  }

  public string GetHoverName()
  {
    return Localization.instance.Localize(m_hoverText);
  }

  private void RPC_RequestControl(long sender, long playerID)
  {
    var isOwner = m_nview.IsOwner();
    var isInBoat = ShipInstance.IsPlayerInBoat(playerID);
    if (!isOwner || !isInBoat) return;

    if (GetUser() == playerID || !HaveValidUser())
    {
      m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
      m_nview.InvokeRPC(sender, "RequestRespons", true);
    }
    else
    {
      m_nview.InvokeRPC(sender, "RequestRespons", false);
    }
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (m_nview.IsOwner() && GetUser() == playerID)
    {
      m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
    }
  }

  private void RPC_RequestRespons(long sender, bool granted)
  {
    if (this != m_lastUsedControls)
    {
      m_lastUsedControls.RPC_RequestRespons(sender, granted);
      return;
    }

    if (!Player.m_localPlayer)
    {
      return;
    }

    if (granted)
    {
      Player.m_localPlayer.StartDoodadControl(this);
      if (AttachPoint != null)
      {
        Player.m_localPlayer.AttachStart(AttachPoint, null, hideWeapons: false, isBed: false,
          onShip: true, m_attachAnimation, m_detachOffset);
      }
    }
    else
    {
      Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
    }
  }

  public void OnUseStop(Player player)
  {
    if (m_nview.IsValid())
    {
      m_nview.InvokeRPC("ReleaseControl", player.GetPlayerID());
      if (AttachPoint != null)
      {
        player.AttachStop();
      }
    }
  }

  public bool HaveValidUser()
  {
    long user = GetUser();
    if (user != 0L)
    {
      return ShipInstance.IsPlayerInBoat(user);
    }

    return false;
  }

  private long GetUser()
  {
    if (!m_nview.IsValid())
    {
      return 0L;
    }

    return m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }

  private bool InUseDistance(Humanoid human)
  {
    return Vector3.Distance(human.transform.position, AttachPoint.position) < MaxUseRange;
  }
}