using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ValheimVehicles.Constants;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.UI;

public class SwivelUIPanelComponentIntegration : SwivelUIPanelComponent
{
  public Action<Text>? OnPanelToggleAction;
  public Unity2dViewStyles buttonStyles = new()
  {
    width = 150
  };
  public Unity2dViewStyles panelStyles = new()
  {
    width = 500
  };

  public override void OnAwake()
  {
    OnPanelToggleAction += OnPanelToggle;
  }

  public void OnEnable()
  {
    OnPanelToggleAction += OnPanelToggle;
  }

  public void OnDestroy()
  {
    OnPanelToggleAction -= OnPanelToggle;
  }

  public void OnPanelToggle(Text text)
  {
    if (text == null) return;
    var nextState = !panelRoot.activeSelf;
    text.text = "X";
  }

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, "X");
  }
}