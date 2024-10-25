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

  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
  [HarmonyPostfix]
  public static void GameCameraUpdateCameraPatch(GameCamera __instance,
    Camera ___m_camera)
  {
    if (Player.m_localPlayer == null)
    {
      return;
    }

    CameraPositionY = ___m_camera.gameObject.transform.position.y;
    if (EnvironmentName != EnvMan.instance.GetCurrentEnvironment().m_name)
    {
      EnvironmentName = EnvMan.instance.GetCurrentEnvironment().m_name;
    }

    EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
    Color color = ((!EnvMan.IsNight())
      ? currentEnvironment.m_fogColorDay
      : currentEnvironment.m_fogColorNight);

    ChangeColorBrightness(color, 0);
    RenderSettings.fogColor = color;
    __instance.m_maxDistance = 4f;
    // int num2 = (((Plugin._IsDiving || Plugin._IsSwimming) && !Plugin._RestInWater && Helper.IsEnvAllowed()) ? 1 : 0);
    var isUnderWater = Character_Patch.IsUnderWaterInVehicle;
    __instance.m_minWaterDistance = isUnderWater ? -5000f : 0.3f;
  }
}