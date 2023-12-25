using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;
using PlanBuild.ModCompat;
using PlanBuild.Plans;
using ValheimRAFT.Patches;

namespace ValheimRAFT.Patches;

internal class PatchController
{
  public static string PlanBuildGUID = "marcopogo.PlanBuild";

  private static Harmony Harmony;

  internal static void Apply(string harmonyGuid)
  {
    Harmony = new Harmony(harmonyGuid);
    Harmony.PatchAll(typeof(Plantable_Patch));
    Harmony.PatchAll(typeof(Teleport_Patch));
    Harmony.PatchAll(typeof(ValheimRAFT_Patch));

    /*
     * PlanBuild uses mmmHookgen so it cannot be detected with bepinex
     *
     * So it does not show up on Chainloader.PluginInfos.ContainsKey(PlanBuildGUID)
     */
    if (
      Directory.Exists(Path.Combine(Paths.PluginPath, "MathiasDecrock-PlanBuild")) ||
      Directory.Exists(Path.Combine(Paths.PluginPath, "PlanBuild")))
    {
      Logger.LogInfo("Applying PlanBuild Patch");
      Harmony.PatchAll(typeof(PlanBuildPatch));
    }
  }
}