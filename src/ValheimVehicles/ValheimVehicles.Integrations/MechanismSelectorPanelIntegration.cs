using System.Linq;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;
namespace ValheimVehicles.Integrations;

public class MechanismSelectorPanelIntegration : MechanismSelectorPanel
{
  private const string PanelName = "ValheimVehicles_MechanismSelectorPanel";
  public Unity2dViewStyles panelStyles = new()
  {
    width = 500,
    height = 200
  };
  public Unity2dViewStyles buttonStyles = new()
  {
    anchorMin = Vector2.zero,
    anchorMax = Vector2.one,
    position = Vector2.zero,
    width = 150
  };

  public static void Init()
  {
    Game.instance.gameObject.AddComponent<MechanismSelectorPanelIntegration>();
  }

  protected override void OnPanelSave()
  {
    var mechanismSwitch = mechanismAction as MechanismSwitch;
    if (!mechanismSwitch || !mechanismSwitch.IsNetViewValid()) return;

    // We do not let local overrides of this readonly value.
    var saveConfig = new MechanismSwitchCustomConfig();
    saveConfig.ApplyFrom(_currentPanelConfig);
    mechanismSwitch.SelectedAction = _currentPanelConfig.SelectedAction;
    mechanismSwitch.TargetSwivelId = _currentPanelConfig.TargetSwivelId;
    mechanismSwitch.TargetSwivel = _currentPanelConfig.TargetSwivel ?? MechanismSwitchCustomConfig.ResolveSwivel(_currentPanelConfig.TargetSwivelId);

    mechanismSwitch.prefabConfigSync.Request_CommitConfigChange(saveConfig);
  }

  public void SyncUIFromPartialConfig(MechanismSwitch updated)
  {
    if (mechanismAction == null) return;
    if (IsEditing) return;

    if (_currentPanelConfig.SelectedAction != updated.prefabConfigSync.Config.SelectedAction)
    {
      actionDropdown.value = (int)updated.prefabConfigSync.Config.SelectedAction;
      _currentPanelConfig.SelectedAction = updated.prefabConfigSync.Config.SelectedAction;
    }

    if (_currentPanelConfig.TargetSwivelId != updated.prefabConfigSync.Config.TargetSwivelId)
    {
      var index = swivelSelectorDropdown.options.FindIndex(x => x.text.Contains(PersistentIdToString(updated.TargetSwivelId)));
      swivelSelectorDropdown.value = index;
      _currentPanelConfig.TargetSwivelId = updated.prefabConfigSync.Config.TargetSwivelId;
    }

    IsEditing = false;
  }

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }
}