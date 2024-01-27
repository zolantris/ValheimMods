using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class ValheimShipControls : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
  public string m_hoverText = "";

  public VVShip ShipInstance;

  public float m_maxUseRange = 10f;

  public Transform m_attachPoint;

  public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

  public string m_attachAnimation = "attach_chair";
  public ValheimShipControls m_lastUsedControls;
  public ZNetView m_nview;

  private void Awake()
  {
    // m_nview = m_ship.GetComponent<ZNetView>();
    // m_nview.Register<long>("RequestControl", RPC_RequestControl);
    // m_nview.Register<long>("ReleaseControl", RPC_ReleaseControl);
    // m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
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
      if (baseRoot != null)
      {
        baseRoot.ComputeAllShipContainerItemWeight();
      }

      m_lastUsedControls = this;
      ShipInstance.m_controlGuiPos.position = transform.position;
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

    if ((player.GetStandingOnShip() as object) as VVShip != ShipInstance)
    {
      Logger.LogDebug("Player is not on VVShip");
      return false;
    }

    m_nview.InvokeRPC("RequestControl", player.GetPlayerID());
    return false;
  }

  public Component GetControlledComponent()
  {
    return ShipInstance;
  }

  public Vector3 GetPosition()
  {
    return base.transform.position;
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    ShipInstance.ApplyControlls(moveDir);
  }

  public string GetHoverText()
  {
    var controller = GetComponentInParent<WaterVehicleController>();
    if (controller == null)
    {
      controller = GetComponent<WaterVehicleController>();
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
      waterVehicleController.m_flags.HasFlag(WaterVehicleController.MBFlags
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
    if (m_nview.IsOwner() && ShipInstance.IsPlayerInBoat(playerID))
    {
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
      if (m_attachPoint != null)
      {
        Player.m_localPlayer.AttachStart(m_attachPoint, null, hideWeapons: false, isBed: false,
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
      if (m_attachPoint != null)
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
    return Vector3.Distance(human.transform.position, m_attachPoint.position) < m_maxUseRange;
  }
}