using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.UI;

public class PanelUtil
{
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