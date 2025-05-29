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

public class SwivelMotionUpdateLerp
{
  public SwivelMotionUpdate update;
  public float lerp = 0f;
}

public class SwivelPrefabConfigRPC
{
  public static bool hasRegistered = false;

  public static string SetMotionRPC_Name = RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_SetMotionState));
  public static string NextMotionRPC_Name = RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_NextMotionState));
  public static string BroadCastMotionRPC_Name = RPCUtils.GetRPCPrefix(nameof(RPC_Swivel_BroadCastMotionUpdate));
  /// <summary>
  /// Global RPCs that must be synced authoritatively on the server.
  /// </summary>
  public static void Register()
  {
    if (hasRegistered) return;
    ZRoutedRpc.instance.Register<ZPackage>(NextMotionRPC_Name, RPC_Swivel_NextMotionState);
    ZRoutedRpc.instance.Register<ZPackage>(SetMotionRPC_Name, RPC_Swivel_SetMotionState);
    ZRoutedRpc.instance.Register<ZPackage>(BroadCastMotionRPC_Name, RPC_Swivel_BroadCastMotionUpdate);
    hasRegistered = true;
  }

  public static void Request_SetMotionState(ZDO zdo, MotionState motionState)
  {
    if (ZRoutedRpc.instance == null) return;
    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    pkg.Write((int)motionState);
    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), SetMotionRPC_Name, pkg);
  }

  public static void Request_NextMotion(ZDO zdo, MotionState currentMotionState)
  {
    if (ZRoutedRpc.instance == null) return;
    var pkg = new ZPackage();
    var zdoid = zdo.m_uid;

    pkg.Write(zdoid);
    pkg.Write((int)currentMotionState);

    if (ZNet.IsSinglePlayer || ZNet.instance.IsServer())
    {
      RPC_Swivel_NextMotionState(zdo.GetOwner(), pkg);
      return;
    }
    ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), NextMotionRPC_Name, pkg);
  }

  public static void RPC_Swivel_NextMotionState(long sender, ZPackage pkg)
  {
    if (ZRoutedRpc.instance == null) return;
    pkg.SetPos(0);

    var zdoId = pkg.ReadZDOID();
    var clientMotionState = (MotionState)pkg.ReadInt();

    var zdo = ZDOMan.instance.GetZDO(zdoId);

    if (zdo == null)
    {
      LoggerProvider.LogError("No ZDO found for RPC_NextMotionState. Bailing...");
      return;
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

  private static Dictionary<ZDOID, Coroutine?> _pendingMotionCoroutines = new();
  private static Dictionary<ZDOID, SwivelMotionUpdateLerp> _pendingMotionUpdateLerps = new();

  public static void StopSwivelUpdate(ZDOID zdoid)
  {
    if (PowerNetworkController.Instance == null || !_pendingMotionCoroutines.TryGetValue(zdoid, out var coroutine)) return;
    if (coroutine != null)
    {
      PowerNetworkController.Instance.StopCoroutine(coroutine);
    }
    _pendingMotionCoroutines.Remove(zdoid);
  }

  public static void StartSwivelUpdate(ZDOID zdoid, ZDO zdo, MotionState currentState, float duration, SwivelMotionUpdateLerp motionUpdateLerp)
  {
    if (PowerNetworkController.Instance == null) return;
    if (_pendingMotionCoroutines.TryGetValue(zdoid, out var coroutine) && coroutine != null)
    {
      PowerNetworkController.Instance.StopCoroutine(coroutine);
    }
    _pendingMotionCoroutines[zdoid] = PowerNetworkController.Instance.StartCoroutine(WaitAndFinishMotion(PowerNetworkController.Instance, zdo, currentState, duration, motionUpdateLerp));
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

    var remainingLerpMultiplier = 1f;
    if (_pendingMotionUpdateLerps.TryGetValue(zdo.m_uid, out var lastUpdateLerp))
    {
      // At zero we have no decrease in time. At near 1 we immediately resolve as we near multiply by zero. 
      remainingLerpMultiplier = Mathf.Clamp01(1 - lastUpdateLerp.lerp);
      _pendingMotionUpdateLerps.Remove(zdo.m_uid);
    }

    var update = new SwivelMotionUpdate
    {
      StartTime = ZNet.instance.GetTimeSeconds(), // Use the game/server clock, not Time.time!
      Duration = duration * remainingLerpMultiplier,
      MotionState = currentState
    };

    var nextMotionUpdateLerp = new SwivelMotionUpdateLerp
    {
      update = update,
      lerp = 0f
    };
    _pendingMotionUpdateLerps[zdo.m_uid] = nextMotionUpdateLerp;

    var pkg = new ZPackage();
    pkg.Write(zdo.m_uid);
    update.WriteTo(pkg);

    RPCUtils.RunIfNearby(zdo, 100f, (sender) =>
    {
      ZRoutedRpc.instance.InvokeRoutedRPC(sender, BroadCastMotionRPC_Name, pkg);
    });

    StopSwivelUpdate(zdo.m_uid);
    StartSwivelUpdate(zdo.m_uid, zdo, currentState, duration, nextMotionUpdateLerp);
  }

  public static void RPC_Swivel_BroadCastMotionUpdate(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoid = pkg.ReadZDOID();
    var motionUpdate = SwivelMotionUpdate.ReadFrom(pkg);
    var zdo = ZDOMan.instance.GetZDO(zdoid);

    if (zdo == null) return;

    if (!SwivelComponentBridge.ZdoToComponent.TryGetValue(zdo, out var swivelComponentBridge))
    {
      return;
    }

    swivelComponentBridge.SetAuthoritativeMotion(motionUpdate, true);
  }

  public static IEnumerator WaitAndFinishMotion(MonoBehaviour behavior, ZDO zdo, MotionState currentMotion, float duration, SwivelMotionUpdateLerp swivelMotionUpdateLerp)
  {
    var currentDuration = 0f;

    // we lerp track motion updates per frame so we accurately match things.
    while (behavior.isActiveAndEnabled && currentDuration < duration)
    {
      currentDuration += Time.deltaTime;
      swivelMotionUpdateLerp.lerp = Mathf.Clamp01(currentDuration / duration);
      yield return null;
    }
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

  public static void RPC_Swivel_SetMotionState(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoId = pkg.ReadZDOID();
    var clientMotionState = (MotionState)pkg.ReadInt();

    var zdo = ZDOMan.instance.GetZDO(zdoId);
    if (zdo == null) return;

    // Read client-reported MotionState
    var serverMotionState = (MotionState)zdo.GetInt(SwivelCustomConfig.Key_MotionState);

    if (clientMotionState == serverMotionState)
    {
      return;
    }

    // do not start motion sync. This call should be for force syncing a motion state.
    Internal_SetAndNotifyMotionState(zdo, clientMotionState, false);
  }
}