using System;
using System.Collections;
using System.Collections.Generic;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;
namespace ValheimVehicles.Integrations;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class SwivelConfigRPCSync : PrefabConfigRPCSync<SwivelCustomConfig, ISwivelConfig>
{
  public override void RegisterRPCListeners()
  {
    base.RegisterRPCListeners();
    // rpcHandler?.Register<ZPackage>(nameof(RPC_NextMotionState), RPC_NextMotionState);
    // rpcHandler?.Register<ZPackage>(nameof(RPC_SetMotionState), RPC_SetMotionState);
  }

  public static CustomRPC RPCInstance_SwivelNextMotionState;
  public static CustomRPC RPCInstance_SwivelSetMotion;

  /// <summary>
  /// Global RPCs that must be synced authoritatively on the server.
  /// </summary>
  public static void Register()
  {
    RPCInstance_SwivelNextMotionState = NetworkManager.Instance.AddRPC(nameof(RPC_NextMotionState), RPC_NextMotionState, RPC_NextMotionState);
    RPCInstance_SwivelSetMotion = NetworkManager.Instance.AddRPC(nameof(RPC_SetMotionState), RPC_SetMotionState, RPC_SetMotionState);
  }

  public void Request_SetMotionState(MotionState motionState)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner() || !ZNet.instance.IsServer()) return;
    var pkg = new ZPackage();
    pkg.Write((int)motionState);
    // Send to the server/owner for validation and potential action
    netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SetMotionState), pkg);
  }

  /// <summary>
  /// TODO might have to use Networkmanager from jotunns for this.
  /// </summary>
  public void Request_NextMotion()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var next = GetNextState(Config.MotionState);
    var pkg = new ZPackage();
    pkg.Write((int)next);

    RPCInstance_SwivelNextMotionState.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);
    // Send to the server/owner for validation and potential action
    // netView.InvokeRPC(ZRoutedRpc.instance.GetServerPeerID(), nameof(RPC_NextMotionState), pkg);
  }

  public static IEnumerator RPC_SetMotionState(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView)) yield break;
    if (!netView.IsOwner() && netView.HasOwner()) yield break;
    if (!netView.HasOwner())
    {
      netView.ClaimOwnership();
    }

    // Read client-reported MotionState
    var clientMotionState = (MotionState)pkg.ReadInt();

    if (clientMotionState == Config.MotionState)
    {
      LoggerProvider.LogDebug($"[Swivel] MotionState unchanged: <{clientMotionState}>. Ignoring.");
      yield break;
    }

#if DEBUG

    ValheimExtensions.LogValheimServerStats();
#endif


    // Valid and expected: apply to real config
    Config.MotionState = clientMotionState;
    Config.Save();
    // CommitConfigChange(Config);

    RPCInstance_SwivelNextMotionState.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), pkg);

    yield return null;
  }

  public static IEnumerator RPC_NextMotionState(long sender, ZPackage pkg)
  {
    pkg.SetPos(0);
    var zdoId = pkg.ReadZDOID();
    var zdo = ZDOMan.instance.GetZDO(zdoId);
    var clientMotionState = (MotionState)pkg.ReadInt();

    // server motion state is used to determine the next motion state. client only used for comparison if desynced.
    var currentMotionState = (MotionState)zdo.GetInt(SwivelCustomConfig.Key_MotionState);
    var nextMotionState = GetNextState(currentMotionState);

    // Clone current config (to avoid mutating it before validation)
#if DEBUG
    LoggerProvider.LogDebug($"Server previous has current MotionState: {currentMotionState}, (pending) nextMotionState: {nextMotionState}");
#endif

    // Validate if client was expecting the correct next state
    if (clientMotionState != currentMotionState)
    {
      LoggerProvider.LogDebug($"[Swivel] MotionState desync. Got {clientMotionState} but expected {expected.MotionState} Re-sending config.");
      Request_SyncKeys
      netView.InvokeRPC(sender, nameof(RPC_Load));
      return;
    }

    // Valid and expected: apply to real config
    Config.MotionState = expected.MotionState;
    CommitConfigChange(Config);
  }

  public override void OnLoad()
  {
    if (controller == null || SwivelUIPanelComponentIntegration.Instance == null) return;

    var swivel = (SwivelComponent)controller;

    // Only update if currently bound to this swivel
    var panel = SwivelUIPanelComponent.Instance as SwivelUIPanelComponentIntegration;
    if (panel != null && panel.CurrentSwivel == swivel)
    {
      panel.SyncUIFromPartialConfig(Config);
    }
  }

  public static MotionState GetNextState(MotionState current)
  {
    return current switch
    {
      MotionState.AtStart => MotionState.ToTarget,
      MotionState.ToStart => MotionState.ToTarget,
      MotionState.AtTarget => MotionState.ToStart,
      MotionState.ToTarget => MotionState.ToStart,
      _ => MotionState.AtStart
    };
  }
}