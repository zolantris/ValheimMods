#region

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {
    public static partial class SwivelUIHelpers
    {
      public static GameObject AddHeaderWithCloseButton(Transform parent, SwivelUISharedStyles viewStyles, string text, UnityAction onClose)
{
    var row = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
    row.transform.SetParent(parent, false);

    var layout = row.GetComponent<HorizontalLayoutGroup>();
    layout.childAlignment = TextAnchor.MiddleCenter;
    layout.childControlHeight = true;
    layout.childControlWidth = true;
    layout.childForceExpandWidth = false;
    layout.spacing = 10f;

    // === Label ===
    var labelGO = new GameObject("HeaderLabel", typeof(TextMeshProUGUI), typeof(LayoutElement));
    labelGO.transform.SetParent(row.transform, false);
    var label = labelGO.GetComponent<TextMeshProUGUI>();
    label.text = text;
    label.fontSize = viewStyles.FontSizeSectionLabel;
    label.color = Color.white;
    label.alignment = TextAlignmentOptions.Left;
    label.enableAutoSizing = false; // ⬅ prevent font autosize

    var labelLayout = labelGO.GetComponent<LayoutElement>();
    labelLayout.flexibleWidth = 1f; // ⬅ allow label to stretch
    labelLayout.minWidth = viewStyles.LabelMinWidth;
    labelLayout.preferredWidth = viewStyles.LabelPreferredWidth;

    // === Close Button ===
    var buttonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
    buttonGO.transform.SetParent(row.transform, false);
    
    var buttonRT = buttonGO.GetComponent<RectTransform>();
    buttonRT.sizeDelta = new Vector2(48f, 48f); // ⬅ lock button size
    buttonRT.anchorMin = new Vector2(1f, 0.5f);
    buttonRT.anchorMax = new Vector2(1f, 0.5f);
    buttonRT.pivot = new Vector2(1f, 0.5f);
    buttonRT.anchoredPosition = Vector2.zero;

    var button = buttonGO.GetComponent<Button>();
    button.onClick.AddListener(onClose);

    var buttonImage = buttonGO.GetComponent<Image>();
    buttonImage.color = SwivelUIColors.grayBg;

    var buttonLE = buttonGO.GetComponent<LayoutElement>();
    buttonLE.preferredWidth = 48f;
    buttonLE.preferredHeight = 48f;
    buttonLE.minWidth = 48f;
    buttonLE.minHeight = 48f;
    buttonLE.flexibleWidth = 0;

    // === X Text ===
    var textGO = new GameObject("XLabel", typeof(TextMeshProUGUI));
    textGO.transform.SetParent(buttonGO.transform, false);
    var xText = textGO.GetComponent<TextMeshProUGUI>();
    xText.text = "X";
    xText.fontSize = 32;
    xText.color = Color.white;
    xText.alignment = TextAlignmentOptions.Center;

    var xRT = textGO.GetComponent<RectTransform>();
    xRT.anchorMin = Vector2.zero;
    xRT.anchorMax = Vector2.one;
    xRT.offsetMin = Vector2.zero;
    xRT.offsetMax = Vector2.zero;

    return row;
}


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