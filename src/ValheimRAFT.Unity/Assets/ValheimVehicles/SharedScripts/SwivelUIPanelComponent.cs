// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Config;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public class SwivelUIPanelComponent : SingletonBehaviour<SwivelUIPanelComponent>
  {
    public static int MinTargetOffset = -50;
    public static int MaxTargetOffset = 50;

    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 700f;
    private float _debounceThreshold = 0.2f; // 200ms debounce

    private bool _hasCreatedUI;

    private float _lastPanelUpdateTime = -999f;
    private TextMeshProUGUI _saveStatus;
    private GameObject hingeAxisRow;
    private Transform layoutParent;
    private GameObject maxXRow;
    private GameObject maxYRow;
    private GameObject maxZRow;

    private TMP_Dropdown modeDropdown;
    private TMP_Dropdown motionStateDropdown;

    private GameObject movementLerpRow;
    private GameObject movementSectionLabel;
    internal GameObject panelRoot;
    private GameObject rotationSectionLabel;
    private GameObject targetDistanceXRow;
    private GameObject targetDistanceYRow;
    private GameObject targetDistanceZRow;
    public SwivelUISharedStyles viewStyles = new();

    internal SwivelComponent? _currentSwivel;

    internal SwivelCustomConfig _currentSwivelTempConfig = new();

    public SwivelComponent? CurrentSwivel
    {
      get => _currentSwivel;
      set => _currentSwivel = value;
    }

    public static bool ShouldDestroyOnNewTarget = true;

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

      _currentSwivelTempConfig = new SwivelCustomConfig();
      _currentSwivelTempConfig.ApplyFrom(target);

      if (!_hasCreatedUI)
      {
        isToggle = false;
        CreateUI();
      }

      modeDropdown.value = (int)_currentSwivelTempConfig.Mode;
      motionStateDropdown.value = (int)_currentSwivelTempConfig.MotionState;

      var max = _currentSwivelTempConfig.MaxEuler;
      maxXRow.GetComponentInChildren<Slider>().value = max.x;
      maxYRow.GetComponentInChildren<Slider>().value = max.y;
      maxZRow.GetComponentInChildren<Slider>().value = max.z;

      var offset = _currentSwivelTempConfig.MovementOffset;
      targetDistanceXRow.GetComponentInChildren<Slider>().value = offset.x;
      targetDistanceYRow.GetComponentInChildren<Slider>().value = offset.y;
      targetDistanceZRow.GetComponentInChildren<Slider>().value = offset.z;

      movementLerpRow.GetComponentInChildren<Slider>().value = _currentSwivelTempConfig.InterpolationSpeed;

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
    protected virtual void OnPanelUpdate()
    {
      // Example: SaveConfig(swivel);
      Debug.Log("SwivelUIPanelComponent.OnPanelUpdate triggered.");
    }

    /// <summary>
    /// Call this method whenever UI changes that might require persistence.
    /// </summary>
    public virtual void UnsetSavedState()
    {
      if (_saveStatus != null) _saveStatus.text = SwivelUIPanelStrings.Save;
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
      modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, SwivelUIPanelStrings.SwivelMode, EnumNames<SwivelMode>(), _currentSwivelTempConfig.Mode.ToString(), i =>
      {
        _currentSwivelTempConfig.Mode = (SwivelMode)i;
        RefreshUI();
        UnsetSavedState();
      });

      motionStateDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, SwivelUIPanelStrings.MotionState, EnumNames<MotionState>(), _currentSwivelTempConfig.MotionState.ToString(), i =>
      {
        _currentSwivelTempConfig.MotionState = (MotionState)i;
        UnsetSavedState();
      });

      movementLerpRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.InterpolationSpeed, 1f, 100f, _currentSwivelTempConfig.InterpolationSpeed, v =>
      {
        _currentSwivelTempConfig.InterpolationSpeed = v;
        UnsetSavedState();
      });

      rotationSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.RotationSettings);

      hingeAxisRow = SwivelUIHelpers.AddMultiToggleRow(layoutParent, viewStyles, SwivelUIPanelStrings.HingeAxes,
        new[] { "X", "Y", "Z" },
        new[]
        {
          _currentSwivelTempConfig.HingeAxes.HasFlag(HingeAxis.X),
          _currentSwivelTempConfig.HingeAxes.HasFlag(HingeAxis.Y),
          _currentSwivelTempConfig.HingeAxes.HasFlag(HingeAxis.Z)
        },
        selected =>
        {
          var axis = HingeAxis.None;
          if (selected[0]) axis |= HingeAxis.X;
          if (selected[1]) axis |= HingeAxis.Y;
          if (selected[2]) axis |= HingeAxis.Z;
          _currentSwivelTempConfig.HingeAxes = axis;
          UnsetSavedState();
        });

      maxXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxXAngle, 0f, 360f, _currentSwivelTempConfig.MaxEuler.x, v =>
      {
        var e = _currentSwivelTempConfig.MaxEuler;
        e.x = v;
        _currentSwivelTempConfig.MaxEuler = e;
        UnsetSavedState();
      });

      maxYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxYAngle, 0f, 360f, _currentSwivelTempConfig.MaxEuler.y, v =>
      {
        var e = _currentSwivelTempConfig.MaxEuler;
        e.y = v;
        _currentSwivelTempConfig.MaxEuler = e;
        UnsetSavedState();
      });

      maxZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxZAngle, 0f, 360f, _currentSwivelTempConfig.MaxEuler.z, v =>
      {
        var e = _currentSwivelTempConfig.MaxEuler;
        e.z = v;
        _currentSwivelTempConfig.MaxEuler = e;
        UnsetSavedState();
      });

      movementSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.MovementSettings);

      targetDistanceXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetXOffset, MinTargetOffset, MaxTargetOffset, _currentSwivelTempConfig.MovementOffset.x, v =>
      {
        var o = _currentSwivelTempConfig.MovementOffset;
        o.x = v;
        _currentSwivelTempConfig.MovementOffset = o;
        UnsetSavedState();
      });

      targetDistanceYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetYOffset, MinTargetOffset, MaxTargetOffset, _currentSwivelTempConfig.MovementOffset.y, v =>
      {
        var o = _currentSwivelTempConfig.MovementOffset;
        o.y = v;
        _currentSwivelTempConfig.MovementOffset = o;
        UnsetSavedState();
      });

      targetDistanceZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetZOffset, MinTargetOffset, MaxTargetOffset, _currentSwivelTempConfig.MovementOffset.z, v =>
      {
        var o = _currentSwivelTempConfig.MovementOffset;
        o.z = v;
        _currentSwivelTempConfig.MovementOffset = o;
        UnsetSavedState();
      });

      SwivelUIHelpers.AddRowWithButton(layoutParent, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnPanelUpdate();
        if (_saveStatus != null) _saveStatus.text = SwivelUIPanelStrings.Saved;
      });

      _hasCreatedUI = true;
    }

    /// <summary>
    /// Only show swivel ui values if there is one found.
    /// </summary>
    private void RefreshUI()
    {
      var isRotating = CurrentSwivel && _currentSwivelTempConfig.Mode == SwivelMode.Rotate;
      var isMoving = CurrentSwivel && _currentSwivelTempConfig.Mode == SwivelMode.Move;

      rotationSectionLabel.SetActive(isRotating);
      hingeAxisRow.SetActive(isRotating);
      maxXRow.SetActive(isRotating);
      maxYRow.SetActive(isRotating);
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
    public static string Saved = "Saved";
    public static string Save = "Save";
    public static string SwivelConfig = "Swivel Config";
    public static string SwivelMode = "Swivel Mode";
    public static string MotionState = "Motion State";
    public static string InterpolationSpeed = "Interpolation Speed";

    public static string RotationSettings = "Rotation Settings";
    public static string HingeAxes = "Hinge Axes";
    public static string MaxXAngle = "Max X Angle";
    public static string MaxYAngle = "Max Y Angle";
    public static string MaxZAngle = "Max Z Angle";

    public static string MovementSettings = "Movement Settings";
    public static string TargetXOffset = "Target X Offset";
    public static string TargetYOffset = "Target Y Offset";
    public static string TargetZOffset = "Target Z Offset";
  }
}