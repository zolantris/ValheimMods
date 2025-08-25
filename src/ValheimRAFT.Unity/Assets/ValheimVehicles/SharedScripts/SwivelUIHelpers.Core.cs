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
      // for all interactions
      public static bool CanNavigatorInteractWithPanel = true;

      public static GameObject AddButton(Transform parent, SwivelUISharedStyles viewStyles, string buttonText, float buttonWidth, float buttonHeight, out Button button, out TextMeshProUGUI statusTextOut, UnityAction onClick)
      {
        var buttonGO = new GameObject("ActionButton", typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
        buttonGO.transform.SetParent(parent.transform, false);

        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        buttonRT.anchorMin = new Vector2(1f, 0.5f);
        buttonRT.anchorMax = new Vector2(1f, 0.5f);
        buttonRT.pivot = new Vector2(1f, 0.5f);
        buttonRT.anchoredPosition = Vector2.zero;

        var buttonInternal = buttonGO.GetComponent<Button>();
        buttonInternal.interactable = CanNavigatorInteractWithPanel;

        var buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = SwivelUIColors.grayBg;
        buttonInternal.onClick.AddListener(onClick);

        var layoutElement = buttonGO.GetComponent<LayoutElement>();
        layoutElement.minWidth = buttonWidth;
        layoutElement.minHeight = buttonHeight;
        layoutElement.preferredWidth = buttonWidth;
        layoutElement.preferredHeight = buttonHeight;
        layoutElement.flexibleWidth = 0;

        var textGO = new GameObject("ButtonText", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(buttonGO.transform, false);
        var btnText = textGO.GetComponent<TextMeshProUGUI>();
        btnText.text = buttonText;
        btnText.fontSize = 24;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;

        statusTextOut = btnText;

        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        button = buttonInternal;

        return buttonGO;
      }

      public static GameObject AddRowWithButton(Transform parent, SwivelUISharedStyles viewStyles, string? label, string buttonText, float buttonWidth, float buttonHeight, out TextMeshProUGUI statusTextOut, UnityAction onClick)
      {
        var row = new GameObject("RowWithButton", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        layout.spacing = 10f;

        if (!string.IsNullOrEmpty(label))
        {
          var labelGO = new GameObject("HeaderLabel", typeof(TextMeshProUGUI), typeof(LayoutElement));
          labelGO.transform.SetParent(row.transform, false);

          var labelText = labelGO.GetComponent<TextMeshProUGUI>();
          labelText.text = label;
          labelText.fontSize = viewStyles.FontSizeSectionLabel;
          labelText.color = viewStyles.LabelColor;
          labelText.alignment = TextAlignmentOptions.Left;
          labelText.enableAutoSizing = false;

          var labelLayout = labelGO.GetComponent<LayoutElement>();
          labelLayout.flexibleWidth = 1f;
          labelLayout.minWidth = viewStyles.LabelMinWidth;
          labelLayout.preferredWidth = viewStyles.LabelPreferredWidth;
        }

        AddButton(row.transform, viewStyles, buttonText, buttonWidth, buttonHeight, out var button, out statusTextOut, onClick);

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