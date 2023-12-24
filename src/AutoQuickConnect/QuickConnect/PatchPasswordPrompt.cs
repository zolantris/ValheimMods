using System.Reflection;
using HarmonyLib;

namespace QuickConnect;

[HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
internal class PatchPasswordPrompt
{
	private static bool Prefix(ZNet __instance, ZRpc rpc, bool needPassword, string serverPasswordSalt)
	{
		string text = QuickConnectUI.instance.CurrentPass();
		if (text != null)
		{
			if (needPassword)
			{
				Mod.Log.LogInfo("Authenticating with saved password...");
				__instance.m_connectingDialog.gameObject.SetActive(value: false);
				FieldInfo field = typeof(ZNet).GetField("m_serverPasswordSalt", BindingFlags.Static | BindingFlags.NonPublic);
				field.SetValue(null, serverPasswordSalt);
				MethodInfo method = typeof(ZNet).GetMethod("SendPeerInfo", BindingFlags.Instance | BindingFlags.NonPublic);
				method.Invoke(__instance, new object[2] { rpc, text });
				return false;
			}
			Mod.Log.LogInfo("Server didn't want password?");
		}
		return true;
	}
}
