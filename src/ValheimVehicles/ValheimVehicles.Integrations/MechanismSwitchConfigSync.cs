using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Interfaces;
using ZdoWatcher;
namespace ValheimVehicles.Integrations;

public class MechanismSwitchConfigSync : PrefabConfigRPCSync<MechanismSwitchCustomConfig, IMechanismSwitchConfig>
{
  public void Request_SetSelectedAction(MechanismAction action)
  {
    // must update this before saving or invoking the RPC to serialize the sync.
    Config.SelectedAction = action;
    Save();
  }

  /// <summary>
  /// For the dropdown selector we request the swivelId
  /// </summary>
  public void Request_SetSwivelTargetId(SwivelComponent swivelComponent)
  {
    var swivelNetView = swivelComponent.GetComponent<ZNetView>();
    if (!swivelNetView || swivelNetView.GetZDO() == null) return;
    var zdoId = ZdoWatchController.Instance.GetOrCreatePersistentID(swivelNetView.GetZDO());
    // must update this before saving or invoking the RPC to serialize the sync.
    Config.SwivelTargetId = zdoId;
    Save();
  }
}