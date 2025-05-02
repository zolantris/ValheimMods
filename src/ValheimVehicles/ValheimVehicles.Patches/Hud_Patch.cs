using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Jotunn.Extensions;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.Compat;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class Hud_Patch
{
  // public static CircleLine ActiveWindCircle;
  // public static CircleLine InactiveWindCircle;
  // public static GameObject WindCircleComponent;
  public static Image WindIndicatorImageInstance;
  public static GameObject AnchorHud;

  /// <summary>
  /// The LineRender Approach is not working, so this patch is disabled in 2.0.0
  /// </summary>
  /// <param name="windIndicatorCircle"></param>
  [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
  [HarmonyPostfix]
  public static void Hud_Awake(Hud __instance)
  {
    VehicleShipHudPatch(__instance);
  }

  // public static void DisableVanillaWindIndicator(GameObject windIndicatorCircle)
  // {
  //   WindIndicatorImageInstance = windIndicatorCircle.GetComponent<Image>();
  //   if (WindIndicatorImageInstance)
  //   {
  //     WindIndicatorImageInstance.enabled = false;
  //   }
  // }
  //
  // public static void CreateCustomWindIndicator(GameObject windIndicatorCircle)
  // {
  //   windIndicatorCircle.AddComponent<CircleWindIndicator>();
  // }

  public static void AddAnchorGameObject(Transform shipPowerHud,
    Transform rudderIndicator)
  {
    AnchorHud =
      Object.Instantiate(LoadValheimVehicleAssets.HudAnchor, shipPowerHud);
    AnchorHud.name = PrefabNames.VehicleHudAnchorIndicator;
    AnchorHud.SetActive(false);

    if (rudderIndicator)
      AnchorHud.transform.localPosition = rudderIndicator.localPosition;
  }

  private static void ToggleAnchorHud(VehicleManager? vehicleShip)
  {
    if (vehicleShip == null || vehicleShip.MovementController == null) return;
    var isAnchored = vehicleShip.MovementController.isAnchored;
    AnchorHud.SetActive(isAnchored);
  }

  /**
    * Will be used for Wind speed after the ship keel is added in future updates to allow upwind sailing
    */
  // private static void UpdateShipWindIndicator()
  // {
  //   // var windIndicator = shipHud?.Find("WindIndicator");
  //   // var windIndicatorCircle = windIndicator?.Find("Circle");
  // }
  private static void VehicleShipHudPatch(Hud hud)
  {
    // fire 3 finds b/c later on these objects will have additional items added to them
    var shipHud = hud.transform?.FindDeepChild("ShipHud");
    var shipPowerIcon = shipHud?.Find("PowerIcon");

    var rudder = shipHud.Find("Rudder");

    if (shipPowerIcon) AddAnchorGameObject(shipPowerIcon, rudder);
  }


  /// <summary>
  /// BaseGameLogic that updates the hud only for vehicle ships
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="player"></param>
  /// <param name="dt"></param>
  /// <param name="vehicleInterface"></param>
  public static void UpdateShipHudV2(Hud __instance, Player player, float dt,
    VehicleControllersCompat vehicleInterface)
  {
    var speedSetting = vehicleInterface.GetSpeedSetting();
    var rudder = vehicleInterface.GetRudder();
    var rudderValue = vehicleInterface.GetRudderValue();
    __instance.m_shipHudRoot.SetActive(true);
    __instance.m_rudderSlow.SetActive(speedSetting == Ship.Speed.Slow);
    __instance.m_rudderForward.SetActive(speedSetting == Ship.Speed.Half);
    __instance.m_rudderFastForward.SetActive(speedSetting == Ship.Speed.Full);
    __instance.m_rudderBackward.SetActive(speedSetting == Ship.Speed.Back);
    __instance.m_rudderLeft.SetActive(false);
    __instance.m_rudderRight.SetActive(false);
    __instance.m_fullSail.SetActive(speedSetting == Ship.Speed.Full);
    __instance.m_halfSail.SetActive(speedSetting == Ship.Speed.Half);
    var rudder2 = __instance.m_rudder;
    int active;
    switch (speedSetting)
    {
      case Ship.Speed.Stop:
        active = Mathf.Abs(rudderValue) > 0.2f ? 1 : 0;
        break;
      default:
        active = 0;
        break;
      case Ship.Speed.Back:
      case Ship.Speed.Slow:
        active = 1;
        break;
    }

    rudder2.SetActive((byte)active != 0);
    if (rudder > 0f && rudderValue < 1f || rudder < 0f && rudderValue > -1f)
      __instance.m_shipRudderIcon.transform.Rotate(new Vector3(0f, 0f,
        200f * (0f - rudder) * dt));

    if (Mathf.Abs(rudderValue) < 0.02f)
    {
      __instance.m_shipRudderIndicator.gameObject.SetActive(false);
    }
    else
    {
      __instance.m_shipRudderIndicator.gameObject.SetActive(true);
      if (rudderValue > 0f)
      {
        __instance.m_shipRudderIndicator.fillClockwise = true;
        __instance.m_shipRudderIndicator.fillAmount = rudderValue * 0.25f;
      }
      else
      {
        __instance.m_shipRudderIndicator.fillClockwise = false;
        __instance.m_shipRudderIndicator.fillAmount =
          (0f - rudderValue) * 0.25f;
      }
    }

    var shipYawAngle = vehicleInterface.GetShipYawAngle();
    __instance.m_shipWindIndicatorRoot.localRotation =
      Quaternion.Euler(0f, 0f, shipYawAngle);
    var windAngle = vehicleInterface.GetWindAngle();
    __instance.m_shipWindIconRoot.localRotation =
      Quaternion.Euler(0f, 0f, windAngle);
    var windAngleFactor = vehicleInterface.GetWindAngleFactor();
    __instance.m_shipWindIcon.color =
      Color.Lerp(new Color(0.2f, 0.2f, 0.2f, 1f), Color.white, windAngleFactor);
    var mainCamera = Utils.GetMainCamera();
    if (!(mainCamera == null))
      __instance.m_shipControlsRoot.transform.position =
        mainCamera.WorldToScreenPointScaled(vehicleInterface.m_controlGuiPos
          .position);
  }

  [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateShipHud))]
  [HarmonyPrefix]
  public static bool UpdateShipHud(Hud __instance, Player player, float dt)
  {
    var controlledShipObj = Player_Patch.HandleGetControlledShip(player);
    if (controlledShipObj == null) return true;

    var vehicleInterface = VehicleControllersCompat.InitFromUnknown(controlledShipObj);

    if (vehicleInterface == null) return true;

    if (vehicleInterface.IsVehicleShip)
      ToggleAnchorHud(vehicleInterface.VehicleShipInstance);
    else
      return true;

    if (vehicleInterface is
        { IsVehicleShip: false, IsMbRaft: false }) return true;

    UpdateShipHudV2(__instance, player, dt, vehicleInterface);
    return false;
  }
}