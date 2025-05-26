// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Integrations;

public class MechanismSwitchConfigSync : PrefabConfigSync<MechanismSwitchCustomConfig, IMechanismSwitchConfig>
{
  private void HandleSetSelectedAction(MechanismAction action)
  {
    var config = new MechanismSwitchCustomConfig();
    config.ApplyFrom(Config);
    config.SelectedAction = action;
    CommitConfigChange(Config);
  }

  public void HandleSetSelectedSwivelId(int persistentSwivelId)
  {
    var config = new MechanismSwitchCustomConfig();
    config.ApplyFrom(Config);
    config.TargetSwivelId = persistentSwivelId;

    CommitConfigChange(config);
  }

  public void Request_SetSelectedAction(MechanismAction action)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner())
    {
      HandleSetSelectedAction(action);
    }
    else
    {
      var pkg = new ZPackage();
      pkg.Write((int)action);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SetSelectedAction), pkg);
    }
  }

  public void Request_SetSwivelTargetId(int persistentSwivelId)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner())
    {
      HandleSetSelectedSwivelId(persistentSwivelId);
    }
    else
    {
      var pkg = new ZPackage();
      pkg.Write(persistentSwivelId);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SetSwivelTargetId), pkg);
    }
  }

  public void Request_ClearSwivelId()
  {
    Request_SetSwivelTargetId(0);
  }

  private void RPC_SetSelectedAction(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    if (!netView.IsOwner() || !ZNet.instance.IsServer()) return;

    pkg.SetPos(0);

    var action = (MechanismAction)pkg.ReadInt();
    HandleSetSelectedAction(action);
  }

  private void RPC_SetSwivelTargetId(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;
    pkg.SetPos(0);

    var persistentSwivelId = pkg.ReadInt();
    HandleSetSelectedSwivelId(persistentSwivelId);
  }

  public override void RegisterRPCListeners()
  {
    base.RegisterRPCListeners();

    rpcHandler?.Register<ZPackage>(nameof(RPC_SetSelectedAction), RPC_SetSelectedAction);
    rpcHandler?.Register<ZPackage>(nameof(RPC_SetSwivelTargetId), RPC_SetSwivelTargetId);
  }
}