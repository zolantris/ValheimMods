using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Jotunn;
using UnityEngine;

namespace ValheimRAFT.Patches;

[HarmonyPatch]
public class Teleport_Patch
{
	public static Dictionary<Player, ZDOID> m_teleportTarget = new Dictionary<Player, ZDOID>();

	public static void TeleportToObject(Player __instance, Vector3 pos, Quaternion rot, ZDOID objectId)
	{
		if (__instance.TeleportTo(pos, rot, distantTeleport: true))
		{
			m_teleportTarget[__instance] = objectId;
		}
	}

	public static void TeleportToActivePosition(TeleportWorld __instance, ZDOID playerId)
	{
		ZDO zDO = ZDOMan.instance.GetZDO(__instance.m_nview.m_zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal));
		if (zDO == null)
		{
			return;
		}
		ZNetView nv = ZNetScene.instance.FindInstance(zDO);
		Vector3 position = (nv ? nv.transform.position : zDO.GetPosition());
		Quaternion rotation = (nv ? nv.transform.rotation : zDO.GetRotation());
		Vector3 vector = rotation * Vector3.forward;
		Vector3 pos = position + vector * __instance.m_exitDistance + Vector3.up;
		GameObject playerGo = ZNetScene.instance.FindInstance(playerId);
		if ((bool)playerGo)
		{
			Player player = playerGo.GetComponent<Player>();
			if ((bool)player)
			{
				TeleportToObject(player, pos, rotation, zDO.m_uid);
			}
		}
	}

	[HarmonyPatch(typeof(TeleportWorld), "Teleport")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> TeleportWorld_Teleport(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Calls(AccessTools.Method(typeof(Character), "TeleportTo")))
			{
				list[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Teleport_Patch), "Player_TeleportTo"));
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

	public static bool Player_TeleportTo(Player player, Vector3 pos, Quaternion rot, bool distantTeleport, TeleportWorld __instance)
	{
		TeleportToActivePosition(__instance, ((Character)player).m_nview.m_zdo.m_uid);
		return true;
	}

	[HarmonyPatch(typeof(Player), "UpdateTeleport")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Player_UpdateTeleport(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].LoadsField(AccessTools.Field(typeof(Player), "m_teleportTargetPos")))
			{
				list[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Teleport_Patch), "GetTeleportTargetPos"));
			}
			if (list[i].StoresField(AccessTools.Field(typeof(Player), "m_teleporting")))
			{
				list[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Teleport_Patch), "SetIsTeleporting"));
			}
		}
		return list;
	}

	private static Vector3 GetTeleportTargetPos(Player __instance)
	{
		if (m_teleportTarget.TryGetValue(__instance, out var zdoid))
		{
			GameObject go = ZNetScene.instance.FindInstance(zdoid);
			if ((bool)go)
			{
				return GetTeleportPosition(go);
			}
		}
		return __instance.m_teleportTargetPos;
	}

	private static Vector3 GetTeleportPosition(GameObject go)
	{
		TeleportWorld tp = go.GetComponent<TeleportWorld>();
		if ((bool)tp)
		{
			return tp.transform.position + tp.transform.forward * tp.m_exitDistance + Vector3.up;
		}
		return go.transform.position;
	}

	private static void SetIsTeleporting(Player __instance, bool isTeleporting)
	{
		__instance.m_teleporting = isTeleporting;
		if (isTeleporting || !m_teleportTarget.TryGetValue(__instance, out var zdoid))
		{
			return;
		}
		ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
		if (zdo != null)
		{
			ZNetView go = ZNetScene.instance.FindInstance(zdo);
			if (!go)
			{
				__instance.m_teleportTimer = 8f;
				__instance.m_teleporting = true;
				return;
			}
			__instance.transform.position = GetTeleportPosition(go.gameObject);
		}
		m_teleportTarget.Remove(__instance);
	}
}
