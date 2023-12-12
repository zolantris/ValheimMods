// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.UI.PanelUtil
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRAFT.UI
{
  public class PanelUtil
  {
    public static void ApplyPanelStyle(GameObject editPanel)
    {
      Array.ForEach<Button>(editPanel.GetComponentsInChildren<Button>(true), (Action<Button>) (b =>
      {
        if (!((Object) b).name.EndsWith("Button"))
          return;
        GUIManager.Instance.ApplyButtonStyle(b, 16);
      }));
      Array.ForEach<InputField>(editPanel.GetComponentsInChildren<InputField>(true), (Action<InputField>) (b => GUIManager.Instance.ApplyInputFieldStyle(b, 16)));
      Array.ForEach<Text>(editPanel.GetComponentsInChildren<Text>(true), (Action<Text>) (b =>
      {
        b.text = Localization.instance.Localize(b.text);
        if (!((Object) b).name.EndsWith("Label"))
          return;
        GUIManager.Instance.ApplyTextStyle(b, 16);
      }));
      Array.ForEach<Toggle>(editPanel.GetComponentsInChildren<Toggle>(true), (Action<Toggle>) (b =>
      {
        if (!((Object) b).name.EndsWith("Toggle"))
          return;
        GUIManager.Instance.ApplyToogleStyle(b);
      }));
      Image component = editPanel.GetComponent<Image>();
      component.sprite = GUIManager.Instance.GetSprite("woodpanel_trophys");
      component.type = (Image.Type) 1;
      ((Graphic) component).material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
      ((Graphic) component).color = Color.white;
    }
  }
}
