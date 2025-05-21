using System;
using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
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
    rpcHandler?.Register<ZPackage>(nameof(RPC_NextMotionState), RPC_NextMotionState);
    rpcHandler?.Register<ZPackage>(nameof(RPC_SetMotionState), RPC_SetMotionState);
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

  public void Request_NextMotion()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    var next = GetNextState(Config.MotionState);
    var pkg = new ZPackage();
    pkg.Write((int)next);

    // Send to the server/owner for validation and potential action
    netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_NextMotionState), pkg);
  }

  public void RPC_SetMotionState(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner() && netView.HasOwner()) return;
    if (!netView.HasOwner())
    {
      netView.ClaimOwnership();
    }

    // Read client-reported MotionState
    var clientMotionState = (MotionState)pkg.ReadInt();

    if (clientMotionState == Config.MotionState)
    {
      LoggerProvider.LogDebug($"[Swivel] MotionState unchanged: <{clientMotionState}>. Ignoring.");
      return;
    }

#if DEBUG
    if (ZNet.instance.IsDedicated())
    {
      LoggerProvider.LogDebug("WE ARE A DEDICATED SERVER and get logs. YAY");
    }
    else if (ZNet.instance.IsServer())
    {
      LoggerProvider.LogDebug("WE ARE A SERVER and get logs. YAY");
    }
    LoggerProvider.LogDebug($"Server has currently has MotionState: <{Config.MotionState}>");
#endif


    // Valid and expected: apply to real config
    Config.MotionState = clientMotionState;
    CommitConfigChange(Config);
  }

  public void RPC_NextMotionState(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

    // Read client-reported MotionState
    var clientMotionState = (MotionState)pkg.ReadInt();

    // Clone current config (to avoid mutating it before validation)
#if DEBUG
    LoggerProvider.LogDebug($"Server previous has MotionState: {Config.MotionState}");
#endif
    var expected = new SwivelCustomConfig
    {
      // Mutate the cloned config with the next motion state
      MotionState = GetNextState(Config.MotionState)
    };

    // Validate if client was expecting the correct next state
    if (clientMotionState != expected.MotionState)
    {
      LoggerProvider.LogDebug($"[Swivel] MotionState desync. Got {clientMotionState} but expected {expected.MotionState} Re-sending config.");
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

  private MotionState GetNextState(MotionState current)
  {
    return current switch
    {
      MotionState.AtStart => MotionState.ToTarget,
      MotionState.ToStart => MotionState.ToTarget,
      MotionState.AtTarget => MotionState.ToStart,
      MotionState.ToTarget => MotionState.ToStart,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  // public void RPC_NextMotion(long sender)
  // {
  //   if (!this.IsNetViewValid(out var netView)) return;
  //   if (!netView.IsOwner() && !ZNet.instance.IsServer()) return;
  //
  //   var currentState = Config.MotionState;
  //   var nextState = currentState switch
  //   {
  //     MotionState.AtStart => MotionState.ToTarget,
  //     MotionState.ToStart => MotionState.ToTarget,
  //     MotionState.AtTarget => MotionState.ToStart,
  //     MotionState.ToTarget => MotionState.ToStart,
  //     _ => MotionState.ToStart
  //   };
  //
  //   var newConfig = Config;
  //   newConfig.MotionState = nextState;
  //   CommitConfigChange(newConfig);
  // }
}