using HarmonyLib;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Patches;

public class GameCamera_CullingPatches
{
  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
  [HarmonyPostfix]
  public static void InjectOcclusionComponent(GameCamera __instance)
  {
    VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
  }
}