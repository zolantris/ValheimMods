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

    public static void ApplyLabelStyle(TextMeshProUGUI label, Unity2dStyles styles)
    {
      label.fontSize = styles.FontSizeRowLabel;
      label.color = styles.LabelColor;
      label.alignment = TextAlignmentOptions.Left;
    }

    public static void ApplyInputStyle(TextMeshProUGUI label, Unity2dStyles styles)
    {
      label.fontSize = styles.FontSizeDropdownLabel;
      label.color = styles.LabelColor;
      label.alignment = TextAlignmentOptions.Left;
    }
  }
}