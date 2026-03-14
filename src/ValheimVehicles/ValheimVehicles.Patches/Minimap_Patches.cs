using System;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Patches;

public class Minimap_VehicleIcons
{
  public static MapPinSync PinSyncInstance = null;

  [HarmonyPatch(typeof(Minimap), "Start")]
  [HarmonyPostfix]
  public static void InjectMapUpdater(Minimap __instance)
  {
    var minimapGo = __instance.gameObject;
    var minimapComponent = minimapGo.GetComponent<MapPinSync>();
    if (minimapGo.GetComponent<MapPinSync>() != null)
    {
      Object.Destroy(minimapComponent);
      return;
    }
    __instance.gameObject.AddComponent<MapPinSync>();
  }

  [HarmonyPatch(typeof(Minimap), "OnDestroy")]
  [HarmonyPrefix]
  public static void DestroyInjecter(Minimap __instance)
  {
    Object.Destroy(PinSyncInstance);
  }


  [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerMarker))]
  [HarmonyPrefix]
  public static bool UpdatePlayerMarker(Minimap __instance, Player player,
    Quaternion playerRot)
  {
    var position = player.transform.position;
    __instance.m_smallMarker.rotation =
      Quaternion.Euler(0.0f, 0.0f, -playerRot.eulerAngles.y);
    if (__instance.m_mode == Minimap.MapMode.Large &&
        __instance.IsPointVisible(position, __instance.m_mapImageLarge))
    {
      __instance.m_largeMarker.gameObject.SetActive(true);
      __instance.m_largeMarker.rotation = __instance.m_smallMarker.rotation;
      float mx;
      float my;
      __instance.WorldToMapPoint(position, out mx, out my);
      __instance.m_largeMarker.anchoredPosition =
        __instance.MapPointToLocalGuiPos(mx, my, __instance.m_mapImageLarge);
    }
    else
    {
      __instance.m_largeMarker.gameObject.SetActive(false);
    }

    var controlledShip = player.GetControlledShip();
    if ((bool)(Object)controlledShip)
    {
      __instance.m_smallShipMarker.gameObject.SetActive(true);
      __instance.m_smallShipMarker.rotation = Quaternion.Euler(0.0f, 0.0f,
        -controlledShip.transform.rotation.eulerAngles.y);
      if (__instance.m_mode != Minimap.MapMode.Large)
        return false;
      __instance.m_largeShipMarker.gameObject.SetActive(true);
      float mx;
      float my;
      __instance.WorldToMapPoint(controlledShip.transform.position, out mx,
        out my);
      __instance.m_largeShipMarker.anchoredPosition =
        __instance.MapPointToLocalGuiPos(mx, my, __instance.m_mapImageLarge);
      __instance.m_largeShipMarker.rotation =
        __instance.m_smallShipMarker.rotation;
    }
    else
    {
      __instance.m_smallShipMarker.gameObject.SetActive(false);
      __instance.m_largeShipMarker.gameObject.SetActive(false);
    }

    return false;
  }
}