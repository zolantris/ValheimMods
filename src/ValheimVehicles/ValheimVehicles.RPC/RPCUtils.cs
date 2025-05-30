using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared.Debug;
namespace ValheimVehicles.RPC;

public static class RPCUtils
{
  public static string GetRPCPrefix(string name)
  {
    return $"{ValheimVehiclesPlugin.ModName}_{name}";
  }
  public static string GetRPCPrefix(Action method)
  {
    return $"{ValheimVehiclesPlugin.ModName}_{nameof(method)}";
  }

  public static Coroutine WithSafeRPCRegister(this MonoBehaviour mb, Action action)
  {
    return mb.StartCoroutine(SafeRPCRegister(action));
  }
  public static IEnumerator SafeRPCRegister(Action action)
  {
    var debugSafeTimer = DebugSafeTimer.StartNew();
    while (debugSafeTimer.ElapsedMilliseconds < 10000f && (!ZNet.instance || ZRoutedRpc.instance != null || !ZNetScene.instance))
    {
      yield return null;
    }

    if (debugSafeTimer.ElapsedMilliseconds >= 10000f)
    {
      LoggerProvider.LogError("Bailed on rpc method");
      yield break;
    }

    action?.Invoke();
  }

  /// <summary>
  /// A method to check if there is a nearby peer so we do not spam peers across the map. Or run methods on ZDOs that will not be loaded for a long while.
  /// </summary>
  /// <param name="nodes"></param>
  /// <param name="maxDistance"></param>
  /// <returns></returns>
  public static bool HasNearbyPlayersOrPeers(List<ZDO> nodes, float maxDistance = 50f)
  {
    var canRun = false;

    if (Player.m_localPlayer != null)
    {
      foreach (var node in nodes)
      {
        var pos = node.GetPosition();
        if (Vector3.Distance(pos, Player.m_localPlayer.transform.position) < maxDistance)
        {
          canRun = true;
          break;
        }
      }
    }

    if (canRun) return true;

    // Step 2: Iterate through peers and match them with nodes.
    var peers = ZNet.instance.GetPeers();
    if (peers.Count == 0) return false;
    // iterate through each node first as this may be a quicker match.
    foreach (var node in nodes)
    foreach (var instanceMPeer in peers)
    {
      var pos = node.GetPosition();
      if (Vector3.Distance(pos, instanceMPeer.m_refPos) < maxDistance)
      {
        canRun = true;
        break;
      }
    }

    return canRun;
  }

  public static bool TryGetNearbyPeers(ZDO zdo, float maxDistance, out List<ZNetPeer> matchingPeers)
  {
    matchingPeers = new List<ZNetPeer>();

    var zdoPosition = zdo.GetPosition();

    if (Player.m_localPlayer && Vector3.Distance(zdoPosition, Player.m_localPlayer.transform.position) < maxDistance)
    {
      var playerPeer = ZRoutedRpc.instance.GetPeer(Player.m_localPlayer.GetOwner());
      matchingPeers.Add(playerPeer);
      zdo.GetPosition();
    }

    var peers = ZNet.instance.GetPeers();
    foreach (var instanceMPeer in peers)
    {
      if (Vector3.Distance(zdoPosition, instanceMPeer.m_refPos) < 25f)
      {
        matchingPeers.Add(instanceMPeer);
      }
    }

    return matchingPeers.Count > 0;
  }


  /// <summary>
  /// Runs if nearby. Requires the callback method to fire RPC to the peer or local player.
  /// </summary>
  public static void RunIfNearby(ZDO zdo, float threshold, Action<long> action)
  {
    if (ZNet.instance && Player.m_localPlayer && Vector3.Distance(zdo.GetPosition(), ZNet.instance.m_referencePosition) < threshold)
    {
      action(Player.m_localPlayer.GetOwner());
    }
    if (!TryGetNearbyPeers(zdo, threshold, out var matchingPeers)) return;
    matchingPeers.ForEach(x =>
    {
      if (x == null) return;
      action(x.m_uid);
    });
  }
}