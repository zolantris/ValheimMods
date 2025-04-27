using HarmonyLib;
using ValheimVehicles.Config;
using ValheimVehicles.Components;

namespace ValheimVehicles.Patches;

public class GameCamera_CullingPatches
{
  public const float minimumMaxDistance = 6f;
  public static float originalMaxDistance = minimumMaxDistance;
  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
  [HarmonyPostfix]
  public static void InjectOcclusionComponent(GameCamera __instance)
  {
    if (CameraConfig.CameraZoomOverridesEnabled.Value)
    {
      originalMaxDistance = __instance.m_maxDistance;
    }

    if (CameraConfig.CameraOcclusionEnabled.Value)
    {
      VehicleCameraCullingComponent.AddOrRemoveCameraCulling();
    }
  }
}