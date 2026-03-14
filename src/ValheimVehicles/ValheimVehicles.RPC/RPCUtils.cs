using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
using Zolantris.Shared.Debug;
namespace ValheimVehicles.RPC;

public static class RPCUtils
{
  /// <summary>
  /// XZ-only distance check. Valheim loads zones based on horizontal distance only — Y is irrelevant.
  /// </summary>
  private static float DistanceXZ(Vector3 a, Vector3 b)
  {
    var dx = a.x - b.x;
    var dz = a.z - b.z;
    return Mathf.Sqrt(dx * dx + dz * dz);
  }

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
    var debugSafeTimer = Stopwatch.StartNew();
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
    if (Player.m_localPlayer != null)
    {
      foreach (var node in nodes)
      {
        if (DistanceXZ(node.GetPosition(), Player.m_localPlayer.transform.position) < maxDistance)
          return true;
      }
    }

    if (!ZNet.instance) return false;

    // Step 2: Iterate through peers and match them with nodes.
    var peers = ZNet.instance.GetPeers();
    if (peers.Count == 0) return false;
    foreach (var node in nodes)
    foreach (var peer in peers)
    {
      if (DistanceXZ(node.GetPosition(), peer.m_refPos) < maxDistance)
        return true;
    }

    return false;
  }

  /// <summary>
  /// Single-ZDO overload — avoids allocating a List per call when checking one vehicle at a time.
  /// Uses XZ-only distance since Valheim loads zones based on horizontal distance only.
  /// </summary>
  public static bool HasNearbyPlayersOrPeers(ZDO node, float maxDistance = 50f)
  {
    var pos = node.GetPosition();

    if (Player.m_localPlayer != null &&
        DistanceXZ(pos, Player.m_localPlayer.transform.position) < maxDistance)
      return true;

    var peers = ZNet.instance.GetPeers();
    foreach (var peer in peers)
    {
      if (DistanceXZ(pos, peer.m_refPos) < maxDistance)
        return true;
    }

    return false;
  }

  public static bool TryGetNearbyPeers(ZDO zdo, float maxDistance, out List<ZNetPeer> matchingPeers)
  {
    matchingPeers = new List<ZNetPeer>();

    var zdoPosition = zdo.GetPosition();

    if (Player.m_localPlayer && DistanceXZ(zdoPosition, Player.m_localPlayer.transform.position) < maxDistance)
    {
      var playerPeer = ZRoutedRpc.instance.GetPeer(Player.m_localPlayer.GetOwner());
      matchingPeers.Add(playerPeer);
    }

    var peers = ZNet.instance.GetPeers();
    foreach (var instanceMPeer in peers)
    {
      if (DistanceXZ(zdoPosition, instanceMPeer.m_refPos) < maxDistance)
        matchingPeers.Add(instanceMPeer);
    }

    return matchingPeers.Count > 0;
  }


  /// <summary>
  /// Runs if nearby. Requires the callback method to fire RPC to the peer or local player.
  ///
  /// Must be run on a dedicate server otherwise this is inaccurate.
  /// </summary>
  public static void RunIfNearby(ZDO zdo, float threshold, Action<long> action)
  {
    if (!ZNet.instance.IsServer())
    {
      LoggerProvider.LogError("This call is not supported on non server clients due to it not being able to decide which peers to run the request on.");
      return;
    }

    if (ZNet.instance && Player.m_localPlayer && DistanceXZ(zdo.GetPosition(), ZNet.instance.m_referencePosition) < threshold)
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