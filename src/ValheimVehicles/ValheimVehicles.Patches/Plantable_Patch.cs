using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;


namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class Plantable_Patch
{
  // [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  // [HarmonyTranspiler]
  // private static IEnumerable<CodeInstruction> UpdatePlacementGhost(
  //   IEnumerable<CodeInstruction> instructions, ILGenerator generator)
  // {
  //   var list = instructions.ToList();
  //   for (var i = 0; i < list.Count; i++)
  //   {
  //     if (!list[i].StoresField(AccessTools.Field(typeof(Player),
  //           "m_placementStatus")) ||
  //         !list[i - 1].LoadsConstant(Player.PlacementStatus.NeedCultivated))
  //       continue;
  //
  //     var labels = list[i - 2].ExtractLabels();
  //     object pieceLocalIndex = null;
  //     var rayPieceMethod = AccessTools.Method(typeof(Player), "PieceRayTest");
  //     for (var j = 0; j < list.Count; j++)
  //       if (list[j].Calls(rayPieceMethod) && list[j - 4].IsLdloc())
  //       {
  //         pieceLocalIndex = list[j - 4].operand;
  //         break;
  //       }
  //
  //     var targetLabel = generator.DefineLabel();
  //     var sourceJump = (Label)list[i - 3].operand;
  //     list.Find((CodeInstruction match) => match.labels.Contains(sourceJump))
  //       .labels
  //       .Add(targetLabel);
  //     list.InsertRange(i - 2, new[]
  //     {
  //       new CodeInstruction(OpCodes.Ldloc, pieceLocalIndex).WithLabels(labels),
  //       new CodeInstruction(OpCodes.Call,
  //         AccessTools.Method(typeof(Plantable_Patch), "IsCultivated")),
  //       new CodeInstruction(OpCodes.Brtrue, targetLabel)
  //     });
  //     break;
  //   }
  //
  //   return list;
  // }
  //
  // private static bool IsCultivated(Piece piece)
  // {
  //   if (!piece) return false;
  //
  //   var cmp = piece.GetComponent<CultivatableComponent>();
  //   return (bool)cmp && cmp.isCultivatable;
  // }
  //
  // [HarmonyPatch(typeof(Plant), "HaveRoof")]
  // [HarmonyPrefix]
  // private static bool HaveRoof(Plant __instance, bool __result)
  // {
  //   if (Plant.m_roofMask == 0)
  //     Plant.m_roofMask = LayerMask.GetMask("Default", "static_solid", "piece");
  //
  //   var raycastHit =
  //     Physics.RaycastAll(__instance.transform.position, Vector3.up, 100f,
  //       Plant.m_roofMask);
  //
  //   if (Physics.Raycast(__instance.transform.position, Vector3.up, 100f,
  //         Plant.m_roofMask))
  //   {
  //     __result = true;
  //     return false;
  //   }
  //
  //   __result = false;
  //   return false;
  // }
  //
  // [HarmonyPatch(typeof(Plant), "Grow")]
  // [HarmonyTranspiler]
  // private static IEnumerable<CodeInstruction> Plant_Grow(
  //   IEnumerable<CodeInstruction> instructions)
  // {
  //   var list = instructions.ToList();
  //   for (var i = 0; i < list.Count; i++)
  //     if (list[i].Calls(AccessTools.Method(typeof(GameObject), "GetComponent",
  //           Type.EmptyTypes,
  //           new Type[1] { typeof(ZNetView) })))
  //     {
  //       list[i] = new CodeInstruction(OpCodes.Call,
  //         AccessTools.Method(typeof(Plantable_Patch), nameof(PlantGrowth)));
  //       list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
  //       break;
  //     }
  //
  //   return list;
  // }
  //
  // private static ZNetView PlantGrowth(GameObject newObject, Plant oldPlant)
  // {
  //   var newPlantNetView = newObject.GetComponent<ZNetView>();
  //
  //   if (newPlantNetView != null)
  //   {
  //     var bvc = oldPlant.GetComponentInParent<VehiclePiecesController>();
  //
  //     if (bvc != null)
  //     {
  //       bvc.AddNewPiece(newPlantNetView);
  //     }
  //   }
  //
  //   var cultivatable = CultivatableComponent.GetParentID(oldPlant.m_nview);
  //   if (cultivatable != 0)
  //     CultivatableComponent.AddNewChild(cultivatable, newPlantNetView);
  //
  //   return newPlantNetView;
  // }
  //
  // [HarmonyPatch(typeof(Plant), "UpdateHealth")]
  // [HarmonyPrefix]
  // private static bool Plant_UpdateHealth(Plant __instance,
  //   double timeSincePlanted)
  // {
  //   if (CultivatableComponent.GetParentID(__instance.m_nview) == 0) return true;
  //
  //   __instance.m_status = Plant.Status.Healthy;
  //   return false;
  // }
}