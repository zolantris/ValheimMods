using HarmonyLib;
using UnityEngine;
using ValheimScalability.Rendering.Clustering;

namespace ValheimScalability.Patches;

[HarmonyPatch]
public static class SectorMeshClusterPatches
{
  [HarmonyPostfix]
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
  private static void ZNetScene_Awake_Postfix(
    ZNetScene __instance)
  {
    if (Application.isBatchMode ||
        !__instance)
    {
      return;
    }

    SectorMeshClusterManager
      .EnsureAttached();
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.AddInstance))]
  private static void ZNetScene_AddInstance_Postfix(
    ZDO zdo,
    ZNetView nview)
  {
    SectorMeshClusterManager
      .EnsureAttached()?
      .RegisterNetworkObject(
        zdo,
        nview);
  }

  [HarmonyPrefix]
  [HarmonyPatch(typeof(ZNetView), "ResetZDO")]
  private static void ZNetView_ResetZDO_Prefix(
    ZNetView __instance)
  {
    SectorMeshClusterManager.Instance?
      .UnregisterNetworkObject(
        __instance);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.PokeLocalZone))]
  private static void ZoneSystem_PokeLocalZone_Postfix(
    ZoneSystem __instance,
    Vector2s zoneID,
    bool __result)
  {
    if (!__result ||
        Application.isBatchMode)
    {
      return;
    }

    if (!__instance.m_zones.TryGetValue(
          zoneID,
          out var zoneData) ||
        !zoneData.m_root)
    {
      return;
    }

    SectorMeshClusterManager
      .EnsureAttached()?
      .RegisterZoneHierarchy(
        zoneID,
        zoneData.m_root);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SpawnProxyLocation))]
  private static void ZoneSystem_SpawnProxyLocation_Postfix(
    GameObject __result)
  {
    if (!__result)
    {
      return;
    }

    SectorMeshClusterManager
      .EnsureAttached()?
      .RegisterLocationHierarchy(
        __result);
  }

  [HarmonyPostfix]
  [HarmonyPatch(typeof(Player), "Awake")]
  private static void Player_Awake_Postfix(
    Player __instance)
  {
    if (Application.isBatchMode ||
        !__instance ||
        __instance.GetComponent<PlayerClusterProximityTracker>())
    {
      return;
    }

    __instance.gameObject
      .AddComponent<PlayerClusterProximityTracker>();
  }
}
