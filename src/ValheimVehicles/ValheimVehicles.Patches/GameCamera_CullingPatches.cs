using HarmonyLib;
using ValheimVehicles.Config;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Patches;

public class GameCamera_CullingPatches
{
  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
  [HarmonyPostfix]
  public static void InjectOcclusionComponent(GameCamera __instance)
  {
    if (CameraConfig.CameraOcclusionEnabled.Value != true) return;
    VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
  }
}