#region

using TMPro;
using UnityEngine;
using UnityEngine.UI;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static GameObject CreateSpacer(Transform parent, float height = 20f)
    {
      var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
      spacer.transform.SetParent(parent, false);
      var layout = spacer.GetComponent<LayoutElement>();
      layout.minHeight = height;
      layout.preferredHeight = height;
      return spacer;
    }

    public static void ApplyLabelStyle(TextMeshProUGUI label, int fontSize = SwivelUIConstants.FontSizeRow)
    {
      label.fontSize = fontSize;
      label.color = SwivelUIConstants.LabelColor;
      label.alignment = TextAlignmentOptions.Left;
    }

    public static void ApplyInputStyle(TextMeshProUGUI label)
    {
      label.fontSize = SwivelUIConstants.FontSizeRow;
      label.color = SwivelUIConstants.InputTextColor;
      label.alignment = TextAlignmentOptions.Left;
    }
  }
}