// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.Integrations.PowerSystem
{
  public static class PowerSystemRPC
  {
    private const string RPC_PlayerEnteredConduit_Name = "PowerSystem_PlayerEnteredConduit";
    private const string RPC_PlayerExitedConduit_Name = "PowerSystem_PlayerExitedConduit";
    private const string RPC_NotifyZDOsChanged_Name = "PowerSystem_NotifyZDOsChanged";

    public static void Register()
    {
      ZRoutedRpc.instance.Register<ZPackage>(RPC_PlayerEnteredConduit_Name, RPC_PlayerEnteredConduit);
      ZRoutedRpc.instance.Register<ZPackage>(RPC_PlayerExitedConduit_Name, RPC_PlayerExitedConduit);
      ZRoutedRpc.instance.Register<ZPackage>(RPC_NotifyZDOsChanged_Name, Client_NotifyZDOsChanged);
    }

    public static void SendPowerZDOsChangedToNearbyPlayers(string networkId, List<ZDOID> zdos, PowerNetworkSimData simData, float range = 40f)
    {
      var pkg = new ZPackage();
      pkg.Write(networkId);
      pkg.Write(zdos.Count);
      foreach (var zdoid in zdos)
        pkg.Write(zdoid);

      var allPlayers = Player.GetAllPlayers();
      var notified = new HashSet<long>(); // Player ID to avoid double-RPC

      // Precompute squared range
      var rangeSqr = range * range;

      foreach (var player in allPlayers)
      {
        if (player == null || player.IsDead()) continue;

        var playerId = player.GetPlayerID();
        var playerPos = player.transform.position;

        // Check if near any power node
        var isNearAny = false;

        foreach (var (source, zdo) in simData.Sources)
        {
          if ((zdo.GetPosition() - playerPos).sqrMagnitude <= rangeSqr)
          {
            isNearAny = true;
            break;
          }
        }

        if (!isNearAny)
        {
          foreach (var (storage, zdo) in simData.Storages)
          {
            if ((zdo.GetPosition() - playerPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (!isNearAny)
        {
          foreach (var (conduit, zdo) in simData.Conduits)
          {
            if ((zdo.GetPosition() - playerPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (!isNearAny)
        {
          foreach (var (pylon, zdo) in simData.Pylons)
          {
            if ((zdo.GetPosition() - playerPos).sqrMagnitude <= rangeSqr)
            {
              isNearAny = true;
              break;
            }
          }
        }

        if (isNearAny && notified.Add(playerId) && player.m_nview && player.m_nview.m_zdo != null)
        {
          ZRoutedRpc.instance.InvokeRoutedRPC(player.m_nview.m_zdo.GetOwner(), RPC_NotifyZDOsChanged_Name, pkg);
        }
      }
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

      if (!PowerZDONetworkManager.TryGetData<PowerConduitData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {conduitId}");
        return;
      }

      if (!data.PlayerIds.Contains(playerId))
      {
        data.PlayerIds.Add(playerId);
        data.ResolvePlayersFromIds();
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

      if (!PowerZDONetworkManager.TryGetData<PowerConduitData>(zdo, out var data))
      {
        LoggerProvider.LogWarning($"[PowerSystemRPC] No PowerConduitData found for {conduitId}");
        return;
      }

      data.PlayerIds.Remove(playerId);
      data.ResolvePlayersFromIds();
    }

  #endregion


    public static void SendPlayerEnteredConduit(ZDOID conduitId, long playerId)
    {
      var pkg = new ZPackage();
      pkg.Write(conduitId);
      pkg.Write(playerId);
      ZRoutedRpc.instance.InvokeRoutedRPC(RPC_PlayerEnteredConduit_Name, pkg);
    }

    public static void SendPlayerExitedConduit(ZDOID conduitId, long playerId)
    {
      var pkg = new ZPackage();
      pkg.Write(conduitId);
      pkg.Write(playerId);
      ZRoutedRpc.instance.InvokeRoutedRPC(RPC_PlayerExitedConduit_Name, pkg);
    }

    private static void Client_NotifyZDOsChanged(long sender, ZPackage pkg)
    {
      pkg.SetPos(0);
      var networkId = pkg.ReadString(); // Ensure this is read first
      var count = pkg.ReadInt();

      PowerNetworkControllerIntegration.MarkNetworkDirty(networkId);

      for (var i = 0; i < count; i++)
      {
        var zdoid = pkg.ReadZDOID();
        LoggerProvider.LogDebug($"[RPC] Client received update for ZDO: {zdoid} networkId: {networkId}");
        // Hook: Client should refresh visuals or cached logic here.
      }
    }

    public static void FirePowerZDOsChanged(List<ZDOID> changedZDOs)
    {
      if (changedZDOs == null || changedZDOs.Count == 0) return;

      var pkg = new ZPackage();
      pkg.Write(changedZDOs.Count);
      foreach (var id in changedZDOs)
        pkg.Write(id);

      ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_NotifyZDOsChanged_Name, pkg);
    }
  }
}