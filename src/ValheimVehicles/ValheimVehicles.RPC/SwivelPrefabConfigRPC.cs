using System.Collections;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.ValheimVehicles.RPC;

public class SwivelPrefabConfigRPC
{
  public static CustomRPC? RPCInstance_Swivel_NextMotionState;
  public static CustomRPC? RPCInstance_Swivel_SetMotionState;
  public static bool hasRegistered = false;

  /// <summary>
  /// Global RPCs that must be synced authoritatively on the server.
  /// </summary>
  public static void RegisterCustom()
  {
    if (hasRegistered) return;
    RPCInstance_Swivel_NextMotionState = NetworkManager.Instance.AddRPC(RPCUtils.GetRPCPrefix(nameof(RPC_NextMotionState)), RPC_NextMotionState, RPC_NextMotionState);
    RPCInstance_Swivel_SetMotionState = NetworkManager.Instance.AddRPC(RPCUtils.GetRPCPrefix(nameof(RPC_SetMotionState)), RPC_SetMotionState, RPC_SetMotionState);
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

  public static void Request_NextMotion(ZDO zdo, MotionState motionState)
  {
    var pkg = new ZPackage();
    var zdoid = zdo.m_uid;

    pkg.Write(zdoid);
    pkg.Write((int)motionState);

    RPCInstance_Swivel_NextMotionState?.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
  }

  public static IEnumerator RPC_NextMotionState(long sender, ZPackage pkg)
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
      yield break;
    }

    Internal_SetAndNotifyMotionState(zdo, nextMotionState);
  }

  private static void Internal_SetAndNotifyMotionState(ZDO zdo, MotionState state)
  {
    // take ownership if not owner
    zdo.TryClaimOwnership();

    // set the ZDO
    zdo.Set(SwivelCustomConfig.Key_MotionState, (int)state);

    // only run subscriber update for side-effect/reload callback.
    if (PrefabConfigRPC.ZdoToPrefabConfigListeners.TryGetValue(zdo, out var subscriber))
    {
      if (subscriber == null) return;
      PrefabConfigRPC.Request_SyncConfigKeys(zdo, [SwivelCustomConfig.Key_MotionState]);
    }
    else
    {
      LoggerProvider.LogError($"Could not find config for ZDO \n {zdo}");
    }
  }

  public static IEnumerator RPC_SetMotionState(long sender, ZPackage pkg)
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

    Internal_SetAndNotifyMotionState(zdo, clientMotionState);
  }
}