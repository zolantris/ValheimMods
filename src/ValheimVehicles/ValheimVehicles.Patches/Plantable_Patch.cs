using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class Plantable_Patch
{
  [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  [HarmonyPostfix]
  private static void UpdatePlacementGhost_Postfix(Player __instance)
  {
    // Allow planting on cultivatable components even if status says NeedCultivated
    if (__instance.m_placementStatus == Player.PlacementStatus.NeedCultivated &&
        __instance.m_placementGhost != null)
    {
      var piece = __instance.m_placementGhost.GetComponent<Piece>();
      if (piece != null)
      {
        // Check if we're hovering over a cultivatable component
        if (Player.m_localPlayer.PieceRayTest(out _, out _, out var hoverPiece,
              out _, out _, false) && hoverPiece != null)
        {
          var cmp = hoverPiece.GetComponent<CultivatableComponent>();
          if (cmp != null && cmp.isCultivatable)
          {
            __instance.m_placementStatus = Player.PlacementStatus.Valid;

            // Update the visual feedback to show green (valid placement)
            UpdatePlacementGhostVisuals(__instance.m_placementGhost, true);
          }
        }
      }
    }
  }

  private static void UpdatePlacementGhostVisuals(GameObject ghost, bool valid)
  {
    if (ghost == null) return;

    // Get all renderers on the placement ghost
    var renderers = ghost.GetComponentsInChildren<Renderer>();
    foreach (var renderer in renderers)
    {
      if (renderer.sharedMaterial == null) continue;

      // Update emission color to green for valid, red for invalid
      // Valheim uses emission color for placement ghost feedback
      var color = valid ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);

      // Set the color on the material property block to avoid material instances
      var propertyBlock = new MaterialPropertyBlock();
      renderer.GetPropertyBlock(propertyBlock);
      propertyBlock.SetColor("_EmissionColor", color);
      renderer.SetPropertyBlock(propertyBlock);
    }
  }

  [HarmonyPatch(typeof(Plant), "HaveRoof")]
  [HarmonyPrefix]
  private static bool HaveRoof(Plant __instance, ref bool __result)
  {
    if (Plant.m_roofMask == 0)
      Plant.m_roofMask = LayerMask.GetMask("Default", "static_solid", "piece");

    if (Physics.Raycast(__instance.transform.position, Vector3.up, 100f,
          Plant.m_roofMask))
    {
      __result = true;
      return false;
    }

    __result = false;
    return false;
  }

  [HarmonyPatch(typeof(Plant), "Grow")]
  [HarmonyPrefix]
  private static void Plant_Grow_Prefix(Plant __instance, out int __state)
  {
    // Capture the parent ID before the plant is destroyed
    __state = 0;
    if (!__instance || __instance.m_nview == null || __instance.m_nview.m_zdo == null) return;
    try
    {
      __state = CultivatableComponent.GetParentID(__instance.m_nview);
    }
    catch (System.Exception ex)
    {
      ZLog.LogWarning($"[Plantable_Patch] Failed to get parent ID in Grow_Prefix: {ex.Message}");
    }
  }

  [HarmonyPatch(typeof(Plant), "Grow")]
  [HarmonyPostfix]
  private static void Plant_Grow_Postfix(Plant __instance, GameObject __result, int __state)
  {
    if (__result == null) return;

    try
    {
      var newPlantNetView = __result.GetComponent<ZNetView>();
      if (newPlantNetView == null) return;

      // Don't use __instance here - it's destroyed. Use __result's parent instead.
      var bvc = __result.GetComponentInParent<VehiclePiecesController>();
      if (bvc != null)
      {
        bvc.AddNewPiece(newPlantNetView);
      }

      // Use the captured parent ID from the Prefix
      if (__state != 0)
      {
        CultivatableComponent.AddNewChild(__state, newPlantNetView);
      }
    }
    catch (System.Exception ex)
    {
      ZLog.LogWarning($"[Plantable_Patch] Failed to setup grown plant: {ex.Message}");
    }
  }

  [HarmonyPatch(typeof(Plant), "UpdateHealth")]
  [HarmonyPrefix]
  private static bool Plant_UpdateHealth(Plant __instance,
    double timeSincePlanted)
  {
    if (__instance.m_nview == null || __instance.m_nview.m_zdo == null) return true;
    if (CultivatableComponent.GetParentID(__instance.m_nview) == 0) return true;

    __instance.m_status = Plant.Status.Healthy;
    return false;
  }

  [HarmonyPatch(typeof(Plant), "HaveGrowSpace")]
  [HarmonyPrefix]
  private static bool Plant_HaveGrowSpace(Plant __instance, ref bool __result)
  {
    if (__instance.m_nview == null || __instance.m_nview.m_zdo == null) return true;
    if (CultivatableComponent.GetParentID(__instance.m_nview) == 0) return true;

    // Plants on vehicles always have grow space
    __result = true;
    return false;
  }

#region DEBUG ONLY CODE - REMOVE FOR RELEASE related to instant plant growth

#if DEBUG

  public static bool AllowNearInstantGrowTime = true;
  public static bool EnablePlantUpdateDebugLogging = true;

  [HarmonyPatch(typeof(Plant), "SUpdate")]
  [HarmonyPrefix]
  private static void Plant_SUpdate_Debug(Plant __instance, float time, Vector2i referenceZone)
  {
    if (!EnablePlantUpdateDebugLogging) return;
    if (__instance.m_nview == null || __instance.m_nview.m_zdo == null) return;

    var parentId = CultivatableComponent.GetParentID(__instance.m_nview);
    if (parentId == 0) return; // Only log plants on vehicles

    var timeSincePlanted = __instance.TimeSincePlanted();
    var growTime = __instance.GetGrowTime();
    var status = __instance.GetStatus();
    var isOwner = __instance.m_nview.IsOwner();
    var spawnTimeDiff = time - __instance.m_spawnTime;

    ZLog.Log($"[Plant Debug] Plant on vehicle - Status: {status}, " +
             $"TimeSincePlanted: {timeSincePlanted:F1}s, GrowTime: {growTime:F1}s, " +
             $"IsOwner: {isOwner}, SpawnTimeDiff: {spawnTimeDiff:F1}s, " +
             $"ShouldGrow: {timeSincePlanted > growTime && spawnTimeDiff > 10.0 && isOwner}");
  }

  [HarmonyPatch(typeof(Plant), "GetGrowTime")]
  [HarmonyPrefix]
  private static bool Plant_GetGrowTime_Debug(Plant __instance, ref float __result)
  {
    if (!AllowNearInstantGrowTime) return true;
    // Debug: Make plants grow in 11 seconds instead of minutes/hours
    __result = 11f;
    return false; // Skip original method
  }

  // Force plants to think they've been planted long enough to grow
  [HarmonyPatch(typeof(Plant), "TimeSincePlanted")]
  [HarmonyPostfix]
  private static void Plant_TimeSincePlanted_Debug(Plant __instance, ref double __result)
  {
    if (!AllowNearInstantGrowTime) return;
    if (__instance.m_nview == null || __instance.m_nview.m_zdo == null) return;

    // If on a vehicle or cultivatable, make it think it's been planted for a long time
    var parentId = CultivatableComponent.GetParentID(__instance.m_nview);
    if (parentId != 0)
    {
      // Return a time well past the grow time (1 hour = 3600 seconds)
      __result = 3600.0;
    }
  }

#endif

#endregion

}