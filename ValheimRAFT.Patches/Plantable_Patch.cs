using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ValheimRAFT.Patches
{
  [HarmonyPatch]
  public class Plantable_Patch
  {
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UpdatePlacementGhost(
      IEnumerable<CodeInstruction> instructions,
      ILGenerator generator)
    {
      List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
      for (int index1 = 0; index1 < list.Count; ++index1)
      {
        if (CodeInstructionExtensions.StoresField(list[index1],
              AccessTools.Field(typeof(Player), "m_placementStatus")) &&
            CodeInstructionExtensions.LoadsConstant(list[index1 - 1],
              (Enum)(object)(Player.PlacementStatus)9))
        {
          List<Label> labels = CodeInstructionExtensions.ExtractLabels(list[index1 - 2]);
          object obj = (object)null;
          MethodInfo methodInfo =
            AccessTools.Method(typeof(Player), "PieceRayTest", (Type[])null, (Type[])null);
          for (int index2 = 0; index2 < list.Count; ++index2)
          {
            if (CodeInstructionExtensions.Calls(list[index2], methodInfo) &&
                CodeInstructionExtensions.IsLdloc(list[index2 - 4], (LocalBuilder)null))
            {
              obj = list[index2 - 4].operand;
              break;
            }
          }

          Label label = generator.DefineLabel();
          Label sourceJump = (Label)list[index1 - 3].operand;
          list.Find((Predicate<CodeInstruction>)(match => match.labels.Contains(sourceJump))).labels
            .Add(label);
          list.InsertRange(index1 - 2, (IEnumerable<CodeInstruction>)new CodeInstruction[3]
          {
            CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Ldloc, obj),
              (IEnumerable<Label>)labels),
            new CodeInstruction(OpCodes.Call,
              (object)AccessTools.Method(typeof(Plantable_Patch), "IsCultivated", (Type[])null,
                (Type[])null)),
            new CodeInstruction(OpCodes.Brtrue, (object)label)
          });
          break;
        }
      }

      return (IEnumerable<CodeInstruction>)list;
    }

    private static bool IsCultivated(Piece piece)
    {
      if (!piece)
        return false;
      CultivatableComponent component = ((Component)piece).GetComponent<CultivatableComponent>();
      return component && component.isCultivatable;
    }

    [HarmonyPatch(typeof(Plant), "Grow")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Plant_Grow(
      IEnumerable<CodeInstruction> instructions)
    {
      List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
      for (int index = 0; index < list.Count; ++index)
      {
        if (CodeInstructionExtensions.Calls(list[index], AccessTools.Method(typeof(GameObject),
              "GetComponent", new Type[0], new Type[1]
              {
                typeof(ZNetView)
              })))
        {
          list[index] = new CodeInstruction(OpCodes.Call,
            (object)AccessTools.Method(typeof(Plantable_Patch), "PlantGrowth", (Type[])null,
              (Type[])null));
          list.Insert(index, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
          break;
        }
      }

      return (IEnumerable<CodeInstruction>)list;
    }

    private static ZNetView PlantGrowth(GameObject newObject, Plant oldPlant)
    {
      ZNetView component = newObject.GetComponent<ZNetView>();
      MoveableBaseRootComponent componentInParent =
        ((Component)oldPlant).GetComponentInParent<MoveableBaseRootComponent>();
      if (component && componentInParent)
        componentInParent.AddNewPiece(component);
      int parentId = CultivatableComponent.GetParentID(oldPlant.m_nview);
      if (parentId != 0)
        CultivatableComponent.AddNewChild(parentId, component);
      return component;
    }

    [HarmonyPatch(typeof(Plant), "UpdateHealth")]
    [HarmonyPrefix]
    private static bool Plant_UpdateHealth(Plant __instance, double timeSincePlanted)
    {
      if (CultivatableComponent.GetParentID(__instance.m_nview) == 0)
        return true;
      __instance.m_status = (Plant.Status)0;
      return false;
    }
  }
}