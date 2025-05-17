using System;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.UI;

public class SwivelUIPanelComponentIntegration : SwivelUIPanelComponent
{
  public Action<Text>? OnPanelToggleAction;

  public Unity2dViewStyles buttonStyles = new()
  {
    anchorMin = Vector2.zero,
    anchorMax = Vector2.one,
    position = Vector2.zero,
    width = 150
  };
  public Unity2dViewStyles panelStyles = new()
  {
    width = 500
  };

  public override void OnAwake()
  {
    if (ZNetView.m_forceDisableInit) return;
  }

  public void OnEnable()
  {
    // OnPanelToggleAction += OnPanelToggle;
  }

  public void OnDisable()
  {
    // OnPanelToggleAction -= OnPanelToggle;
  }

  public static void Init()
  {
    if (!Instance && Game.instance)
    {
      Game.instance.gameObject.AddComponent<SwivelUIPanelComponentIntegration>();
    }
  }

  public void TryGetCurrentSwivelIntegration()
  {
    if (!_currentSwivel && Player.m_localPlayer)
    {
      SwivelHelpers.FindAllSwivelsWithinRange(transform.position, out _, out _currentSwivel);
    }
  }

  public override void OnBindTo()
  {
    TryGetCurrentSwivelIntegration();
  }

  // todo hide/show the panel from here if we want to keep hide show buttons otherwise do not use this method.
  // public void OnPanelToggle(Text text)
  // {
  //   if (text == null) return;
  //   var nextState = !panelRoot.activeSelf;
  //   text.text = "X";
  // }

  protected override void OnPanelUpdate()
  {
    TryGetCurrentSwivelIntegration();
    var swivelComponentIntegration = _currentSwivel as SwivelComponentIntegration;
    if (!swivelComponentIntegration || !swivelComponentIntegration.IsNetViewValid()) return;

    var config = new SwivelCustomConfig();
    config.ApplyFrom(swivelComponentIntegration);
    swivelComponentIntegration.prefabConfigSync.RequestCommitConfigChange(config);
  }

  private const string PanelName = "ValheimVehicles_SwivelPanel";

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }
}