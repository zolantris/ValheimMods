using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;
using ValheimRAFT.Util;

namespace ValheimRAFT.Patches;

internal static class PatchController
{
  public static string PlanBuildGUID = "marcopogo.PlanBuild";

  private static Harmony? Harmony;

  internal static void Apply(string harmonyGuid)
  {
    Harmony = new Harmony(harmonyGuid);
    Harmony.PatchAll(typeof(Character_Patch));
    Harmony.PatchAll(typeof(CharacterAnimEvent_Patch));
    Harmony.PatchAll(typeof(Plantable_Patch));
    Harmony.PatchAll(typeof(Player_Patch));
    Harmony.PatchAll(typeof(Ship_Patch));
    Harmony.PatchAll(typeof(ShipControls_Patch));
    Harmony.PatchAll(typeof(Teleport_Patch));
    Harmony.PatchAll(typeof(WearNTear_Patch));
    Harmony.PatchAll(typeof(ZDO_Patch));
    Harmony.PatchAll(typeof(ZNetScene_Patch));
    Harmony.PatchAll(typeof(ZNetView_Patch));
    Harmony.PatchAll(typeof(Hud_Patch));
    Harmony.PatchAll(typeof(StartScene_Patch));
    /*
     * PlanBuild uses mmmHookgen so it cannot be detected with bepinex
     *
     * The patch flag must be enabled and the folder must be detected otherwise it will not apply to patch a mod that does not exist
     *
     * So it does not show up on Chainloader.PluginInfos.ContainsKey(PlanBuildGUID)
     */
    if (ValheimRaftPlugin.Instance.PatchPlanBuildPositionIssues.Value &&
        (Directory.Exists(Path.Combine(Paths.PluginPath, "MathiasDecrock-PlanBuild")) ||
         Directory.Exists(Path.Combine(Paths.PluginPath, "PlanBuild"))))
    {
      Logger.LogInfo("Applying PlanBuild Patch");
      Harmony.PatchAll(typeof(PlanBuild_Patch));
    }
  }
}