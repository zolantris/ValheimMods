using DynamicLocations.Config;
using DynamicLocations.Controllers;
using HarmonyLib;

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

  [HarmonyPatch(typeof(Game), "Awake")]
  [HarmonyPostfix]
  private static void AddSpawnController(Game __instance)
  {
    __instance.gameObject.AddComponent<PlayerSpawnController>();
  }


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
    if (!PlayerSpawnController.Instance)
    {
      // adds to Game instead of Player which is destroyed on Spawn
      __instance.gameObject.AddComponent<PlayerSpawnController>();
    }

    if (__instance.m_respawnAfterDeath &&
        DynamicLocationsConfig.EnableDynamicSpawnPoint.Value)
    {
      PlayerSpawnController.Instance?.MovePlayerToSpawnPoint();
    }
    else
    {
      if (DynamicLocationsConfig.EnableDynamicLogoutPoint.Value)
      {
        PlayerSpawnController.Instance?.MovePlayerToLoginPoint();
      }
    }
  }
}