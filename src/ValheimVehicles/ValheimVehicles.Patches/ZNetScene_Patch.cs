#region

  using System.Collections.Generic;
  using HarmonyLib;
  using UnityEngine;
  using ValheimVehicles.Components;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Shared.Constants;
  using Zolantris.Shared;

#endregion

  namespace ValheimVehicles.Patches;

  [HarmonyPatch]
  public static class ZNetScene_Patch
  {
    private static readonly AccessTools.FieldRef<ZNetScene, List<ZNetView>> TempRemovedRef =
      AccessTools.FieldRefAccess<ZNetScene, List<ZNetView>>("m_tempRemoved");

    private static readonly AccessTools.FieldRef<ZNetScene, Dictionary<ZDO, ZNetView>> InstancesRef =
      AccessTools.FieldRefAccess<ZNetScene, Dictionary<ZDO, ZNetView>>("m_instances");

    [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
    [HarmonyPrefix]
    private static bool CreateDestroyObjects()
    {
      return !PatchSharedData.m_disableCreateDestroy;
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    [HarmonyPostfix]
    private static void InjectGlobalVehicleSyncRoutine()
    {
      VehiclePiecesController.StartServerUpdaters();
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.RemoveObjects))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static bool RemoveObjects_Prefix(
      ZNetScene __instance,
      List<ZDO> currentNearObjects,
      List<ZDO> currentDistantObjects)
    {
      var earmark = (byte)(Time.frameCount & byte.MaxValue);

      for (var i = 0; i < currentNearObjects.Count; i++)
      {
        var zdo = currentNearObjects[i];
        if (zdo == null)
        {
          continue;
        }

        zdo.TempRemoveEarmark = earmark;
      }

      for (var i = 0; i < currentDistantObjects.Count; i++)
      {
        var zdo = currentDistantObjects[i];
        if (zdo == null)
        {
          continue;
        }

        zdo.TempRemoveEarmark = earmark;
      }

      var tempRemoved = TempRemovedRef(__instance);
      var instances = InstancesRef(__instance);

      tempRemoved.Clear();

      foreach (var kvp in instances)
      {
        var znetView = kvp.Value;
        if (!znetView)
        {
          continue;
        }

        var zdo = znetView.GetZDO();
        if (zdo == null)
        {
          continue;
        }

        if (zdo.TempRemoveEarmark != earmark)
        {
          tempRemoved.Add(znetView);
        }
      }

      for (var index = 0; index < tempRemoved.Count; index++)
      {
        var znetView = tempRemoved[index];
        if (!znetView)
        {
          continue;
        }

        var zdo = znetView.GetZDO();
        if (zdo == null)
        {
          continue;
        }

        if (ShouldRetainVehicleChildZdo(zdo))
        {
          continue;
        }

        znetView.ResetZDO();
        Object.Destroy(znetView.gameObject);

        if (!zdo.Persistent && zdo.IsOwner())
        {
          ZDOMan.instance.DestroyZDO(zdo);
        }

        instances.Remove(zdo);
      }

      return false;
    }

    /// <summary>
    /// Prevent scene unloading of a vehicle child ZDO while its parent vehicle is still active.
    /// </summary>
    private static bool ShouldRetainVehicleChildZdo(ZDO zdo)
    {
      if (zdo == null)
      {
        return false;
      }

      if (VehiclePiecesController.VehicleParentIdCache.TryGetValue(zdo, out var cachedParentPersistentId))
      {
        return IsVehicleParentActive(cachedParentPersistentId);
      }

      var parentPersistentId = zdo.GetInt(VehicleZdoVars.MBParentId, 0);
      VehiclePiecesController.VehicleParentIdCache[zdo] = parentPersistentId;

      return IsVehicleParentActive(parentPersistentId);
    }

    private static bool IsVehicleParentActive(int parentPersistentId)
    {
      if (parentPersistentId == 0)
      {
        return false;
      }

      if (!VehicleManager.VehicleInstances.TryGetValue(parentPersistentId, out var vehicleManager))
      {
        return false;
      }

// todo this other logic is likely not needed.
      return true;

      //
      // if (!vehicleManager || !vehicleManager.isActiveAndEnabled)
      // {
      //   return false;
      // }
      //
      //
      // 
      //
      // if (vehicleManager.m_nview == null)
      // {
      //   return false;
      // }
      //
      // var vehicleZdo = vehicleManager.m_nview.GetZDO();
      // if (vehicleZdo == null)
      // {
      //   return false;
      // }
      //
      // return true;
    }

#if DEBUG
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.OnDestroy))]
    [HarmonyPrefix]
    private static void ZNetScene_OnDestroy_Subscribe()
    {
      LoggerProvider.LogDev("called ZNetScene_OnDestroy");
    }
#endif
  }