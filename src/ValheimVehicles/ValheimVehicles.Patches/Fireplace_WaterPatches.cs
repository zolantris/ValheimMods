using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Patches;

public static class Fireplace_WaterPatches
{
  [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.IsBurning))]
  public static IEnumerable<CodeInstruction> Transpile(
    IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);

    // Find the index of the "IsUnderWater" call
    int index = codes.FindIndex(code => code.opcode == OpCodes.Call &&
                                        code.operand is MethodInfo method &&
                                        method.Name ==
                                        nameof(Floating.IsUnderWater));

    if (index >= 0)
    {
      // Insert our own check before the original call
      // Assuming you have a method "CustomUnderWaterCheck" that returns a boolean
      var customCheckMethod = AccessTools.Method(typeof(Fireplace_WaterPatches),
        nameof(Fireplace_WaterPatches.IsFireInDisplacedVehicle));
      codes.Insert(index, new CodeInstruction(OpCodes.Call, customCheckMethod));
      codes.Insert(index + 1,
        new CodeInstruction(OpCodes
          .Ldc_I4_0)); // Assuming we want to return false if custom check fails
      codes.Insert(index + 2,
        new CodeInstruction(OpCodes.Bne_Un_S,
          codes[index + 3]
            .labels[0])); // Jump to the original call if custom check passes
    }

    return codes;
  }

  public static bool IsFireInDisplacedVehicle(Fireplace __instance)
  {
    if (VehiclePiecesController.IsWithin(__instance.transform,
          out var piecesController))
    {
      return true;
    }

    return false;
  }
}