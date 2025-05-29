// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.ValheimVehicles.RPC;

namespace ValheimVehicles.Integrations.PowerSystem
{
  public static class PowerSystemRPC
  {
    private const string RPC_PlayerEnteredConduit_Name = "PowerSystem_PlayerEnteredConduit";
    private const string RPC_PlayerExitedConduit_Name = "PowerSystem_PlayerExitedConduit";
    private const string RPC_NotifyZDOsChanged_Name = "PowerSystem_NotifyZDOsChanged";
    private const string RPC_UpdatePowerConsumer_Name = "PowerSystem_UpdatePowerConsumer";
    public static bool hasRegistered = false;

    // fuel
    private const string RPC_AddFuelToSource_Name = "PowerSystem_AddFuelToSource";
    private const string RPC_CommitFuelUsed_Name = "PowerSystem_CommitFuelUsed";
    private const string RPC_ConduitOfferAllEitr_Name = "PowerSystem_ConduitOfferAllEitr";

    private static CustomRPC? RPC_CommitFuelUsedInstance;
    private static CustomRPC? RPC_NotifyZDOsChanged;
    private static CustomRPC? RPC_AddFuelInstance;

    public static void RegisterCustom()
    {
      // fuel
      RPC_AddFuelInstance = NetworkManager.Instance.AddRPC(RPC_AddFuelToSource_Name, RPC_PowerSystem_AddFuel, RPC_PowerSystem_AddFuel);
      RPC_CommitFuelUsedInstance = NetworkManager.Instance.AddRPC(RPC_CommitFuelUsed_Name, RPC_PowerSystem_CommitFuel, RPC_PowerSystem_CommitFuel);

      RPC_NotifyZDOsChanged = NetworkManager.Instance.AddRPC(RPC_NotifyZDOsChanged_Name, null, Client_NotifyZDOsChanged);
    }

    public static void Register()
    {
      if (hasRegistered) return;
      try
      {
        ZRoutedRpc.instance.Register<ZPackage>(RPC_PlayerExitedConduit_Name, RPC_PlayerExitedConduit);
        ZRoutedRpc.instance.Register<ZPackage>(RPC_UpdatePowerConsumer_Name, RPC_UpdatePowerConsumer);
        ZRoutedRpc.instance.Register<ZPackage>(RPC_ConduitOfferAllEitr_Name, RPC_ConduitOfferAllEitr);
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"Something bad happened during RPC registration this means critical power system messaging is not setup.\n {e}");
      }
      LoggerProvider.LogDebug("Registered PowerSystemRPCs");
      hasRegistered = true;
    }

    public static void Request_PowerZDOsChangedToNearbyPlayers(string networkId, List<ZDOID> zdos, PowerSimulationData simData, float range = 40f)
    {
      if (!ZNet.instance || RPC_NotifyZDOsChanged == null) return;
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
          RPC_NotifyZDOsChanged.SendPackage(peer.m_uid, pkg);
        }
      }
    }

    public static void Request_AddFuelToSource(ZDOID zdoid, int amount, string commitId)
    {
      var pkg = new ZPackage();
      pkg.Write(zdoid);
      pkg.Write(amount);
      pkg.Write(commitId);
      // ZRoutedRpc.instance.InvokeRoutedRPC(RPC_AddFuelToSource_Name, pkg);
      RPC_AddFuelInstance?.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
    }

    public static void Request_CommitFuelUsed(long sender, string commitId)
    {
      var pkg = new ZPackage();
      pkg.Write(commitId);

      RPC_CommitFuelUsedInstance?.SendPackage(sender, pkg);
      // RPC_AddFuelInstance.SendPackage(ZRoutedRpc.Everybody, pkg);
      // ZRoutedRpc.instance.InvokeRoutedRPC(sender, RPC_CommitFuelUsed_Name, pkg);
    }


  #region RPCS

    private static void RPC_PlayerEnteredConduit(long sender, ZPackage pkg)
    {
      var conduitId = pkg.ReadZDOID();
      var playerId = pkg.ReadLong();

      var zdo = ZDOMan.instance.GetZDO(conduitId);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_PlayerEnteredConduit] ZDO not found for {conduitId}");
        return;
      }

      if (!PowerSystemRegistry.TryGetData<PowerConduitData>(zdo, out var data))
      {
#if DEBUG
        LoggerProvider.LogDebugDebounced($"[PowerSystemRPC] No PowerConduitData found for {conduitId}");
#endif
        return;
      }
    }

    private static void RPC_PlayerExitedConduit(long sender, ZPackage pkg)
    {
      var conduitId = pkg.ReadZDOID();
      var playerId = pkg.ReadLong();

      var zdo = ZDOMan.instance.GetZDO(conduitId);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_PlayerEnteredConduit] ZDO not found for {conduitId}");
        return;
      }

      if (!PowerSystemRegistry.TryGetData<PowerConduitData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {conduitId}");
        return;
      }

      data.RemovePlayer(playerId);
    }

    public static void Server_UpdatePowerConsumer(ZDO zdo)
    {
      if (!PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var powerData)) return;
      powerData.SetDemandState(true);
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
        pkg.Write(player.GetPlayerID());
        pkg.Write(player.GetEitr());
        pkg.Write(player.GetMaxEitr());
      }

      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), RPC_ConduitOfferAllEitr_Name, pkg);
    }

    public static void RPC_ConduitCommitEitrOffer(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var amountToCommit = pkg.ReadInt();
      Player.m_localPlayer.UseEitr(amountToCommit);
    }

    public static void RPC_ConduitOfferAllEitr(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var conduitZDOID = pkg.ReadZDOID();

      var zdo = ZDOMan.instance.GetZDO(conduitZDOID);
      if (zdo == null)
      {
        LoggerProvider.LogWarning($"[RPC_ConduitOfferEitr] ZDO not found for {conduitZDOID}");
        return;
      }
      if (!PowerSystemRegistry.TryGetData<PowerConduitData>(zdo, out var conduitData)) return;

      var playerCount = pkg.ReadInt();
      for (var i = 0; i < playerCount; i++)
      {
        var playerId = pkg.ReadLong();
        var eitrAmount = pkg.ReadSingle();
        var eitrMaxAmount = pkg.ReadSingle();
        conduitData.AddOrUpdate(playerId, eitrAmount, eitrMaxAmount);
      }
    }

    public static void RPC_UpdatePowerConsumer(long sender, ZPackage pkg)
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
        return;
      }

      if (!PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {id}");
        return;
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

  #endregion

    public static void Request_UpdatePowerConsumer(ZDOID consumerId, PowerConsumerData data)
    {
      var pkg = new ZPackage();
      pkg.Write(consumerId);
      pkg.Write(data.IsDemanding);
      pkg.Write(data.BasePowerConsumption);
      pkg.Write((int)data.PowerIntensityLevel);
      ZRoutedRpc.instance.InvokeRoutedRPC(RPC_UpdatePowerConsumer_Name, pkg);
    }

    public static void Request_PlayerExitedConduit(ZDOID conduitId, long playerId)
    {
      var pkg = new ZPackage();
      pkg.Write(conduitId);
      pkg.Write(playerId);
      ZRoutedRpc.instance.InvokeRoutedRPC(RPC_PlayerExitedConduit_Name, pkg);
    }

    public static bool shouldForceUpdateClusterOnZDOChange = false;

    private static IEnumerator Client_NotifyZDOsChanged(long sender, ZPackage pkg)
    {
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
        if (!PowerSystemRegistry.TryGetByZdoid(zdoid, out var data)) yield break;
        data.Data.Load();
        // LoggerProvider.LogDebug($"[RPC] Client received update for ZDO: {zdoid} networkId: {networkId}");
      }
    }
  }
}