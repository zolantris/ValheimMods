// Decompiled with JetBrains decompiler
// Type: ValheimRAFT.Patches.Teleport_Patch
// Assembly: ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: B1A8BB6C-BD4E-4881-9FD4-7E1D68B1443D
// Assembly location: C:\Users\Frederick Engelhardt\Downloads\ValheimRAFT 1.4.9-1136-1-4-9-1692901079\ValheimRAFT\ValheimRAFT.dll

using HarmonyLib;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ValheimRAFT.Patches
{
  [HarmonyPatch]
  public class Teleport_Patch
  {
    public static Dictionary<Player, ZDOID> m_teleportTarget = new Dictionary<Player, ZDOID>();

    public static void TeleportToObject(
      Player __instance,
      Vector3 pos,
      Quaternion rot,
      ZDOID objectId)
    {
      if (!((Character) __instance).TeleportTo(pos, rot, true))
        return;
      Teleport_Patch.m_teleportTarget[__instance] = objectId;
    }

    public static void TeleportToActivePosition(TeleportWorld __instance, ZDOID playerId)
    {
      ZDO zdo = ZDOMan.instance.GetZDO(__instance.m_nview.m_zdo.GetConnectionZDOID((ZDOExtraData.ConnectionType) 1));
      if (zdo == null)
        return;
      ZNetView instance1 = ZNetScene.instance.FindInstance(zdo);
      Vector3 vector3_1 = Object.op_Implicit((Object) instance1) ? ((Component) instance1).transform.position : zdo.GetPosition();
      Quaternion rot = Object.op_Implicit((Object) instance1) ? ((Component) instance1).transform.rotation : zdo.GetRotation();
      Vector3 vector3_2 = Quaternion.op_Multiply(rot, Vector3.forward);
      Vector3 pos = Vector3.op_Addition(Vector3.op_Addition(vector3_1, Vector3.op_Multiply(vector3_2, __instance.m_exitDistance)), Vector3.up);
      GameObject instance2 = ZNetScene.instance.FindInstance(playerId);
      if (Object.op_Implicit((Object) instance2))
      {
        Player component = instance2.GetComponent<Player>();
        if (Object.op_Implicit((Object) component))
          Teleport_Patch.TeleportToObject(component, pos, rot, zdo.m_uid);
      }
    }

    [HarmonyPatch(typeof (TeleportWorld), "Teleport")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TeleportWorld_Teleport(
      IEnumerable<CodeInstruction> instructions)
    {
      bool flag = false;
      List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
      for (int index = 0; index < list.Count; ++index)
      {
        if (CodeInstructionExtensions.Calls(list[index], AccessTools.Method(typeof (Character), "TeleportTo", (Type[]) null, (Type[]) null)))
        {
          list[index] = new CodeInstruction(OpCodes.Call, (object) AccessTools.Method(typeof (Teleport_Patch), "Player_TeleportTo", (Type[]) null, (Type[]) null));
          list.Insert(index, new CodeInstruction(OpCodes.Ldarg_0, (object) null));
          flag = true;
          break;
        }
      }
      if (!flag)
        Logger.LogWarning((object) "TeleportWorld patch failed.");
      return (IEnumerable<CodeInstruction>) list;
    }

    public static bool Player_TeleportTo(
      Player player,
      Vector3 pos,
      Quaternion rot,
      bool distantTeleport,
      TeleportWorld __instance)
    {
      Teleport_Patch.TeleportToActivePosition(__instance, ((Character) player).m_nview.m_zdo.m_uid);
      return true;
    }

    [HarmonyPatch(typeof (Player), "UpdateTeleport")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Player_UpdateTeleport(
      IEnumerable<CodeInstruction> instructions)
    {
      List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
      for (int index = 0; index < list.Count; ++index)
      {
        if (CodeInstructionExtensions.LoadsField(list[index], AccessTools.Field(typeof (Player), "m_teleportTargetPos"), false))
          list[index] = new CodeInstruction(OpCodes.Call, (object) AccessTools.Method(typeof (Teleport_Patch), "GetTeleportTargetPos", (Type[]) null, (Type[]) null));
        if (CodeInstructionExtensions.StoresField(list[index], AccessTools.Field(typeof (Player), "m_teleporting")))
          list[index] = new CodeInstruction(OpCodes.Call, (object) AccessTools.Method(typeof (Teleport_Patch), "SetIsTeleporting", (Type[]) null, (Type[]) null));
      }
      return (IEnumerable<CodeInstruction>) list;
    }

    private static Vector3 GetTeleportTargetPos(Player __instance)
    {
      ZDOID zdoid;
      if (Teleport_Patch.m_teleportTarget.TryGetValue(__instance, out zdoid))
      {
        GameObject instance = ZNetScene.instance.FindInstance(zdoid);
        if (Object.op_Implicit((Object) instance))
          return Teleport_Patch.GetTeleportPosition(instance);
      }
      return __instance.m_teleportTargetPos;
    }

    private static Vector3 GetTeleportPosition(GameObject go)
    {
      TeleportWorld component = go.GetComponent<TeleportWorld>();
      return Object.op_Implicit((Object) component) ? Vector3.op_Addition(Vector3.op_Addition(((Component) component).transform.position, Vector3.op_Multiply(((Component) component).transform.forward, component.m_exitDistance)), Vector3.up) : go.transform.position;
    }

    private static void SetIsTeleporting(Player __instance, bool isTeleporting)
    {
      __instance.m_teleporting = isTeleporting;
      ZDOID zdoid;
      if (isTeleporting || !Teleport_Patch.m_teleportTarget.TryGetValue(__instance, out zdoid))
        return;
      ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
      if (zdo != null)
      {
        ZNetView instance = ZNetScene.instance.FindInstance(zdo);
        if (!Object.op_Implicit((Object) instance))
        {
          __instance.m_teleportTimer = 8f;
          __instance.m_teleporting = true;
          return;
        }
        ((Component) __instance).transform.position = Teleport_Patch.GetTeleportPosition(((Component) instance).gameObject);
      }
      Teleport_Patch.m_teleportTarget.Remove(__instance);
    }
  }
}
