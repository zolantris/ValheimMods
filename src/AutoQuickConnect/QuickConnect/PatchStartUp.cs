using System.Collections.Generic;
using HarmonyLib;
using Jotunn;

namespace QuickConnect;

public class PatchStartUp
{
  private static PlayerProfile loadedChar;
  static string loadedWorld;

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
  //   IEnumerable<CodeInstruction> instructions)
  // {
  //   instructions = instructions.MethodReplacer(
  //     // AccessTools.Method(typeof(FejdStartup), "SetSelectedProfile"),
  //     // AccessTools.Method(typeof(), "SetSelectedProfile_Stub"));
  //     // instructions = instructions.MethodReplacer(
  //     // AccessTools.Method(typeof(PlayerProfile), "GetAllPlayerProfiles"),
  //     // AccessTools.Method(typeof(FejdStartup_SkipMenu_Patches), "GetAllPlayerProfiles_Stub"));
  //     instructions = instructions.MethodReplacer(
  //       AccessTools.Method(typeof(FejdStartup), "UpdateCharacterList"), 
  //   //   AccessTools.Method(typeof(FejdStartup_SkipMenu_Patches), "UpdateCharacterList_Stub"));
  //   // return instructions;
  // }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(FejdStartup), "Start")]
  [HarmonyPriority(800)]
  public static void QuickStart(FejdStartup __instance)
  {
    loadedChar = null;
    loadedWorld = null;
    // Logger.LogDebug("Hiding everything and muting music...");
    // AccessTools.DeclaredMethod(typeof(FejdStartup), "HideAll").Invoke(__instance, null);
    // MusicMan.instance.Reset();
    if (loadedChar == null)
    {
      loadedChar = QuickConnectUI.LoadChar();
      ZLog.Log($"CHARACTER LOADED NAME {loadedChar?.m_playerName}");
      if (loadedChar == null) __instance.OnAbort();
    }

    ZLog.Log($"CHARACTER LOADED NAME {loadedChar?.m_playerName}");

    // MusicMan.instance.TriggerMusic("menu");
    // AccessTools.DeclaredMethod(typeof(FejdStartup), "UpdateCharacterList").Invoke(__instance, null);
    // if (loadedChar != null)
    //   AccessTools.DeclaredMethod(typeof(FejdStartup), "SetSelectedProfile")
    //     .Invoke(__instance, new object[1] { loadedChar.GetFilename() });
    // if (loadedChar == null)
    //   AccessTools.DeclaredMethod(typeof(FejdStartup), "ShowCharacterSelection")
    //     .Invoke(__instance, null);
    QuickConnectUI.instance.DoConnect(Servers.entries[0]);
  }
}