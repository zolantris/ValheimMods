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

  private static Harmony Harmony;

  internal static void ApplyMetricIfAvailable()
  {
    string @namespace = "SentryUnityWrapper";
    string @pluginClass = "SentryUnityWrapperPlugin";
    if (ValheimRaftPlugin.Instance.EnableMetrics.Value &&
        Chainloader.PluginInfos.ContainsKey("zolantris.SentryUnityWrapper"))
    {
      if (Type.GetType($"{@namespace}.{@pluginClass}") != null)
      {
        SentryMetrics.ApplyMetrics();
      }
    }
  }

  internal static void Apply(string harmonyGuid)
  {
    Harmony = new Harmony(harmonyGuid);
    Harmony.PatchAll(typeof(Plantable_Patch));
    Harmony.PatchAll(typeof(Teleport_Patch));
    Harmony.PatchAll(typeof(ValheimRAFT_Patch));

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
      Harmony.PatchAll(typeof(PlanBuildPatch));
    }
  }
}