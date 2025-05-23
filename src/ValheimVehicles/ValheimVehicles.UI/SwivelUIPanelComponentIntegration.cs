using System;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Config;
using ValheimVehicles.Constants;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.UI;

public class SwivelUIPanelComponentIntegration : SwivelUIPanelComponent
{
  public Action<Text>? OnPanelToggleAction;
  private SwivelComponentIntegration m_swivelComponentIntegration;

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
    OnPanelToggleAction += OnPanelToggle;
  }

  public void OnDisable()
  {
    OnPanelToggleAction -= OnPanelToggle;
  }

  public static void Init()
  {
    if (!Instance && Game.instance)
    {
      Game.instance.gameObject.AddComponent<SwivelUIPanelComponentIntegration>();
    }
  }

  public override void OnBindTo()
  {
    if (CurrentSwivel == null) return;
    m_swivelComponentIntegration = CurrentSwivel.gameObject.GetComponent<SwivelComponentIntegration>();
  }

  // todo hide/show the panel from here if we want to keep hide show buttons otherwise do not use this method.
  public void OnPanelToggle(Text text)
  {
    if (text == null) return;
    var nextState = !panelRoot.activeSelf;
    text.text = "X";
  }

  protected override void OnPanelUpdate()
  {
    if (!m_swivelComponentIntegration || !m_swivelComponentIntegration.m_nview || !m_swivelComponentIntegration.m_nview.IsValid()) return;
    m_swivelComponentIntegration.m_config.ApplyFrom(m_swivelComponentIntegration);
    m_swivelComponentIntegration.m_config.Save(m_swivelComponentIntegration.m_nview.m_zdo, m_swivelComponentIntegration.m_config);
    m_swivelComponentIntegration.prefabConfigSync.SendPrefabConfig();
  }

  private const string PanelName = "ValheimVehicles_SwivelPanel";

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }
}