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
      public static GameObject CreateSpacer(Transform parent, float height = 20f)
      {
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(parent, false);
        var layout = spacer.GetComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        return spacer;
      }

      public static void ApplyLabelStyle(TextMeshProUGUI label, SwivelUISharedStyles viewStyles)
      {
        label.fontSize = viewStyles.FontSizeRowLabel;
        label.color = viewStyles.LabelColor;
        label.alignment = TextAlignmentOptions.Left;
      }

      public static void ApplyInputStyle(TextMeshProUGUI label, SwivelUISharedStyles viewStyles)
      {
        label.fontSize = viewStyles.FontSizeDropdownLabel;
        label.color = viewStyles.LabelColor;
        label.alignment = TextAlignmentOptions.Left;
      }
    }
  }