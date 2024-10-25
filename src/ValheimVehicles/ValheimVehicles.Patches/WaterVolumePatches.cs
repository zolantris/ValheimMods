using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
internal class WaterVolumePatch
{
  public static float WaterLevelCamera = 0f;

  [HarmonyPatch(typeof(WaterVolume), "UpdateMaterials")]
  [HarmonyPrefix]
  public static void WaterVolumeUpdatePatchWaterVolume(WaterVolume __instance,
    ref float[] ___m_normalizedDepth)

  {
    if ((bool)GameCamera.instance)
    {
      Vector3 position = GameCamera.instance.transform.position;
      WaterLevelCamera =
        __instance.GetWaterSurface(new Vector3(position.x, position.y,
          position.z));
    }

    if ((bool)Player.m_localPlayer)
    {
      Vector3 position2 = Player.m_localPlayer.transform.position;
      WaterLevelCamera =
        __instance.GetWaterSurface(new Vector3(position2.x, position2.y,
          position2.z));
    }

    if (GameCameraPatch.CameraPositionY < WaterLevelCamera)
    {
      if (!__instance.m_waterSurface.GetComponent<MeshRenderer>().transform
            .rotation.eulerAngles.y.Equals(180f))
      {
        __instance.m_waterSurface.transform.Rotate(180f, 0f, 0f);
        __instance.m_waterSurface.shadowCastingMode =
          ShadowCastingMode.TwoSided;
        if (__instance.m_forceDepth >= 0f)
        {
          __instance.m_waterSurface.material.SetFloatArray(
            Shader.PropertyToID("_depth"),
            new float[4]
            {
              __instance.m_forceDepth, __instance.m_forceDepth,
              __instance.m_forceDepth, __instance.m_forceDepth
            });
        }
        else
        {
          __instance.m_waterSurface.material.SetFloatArray(
            Shader.PropertyToID("_depth"), ___m_normalizedDepth);
        }

        __instance.m_waterSurface.material.SetFloat(
          Shader.PropertyToID("_UseGlobalWind"),
          __instance.m_useGlobalWind ? 1f : 0f);
      }

      Transform transform = __instance.m_waterSurface.transform;
      Vector3 position3 = transform.position;
      position3 = new Vector3(position3.x, WaterLevelCamera,
        position3.z);
      transform.position = position3;
    }
    else if (__instance.m_waterSurface.GetComponent<MeshRenderer>().transform
             .rotation.eulerAngles.y.Equals(180f))
    {
      __instance.m_waterSurface.transform.Rotate(-180f, 0f, 0f);
      if (__instance.m_forceDepth >= 0f)
      {
        __instance.m_waterSurface.material.SetFloatArray(
          Shader.PropertyToID("_depth"),
          new float[4]
          {
            __instance.m_forceDepth, __instance.m_forceDepth,
            __instance.m_forceDepth, __instance.m_forceDepth
          });
      }
      else
      {
        __instance.m_waterSurface.material.SetFloatArray(
          Shader.PropertyToID("_depth"), ___m_normalizedDepth);
      }

      Transform transform2 = __instance.m_waterSurface.transform;
      Vector3 position4 = transform2.position;
      position4 = new Vector3(position4.x, 30f, position4.z);
      transform2.position = position4;
      __instance.m_waterSurface.material.SetFloat(
        Shader.PropertyToID("_UseGlobalWind"),
        __instance.m_useGlobalWind ? 1f : 0f);
    }
  }
}