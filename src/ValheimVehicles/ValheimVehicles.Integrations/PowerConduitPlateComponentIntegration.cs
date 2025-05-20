// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;
using ValheimVehicles.Structs;

public class PowerConduitPlateComponentIntegration :
  NetworkedComponentIntegration<PowerConduitPlateComponentIntegration, PowerConduitPlateComponent, NoZDOConfig<PowerConduitPlateComponentIntegration>>,
  IPowerConduit
{
  public string NetworkId => Logic.NetworkId;
  public Vector3 Position => transform.position;
  public Vector3 ConnectorPoint => Position;

  public bool IsActive => Data.IsActive;
  public bool IsDemanding => Logic.IsDemanding;

  public float RequestPower(float deltaTime)
  {
    LoggerProvider.LogWarning("Deprecated RequestPower");
    return 0f;
  }
  public float SupplyPower(float deltaTime)
  {
    LoggerProvider.LogWarning("Deprecated SupplyPower");
    return 0f;
  }

  public PowerConduitData Data = new();
  public bool MustSync = false;
  public Rigidbody m_body;
  public FixedJoint m_joint;
  private Transform? _lastParent;

  private const string RPC_AddEitr_Name = nameof(RPC_AddEitr);

  protected override void RegisterDefaultRPCs()
  {
    RpcHandler.Register<float>(RPC_AddEitr_Name, RPC_AddEitr);
  }

  public static void Request_AddEitr(Player player, float amount)
  {
    if (player == null || amount <= 0f)
    {
      LoggerProvider.LogWarning("Player is null or amount is <= 0");
      return;
    }

    var netView = player.GetComponent<ZNetView>();
    if (!netView || !netView.IsValid())
    {
      LoggerProvider.LogWarning("Player has no ZNetView or is invalid");
      return;
    }

    if (!netView.IsOwner())
    {
      var zdoOwner = netView.GetZDO().GetOwner();
      netView.InvokeRPC(zdoOwner, RPC_AddEitr_Name, amount);
    }
    else
    {
      player.AddEitr(amount);
    }
  }

  private void HandlePlayerEnterActiveZone(Player player)
  {
    if (player == null) return;
    var id = player.GetPlayerID();
    if (!Data.PlayerIds.Contains(id))
    {
      Data.PlayerIds.Add(id);
      Data.ResolvePlayersFromIds();
    }
    Logic.SetHasPlayerInRange(Data.HasPlayersWithEitr);
  }

  private void HandlePlayerExitActiveZone(Player player)
  {
    if (player == null) return;
    Data.PlayerIds.Remove(player.GetPlayerID());
    Data.ResolvePlayersFromIds();
    Logic.SetHasPlayerInRange(Data.HasPlayersWithEitr);
  }

#region RPCS

  private void RPC_AddEitr(long sender, float amount)
  {
    if (Player.m_localPlayer != null)
    {
      Player.m_localPlayer.AddEitr(amount);
    }
  }

#endregion

#region Events

  private void OnTriggerEnter(Collider other)
  {
    var player = other.GetComponentInParent<Player>();
    if (!player)
    {
      Physics.IgnoreCollision(other, Logic.m_triggerCollider, true);
      return;
    }

    HandlePlayerEnterActiveZone(player);

    if (this.IsNetViewValid(out var netView))
    {
      PowerSystemRPC.SendPlayerEnteredConduit(netView.GetZDO().m_uid, player.GetPlayerID());
    }
  }

  private void OnTriggerExit(Collider other)
  {
    var player = other.GetComponentInParent<Player>();
    if (!player) return;
    HandlePlayerExitActiveZone(player);

    if (this.IsNetViewValid(out var netView))
    {
      PowerSystemRPC.SendPlayerExitedConduit(netView.GetZDO().m_uid, player.GetPlayerID());
    }
  }

#endregion

  protected override void Start()
  {
    this.WaitForZNetView((netView) =>
    {
      var zdo = netView.GetZDO();
      if (!PowerZDONetworkManager.TryGetData(zdo, out PowerConduitData data))
      {
        LoggerProvider.LogWarning("[PowerConduitPlateComponentIntegration] Failed to get PowerConduitData from PowerZDONetworkManager.");
        return;
      }
      Data = data;
      PowerNetworkController.RegisterPowerComponent(this);

      Logic.GetPlayerEitr = () => PowerConduitData.GetAverageEitr(Data.Players);
      Logic.AddPlayerEitr = (float val) => Data.AddEitrToPlayers(val);
      Logic.SubtractPlayerEitr = (float val) => Data.SubtractEitrFromPlayers(val);

      _lastParent = transform.parent;

      AddRigidbodyIfParentIsRigidbody();
    });

    base.Start();
  }

  public void OnTransformParentChanged()
  {
    var newParent = transform.parent;
    if (newParent && newParent == _lastParent) return;
    LoggerProvider.LogDev($"Parent changed from {_lastParent?.name ?? "null"} to {newParent?.name ?? "null"}");
    _lastParent = newParent;
    AddRigidbodyIfParentIsRigidbody();
  }

  protected void AddRigidbodyIfParentIsRigidbody()
  {
    var parentRigidbody = GetComponentInParent<Rigidbody>();
    if (!m_body)
    {
      m_body = gameObject.AddComponent<Rigidbody>();
    }
    m_body.isKinematic = !parentRigidbody;
    if (!parentRigidbody)
    {
      if (m_joint) Destroy(m_joint);
      return;
    }

    var joint = gameObject.AddComponent<FixedJoint>();
    joint.connectedBody = parentRigidbody;
    joint.enableCollision = false;
  }

  protected override void OnDestroy()
  {
    PowerNetworkController.UnregisterPowerComponent(this);
    base.OnDestroy();
  }

  public void SetNetworkId(string id)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var idFromZdo = netView.GetZDO().GetString(VehicleZdoVars.Power_NetworkId);
    Logic.SetNetworkId(idFromZdo);
  }
}