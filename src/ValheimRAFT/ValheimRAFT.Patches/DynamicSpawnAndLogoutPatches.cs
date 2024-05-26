using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Vehicles.Components;

namespace ValheimRAFT.Patches;

public class DynamicSpawnAndLogoutPatches
{
  // [HarmonyTranspiler]
  // [HarmonyPatch(typeof(Bed), "Interact")]
  // private static IEnumerable<CodeInstruction> PlacePieceTranspiler(
  //   IEnumerable<CodeInstruction> instructions)
  // {
  //   var operand =
  //     HarmonyPatchMethods.GetGenericMethod(typeof(PlayerProfile), "SetCustomSpawnPoint", 1,
  //       new Type[1]);
  //
  //
  //   var list = instructions.ToList();
  //   // for (var i = 0; i < list.Count; i++)
  //   //   if (list[i].Calls(AccessTools.Method(typeof(Quaternion), "Euler", new[]
  //   //       {
  //   //         typeof(float),
  //   //       })))
  //   //     list[i] = new CodeInstruction(OpCodes.Call, operand);
  //   // return list;
  //   CodeInstruction.LoadField(OpCodes.Ldloc, )
  //   CodeInstructionExtensions.ArgumentIndex(1);
  //   var matches = new CodeMatch[]
  //   {
  //     new(OpCodes.Call, operand),
  //   };
  //   return new CodeMatcher(instructions).MatchForward(useEnd: true, matches).Advance(1)
  //     .InsertAndAdvance(
  //       Transpilers.EmitDelegate<Func<Bed>>(OnSpawnPointUpdated))
  //     .InstructionEnumeration();
  // }
  private static Vector3? prevCustomSpawnPoint = null;

  [HarmonyPatch(typeof(Bed), "Interact")]
  [HarmonyPrefix]
  private static void OnSpawnPointUpdatedPrefix()
  {
    prevCustomSpawnPoint = Game.instance.GetPlayerProfile().GetCustomSpawnPoint();
  }

  [HarmonyPatch(typeof(Bed), "Interact")]
  [HarmonyPostfix]
  private static void OnSpawnPointUpdated(Bed __instance)
  {
    var currentSpawnPoint = Game.instance.GetPlayerProfile().GetCustomSpawnPoint();
    // if (prevCustomSpawnPoint == currentSpawnPoint) return;

    var spawnController = PlayerSpawnController.GetSpawnController(Player.m_localPlayer);
    if (!spawnController) return;
    spawnController?.SyncBedSpawnPoint(__instance.m_nview, __instance);
    prevCustomSpawnPoint = null;
  }

  [HarmonyPatch(typeof(PlayerProfile), "SaveLogoutPoint")]
  [HarmonyPostfix]
  private static void OnSaveLogoutPoint()
  {
    var spawnController = Player.m_localPlayer.GetComponentInChildren<PlayerSpawnController>();
    if (spawnController)
    {
      spawnController.SyncLogoutPoint();
    }
  }

  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  private static void OnSpawned(Player __result)
  {
    if (ZNetView.m_forceDisableInit) return;
    __result.gameObject.AddComponent<PlayerSpawnController>();
  }
}