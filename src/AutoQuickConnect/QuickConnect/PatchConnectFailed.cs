using HarmonyLib;

namespace QuickConnect;

[HarmonyPatch(typeof(ZSteamMatchmaking), "OnJoinServerFailed")]
internal class PatchConnectFailed
{
	private static void Postfix()
	{
		if ((bool)QuickConnectUI.instance)
		{
			QuickConnectUI.instance.JoinServerFailed();
		}
	}
}
