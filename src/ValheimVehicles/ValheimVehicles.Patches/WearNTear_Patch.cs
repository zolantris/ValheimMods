#region

  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection.Emit;
  using HarmonyLib;
  using UnityEngine;
  using ValheimVehicles.Components;
  using ValheimVehicles.Config;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.Prefabs.Registry;
  using ValheimVehicles.Structs;
  using ZdoWatcher;

#endregion

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class WearNTear_Patch
{
  [HarmonyPatch(typeof(WearNTear), "Start")]
  [HarmonyPrefix]
  private static bool WearNTear_Start(WearNTear __instance)
  {
    // we could check to see if the object is within a Controller, but this is unnecessary. Start just needs a protector.
    // this is a patch for basegame to prevent WNT from calling on objects without heightmaps which will return a NRE
    var hInstance = Heightmap.FindHeightmap(__instance.transform.position);
    if (hInstance != null) return true;

    // todo add heightmaps to vehicle. See if this helps improve some functionality.

    // if (Environment.All)
    // Logger.LogWarning(
    // $"WearNTear heightmap not found, this could be a problem with a prefab layer type not being a piece, netview name: {__instance.m_nview.name}");
    
    __instance.m_connectedHeightMap = hInstance;
    return false;
  }

  [HarmonyPatch(typeof(WearNTear), "Repair")]
  [HarmonyPrefix]
  private static bool WearNTear_Repair(WearNTear __instance, ref bool __result)
  {
    if (RamConfig.CanRepairRams.Value ||
        !RamPrefabs.IsRam(__instance.gameObject.name)) return true;

    __result = false;
    return false;
  }

  /*
   * IF the mod breaks, this is a SAFETY FEATURE
   * - prevents destruction of ship attached pieces if the ship fails to initialize properly
   */
  private static bool PreventDestructionOfItemWithoutInitializedRaft(
    WearNTear __instance)
  {
    if (!PrefabConfig.ProtectVehiclePiecesOnErrorFromWearNTearDamage.Value)
      return false;

    var parentVehicleHash =
      __instance.m_nview.m_zdo.GetInt(VehicleZdoVars.MBParentId, 0);

    var hasParentVehicleHash = parentVehicleHash != 0;
    if (!hasParentVehicleHash) return false;

    var id = ZdoWatchController.ZdoIdToId(__instance.m_nview.GetZDO().m_uid);
    var zdoExists = ZdoWatchController.Instance.GetZdo(id);
    if (zdoExists == null) return false;

    __instance.enabled = false;
    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "Destroy")]
  [HarmonyPrefix]
  private static bool WearNTear_Destroy(WearNTear __instance)
  {
    if (PrefabNames.IsVehicle(__instance.gameObject.name))
      try
      {
        var canDestroyVehicle =
          VehiclePiecesController.CanDestroyVehicle(__instance.m_nview);
        return canDestroyVehicle;
      }
      catch
      {
        // if the mod is crashed it will not delete the raft controlling object to prevent the raft from being deleted if the user had a bad install or the game updated
        return false;
      }

    if (RamPrefabs.IsRam(__instance.name))
    {
      var vehicle = __instance.GetComponentInParent<VehicleBaseController>();
      vehicle?.PiecesController?.DestroyPiece(__instance);
    }

    var pieceController = __instance.GetComponentInParent<IPieceController>();
    if (pieceController != null)
    {
      pieceController.DestroyPiece(__instance);
    }

    return true;
  }

  [HarmonyPatch(typeof(WearNTear), "SetHealthVisual")]
  [HarmonyPrefix]
  private static bool WearNTear_SetHealthVisual(WearNTear __instance,
    float health,
    bool triggerEffects)
  {
    var isHull = PrefabNames.IsHull(__instance.gameObject);
    if (!isHull) return true;


    if (__instance.m_worn == null && __instance.m_broken == null &&
        __instance.m_new == null) return false;

    if (health > 0.75f)
    {
      if (__instance.m_worn != __instance.m_new)
        __instance.m_worn.SetActive(false);

      if (__instance.m_broken != __instance.m_new)
        __instance.m_broken.SetActive(false);

      __instance.m_new.SetActive(true);
    }
    else if (health > 0.25f)
    {
      if (triggerEffects && !__instance.m_worn.activeSelf)
        __instance.m_switchEffect.Create(__instance.transform.position,
          __instance.transform.rotation, __instance.transform);

      if (__instance.m_new != __instance.m_worn)
        __instance.m_new.SetActive(false);

      if (__instance.m_broken != __instance.m_worn)
        __instance.m_broken.SetActive(false);

      __instance.m_worn.SetActive(true);
    }
    else
    {
      if (triggerEffects && !__instance.m_broken.activeSelf)
        __instance.m_switchEffect.Create(__instance.transform.position,
          __instance.transform.rotation, __instance.transform);

      if (__instance.m_new != __instance.m_broken)
        __instance.m_new.SetActive(false);

      if (__instance.m_worn != __instance.m_broken)
        __instance.m_worn.SetActive(false);

      __instance.m_broken.SetActive(true);
    }

    return false;
  }

  [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
  [HarmonyPrefix]
  private static bool WearNTear_ApplyDamage(WearNTear __instance, float damage)
  {
    // watervehicleship should receive no wearntear damage
    if (PrefabNames.IsVehicle(__instance.gameObject.name))
      return false;

    var bv = __instance.GetComponentInParent<VehiclePiecesController>();
    if ((bool)bv) return true;

    // scans all the items to see if there is a vehicle reference.
    return !PreventDestructionOfItemWithoutInitializedRaft(__instance);
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPatch(typeof(WearNTear), "SetupColliders")]
  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> WearNTear_AttachShip(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i]
          .Calls(AccessTools.PropertyGetter(typeof(Collider),
            "attachedRigidbody")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(WearNTear_Patch),
            nameof(AttachRigidbodyMovableBase)));
        break;
      }

    return list;
  }

  [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
  [HarmonyPrefix]
  private static bool UpdateSupport(WearNTear __instance)
  {
    if (!__instance.isActiveAndEnabled) return false;
    var baseVehicle =
      __instance.GetComponentInParent<IPieceActivatorHost>();
    if (baseVehicle == null) return true;

    // makes all support values below 1f very high
    if (!Mathf.Approximately(__instance.m_support, 1500f) && __instance.m_nview != null && __instance.m_nview.GetZDO() != null)
    {
      __instance.m_nview.GetZDO().Set(ZDOVars.s_support, 1500f);
    }
    __instance.m_support = 1500f;
    __instance.m_supports = true;
    __instance.m_noSupportWear = true;
    return false;
  }

  private static Rigidbody? AttachRigidbodyMovableBase(Collider collider)
  {
    var rb = collider.attachedRigidbody;
    if (!rb) return null;
    var bvc = rb.GetComponent<VehiclePiecesController>();
    if (bvc) return null;
    return rb;
  }
}