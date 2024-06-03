using HarmonyLib;
using UnityEngine;

namespace ValheimRAFT.Patches;

public class ComfyGizmo_Patch
{
  [HarmonyPatch(typeof(ComfyGizmo.RotationManager), "GetRotation")]
  [HarmonyPostfix]
  public static void ValheimVehicle_GetRotation(Quaternion __result)
  {
    __result =
      ValheimVehicles.Helpers.VehicleRotionHelpers.RelativeEulerFromVector(__result.eulerAngles);
  }

  public static float GetNearestSnapRotation(float eulerYRotation)
  {
    var degreesPerDivison = ComfyGizmo.RotationManager.GetActiveRotator().GetAngle();
    var nearestDivision = Mathf.RoundToInt(eulerYRotation / degreesPerDivison);
    var nearestDegree = nearestDivision * degreesPerDivison;
    return nearestDegree;
  }
}