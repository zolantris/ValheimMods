using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class VehicleMovementController : MonoBehaviour, Interactable, Hoverable, IDoodadController,
  IRudderControls
{
  public IVehicleShip ShipInstance { get; set; }
  public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);
  [SerializeField] private string hoverText = "$valheim_vehicles_ship_controls";

  public string m_hoverText
  {
    get => hoverText;
    set => hoverText = value;
  }

  [SerializeField] private float maxUseRange = 10f;

  public float MaxUseRange
  {
    get => maxUseRange;
    set => maxUseRange = value;
  }

  public Transform AttachPoint { get; set; }

  public string m_attachAnimation = "Standing Torch Idle right";
  public VehicleMovementController m_lastUsedControls;
  public ZNetView m_nview;

  private bool hasRegister = false;

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    if (m_nview == null || m_nview.m_zdo == null) return;

    InitializeRPC();
  }

  /**
   * Will not be supported in v3.x.x
   */
  public void DEPRECATED_InitializeRudderWithShip(IVehicleShip vehicleShip,
    RudderWheelComponent rudderWheel, Ship ship)
  {
    m_nview = ship.m_nview;
    ship.m_controlGuiPos = transform;
    var rudderAttachPoint = rudderWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }
  }

  private void InitializeRPC()
  {
    if (m_nview != null && hasRegister)
    {
      UnRegisterRPCListeners();
    }

    if (m_nview != null && !hasRegister)
    {
      RegisterRPCListeners();
      hasRegister = true;
    }
  }

  private void UnRegisterRPCListeners()
  {
    m_nview.Unregister(nameof(RPC_RequestControl));
    m_nview.Unregister(nameof(RPC_ReleaseControl));
    m_nview.Unregister(nameof(RPC_RequestResponse));
    hasRegister = false;
  }

  private void RegisterRPCListeners()
  {
    m_nview.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
    m_nview.Register<long>(nameof(RPC_ReleaseControl), RPC_ReleaseControl);
    m_nview.Register<bool>(nameof(RPC_RequestResponse), RPC_RequestResponse);
    hasRegister = true;
  }

  public void InitializeRudderWithShip(IVehicleShip vehicleShip, RudderWheelComponent rudderWheel)
  {
    ShipInstance = vehicleShip;
    ShipInstance.Instance.m_controlGuiPos = transform;

    var rudderAttachPoint = rudderWheel.transform.Find("attachpoint");
    if (rudderAttachPoint != null)
    {
      AttachPoint = rudderAttachPoint.transform;
    }

    m_nview = vehicleShip.Instance.GetComponent<ZNetView>();

    if (m_nview != null && !hasRegister)
    {
      RegisterRPCListeners();
      hasRegister = true;
    }
  }

  private void OnDestroy()
  {
    if (!hasRegister) return;
    UnRegisterRPCListeners();
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
    var isBaseVehicleParent = false;
    if (character == Player.m_localPlayer && isActiveAndEnabled)
    {
      var baseVehicle = GetComponentInParent<BaseVehicleController>();
      if (baseVehicle != null)
      {
        isBaseVehicleParent = true;
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

      m_lastUsedControls = this;
    }

    if (repeat)
    {
      return false;
    }

    if (m_nview == null) return false;

    if (!m_nview.IsValid())
    {
      return false;
    }

    if (!InUseDistance(character))
    {
      return false;
    }

    var player = character as Player;


    var playerOnShipViaShipInstance =
      ShipInstance?.Instance?.GetComponentsInChildren<Player>() ?? null;

    /*
     * <note /> This logic allows for the player to just look at the Raft and see if the player is a child within it.
     */
    if (playerOnShipViaShipInstance != null)
      foreach (var player1 in playerOnShipViaShipInstance)
      {
        Logger.LogDebug(
          $"Interact PlayerId {player1.GetPlayerID()}, currentPlayerId: {player.GetPlayerID()}");
        if (player1.GetPlayerID() != player.GetPlayerID()) continue;
        m_nview.InvokeRPC(nameof(RPC_RequestControl), player.GetPlayerID());
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

    m_nview.InvokeRPC(nameof(RPC_RequestControl), player.GetPlayerID());
    return false;
  }

  public Component GetControlledComponent()
  {
    return ShipInstance?.Instance;
  }

  public Vector3 GetPosition()
  {
    return base.transform.position;
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    ShipInstance?.Instance.ApplyControls(moveDir);
  }

  public string GetHoverText()
  {
    var controller = ShipInstance?.VehicleController?.Instance;
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

    var isAnchored =
      controller.VehicleFlags.HasFlag(WaterVehicleFlags
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

    var isValidUser = false;
    if (GetUser() == playerID || !HaveValidUser())
    {
      m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
      isValidUser = true;
    }

    m_nview.InvokeRPC(sender, nameof(RPC_RequestResponse), isValidUser);
  }

  private void RPC_ReleaseControl(long sender, long playerID)
  {
    if (m_nview.IsOwner() && GetUser() == playerID)
    {
      m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
    }
  }

  private void RPC_RequestResponse(long sender, bool granted)
  {
    if (this != m_lastUsedControls)
    {
      m_lastUsedControls.RPC_RequestResponse(sender, granted);
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
    if (!m_nview.IsValid()) return;
    m_nview.InvokeRPC(nameof(RPC_ReleaseControl), player.GetPlayerID());
    if (AttachPoint != null)
    {
      player.AttachStop();
    }
  }

  public bool HaveValidUser()
  {
    var user = GetUser();
    return user != 0L && ShipInstance.IsPlayerInBoat(user);
  }

  private long GetUser()
  {
    return !m_nview.IsValid() ? 0L : m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
  }

  private bool InUseDistance(Humanoid human)
  {
    if (AttachPoint == null) return false;
    return Vector3.Distance(human.transform.position, AttachPoint.position) < MaxUseRange;
  }
}