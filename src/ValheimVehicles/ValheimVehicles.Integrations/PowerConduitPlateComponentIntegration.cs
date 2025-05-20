using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

public class PowerConduitPlateComponentIntegration :
  NetworkedComponentIntegration<PowerConduitPlateComponentIntegration, PowerConduitPlateComponent, NoZDOConfig<PowerConduitPlateComponentIntegration>>,
  IPowerConduit
{
  private const float MinEitrDrainThreshold = 0.01f;
  private const float MaxEitrCapMargin = 0.1f; // If within 0.1 of max, skip charging
  public string NetworkId => Logic.NetworkId;
  public Vector3 Position => transform.position;
  public Transform ConnectorPoint => transform;

  public bool IsActive => true;
  public bool IsDemanding => Logic.IsDemanding;

  private readonly List<Player> _playersWithinZone = new();


  public bool MustSync = false;
  public Rigidbody m_body;
  public FixedJoint m_joint;
  private Transform? _lastParent;


  // Missing method for Players. There is no syncing method for Eitr additions only removals.
  private const string RPC_AddEitr_Name = nameof(RPC_AddEitr);
  private const string RPC_TriggerEnter_Name = nameof(RPC_TriggerEnter);
  private const string RPC_TriggerExit_Name = nameof(RPC_TriggerExit);

  protected override void RegisterDefaultRPCs()
  {
    RpcHandler.Register<float>(RPC_AddEitr_Name, RPC_AddEitr);
    RpcHandler.Register<long>(RPC_TriggerEnter_Name, RPC_TriggerEnter);
    RpcHandler.Register<long>(RPC_TriggerExit_Name, RPC_TriggerExit);
  }

  public static void Request_AddEitr(Player player, float amount)
  {
    if (player == null || amount <= 0f)
    {
      LoggerProvider.LogWarning($"Player is null or amount is <= 0");
      return;
    }

    var netView = player.GetComponent<ZNetView>();
    if (!netView || !netView.IsValid())
    {
      LoggerProvider.LogWarning($"Player has no ZNetView or is invalid");
      return;
    }

    // Only send to the owner of this player
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
    if (player != null && !_playersWithinZone.Contains(player))
    {
      _playersWithinZone.Add(player);
    }
    _playersWithinZone.RemoveAll(x => !x);

    Logic.SetHasPlayerInRange(GetAverageEitr() > 0f);
  }

  private void HandlePlayerExitActiveZone(Player player)
  {
    if (player != null)
    {
      _playersWithinZone.Remove(player);
    }
    _playersWithinZone.RemoveAll(x => !x);

    if (_playersWithinZone.Count == 0)
    {
      Logic.SetHasPlayerInRange(false);
      return;
    }

    Logic.SetHasPlayerInRange(GetAverageEitr() > 0f);
  }

#region RPCS

  private void RPC_AddEitr(long sender, float amount)
  {
    if (Player.m_localPlayer != null)
    {
      Player.m_localPlayer.AddEitr(amount);
    }
  }


  private void RPC_TriggerEnter(long sender, long playerId)
  {
    var player = Player.GetPlayer(playerId);
    HandlePlayerEnterActiveZone(player);
  }

  private void RPC_TriggerExit(long sender, long playerId)
  {
    var player = Player.GetPlayer(playerId);
    HandlePlayerExitActiveZone(player);
  }

#endregion


#region Events

  // Events
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
      netView.InvokeRPC(nameof(RPC_TriggerEnter_Name), player.GetPlayerID());
    }
  }

  private void OnTriggerExit(Collider other)
  {
    var player = other.GetComponentInParent<Player>();
    if (!player) return;
    HandlePlayerExitActiveZone(player);

    if (this.IsNetViewValid(out var netView))
    {
      netView.InvokeRPC(nameof(RPC_TriggerExit_Name), player.GetPlayerID());
    }
  }

#endregion

  protected override void Start()
  {
    this.WaitForZNetView(() =>
    {
      PowerNetworkController.RegisterPowerComponent(this);
    });

    if (!this.IsNetViewValid(out var netView)) return;
    base.Start();

    Logic.GetPlayerEitr = GetAverageEitr;
    Logic.AddPlayerEitr = AddEitrToPlayers;
    Logic.SubtractPlayerEitr = SubtractEitrFromPlayers;

    _lastParent = transform.parent;

    AddRigidbodyIfParentIsRigidbody();
  }

  // Very important. Let's us detect if this component gets moved into another parent like a vehicle.
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

    // joint allows us to sync the rigidbody without running a FixedUpdate having to sync both rotation and position relative to parent and a frozen local position.
    var joint = gameObject.AddComponent<FixedJoint>();
    joint.connectedBody = parentRigidbody;
    joint.enableCollision = false; // usually what you want
  }

  protected override void OnDestroy()
  {
    PowerNetworkController.UnregisterPowerComponent(this);
    base.OnDestroy();
  }

  public void SetNetworkId(string id)
  {
    Logic.SetNetworkId(id);
  }

  public float RequestPower(float deltaTime)
  {
    return Logic.RequestPower(deltaTime);
  }
  public float SupplyPower(float deltaTime)
  {
    return Logic.SupplyPower(deltaTime);
  }

  private void CleanupPlayerList()
  {
    for (var i = 0; i < _playersWithinZone.Count; i++)
    {
      if (_playersWithinZone[i] == null)
      {
        _playersWithinZone.FastRemoveAt(ref i);
        i--;
      }
    }
  }

  private float GetAverageEitr()
  {
    CleanupPlayerList();

    var count = 0;
    var total = 0f;

    foreach (var player in _playersWithinZone)
    {
      total += player.m_eitr;
      count++;
    }

    return count > 0 ? total / count : 0f;
  }

  private void AddEitrToPlayers(float amount)
  {
    CleanupPlayerList();

    if (_playersWithinZone.Count == 0 || amount <= 0f) return;

    // Filter out players near Eitr cap
    List<Player> validReceivers = new(_playersWithinZone.Count);
    foreach (var player in _playersWithinZone)
    {
      if (player.m_eitr < player.m_maxEitr - MaxEitrCapMargin)
      {
        validReceivers.Add(player);
      }
    }

    if (validReceivers.Count == 0) return;

    var perPlayer = amount / validReceivers.Count;
    foreach (var player in validReceivers)
    {
      Request_AddEitr(player, perPlayer);
    }
  }

  private void SubtractEitrFromPlayers(float amount)
  {
    CleanupPlayerList();

    if (_playersWithinZone.Count == 0 || amount <= 0f) return;

    List<Player> validPlayers = new(_playersWithinZone.Count);
    foreach (var player in _playersWithinZone)
    {
      if (player.HaveEitr(0.01f)) // Must have *some* Eitr
      {
        validPlayers.Add(player);
      }
    }

    if (validPlayers.Count == 0) return;

    var remaining = amount;
    var attempts = 0;

    while (remaining > 0f && attempts++ < 5)
    {
      var perPlayer = remaining / validPlayers.Count;
      List<Player> stillValid = new(validPlayers.Count);

      foreach (var player in validPlayers)
      {
        if (player.HaveEitr(perPlayer))
        {
          player.UseEitr(perPlayer);
          remaining -= perPlayer;
          stillValid.Add(player);
        }
      }

      if (stillValid.Count == 0) break;
      validPlayers = stillValid;
    }
  }
}