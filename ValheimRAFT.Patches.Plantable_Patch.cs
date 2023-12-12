// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.Patches.Plantable_Patch
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimRAFT;
using ValheimRAFT.Patches;

[HarmonyPatch]
public class Plantable_Patch
{
	[HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> UpdatePlacementGhost(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (!CodeInstructionExtensions.StoresField(list[i], AccessTools.Field(typeof(Player), "m_placementStatus")) || !CodeInstructionExtensions.LoadsConstant(list[i - 1], (Enum)(object)(PlacementStatus)9))
			{
				continue;
			}
			List<Label> labels = CodeInstructionExtensions.ExtractLabels(list[i - 2]);
			object pieceLocalIndex = null;
			MethodInfo rayPieceMethod = AccessTools.Method(typeof(Player), "PieceRayTest", (Type[])null, (Type[])null);
			for (int j = 0; j < list.Count; j++)
			{
				if (CodeInstructionExtensions.Calls(list[j], rayPieceMethod) && CodeInstructionExtensions.IsLdloc(list[j - 4], (LocalBuilder)null))
				{
					pieceLocalIndex = list[j - 4].operand;
					break;
				}
			}
			Label targetLabel = generator.DefineLabel();
			Label sourceJump = (Label)list[i - 3].operand;
			list.Find((CodeInstruction match) => match.labels.Contains(sourceJump)).labels.Add(targetLabel);
			list.InsertRange(i - 2, (IEnumerable<CodeInstruction>)(object)new CodeInstruction[3]
			{
				CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Ldloc, pieceLocalIndex), (IEnumerable<Label>)labels),
				new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(Plantable_Patch), "IsCultivated", (Type[])null, (Type[])null)),
				new CodeInstruction(OpCodes.Brtrue, (object)targetLabel)
			});
			break;
		}
		return list;
	}

	private static bool IsCultivated(Piece piece)
	{
		if (!Object.op_Implicit((Object)(object)piece))
		{
			return false;
		}
		CultivatableComponent cmp = ((Component)piece).GetComponent<CultivatableComponent>();
		return Object.op_Implicit((Object)(object)cmp) && cmp.isCultivatable;
	}

	[HarmonyPatch(typeof(Plant), "Grow")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Plant_Grow(IEnumerable<CodeInstruction> instructions)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(list[i], AccessTools.Method(typeof(GameObject), "GetComponent", new Type[0], new Type[1] { typeof(ZNetView) })))
			{
				list[i] = new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(Plantable_Patch), "PlantGrowth", (Type[])null, (Type[])null));
				list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
				break;
			}
		}
		return list;
	}

	private static ZNetView PlantGrowth(GameObject newObject, Plant oldPlant)
	{
		ZNetView newPlantNetView = newObject.GetComponent<ZNetView>();
		MoveableBaseRootComponent mbr = ((Component)oldPlant).GetComponentInParent<MoveableBaseRootComponent>();
		if (Object.op_Implicit((Object)(object)newPlantNetView) && Object.op_Implicit((Object)(object)mbr))
		{
			mbr.AddNewPiece(newPlantNetView);
		}
		int cultivatable = CultivatableComponent.GetParentID(oldPlant.m_nview);
		if (cultivatable != 0)
		{
			CultivatableComponent.AddNewChild(cultivatable, newPlantNetView);
		}
		return newPlantNetView;
	}

	[HarmonyPatch(typeof(Plant), "UpdateHealth")]
	[HarmonyPrefix]
	private static bool Plant_UpdateHealth(Plant __instance, double timeSincePlanted)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (CultivatableComponent.GetParentID(__instance.m_nview) == 0)
		{
			return true;
		}
		__instance.m_status = (Status)0;
		return false;
	}
}
