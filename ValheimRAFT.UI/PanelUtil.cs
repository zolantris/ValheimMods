using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimRAFT.UI;

public class PanelUtil
{
	public static void ApplyPanelStyle(GameObject editPanel)
	{
		Array.ForEach(editPanel.GetComponentsInChildren<Button>(includeInactive: true), delegate(Button b)
		{
			if (b.name.EndsWith("Button"))
			{
				GUIManager.Instance.ApplyButtonStyle(b);
			}
		});
		Array.ForEach(editPanel.GetComponentsInChildren<InputField>(includeInactive: true), delegate(InputField b)
		{
			GUIManager.Instance.ApplyInputFieldStyle(b, 16);
		});
		Array.ForEach(editPanel.GetComponentsInChildren<Text>(includeInactive: true), delegate(Text b)
		{
			b.text = Localization.instance.Localize(b.text);
			if (b.name.EndsWith("Label"))
			{
				GUIManager.Instance.ApplyTextStyle(b);
			}
		});
		Array.ForEach(editPanel.GetComponentsInChildren<Toggle>(includeInactive: true), delegate(Toggle b)
		{
			if (b.name.EndsWith("Toggle"))
			{
				GUIManager.Instance.ApplyToogleStyle(b);
			}
		});
		Image image = editPanel.GetComponent<Image>();
		image.sprite = GUIManager.Instance.GetSprite("woodpanel_trophys");
		image.type = Image.Type.Sliced;
		image.material = PrefabManager.Cache.GetPrefab<Material>("litpanel");
		image.color = Color.white;
	}
}
