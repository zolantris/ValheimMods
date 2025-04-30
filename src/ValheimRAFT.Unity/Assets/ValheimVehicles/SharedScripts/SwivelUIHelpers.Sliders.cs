#region

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static Slider AddSliderRow(Transform parent, Unity2dStyles styles, string label, float min, float max, float initial, UnityAction<float> onChanged)
    {
      var row = CreateRow(parent, styles, label, out _);

      var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
      sliderGO.transform.SetParent(row.transform, false);
      var slider = sliderGO.GetComponent<Slider>();
      slider.minValue = min;
      slider.maxValue = max;
      slider.value = initial;
      slider.onValueChanged.AddListener(onChanged);

      return slider;
    }
  }
}