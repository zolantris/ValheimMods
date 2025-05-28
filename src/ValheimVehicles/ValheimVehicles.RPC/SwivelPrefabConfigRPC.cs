using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.ValheimVehicles.RPC;

public struct SwivelMotionUpdate
{
  public MotionState MotionState;
  public double StartTime;
  public float Duration;
  public void WriteTo(ZPackage pkg)
  {
    pkg.Write((int)MotionState);
    pkg.Write(StartTime);
    pkg.Write(Duration);
  }
  public static SwivelMotionUpdate ReadFrom(ZPackage pkg)
  {
    return new SwivelMotionUpdate
    {
      MotionState = (MotionState)pkg.ReadInt(),
      StartTime = pkg.ReadDouble(),
      Duration = pkg.ReadSingle()
    };
  }
}

public class SwivelPrefabConfigRPC
{
  public static CustomRPC? RPCInstance_Swivel_NextMotionState;
  public static CustomRPC? RPCInstance_Swivel_SetMotionState;
  public static CustomRPC? RPCInstance_Swivel_BroadcastMotionAnimation;
  public static bool hasRegistered = false;

  /// <summary>
  /// Global RPCs that must be synced authoritatively on the server.
  /// </summary>
  public static void RegisterCustom()
  {
    if (hasRegistered) return;
    RPCInstance_Swivel_NextMotionState = NetworkManager.Instance.AddRPC(RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_NextMotionState)), RPC_Swivel_NextMotionState, RPC_Swivel_NextMotionState);
    RPCInstance_Swivel_SetMotionState = NetworkManager.Instance.AddRPC(RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_SetMotionState)), RPC_Swivel_SetMotionState, RPC_Swivel_SetMotionState);
    RPCInstance_Swivel_BroadcastMotionAnimation = NetworkManager.Instance.AddRPC(RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_BroadCastMotionUpdate)), RPC_Swivel_BroadCastMotionUpdate, RPC_Swivel_BroadCastMotionUpdate);
    hasRegistered = true;
  }

  public static void Request_SetMotionState(ZDO zdo, MotionState motionState)
  {
    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    pkg.Write((int)motionState);
    // ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_SetMotionState), pkg);
    RPCInstance_Swivel_SetMotionState?.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
  }

  public static void Request_NextMotion(ZDO zdo, MotionState currentMotionState)
  {
    var pkg = new ZPackage();
    var zdoid = zdo.m_uid;

    pkg.Write(zdoid);
    pkg.Write((int)currentMotionState);

    RPCInstance_Swivel_NextMotionState?.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
  }

  public static IEnumerator RPC_Swivel_NextMotionState(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);

    var zdoId = pkg.ReadZDOID();
    var clientMotionState = (MotionState)pkg.ReadInt();

    var zdo = ZDOMan.instance.GetZDO(zdoId);

    if (zdo == null)
    {
      LoggerProvider.LogError("No ZDO found for RPC_NextMotionState. Bailing...");
      yield break;
    }

    // server motion state is used to determine the next motion state. client only used for comparison if desynced.
    var currentMotionState = (MotionState)zdo.GetInt(SwivelCustomConfig.Key_MotionState);
    var nextMotionState = SwivelCustomConfig.GetNextMotionState(currentMotionState);

    // Clone current config (to avoid mutating it before validation)
#if DEBUG
    LoggerProvider.LogDebug($"Server previous has current MotionState: {currentMotionState}, (pending) nextMotionState: {nextMotionState}");
#endif

    // Validate if client was expecting the correct next state
    if (clientMotionState != currentMotionState)
    {
      LoggerProvider.LogDev($"[Swivel] MotionState desync. Got <{clientMotionState}> but expected <{nextMotionState}> Re-sending config.");
      PrefabConfigRPC.Request_SyncConfigKeys(zdo, [SwivelCustomConfig.Key_MotionState], sender);
      yield return new WaitForSeconds(0.1f);
    }

    if (PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var powerData))
    {
      if (!powerData.IsActive)
      {
        powerData.SetActive(true);
      }
      if (!powerData.IsDemanding)
      {
        powerData.SetDemandState(true);
      }
    }

    Internal_SetAndNotifyMotionState(zdo, nextMotionState, true);
  }

  public static Coroutine? _pendingMotionCoroutine;

  /// <summary>
  /// Motion should always be from a "ToStart" or "ToTarget" state.
  /// </summary>
  /// <param name="zdo"></param>
  /// <param name="currentState"></param>
  public static void Server_StartMotion(ZDO zdo, MotionState currentState)
  {
    if (RPCInstance_Swivel_BroadcastMotionAnimation == null) return;
    var swivelConfig = new SwivelCustomConfig();
    swivelConfig = swivelConfig.Load(zdo, swivelConfig);
    var duration = SwivelComponentBridge.ComputeMotionDuration(swivelConfig, currentState);

    var update = new SwivelMotionUpdate
    {
      StartTime = ZNet.instance.GetTimeSeconds(), // Use the game/server clock, not Time.time!
      Duration = duration,
      MotionState = currentState
    };

    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    update.WriteTo(pkg);

    RPCUtils.RunIfNearby(zdo, 100f, (sender) =>
    {
      RPCInstance_Swivel_BroadcastMotionAnimation.SendPackage(sender, pkg);
    });

    if (PowerNetworkController.Instance == null) return;
    if (_pendingMotionCoroutine != null) PowerNetworkController.Instance.StopCoroutine(_pendingMotionCoroutine);
    _pendingMotionCoroutine = PowerNetworkController.Instance.StartCoroutine(WaitAndFinishMotion(PowerNetworkController.Instance, zdo, currentState, duration));
  }

  public static IEnumerator RPC_Swivel_BroadCastMotionUpdate(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoid = pkg.ReadZDOID();
    var motionUpdate = SwivelMotionUpdate.ReadFrom(pkg);
    var zdo = ZDOMan.instance.GetZDO(zdoid);

    if (zdo == null) yield break;

    if (!SwivelComponentBridge.ZdoToComponent.TryGetValue(zdo, out var swivelComponentBridge))
    {
      yield break;
    }

    swivelComponentBridge.SetAuthoritativeMotion(motionUpdate, true);
    yield return false;
  }

  public static IEnumerator WaitAndFinishMotion(MonoBehaviour behavior, ZDO zdo, MotionState currentMotion, float duration)
  {
    yield return new WaitForSeconds(duration);

    if (!ZNet.instance || !behavior || !behavior.isActiveAndEnabled) yield break;
    // Only finish if state not canceled
    var completedMotionState = SwivelCustomConfig.GetCompleteMotionState(currentMotion);
    zdo.TryClaimOwnership();
    zdo.Set(SwivelCustomConfig.Key_MotionState, (int)completedMotionState);
    yield return null;


    var hasLocalInstance = SwivelComponentBridge.ZdoToComponent.TryGetValue(zdo, out var swivelComponentBridge);
    if (hasLocalInstance && swivelComponentBridge != null)
    {
      swivelComponentBridge.SetAuthoritativeMotion(new SwivelMotionUpdate
      {
        MotionState = completedMotionState,
        StartTime = 0f,
        Duration = 0f
      }, false);
    }

    // should always sync otherwise there is a desync on clients that are servers.
    PrefabConfigRPC.Request_SyncConfigKeys(zdo, [SwivelCustomConfig.Key_MotionState]);

    if (PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var powerData))
    {
      if (powerData.IsDemanding)
      {
        powerData.SetDemandState(false);
      }
      if (!PowerSystemClusterManager.TryBuildPowerNetworkSimData(powerData.NetworkId, out var simNetworkData)) yield break;
      PowerSystemRPC.Request_PowerZDOsChangedToNearbyPlayers(powerData.NetworkId, new List<ZDOID> { zdo.m_uid }, simNetworkData);
    }
  }

  private static void Internal_SetAndNotifyMotionState(ZDO zdo, MotionState state, bool canStartMotion)
  {
    // take ownership if not owner
    zdo.TryClaimOwnership();

    // set the ZDO
    zdo.Set(SwivelCustomConfig.Key_MotionState, (int)state);

    if (_pendingMotionCoroutine != null && PowerNetworkController.Instance != null)
    {
      PowerNetworkController.Instance.StopCoroutine(_pendingMotionCoroutine);
    }
    if (canStartMotion && (state == MotionState.ToStart || state == MotionState.ToTarget))
    {
      Server_StartMotion(zdo, state);
    }

    // PrefabConfigRPC.Request_SyncConfigKeys(zdo, [SwivelCustomConfig.Key_MotionState]);


    // only run subscriber update for side-effect/reload callback.
    // Dedicated server will not have any of these components so skip it.
    // if (!ZNet.instance.IsDedicated())
    // {
    //   if (PrefabConfigRPC.ZdoToPrefabConfigListeners.TryGetValue(zdo, out var subscriber))
    //   {
    //     if (subscriber == null) return;
    //     subscriber.Load(zdo, [SwivelCustomConfig.Key_MotionState]);
    //   }
    // }

    if (PowerSystemRegistry.TryGetData<PowerConsumerData>(zdo, out var powerData))
    {
      if (!powerData.IsActive)
      {
        powerData.SetActive(true);
      }
      if (!powerData.IsDemanding)
      {
        powerData.SetDemandState(true);
      }
    }
  }

  public static IEnumerator RPC_Swivel_SetMotionState(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoId = pkg.ReadZDOID();
    var clientMotionState = (MotionState)pkg.ReadInt();

    var zdo = ZDOMan.instance.GetZDO(zdoId);
    if (zdo == null) yield break;

    // Read client-reported MotionState
    var serverMotionState = (MotionState)zdo.GetInt(SwivelCustomConfig.Key_MotionState);

    if (clientMotionState == serverMotionState)
    {
      yield break;
    }

    // do not start motion sync. This call should be for force syncing a motion state.
    Internal_SetAndNotifyMotionState(zdo, clientMotionState, false);
  }
}