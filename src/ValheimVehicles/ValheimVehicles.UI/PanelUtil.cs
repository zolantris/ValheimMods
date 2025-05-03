using System;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.UI;

public class PanelUtil
{
  public static GameObject CreateBasicPanel(string panelName, bool hasImage)
  {
    // Create root panel object
    var uiElementRoot = new GameObject(panelName, typeof(RectTransform), typeof(Image));

    uiElementRoot.name = panelName;

    // Optionally disable background image
    if (!hasImage)
    {
      var image = uiElementRoot.GetComponent<Image>();
      image.enabled = false;
    }

    // Set transform
    var rect = uiElementRoot.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = Vector2.zero;
    rect.sizeDelta = new Vector2(600f, 0f); // width fixed, height driven by children

    // Add layout group
    var layout = uiElementRoot.AddComponent<VerticalLayoutGroup>();
    layout.childControlHeight = true;
    layout.childForceExpandHeight = false;
    layout.childControlWidth = true;
    layout.childForceExpandWidth = true;
    layout.spacing = 10f;
    layout.padding = new RectOffset(20, 20, 20, 20);

    // Let the panel grow with its content
    var fitter = uiElementRoot.AddComponent<ContentSizeFitter>();
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

    // Clamp panel height to screen height
    var layoutElement = uiElementRoot.AddComponent<LayoutElement>();
    layoutElement.preferredHeight = Mathf.Clamp(Screen.height - 100f, 500, 800f);
    layoutElement.flexibleHeight = 0;

    return uiElementRoot;
  }


  public static void OnPanelPositionChange(RectTransform panelTransform, ConfigEntry<Vector2> WindowPosition)
  {
    var anchoredPosition = panelTransform.anchoredPosition;
    WindowPosition.Value = anchoredPosition;
  }

  public static GameObject CreateDraggableHideShowPanel(string panelName, Unity2dViewStyles panelStyles, Unity2dViewStyles buttonStyles, string hideText, string showText, ConfigEntry<Vector2> WindowPosition, Action<Text>? onToggle = null, bool isActive = false)
  {
    var panel = CreateBasicPanel(panelName, false);
    var dragWindowExtension = panel.AddComponent<DragWindowControllerExtension>();
    panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
    panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;

    var panelTransform = (RectTransform)panel.transform;
    panelTransform.anchoredPosition = WindowPosition.Value;
    panelTransform.anchorMin = panelStyles.anchorMin;
    panelTransform.anchorMax = panelStyles.anchorMax;

    if (panelStyles.width != null)
    {
      panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelStyles.width ?? 0);
    }
    if (panelStyles.height != null)
    {
      panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelStyles.height ?? 0);
    }

    dragWindowExtension.OnDragCalled += (_panelTransform) =>
    {
      OnPanelPositionChange(_panelTransform, WindowPosition);
    };
    // Create the button object above the gui manager. So it can hide itself.
    var buttonObject = GUIManager.Instance.CreateButton(
      isActive ? hideText : showText,
      panel.transform,
      buttonStyles.anchorMin,
      buttonStyles.anchorMax,
      buttonStyles.position,
      buttonStyles.width ?? 150,
      buttonStyles.height ?? 150);
    var buttonLayout = buttonObject.AddComponent<LayoutElement>();
    buttonLayout.flexibleHeight = 0; // Don't stretch vertically
    buttonLayout.preferredHeight = buttonStyles.height ?? 40f; // Set fixed height
    buttonLayout.minHeight = 32f; // Prevent collapsing
    var text = buttonObject.GetComponentInChildren<Text>();
    // Add a listener to the button to close the panel again
    var button = buttonObject.GetComponent<Button>();


    var hasToggle = onToggle != null;


    button.onClick.AddListener(() =>
    {
      if (text == null) return;
      if (hasToggle)
      {
        onToggle?.Invoke(text);
        return;
      }
      // logic for providing a null toggle.
      var nextState = !panel.activeSelf;
      text.text = nextState ? hideText : showText;
      panel.SetActive(nextState);
    });


    return panel;
  }

  public static void ApplyPanelStyle(GameObject editPanel)
  {
    Array.ForEach(editPanel.GetComponentsInChildren<Button>(true),
      delegate(Button b)
      {
        if (b.name.EndsWith("Button")) GUIManager.Instance.ApplyButtonStyle(b);
      });
    Array.ForEach(editPanel.GetComponentsInChildren<InputField>(true),
      delegate(InputField b)
      {
        GUIManager.Instance.ApplyInputFieldStyle(b, 16);
      });
    Array.ForEach(editPanel.GetComponentsInChildren<Text>(true),
      delegate(Text b)
      {
        b.text = Localization.instance.Localize(b.text);
        if (b.name.EndsWith("Label")) GUIManager.Instance.ApplyTextStyle(b);
      });
    Array.ForEach(editPanel.GetComponentsInChildren<Toggle>(true),
      delegate(Toggle b)
      {
        Logger.LogInfo($"PANEL_UTIL CHILD {b.name}");
        if (b.name.EndsWith("Toggle")) GUIManager.Instance.ApplyToogleStyle(b);
      });
    var image = editPanel.GetComponent<Image>();
    image.sprite = GUIManager.Instance.GetSprite("woodpanel_trophys");
    image.type = Image.Type.Sliced;
    image.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
    image.color = Color.white;
  }
}