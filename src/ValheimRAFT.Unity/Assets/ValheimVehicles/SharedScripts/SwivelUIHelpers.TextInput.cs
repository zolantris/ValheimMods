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
      public static GameObject AddTextInputRow(Transform parent, SwivelUISharedStyles viewStyles, string label, string currentValue, UnityAction<string> onChanged)
      {
        return AddTextInputRow(parent, viewStyles, label, currentValue, onChanged, out _);
      }

      public static GameObject AddTextInputRow(
        Transform parent,
        SwivelUISharedStyles viewStyles,
        string label,
        string currentValue,
        UnityAction<string> onChanged,
        out TMP_InputField inputField,
        string? placeholder = null,
        TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard,
        UnityAction<string>? onEndEdit = null,
        float minInputWidth = 220f)
      {
        var row = CreateRow(parent, viewStyles, label, out _, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childForceExpandWidth = false;

        var inputGO = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(TMP_InputField));
        inputGO.transform.SetParent(row.transform, false);

        var inputLayout = inputGO.GetComponent<LayoutElement>();
        inputLayout.minWidth = minInputWidth;
        inputLayout.preferredWidth = Mathf.Max(minInputWidth, viewStyles.LabelPreferredWidth);
        inputLayout.minHeight = 52f;
        inputLayout.preferredHeight = 52f;
        inputLayout.flexibleWidth = 2f;

        var inputBackground = inputGO.GetComponent<Image>();
        inputBackground.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        inputField = inputGO.GetComponent<TMP_InputField>();
        inputField.interactable = CanNavigatorInteractWithPanel;
        inputField.contentType = contentType;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.text = currentValue;
        inputField.richText = false;
        inputField.customCaretColor = true;
        inputField.caretColor = viewStyles.InputTextColor;
        inputField.selectionColor = new Color(0.25f, 0.45f, 0.85f, 0.35f);
        inputField.onValueChanged.AddListener(onChanged);
        if (onEndEdit != null) inputField.onEndEdit.AddListener(onEndEdit);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportGO.transform.SetParent(inputGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(16f, 8f);
        viewportRT.offsetMax = new Vector2(-16f, -8f);
        inputField.textViewport = viewportRT;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(viewportGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var text = textGO.GetComponent<TextMeshProUGUI>();
        ApplyInputStyle(text, viewStyles);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontSizeMin = 14;
        text.fontSizeMax = viewStyles.FontSizeDropdownLabel;
        text.enableAutoSizing = true;
        inputField.textComponent = text;

        var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(viewportGO.transform, false);
        var placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = Vector2.zero;
        placeholderRT.offsetMax = Vector2.zero;

        var placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
        ApplyInputStyle(placeholderText, viewStyles);
        placeholderText.text = string.IsNullOrWhiteSpace(placeholder) ? currentValue : placeholder;
        placeholderText.fontStyle = FontStyles.Italic;
        placeholderText.color = new Color(viewStyles.InputTextColor.r, viewStyles.InputTextColor.g, viewStyles.InputTextColor.b, 0.55f);
        placeholderText.textWrappingMode = TextWrappingModes.NoWrap;
        placeholderText.overflowMode = TextOverflowModes.Ellipsis;
        placeholderText.fontSizeMin = 14;
        placeholderText.fontSizeMax = viewStyles.FontSizeDropdownLabel;
        placeholderText.enableAutoSizing = true;
        inputField.placeholder = placeholderText;

        return row;
      }
    }
  }