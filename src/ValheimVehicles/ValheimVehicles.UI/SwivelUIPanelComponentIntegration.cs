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

  public override void BindTo(SwivelComponent target, bool isToggle = false)
  {
    if (!target)
    {
      LoggerProvider.LogDebug("No swivel to open this panel");
      Destroy(panelRoot);
      return;
    }

    if (_currentSwivel)
    {
      var swivelComponentIntegration = _currentSwivel as SwivelComponentIntegration;
      if (swivelComponentIntegration != null)
      {
        swivelComponentIntegration.prefabConfigSync.Load();
      }
    }

    base.BindTo(target, isToggle);
  }

  public void SyncUIFromPartialConfig(SwivelCustomConfig updated)
  {
    if (_currentSwivel == null) return;
    if (IsEditing) return;

    // Ignore MotionState (readonly)
    var old = _currentSwivelTempConfig;

    if (updated.Mode != old.Mode)
    {
      modeDropdown.value = (int)updated.Mode;
      _currentSwivelTempConfig.Mode = updated.Mode;
      RefreshUI();
    }

    if (!Mathf.Approximately(updated.InterpolationSpeed, old.InterpolationSpeed))
    {
      movementLerpRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(updated.InterpolationSpeed);
      _currentSwivelTempConfig.InterpolationSpeed = updated.InterpolationSpeed;
    }

    if (updated.HingeAxes != old.HingeAxes)
    {
      var toggles = hingeAxisRow.GetComponentsInChildren<Toggle>();
      if (toggles.Length == 3)
      {
        toggles[0].isOn = updated.HingeAxes.HasFlag(HingeAxis.X);
        toggles[1].isOn = updated.HingeAxes.HasFlag(HingeAxis.Y);
        toggles[2].isOn = updated.HingeAxes.HasFlag(HingeAxis.Z);
      }
      _currentSwivelTempConfig.HingeAxes = updated.HingeAxes;
    }

    if (updated.MaxEuler != old.MaxEuler)
    {
      maxXRow.GetComponentInChildren<Slider>().value = updated.MaxEuler.x;
      maxYRow.GetComponentInChildren<Slider>().value = updated.MaxEuler.y;
      maxZRow.GetComponentInChildren<Slider>().value = updated.MaxEuler.z;
      _currentSwivelTempConfig.MaxEuler = updated.MaxEuler;
    }

    if (updated.MovementOffset != old.MovementOffset)
    {
      targetDistanceXRow.GetComponentInChildren<Slider>().value = updated.MovementOffset.x;
      targetDistanceYRow.GetComponentInChildren<Slider>().value = updated.MovementOffset.y;
      targetDistanceZRow.GetComponentInChildren<Slider>().value = updated.MovementOffset.z;
      _currentSwivelTempConfig.MovementOffset = updated.MovementOffset;
    }

    // Optional: update MotionState label visually if needed
    motionStateDropdown.value = (int)updated.MotionState;
    IsEditing = false;
  }


  protected override void OnPanelSave()
  {
    TryGetCurrentSwivelIntegration();
    var swivelComponentIntegration = _currentSwivel as SwivelComponentIntegration;
    if (!swivelComponentIntegration || !swivelComponentIntegration.IsNetViewValid()) return;

    // We do not let local overrides of this readonly value.
    var saveConfig = new SwivelCustomConfig();
    saveConfig.ApplyFrom(_currentSwivelTempConfig);

    // overrides/guards for config
    saveConfig.MotionState = swivelComponentIntegration.MotionState;

    if (saveConfig.Mode == SwivelMode.None && modeDropdown.value != (int)SwivelMode.None)
    {
      LoggerProvider.LogWarning("Swivel is in None mode, but the UI does not match. Not saving. This is a bug.");
      return;
    }

    swivelComponentIntegration.prefabConfigSync.RequestCommitConfigChange(_currentSwivelTempConfig);
  }

  private const string PanelName = "ValheimVehicles_SwivelPanel";

  public override GameObject CreateUIRoot()
  {
    return PanelUtil.CreateDraggableHideShowPanel(PanelName, panelStyles, buttonStyles, ModTranslations.GuiShow, ModTranslations.GuiHide, GuiConfig.SwivelPanelLocation);
  }
}