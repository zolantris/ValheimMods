using HarmonyLib;

namespace QuickConnect;

[HarmonyPatch(typeof(FejdStartup), "OnSelelectCharacterBack")]
internal class PatchCharacterBack
{
	private static void Postfix()
	{
		if ((bool)QuickConnectUI.instance) QuickConnectUI.instance.AbortConnect();
	}
}