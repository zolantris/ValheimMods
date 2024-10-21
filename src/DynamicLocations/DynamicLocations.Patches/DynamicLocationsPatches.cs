using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DynamicLocations.Config;
using DynamicLocations.Controllers;
using HarmonyLib;
using UnityEngine;

namespace DynamicLocations.Patches;

public class DynamicLocationsPatches
{
  [HarmonyPatch(typeof(Bed), "Interact")]
  [HarmonyPostfix]
  private static void OnSpawnPointUpdated(Bed __instance)
  {
    // todo compare if the current bed zdo is the players otherwise update it.
    var currentSpawnPoint =
      Game.instance.GetPlayerProfile().GetCustomSpawnPoint();

    var spawnController = PlayerSpawnController.Instance;
    if (!spawnController) return;
    spawnController?.SyncBedSpawnPoint(__instance.m_nview, __instance);
  }

  [HarmonyPatch(typeof(PlayerProfile), "SetLogoutPoint")]
  [HarmonyPostfix]
  private static void OnSaveLogoutPoint()
  {
    PlayerSpawnController.Instance.SyncLogoutPoint();
  }

  private static bool IsRespawningFromDeath = false;

  [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
  [HarmonyPostfix]
  private static void OnDeathDestroyLogoutPoint(Player __instance)
  {
    if (!__instance.m_nview.IsOwner())
    {
      return;
    }

    LocationController.RemoveLogoutZdo(__instance);
  }

  [HarmonyPatch(typeof(Player), "ShowTeleportAnimation")]
  [HarmonyPostfix]
  private static void ShowTeleportAnimation(bool __result)
  {
    var isRespawnTeleporting =
      PlayerSpawnController.Instance?.IsTeleportingToDynamicLocation ?? false;
    if (isRespawnTeleporting)
    {
      __result = false;
    }
  }

  // [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
  // [HarmonyPrefix]
  // private static bool FindSpawnPoint(Game __instance, bool __result, out Vector3 point,
  //   out bool usedLogoutPoint, float dt)
  // {
  //   usedLogoutPoint = false;
  //
  //   if (PlayerSpawnController.Instance && __instance.m_respawnAfterDeath)
  //   {
  //     var offset = DynamicLocations.GetSpawnTargetZdoOffset(Player.m_localPlayer);
  //     var zdoid = DynamicLocations.GetSpawnTargetZdo(Player.m_localPlayer);
  //     if (zdoid == null)
  //     {
  //       point = Vector3.zero;
  //       return true;
  //     }
  //
  //     ZDOMan.instance.RequestZDO((ZDOID)zdoid);
  //     var spawnZdo = ZDOMan.instance.GetZDO((ZDOID)zdoid);
  //
  //     if (spawnZdo != null)
  //     {
  //       __instance.m_respawnWait += dt;
  //       usedLogoutPoint = false;
  //
  //       point = spawnZdo.m_position + offset;
  //       __result = true;
  //       return false;
  //     }
  //   }
  //
  //   point = Vector3.zero;
  //   return true;
  // }

  [HarmonyPatch(typeof(ZNetScene), "Awake")]
  [HarmonyPostfix]
  private static void AddSpawnController(Game __instance)
  {
    __instance.gameObject.AddComponent<LocationController>();
    __instance.gameObject.AddComponent<PlayerSpawnController>();
  }

  [HarmonyPatch(typeof(ZNetScene), "Destroy")]
  [HarmonyPostfix]
  private static void ResetSpawnController(ZNetScene __instance)
  {
    LocationController.ResetCachedValues();
    // may not need to call this provided ZNetScene already calls onDestroy
    Object.Destroy(PlayerSpawnController.Instance);
  }


  [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
  [HarmonyPostfix]
  private static void OnFindSpawnPoint(Game __instance)
  {
    var spawnType = PlayerSpawnController.GetLocationType(__instance);
    var output = PlayerSpawnController.Instance?.OnFindSpawnPoint(spawnType);
    if (output != null)
    {
      __instance.m_respawnAfterDeath = true;
    }
  }


  /// <summary>
  /// Patches request respawn so the respawn time is customized and much faster.
  /// </summary>
  /// <notes>GPT-4 Generated</notes>
  /// <param name="instructions"></param>
  /// <returns></returns>
  [HarmonyPatch(typeof(Game), nameof(Game.RequestRespawn))]
  [HarmonyTranspiler]
  // The transpiler method
  public static IEnumerable<CodeInstruction> Transpiler(
    IEnumerable<CodeInstruction> instructions)
  {
    var codes = new List<CodeInstruction>(instructions);

    // bails if the config is disabled.
    if (!DynamicLocationsConfig.HasCustomSpawnDelay.Value) return codes;

    // Loop through instructions to find the Invoke call
    for (var i = 0; i < codes.Count; i++)
    {
      // Look for the Invoke call
      if (codes[i].opcode == OpCodes.Call &&
          codes[i].operand is MethodInfo methodInfo &&
          methodInfo.Name == "Invoke")
      {
        // Replace the delay argument before the Invoke call
        // Assuming the delay is the second argument, we replace it with 0
        // Move back two instructions: ldarg.1 (delay) and replace it with ldc.r4 0

        // Insert the 0 before the Invoke call
        codes.Insert(i - 1,
          new CodeInstruction(OpCodes.Ldc_R4,
            DynamicLocationsConfig.CustomSpawnDelay.Value));

        // The original delay argument will still be on the stack, so we need to remove it
        codes.RemoveAt(i); // Remove the call to Invoke
        break; // No need to continue searching after we've modified
      }
    }

    return codes.AsEnumerable();
  }


  /// <summary>
  /// Noting that I could override Game.FindSpawnPoint and PlayerProfile.HaveLogoutPoint to return the updated data. Updating this would then force the game to load in the correct location.
  ///
  /// Limitations
  /// - requested point would have to be accurate but since the check runs ever FixedUpdate, it could not have asynchronous task data, meaning it still needs a coroutine for loading the ZDO + setting the area to load   
  /// </summary>
  /// <param name="__instance"></param>
  /// <param name="__result"></param>
  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  private static void OnSpawned(Game __instance, Player __result)
  {
#if DEBUG
    // speed up debugging.
    Game.instance.m_fadeTimeDeath = 0;
    Player.m_localPlayer.m_flyFastSpeed = 60;
#endif

    if (ZNetView.m_forceDisableInit) return;
    Character character = __result;

    if (__instance.m_respawnAfterDeath &&
        DynamicLocationsConfig.EnableDynamicSpawnPoint.Value &&
        !character.InIntro())
    {
      PlayerSpawnController.Instance?.MovePlayerToSpawnPoint();
    }
    else
    {
      if (DynamicLocationsConfig.EnableDynamicLogoutPoint.Value &&
          !character.InInterior() && !character.InIntro())
      {
        PlayerSpawnController.Instance?.MovePlayerToLogoutPoint();
      }
    }
  }
}