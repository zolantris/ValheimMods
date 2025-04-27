using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimVehicles.ValheimVehicles.GUI;

public static class TMPDropdownFactory
{
    public static Color BackgroundElementColor = new(0f, 0f, 0f, 1f); // black

    public static TMP_Dropdown CreateTMPDropDown(
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        string captionText = "",
        string itemPrototypeText = "Option")
    {
        // Create root object
        var dropdownGO = new GameObject("VehicleGUI_TMP_Dropdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        dropdownGO.transform.SetParent(parent, false);

        var dropdownRT = dropdownGO.GetComponent<RectTransform>();
        dropdownRT.sizeDelta = size;
        dropdownRT.anchoredPosition = anchoredPosition;

        // Create Label
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(dropdownGO.transform, false);

        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10f, 6f);
        labelRT.offsetMax = new Vector2(-25f, -7f);

        var labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.font = TMP_Settings.defaultFontAsset;
        labelText.text = captionText ?? "";
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.fontSize = 24;
        labelText.color = Jotunn.Managers.GUIManager.Instance.ValheimOrange;

        // Create Arrow
        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        arrowGO.transform.SetParent(dropdownGO.transform, false);

        var arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(20f, 20f);
        arrowRT.anchoredPosition = new Vector2(-15f, 0f);

        // Create Template
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(dropdownGO.transform, false);
        templateGO.SetActive(false);

        var templateRT = templateGO.GetComponent<RectTransform>();
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchorMin = new Vector2(0f, 0f);
        templateRT.anchorMax = new Vector2(1f, 0f);
        templateRT.sizeDelta = new Vector2(0f, 150f);

        // 🔥 Add Canvas to Template so dropdown expands over other UI
        var templateCanvas = templateGO.AddComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 300;

        var scrollRect = templateGO.GetComponent<ScrollRect>();

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);

        var viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.pivot = new Vector2(0f, 1f);
        viewportRT.anchorMin = new Vector2(0f, 0f);
        viewportRT.anchorMax = new Vector2(1f, 1f);
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        var viewportMask = viewportGO.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scrollRect.viewport = viewportRT;

        // Content (actual items holder)
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);

        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        var layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.spacing = 5f;
        layoutGroup.padding = new RectOffset(0, 0, 5, 5);

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // Create an item (Option prototype)
        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGO.transform.SetParent(contentGO.transform, false);

        var itemRT = itemGO.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0f, 30f);

        var toggle = itemGO.GetComponent<Toggle>();
        var itemBackground = itemGO.AddComponent<Image>();
        toggle.targetGraphic = itemBackground;

        // Item label
        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(itemGO.transform, false);

        var itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(10f, 0f);
        itemLabelRT.offsetMax = new Vector2(-10f, 0f);

        var itemLabelText = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabelText.font = TMP_Settings.defaultFontAsset;
        itemLabelText.text = itemPrototypeText ?? "Option";
        itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabelText.fontSize = 22;
        itemLabelText.color = Jotunn.Managers.GUIManager.Instance.ValheimOrange;

        // Setup TMP_Dropdown fields
        var dropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        dropdown.captionText = labelText;
        dropdown.template = templateRT;
        dropdown.itemText = itemLabelText;
        dropdown.targetGraphic = dropdownGO.GetComponent<Image>();

        // Hook up ScrollRect's Content
        scrollRect.content = contentRT;

        // Background coloring
        var dropdownBackground = dropdownGO.GetComponent<Image>();
        var templateBackground = templateGO.GetComponent<Image>();
        dropdownBackground.color = BackgroundElementColor;
        templateBackground.color = BackgroundElementColor;
        itemBackground.color = BackgroundElementColor;

        return dropdown;
    }
}