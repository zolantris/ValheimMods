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

  public static bool _HandCannonWasHandled = false;
  // todo determine if this is needed for flexibility.
  public static bool AllowOtherPrefixesToApplyToHandheldCannon = false;

  // We need a prefix otherwise other patches could run and fire the cannon or interfere with the cannon logic. This must be run first.
  [HarmonyPrefix]
  [HarmonyPriority(Priority.VeryHigh)] // This patch will run before others with lower priority
#if DEBUG
  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
#else
  [HarmonyPatch(typeof(Humanoid), "OnAttackTrigger")]
#endif
  private static bool HandCannon_OnAttackTrigger(Player __instance)
  {
    // resets it always.
    _HandCannonWasHandled = false;

    if (__instance == null) return true;
    if (__instance.IsDead()) return true;

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

    _HandCannonWasHandled = true;

    var ammoItem = __instance.GetAmmoItem();

    if (ammoItem == null)
    {
      if (AllowOtherPrefixesToApplyToHandheldCannon)
      {
        return true;
      }
      else
      {
        // no prefixes run after this one.
        return false;
      }
    }

    cannonHandheldController.SetAmmoVariantFromToken(ammoItem.m_shared.m_name);
    cannonHandheldController.Request_FireHandHeld();

    if (AllowOtherPrefixesToApplyToHandheldCannon)
    {
      return true;
    }
    else
    {
      // no prefixes run after this one.
      return false;
    }
  }

  /// <summary>
  /// This prefix only runs if we allow additional OnAttackTrigger prefixes to run for the cannon. Otherwise it will just return true and allow original methods to fire for OnAttackTrigger.
  /// </summary>
  /// <param name="__instance"></param>
  /// <returns></returns>
  [HarmonyPrefix]
  [HarmonyPriority(Priority.Last)] // This patch will run after others with lower priority and will only bail if the first prefix was handled.
#if DEBUG
  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
#else
  [HarmonyPatch(typeof(Humanoid), "OnAttackTrigger")]
#endif
  private static bool HandCannon_OnAttackTriggerShouldBail(Player __instance)
  {
    if (_HandCannonWasHandled)
    {
      _HandCannonWasHandled = false;
      return false;
    }

    return true;
  }
}