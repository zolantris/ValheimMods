using System;
using System.Collections.Generic;
using HarmonyLib;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using Zolantris.Shared;
namespace ValheimVehicles.Patches;

public static class Humanoid_EquipPatch
{
  // must use translation names for shared.m_name
  public static Dictionary<Player, CannonHandHeldController> PlayerCannonController = new();

  // [HarmonyPatch(typeof(Humanoid), nameof(EquipItem), new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPatch(typeof(Humanoid), "EquipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPostfix]
  private static void EquipItemPatch(Player __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
  {
    try
    {
      if (__instance == null) return;
      if (item == null) return;
      if (item.m_shared == null) return;
      if (item.m_shared.m_name != PrefabItemNameToken.CannonHandHeldName) return;
      CannonPrefabConfig.SyncHandheldItemData(item);
    }
    catch (Exception e)
    {
      LoggerProvider.LogDebugDebounced($"Problem occurred with EquipItemPatch. Report this issue. {e}");
    }
  }

  // [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem), new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPatch(typeof(Humanoid), "UnequipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
  [HarmonyPostfix]
  private static void UnequipItemPatch(Player __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
  {
    if (item == null || __instance == null) return;
    if (item.m_shared == null) return;
    if (item.m_shared.m_name != PrefabItemNameToken.CannonHandHeldName) return;
    PlayerCannonController.Remove(__instance);
    PlayerCannonController.RemoveNullKeys();
  }


  // [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
  [HarmonyPatch(typeof(Humanoid), "OnAttackTrigger")]
  [HarmonyPrefix]
  private static bool HandCannon_OnAttackTrigger(Player __instance)
  {
    if (__instance == null) return false;
    if (__instance.IsDead()) return false;

    var currentWeapon = __instance.GetCurrentWeapon();
    if (currentWeapon == null || currentWeapon.m_shared.m_name != PrefabItemNameToken.CannonHandHeldName) return true;
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

    CannonPrefabConfig.SyncHandheldItemData(currentWeapon);

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