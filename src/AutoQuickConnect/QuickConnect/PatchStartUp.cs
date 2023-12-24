using HarmonyLib;

namespace QuickConnect;

public class PatchStartUp
{
	[HarmonyPatch(typeof(FejdStartup), "OnSelelectCharacterBack")]
	internal class PatchCharacterBack
	{
		private static void Postfix()
		{
			if ((bool)QuickConnectUI.instance) QuickConnectUI.instance.AbortConnect();
		}
	}

	// [HarmonyTranspiler]
	// [HarmonyPatch(typeof(FejdStartup), "Start")]
	// public static IEnumerable<CodeInstruction> AvoidLoadingAllPlayersInStart(
	// 	IEnumerable<CodeInstruction> instructions)
	// {
	// 	instructions = instructions.MethodReplacer(
	// 		AccessTools.Method(typeof(FejdStartup), "SetSelectedProfile"),
	// 		AccessTools.Method(typeof(FejdStartup_SkipMenu_Patches), "SetSelectedProfile_Stub"));
	// 	instructions = instructions.MethodReplacer(
	// 		AccessTools.Method(typeof(PlayerProfile), "GetAllPlayerProfiles"),
	// 		AccessTools.Method(typeof(FejdStartup_SkipMenu_Patches), "GetAllPlayerProfiles_Stub"));
	// 	instructions = instructions.MethodReplacer(
	// 		AccessTools.Method(typeof(FejdStartup), "UpdateCharacterList"),
	// 		AccessTools.Method(typeof(FejdStartup_SkipMenu_Patches), "UpdateCharacterList_Stub"));
	// 	return instructions;
	// }

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(FejdStartup), "Start")]
	// [HarmonyPriority(800)]
	// public static void QuickStart(FejdStartup __instance)
	// {
	// 	loadedChar = null;
	// 	loadedWorld = null;
	// 	logger.LogDebug("Hiding everything and muting music...");
	// 	AccessTools.DeclaredMethod(typeof(FejdStartup), "HideAll").Invoke(__instance, null);
	// 	MusicMan.instance.Reset();
	// 	if (CharacterSpecified)
	// 	{
	// 		loadedChar = LoadChar();
	// 		if (loadedChar == null && StrictMode.Value) __instance.OnAbort();
	// 	}
	//
	// 	if (ServerSpecified)
	// 	{
	// 		logger.LogDebug("Queue server join: " + StartServer.Value);
	// 		ZSteamMatchmaking.instance.QueueServerJoin(StartServer.Value);
	// 	}
	// 	else if (WorldSpecified)
	// 	{
	// 		loadedWorld = LoadWorld();
	// 		if (loadedWorld == null && StrictMode.Value) __instance.OnAbort();
	// 	}
	//
	// 	if (loadedChar != null && loadedWorld != null)
	// 	{
	// 		AccessTools.Method(typeof(FejdStartup), "TransitionToMainScene").Invoke(__instance, null);
	// 		return;
	// 	}
	//
	// 	MusicMan.instance.TriggerMusic("menu");
	// 	AccessTools.DeclaredMethod(typeof(FejdStartup), "UpdateCharacterList").Invoke(__instance, null);
	// 	if (loadedChar != null)
	// 		AccessTools.DeclaredMethod(typeof(FejdStartup), "SetSelectedProfile")
	// 			.Invoke(__instance, new object[1] { loadedChar.GetFilename() });
	// 	if (loadedChar == null)
	// 		AccessTools.DeclaredMethod(typeof(FejdStartup), "ShowCharacterSelection")
	// 			.Invoke(__instance, null);
	// 	else if (!ServerSpecified && loadedWorld == null)
	// 		AccessTools.DeclaredMethod(typeof(FejdStartup), "ShowStartGame").Invoke(__instance, null);
  //  }
}