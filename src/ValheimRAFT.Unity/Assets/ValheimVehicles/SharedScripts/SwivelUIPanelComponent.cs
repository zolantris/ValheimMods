// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.SharedScripts.Helpers;
using Zolantris.Shared;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public class SwivelUIPanelComponent : SingletonBehaviour<SwivelUIPanelComponent>
  {
    public static int MinTargetOffset = -50;
    public static int MaxTargetOffset = 50;

    public static bool ShouldDestroyOnNewTarget = true;

    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 700f;
    public GameObject hingeAxisRow;
    public Transform layoutParent;
    public GameObject maxXRow;
    public GameObject maxYRow;
    public GameObject maxZRow;
    public bool IsEditing;

    internal SwivelCustomConfig _currentPanelConfig = new();

    internal SwivelComponent? _currentSwivel;
    private float _debounceThreshold = 0.2f; // 200ms debounce

    private bool _hasCreatedUI;

    private float _lastPanelUpdateTime = -999f;
    private TextMeshProUGUI _saveStatus;

    internal TMP_Dropdown modeDropdown;
    internal TMP_Dropdown motionStateDropdown;

    internal GameObject movementLerpRow;
    internal GameObject movementSectionLabel;
    internal GameObject panelRoot;
    internal GameObject rotationSectionLabel;
    internal GameObject targetDistanceXRow;
    internal GameObject targetDistanceYRow;
    internal GameObject targetDistanceZRow;
    public SwivelUISharedStyles viewStyles = new();

    public SwivelComponent? CurrentSwivel
    {
      get => _currentSwivel;
      set => _currentSwivel = value;
    }

    // for integrations
    [UsedImplicitly]
    public virtual void BindTo(SwivelComponent target, bool isToggle = false)
    {
      if (ShouldDestroyOnNewTarget)
      {
        if (CurrentSwivel != null && CurrentSwivel != target)
        {
          Destroy(panelRoot);
          _hasCreatedUI = false;
        }
      }

      CurrentSwivel = target;
      if (CurrentSwivel == null)
      {
        return;
      }

      _currentPanelConfig = new SwivelCustomConfig();
      _currentPanelConfig.ApplyFrom(target);

      if (!_hasCreatedUI || maxXRow == null || maxYRow == null || maxZRow == null || targetDistanceXRow == null || targetDistanceYRow == null || targetDistanceZRow == null)
      {
        isToggle = false;
        CreateUI();
      }

      modeDropdown.value = (int)_currentPanelConfig.Mode;
      motionStateDropdown.value = (int)_currentPanelConfig.MotionState;

      var max = _currentPanelConfig.MaxEuler;

      maxXRow.GetComponentInChildren<Slider>().value = max.x;
      maxYRow.GetComponentInChildren<Slider>().value = max.y;
      maxZRow.GetComponentInChildren<Slider>().value = max.z;

      var offset = _currentPanelConfig.MovementOffset;
      targetDistanceXRow.GetComponentInChildren<Slider>().value = offset.x;
      targetDistanceYRow.GetComponentInChildren<Slider>().value = offset.y;
      targetDistanceZRow.GetComponentInChildren<Slider>().value = offset.z;

      movementLerpRow.GetComponentInChildren<Slider>().value = _currentPanelConfig.InterpolationSpeed;

      RefreshUI();
      if (!isToggle)
      {
        Show();
      }
      else
      {
        Toggle();
      }
    }

    public void Toggle()
    {
      if (panelRoot == null) return;
      if (!panelRoot.activeSelf)
      {
        Show();
        return;
      }
      Hide();
    }

    public void Show()
    {
      if (panelRoot == null) CreateUI();
      panelRoot.gameObject.SetActive(true);
    }

    public void Hide()
    {
      if (panelRoot != null) panelRoot.gameObject.SetActive(false);
    }

    public virtual GameObject CreateUIRoot()
    {
      var rootUI = SwivelUIHelpers.CreateUICanvas("SwivelUICanvas", transform);
      return rootUI.gameObject;
    }

    /// <summary>
    /// Override this to persist configuration or perform updates.
    /// </summary>
    protected virtual void OnPanelSave()
    {
      // Example: SaveConfig(swivel);
      Debug.Log("SwivelUIPanelComponent.OnPanelUpdate triggered.");
    }

    /// <summary>
    /// Call this method whenever UI changes that might require persistence.
    /// </summary>
    public virtual void UnsetSavedState()
    {
      IsEditing = true;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Save;
    }
    public virtual void SetSavedState()
    {
      IsEditing = false;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Saved;
    }
    // public virtual void RequestSaveDebounced()
    // {
    //   if (_saveStatus != null) _saveStatus.text = SwivelUIPanelStrings.Saved;
    //   if (Time.time - _lastPanelUpdateTime >= _debounceThreshold)
    //   {
    //     _lastPanelUpdateTime = Time.time;
    //     OnPanelUpdate();
    //     if (_saveStatus != null) _saveStatus.text = SwivelUIPanelStrings.Save;
    //   }
    // }

    private void CreateUI()
    {
      panelRoot = CreateUIRoot();

      // var rootViewPort = SwivelUIHelpers.CreateViewport(panelRoot.transform, viewStyles, false, true);
      // var viewPortRT = rootViewPort.GetComponent<RectTransform>();
      // viewPortRT.pivot = new Vector2(0f, 1f);
      // viewPortRT.anchorMin= new Vector2(0f, 0f);
      // viewPortRT.anchorMax = new Vector2(1f, 1f);
      //
      // var rootViewPortLayout = viewPortRT.gameObject.AddComponent<LayoutElement>();
      // rootViewPortLayout.flexibleHeight = 800f;
      // rootViewPortLayout.flexibleWidth = 800f;
      // rootViewPortLayout.minWidth = 500f;
      // rootViewPortLayout.minHeight = 300f;
      // rootViewPortLayout.preferredHeight = 500f;

      // var maxWidth = Mathf.Clamp(Screen.width * 0.3f, viewStyles.minWidth, viewStyles.maxWidth);
      // var maxWidth = Mathf.Min(800f, Screen.width);
      // var maxHeight = Mathf.Min(viewStyles.maxHeight, Screen.height);
      //
      // viewPortRT.sizeDelta = new Vector2(maxWidth, maxHeight);
      // viewPortRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
      // viewPortRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);

      // var rootContent = SwivelUIHelpers.CreateContent("Content", panelRoot.transform, viewStyles, new Vector2(0f, 1f), new Vector2(0f, 1f));
      var scrollGo = SwivelUIHelpers.CreateScrollView(panelRoot.transform, viewStyles, out var scrollRect);
      var scrollViewport = SwivelUIHelpers.CreateViewport(scrollGo.transform, viewStyles, true);
      var scrollViewContent = SwivelUIHelpers.CreateContent("Content", scrollViewport.transform, viewStyles, null, null);
      layoutParent = scrollViewContent.transform;
      scrollRect.content = scrollViewContent.GetComponent<RectTransform>();

      var layout = scrollRect.gameObject.AddComponent<LayoutElement>();
      layout.flexibleHeight = 800f;
      layout.flexibleWidth = 800f;
      layout.minWidth = 500f;
      layout.minHeight = 300f;
      layout.preferredHeight = 500f;

      SwivelUIHelpers.AddRowWithButton(layoutParent, viewStyles, SwivelUIPanelStrings.SwivelConfig, "X", 48f, 48f, out _, Hide);
      modeDropdown = SwivelUIHelpers.AddDropdownRow(
        layoutParent,
        viewStyles,
        SwivelUIPanelStrings.SwivelMode,
        EnumDisplay.GetSwivelModeNames(),
        EnumDisplay.GetSwivelModeNames()[(int)_currentPanelConfig.Mode],
        i =>
        {
          _currentPanelConfig.Mode = (SwivelMode)i;
          RefreshUI();
          UnsetSavedState();
        });

      motionStateDropdown = SwivelUIHelpers.AddDropdownRow(
        layoutParent,
        viewStyles,
        SwivelUIPanelStrings.MotionState,
        EnumDisplay.GetMotionStateNames(),
        EnumDisplay.GetMotionStateNames()[(int)_currentPanelConfig.MotionState],
        i =>
        {
          _currentPanelConfig.MotionState = (MotionState)i;
          UnsetSavedState();
        });

      movementLerpRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.InterpolationSpeed, 1f, 100f, _currentPanelConfig.InterpolationSpeed, v =>
      {
        _currentPanelConfig.InterpolationSpeed = Mathf.Clamp(v, 1, 100f);
        UnsetSavedState();
      });

      rotationSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.RotationSettings);

      hingeAxisRow = SwivelUIHelpers.AddMultiToggleRow(layoutParent, viewStyles, SwivelUIPanelStrings.HingeAxes,
        new[] { "X", "Y", "Z" },
        new[]
        {
          _currentPanelConfig.HingeAxes.HasFlag(HingeAxis.X),
          _currentPanelConfig.HingeAxes.HasFlag(HingeAxis.Y),
          _currentPanelConfig.HingeAxes.HasFlag(HingeAxis.Z)
        },
        selected =>
        {
          var axis = HingeAxis.None;
          if (selected[0]) axis |= HingeAxis.X;
          if (selected[1]) axis |= HingeAxis.Y;
          if (selected[2]) axis |= HingeAxis.Z;
          _currentPanelConfig.HingeAxes = axis;
          UnsetSavedState();
        });

      maxXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxXAngle, 0f, 360f, _currentPanelConfig.MaxEuler.x, v =>
      {
        var e = _currentPanelConfig.MaxEuler;
        e.x = v;
        _currentPanelConfig.MaxEuler = e;
        UnsetSavedState();
      });

      maxYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxYAngle, 0f, 360f, _currentPanelConfig.MaxEuler.y, v =>
      {
        var e = _currentPanelConfig.MaxEuler;
        e.y = v;
        _currentPanelConfig.MaxEuler = e;
        UnsetSavedState();
      });

      maxZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxZAngle, 0f, 360f, _currentPanelConfig.MaxEuler.z, v =>
      {
        var e = _currentPanelConfig.MaxEuler;
        e.z = v;
        _currentPanelConfig.MaxEuler = e;
        UnsetSavedState();
      });

      movementSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.MovementSettings);

      targetDistanceXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetXOffset, MinTargetOffset, MaxTargetOffset, _currentPanelConfig.MovementOffset.x, v =>
      {
        var o = _currentPanelConfig.MovementOffset;
        o.x = v;
        _currentPanelConfig.MovementOffset = o;
        UnsetSavedState();
      });

      targetDistanceYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetYOffset, MinTargetOffset, MaxTargetOffset, _currentPanelConfig.MovementOffset.y, v =>
      {
        var o = _currentPanelConfig.MovementOffset;
        o.y = v;
        _currentPanelConfig.MovementOffset = o;
        UnsetSavedState();
      });

      targetDistanceZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetZOffset, MinTargetOffset, MaxTargetOffset, _currentPanelConfig.MovementOffset.z, v =>
      {
        var o = _currentPanelConfig.MovementOffset;
        o.z = v;
        _currentPanelConfig.MovementOffset = o;
        UnsetSavedState();
      });

      SwivelUIHelpers.AddRowWithButton(layoutParent, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnPanelSave();
        SetSavedState();
      });

      _hasCreatedUI = true;
    }

    /// <summary>
    /// Only show swivel ui values if there is one found.
    /// </summary>
    internal void RefreshUI()
    {
      var isRotating = CurrentSwivel && _currentPanelConfig.Mode == SwivelMode.Rotate;
      var isMoving = CurrentSwivel && _currentPanelConfig.Mode == SwivelMode.Move;
      var isWindTarget = _currentPanelConfig.Mode == SwivelMode.TargetWind;

      motionStateDropdown.gameObject.SetActive(!isWindTarget);

      rotationSectionLabel.SetActive(isRotating);
      hingeAxisRow.SetActive(isRotating);

      maxYRow.SetActive(isRotating || isWindTarget);

      maxXRow.SetActive(isRotating);
      maxZRow.SetActive(isRotating);

      targetDistanceXRow.SetActive(isMoving);
      targetDistanceYRow.SetActive(isMoving);
      targetDistanceZRow.SetActive(isMoving);
      movementSectionLabel.SetActive(isMoving);
    }

    private string[] EnumNames<T>() where T : Enum
    {
      return Enum.GetNames(typeof(T));
    }
  }

  [Serializable]
  [SuppressMessage("ReSharper", "ConvertToConstant.Global")]
  public static class SwivelUIPanelStrings
  {
    public static string Saved => ModTranslations.Swivel_Saved ?? "Saved";
    public static string Save => ModTranslations.Swivel_Save ?? "Save";
    public static string SwivelConfig => ModTranslations.Swivel_Config ?? "Swivel Config";
    public static string SwivelMode => ModTranslations.Swivel_Mode ?? "Swivel Mode";
    public static string MotionState => ModTranslations.Swivel_MotionState ?? "Motion State";
    public static string InterpolationSpeed => ModTranslations.Swivel_InterpolationSpeed ?? "Interpolation Speed";

    public static string RotationSettings => ModTranslations.Swivel_RotationSettings ?? "Rotation Settings";
    public static string HingeAxes => ModTranslations.Swivel_HingeAxes ?? "Hinge Axes";
    public static string MaxXAngle => ModTranslations.Swivel_MaxXAngle ?? "Max X Angle";
    public static string MaxYAngle => ModTranslations.Swivel_MaxYAngle ?? "Max Y Angle";
    public static string MaxZAngle => ModTranslations.Swivel_MaxZAngle ?? "Max Z Angle";

    public static string MovementSettings => ModTranslations.Swivel_MovementSettings ?? "Movement Settings";
    public static string TargetXOffset => ModTranslations.Swivel_TargetXOffset ?? "Target X Offset";
    public static string TargetYOffset => ModTranslations.Swivel_TargetYOffset ?? "Target Y Offset";
    public static string TargetZOffset => ModTranslations.Swivel_TargetZOffset ?? "Target Z Offset";

    public static string SwivelMode_None => ModTranslations.SwivelMode_None ?? "None";
    public static string SwivelMode_Rotate => ModTranslations.SwivelMode_Rotate ?? "Rotate";
    public static string SwivelMode_Move => ModTranslations.SwivelMode_Move ?? "Move";
    public static string SwivelMode_TargetEnemy => ModTranslations.SwivelMode_TargetEnemy ?? "Target Enemy";
    public static string SwivelMode_TargetWind => ModTranslations.SwivelMode_TargetWind ?? "Target Wind";
  }

}