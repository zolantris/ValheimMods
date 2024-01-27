using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimRAFT.Patches;

public class Player_Patch
{
  [HarmonyPatch(typeof(Player), "PlacePiece")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> PlacePiece(IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].operand != null && list[i].operand.ToString() ==
          "UnityEngine.GameObject Instantiate[GameObject](UnityEngine.GameObject, UnityEngine.Vector3, UnityEngine.Quaternion)")
      {
        list.InsertRange(i + 2, new CodeInstruction[3]
        {
          new(OpCodes.Ldarg_0),
          new(OpCodes.Ldloc_3),
          new(OpCodes.Call, AccessTools.Method(typeof(Player_Patch), nameof(PlacedPiece)))
        });
        break;
      }

    return list;
  }

  private static void PlacedPiece(Player player, GameObject gameObject)
  {
    var piece = gameObject.GetComponent<Piece>();
    if (!piece) return;
    var rb = piece.GetComponentInChildren<Rigidbody>();
    if (((bool)rb && !rb.isKinematic) || !PatchSharedData.PlayerLastRayPiece) return;
    var netView = piece.GetComponent<ZNetView>();
    if ((bool)netView)
    {
      var cul = PatchSharedData.PlayerLastRayPiece.GetComponent<CultivatableComponent>();
      if ((bool)cul) cul.AddNewChild(netView);
    }

    var bvc = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
    {
      if ((bool)netView)
      {
        Logger.LogDebug($"BaseVehicleController: adding new piece {piece.name} {gameObject.name}");
        bvc.AddNewPiece(netView);
      }
      else
      {
        Logger.LogDebug("BaseVehicleController: adding temp piece");
        bvc.AddTemporaryPiece(piece);
      }

      return;
    }

    var mb = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mb)
    {
      if ((bool)netView)
      {
        Logger.LogDebug($"adding new piece {piece.name} {gameObject.name}");
        mb.AddNewPiece(netView);
      }
      else
      {
        Logger.LogDebug("adding temp piece");
        mb.AddTemporaryPiece(piece);
      }
    }
  }

  private static bool HandleGameObjectRayCast(Transform transform, LayerMask layerMask,
    Player __instance, ref bool __result,
    ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface,
    bool water)
  {
    if ((bool)transform)
    {
      var localPos = transform.transform.InverseTransformPoint(__instance.transform.position);
      var start = localPos + Vector3.up * 2f;
      start = transform.transform.TransformPoint(start);
      var localDir = ((Character)__instance).m_lookYaw * Quaternion.Euler(__instance.m_lookPitch,
        0f - transform.transform.rotation.eulerAngles.y + PatchSharedData.YawOffset, 0f);
      var end = transform.transform.rotation * localDir * Vector3.forward;
      if (Physics.Raycast(start, end, out var hitInfo, 10f, layerMask) && (bool)hitInfo.collider)
      {
        var mbrTarget = hitInfo.collider.GetComponentInParent<MoveableBaseRootComponent>();
        var bvcTarget = hitInfo.collider.GetComponentInParent<BaseVehicleController>();
        if ((bool)mbrTarget || (bool)bvcTarget)
        {
          point = hitInfo.point;
          normal = hitInfo.normal;
          piece = hitInfo.collider.GetComponentInParent<Piece>();
          heightmap = null;
          waterSurface = null;
          __result = true;
          return false;
        }
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPrefix]
  private static bool PieceRayTest(Player __instance, ref bool __result, ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface,
    bool water)
  {
    var layerMask = __instance.m_placeRayMask;

    var bvc = __instance.GetComponentInParent<BaseVehicleController>();
    if ((bool)bvc)
      return HandleGameObjectRayCast(bvc.transform, layerMask, __instance, ref __result, ref point,
        ref normal, ref piece,
        ref heightmap,
        ref waterSurface, water);

    var mbr = __instance.GetComponentInParent<MoveableBaseRootComponent>();
    if ((bool)mbr)
    {
      return HandleGameObjectRayCast(mbr.transform, layerMask, __instance, ref __result, ref point,
        ref normal, ref piece,
        ref heightmap,
        ref waterSurface, water);
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "Save")]
  [HarmonyPrefix]
  private static void Player_Save(Player __instance, ZPackage pkg)
  {
    if ((bool)((Character)__instance).m_lastGroundCollider &&
        ((Character)__instance).m_lastGroundTouch < 0.3f)
    {
      MoveableBaseRootComponent.AddDynamicParent((__instance).m_nview,
        (__instance).m_lastGroundCollider.gameObject);
      BaseVehicleController.AddDynamicParent((__instance).m_nview,
        (__instance).m_lastGroundCollider.gameObject);
    }
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPostfix]
  private static void PieceRayTestPostfix(Player __instance, ref bool __result, ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap, ref Collider waterSurface,
    bool water)
  {
    PatchSharedData.PlayerLastRayPiece = piece;
  }

  [HarmonyPatch(typeof(Player), "FindHoverObject")]
  [HarmonyPrefix]
  private static bool FindHoverObject(Player __instance, ref GameObject hover,
    ref Character hoverCreature)
  {
    hover = null;
    hoverCreature = null;
    var array = Physics.RaycastAll(GameCamera.instance.transform.position,
      GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
    Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    var array2 = array;
    for (var i = 0; i < array2.Length; i++)
    {
      var raycastHit = array2[i];
      if ((bool)raycastHit.collider.attachedRigidbody &&
          raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject) continue;
      if (hoverCreature == null)
      {
        var character = raycastHit.collider.attachedRigidbody
          ? raycastHit.collider.attachedRigidbody.GetComponent<Character>()
          : raycastHit.collider.GetComponent<Character>();
        if (character != null) hoverCreature = character;
      }

      if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) <
          __instance.m_maxInteractDistance)
      {
        if (raycastHit.collider.GetComponent<Hoverable>() != null)
          hover = raycastHit.collider.gameObject;
        else if ((bool)raycastHit.collider.attachedRigidbody && (!(bool)raycastHit.collider
                   .attachedRigidbody.GetComponent<MoveableBaseRootComponent>() || !(bool)raycastHit
                   .collider
                   .attachedRigidbody.GetComponent<BaseVehicleController>()))
          hover = raycastHit.collider.attachedRigidbody.gameObject;
        else
          hover = raycastHit.collider.gameObject;
      }

      break;
    }

    RopeAnchorComponent.m_draggingRopeTo = null;
    if ((bool)hover && (bool)RopeAnchorComponent.m_draggingRopeFrom)
    {
      RopeAnchorComponent.m_draggingRopeTo = hover;
      hover = RopeAnchorComponent.m_draggingRopeFrom.gameObject;
    }

    return false;
  }

  [HarmonyPatch(typeof(Player), "AttachStop")]
  [HarmonyPrefix]
  private static void AttachStop(Player __instance)
  {
    if (__instance.IsAttached() && (bool)__instance.m_attachPoint &&
        (bool)__instance.m_attachPoint.parent)
    {
      var ladder = __instance.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
      if ((bool)ladder) ladder.StepOffLadder(__instance);
      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
      ((Character)__instance).m_animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }
  }

  // Logic for anchor needs to be moved to the Update method instead of fixed update which SetControls is called in
  [HarmonyPatch(typeof(Player), "SetControls")]
  [HarmonyPrefix]
  private static bool SetControls(Player __instance, Vector3 movedir, bool attack, bool attackHold,
    bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch, bool run,
    bool autoRun)
  {
    if (__instance.IsAttached() && (bool)__instance.m_attachPoint &&
        (bool)__instance.m_attachPoint.parent)
    {
      if (movedir.x == 0f && movedir.y == 0f && !jump && !crouch && !attack && !attackHold &&
          !secondaryAttack && !block)
      {
        var ladder = __instance.m_attachPoint.parent.GetComponent<RopeLadderComponent>();
        if ((bool)ladder)
        {
          ladder.MoveOnLadder(__instance, movedir.z);
          return false;
        }
      }

      var rudder = __instance.m_attachPoint.parent.GetComponent<RudderComponent>();
      if ((bool)rudder && __instance.m_doodadController != null)
      {
        __instance.SetDoodadControlls(ref movedir, ref ((Character)__instance).m_lookDir, ref run,
          ref autoRun, blockHold);
        if (rudder.Controls != null)
        {
          // might be a problem....
          var waterVehicleController = rudder.GetComponentInParent<WaterVehicleController>();
          var wvc2 = rudder.GetComponent<WaterVehicleController>();
          if (waterVehicleController != null)
          {
            var anchorKey =
              (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "False" &&
               ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set")
                ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown()
                : ZInput
                  .GetButtonDown("Run");
            if (anchorKey || ZInput.GetButtonDown("JoyRun"))
            {
              Logger.LogDebug("Anchor button is down setting anchor");
              waterVehicleController.SetAnchor(
                !waterVehicleController.VehicleFlags.HasFlag(
                  MoveableBaseShipComponent.MBFlags.IsAnchored));
            }
            else if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
            {
              waterVehicleController.Ascend();
            }
            else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
            {
              waterVehicleController.Descent();
            }
          }
        }
        else if (rudder.Controls != null)
        {
          var mb = rudder.GetComponentInParent<MoveableBaseShipComponent>();
          // may break, this might need GetComponent
          if ((bool)mb)
          {
            var anchorKey =
              (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "False" &&
               ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() != "Not set")
                ? ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown()
                : ZInput
                  .GetButtonDown("Run");
            if (anchorKey || ZInput.GetButtonDown("JoyRun"))
            {
              Logger.LogDebug("Anchor button is down setting anchor");
              mb.SetAnchor(!mb.m_flags.HasFlag(MoveableBaseShipComponent.MBFlags.IsAnchored));
            }
            else if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
            {
              mb.Ascend();
            }
            else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
            {
              mb.Descent();
            }
          }
        }

        return false;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  [HarmonyTranspiler]
  private static IEnumerable<CodeInstruction> UpdatePlacementGhost(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.Method(typeof(Quaternion), "Euler", new[]
          {
            typeof(float),
            typeof(float),
            typeof(float)
          })))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Player_Patch), nameof(RelativeEuler)));
    return list;
  }

  private static Quaternion RelativeEuler(float x, float y, float z)
  {
    var rot = Quaternion.Euler(x, y, z);
    if (!PatchSharedData.PlayerLastRayPiece) return rot;
    var mbr = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<MoveableBaseRootComponent>();
    var bvc = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<BaseVehicleController>();
    if (!mbr && !bvc) return rot;
    if (bvc)
    {
      return bvc.transform.rotation * rot;
    }

    return mbr.transform.rotation * rot;
  }
}