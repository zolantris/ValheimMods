using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.Patches;

public class VehicleMovementPatches
{
  [HarmonyPatch(typeof(MonoUpdaters), "FixedUpdate")]
  [HarmonyPostfix]
  public static void VehicleMovementControllerFixedUpdate(MonoUpdaters __instance)
  {
    __instance.m_update.CustomFixedUpdate(VehicleMovementController.Instances,
      "MonoUpdaters.FixedUpdate.Ship",
      Time.fixedDeltaTime);
  }
}