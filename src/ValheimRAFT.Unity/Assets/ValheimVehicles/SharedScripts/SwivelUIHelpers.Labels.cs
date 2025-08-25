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
      public static GameObject AddSectionLabel(Transform parent, SwivelUISharedStyles viewStyles, string text, bool hasLayoutElement = false)
      {
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(parent, false);

        var label = labelGO.AddComponent<TMP_Text>();
        label.text = text;
        label.fontSize = viewStyles.FontSizeSectionLabel;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;

        if (hasLayoutElement)
        {
          var le = labelGO.AddComponent<LayoutElement>();
          // le.preferredWidth = 100;
          // le.preferredHeight = 20;
          le.minWidth = 200f;
          le.minHeight = 60f;
          le.flexibleWidth = 400;
          le.flexibleHeight = 0;
        }

        return labelGO;
      }
    }
  }