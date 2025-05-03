#region

  using TMPro;
  using UnityEngine;
  using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {

    public static partial class SwivelUIHelpers
    {
      public static GameObject CreateRow(Transform parent, SwivelUISharedStyles viewStyles, string label, out TextMeshProUGUI labelText)
      {
        var rowGO = new GameObject($"{label}_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowGO.transform.SetParent(parent, false);

        var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 10f;

        var labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGO.transform.SetParent(rowGO.transform, false);
        labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = viewStyles.FontSizeRowLabel;
        labelText.color = viewStyles.LabelColor;
        labelText.alignment = TextAlignmentOptions.Left;

        var labelLayout = labelGO.GetComponent<LayoutElement>();
        labelLayout.minWidth = viewStyles.LabelMinWidth;
        labelLayout.preferredWidth = viewStyles.LabelPreferredWidth;

        return rowGO;
      }
    }
  }