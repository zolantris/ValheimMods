using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Hud_Patch
{
  [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateShipHud))]
  [HarmonyPrefix]
  public static bool UpdateShipHud(Hud __instance, Player player, float dt)
  {
    VehicleShip? controlledShip = Player_Patch.HandleGetControlledShip(player) as VehicleShip;
    if (controlledShip == null)
    {
      __instance.m_shipHudRoot.gameObject.SetActive(value: false);
      return false;
    }

    ValheimBaseGameShip.Speed speedSetting = controlledShip.GetSpeedSetting();
    float rudder = controlledShip.GetRudder();
    float rudderValue = controlledShip.GetRudderValue();
    __instance.m_shipHudRoot.SetActive(value: true);
    __instance.m_rudderSlow.SetActive(speedSetting == ValheimBaseGameShip.Speed.Slow);
    __instance.m_rudderForward.SetActive(speedSetting == ValheimBaseGameShip.Speed.Half);
    __instance.m_rudderFastForward.SetActive(speedSetting == ValheimBaseGameShip.Speed.Full);
    __instance.m_rudderBackward.SetActive(speedSetting == ValheimBaseGameShip.Speed.Back);
    __instance.m_rudderLeft.SetActive(value: false);
    __instance.m_rudderRight.SetActive(value: false);
    __instance.m_fullSail.SetActive(speedSetting == ValheimBaseGameShip.Speed.Full);
    __instance.m_halfSail.SetActive(speedSetting == ValheimBaseGameShip.Speed.Half);
    GameObject rudder2 = __instance.m_rudder;
    int active;
    switch (speedSetting)
    {
      case ValheimBaseGameShip.Speed.Stop:
        active = ((Mathf.Abs(rudderValue) > 0.2f) ? 1 : 0);
        break;
      default:
        active = 0;
        break;
      case ValheimBaseGameShip.Speed.Back:
      case ValheimBaseGameShip.Speed.Slow:
        active = 1;
        break;
    }

    rudder2.SetActive((byte)active != 0);
    if ((rudder > 0f && rudderValue < 1f) || (rudder < 0f && rudderValue > -1f))
    {
      __instance.m_shipRudderIcon.transform.Rotate(new Vector3(0f, 0f,
        200f * (0f - rudder) * dt));
    }

    if (Mathf.Abs(rudderValue) < 0.02f)
    {
      __instance.m_shipRudderIndicator.gameObject.SetActive(value: false);
    }
    else
    {
      __instance.m_shipRudderIndicator.gameObject.SetActive(value: true);
      if (rudderValue > 0f)
      {
        __instance.m_shipRudderIndicator.fillClockwise = true;
        __instance.m_shipRudderIndicator.fillAmount = rudderValue * 0.25f;
      }
      else
      {
        __instance.m_shipRudderIndicator.fillClockwise = false;
        __instance.m_shipRudderIndicator.fillAmount = (0f - rudderValue) * 0.25f;
      }
    }

    float shipYawAngle = controlledShip.GetShipYawAngle();
    __instance.m_shipWindIndicatorRoot.localRotation = Quaternion.Euler(0f, 0f, shipYawAngle);
    float windAngle = controlledShip.GetWindAngle();
    __instance.m_shipWindIconRoot.localRotation = Quaternion.Euler(0f, 0f, windAngle);
    float windAngleFactor = controlledShip.GetWindAngleFactor();
    __instance.m_shipWindIcon.color =
      Color.Lerp(new Color(0.2f, 0.2f, 0.2f, 1f), Color.white, windAngleFactor);
    Camera mainCamera = Utils.GetMainCamera();
    if (!(mainCamera == null))
    {
      __instance.m_shipControlsRoot.transform.position =
        mainCamera.WorldToScreenPointScaled(controlledShip.m_controlGuiPos.position);
    }

    return false;
  }
}