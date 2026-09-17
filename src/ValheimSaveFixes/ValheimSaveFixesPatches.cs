using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Splatform;

namespace ValheimSaveFixes.Patches;

[HarmonyPatch]
internal static class SaveMountFixes
{
  private static bool ShouldMountCloudForCreation(bool forceLocal)
  {
    return !forceLocal && FileHelpers.CloudStorageSupportedAndEnabled;
  }

  [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewWorldDone))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnNewWorldDone_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCloudSupportedCheckWithForceLocal(
      instructions,
      nameof(FejdStartup.OnNewWorldDone));
  }

  [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnNewCharacterDone_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    return ReplaceCloudSupportedCheckWithForceLocal(
      instructions,
      nameof(FejdStartup.OnNewCharacterDone));
  }

  private static IEnumerable<CodeInstruction> ReplaceCloudSupportedCheckWithForceLocal(
    IEnumerable<CodeInstruction> instructions,
    string methodName)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        nameof(ShouldMountCloudForCreation));

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced && instruction.Calls(cloudSupportedGetter))
      {
        yield return new CodeInstruction(OpCodes.Ldarg_1);
        yield return new CodeInstruction(OpCodes.Call, replacement);

        replaced = true;
        continue;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        $"[ValheimSaveFixes] Failed to patch cloud mount check in {methodName}");
    }
  }

  private static bool ShouldMountCloudForWorldSave()
  {
    return FileHelpers.CloudStorageSupported &&
           ZNet.m_world != null &&
           ZNet.m_world.m_fileSource.IsCloud();
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorldThread))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> SaveWorldThread_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        nameof(ShouldMountCloudForWorldSave));

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced && instruction.Calls(cloudSupportedGetter))
      {
        yield return new CodeInstruction(OpCodes.Call, replacement);

        replaced = true;
        continue;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        "[ValheimSaveFixes] Failed to patch cloud mount check in ZNet.SaveWorldThread");
    }
  }

  private static bool ShouldMountCloudForManualSave()
  {
    return FileHelpers.CloudStorageSupported &&
           ZNet.m_world != null &&
           ZNet.m_world.m_fileSource.IsCloud();
  }

  [HarmonyPatch(typeof(Menu), nameof(Menu.OnManualSave))]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> OnManualSave_Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    var cloudSupportedGetter =
      AccessTools.PropertyGetter(
        typeof(FileHelpers),
        nameof(FileHelpers.CloudStorageSupported));

    var replacement =
      AccessTools.Method(
        typeof(SaveMountFixes),
        nameof(ShouldMountCloudForManualSave));

    var replaced = false;

    foreach (var instruction in instructions)
    {
      if (!replaced && instruction.Calls(cloudSupportedGetter))
      {
        yield return new CodeInstruction(OpCodes.Call, replacement);

        replaced = true;
        continue;
      }

      yield return instruction;
    }

    if (!replaced)
    {
      ZLog.LogError(
        "[ValheimSaveFixes] Failed to patch cloud mount check in Menu.OnManualSave");
    }
  }
}
