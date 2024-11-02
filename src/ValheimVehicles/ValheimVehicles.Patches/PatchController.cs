using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Jotunn;

namespace ValheimVehicles.Patches;

internal class PatchController
{
  private static Harmony Harmony;

  internal static void Apply(string harmonyGuid)
  {
    Harmony = new Harmony(harmonyGuid);

    Harmony.PatchAll(typeof(BaseGamePatches));

    // Other patches
  }
}