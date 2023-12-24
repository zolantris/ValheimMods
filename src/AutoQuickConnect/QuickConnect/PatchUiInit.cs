using HarmonyLib;
using UnityEngine;

namespace QuickConnect;

[HarmonyPatch(typeof(FejdStartup), "SetupGui")]
internal class PatchUiInit
{
	private static void Postfix()
	{
		if (!QuickConnectUI.instance)
		{
			Servers.Init();
			GameObject gameObject = new GameObject("QuickConnect");
			QuickConnectUI.instance = gameObject.AddComponent<QuickConnectUI>();
		}
	}
}
