using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Patches;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObjects))]
public class ZNetScene_CreateObjects_Patch
{
  public static int CustomMaxCreatedPerFrame = 100;
  public static int CustomLoadingScreenMaxCreatedPerFrame = 100;

  private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var replacedNormal = false;
    var replacedLoading = false;

    foreach (var instruction in instructions)
    {
      // Defensive: Only replace the *first* ldc.i4.s 10 and *first* ldc.i4.s 100
      if (!replacedNormal && instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 10)
      {
        replacedNormal = true;
        yield return new CodeInstruction(OpCodes.Ldsfld,
          typeof(ZNetScene_CreateObjects_Patch).GetField(nameof(CustomMaxCreatedPerFrame)));
        continue;
      }
      if (!replacedLoading && instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 100)
      {
        replacedLoading = true;
        yield return new CodeInstruction(OpCodes.Ldsfld,
          typeof(ZNetScene_CreateObjects_Patch).GetField(nameof(CustomLoadingScreenMaxCreatedPerFrame)));
        continue;
      }
      yield return instruction;
    }

    if (!replacedNormal || !replacedLoading)
      LoggerProvider.LogWarning("[ZNetScenePatch] Could not patch all expected instructions (possibly another mod has already modified them).");
  }
}