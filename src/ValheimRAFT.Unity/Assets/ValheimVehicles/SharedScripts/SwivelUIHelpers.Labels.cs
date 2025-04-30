#region

using TMPro;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.UI
{
  public static partial class SwivelUIHelpers
  {
    public static void AddSectionLabel(Transform parent, Unity2dStyles styles, string text)
    {
      var labelGO = new GameObject("Label", typeof(RectTransform));
      labelGO.transform.SetParent(parent, false);

      var label = labelGO.AddComponent<TextMeshProUGUI>();
      label.text = text;
      label.fontSize = styles.FontSizeSectionLabel;
      label.color = Color.white;
      label.alignment = TextAlignmentOptions.Left;
    }
  }
}