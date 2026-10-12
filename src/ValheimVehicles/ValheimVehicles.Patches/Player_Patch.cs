#region

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using System.Reflection.Emit;
  using HarmonyLib;
  using UnityEngine;
  using Valheim.UI;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Components;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Propulsion.Rudder;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.Controllers;
  using Zolantris.Shared;
  using Object = UnityEngine.Object;

#endregion

  namespace ValheimVehicles.Patches;

  public class Player_Patch
  {
    // /// <summary>
    // /// TODO may need to add a TryPlacePiece patch to override if blocked by water on boat.
    // /// </summary>
    // /// <param name="instructions"></param>
    // /// <returns></returns>
    // [HarmonyTranspiler]
    // [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    // private static IEnumerable<CodeInstruction> PlacePieceTranspiler(
    //   IEnumerable<CodeInstruction> instructions)
    // {
    //   var operand = HarmonyPatchMethods.GetGenericMethod(
    //     typeof(Object), "Instantiate", 1,
    //     new Type[3]
    //     {
    //       typeof(Type),
    //       typeof(Vector3),
    //       typeof(Quaternion)
    //     }).MakeGenericMethod(typeof(GameObject));
    //   var matches = new CodeMatch[]
    //   {
    //     new(OpCodes.Call, operand)
    //   };
    //   return new CodeMatcher(instructions).MatchForward(true, matches)
    //     .Advance(1)
    //     .InsertAndAdvance(
    //       Transpilers.EmitDelegate<Func<GameObject, GameObject>>(
    //         PlacedPiece))
    //     .InstructionEnumeration();
    // }

    // fix for MassFarming
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    private static IEnumerable<CodeInstruction> PlacePieceTranspiler(IEnumerable<CodeInstruction> instructions)
    {
      var codes = new List<CodeInstruction>(instructions);

      // Match the generic Instantiate<GameObject>(...)
      var instantiateGeneric = typeof(Object)
        .GetMethods()
        .First(m => m.Name == "Instantiate"
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 3)
        .MakeGenericMethod(typeof(GameObject));

      for (var i = 0; i < codes.Count; i++)
      {
        if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == instantiateGeneric)
        {
          codes[i] = CodeInstruction.Call(typeof(Player_Patch), nameof(WrapInstantiateWithPlacedPiece));
        }
      }

      return codes;
    }

    // [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    // [HarmonyPrefix]
    // [HarmonyPriority(Priority.High)]
    // // Use a Prefix to intercept the placement before it happens
    // public static bool PreventPlacementPatchIfDisabledPatch(Player __instance, Piece piece)
    // {
    //   // 1. Check if the player is currently hovering over or targeting a vehicle placement ghost
    //   if (__instance.m_placementGhost != null)
    //   {
    //     // ValheimRAFT structures are parented under a vehicle root object
    //     // Look for ValheimRAFT's vehicle component in the parent hierarchy
    //     var waterVehicle = __instance.m_placementGhost.GetComponentInParent<VehiclePiecesController>(); // Or MoveableBaseRoot depending on the targeted ValheimRAFT version
    //
    //     if (waterVehicle != null)
    //     {
    //       // 2. Fetch your custom logic or check ValheimRAFT's build mode state 
    //       // Replace 'YourCustomBuildModeCheck' with the exact boolean or property you are using to track the vehicle's state
    //       var container = piece.GetComponentInChildren<Container>();
    //       var isBuildModeDisabled = waterVehicle.Manager.BuildMode.CanPlacePiece(container != null);
    //
    //       if (isBuildModeDisabled)
    //       {
    //         // 3. Display an in-game error message to the player
    //         __instance.Message(MessageHud.MessageType.Center, "$msg_placement_disabled_vehicle");
    //
    //         // 4. Return false to skip Valheim's original PlacePiece execution
    //         return false;
    //       }
    //     }
    //   }
    //
    //   // Return true to allow normal piece placement if conditions aren't met
    //   return true;
    // }

    public static GameObject WrapInstantiateWithPlacedPiece(Object original, Vector3 pos, Quaternion rot)
    {
      if (ZNet.instance && ZNet.instance.IsDedicated())
      {
        LoggerProvider.LogWarning("[WrapInstantiateWithPlacedPiece] WARNING: This should not run on server!");
      }
      var obj = Object.Instantiate(original, pos, rot) as GameObject;
      return PlacedPiece(obj);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TryPlacePieceTranspiler(IEnumerable<CodeInstruction> instructions)
    {
      var codes = new List<CodeInstruction>(instructions);

      // FIX: Match Valheim's exact 5-parameter Message method signature
      var messageMethod = AccessTools.Method(
        typeof(Character),
        nameof(Character.Message),
        new Type[] { typeof(MessageHud.MessageType), typeof(string), typeof(int), typeof(Sprite), typeof(bool) }
      );

      if (messageMethod == null)
      {
        LoggerProvider.LogError("[TryPlacePieceTranspiler] CRITICAL ERROR: Could not find the 5-parameter Character.Message method!");
        return instructions;
      }

      var patchedCount = 0;
      for (var i = 0; i < codes.Count; i++)
      {
        // Find where Valheim hits a matching Message instruction call site
        if (codes[i].opcode == OpCodes.Call || codes[i].opcode == OpCodes.Callvirt)
        {
          if (codes[i].operand as MethodInfo == messageMethod)
          {
            // Swap the call address over to our interceptor method
            codes[i] = CodeInstruction.Call(typeof(Player_Patch), nameof(InterceptPlacementErrorMessage));
            patchedCount++;
          }
        }
      }

      LoggerProvider.LogInfo($"[TryPlacePieceTranspiler] Successfully patched {patchedCount} Message call sites inside TryPlacePiece.");
      return codes;
    }

    // FIX: Update this helper method signature to support all 5 parameters natively
    public static void InterceptPlacementErrorMessage(Character character, MessageHud.MessageType type, string originalMsg, int amount, Sprite icon, bool flash)
    {
      var player = character as Player;

      if (player != null && player.m_placementGhost != null && PatchSharedData.PlayerLastRayPiece != null)
      {
        var pieceController = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<VehiclePiecesController>();
        if (pieceController != null)
        {
          var ghostPiece = player.m_placementGhost.GetComponent<Piece>();
          if (ghostPiece != null)
          {
            var container = ghostPiece.GetComponentInChildren<Container>();
            var isBuildModeDisabled = !pieceController.Manager.BuildMode.CanPlacePiece(container != null);

            if (isBuildModeDisabled)
            {
              // Bypasses check for utility toggles or water floats
              var ghostName = player.m_placementGhost.name;
              if (!ghostName.StartsWith(PrefabNames.CustomWaterFloatation) && !ghostName.StartsWith(PrefabNames.BuildModeToggle))
              {
                // Push your vehicle custom localized center-screen warning string
                player.Message(
                  MessageHud.MessageType.Center,
                  Localization.instance.Localize("$valheim_vehicles_buildmode_unable_to_place_piece_message"),
                  amount,
                  icon,
                  flash
                );
                return; // Suppress and abort early so originalMsg is completely discarded!
              }
            }
          }
        }
      }

      // If it's a normal vanilla failure context (Workbench missing, territory block), let it flow normally
      character.Message(type, originalMsg, amount, icon, flash);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    public static void UpdatePlacementGhost_BlockBuildMode(Player __instance)
    {
      // Ensure there is an active ghost and we are hovering over an existing vehicle structural component
      if (__instance.m_placementGhost == null || PatchSharedData.PlayerLastRayPiece == null) return;

      // BYPASS: If they are trying to place a build mode toggle or a water flotation device, don't lock it out
      var ghostName = __instance.m_placementGhost.name;
      if (ghostName.StartsWith(PrefabNames.BuildModeToggle))
      {
        return; // let placing these prefabs happen as these are critical to toggling build mode and it would trap users.
      }

      // Use your tracked raycast history to discover if the target is attached to a vehicle controller
      var pieceController = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<VehiclePiecesController>();
      if (pieceController != null)
      {
        // Safely extract the Piece settings belonging to the ghost prefab template
        var ghostPiece = __instance.m_placementGhost.GetComponent<Piece>();
        if (ghostPiece != null)
        {
          var container = ghostPiece.GetComponentInChildren<Container>();

          // Check if building is disabled for this type of structure
          var isBuildModeDisabled = !pieceController.Manager.BuildMode.CanPlacePiece(container != null);

          if (isBuildModeDisabled)
          {
            // Force Valheim's internal placement valid tracking loop to fail
            // 1. Turns the visual placement ghost bright red instantly
            // 2. Halts vanilla's internal placement logic so clicking does absolutely nothing!
            __instance.m_placementStatus = Player.PlacementStatus.Invalid; // or .InvalidRegion / .BlockedByPlayer depending on your preferred fallback feedback type

            // Manually refresh the color maps on the ghost renderer mesh instances to reflect the new state
            __instance.SetPlacementGhostValid(false);
          }
        }
      }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
    [HarmonyPrefix]
    public static bool RemovePiece_BlockBuildMode(Player __instance, ref bool __result)
    {
      // 1. Leverage Valheim's internal raycast tracking to find what piece the player is looking at
      var targetedPiece = __instance.m_hoveringPiece;

      // Fallback to your shared custom raycast tracking data if vanilla's hover cache is empty
      if (targetedPiece == null)
      {
        targetedPiece = PatchSharedData.PlayerLastRayPiece;
      }

      if (targetedPiece != null)
      {
        // 2. Discover if the piece they want to break is parented to a vehicle controller
        var pieceController = targetedPiece.GetComponentInParent<VehiclePiecesController>();
        if (pieceController != null)
        {
          // BYPASS: Never block removing utility toggles or floatation items
          var pieceName = targetedPiece.gameObject.name;
          if (pieceName.StartsWith(PrefabNames.CustomWaterFloatation) || pieceName.StartsWith(PrefabNames.BuildModeToggle))
          {
            return true; // Allow the removal to proceed normally
          }

          // 3. Evaluate the vehicle container requirements
          var container = targetedPiece.GetComponentInChildren<Container>();
          var isBuildModeDisabled = !pieceController.Manager.BuildMode.CanPlacePiece(container != null);

          if (isBuildModeDisabled)
          {
            // 4. Send your custom warning to the UI center canvas layer
            __instance.Message(
              MessageHud.MessageType.Center,
              Localization.instance.Localize("$valheim_vehicles_buildmode_unable_to_remove_piece_message")
            );

            // Set the method's return value to false and return false to abort vanilla execution
            __result = false;
            return false;
          }
        }
      }

      return true; // Allow standard piece removal if conditions are met
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
      var vehicleShip = netView.GetComponent<VehicleManager>();
      if (vehicleShip == null) return;
      var ghostContainer = vehicleShip.GhostContainer();
      if (ghostContainer != null)
      {
        ghostContainer.SetActive(false);
      }
    }

    /// <summary>
    /// Important for piece side-effects
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
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

      if ((bool)rb && !rb.isKinematic)
      {
        return gameObject;
      }

      // can run fix piece even if no last piece raycast.
      if (!PatchSharedData.PlayerLastRayPiece)
      {
        TryFixPieceOverlap(gameObject);
        return gameObject;
      }

      if (netView != null)
      {
        var cul = PatchSharedData.PlayerLastRayPiece?
          .GetComponent<CultivatableComponent>();
        if (cul != null) cul.AddNewChild(netView);
      }

      if (PatchSharedData.PlayerLastRayPiece == null)
      {
        // should always run FixPieceOverlap
        TryFixPieceOverlap(gameObject);
        return gameObject;
      }

      var pieceController = PatchSharedData.PlayerLastRayPiece.GetComponentInParent<IPieceController>();
      if (pieceController != null)
      {
        if (gameObject.name.StartsWith(PrefabNames.CustomWaterFloatation) || gameObject.name.StartsWith(PrefabNames.BuildModeToggle))
        {
          pieceController.AddTempUtilityPiece(gameObject);
          return gameObject;
        }

        if (pieceController.CanPlacePiece(netView))
        {
          if (piece.m_nview != null)
          {
            pieceController.AddNewPiece(piece.m_nview);
          }
          else
          {
            pieceController.TrySetPieceToParent(piece.gameObject);
            // should always run afterward.
            TryFixPieceOverlap(gameObject);
          }
        }

        return gameObject;
      }

      TryFixPieceOverlap(gameObject);

      return gameObject;
    }

#if DEBUG
    /// <summary>
    /// Controls snappoint alignment with rotationals that can flip a snappoint.
    ///
    /// - Supports Rotating Cannons. But is not perfect. May need to call RotateTowards or RotateAround which would move the point upwards. Experimental flag but enabled by default as it does not appear to regress other prefabs.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    [HarmonyPostfix]
    public static void UpdatePlacementGhost_InjectRotation(Player __instance)
    {
      if (!PatchConfig.Experimental_SnappointRotationalPatch.Value) return;
      if (!__instance.m_placementGhost) return;
      if (!__instance.m_placementGhost.transform.name.StartsWith(PrefabNames.ValheimVehiclesPrefix)) return;
      var manualSnapIndex = __instance.m_manualSnapPoint;
      if (manualSnapIndex < 1 || __instance.m_tempSnapPoints1.Count < manualSnapIndex) return;
      var quaternion = Quaternion.Euler(0.0f, __instance.m_placeRotationDegrees * (float)__instance.m_placeRotation, 0.0f);

      Transform? snappointTransform = null;

      if (snappointTransform == null)
      {
        snappointTransform = __instance.m_tempSnapPoints1[__instance.m_manualSnapPoint];
      }

      if (snappointTransform != null)
      {
        __instance.m_placementGhost.transform.rotation = quaternion * snappointTransform.localRotation;
      }
    }
#endif

    public static void TryFixPieceOverlap(GameObject gameObject)
    {
      if (!Mod_PieceOverlapConfig.PieceOverlap_Enabled.Value)
      {
        return;
      }
      PieceOverlapUtils.TryResolveCoplanarityOnPlacement(
        gameObject,
        go =>
        {
          var piece = go.GetComponentInParent<Piece>();
          if (piece) return piece.gameObject;
          var rootGo = go.transform.root.gameObject;
          LoggerProvider.LogWarning($"Could not find root piece for go {go.name} using root transform {rootGo.name}");
          return rootGo;
        },
        0.001f,
        16,
        LayerHelpers.PieceLayerMask,
        (prefabRoot, pos) =>
        {
          if (!prefabRoot.GetZDO(out var zdo)) return;
          zdo.SetPosition(pos);
        }
      );
    }

    /// <summary>
    /// Verbose way to track allowed prefabs that can have other prefabs raycast hit to.
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    public static bool CanHitPiece(Collider collider)
    {
      var genericPieceController = collider.GetComponentInParent<IPieceController>();

      if (genericPieceController == null) return false;
      var colliderRoot = collider.transform.root;
      var canHitPiece = genericPieceController.CanRaycastHitPiece();
      var isSwivel = genericPieceController.ComponentName == PrefabNames.SwivelPrefabName;

      // swivel nesting might not be necessary.
      if (canHitPiece)
      {
        // do not allow nested swivel item placement.
        // todo this might be okay. allowing it for now.

        // if (isSwivel && colliderRoot != genericPieceController.transform && colliderRoot.transform.name.StartsWith(PrefabNames.SwivelPrefabName))
        // {
        //   return false;
        // }
        return true;
      }
      return false;
    }


    public static void HandleGameObjectRayCast(Transform? vehicleTransform,
      LayerMask layerMask,
      Player __instance, ref bool __result,
      ref Vector3 point,
      ref Vector3 normal, ref Piece piece, ref Heightmap heightmap,
      ref Collider waterSurface,
      bool water, out bool ShouldRunOriginalMethod)
    {
      ShouldRunOriginalMethod = true;

      var gameCameraTransform = GameCamera.instance.transform;
      var start = gameCameraTransform.position;
      var end = gameCameraTransform.forward;

      if (vehicleTransform != null)
      {
        var localPos = vehicleTransform.InverseTransformPoint(__instance.transform
          .position);
        var localStart = localPos + Vector3.up * 2f;
        var localDir = ((Character)__instance).m_lookYaw * Quaternion.Euler(
          __instance.m_lookPitch,
          0 - vehicleTransform.transform.rotation.eulerAngles.y +
          PatchSharedData.YawOffset, 0);

        start = vehicleTransform.TransformPoint(localStart);
        end = vehicleTransform.rotation * localDir * Vector3.forward;
      }

      // allows raycast to work properly when zoomed out.
      var distanceBetweenPlayerAndCamera = Vector3.Distance(__instance.transform.position, gameCameraTransform.position);
      var castDistance = distanceBetweenPlayerAndCamera + 10f;

      if (Physics.Raycast(start, end, out var hitInfo, castDistance, layerMask) &&
          (bool)hitInfo.collider)
      {
        if (!CanHitPiece(hitInfo.collider))
        {
          ShouldRunOriginalMethod = true;
          return;
        }

        point = hitInfo.point;
        normal = hitInfo.normal;
        piece = hitInfo.collider.GetComponentInParent<Piece>();
        heightmap = null;
        waterSurface = null;
        __result = true;

        // Let the prefix run. This means we double up on Raycasts which is heavier on performance...
        ShouldRunOriginalMethod = false;
      }
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

      var raycastPieceActivator = PieceActivatorHelpers.GetRaycastPieceActivator(__instance.transform);

      HandleGameObjectRayCast(raycastPieceActivator?.transform, layerMask, __instance,
        ref __result, ref point,
        ref normal, ref piece,
        ref heightmap,
        ref waterSurface, water, out var shouldRunOriginalMethod);

      return shouldRunOriginalMethod;
    }

#if DEBUG
    [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
#else
    [HarmonyPatch(typeof(Player), "PieceRayTest")]
#endif
    [HarmonyPostfix]
    public static void PieceRayTestPostfix(Player __instance, ref bool __result,
      ref Vector3 point,
      ref Vector3 normal, ref Piece piece, ref Heightmap heightmap,
      ref Collider waterSurface,
      bool water)
    {
#if DEBUG
      var pieceName = piece == null ? "null" : piece.name;
      LoggerProvider.LogDebugDebounced($"piece raycast: {pieceName} water: {water}");
      if (piece == null)
      {
        PieceRayTest(__instance, out var p, out var n, out var pc, out var h, out var w, water);
        if (pc == null)
        {
          LoggerProvider.LogDebugDebounced("  Recheck: piece is still null");
        }
        else
        {
          LoggerProvider.LogDebugDebounced($"  Recheck: piece is {pc.name}");
          piece = pc;
          __result = true;
        }
      }
#endif
      PatchSharedData.PlayerLastRayPiece = piece;
    }


#if DEBUG
    public static float RaycastDistance = 150f;

    public static bool PieceRayTest(
      Player __instance,
      out Vector3 point,
      out Vector3 normal,
      out Piece piece,
      out Heightmap heightmap,
      out Collider waterSurface,
      bool water)
    {
      var layerMask = __instance.m_placeRayMask;
      if (water)
        layerMask = __instance.m_placeWaterRayMask;
      RaycastHit hitInfo;
      if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out hitInfo, RaycastDistance, layerMask))
      {
        var maxPlaceDistance = __instance.m_maxPlaceDistance;
        if ((bool)(Object)__instance.m_placementGhost)
        {
          var component = __instance.m_placementGhost.GetComponent<Piece>();
          if (component != null)
            maxPlaceDistance += (float)component.m_extraPlacementDistance;
        }
        if ((bool)(Object)hitInfo.collider && !(bool)(Object)hitInfo.collider.attachedRigidbody && (double)Vector3.Distance(__instance.m_eye.position, hitInfo.point) < (double)maxPlaceDistance)
        {
          point = hitInfo.point;
          normal = hitInfo.normal;
          piece = hitInfo.collider.GetComponentInParent<Piece>();
          heightmap = hitInfo.collider.GetComponent<Heightmap>();
          waterSurface = hitInfo.collider.gameObject.layer != LayerMask.NameToLayer("Water") ? (Collider)null : hitInfo.collider;
          return true;
        }
      }
      point = Vector3.zero;
      normal = Vector3.zero;
      piece = (Piece)null;
      heightmap = (Heightmap)null;
      waterSurface = (Collider)null;
      return false;
    }
#endif

    [HarmonyPatch(typeof(Player), "Save")]
    [HarmonyPrefix]
    public static void Player_Save(Player __instance, ZPackage pkg)
    {
      if (__instance.m_lastGroundCollider != null &&
          __instance.m_lastGroundTouch < 0.3f)
      {
        VehiclePiecesController.AddTempParent(__instance.m_nview,
          __instance.m_lastGroundCollider.gameObject);
      }
    }

    [HarmonyPatch(typeof(Player), "FindHoverObject")]
    [HarmonyPrefix]
    private static bool FindHoverObject(Player __instance, ref GameObject hover,
      ref Character hoverCreature)
    {
      hover = null;
      hoverCreature = null;
      // var eyePos = Player.m_localPlayer.m_eye.position;
      var cameraTransform = GameCamera.instance.transform;
      var eyePos = cameraTransform.position;
      // var cam = GameCamera.instance;
      //
      // var targetPoint = cam.transform.position + cam.transform.forward * 100f; // Far enough to avoid near plane error
      // var rayDir = (targetPoint - eyePos).normalized;

      var rayDir = cameraTransform.forward;

      var array = Physics.RaycastAll(eyePos,
        rayDir, 50f, __instance.m_interactMask);
      Array.Sort(array,
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
          else if (raycastHit.collider && raycastHit.collider.name.StartsWith("lever") && ValheimExtensions.TryGetHoverableParent(raycastHit.collider.gameObject, out var leverHoverableParent))
          {
            hover = leverHoverableParent;
          }
          else if ((bool)raycastHit.collider.attachedRigidbody && raycastHit
                     .collider
                     .attachedRigidbody.GetComponentInParent<IPieceActivatorHost>() == null)
            hover = raycastHit.collider.attachedRigidbody.gameObject;
          else
            hover = raycastHit.collider.gameObject;
        }

        if (hover != null) break;
      }

      RopeAnchorComponent.m_draggingRopeTo = null;
      if (hover != null && RopeAnchorComponent.m_draggingRopeFrom != null)
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

        var vpc = __instance.m_attachPoint.GetComponentInParent<VehiclePiecesController>();

        if (vpc != null)
        {
          vpc.OnboardController.SetControlling(__instance.GetPlayerID(), false);
        }

        if ((bool)ladder)
        {
          ladder.OnStepOffLadder(__instance);
          ((Character)__instance).m_animator.SetIKPositionWeight(
            AvatarIKGoal.LeftHand, 0);
          ((Character)__instance).m_animator.SetIKPositionWeight(
            AvatarIKGoal.RightHand, 0);
          ((Character)__instance).m_animator.SetIKRotationWeight(
            AvatarIKGoal.LeftHand, 0);
          ((Character)__instance).m_animator.SetIKRotationWeight(
            AvatarIKGoal.RightHand, 0);
        }
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
    [HarmonyPriority(Priority.First)]
    [HarmonyBefore("org.bepinex.plugins.valheim_plus")]
    public static IEnumerable<CodeInstruction> UpdatePlacementGhost(
      IEnumerable<CodeInstruction> instructions)
    {
      var list = instructions.ToList();

      var quaternionEuler = AccessTools.Method(
        typeof(Quaternion),
        nameof(Quaternion.Euler),
        new[]
        {
          typeof(float),
          typeof(float),
          typeof(float)
        });

      var applyVehicleRelativeRotation = AccessTools.Method(
        typeof(VehicleRotationHelpers),
        nameof(VehicleRotationHelpers.ApplyVehicleRelativeRotation));

      for (var i = 0; i < list.Count - 1; i++)
      {
        if (!list[i].Calls(quaternionEuler))
          continue;

        // Keep this exact sequence intact for compatibility with other
        // transpilers such as Valheim Plus FreePlacementRotation:
        //
        //   call Quaternion.Euler
        //   stloc.s <rotation>
        //
        // Valheim Plus explicitly searches for that pair.
        var storeRotation = list[i + 1];

        if (storeRotation.opcode != OpCodes.Stloc_S)
          continue;

        var rotationLocal = storeRotation.operand;

        // Transform the already-created/stored Quaternion instead of
        // replacing Quaternion.Euler itself.
        //
        // Result:
        //
        //   call Quaternion.Euler  
        //   stloc.s rotation
        //
        //   ldloc.s rotation
        //   call ApplyVehicleRelativeRotation
        //   stloc.s rotation
        //
        // Vanilla then continues using the vehicle-relative rotation for
        // snap-point and placement calculations.
        list.InsertRange(i + 2, new[]
        {
          new CodeInstruction(OpCodes.Ldloc_S, rotationLocal),
          new CodeInstruction(OpCodes.Call, applyVehicleRelativeRotation),
          new CodeInstruction(OpCodes.Stloc_S, rotationLocal)
        });

        // UpdatePlacementGhost currently contains exactly one matching call,
        // and this is specifically the placement rotation call.
        break;
      }

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
          ? controlledComponent as IVehicleBaseProperties
          : null;

      return vvShipResult;
    }
  }