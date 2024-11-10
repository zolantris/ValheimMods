using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT.Patches;
using ValheimVehicles.Vehicles.Components;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Patches;

public class Minimap_VehicleIcons
{
  public static MapPinSync PinSyncInstance = null;

  [HarmonyPatch(typeof(Minimap), "Start")]
  [HarmonyPostfix]
  public static void InjectMapUpdater(Minimap __instance)
  {
    __instance.gameObject.AddComponent<MapPinSync>();
  }

  [HarmonyPatch(typeof(Minimap), "OnDestroy")]
  [HarmonyPrefix]
  public static void DestroyInjecter(Minimap __instance)
  {
    Object.Destroy(PinSyncInstance);
  }

  // [HarmonyPatch(typeof(Minimap), nameof(Minimap.Start))]
  // [HarmonyPostfix]
  // public static void ForceUpdateIconSpriteSize(Minimap __instance)
  // {
  //   __instance.m_visibleIconTypes =
  //     new bool[Enum.GetValues(typeof(Minimap.PinType)).Length];
  //
  //   // adds additional indexes for icons we want.
  //   __instance.m_visibleIconTypes[__instance.m_visibleIconTypes.Length] = true;
  //
  //   return;
  // }
  [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerMarker))]
  [HarmonyPrefix]
  public static bool UpdatePlayerMarker(Minimap __instance, Player player,
    Quaternion playerRot)
  {
    Vector3 position = player.transform.position;
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
      __instance.m_largeMarker.gameObject.SetActive(false);

    Ship controlledShip = player.GetControlledShip();
    if ((bool)(UnityEngine.Object)controlledShip)
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

  // [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePlayerMarker))]
  // static IEnumerable<CodeInstruction> Transpiler(
  //   IEnumerable<CodeInstruction> instructions)
  // {
  //   var codes = new List<CodeInstruction>(instructions);
  //   var getControlledShipMethod = AccessTools.Method(typeof(Player),
  //     nameof(Player.GetControlledShip));
  //   var customCheckMethod = AccessTools.Method(typeof(Player_Patch),
  //     nameof(Player_Patch.HandleGetControlledShip), [typeof(Player)]);
  //
  //   for (int i = 0; i < codes.Count; i++)
  //   {
  //     if (codes[i].Calls(getControlledShipMethod))
  //     {
  //       // Replace the call to GetControlledShip with MyCustomGetControlledShipCheck
  //       codes[i] = new CodeInstruction(OpCodes.Call, customCheckMethod);
  //     }
  //   }
  //
  //   return codes.AsEnumerable();
  // }
}