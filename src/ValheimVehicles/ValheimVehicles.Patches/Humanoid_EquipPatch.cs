using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using Object = UnityEngine.Object;
namespace ValheimVehicles.Patches;

public static class Humanoid_EquipPatch
{
  // must use translation names for shared.m_name

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem), new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPostfix]
  private static void EquipItemPatch(Player __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
  {
    if (__instance == null) return;
    if (item.m_shared.m_name != PrefabItemTranslations.TelescopeName) return;
    // if (item.m_shared.m_name != HandCannonName) return;
    // var currentWeapon = __instance.GetCurrentWeapon();
    // var cannonController = __instance.GetComponentInChildren<CannonControllerBridge>();
    // if (!cannonController) return;
    // LoggerProvider.LogDebug("Got cannoncontroller on equipe");
    var currentWeapon = __instance.GetCurrentWeapon();
  }

  public static void LogWeaponData(ItemDrop.ItemData currentWeapon)
  {
    LoggerProvider.LogDebug($"ammo currentAmmoType {currentWeapon.m_shared.m_ammoType}");
    LoggerProvider.LogDebug($"ammo m_attack.m_attackProjectile {currentWeapon.m_shared.m_attack.m_attackProjectile}");
    LoggerProvider.LogDebug($"ammo ammoItem {currentWeapon.m_shared?.m_attack?.m_ammoItem?.m_shared.m_name}");
  }

  public static void UpdateCannonControllerAmmo(Player __instance)
  {
    if (__instance == null) return;
    var cannonController = __instance.GetComponentInChildren<CannonControllerBridge>();
    if (cannonController == null) return;
  }


  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem), new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPostfix]
  private static void UnequipItemPatch(Player __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
  {

    // if (item == null || __instance == null) return;
    // if (item.m_shared.m_name == HandCannonName)
    // {
    //   // Remove the component
    //   var comp = __instance.gameObject.GetComponent<CannonControllerBridge>();
    //   if (comp) Object.Destroy(comp);
    // }
  }

  public static bool DebugOverrideCannon = true;
  public static bool SkipCustomCannonFire = false;
  public static bool CanDirectlyConsume = true;

  public static Dictionary<Player, CannonHandHeldController> PlayerCannonController = new();

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
  [HarmonyPrefix]
  private static bool HandCannon_OnAttackTrigger(Player __instance)
  {
    if (SkipCustomCannonFire) return true;
    var currentWeapon = __instance.GetCurrentWeapon();
    if (currentWeapon == null || currentWeapon.m_shared.m_name != PrefabItemTranslations.CannonHandHeldName) return true;
    if (!PlayerCannonController.TryGetValue(__instance, out var cannonHandheldController) || cannonHandheldController == null)
    {
      cannonHandheldController = __instance.GetComponentInChildren<CannonHandHeldController>();
      if (cannonHandheldController)
      {
        PlayerCannonController[__instance] = cannonHandheldController;
      }
    }

    if (!cannonHandheldController)
    {
      PlayerCannonController.Remove(__instance);
      return true;
    }

    currentWeapon.m_shared.m_attack.m_reloadTime = PrefabConfig.CannonHandHeld_ReloadTime.Value;

    LogWeaponData(currentWeapon);
    var ammoItem = __instance.GetAmmoItem();
    if (ammoItem == null)
    {
      return false;
    }
    cannonHandheldController.SetAmmoVariantFromToken(ammoItem.m_shared.m_name);
    cannonHandheldController.Request_FireHandHeld();

    return false;
  }
}