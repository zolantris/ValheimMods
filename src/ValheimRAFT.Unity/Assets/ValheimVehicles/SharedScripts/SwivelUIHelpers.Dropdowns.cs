#region

  using System.Collections.Generic;
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
      private static string GetCaptionText(string selectedOption, string[] options)
      {
        return selectedOption != ""
          ? selectedOption
          : options.Length > 0
            ? options[0]
            : "";
      }
      public static TMP_Dropdown AddDropdownRow(Transform parent, SwivelUISharedStyles viewStyles, string label, string[] options, string selectedOption, UnityAction<int> onChanged)
      {
        // === Root Vertical Group ===
        var root = new GameObject($"{label}_DropdownGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(parent, false);

        var layout = root.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.spacing = 2f;

        // === Label ===
        var labelGO = new GameObject("Label", typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(root.transform, false);
        var labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = viewStyles.FontSizeRowLabel;
        labelText.color = viewStyles.LabelColor;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.enableWordWrapping = false;

        // === Dropdown ===
        var dropdownGO = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        dropdownGO.transform.SetParent(root.transform, false);
        var dropdownRT = dropdownGO.GetComponent<RectTransform>();
        dropdownRT.anchorMin = new Vector2(0, 0);
        dropdownRT.anchorMax = new Vector2(1, 0);
        dropdownRT.offsetMin = Vector2.zero;
        dropdownRT.offsetMax = new Vector2(0, 32);

        var dropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        var dropdownLE = dropdownGO.AddComponent<LayoutElement>();
        dropdownLE.minHeight = 32f;
        dropdownLE.flexibleWidth = 1f;

        dropdown.options = new List<TMP_Dropdown.OptionData>();
        foreach (var option in options)
          dropdown.options.Add(new TMP_Dropdown.OptionData(option));
        dropdown.onValueChanged.AddListener(onChanged);

        dropdownGO.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray background

        // === Caption ===
        var captionGO = new GameObject("Caption", typeof(TextMeshProUGUI));
        var captionRect = captionGO.GetComponent<RectTransform>();
        captionRect.anchorMin = new Vector2(0, 0.5f);
        captionRect.anchorMax = new Vector2(1, 0.5f);
        captionRect.offsetMin = new Vector2(viewStyles.DropdownContentPaddingLeft, 0);
        captionRect.offsetMax = new Vector2(-viewStyles.DropdownContentPaddingRight, 0);
        captionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);

        captionGO.transform.SetParent(dropdownGO.transform, false);
        var caption = captionGO.GetComponent<TextMeshProUGUI>();
        caption.text = GetCaptionText(selectedOption, options);
        caption.color = viewStyles.InputTextColor;
        caption.alignment = TextAlignmentOptions.Left;
        caption.enableWordWrapping = false;
        caption.enableAutoSizing = true;
        caption.overflowMode = TextOverflowModes.Ellipsis;
        dropdown.captionText = caption;

        // === Template ===
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(dropdownGO.transform, false);
        templateGO.SetActive(true); // Force layout init
        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 1);
        templateRT.sizeDelta = new Vector2(0, Mathf.Clamp(viewStyles.DropdownItemHeight * options.Length + viewStyles.DropdownItemHeight, 150f, viewStyles.DropdownContentHeight));
        templateGO.GetComponent<Image>().color = viewStyles.DropdownOptionsContainerColor;

        dropdown.template = templateRT;
        var scrollRect = templateGO.GetComponent<ScrollRect>();
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // === Viewport ===
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportGO.transform.SetParent(templateGO.transform, false);
        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.one;

        scrollRect.viewport = viewportRT;
        viewportGO.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // === Content ===
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, 0);
        scrollRect.content = contentRT;

        var contentLayout = contentGO.GetComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 18f;
        contentLayout.padding = new RectOffset(viewStyles.DropdownContentPaddingLeft, viewStyles.DropdownContentPaddingRight, viewStyles.DropdownContentPaddingTop, viewStyles.DropdownContentPaddingBottom); // Extra padding top/bottom between items

        var contentFitter = contentGO.GetComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // === Item ===
        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.offsetMin = new Vector2(viewStyles.DropdownContentPaddingLeft, 0);
        itemRT.offsetMax = new Vector2(viewStyles.DropdownContentPaddingRight, 0);
        itemRT.pivot = new Vector2(0, 0.5f);

        var itemBG = itemGO.AddComponent<Image>();
        itemBG.color = new Color(0.65f, 0.65f, 0.65f, 1f); // Darker background

        var itemToggle = itemGO.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemBG;
        itemToggle.graphic = itemBG;

        var itemLayout = itemGO.AddComponent<LayoutElement>();
        itemLayout.minHeight = viewStyles.DropdownItemHeight;
        itemLayout.preferredHeight = viewStyles.DropdownItemHeight;

        var itemLabelGO = new GameObject("Item Label", typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(itemGO.transform, false);

        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = new Vector2(0, 0.5f);
        itemLabelRT.anchorMax = new Vector2(1, 0.5f);
        itemLabelRT.pivot = new Vector2(0, 0.5f);
        itemLabelRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);

        var itemLabel = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.color = viewStyles.InputTextColor;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.enableWordWrapping = true;
        itemLabel.overflowMode = TextOverflowModes.Overflow;
        itemLabel.fontSize = viewStyles.FontSizeRowLabel;
        itemLabel.enableAutoSizing = true;

        dropdown.itemText = itemLabel;
        dropdown.itemImage = itemBG;

        templateGO.SetActive(false); // Finalize layout

        return dropdown;
      }
    }
  }