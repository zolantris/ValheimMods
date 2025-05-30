// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.RPC
{
  public static class PowerSystemRPC
  {
    // conduits
    private static readonly RPCEntity Conduit_PlayerExitedConduit_RPC = RPCManager.RegisterRPC(nameof(RPC_Conduit_PlayerExitedConduit), RPC_Conduit_PlayerExitedConduit);
    private static readonly RPCEntity Conduit_OfferAllEitr_RPC = RPCManager.RegisterRPC(nameof(RPC_Conduit_OfferAllEitr), RPC_Conduit_OfferAllEitr);

    // object config update
    private static readonly RPCEntity NotifyZDOsChanged_RPC = RPCManager.RegisterRPC(nameof(RPC_Client_NotifyZDOsChanged), RPC_Client_NotifyZDOsChanged);

    // consumers
    private static readonly RPCEntity UpdatePowerConsumer_RPC = RPCManager.RegisterRPC(nameof(RPC_UpdatePowerConsumer), RPC_UpdatePowerConsumer);

    // fuel
    private static readonly RPCEntity AddFuelToSource_RPC = RPCManager.RegisterRPC(nameof(RPC_PowerSystem_AddFuel), RPC_PowerSystem_AddFuel);
    private static readonly RPCEntity CommitFuelUsed_RPC = RPCManager.RegisterRPC(nameof(RPC_PowerSystem_CommitFuel), RPC_PowerSystem_CommitFuel);

    public static void Request_PowerZDOsChangedToNearbyPlayers(string networkId, List<ZDOID> zdos, PowerSimulationData simData, float range = 40f)
    {
      if (!ZNet.instance) return;
      var pkg = new ZPackage();
      pkg.Write(networkId);
      pkg.Write(zdos.Count);
      foreach (var zdoid in zdos)
        pkg.Write(zdoid);

      var allPeers = ZNet.instance.m_peers;
      var notified = new HashSet<long>(); // Player ID to avoid double-RPC

      // Precompute squared range
      var rangeSqr = range * range;

      foreach (var peer in allPeers)
      {
        if (peer == null || !peer.IsReady()) continue;

        // var playerId = player.GetPlayerID();
        // var playerPos = player.transform.position;
        var refPos = peer.m_refPos;

        // Check if near any power node
        var isNearAny = false;

        foreach (var source in simData.Sources)
        {
          if ((source.zdo.GetPosition() - refPos).sqrMagnitude <= rangeSqr)
          {
            isNearAny = true;
            break;
          }
        }

        if (!isNearAny)
        {
          foreach (var storage in simData.Storages)
          {
            if ((storage.zdo.GetPosition() - refPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (!isNearAny)
        {
          foreach (var conduit in simData.Conduits)
          {
            if ((conduit.zdo.GetPosition() - refPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (!isNearAny)
        {
          foreach (var pylon in simData.Pylons)
          {
            if ((pylon.zdo.GetPosition() - refPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (isNearAny && notified.Add(peer.m_uid))
        {
          NotifyZDOsChanged_RPC.Send(peer.m_uid, pkg);
        }
      }
    }

    public static void Request_AddFuelToSource(ZDOID zdoid, int amount, string commitId)
    {
      if (!ZNet.instance) return;
      var pkg = new ZPackage();
      pkg.Write(zdoid);
      pkg.Write(amount);
      pkg.Write(commitId);

      AddFuelToSource_RPC.Send(ZRoutedRpc.instance.GetServerPeerID(), pkg);
    }

    public static void Request_CommitFuelUsed(long sender, string commitId)
    {
      if (!ZNet.instance) return;
      var pkg = new ZPackage();
      pkg.Write(commitId);

      CommitFuelUsed_RPC.Send(sender, pkg);
    }


  #region RPCS

    private static IEnumerator RPC_Conduit_PlayerExitedConduit(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var conduitId = pkg.ReadZDOID();
      var playerId = pkg.ReadLong();

      var zdo = ZDOMan.instance.GetZDO(conduitId);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_PlayerEnteredConduit] ZDO not found for {conduitId}");
        yield break;
      }

      if (!PowerSystemRegistry.TryGetData<PowerConduitData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {conduitId}");
        yield break;
      }

      data.RemovePlayer(playerId);
    }

    public static void Server_UpdatePowerConsumer(ZDO zdo, bool isDemanding)
    {
      if (!PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var powerData)) return;
      powerData.SetDemandState(isDemanding);
      powerData.SetActive(true);
      powerData.SetPowerIntensity(PowerIntensityLevel.Low);
      powerData.Save();
    }

    /// <summary>
    /// A request called when players are within a conduit it is polled.
    /// </summary>
    /// <param name="conduitZdo"></param>
    /// <param name="players"></param>
    public static void Request_OfferAllPlayerEitr(ZDO conduitZdo, List<Player> players)
    {
      var pkg = new ZPackage();
      pkg.Write(conduitZdo.m_uid);
      pkg.Write(players.Count);

      foreach (var player in players)
      {
        pkg.Write(player.GetOwner());
        pkg.Write(player.GetEitr());
        pkg.Write(player.GetMaxEitr());
      }

      Conduit_OfferAllEitr_RPC.Send(pkg);
    }

    public static IEnumerator RPC_Conduit_OfferAllEitr(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var conduitZDOID = pkg.ReadZDOID();

      var zdo = ZDOMan.instance.GetZDO(conduitZDOID);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_ConduitOfferEitr] ZDO not found for {conduitZDOID}");
        yield break;
      }
      if (!PowerSystemRegistry.TryGetData<PowerConduitData>(zdo, out var conduitData)) yield break;

      var playerCount = pkg.ReadInt();
      for (var i = 0; i < playerCount; i++)
      {
        var playerPeerId = pkg.ReadLong();
        var eitrAmount = pkg.ReadSingle();
        var eitrMaxAmount = pkg.ReadSingle();
        conduitData.AddOrUpdate(playerPeerId, eitrAmount, eitrMaxAmount);
      }
    }

    public static IEnumerator RPC_UpdatePowerConsumer(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);

      var id = pkg.ReadZDOID();
      var isDemanding = pkg.ReadBool();
      var basePowerConsumption = pkg.ReadSingle();
      var powerMode = PowerConsumerData.GetPowerIntensityFromPrefab(pkg.ReadInt());

      var zdo = ZDOMan.instance.GetZDO(id);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_UpdatePowerConsumer] ZDO not found for {id}");
        yield break;
      }

      if (!PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {id}");
        yield break;
      }

      data.SetDemandState(isDemanding);
      data.SetBasePowerConsumption(basePowerConsumption);
      data.SetPowerIntensity(powerMode);

      if (zdo.IsOwner() || ZNet.instance.IsServer())
      {
        if (!zdo.IsOwner())
        {
          zdo.SetOwner(ZDOMan.GetSessionID());
        }
        data.Save();
      }
      else
      {
#if DEBUG
        LoggerProvider.LogWarning("Sent a update to power consumer but not owner or server.");
#endif
      }
    }

    private static IEnumerator RPC_PowerSystem_AddFuel(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var id = pkg.ReadZDOID();
      var amount = pkg.ReadInt();
      var commitId = pkg.ReadString();

      var zdo = ZDOMan.instance.GetZDO(id);
      if (!PowerSystemRegistry.TryGetData<PowerSourceData>(zdo, out var data))
      {
        LoggerProvider.LogWarning("Unable to find zdo requested for adding fuel.");
        yield break;
      }

      // update fuel for the active data model.
      data.AddFuel(amount);

      // notify sender they can remove items and update inventory.
      Request_CommitFuelUsed(sender, commitId);
    }

    private static IEnumerator RPC_PowerSystem_CommitFuel(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var commitId = pkg.ReadString();
      PowerHoverComponent.PendingFuelPromisesResolutions.Remove(commitId);
      yield return null;
    }

    private static IEnumerator RPC_Client_NotifyZDOsChanged(long sender, ZPackage pkg)
    {
      if (ZNet.instance == null || ZNet.instance.IsDedicated()) yield break;

      pkg.SetPos(0);
      var networkId = pkg.ReadString(); // Ensure this is read first
      var count = pkg.ReadInt();

      if (!PowerSystemClusterManager.TryBuildPowerNetworkSimData(networkId, out var networkData, shouldForceUpdateClusterOnZDOChange))
      {
        LoggerProvider.LogDebugDebounced("Failed to build network data, force rebuilding clusters now...");
        PowerSystemClusterManager.RebuildClusters();
      }

      for (var i = 0; i < count; i++)
      {
        var zdoid = pkg.ReadZDOID();
        if (!PowerSystemRegistry.TryGetByZdoid(zdoid, out var data)) continue;
        data.Data.Load();
      }
    }

  #endregion

    public static void Request_UpdatePowerConsumer(ZDOID consumerId, PowerConsumerData data)
    {
      var pkg = new ZPackage();
      pkg.Write(consumerId);
      pkg.Write(data.IsDemanding);
      pkg.Write(data.BasePowerConsumption);
      pkg.Write((int)data.PowerIntensityLevel);
      UpdatePowerConsumer_RPC.Send(pkg);
    }

    public static void Request_PlayerExitedConduit(ZDOID conduitId, long playerId)
    {
      var pkg = new ZPackage();
      pkg.Write(conduitId);
      pkg.Write(playerId);
      Conduit_PlayerExitedConduit_RPC.Send(pkg);
    }

    public static bool shouldForceUpdateClusterOnZDOChange = false;
  }
}