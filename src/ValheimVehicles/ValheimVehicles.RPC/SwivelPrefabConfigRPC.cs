using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
namespace ValheimVehicles.RPC;

/// <summary>
/// This only needs to be a class due to DurationLerp being mutated.
/// </summary>
public class SwivelMotionUpdateData
{
  public MotionState MotionState;
  public double StartTime;
  public float Duration;
  public float DurationLerp;

  public float RemainingDuration => Duration - DurationLerp * Duration;

  public void WriteTo(ZPackage pkg)
  {
    pkg.Write((int)MotionState);
    pkg.Write(StartTime);
    pkg.Write(Duration);
    pkg.Write(DurationLerp);
  }
  public static SwivelMotionUpdateData ReadFrom(ZPackage pkg)
  {
    return new SwivelMotionUpdateData
    {
      MotionState = (MotionState)pkg.ReadInt(),
      StartTime = pkg.ReadDouble(),
      Duration = pkg.ReadSingle(),
      DurationLerp = pkg.ReadSingle()
    };
  }
}

public static class SwivelPrefabConfigRPC
{
  public static RPCEntity BroadCastMotionRPC;
  public static RPCEntity NextMotionRPC;
  public static RPCEntity SetMotionRPC;

  public static void RegisterAll()
  {
    BroadCastMotionRPC = RPCManager.RegisterRPC(nameof(RPC_Swivel_BroadCastMotionUpdate), RPC_Swivel_BroadCastMotionUpdate);
    NextMotionRPC = RPCManager.RegisterRPC(nameof(RPC_Swivel_NextMotionState), RPC_Swivel_NextMotionState);
    SetMotionRPC = RPCManager.RegisterRPC(nameof(RPC_Swivel_SetMotionState), RPC_Swivel_SetMotionState);
  }

  public static void Request_SetMotionState(ZDO zdo, MotionState motionState)
  {
    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    pkg.Write((int)motionState);

    SetMotionRPC.Send(ZRoutedRpc.instance.GetServerPeerID(), pkg);
  }

  public static void Request_NextMotion(ZDO zdo, MotionState currentMotionState)
  {
    var pkg = new ZPackage();
    var zdoid = zdo.m_uid;

    pkg.Write(zdoid);
    pkg.Write((int)currentMotionState);

    // if (ZNet.IsSinglePlayer || ZNet.instance.IsServer())
    // {
    //   ZNet.instance.StartCoroutine(RPC_Swivel_NextMotionState(zdo.GetOwner(), pkg));
    //   return;
    // }
    NextMotionRPC.Send(ZRoutedRpc.instance.GetServerPeerID(), pkg);
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
    }

    Internal_SetAndNotifyMotionState(zdo, nextMotionState, true);
  }

  private static Dictionary<ZDOID, Coroutine?> _pendingMotionCoroutines = new();
  private static Dictionary<ZDOID, SwivelMotionUpdateData> _pendingMotionUpdateLerps = new();

  public static void StopSwivelUpdate(ZDOID zdoid)
  {
    if (PowerNetworkController.Instance == null || !_pendingMotionCoroutines.TryGetValue(zdoid, out var coroutine)) return;
    if (coroutine != null)
    {
      PowerNetworkController.Instance.StopCoroutine(coroutine);
    }
    _pendingMotionCoroutines.Remove(zdoid);
  }

  public static void StartSwivelUpdate(ZDOID zdoid, ZDO zdo, MotionState currentState, SwivelMotionUpdateData motionUpdateData)
  {
    if (PowerNetworkController.Instance == null) return;
    if (_pendingMotionCoroutines.TryGetValue(zdoid, out var coroutine) && coroutine != null)
    {
      PowerNetworkController.Instance.StopCoroutine(coroutine);
    }
    _pendingMotionCoroutines[zdoid] = PowerNetworkController.Instance.StartCoroutine(WaitAndFinishMotion(PowerNetworkController.Instance, zdo, currentState, motionUpdateData));
  }

  /// <summary>
  /// Motion should always be from a "ToStart" or "ToTarget" state.
  /// </summary>
  /// <param name="zdo"></param>
  /// <param name="currentState"></param>
  public static void Server_StartMotion(ZDO zdo, MotionState currentState)
  {
    if (PowerNetworkController.Instance == null) return;
    var swivelConfig = new SwivelCustomConfig();
    swivelConfig = swivelConfig.Load(zdo, swivelConfig);

    var duration = SwivelComponentBridge.ComputeMotionDuration(swivelConfig, currentState);

    var remainingLerpMultiplier = 0f;
    if (_pendingMotionUpdateLerps.TryGetValue(zdo.m_uid, out var lastUpdate))
    {
      // At zero we have no decrease in time. At near 1 we immediately resolve as we near multiply by zero. 
      remainingLerpMultiplier = Mathf.Clamp01(1 - lastUpdate.DurationLerp);
      _pendingMotionUpdateLerps.Remove(zdo.m_uid);
    }

    var update = new SwivelMotionUpdateData
    {
      StartTime = ZNet.instance.GetTimeSeconds(), // Use the game/server clock, not Time.time!
      Duration = duration,
      DurationLerp = remainingLerpMultiplier,
      MotionState = currentState
    };

    _pendingMotionUpdateLerps[zdo.m_uid] = update;

    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    update.WriteTo(pkg);

    RPCUtils.RunIfNearby(zdo, 100f, (sender) =>
    {
      BroadCastMotionRPC.Send(sender, pkg);
    });

    StopSwivelUpdate(zdo.m_uid);
    StartSwivelUpdate(zdo.m_uid, zdo, currentState, update);
  }

  public static IEnumerator RPC_Swivel_BroadCastMotionUpdate(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoid = pkg.ReadZDOID();
    var motionUpdate = SwivelMotionUpdateData.ReadFrom(pkg);
    var zdo = ZDOMan.instance.GetZDO(zdoid);

    if (zdo == null) yield break;

    if (!SwivelComponentBridge.ZdoToComponent.TryGetValue(zdo, out var swivelComponentBridge))
    {
      yield break;
    }

    swivelComponentBridge.SetAuthoritativeMotion(motionUpdate, true);
  }

  public static IEnumerator WaitAndFinishMotion(MonoBehaviour behavior, ZDO zdo, MotionState currentMotion, SwivelMotionUpdateData swivelMotionUpdateData)
  {
    if (zdo == null || !zdo.IsValid()) yield break;

    // always start from the lerped value * Duration to get a difference between.
    var currentDuration = swivelMotionUpdateData.DurationLerp * swivelMotionUpdateData.Duration;

    var zdoid = zdo.m_uid;
    // we lerp track motion updates per frame so we accurately match things.
    while (behavior != null && behavior.isActiveAndEnabled && currentDuration < swivelMotionUpdateData.Duration)
    {
      currentDuration += Time.deltaTime;
      swivelMotionUpdateData.DurationLerp = Mathf.Clamp01(currentDuration / swivelMotionUpdateData.Duration);
      yield return null;
    }
    if (!ZNet.instance || zdo == null || !behavior || !behavior.isActiveAndEnabled)
    {
      _pendingMotionUpdateLerps.Remove(zdoid);
      yield break;
    }
    // Only finish if state not canceled
    var completedMotionState = SwivelCustomConfig.GetCompleteMotionState(currentMotion);
    zdo.TryClaimOwnership();
    zdo.Set(SwivelCustomConfig.Key_MotionState, (int)completedMotionState);
    yield return null;

    var hasLocalInstance = SwivelComponentBridge.ZdoToComponent.TryGetValue(zdo, out var swivelComponentBridge);

    // stop authoritative update.
    if (hasLocalInstance && swivelComponentBridge != null)
    {
      swivelComponentBridge.SetAuthoritativeMotion(new SwivelMotionUpdateData
      {
        MotionState = completedMotionState,
        StartTime = 0f,
        Duration = 0f
      }, false);
    }
    _pendingMotionUpdateLerps.Remove(zdo.m_uid);

    PowerSystemRPC.Server_UpdatePowerConsumer(zdo, false);

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
    if (zdo == null) return;

    // take ownership if not owner
    zdo.TryClaimOwnership();

    // set the ZDO
    zdo.Set(SwivelCustomConfig.Key_MotionState, (int)state);

    StopSwivelUpdate(zdo.m_uid);
    if (canStartMotion && (state == MotionState.ToStart || state == MotionState.ToTarget))
    {
      PowerSystemRPC.Server_UpdatePowerConsumer(zdo, true);
      Server_StartMotion(zdo, state);
    }
    else
    {
      PowerSystemRPC.Server_UpdatePowerConsumer(zdo, false);
      _pendingMotionUpdateLerps.Remove(zdo.m_uid);
    }

    // PrefabConfigRPC.Request_SyncConfigKeys(zdo, [SwivelCustomConfig.Key_MotionState]);


    // only run subscriber update for side-effect/reload callback.
    // Dedicated server will not have any of these components so skip it.
    if (!ZNet.instance.IsDedicated())
    {
      if (PrefabConfigRPC.ZdoToPrefabConfigListeners.TryGetValue(zdo, out var subscriber))
      {
        if (subscriber == null) return;
        subscriber.Load(zdo, [SwivelCustomConfig.Key_MotionState]);
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