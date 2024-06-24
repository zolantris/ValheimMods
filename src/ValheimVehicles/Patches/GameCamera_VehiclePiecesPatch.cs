using HarmonyLib;
using UnityEngine;

namespace ValheimVehicles.Patches;

public class GameCamera_VehiclePiecesPatch
{
  [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.GetCameraPosition))]
  [HarmonyPrefix]
  public static bool GetCameraPosition(GameCamera __instance, float ___dt, out Vector3 ___pos,
    out Quaternion ___rot)
  {
    Player localPlayer = Player.m_localPlayer;
    if (localPlayer == null)
    {
      ___pos = __instance.transform.position;
      ___rot = __instance.transform.rotation;
      return false;
    }

    Vector3 vector = __instance.GetOffsetedEyePos();
    float num = __instance.m_distance;
    if (localPlayer.InIntro())
    {
      vector = localPlayer.transform.position;
      num = __instance.m_flyingDistance;
    }

    Vector3 vector2 = -localPlayer.m_eye.transform.forward;
    if (__instance.m_smoothYTilt && !localPlayer.InIntro())
    {
      num = Mathf.Lerp(num, 1.5f, Utils.SmoothStep(0f, -0.5f, vector2.y));
    }

    Vector3 end = vector + vector2 * num;
    __instance.CollideRay2(localPlayer.m_eye.position, vector, ref end);
    __instance.UpdateNearClipping(vector, end, ___dt);
    float liquidLevel = Floating.GetLiquidLevel(end);
    if (end.y < liquidLevel + __instance.m_minWaterDistance)
    {
      end.y = liquidLevel + __instance.m_minWaterDistance;
      __instance.m_waterClipping = true;
    }
    else
    {
      __instance.m_waterClipping = false;
    }

    ___pos = end;
    ___rot = localPlayer.m_eye.transform.rotation;
    if (__instance.m_shipCameraTilt)
    {
      __instance.ApplyCameraTilt(localPlayer, ___dt, ref ___rot);
    }

    return false;
  }
}