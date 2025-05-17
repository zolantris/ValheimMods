// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
using ValheimVehicles.Structs;
using ZdoWatcher;

namespace ValheimVehicles.Integrations;

public class MechanismSwitchConfigSync : PrefabConfigRPCSync<MechanismSwitchCustomConfig, IMechanismSwitchConfig>
{
  public void Request_SetSelectedAction(MechanismAction action)
  {
    if (!this.IsNetViewValid(out var netView)) return;

    if (netView.IsOwner())
    {
      Handle_SetSelectedAction(action);
      CommitConfigChange(Config);
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
      Handle_SetSwivelTargetId(persistentSwivelId);
      CommitConfigChange(Config);
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

  private void Handle_SetSelectedAction(MechanismAction action)
  {
    Config.SelectedAction = action;
  }

  private void Handle_SetSwivelTargetId(int persistentSwivelId)
  {
    Config.TargetSwivelId = persistentSwivelId;
  }

  private void RPC_SetSelectedAction(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

    var action = (MechanismAction)pkg.ReadInt();
    Handle_SetSelectedAction(action);
    CommitConfigChange(Config);
  }

  private void RPC_SetSwivelTargetId(long sender, ZPackage pkg)
  {
    if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

    var swivelId = pkg.ReadInt();
    Handle_SetSwivelTargetId(swivelId);
    CommitConfigChange(Config);
  }

  public override void RegisterRPCListeners()
  {
    base.RegisterRPCListeners();

    rpcHandler?.Register<ZPackage>(nameof(RPC_SetSelectedAction), RPC_SetSelectedAction);
    rpcHandler?.Register<ZPackage>(nameof(RPC_SetSwivelTargetId), RPC_SetSwivelTargetId);
  }
}