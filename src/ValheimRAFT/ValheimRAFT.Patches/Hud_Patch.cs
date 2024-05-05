using HarmonyLib;
using Jotunn.Extensions;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Hud_Patch
{
  public static CircleLine ActiveWindCircle;
  public static CircleLine InactiveWindCircle;
  public static GameObject WindCircleComponent;
  public static Image WindIndicatorImageInstance;

  [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
  [HarmonyPostfix]
  public static void Hud_Awake(Hud __instance)
  {
    VehicleShipHudPatch(__instance);
  }

  public static void DisableVanillaWindIndicator(GameObject windIndicatorCircle)
  {
    WindIndicatorImageInstance = windIndicatorCircle.GetComponent<Image>();
    if (WindIndicatorImageInstance)
    {
      WindIndicatorImageInstance.enabled = false;
    }
  }

  public static void CreateCustomWindIndicator(GameObject windIndicatorCircle)
  {
    windIndicatorCircle.AddComponent<CircleWindIndicator>();
  }

  public static void VehicleShipHudPatch(Hud hud)
  {
    // fire 3 finds b/c later on these objects will have additional items added to them
    var shipHud = hud.transform?.FindDeepChild("ShipHud");
    var windIndicator = shipHud?.Find("WindIndicator");

    var windIndicatorCircle = windIndicator?.Find("Circle");

    if (windIndicatorCircle?.gameObject)
    {
      DisableVanillaWindIndicator(windIndicatorCircle.gameObject);
      CreateCustomWindIndicator(windIndicator.gameObject);
    }
  }

  public static void ApplyVehicleHudPatchGlobally()
  {
    var allHudObjects = Resources.FindObjectsOfTypeAll<Hud>();

    if (allHudObjects.Length > 1)
    {
      Logger.LogWarning(
        "Multiple Huds detected, ValheimRaft was designed to support a single Hud interface, please consider submitting a bug if there are problems with vehicle hud.");
    }

    foreach (var hud in allHudObjects)
    {
      VehicleShipHudPatch(hud);
    }
  }

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