#region

using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static GameObject CreateRow(Transform parent, SwivelUISharedStyles viewStyles, string label, out TextMeshProUGUI labelText, bool hasForceExpandWidth = true)
    {
      // === Horizontal layout container ===
      var rowGO = new GameObject($"{label}_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
      rowGO.transform.SetParent(parent, false);

      var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
      layout.childAlignment = TextAnchor.MiddleLeft;
      layout.childControlHeight = true;
      layout.childControlWidth = true;
      layout.childForceExpandWidth = hasForceExpandWidth;
      layout.spacing = 10f;

      // === Label ===
      var labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
      labelGO.transform.SetParent(rowGO.transform, false);

      labelText = labelGO.GetComponent<TextMeshProUGUI>();
      labelText.text = label;
      labelText.fontSize = viewStyles.FontSizeRowLabel;
      labelText.color = viewStyles.LabelColor;
      labelText.alignment = TextAlignmentOptions.Left;
      labelText.enableAutoSizing = false;

      var labelLayout = labelGO.GetComponent<LayoutElement>();
      labelLayout.minWidth = viewStyles.LabelMinWidth;
      labelLayout.preferredWidth = viewStyles.LabelPreferredWidth;
      labelLayout.flexibleWidth = 1f; // <- allows label to stretch

      return rowGO;
    }
  }
}