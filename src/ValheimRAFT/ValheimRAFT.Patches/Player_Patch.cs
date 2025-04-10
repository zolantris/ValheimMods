using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Valheim.UI;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimRAFT.Patches;

public class Player_Patch
{
  /// <summary>
  /// TODO may need to add a TryPlacePiece patch to override if blocked by water on boat.
  /// </summary>
  /// <param name="instructions"></param>
  /// <returns></returns>
  [HarmonyTranspiler]
  [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
  private static IEnumerable<CodeInstruction> PlacePieceTranspiler(
    IEnumerable<CodeInstruction> instructions)
  {
    var operand = HarmonyPatchMethods.GetGenericMethod(
      typeof(Object), "Instantiate", 1,
      new System.Type[3]
      {
        typeof(System.Type),
        typeof(Vector3),
        typeof(Quaternion)
      }).MakeGenericMethod(typeof(GameObject));
    var matches = new CodeMatch[]
    {
      new(OpCodes.Call, operand)
    };
    return new CodeMatcher(instructions).MatchForward(true, matches)
      .Advance(1)
      .InsertAndAdvance(
        Transpilers.EmitDelegate<System.Func<GameObject, GameObject>>(
          PlacedPiece))
      .InstructionEnumeration();
  }

  [HarmonyPatch(typeof(Player), nameof(Player.InRepairMode))]
  [HarmonyPostfix]
  public static void Player_InRepairMode(Player __instance, ref bool __result)
  {
    if (__instance.InPlaceMode())
    {
      var selectedPiece = __instance.m_buildPieces.GetSelectedPiece();
      if (selectedPiece != null)
      {
        __result = selectedPiece.m_repairPiece || selectedPiece.m_removePiece;
        return;
      }
    }
    __result = false;
  }

  [HarmonyPatch(typeof(HammerItemElement), nameof(HammerItemElement.IsHammer))]
  [HarmonyPostfix]
  public static void HammerItemElement_IsHammer(HammerItemElement __instance, ItemDrop.ItemData item, bool __result)
  {
    if (
      item.m_shared.m_name == PrefabNames.VehicleHammer)
    {
      // ReSharper disable once RedundantAssignment
      __result = true;
    }
  }

  public static void HidePreviewComponent(ZNetView netView)
  {
    if (!PrefabNames.IsVehicle(netView.name)) return;
    var vehicleShip = netView.GetComponent<VehicleShip>();
    if (vehicleShip == null) return;
    var ghostContainer = vehicleShip.GhostContainer();
    if (ghostContainer != null)
    {
      ghostContainer.SetActive(false);
    }
  }

  public static GameObject PlacedPiece(GameObject gameObject)
  {
    var piece = gameObject.GetComponent<Piece>();

    if (!piece) return gameObject;
    if (PrefabNames.IsVehicle(gameObject.name)) return gameObject;

    var rb = piece.GetComponentInChildren<Rigidbody>();
    var netView = piece.GetComponent<ZNetView>();

    if (netView != null)
    {
      HidePreviewComponent(netView);
    }

    if ((bool)rb && !rb.isKinematic || !PatchSharedData.PlayerLastRayPiece)
    {
      return gameObject;
    }

    if (netView != null)
    {
      var cul = PatchSharedData.PlayerLastRayPiece?
        .GetComponent<CultivatableComponent>();
      if (cul != null) cul.AddNewChild(netView);
    }

    var bvc = PatchSharedData.PlayerLastRayPiece?.transform?.parent?
      .GetComponent<VehiclePiecesController>() ?? null;
    if (bvc != null)
    {
      if (netView != null)
      {
        Logger.LogDebug(
          $"BaseVehicleController: adding new piece {piece.name} {gameObject.name}");
        bvc.AddNewPiece(netView);
      }
      else
      {
        Logger.LogDebug("BaseVehicleController: adding temp piece");
        bvc.SetPieceToParent(piece.transform);
      }

      return gameObject;
    }

    // TODO V4.0.0 remove all these references.
    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      var mb = PatchSharedData.PlayerLastRayPiece
        .GetComponentInParent<MoveableBaseRootComponent>();
      if (mb != null)
      {
        if (netView != null)
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

    return gameObject;
  }

  public static bool HandleGameObjectRayCast(Transform transform,
    LayerMask layerMask,
    Player __instance, ref bool __result,
    ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap,
    ref Collider waterSurface,
    bool water)
  {
    if ((bool)transform)
    {
      var localPos =
        transform.transform.InverseTransformPoint(__instance.transform
          .position);
      var start = localPos + Vector3.up * 2f;
      start = transform.transform.TransformPoint(start);
      var localDir = ((Character)__instance).m_lookYaw * Quaternion.Euler(
        __instance.m_lookPitch,
        0 - transform.transform.rotation.eulerAngles.y +
        PatchSharedData.YawOffset, 0);
      var end = transform.transform.rotation * localDir * Vector3.forward;
      if (Physics.Raycast(start, end, out var hitInfo, 10f, layerMask) &&
          (bool)hitInfo.collider)
      {
        var target =
          hitInfo.collider.GetComponentInParent<VehiclePiecesController>() ??
          (Object)hitInfo.collider
            .GetComponentInParent<MoveableBaseRootComponent>();

        if (target == null) return true;

        point = hitInfo.point;
        normal = hitInfo.normal;
        piece = hitInfo.collider.GetComponentInParent<Piece>();
        heightmap = null;
        waterSurface = null;
        __result = true;
        return true;
      }
    }

    return true;
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPrefix]
  public static bool PieceRayTest(Player __instance, ref bool __result,
    ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap,
    ref Collider waterSurface,
    bool water)
  {
    var layerMask = __instance.m_placeRayMask;

    var bvc =
      VehiclePiecesController.GetPieceControllerFromPlayer(
        __instance.gameObject);

    if (bvc != null)
      return HandleGameObjectRayCast(bvc.transform, layerMask, __instance,
        ref __result, ref point,
        ref normal, ref piece,
        ref heightmap,
        ref waterSurface, water);

    if (ValheimRaftPlugin.Instance.AllowOldV1RaftRecipe.Value)
    {
      var mbr = __instance.transform.root
        .GetComponent<MoveableBaseRootComponent>();
      if ((bool)mbr)
      {
        return HandleGameObjectRayCast(mbr.transform, layerMask, __instance,
          ref __result,
          ref point,
          ref normal, ref piece,
          ref heightmap,
          ref waterSurface, water);
      }
    }

    return true;
  }


  [HarmonyPatch(typeof(Player), "Save")]
  [HarmonyPrefix]
  public static void Player_Save(Player __instance, ZPackage pkg)
  {
    if ((bool)((Character)__instance).m_lastGroundCollider &&
        ((Character)__instance).m_lastGroundTouch < 0.3f)
    {
      if (!VehiclePiecesController.AddTempParent(__instance.m_nview,
            __instance.m_lastGroundCollider.gameObject))
      {
        MoveableBaseRootComponent.AddDynamicParent(__instance.m_nview,
          __instance.m_lastGroundCollider.gameObject);
      }
    }
  }

  [HarmonyPatch(typeof(Player), "PieceRayTest")]
  [HarmonyPostfix]
  public static void PieceRayTestPostfix(Player __instance, ref bool __result,
    ref Vector3 point,
    ref Vector3 normal, ref Piece piece, ref Heightmap heightmap,
    ref Collider waterSurface,
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
    System.Array.Sort(array,
      (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    var array2 = array;
    for (var i = 0; i < array2.Length; i++)
    {
      var raycastHit = array2[i];
      if ((bool)raycastHit.collider.attachedRigidbody &&
          raycastHit.collider.attachedRigidbody.gameObject ==
          __instance.gameObject) continue;
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
        else if ((bool)raycastHit.collider.attachedRigidbody && !raycastHit
                   .collider
                   .attachedRigidbody
                   .GetComponent<MoveableBaseRootComponent>() && !raycastHit
                   .collider
                   .attachedRigidbody.GetComponent<VehiclePiecesController>())
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
  public static bool AttachStop(Player __instance)
  {
    if (__instance.IsAttached() && (bool)__instance.m_attachPoint &&
        (bool)__instance.m_attachPoint.parent)
    {
      var ladder = __instance.m_attachPoint.parent
        .GetComponent<RopeLadderComponent>();
      if ((bool)ladder) ladder.OnStepOffLadder(__instance);
      ((Character)__instance).m_animator.SetIKPositionWeight(
        AvatarIKGoal.LeftHand, 0);
      ((Character)__instance).m_animator.SetIKPositionWeight(
        AvatarIKGoal.RightHand, 0);
      ((Character)__instance).m_animator.SetIKRotationWeight(
        AvatarIKGoal.LeftHand, 0);
      ((Character)__instance).m_animator.SetIKRotationWeight(
        AvatarIKGoal.RightHand, 0);
    }

    // always call this as the player might detach and not be in the vehicle briefly.
    WaterZoneUtils.RestoreColliderCollisionsAfterDetach(__instance);

    return true;
  }

  /**
   * todo migrate to a hotkey handler
   * This way of detecting keys is much more efficient and is not bogged down my Component getters
   */
  private static bool GetAnchorKey()
  {
    return VehicleMovementController.GetAnchorKeyDown();
  }

  // Logic for anchor needs to be moved to the Update method instead of fixed update which SetControls is called in
  [HarmonyPatch(typeof(Player), "SetControls")]
  [HarmonyPrefix]
  public static bool SetControls(Player __instance, Vector3 movedir,
    bool attack, bool attackHold,
    bool secondaryAttack, bool block, bool blockHold, bool jump, bool crouch,
    bool run,
    bool autoRun)
  {
    var isAttached = __instance.IsAttached();
    var shouldHandle = isAttached && (bool)__instance.m_attachPoint &&
                       (bool)__instance.m_attachPoint.parent;
    if (!shouldHandle) return true;
    if (movedir.x == 0f && movedir.y == 0f && !jump && !crouch && !attack &&
        !attackHold &&
        !secondaryAttack && !block)
    {
      var ladder = __instance.m_attachPoint.parent
        .GetComponent<RopeLadderComponent>();
      if ((bool)ladder)
      {
        ladder.MoveOnLadder(__instance, movedir.z);
        return false;
      }
    }

    var wheel = __instance.m_attachPoint.parent
      .GetComponent<SteeringWheelComponent>();

    if (!(bool)wheel || __instance.m_doodadController == null) return true;

    __instance.SetDoodadControlls(ref movedir,
      ref ((Character)__instance).m_lookDir, ref run,
      ref autoRun, blockHold);
    return false;
  }

  [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> UpdatePlacementGhost(
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
          AccessTools.Method(typeof(VehicleRotionHelpers),
            nameof(VehicleRotionHelpers.RelativeEuler)));
    return list;
  }

  [HarmonyPatch(typeof(Player), "GetControlledShip")]
  [HarmonyPrefix]
  public static bool GetControlledShip(Player __instance, object? __result)
  {
    /*
     * This patch protects against the type case used in the original GetControlledShip which prevents controls overrides from triggering hud.
     */
    var vvShipResult = HandleGetControlledShip(__instance);

    if (vvShipResult == null)
    {
      return true;
    }

    __result = vvShipResult;
    return false;
  }

  public static object? HandleGetControlledShip()
  {
    return HandleGetControlledShip(Player.m_localPlayer);
  }

  public static object? HandleGetControlledShip(Player player)
  {
    var hasDoodadController = player.m_doodadController != null;
    var isShipWheelControllerValid =
      player.m_doodadController?.IsValid() ?? false;
    var controlledComponent =
      player.m_doodadController?.GetControlledComponent();

    if (controlledComponent != null &&
        controlledComponent.name.Contains(PrefabNames.MBRaft))
    {
      return controlledComponent;
    }

    var vvShipResult =
      hasDoodadController && isShipWheelControllerValid
        ? controlledComponent as IVehicleShip
        : null;

    return vvShipResult;
  }
}