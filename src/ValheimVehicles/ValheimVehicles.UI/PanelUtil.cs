using System;
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
  public static GameObject CreateDraggableHideShowPanel(Unity2dViewStyles panelStyles, Unity2dViewStyles buttonStyles, string hideText, string showText,
    string buttonTitle, Action<Text>? onToggle = null, bool isActive = false)
  {

    var panel = DefaultControls.CreatePanel(
      GUIManager.Instance.ValheimControlResources
    );
    panel.name = "ValheimVehicles_commandsWindow";
    var dragWindowExtension = panel.AddComponent<DragWindowCntrlExtension>();
    panel.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
    panel.GetComponent<Image>().pixelsPerUnitMultiplier = 1f;
    var panelTransform = (RectTransform)panel.transform;
    panelTransform.anchoredPosition = new Vector2(VehicleDebugConfig.CommandsWindowPosX.Value, VehicleDebugConfig.CommandsWindowPosY.Value);
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

    dragWindowExtension.OnDragCalled += () =>
    {
      var anchoredPosition = panelTransform.anchoredPosition;
      VehicleDebugConfig.CommandsWindowPosX.Value = anchoredPosition.x;
      VehicleDebugConfig.CommandsWindowPosY.Value = anchoredPosition.y;
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