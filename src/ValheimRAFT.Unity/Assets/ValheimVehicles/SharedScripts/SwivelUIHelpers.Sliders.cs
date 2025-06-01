// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{

  public static partial class SwivelUIHelpers
  {
    public class CustomSlider : Slider
    {
      public float keyboardStep = 0.5f;

      public override void OnMove(AxisEventData eventData)
      {
        // Prevent default slider behavior
        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
        {
          var delta = eventData.moveDir == MoveDirection.Right ? keyboardStep : -keyboardStep;
          var newValue = Mathf.Clamp(value + delta, minValue, maxValue);
          SetValueWithoutNotify(newValue);
          onValueChanged.Invoke(newValue);
          eventData.Use(); // prevent bubbling
        }
        else
        {
          base.OnMove(eventData); // for Up/Down/Tab etc.
        }
      }
    }

    public static GameObject AddSliderRow(Transform parent, SwivelUISharedStyles viewStyles, string label, float min, float max, float initial, UnityAction<float> onChanged)
    {
      return AddSliderRow(parent, viewStyles, label, min, max, initial, onChanged, out _, null);
    }
    public static GameObject AddSliderRow(Transform parent, SwivelUISharedStyles viewStyles, string label, float min, float max, float initial, UnityAction<float> onChanged, out Slider slider, float? forceExpandWidth = null)
    {
      var container = new GameObject("Slider_Container", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
      container.transform.SetParent(parent, false);

      var topRow = CreateRow(container.transform, viewStyles, label, out _);
      var row = CreateRow(container.transform, viewStyles, null, out _);

      var horizontalLayout = row.GetComponent<HorizontalLayoutGroup>();
      horizontalLayout.padding = new RectOffset(0, 0, 12, 12);

      // === Slider GameObject ===
      var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(CustomSlider), typeof(LayoutElement));

      var sliderLayoutElement = sliderGO.GetComponent<LayoutElement>();
      sliderLayoutElement.minHeight = 24;

      if (forceExpandWidth.HasValue)
      {
        sliderLayoutElement.minWidth = forceExpandWidth.Value;
        sliderLayoutElement.flexibleWidth = 0f;
      }

      var maxOffsetRight = -70; // for label spacing

      sliderGO.transform.SetParent(row.transform, false);
      var sliderRT = sliderGO.GetComponent<RectTransform>();
      sliderRT.anchorMin = new Vector2(0, 0.5f);
      sliderRT.anchorMax = new Vector2(1, 0.5f);
      sliderRT.offsetMin = new Vector2(10, 0);
      sliderRT.offsetMax = new Vector2(maxOffsetRight, 0);
      sliderRT.pivot = new Vector2(0.5f, 0.5f);

      var localSlider = sliderGO.GetComponent<Slider>();
      localSlider.minValue = min;
      localSlider.maxValue = max;
      localSlider.value = initial;
      localSlider.wholeNumbers = false;
      localSlider.interactable = CanNavigatorInteractWithPanel;

      // === Background ===
      var backgroundGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
      backgroundGO.transform.SetParent(sliderGO.transform, false);
      var bgImage = backgroundGO.GetComponent<Image>();
      bgImage.color = SwivelUIColors.grayBg;
      bgImage.raycastTarget = false;

      var bgRT = backgroundGO.GetComponent<RectTransform>();
      bgRT.anchorMin = Vector2.zero;
      bgRT.anchorMax = Vector2.one;
      bgRT.offsetMin = Vector2.zero;
      bgRT.offsetMax = Vector2.zero;

      localSlider.targetGraphic = bgImage;

      // === Fill Area ===
      var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
      fillAreaGO.transform.SetParent(sliderGO.transform, false);
      var fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
      fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
      fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
      fillAreaRT.offsetMin = new Vector2(10, 0);
      fillAreaRT.offsetMax = new Vector2(maxOffsetRight, 0); // leave space for label

      // === Fill ===
      var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
      fillGO.transform.SetParent(fillAreaGO.transform, false);
      var fillImage = fillGO.GetComponent<Image>();
      fillImage.color = new Color(0.1f, 0.6f, 0.1f, 1f);
      fillImage.raycastTarget = false;

      var fillRT = fillGO.GetComponent<RectTransform>();
      fillRT.anchorMin = Vector2.zero;
      fillRT.anchorMax = Vector2.one;
      fillRT.offsetMin = Vector2.zero;
      fillRT.offsetMax = Vector2.zero;

      localSlider.fillRect = fillRT;

      // === Handle Slide Area ===
      var handleSlideAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
      handleSlideAreaGO.transform.SetParent(sliderGO.transform, false);
      var handleSlideRT = handleSlideAreaGO.GetComponent<RectTransform>();
      handleSlideRT.anchorMin = new Vector2(0, 0);
      handleSlideRT.anchorMax = new Vector2(1, 1);
      handleSlideRT.offsetMin = new Vector2(10, 0);
      handleSlideRT.offsetMax = new Vector2(maxOffsetRight, 0);

      // === Handle ===
      var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
      handleGO.transform.SetParent(handleSlideAreaGO.transform, false);
      var handleImage = handleGO.GetComponent<Image>();
      handleImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
      handleImage.raycastTarget = true;

      var handleRT = handleGO.GetComponent<RectTransform>();
      handleRT.sizeDelta = new Vector2(20, 20);
      handleRT.anchorMin = new Vector2(0, 0.5f);
      handleRT.anchorMax = new Vector2(0, 0.5f);
      handleRT.anchoredPosition = Vector2.zero;

      localSlider.handleRect = handleRT;

      // === Value Label ===
      var valueLabelGO = new GameObject("ValueLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
      valueLabelGO.transform.SetParent(sliderGO.transform, false);
      var valueLabel = valueLabelGO.GetComponent<TextMeshProUGUI>();
      valueLabel.fontSize = viewStyles.FontSizeRowLabel;
      valueLabel.color = viewStyles.InputTextColor;
      valueLabel.alignment = TextAlignmentOptions.MidlineRight;
      valueLabel.enableAutoSizing = true;
      valueLabel.text = Mathf.RoundToInt(initial).ToString();

      var valueRT = valueLabelGO.GetComponent<RectTransform>();
      valueRT.anchorMin = new Vector2(1f, 0);
      valueRT.anchorMax = new Vector2(1f, 1f);
      valueRT.pivot = new Vector2(1f, 0.5f);
      valueRT.offsetMin = new Vector2(-50, 0);
      valueRT.offsetMax = new Vector2(0, 0);

      // === Update ===
      localSlider.onValueChanged.AddListener(v =>
      {
        // half-step IE 0.5f -> 1f -> 1.5f;
        var rounded = Mathf.Round(v * 2f) * 0.5f;
        localSlider.SetValueWithoutNotify(rounded); // prevent infinite loop
        valueLabel.text = rounded.ToString(CultureInfo.CurrentCulture);
        onChanged?.Invoke(rounded);
      });

      slider = localSlider;

      return container;
    }
  }
}