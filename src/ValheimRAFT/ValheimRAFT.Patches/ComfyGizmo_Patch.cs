using ComfyGizmo;
using HarmonyLib;
using UnityEngine;

namespace ValheimRAFT.Patches;

public class ComfyGizmo_Patch
{
  [HarmonyPatch(typeof(RotationManager), "GetRotation")]
  [HarmonyPostfix]
  public static void ValheimVehicle_GetRotation(Quaternion __result)
  {
    var x = __result.eulerAngles.x;
    var y = __result.eulerAngles.y;
    var z = __result.eulerAngles.z;
    __result =
      ValheimVehicles.Helpers.VehicleRotionHelpers.RelativeEulerFromVector(__result.eulerAngles);
  }
}