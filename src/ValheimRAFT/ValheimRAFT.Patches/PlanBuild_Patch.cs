using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using PlanBuild.Blueprints;
using PlanBuild.Blueprints.Components;
using PlanBuild.ModCompat;
using PlanBuild.Plans;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class PlanBuild_Patch
{
  [HarmonyPatch(typeof(PlanPiece),
    nameof(PlanPiece.CalculateSupported))]
  [HarmonyPrefix]
  private static bool PlanPiece_CalculateSupported_Prefix(PlanPiece __instance,
    ref bool __result)
  {
    if (__instance.GetComponentInParent<VehiclePiecesController>())
    {
      __result = true;
      return false;
    }

    if (__instance.GetComponentInParent<MoveableBaseRootComponent>())
    {
      __result = true;
      return false;
    }

    return true;
  }

  [HarmonyPatch(typeof(PlanPiece),
    nameof(PlanPiece.OnPieceReplaced))]
  [HarmonyPrefix]
  private static void PlanPiece_OnPieceReplaced_Postfix(
    GameObject originatingPiece,
    GameObject placedPiece)
  {
    var baseVehicle =
      originatingPiece.GetComponentInParent<VehiclePiecesController>();

    if (baseVehicle)
    {
      baseVehicle.AddNewPiece(placedPiece.GetComponent<Piece>());
      return;
    }

    var moveableBaseRoot =
      originatingPiece.GetComponentInParent<MoveableBaseRootComponent>();
    if (moveableBaseRoot)
      moveableBaseRoot.AddNewPiece(placedPiece.GetComponent<Piece>());
  }

  [HarmonyPatch(typeof(PlacementComponent),
    nameof(PlacementComponent.OnPiecePlaced))]
  [HarmonyPrefix]
  private static void BlueprintManager_OnPiecePlaced_Postfix(
    GameObject placedPiece)
  {
    Player_Patch.PlacedPiece(placedPiece);
  }


  // [HarmonyPatch(typeof(Blueprint), nameof(Blueprint.Capture))]
  // [HarmonyPrefix]
  // public static bool Capture(Blueprint __instance, ref bool __result,
  //   Selection selection,
  //   bool captureCurrentSnapPoints = false, bool keepMarkers = false)
  // {
  //   Logger.LogDebug("Using ValheimRAFT patch for: PlanBuild.Blueprint.Capture");
  //
  //   Logger.LogDebug("Collecting piece information");
  //   var num = 0;
  //   List<Piece> list = new();
  //   var list2 = new List<Vector3>();
  //   Transform transform = null;
  //   List<TerrainModEntry> list3 = new();
  //   foreach (var item2 in selection)
  //   {
  //     var gameObject =
  //       BlueprintManager.GetGameObject(item2, true);
  //     Logger.LogDebug($"GameObject {gameObject.name}");
  //     if (gameObject.name.StartsWith("piece_bpsnappoint"))
  //     {
  //       list2.Add(gameObject.transform.position);
  //       if (!keepMarkers)
  //       {
  //         var component = gameObject.GetComponent<WearNTear>();
  //         component.Remove();
  //       }
  //
  //       continue;
  //     }
  //
  //     if (gameObject.name.StartsWith("piece_bpcenterpoint"))
  //     {
  //       if (transform == null)
  //         transform = gameObject.transform;
  //       else
  //         Logger.LogWarning(
  //           $"Multiple center points! Ignoring @ {gameObject.transform.position}");
  //
  //       if (!keepMarkers)
  //       {
  //         var component2 = gameObject.GetComponent<WearNTear>();
  //         component2.Remove();
  //       }
  //
  //       continue;
  //     }
  //
  //     if (gameObject.name.StartsWith("piece_bpterrainmod"))
  //     {
  //       var zDO = gameObject.GetComponent<ZNetView>().GetZDO();
  //       var item = new TerrainModEntry(zDO.GetString("shape"),
  //         gameObject.transform.position,
  //         float.Parse(zDO.GetString("radius"), CultureInfo.InvariantCulture),
  //         int.Parse(zDO.GetString("rotation"), CultureInfo.InvariantCulture),
  //         float.Parse(zDO.GetString("smooth"), CultureInfo.InvariantCulture),
  //         zDO.GetString("paint"));
  //       list3.Add(item);
  //       if (!keepMarkers)
  //       {
  //         var component3 = gameObject.GetComponent<WearNTear>();
  //         component3.Remove();
  //       }
  //
  //       continue;
  //     }
  //
  //     var component4 = gameObject.GetComponent<Piece>();
  //     if (!BlueprintManager.CanCapture(component4))
  //     {
  //       Logger.LogWarning(
  //         $"Ignoring piece {component4}, not able to make blueprint");
  //       continue;
  //     }
  //
  //     if (captureCurrentSnapPoints)
  //     {
  //       var componentsInChildren =
  //         gameObject.GetComponentsInChildren<Transform>(true);
  //       foreach (var transform2 in componentsInChildren)
  //         if (transform2.name.StartsWith("_snappoint"))
  //           list2.Add(transform2.position);
  //     }
  //
  //     list.Add(component4);
  //     num++;
  //   }
  //
  //   if (!list.Any())
  //   {
  //     __result = false;
  //     return true;
  //   }
  //
  //   Logger.LogDebug($"Found {num} pieces");
  //   Vector3 vector;
  //   if (transform == null)
  //   {
  //     var num2 = 10000000f;
  //     var num3 = 10000000f;
  //     var num4 = 10000000f;
  //     /*
  //      * @note this section needs to be optimized to not set a single vector3 object with new values per iterator.
  //      */
  //     foreach (var item3 in list)
  //     {
  //       num3 = Math.Min(item3.m_nview.GetZDO().m_position.x, num3);
  //       num2 = Math.Min(item3.m_nview.GetZDO().m_position.z, num2);
  //       num4 = Math.Min(item3.m_nview.GetZDO().m_position.y, num4);
  //     }
  //
  //     Logger.LogDebug($"{num3} - {num4} - {num2}");
  //     vector = new Vector3(num3, num4, num2);
  //   }
  //   else
  //   {
  //     vector = transform.position;
  //   }
  //
  //   IOrderedEnumerable<Piece> orderedEnumerable = from x in list
  //     orderby x.transform.position.y, x.transform.position.x, x.transform
  //       .position.z
  //     select x;
  //   var num5 = orderedEnumerable.Count();
  //   if (__instance.PieceEntries == null)
  //   {
  //     __instance.PieceEntries = new PieceEntry[num5];
  //   }
  //   else if (__instance.PieceEntries.Length != 0)
  //   {
  //     Array.Clear(__instance.PieceEntries, 0,
  //       __instance.PieceEntries.Length - 1);
  //     Array.Resize(ref __instance.PieceEntries, num5);
  //   }
  //
  //   var num6 = 0u;
  //   foreach (var item4 in orderedEnumerable)
  //   {
  //     Vector3 pos;
  //     var shipBase =
  //       item4.m_nview.GetComponentInParent<MoveableBaseRootComponent>();
  //     var vehicleBase =
  //       item4.m_nview.GetComponentInParent<VehiclePiecesController>();
  //     if (shipBase)
  //     {
  //       Logger.LogDebug(
  //         $"used ValheimRAFT.MoveableBaseRootComponent calc for position {item4.m_nview.m_zdo.GetVec3(MoveableBaseRootComponent.MBPositionHash, Vector3.zero)}");
  //       pos = item4.m_nview.transform.localPosition;
  //     }
  //     else if (vehicleBase)
  //     {
  //       Logger.LogDebug(
  //         $"used ValheimVehicles.BaseVehicleController calc for position {item4.m_nview.m_zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero)}");
  //       pos = item4.m_nview.transform.localPosition;
  //     }
  //     else
  //     {
  //       /*
  //        * @warning this vector spot looks like it will mismatch. The list above should really be a vector[] which matches the index
  //        * Example: pos = item4.m_nview.GetZDO().GetPosition() - vector[i]
  //        */
  //       pos = item4.m_nview.GetZDO().GetPosition() - vector;
  //       Logger.LogDebug("used default calc for position");
  //     }
  //
  //     Logger.LogDebug($"pos {pos}");
  //
  //
  //     Quaternion rotation;
  //
  //     if (shipBase)
  //     {
  //       Logger.LogDebug("used ship calc for rotation");
  //       rotation = item4.m_nview.transform.localRotation;
  //     }
  //     else
  //     {
  //       rotation = item4.m_nview.GetZDO().GetRotation();
  //       Logger.LogDebug("used default calc for rotation");
  //       rotation.eulerAngles = item4.transform.eulerAngles;
  //     }
  //
  //
  //     var text = string.Empty;
  //     var component5 = item4.GetComponent<TextReceiver>();
  //     if (component5 != null) text = component5.GetText();
  //
  //     var component6 = item4.GetComponent<ItemStand>();
  //     if (component6 != null && component6.HaveAttachment() &&
  //         (bool)component6.m_nview)
  //       text = string.Format("{0}:{1}:{2}",
  //         component6.m_nview.m_zdo.GetString("item"),
  //         component6.m_nview.m_zdo.GetInt("variant"),
  //         component6.m_nview.m_zdo.GetInt("quality"));
  //
  //     var component7 = item4.GetComponent<ArmorStand>();
  //     if (component7 != null && (bool)component7.m_nview)
  //     {
  //       text = $"{component7.m_pose}:";
  //       text += $"{component7.m_slots.Count}:";
  //       foreach (var slot in component7.m_slots)
  //         text += $"{slot.m_visualName}:{slot.m_visualVariant}:";
  //     }
  //
  //     var component8 = item4.GetComponent<Door>();
  //     if (component8 != null && (bool)component8.m_nview)
  //       text = string.Format("{0}", component8.m_nview.m_zdo.GetInt("state"));
  //
  //     var component9 = item4.GetComponent<PrivateArea>();
  //     if (component9 != null && (bool)component9.m_nview)
  //       text = string.Format("{0}",
  //         component9.m_nview.m_zdo.GetBool("enabled"));
  //
  //     var component10 = item4.GetComponent<Container>();
  //     if (component10 != null && (bool)component10.m_nview)
  //       text = component10.m_nview.GetZDO().GetString("items");
  //
  //     var localScale = item4.transform.localScale;
  //     var text2 = item4.name.Split('(')[0];
  //     var component11 = item4.gameObject.GetComponent<ZNetView>();
  //     if ((object)component11 != null && component11.m_zdo != null)
  //     {
  //       var prefab =
  //         ZNetScene.instance.GetPrefab(component11.m_zdo.m_prefab);
  //       if ((object)prefab != null) text2 = prefab.name;
  //     }
  //
  //     if (text2.EndsWith("_planned")) text2 = text2.Replace("_planned", null);
  //
  //     __instance.PieceEntries[num6++] = new PieceEntry(text2,
  //       item4.m_category.ToString(), pos,
  //       rotation, text, localScale);
  //   }
  //
  //   var list4 = (from x in list2
  //     group x by x
  //     into x
  //     where x.Count() == 1
  //     select x.Key).ToList();
  //   if (__instance.SnapPoints == null)
  //   {
  //     __instance.SnapPoints = new SnapPointEntry[list4.Count];
  //   }
  //   else if (__instance.SnapPoints.Length != 0)
  //   {
  //     Array.Clear(__instance.SnapPoints, 0, __instance.SnapPoints.Length - 1);
  //     Array.Resize(ref __instance.SnapPoints, list4.Count);
  //   }
  //
  //   for (var j = 0; j < list4.Count; j++)
  //     __instance.SnapPoints[j] = new SnapPointEntry(list4[j] - vector);
  //
  //   if (__instance.TerrainMods == null)
  //   {
  //     __instance.TerrainMods = new TerrainModEntry[list3.Count];
  //   }
  //   else if (__instance.TerrainMods.Length != 0)
  //   {
  //     Array.Clear(__instance.TerrainMods, 0, __instance.TerrainMods.Length - 1);
  //     Array.Resize(ref __instance.TerrainMods, list3.Count);
  //   }
  //
  //   var num7 = 0u;
  //   foreach (var item5 in list3)
  //     __instance.TerrainMods[num7++] = new TerrainModEntry(item5.shape,
  //       item5.GetPosition() - vector, item5.radius, item5.rotation,
  //       item5.smooth, item5.paint);
  //
  //   __result = true;
  //   // Prevents PlanBuild from running this method
  //   return false;
  // }
}