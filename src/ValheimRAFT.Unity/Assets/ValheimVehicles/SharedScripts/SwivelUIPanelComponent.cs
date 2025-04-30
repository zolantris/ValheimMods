#region

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public class SwivelUIPanelComponent : SingletonBehaviour<SwivelUIPanelComponent>
  {
    [Header("UI Settings")]
    [SerializeField] public float MaxUIWidth = 700f;
    private Toggle doorToggle;
    private TMP_Dropdown hingeDropdown;
    private Transform layoutParent;
    private Slider maxYSlider;
    private Slider maxZSlider;
    private TMP_Dropdown modeDropdown;

    private SwivelComponent swivel;
    private TMP_Dropdown yDirDropdown;
    private TMP_Dropdown zDirDropdown;

    protected override void OnAwake()
    {
      CreateUI();
      Hide();
    }

    public void BindTo(SwivelComponent target)
    {
      swivel = target;

      modeDropdown.SetValueWithoutNotify((int)swivel.Mode);
      doorToggle.isOn = swivel.IsDoorOpen;
      hingeDropdown.SetValueWithoutNotify((int)swivel.CurrentHingeMode);
      zDirDropdown.SetValueWithoutNotify((int)swivel.CurrentZHingeDirection);
      yDirDropdown.SetValueWithoutNotify((int)swivel.CurrentYHingeDirection);
      maxZSlider.SetValueWithoutNotify(swivel.MaxInclineZ);
      maxYSlider.SetValueWithoutNotify(swivel.MaxYAngle);

      RefreshUI();
      Show();
    }

    public void Show()
    {
      gameObject.SetActive(true);
    }
    public void Hide()
    {
      gameObject.SetActive(false);
    }

    private void CreateUI()
    {
      var canvasGO = new GameObject("SwivelUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
      canvasGO.transform.SetParent(transform, false);
      var canvas = canvasGO.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;

      var scaler = canvasGO.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920, 1080);

      var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
      scrollGO.transform.SetParent(canvasGO.transform, false);
      var scrollRect = scrollGO.GetComponent<ScrollRect>();
      var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
      scrollRectTransform.anchorMin = new Vector2(0, 1);
      scrollRectTransform.anchorMax = new Vector2(0, 1);
      scrollRectTransform.pivot = new Vector2(0, 1);
      scrollRectTransform.anchoredPosition = new Vector2(20f, -20f);
      scrollRectTransform.sizeDelta = new Vector2(Mathf.Clamp(Screen.width * 0.3f, 500f, MaxUIWidth), 600f);

      var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
      viewportGO.transform.SetParent(scrollGO.transform, false);
      var viewportRT = viewportGO.GetComponent<RectTransform>();
      viewportRT.anchorMin = Vector2.zero;
      viewportRT.anchorMax = Vector2.one;
      viewportRT.offsetMin = Vector2.zero;
      viewportRT.offsetMax = Vector2.zero;
      scrollRect.viewport = viewportRT;

      var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
      contentGO.transform.SetParent(viewportGO.transform, false);
      layoutParent = contentGO.transform;

      var layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
      layoutGroup.childControlHeight = true;
      layoutGroup.childControlWidth = true;
      layoutGroup.spacing = 10f;
      layoutGroup.padding = new RectOffset(20, 20, 20, 20);
      layoutGroup.childAlignment = TextAnchor.UpperLeft;

      var fitter = contentGO.GetComponent<ContentSizeFitter>();
      fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
      fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

      var contentRT = contentGO.GetComponent<RectTransform>();
      contentRT.anchorMin = new Vector2(0, 1);
      contentRT.anchorMax = new Vector2(1, 1);
      contentRT.pivot = new Vector2(0.5f, 1);
      contentRT.sizeDelta = new Vector2(0, 0);

      scrollRect.content = contentRT;

      SwivelUIHelpers.AddSectionLabel(layoutParent, "Swivel Mode");
      modeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, "Mode", EnumNames<SwivelMode>(), i =>
      {
        if (swivel != null)
        {
          swivel.SetMode((SwivelMode)i);
          RefreshUI();
        }
      });

      SwivelUIHelpers.AddSectionLabel(layoutParent, "Door Mode Settings");
      doorToggle = SwivelUIHelpers.AddToggleRow(layoutParent, "Door Open", false, isOn =>
      {
        if (swivel != null) swivel.SetDoorOpen(isOn);
      });
      hingeDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, "Hinge Mode", EnumNames<SwivelComponent.DoorHingeMode>(), i =>
      {
        if (swivel != null)
        {
          swivel.SetHingeMode((SwivelComponent.DoorHingeMode)i);
          RefreshUI();
        }
      });
      zDirDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, "Z Hinge Dir", EnumNames<SwivelComponent.HingeDirection>(), i =>
      {
        if (swivel != null) swivel.SetZHingeDirection((SwivelComponent.HingeDirection)i);
      });
      yDirDropdown = SwivelUIHelpers.AddDropdownRow(layoutParent, "Y Hinge Dir", EnumNames<SwivelComponent.HingeDirection>(), i =>
      {
        if (swivel != null) swivel.SetYHingeDirection((SwivelComponent.HingeDirection)i);
      });
      maxZSlider = SwivelUIHelpers.AddSliderRow(layoutParent, "Max Z Angle", 0f, 90f, 0f, v =>
      {
        if (swivel != null) swivel.SetMaxInclineZ(v);
      });
      maxYSlider = SwivelUIHelpers.AddSliderRow(layoutParent, "Max Y Angle", 0f, 90f, 0f, v =>
      {
        if (swivel != null) swivel.SetMaxYAngle(v);
      });
    }

    private void RefreshUI()
    {
      if (swivel == null) return;
      var isDoor = swivel.Mode == SwivelMode.DoorMode;
      doorToggle.gameObject.SetActive(isDoor);
      hingeDropdown.gameObject.SetActive(isDoor);
      zDirDropdown.gameObject.SetActive(isDoor);
      yDirDropdown.gameObject.SetActive(isDoor);
      maxZSlider.gameObject.SetActive(isDoor);
      maxYSlider.gameObject.SetActive(isDoor);
    }

    private string[] EnumNames<T>() where T : Enum
    {
      return Enum.GetNames(typeof(T));
    }
  }
}