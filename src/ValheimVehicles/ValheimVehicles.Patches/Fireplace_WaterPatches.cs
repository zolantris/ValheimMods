using HarmonyLib;
using ValheimVehicles.Controllers;


namespace ValheimVehicles.Patches;

public static class Fireplace_WaterPatches
{
  [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.IsBurning))]
  [HarmonyPostfix]
  public static void IsBurning_Underwater(Fireplace __instance,
    ref bool __result)
  {
    if (__result == false && IsFireInDisplacedVehicle(__instance))
    {
      __result = true;
    }
  }

  public static bool IsFireInDisplacedVehicle(Fireplace __instance)
  {
    return VehiclePiecesController.IsWithin(__instance.transform,
      out _);
  }
}