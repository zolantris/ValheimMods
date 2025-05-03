#region

  using TMPro;
  using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {
    public static partial class SwivelUIHelpers
    {
      public static void AddSectionLabel(Transform parent, SwivelUISharedStyles viewStyles, string text)
      {
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);

        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = viewStyles.FontSizeSectionLabel;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
      }
    }
  }