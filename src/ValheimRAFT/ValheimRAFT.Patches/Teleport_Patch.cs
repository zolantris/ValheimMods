using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Jotunn;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Teleport_Patch
{
  public static Dictionary<Player, ZDOID> m_teleportTarget = new Dictionary<Player, ZDOID>();

  public static void TeleportToObject(Player __instance, Vector3 pos, Quaternion rot,
    ZDOID objectId)
  {
    if (__instance.TeleportTo(pos, rot, distantTeleport: true))
    {
      m_teleportTarget[__instance] = objectId;
    }
  }

  public static void TeleportToActivePosition(TeleportWorld __instance, ZDOID playerId)
  {
    var zDO = ZDOMan.instance.GetZDO(
      __instance.m_nview.m_zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
    if (zDO == null)
    {
      return;
    }

    var nv = ZNetScene.instance.FindInstance(zDO);
    var position = (nv ? nv.transform.position : zDO.GetPosition());
    var rotation = (nv ? nv.transform.rotation : zDO.GetRotation());
    var vector = rotation * Vector3.forward;
    var pos = position + vector * __instance.m_exitDistance + Vector3.up;
    var playerGo = ZNetScene.instance.FindInstance(playerId);
    if (!(bool)playerGo) return;

    var player = playerGo.GetComponent<Player>();
    if (!(bool)player) return;

    // very important, without this the player gets deactivated
    if (player.transform.parent != null)
    {
      player.transform.SetParent(null);
    }

    TeleportToObject(player, pos, rotation, zDO.m_uid);
  }

  [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> TeleportWorld_Teleport(
    IEnumerable<CodeInstruction> instructions)
  {
    bool found = false;
    List<CodeInstruction> list = instructions.ToList();
    for (int i = 0; i < list.Count; i++)
    {
      if (list[i].Calls(AccessTools.Method(typeof(Character), "TeleportTo")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch), nameof(Player_TeleportTo)));
        list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
        found = true;
        break;
      }
    }

    if (!found)
    {
      Jotunn.Logger.LogWarning("TeleportWorld patch failed.");
    }

    return list;
  }

  public static bool Player_TeleportTo(Player player, Vector3 pos, Quaternion rot,
    bool distantTeleport, TeleportWorld __instance)
  {
    TeleportToActivePosition(__instance, ((Character)player).m_nview.m_zdo.m_uid);
    return true;
  }

  [HarmonyPatch(typeof(Player), "UpdateTeleport")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Player_UpdateTeleport(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (int i = 0; i < list.Count; i++)
    {
      if (list[i].LoadsField(AccessTools.Field(typeof(Player), "m_teleportTargetPos")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch), nameof(GetTeleportTargetPos)));
      }

      if (list[i].StoresField(AccessTools.Field(typeof(Player), "m_teleporting")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch), nameof(SetIsTeleporting)));
      }
    }

    return list;
  }

  // [HarmonyPatch(typeof(Player), "UpdateTeleport")]
  // [HarmonyPrefix]
  // public static bool Player_UpdateTeleport(Player __instance, float dt)
  // {
  //   if (!__instance.m_teleporting)
  //   {
  //     __instance.m_teleportCooldown += dt;
  //     return false;
  //   }
  //
  //   __instance.m_teleportCooldown = 0f;
  //   __instance.m_teleportTimer += dt;
  //   if (!(__instance.m_teleportTimer > 2f))
  //   {
  //     return false;
  //   }
  //
  //   Vector3 dir = __instance.m_teleportTargetRot * Vector3.forward;
  //   __instance.transform.position = __instance.m_teleportTargetPos;
  //   __instance.transform.rotation = __instance.m_teleportTargetRot;
  //   __instance.m_body.velocity = Vector3.zero;
  //   __instance.m_maxAirAltitude = __instance.transform.position.y;
  //   __instance.SetLookDir(dir);
  //   if ((!(__instance.m_teleportTimer > 8f) && __instance.m_distantTeleport) ||
  //       !ZNetScene.instance.IsAreaReady(__instance.m_teleportTargetPos))
  //   {
  //     return false;
  //   }
  //
  //   float height = 0f;
  //   if (ZoneSystem.instance.FindFloor(__instance.m_teleportTargetPos, out height))
  //   {
  //     __instance.m_teleportTimer = 0f;
  //     __instance.m_teleporting = false;
  //     __instance.ResetCloth();
  //   }
  //   else if (__instance.m_teleportTimer > 15f || !__instance.m_distantTeleport)
  //   {
  //     if (__instance.m_distantTeleport)
  //     {
  //       Vector3 position = __instance.transform.position;
  //       position.y = ZoneSystem.instance.GetSolidHeight(__instance.m_teleportTargetPos) + 0.5f;
  //       __instance.transform.position = position;
  //     }
  //     else
  //     {
  //       __instance.transform.rotation = __instance.m_teleportFromRot;
  //       __instance.transform.position = __instance.m_teleportFromPos;
  //       __instance.m_maxAirAltitude = __instance.transform.position.y;
  //       __instance.Message(MessageHud.MessageType.Center, "$msg_portal_blocked");
  //     }
  //
  //     __instance.m_teleportTimer = 0f;
  //     __instance.m_teleporting = false;
  //     __instance.ResetCloth();
  //   }
  //
  //   return false;
  // }

  private static Vector3 GetTeleportTargetPos(Player __instance)
  {
    if (!m_teleportTarget.TryGetValue(__instance, out var zdoid))
      return __instance.m_teleportTargetPos;

    var go = ZNetScene.instance.FindInstance(zdoid);
    if ((bool)go)
    {
      return GetTeleportPosition(go);
    }

    return __instance.m_teleportTargetPos;
  }

  private static Vector3 GetTeleportPosition(GameObject go)
  {
    var tp = go.GetComponent<TeleportWorld>();

    // Might be required to get updated position
    // Physics.SyncTransforms();
    if ((bool)tp)
    {
      return tp.transform.position + tp.transform.forward * tp.m_exitDistance + Vector3.up;
    }

    return go.transform.position;
  }

  private static IEnumerator DebouncedTeleportCoordinateUpdater(Player __instance,
    bool isTeleporting, ZDOID zdoid)
  {
    var zdo = ZDOMan.instance.GetZDO(zdoid);
    if (zdo == null)
    {
      __instance.m_teleporting = false;
      m_teleportTarget.Remove(__instance);
      yield break;
    }

    ZNetView? go = null;

    var zoneId = ZoneSystem.instance.GetZone(zdo.m_position);

    while (go == null)
    {
      go = ZNetScene.instance.FindInstance(zdo);
      if (go) break;
      zoneId = ZoneSystem.instance.GetZone(zdo.m_position);
      ZoneSystem.instance.PokeLocalZone(zoneId);
      yield return new WaitForFixedUpdate();
    }

    zoneId = ZoneSystem.instance.GetZone(zdo.m_position);
    ZoneSystem.instance.PokeLocalZone(zoneId);
    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zoneId));

    var teleportPosition = GetTeleportPosition(go.gameObject);
    __instance.transform.position = teleportPosition;

    m_teleportTarget.Remove(__instance);
  }

  private static void SetIsTeleporting(Player __instance, bool isTeleporting)
  {
    __instance.m_teleporting = isTeleporting;
    if (isTeleporting || !m_teleportTarget.TryGetValue(__instance, out var zdoid))
    {
      return;
    }

    __instance.StopCoroutine(nameof(DebouncedTeleportCoordinateUpdater));
    __instance.StartCoroutine(DebouncedTeleportCoordinateUpdater(__instance, isTeleporting, zdoid));
  }
}