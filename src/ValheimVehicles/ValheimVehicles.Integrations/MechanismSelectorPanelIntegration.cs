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

  public static void InitComponent()
  {
    Game.instance.gameObject.AddComponent<MechanismSelectorPanel>();
  }

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }
}