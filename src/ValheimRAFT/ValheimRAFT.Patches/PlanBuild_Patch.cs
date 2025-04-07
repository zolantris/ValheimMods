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
}