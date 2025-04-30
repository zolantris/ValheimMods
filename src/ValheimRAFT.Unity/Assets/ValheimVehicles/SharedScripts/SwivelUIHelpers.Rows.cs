#region

using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static GameObject CreateRow(Transform parent, string label, out TextMeshProUGUI labelText)
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
      labelText.fontSize = SwivelUIConstants.FontSizeRow;
      labelText.color = SwivelUIConstants.LabelColor;
      labelText.alignment = TextAlignmentOptions.Left;

      var labelLayout = labelGO.GetComponent<LayoutElement>();
      labelLayout.minWidth = SwivelUIConstants.LabelMinWidth;
      labelLayout.preferredWidth = SwivelUIConstants.LabelPreferredWidth;

      return rowGO;
    }
  }
}