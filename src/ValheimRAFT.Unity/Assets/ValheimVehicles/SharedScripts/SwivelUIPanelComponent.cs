// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public SwivelComponent CurrentSwivel
    {
      get;
      private set;
    }

    // for integrations
    [UsedImplicitly]
    public virtual void OnBindTo() {}
    public void BindTo(SwivelComponent target, bool isToggle = false)
    {
      CurrentSwivel = target;
      if (CurrentSwivel == null) return;

      if (!_hasCreatedUI)
        CreateUI();

      modeDropdown.SetValueWithoutNotify((int)CurrentSwivel.Mode);
      motionStateDropdown.SetValueWithoutNotify((int)CurrentSwivel.CurrentMotionState);

      var max = CurrentSwivel.MaxEuler;
      maxXRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.x);
      maxYRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.y);
      maxZRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(max.z);

      var offset = CurrentSwivel.movementOffset;
      targetDistanceXRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.x);
      targetDistanceYRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.y);
      targetDistanceZRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(offset.z);

      movementLerpRow.GetComponentInChildren<Slider>().SetValueWithoutNotify(CurrentSwivel.InterpolationSpeed);

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
      scrollRect.content = scrollViewContent;

      var layout = scrollRect.gameObject.AddComponent<LayoutElement>();
      layout.flexibleHeight = 800f;
      layout.flexibleWidth = 800f;
      layout.minWidth = 500f;
      layout.minHeight = 300f;
      layout.preferredHeight = 500f;

      SwivelUIHelpers.AddRowWithButton(layoutParent, viewStyles, SwivelUIPanelStrings.SwivelConfig, "X", 48f, 48f, out _, Hide);
      modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, SwivelUIPanelStrings.SwivelMode, EnumNames<SwivelMode>(), CurrentSwivel.Mode.ToString(), i =>
      {
        CurrentSwivel.SetMode((SwivelMode)i);
        RefreshUI();
        UnsetSavedState();
      });

      motionStateDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, viewStyles, SwivelUIPanelStrings.MotionState, EnumNames<MotionState>(), CurrentSwivel.CurrentMotionState.ToString(), i =>
      {
        CurrentSwivel.SetMotionState((MotionState)i);
        UnsetSavedState();
      });

      movementLerpRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.InterpolationSpeed, 1f, 100f, CurrentSwivel.InterpolationSpeed, v =>
      {
        CurrentSwivel.SetMovementLerpSpeed(v);
        UnsetSavedState();
      });

      rotationSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.RotationSettings);

      hingeAxisRow = SwivelUIHelpers.AddMultiToggleRow(layoutParent, viewStyles, SwivelUIPanelStrings.HingeAxes,
        new[] { "X", "Y", "Z" },
        new[]
        {
          CurrentSwivel.HingeAxes.HasFlag(HingeAxis.X),
          CurrentSwivel.HingeAxes.HasFlag(HingeAxis.Y),
          CurrentSwivel.HingeAxes.HasFlag(HingeAxis.Z)
        },
        selected =>
        {
          var axis = HingeAxis.None;
          if (selected[0]) axis |= HingeAxis.X;
          if (selected[1]) axis |= HingeAxis.Y;
          if (selected[2]) axis |= HingeAxis.Z;
          CurrentSwivel.SetHingeAxes(axis);
          UnsetSavedState();
        });

      maxXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxXAngle, 0f, 360f, CurrentSwivel.MaxEuler.x, v =>
      {
        var e = CurrentSwivel.MaxEuler;
        e.x = v;
        CurrentSwivel.SetMaxEuler(e);
        UnsetSavedState();
      });

      maxYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxYAngle, 0f, 360f, CurrentSwivel.MaxEuler.y, v =>
      {
        var e = CurrentSwivel.MaxEuler;
        e.y = v;
        CurrentSwivel.SetMaxEuler(e);
        UnsetSavedState();
      });

      maxZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.MaxZAngle, 0f, 360f, CurrentSwivel.MaxEuler.z, v =>
      {
        var e = CurrentSwivel.MaxEuler;
        e.z = v;
        CurrentSwivel.SetMaxEuler(e);
        UnsetSavedState();
      });

      movementSectionLabel = SwivelUIHelpers.AddSectionLabel(layoutParent, viewStyles, SwivelUIPanelStrings.MovementSettings);

      targetDistanceXRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetXOffset, MinTargetOffset, MaxTargetOffset, CurrentSwivel.movementOffset.x, v =>
      {
        var o = CurrentSwivel.movementOffset;
        o.x = v;
        CurrentSwivel.SetMovementOffset(o);
        UnsetSavedState();
      });

      targetDistanceYRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetYOffset, MinTargetOffset, MaxTargetOffset, CurrentSwivel.movementOffset.y, v =>
      {
        var o = CurrentSwivel.movementOffset;
        o.y = v;
        CurrentSwivel.SetMovementOffset(o);
        UnsetSavedState();
      });

      targetDistanceZRow = SwivelUIHelpers.AddSliderRow(layoutParent, viewStyles, SwivelUIPanelStrings.TargetZOffset, MinTargetOffset, MaxTargetOffset, CurrentSwivel.movementOffset.z, v =>
      {
        var o = CurrentSwivel.movementOffset;
        o.z = v;
        CurrentSwivel.SetMovementOffset(o);
        UnsetSavedState();
      });

      SwivelUIHelpers.AddRowWithButton(layoutParent, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnPanelUpdate();
        if (_saveStatus != null) _saveStatus.text = SwivelUIPanelStrings.Saved;
      });

      _hasCreatedUI = true;
    }

    private void RefreshUI()
    {
      if (CurrentSwivel == null) return;
      var isRotating = CurrentSwivel.Mode == SwivelMode.Rotate;
      var isMoving = CurrentSwivel.Mode == SwivelMode.Move;

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