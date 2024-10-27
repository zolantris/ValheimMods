using HarmonyLib;
using UnityEngine;
using ValheimRAFT.Patches;

namespace ValheimVehicles.Patches;

/// <summary>
/// from vikings do swim might get things working 
/// </summary>
[HarmonyPatch]
internal class GameCameraPatch
{
  public static string EnvironmentName = "";

  private static Color ChangeColorBrightness(Color color,
    float correctionFactor)
  {
    float r = color.r;
    float g = color.g;
    float b = color.b;
    if (!(correctionFactor < 0f))
    {
      return new Color(r, g, b, color.a);
    }

    correctionFactor *= -1f;
    r -= r * correctionFactor;
    if (r < 0f)
    {
      r = 0f;
    }

    g -= g * correctionFactor;
    if (g < 0f)
    {
      g = 0f;
    }

    b -= b * correctionFactor;
    if (b < 0f)
    {
      b = 0f;
    }

    return new Color(r, g, b, color.a);
  }

  public static float CameraPositionY = 0f;

  public static float prevFogDensity;
  public static bool prevFog;
  public static Color prevFogColor;
  public static bool hasPrevValues;
  public static bool MustRestorePrevValues;


  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
  [HarmonyPostfix]
  public static void GameCameraUpdateCameraPatch(GameCamera __instance,
    Camera ___m_camera)
  {
    if (Player.m_localPlayer == null)
    {
      return;
    }

    if (!WaterVolumePatch.IsCameraAboveWater && MustRestorePrevValues)
    {
      if (!hasPrevValues)
      {
        prevFogDensity = RenderSettings.fogDensity;
        prevFog = RenderSettings.fog;
        prevFogColor = RenderSettings.fogColor;
        hasPrevValues = true;
      }

      if (hasPrevValues)
      {
        RenderSettings.fogDensity = prevFogDensity;
        RenderSettings.fog = prevFog;
        RenderSettings.fogColor = prevFogColor;

        prevFogDensity = RenderSettings.fogDensity;
        prevFog = RenderSettings.fog;
        prevFogColor = RenderSettings.fogColor;

        hasPrevValues = false;
      }
    }

    CameraPositionY = ___m_camera.gameObject.transform.position.y;


    if (WaterVolumePatch.IsCameraAboveWater)
    {
      EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
      Color color = ((!EnvMan.IsNight())
        ? currentEnvironment.m_fogColorDay
        : currentEnvironment.m_fogColorNight);

      MustRestorePrevValues = true;
      if (EnvironmentName != EnvMan.instance.GetCurrentEnvironment().m_name)
      {
        EnvironmentName = EnvMan.instance.GetCurrentEnvironment().m_name;
      }

      ChangeColorBrightness(color, 0.1f);
      RenderSettings.fogColor = color;
      RenderSettings.fogDensity = 0.05f;
      RenderSettings.fog = true;
      __instance.m_maxDistance = 20f;
    }


    // int num2 = (((Plugin._IsDiving || Plugin._IsSwimming) && !Plugin._RestInWater && Helper.IsEnvAllowed()) ? 1 : 0);
    // should always be viewable
    __instance.m_minWaterDistance = -5000f;
    // __instance.m_minWaterDistance =
    //   Character_Patch.IsUnderWaterInVehicle ? -5000f : 0.3f;
  }
}