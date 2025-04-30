#region

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static Toggle AddToggleRow(Transform parent, Unity2dStyles styles, string label, bool initial, UnityAction<bool> onChanged)
    {
      var row = CreateRow(parent, styles, label, out _);

      // === Toggle container ===
      var toggleContainerGO = new GameObject("ToggleContainer", typeof(RectTransform), typeof(LayoutElement));
      toggleContainerGO.transform.SetParent(row.transform, false);
      var toggleContainerRT = toggleContainerGO.GetComponent<RectTransform>();
      var toggleContainerLE = toggleContainerGO.GetComponent<LayoutElement>();
      toggleContainerLE.minWidth = 24;
      toggleContainerLE.minHeight = 24;
      toggleContainerLE.preferredWidth = 24;
      toggleContainerLE.preferredHeight = 24;

      // === Toggle ===
      var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
      toggleGO.transform.SetParent(toggleContainerGO.transform, false);
      var toggleImage = toggleGO.GetComponent<Image>();
      toggleImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
      toggleImage.raycastTarget = true;

      var toggle = toggleGO.GetComponent<Toggle>();
      toggle.isOn = initial;
      toggle.onValueChanged.AddListener(onChanged);
      toggle.targetGraphic = toggleImage;

      // === Checkmark ===
      var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
      checkmarkGO.transform.SetParent(toggleGO.transform, false);
      var checkmarkImage = checkmarkGO.GetComponent<Image>();
      checkmarkImage.color = new Color(0f, 0.4f, 0f, 1f);
      checkmarkImage.raycastTarget = false;
      toggle.graphic = checkmarkImage;

      var checkmarkRT = checkmarkGO.GetComponent<RectTransform>();
      checkmarkRT.anchorMin = new Vector2(0.2f, 0.2f);
      checkmarkRT.anchorMax = new Vector2(0.8f, 0.8f);
      checkmarkRT.offsetMin = Vector2.zero;
      checkmarkRT.offsetMax = Vector2.zero;

      var toggleRT = toggleGO.GetComponent<RectTransform>();
      toggleRT.anchorMin = Vector2.zero;
      toggleRT.anchorMax = Vector2.one;
      toggleRT.offsetMin = Vector2.zero;
      toggleRT.offsetMax = Vector2.zero;

      return toggle;
    }
  }
}