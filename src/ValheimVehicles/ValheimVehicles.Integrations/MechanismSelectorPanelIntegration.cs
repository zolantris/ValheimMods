using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
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

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }

  /// <summary>
  /// TODO use create draggable CreateDraggableHideShowPanel() from PanelUtil instead for this smaller component
  /// </summary>
  // public override void CreateUI()
  // {
  //
  //   // CreateDraggableHideShowPanel
  //   panelRoot = CreateUIRoot();
  //   base.CreateUI();
  // }
}