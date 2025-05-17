using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
namespace ValheimVehicles.Integrations;

public class MechanismSwitchConfigSync : PrefabConfigRPCSync<MechanismSwitchCustomConfig, IMechanismSwitchConfig>
{
  public void Request_SetSelectedAction(MechanismAction action)
  {
    // must update this before saving or invoking the RPC to serialize the sync.
    Config.SelectedAction = action;
    Request_Save();
  }

  public void Request_SetSwivelTargetId(int action)
  {
    // must update this before saving or invoking the RPC to serialize the sync.
    Config.SwivelTargetId = action;
    Request_Save();
  }
}