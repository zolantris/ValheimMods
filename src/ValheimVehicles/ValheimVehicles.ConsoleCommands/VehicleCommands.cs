using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using ComfyGizmo;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Assertions.Must;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.UI;
using Zolantris.Shared;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.ConsoleCommands;

#if DEBUG
public class VehicleDebugCommands : VehicleCommands
{
  public override string Name => "v";
}
#endif

public class VehicleCommands : ConsoleCommand
{
  private static class VehicleCommandArgs
  {
    // public const string locate = "locate";
    // public const string rotate = "rotate";
    public const string destroy = "destroy";
    public const string reportInfo = "report-info";
    public const string debug = "debug";
    public const string debugShort = "d";
    public const string config = "config";
    public const string creative = "creative";
    public const string colliderEditMode = "colliderEditMode";
    public const string help = "help";
    public const string recover = "recover";
    public const string rotate = "rotate";
    public const string moveUp = "moveUp";
    public const string move = "move";
    public const string toggleOceanSway = "toggleOceanSway";
    public const string resetVehicleOwner = "resetLocalOwnership";
    public const string clearBoundaryChunkData = "clearBoundaryChunkData";
    public const string recenter = "recenter";
    public const string repairAllVehiclePositions = "repairAllVehiclePositions";
  }

  public override string Help => OnHelp();

  public static string OnHelp()
  {
    return
      "Runs vehicle commands, each command will require parameters to run use help to see the input values." +
#if DEBUG
      // config is only debug for now.
      $"\n<{VehicleCommandArgs.config}>: will show a menu related to the current vehicle you are on. This GUI menu will let you customize values specifically for your current vehicle." +
#endif
      $"\n<{VehicleCommandArgs.destroy}>: will DELETE the current raft and BREAK all pieces. This is a destructive admin-only command or (if cheats are enabled). You have been warned!" +
      $"\n<{VehicleCommandArgs.debug}>: will show a menu with options like rotating or debugging vehicle colliders" +
      $"\n<{VehicleCommandArgs.recover}>: will recover any vehicles within range of 1000" +
      $"\n<{VehicleCommandArgs.rotate}>: defaults to zeroing x and z tilt. Can also provide 3 args: x y z" +
      $"\n<{VehicleCommandArgs.toggleOceanSway}>: stops the vehicle from swaying in the water. It will stay at 0 degrees (x and z) tilt and only allow rotating on y axis" +
      $"\n<{VehicleCommandArgs.reportInfo}>: outputs information related to the vehicle the player is on or near. This is meant for error reports" +
      $"\n<{VehicleCommandArgs.moveUp}>: Moves the vehicle within 50 units upwards by the value provided. Capped at 30 units to be safe. And Capped at 10 units lowest world position." +
      $"\n<{VehicleCommandArgs.move}>: Must provide 3 args: x y z, the movement is relative to those points" +
      $"\n<{VehicleCommandArgs.colliderEditMode}>: Lets the player toggle collider edit mode for all vehicles allowing editing water displacement masks and other hidden items" +
      $"\n<{VehicleCommandArgs.recenter}>: Manually recenters the vehicle's ZDO origin to the geometric hull center. This prevents piece ZDOs from drifting into foreign zone sectors." +
      $"\n<{VehicleCommandArgs.clearBoundaryChunkData}>: Clears the boundary chunk data for the nearest vehicle. This will force a rebuild of the convex hull boundary constraint. Boundary chunk data is use to limit the extent the vehicle can grow to." +
      $"\n<{VehicleCommandArgs.repairAllVehiclePositions}>: Iterates all tracked vehicles and forces all pieces to be synchronized to the current vehicle location. Optional args: minHeight maxHeight. When provided, the vehicle Y position is clamped to [minHeight, maxHeight] before syncing pieces. Useful if vehicles have fallen through the ground or launched into the sky. Example: vehicle repairAllVehiclePositions -100 500";
  }

  public override void Run(string[] args)
  {
    ParseFirstArg(args);
  }

  private void ParseFirstArg(string[] args)
  {
    if (args.Length < 1)
    {
      Logger.LogMessage(
        "Must provide a argument for `vehicle` command, type vehicle help to see all commands");
      return;
    }

    var firstArg = args.First();
    if (firstArg == null)
    {
      Logger.LogMessage("Must provide a argument for `vehicle` command");
      return;
    }

    var nextArgs = args.Skip(1).ToArray();

    switch (firstArg)
    {
      case VehicleCommandArgs.move:
        if (!CanRunCheatCommand()) return;
        VehicleMove(nextArgs);
        break;
      case VehicleCommandArgs.toggleOceanSway:
        VehicleToggleOceanSway();
        break;
      case VehicleCommandArgs.rotate:
        if (!CanRunCheatCommand()) return;
        VehicleRotate(args);
        break;
      case VehicleCommandArgs.recover:
        RecoverRaftConsoleCommand.RecoverRaftWithoutDryRun(
          $"{Name} {VehicleCommandArgs.recover}");
        break;
      case VehicleCommandArgs.creative:
        ToggleCreativeMode();
        break;
#if DEBUG
      case VehicleCommandArgs.debugShort:
        ToggleVehicleCommandsHud();
        break;
#endif
      case VehicleCommandArgs.debug:
        ToggleVehicleCommandsHud();
        break;
      case VehicleCommandArgs.destroy:
        DestroyCurrentVehicle();
        break;
#if DEBUG
      // config is not ready - only debug for now.
      case VehicleCommandArgs.config:
        ToggleVehicleGuiConfig();
        break;
#endif
      case VehicleCommandArgs.reportInfo:
        OnReportInfo();
        break;
      case VehicleCommandArgs.colliderEditMode:
        ToggleColliderEditMode();
        break;
      case VehicleCommandArgs.moveUp:
        if (!CanRunCheatCommand()) return;
        VehicleMoveVertically(nextArgs);
        break;
      case VehicleCommandArgs.resetVehicleOwner:
        VehicleOwnerReset();
        break;
      case VehicleCommandArgs.help:
        Logger.LogMessage(OnHelp());
        break;
      case VehicleCommandArgs.clearBoundaryChunkData:
        ClearAllVehicleBoundaryChunks();
        break;
      case VehicleCommandArgs.recenter:
        VehicleRecenter();
        break;
      case VehicleCommandArgs.repairAllVehiclePositions:
        if (!CanRunCheatCommand()) return;
        RepairAllVehiclePositions(nextArgs);
        break;
    }
  }

  public void VehicleOwnerReset()
  {
    var currentVehicle =
      GetNearestVehicleManager();
    if (currentVehicle?.MovementController == null)
    {
      Logger.LogMessage("No vehicle nearby");
      return;
    }

    if (!VehicleOnboardController.IsCharacterOnboard(Player.m_localPlayer))
    {
      Logger.LogMessage(
        "You player is not onboard any vehicle. You must be onboard the vehicle to safely take control of it.");
      return;
    }

    currentVehicle.MovementController.ForceTakeoverControls(Player.m_localPlayer
      .GetPlayerID());
  }

  public static void FloatArgErrorMessage(string arg)
  {
    var message =
      $"The arg provided {arg} was not a float. Example -10, 0, 15.5, 30.1 are all accepted. (positive/negative). Values above 50.0 locked at 50. Values that would put the vehicle below the map are prevented if you want to do that, use Unity Explorer.";
    Logger.LogMessage(arg);
  }

  /// <summary>
  /// Handles vertical movement for vehicles. Can be merged with VehicleMove for shared functionality.
  /// </summary>
  /// <param name="args">Command arguments.</param>
  public static void VehicleMoveVertically(string[]? args)
  {
    if (!CanRunCheatCommand()) return;
    if (args == null || args.Length < 1)
    {
      FloatArgErrorMessage("No args provided");
      return;
    }

    if (!float.TryParse(args[0], out var offset))
    {
      FloatArgErrorMessage(args[0]);
      return;
    }

    var vehicleInstance =
      GetNearestVehicleManager();

    if (vehicleInstance?.MovementController == null)
    {
      Logger.LogMessage("No vehicle found near the player");
      return;
    }

    Game.instance.StartCoroutine(MoveVehicle(vehicleInstance,
      Vector3.up * Mathf.Clamp(offset, -100f, 100f)));
  }

  public struct SafeMoveCharacterData
  {
    public Vector3 lastLocalOffset;
    public Character character;
    public bool isDebugFlying;
  }

  public struct SafeMoveData
  {
    public List<SafeMoveCharacterData> charactersOnShip;
    public bool IsLocalPlayerDebugFlying;
    public VehicleOnboardController OnboardController;
  }

  public static SafeMoveData? SafeMovePlayerBefore(
    VehicleOnboardController? vehicleOnboardController,
    bool shouldMoveLocalPlayer = true)
  {
    if (vehicleOnboardController == null) return null;
    if (vehicleOnboardController.PiecesController == null) return null;

    var charactersOnShip = vehicleOnboardController.GetCharactersOnShip();
    var characterData = new List<SafeMoveCharacterData>();

    if (Player.m_localPlayer && Player.m_localPlayer.transform.parent != null)
    {
      Player.m_localPlayer.transform.SetParent(null);
    }

    if (shouldMoveLocalPlayer && !charactersOnShip.Contains(Player.m_localPlayer))
    {
      charactersOnShip.Add(Player.m_localPlayer);
    }

    // excludes current player to avoid double toggling.
    if (charactersOnShip.Count > 0)
      foreach (var character in charactersOnShip)
      {
        var isDebugFlying = character.IsDebugFlying();
        if (character.transform.parent)
        {
          character.transform.SetParent(null);
        }

        var lastLocalOffset = vehicleOnboardController.PiecesController.transform.InverseTransformPoint(character.transform.position);

        // bail on transporting the player if too far away from expected vehicle center of mass.
        if (vehicleOnboardController.MovementController != null && Vector3.Distance(vehicleOnboardController.MovementController.m_body.centerOfMass, lastLocalOffset) > 200f)
        {
          continue;
        }

        characterData.Add(new SafeMoveCharacterData
        {
          character = character,
          isDebugFlying = isDebugFlying,
          lastLocalOffset = lastLocalOffset
        });
      }

    var wasDebugFlying = false;

    if (Player.m_localPlayer)
    {
      wasDebugFlying = Player.m_localPlayer.IsDebugFlying();
    }

    return new SafeMoveData
    {
      charactersOnShip = characterData,
      IsLocalPlayerDebugFlying = wasDebugFlying,
      OnboardController = vehicleOnboardController
    };
  }

  /// <summary>
  /// Protects the player so they do not die mid-transit if the ship needs to fix itself.
  /// Allows for passing a ShipUpdateCallback which is the part that can kill a player when the ship moves it's physics rapidly. 
  /// </summary>
  /// <param name="onboardController"></param>
  /// <param name="shouldMoveLocalPlayerOffship">Meant for commands if the player is outside the range they can still follow the vehicle</param>
  /// <param name="GetPositionAfterMoveCallback">The final position</param>
  /// <param name="coroutineFunc">A method that may need to be called before running complete</param>
  /// <param name="shouldProtectAgainstFallDamage"></param>
  /// <returns></returns>
  public static IEnumerator SafeMovePlayer(
    VehicleOnboardController onboardController,
    bool shouldMoveLocalPlayerOffship,
    Func<Vector3> GetPositionAfterMoveCallback, IEnumerator? coroutineFunc, bool shouldProtectAgainstFallDamage = true)
  {
    var safeMoveData =
      SafeMovePlayerBefore(onboardController, shouldMoveLocalPlayerOffship);

    if (coroutineFunc != null) yield return coroutineFunc;
    yield return new WaitForFixedUpdate();

    var nextPosition = GetPositionAfterMoveCallback();
    yield return SafeMoveCharacterAfter(safeMoveData, nextPosition, shouldProtectAgainstFallDamage);
    yield return new WaitForFixedUpdate();
  }

  public static void ResetPlayerVelocities(Character player)
  {
    if (player.m_body.isKinematic) player.m_body.isKinematic = false;

    player.m_body.angularVelocity = Vector3.zero;
    player.m_body.linearVelocity = Vector3.zero;
    player.m_fallTimer = 0f;
    player.m_maxAirAltitude = -10000f;
  }

  public static void TeleportImmediately(Character character, Vector3 toPosition)
  {
    var player = character.GetComponent<Player>();
    if (player == null)
    {
      // Non-player character — move body + ZDO directly.
      character.m_body.position = toPosition;
      character.m_nview.m_zdo.SetPosition(toPosition);
      character.m_nview.m_zdo.SetRotation(character.transform.rotation);
      return;
    }

    if (Player.m_localPlayer == player)
    {
      // For the local player we must update ALL of: reference position, transform,
      // rigidbody, and ZDO. Updating only SetReferencePosition leaves the ZDO in
      // the old zone — the server culls it and the player gets deleted.
      ZNet.instance.SetReferencePosition(toPosition);

      // Move the physics body (authoritative position for physics).
      if (player.m_body.isKinematic)
        player.m_body.isKinematic = false;
      player.m_body.position = toPosition;
      player.m_body.linearVelocity = Vector3.zero;
      player.m_body.angularVelocity = Vector3.zero;

      // Move the transform so it matches immediately before the next frame.
      player.transform.position = toPosition;

      // Stamp the ZDO to the new position and sector so the server keeps the
      // player alive in the new zone rather than culling them from the old one.
      var playerZdo = player.m_nview?.GetZDO();
      if (playerZdo != null)
      {
        playerZdo.SetPosition(toPosition);
        playerZdo.SetSector(ZoneSystem.GetZone(toPosition));
      }
    }
    else
    {
      // Remote player — use the standard teleport path.
      player.m_teleporting = false;
      player.m_teleportTimer = 999f;
      player.TeleportTo(toPosition, character.transform.rotation, false);
    }
  }

  /// <summary>
  /// Moves player back to non debug mode.
  /// - todo move players to original position.
  /// </summary>
  /// <param name="data"></param>
  /// <param name="nextPosition"></param>
  public static IEnumerator SafeMoveCharacterAfter(SafeMoveData? data,
    Vector3 nextPosition, bool shouldProtectAgainstFallDamage = true)
  {
    if (data == null) yield break;
    if (data.Value.charactersOnShip.Count <= 0) yield break;

    var piecesController = data.Value.OnboardController?.PiecesController?.transform;
    var zdo = data.Value.OnboardController?.Manager?.m_nview?.GetZDO();

    if (zdo == null) yield break;
    // piecesController transform may be null if the vehicle was unloaded during
    // the far-zone teleport — all access below is guarded against this.

    foreach (var safeMoveCharacterData in data.Value.charactersOnShip)
    {
      var targetLocation = nextPosition + safeMoveCharacterData.lastLocalOffset;

      // Only do the delta-distance correction if the transform is still alive.
      if (piecesController != null && piecesController)
      {
        var safeMoveCharacterPos = safeMoveCharacterData.character.transform.position;
        var piecesControllerPos = piecesController.position;

        var deltaX = safeMoveCharacterPos.x - piecesControllerPos.x;
        var deltaY = safeMoveCharacterPos.y - piecesControllerPos.y;
        var deltaZ = safeMoveCharacterPos.z - piecesControllerPos.z;

        if (Mathf.Abs(deltaX) > 50f || Mathf.Abs(deltaY) > 50f || Mathf.Abs(deltaZ) > 50f)
          targetLocation = zdo.GetPosition() + safeMoveCharacterData.lastLocalOffset;
      }
      else
      {
        // Vehicle transform destroyed — use ZDO position as the origin.
        targetLocation = zdo.GetPosition() + safeMoveCharacterData.lastLocalOffset;
      }

      TeleportImmediately(safeMoveCharacterData.character, targetLocation);

      if (Player.m_localPlayer == safeMoveCharacterData.character)
        ZNet.instance.SetReferencePosition(targetLocation);
    }

    yield return new WaitForFixedUpdate();

    var timer = Stopwatch.StartNew();
    var complete = false;
    while (timer.ElapsedMilliseconds < 5000 && !complete)
    {
      var isSuccess = true;
      foreach (var playerData in data.Value.charactersOnShip)
      {
        if (!playerData.character) continue;

        if (playerData.character.IsTeleporting())
        {
          isSuccess = false;
          break;
        }

        // Only re-parent onto the vehicle if it's still alive.
        if (piecesController != null && piecesController)
        {
          if (!playerData.isDebugFlying)
          {
            playerData.character.transform.SetParent(piecesController);
            playerData.character.transform.localPosition = playerData.lastLocalOffset;
          }
          else
          {
            playerData.character.transform.position =
              piecesController.position + playerData.lastLocalOffset;
          }
        }

        ResetPlayerVelocities(playerData.character);
      }

      yield return new WaitForFixedUpdate();
      complete = isSuccess;
    }

    if (!complete)
      foreach (var playerData in data.Value.charactersOnShip)
      {
        if (!playerData.character) continue;
        ResetPlayerVelocities(playerData.character);
      }

    if (shouldProtectAgainstFallDamage)
    {
      while (timer.ElapsedMilliseconds < 5000)
      {
        foreach (var playerData in data.Value.charactersOnShip)
        {
          if (!playerData.character) continue;
          playerData.character.m_fallTimer = 0f;
          playerData.character.m_maxAirAltitude = -10000f;
        }
        yield return new WaitForFixedUpdate();
      }
    }

    timer.Restart();
    yield return null;
  }

  private static IEnumerator MoveVehicleIntoFarZone(VehicleManager vehicleInstance,
    Vector3 offset, Action<Vector3> onPositionReady)
  {
    if (vehicleInstance == null || vehicleInstance.PiecesController == null) yield break;

    var newLocation = VectorUtils.MergeVectors(vehicleInstance.transform.position, offset);
    var zoneToMoveTo = ZoneSystem.GetZone(newLocation);

    // -----------------------------------------------------------------------
    // Step 1: Broadcast IsTeleporting = true to ALL clients via RPC + ZDO.
    // GuardedFixedUpdate on every client will keep the body kinematic and
    // block ownership claims for the entire duration of the move.
    // Also freeze the local body directly so there is zero physics gap between
    // now and the first time GuardedFixedUpdate reads the ZDO value.
    // -----------------------------------------------------------------------
    var movementController = vehicleInstance.MovementController;
    movementController?.SetIsTeleporting(true);

    var wasKinematic = false;
    // Use vehicle ZDO position as the authoritative reference (source of truth in multiplayer).
    // NEVER use transform.position as it may be out of sync on non-owner clients.
    var vehicleCurrentPos = vehicleInstance.m_nview.m_zdo.GetPosition();
    if (movementController != null && movementController.m_body != null)
    {
      wasKinematic = movementController.m_body.isKinematic;
      movementController.m_body.isKinematic = true;
      movementController.m_body.linearVelocity = Vector3.zero;
      movementController.m_body.angularVelocity = Vector3.zero;
    }

    // Capture destinations outside the try so the post-yield steps can access it.
    Dictionary<ZNetView, Vector3> characterDestinations;

    try
    {
      // -----------------------------------------------------------------------
      // Step 2: Snapshot offsets and stamp all ZDOs (pieces + dynamic objects +
      // players) BEFORE the zone-load wait.
      // -----------------------------------------------------------------------
      var persistentId = vehicleInstance.PersistentZdoId;
      var liveTempPieces = vehicleInstance.PiecesController.m_tempPieces;

      // Root vehicle ZDO first — anchors the vehicle in the new sector.
      vehicleInstance.m_nview.m_zdo.SetPosition(newLocation);
      vehicleInstance.m_nview.m_zdo.SetSector(ZoneSystem.GetZone(newLocation));

      characterDestinations = VehiclePiecesController.StampAllVehicleZdosToPosition(
        persistentId, newLocation, vehicleCurrentPos, liveTempPieces);

      // Only update reference position if local player is actually onboard (in characterDestinations).
      // StampAllVehicleZdosToPosition handles all onboard characters via temp pieces and dynamic objects.
      // If the player is not in that list, they are NOT onboard and should NOT be teleported.
      var localPlayer = Player.m_localPlayer;
      if (localPlayer != null && localPlayer.m_nview != null)
      {
        if (characterDestinations.ContainsKey(localPlayer.m_nview))
        {
          // Player is onboard and will be teleported — update reference position for zone streaming.
          ZNet.instance.SetReferencePosition(newLocation);
        }
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"MoveVehicleIntoFarZone: exception during ZDO stamping — aborting teleport. {e}");
      movementController?.SetIsTeleporting(false);
      yield break;
    }

    // -----------------------------------------------------------------------
    // Step 3: Kick off zone generation at the destination.
    // -----------------------------------------------------------------------
    if (!ZoneSystem.instance.PokeLocalZone(zoneToMoveTo))
      if (!ZoneSystem.instance.SpawnZone(zoneToMoveTo, ZoneSystem.SpawnMode.Full, out _))
        ZoneSystem.instance.CreateLocalZones(newLocation);

    var timer = Stopwatch.StartNew();
    yield return new WaitUntil(() =>
      ZoneSystem.instance.IsZoneGenerated(zoneToMoveTo) ||
      timer.ElapsedMilliseconds > 15000);

    timer.Restart();
    yield return new WaitUntil(() =>
      ZoneSystem.instance.IsZoneLoaded(newLocation) ||
      timer.ElapsedMilliseconds > 5000);

    // -----------------------------------------------------------------------
    // Step 4: Zone is loaded. Validate vehicle ref, then move body.
    // -----------------------------------------------------------------------
    if (vehicleInstance == null ||
        vehicleInstance.m_nview == null ||
        vehicleInstance.m_nview.m_zdo == null)
    {
      // Clear teleport flag so no client stays permanently frozen.
      movementController?.SetIsTeleporting(false);
      foreach (var kvp in characterDestinations)
        if (kvp.Key && kvp.Key.GetComponent<Character>() is {} c)
          c.m_body.isKinematic = false;
      yield break;
    }

    // Move vehicle body while still kinematic — no physics pop.
    if (movementController != null && movementController.m_body != null)
    {
      movementController.m_body.position = newLocation;
      movementController.m_body.rotation = Quaternion.Euler(
        0f, movementController.m_body.rotation.eulerAngles.y, 0f);
    }

    if (vehicleInstance) vehicleInstance.transform.position = newLocation;

    // -----------------------------------------------------------------------
    // Step 5: Move every onboard character to its correct relative position.
    // -----------------------------------------------------------------------
    foreach (var kvp in characterDestinations)
    {
      var nv = kvp.Key;
      var destPos = kvp.Value;
      if (!nv) continue;

      var character = nv.GetComponent<Character>();
      if (character == null) continue;

      character.transform.position = destPos;
      if (character.m_body != null)
      {
        character.m_body.position = destPos;
        character.m_body.isKinematic = false;
        character.m_body.linearVelocity = Vector3.zero;
        character.m_body.angularVelocity = Vector3.zero;
      }

      character.m_fallTimer = 0f;
      character.m_maxAirAltitude = -10000f;
    }

    // -----------------------------------------------------------------------
    // Step 6: Restore vehicle physics and clear the teleporting flag.
    // Clearing LAST ensures GuardedFixedUpdate won't re-enable physics until
    // the body is already at the correct position.
    // -----------------------------------------------------------------------
    if (movementController != null && movementController.m_body != null && !wasKinematic)
    {
      movementController.m_body.isKinematic = false;
      movementController.m_body.linearVelocity = Vector3.zero;
      movementController.m_body.angularVelocity = Vector3.zero;
    }

    // Broadcast to all clients that the teleport is complete — physics resumes.
    movementController?.SetIsTeleporting(false);

    onPositionReady(newLocation);
  }

  /// <summary>
  /// Moves the vehicle based on the provided offset vector.
  /// - Must be only called via commands. This is meant to move the player even if they are not attached to the vehicle.
  /// - MoveVehicleIntoFarZone handles all character teleportation internally, so we don't use SafeMovePlayer here.
  /// </summary>
  /// <param name="vehicleInstance">The vehicle instance to move.</param>
  /// <param name="offset">The offset vector to apply.</param>
  private static IEnumerator MoveVehicle(VehicleManager? vehicleInstance,
    Vector3 offset)
  {
    if (vehicleInstance?.OnboardController == null)
      yield break;

    vehicleInstance.SetCreativeMode(true);
    yield return new WaitForFixedUpdate();

    // MoveVehicleIntoFarZone handles ALL character teleportation via StampAllVehicleZdosToPosition,
    // including players who may be far from the vehicle. No SafeMovePlayer wrapper needed.
    yield return MoveVehicleIntoFarZone(vehicleInstance, offset, _ => {});

    if (vehicleInstance != null) vehicleInstance.SetCreativeMode(false);
  }

  public Vector3 ClampFromPermissions(Vector3 position)
  {
    if (!SynchronizationManager.Instance.PlayerIsAdmin)
    {
      Logger.LogMessage(
        "Clamping to 50 units in any direction for any point if not admin");
      return VectorUtils.ClampVector(position, -50, 50f);
    }

    Logger.LogMessage("Clamping to -5000f and 5000f units for all vectors");

    return VectorUtils.ClampVector(position, -5000f, 5000f);
  }

  public static bool CanRunCheatCommand()
  {
    try
    {
      if (!VehicleGuiMenuConfig.AllowDebugCommandsForNonAdmins.Value)
      {
        if (Player.m_localPlayer == null || ZNet.instance == false) return false;
        var playerId = Player.m_localPlayer.GetPlayerID();
        if (!ZNet.instance.IsAdmin(playerId))
        {
          Logger.LogMessage($"Player is not an admin. They cannot run this cheat command without setting configKeySection <{VehicleGuiMenuConfig.AllowDebugCommandsForNonAdmins.Definition.Section}> ConfigKey: <{VehicleGuiMenuConfig.AllowDebugCommandsForNonAdmins.Definition.Key}> to true in the raft config or being an Admin.");
          return false;
        }
      }

      return true;
    }
    catch (Exception e)
    {
      return false;
    }
  }

  public static bool CanRunEditCommand()
  {
    if (!VehicleGuiMenuConfig.AllowDebugCommandsForNonAdmins.Value)
    {
      if (Player.m_localPlayer == null || ZNet.instance == false) return false;
      var playerId = Player.m_localPlayer.GetPlayerID();
      if (!ZNet.instance.IsAdmin(playerId))
      {
        Logger.LogMessage($"Player is not an admin. They cannot run this edit command without setting configKeySection <{VehicleGuiMenuConfig.AllowEditCommandsForNonAdmins.Definition.Section}> ConfigKey: <{VehicleGuiMenuConfig.AllowEditCommandsForNonAdmins.Definition.Key}> to true in the raft config or being an Admin.");
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Moves the vehicle based on the provided x, y, z parameters.
  /// </summary>
  /// <param name="args">Command arguments.</param>
  public void VehicleMove(string[] args)
  {
    var shipInstance =
      GetNearestVehicleManager();
    if (shipInstance == null)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 3 &&
        float.TryParse(args[0], out var x) &&
        float.TryParse(args[1], out var y) &&
        float.TryParse(args[2], out var z))
    {
      var offsetVector = ClampFromPermissions(new Vector3(x, y, z));


      Game.instance.StartCoroutine(MoveVehicle(shipInstance,
        offsetVector));
    }
    else
    {
      Logger.LogMessage(
        "Must provide x y z parameters, e.g., vehicle move 0.5 0 10");
    }
  }


  /// <summary>
  /// Freezes the Vehicle rotation permenantly until the boat is unloaded similar to raftcreative 
  /// </summary>
  public static void VehicleToggleOceanSway()
  {
    var vehicleController = GetNearestVehicleManager();
    if (vehicleController == null)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    vehicleController.MovementController?
      .SendToggleOceanSway();
  }

  private static void VehicleRotate(string[] args)
  {
    var vehicleController = GetNearestVehicleManager();
    if (vehicleController == null || vehicleController.MovementController == null)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 1)
      Game.instance.StartCoroutine(vehicleController.MovementController.FixShipRotation());

    if (args.Length == 4)
    {
      float.TryParse(args[1], out var x);
      float.TryParse(args[2], out var y);
      float.TryParse(args[3], out var z);
      vehicleController.MovementController.m_body
        .MoveRotation(
          Quaternion.Euler(
            Mathf.Approximately(0f, x) ? 0 : x,
            Mathf.Approximately(0f, y) ? 0 : y,
            Mathf.Approximately(0f, z) ? 0 : z));
    }
    else
    {
      Logger.LogMessage(
        "Must provide x y z parameters, IE: vehicle rotate 0.5 0 10");
    }
  }

  private static void ToggleVehicleGuiConfig()
  {
    if (!VehicleGui.Instance)
    {
      VehicleGui.AddRemoveVehicleGui();
    }

    VehicleGui.ToggleConfigPanelState(true);
  }

  public static void ToggleVehicleCommandsHud()
  {
    if (!CanRunCheatCommand()) return;
    if (Player.m_localPlayer == null) return;
    // must do this otherwise the commands panel will not cycle debug value if we need to enable it.
    VehicleGui.ToggleCommandsPanelState(true);

    var closestVehicle = GetNearestVehicleManager();

    if (closestVehicle != null)
    {
      closestVehicle.Instance.HasVehicleDebugger = VehicleGui.hasCommandsWindowOpened;
      closestVehicle.AddOrRemoveVehicleDebugger();
    }
  }

  public static void VehicleNotDetectedMessage()
  {
    if (Player.m_localPlayer == null) return;
    var vehicleNotFoundMsg = ModTranslations.VehicleCommand_Message_VehicleNotFound;
    Player.m_localPlayer.Message(MessageHud.MessageType.Center, vehicleNotFoundMsg);
    LoggerProvider.LogWarning(
      $"{vehicleNotFoundMsg} \nMust be within <50f> (game meters). The player must be closer to the boat.");
  }
  public static RaycastHit[] AllocatedRaycast = new RaycastHit[20];

  public static bool TryGetVehicleManager(Collider collider, [NotNullWhen(true)] out VehicleManager? vehicleManager)
  {
    vehicleManager = null;
    var vpc = collider.GetComponentInParent<VehiclePiecesController>();
    if (vpc)
    {
      vehicleManager = vpc.Manager;
    }

    var vm = collider.GetComponentInParent<VehicleManager>();
    if (vm)
    {
      vehicleManager = vm;
    }

    return vehicleManager;
  }

  /// <summary>
  /// Do a downwards raycast always so the cast position should be a value above where it's expected
  /// </summary>
  /// todo figure out why CustomVehicleMask never collides with raycast.
  /// <returns></returns>
  public static VehicleManager? GetNearestVehicleManagerInRay(Vector3 castPositionRay, float maxDistanceRay, VehicleManager? excludedManager)
  {
    var hits = Physics.RaycastNonAlloc(castPositionRay, Vector3.down, AllocatedRaycast, maxDistanceRay, LayerHelpers.PieceAndCustomVehicleMask);
    VehicleManager? vehicleManager = null;

    if (hits != 0)
    {
      for (var index = 0; index < hits; index++)
      {
        var raycastHit = AllocatedRaycast[index];
        if (TryGetVehicleManager(raycastHit.collider, out vehicleManager))
        {
          if (excludedManager != null && excludedManager == vehicleManager)
          {
            vehicleManager = null;
          }
          else
          {
            break;
          }
        }
      }
    }
    return vehicleManager;
  }

  public static VehicleManager? GetNearestVehicleManagerInBox(Vector3 castPositionRay, float maxDistance, Bounds bounds, VehicleManager? excludedManager)
  {
    var halfExtents = bounds.extents;
    halfExtents.y = 0.1f; // do not use a full 3d box otherwise it will be much larger cast.

    var hits = Physics.BoxCastNonAlloc(castPositionRay, halfExtents, Vector3.up, AllocatedRaycast, Quaternion.identity, maxDistance, LayerHelpers.PieceAndCustomVehicleMask);
    VehicleManager? vehicleManager = null;

    if (hits != 0)
    {
      for (var index = 0; index < hits; index++)
      {
        var raycastHit = AllocatedRaycast[index];
        if (TryGetVehicleManager(raycastHit.collider, out vehicleManager))
        {
          if (excludedManager != null && excludedManager == vehicleManager)
          {
            vehicleManager = null;
          }
          else
          {
            break;
          }
        }
      }
    }
    return vehicleManager;
  }

  public static VehicleManager? GetNearestVehicleManagerInRayOrSphere(Vector3 castPositionRay, Vector3 castPositionSphere, float maxDistanceRay, float maxSphereRadius, VehicleManager? excludedManager)
  {
    if (!GameCamera.instance || !GameCamera.instance) return null;
    if (!Player.m_localPlayer) return null;

    var vehicleManager = GetNearestVehicleManagerInRay(castPositionRay, maxDistanceRay, excludedManager);

    if (!vehicleManager)
    {
      vehicleManager = GetNearestVehicleManagerInSphere(castPositionSphere, maxSphereRadius, excludedManager);
    }

    if (!vehicleManager)
    {
      VehicleNotDetectedMessage();
      return null;
    }

    return vehicleManager;
  }

  /// <summary>
  /// Distance based on player relative to camera looking direction intersection.
  /// </summary>
  /// <returns></returns>
  public static VehicleManager? GetNearestVehicleManagerInSphere(Vector3 castPosition, float maxDistance, VehicleManager? excludedManager)
  {
    if (!GameCamera.instance || !Player.m_localPlayer) return null;

    VehicleManager? nearestManager = null;

    // Efficient static radius check — no false hits like SphereCastAll(…, 0f)
    var colliders = Physics.OverlapSphere(castPosition, maxDistance, LayerHelpers.PieceAndCustomVehicleMask);

    if (colliders.Length == 0)
      return null;

    for (var i = 0; i < colliders.Length; i++)
    {
      var collider = colliders[i];

      if (excludedManager != null &&
          excludedManager.PiecesController != null &&
          collider.transform.root == excludedManager.PiecesController.transform)
      {
        continue; // skip excluded vehicle
      }

      if (TryGetVehicleManager(collider, out var manager))
      {
        if (excludedManager != null && excludedManager == manager)
          continue;

        nearestManager = manager;
        break;
      }
    }

    return nearestManager;
  }

  /// <summary>
  /// Distance based on player relative to camera looking direction intersection.
  /// </summary>
  /// <returns></returns>
  public static VehicleManager? GetNearestVehicleManager()
  {
    if (!GameCamera.instance || !GameCamera.instance) return null;
    if (!Player.m_localPlayer) return null;

    var playerDistanceToCamera = Mathf.Abs(Vector3.Distance(Player.m_localPlayer.transform.position, GameCamera.instance.transform.position));
    var maxDistance = Mathf.Min(50f + playerDistanceToCamera, 200f);
    var cameraTransform = GameCamera.instance.m_camera.transform;
    var cameraPos = cameraTransform.position;
    var cameraDir = cameraTransform.forward;

    var localCast = Physics.Raycast(
      cameraPos,
      cameraDir,
      out var hitinfo, maxDistance,
      LayerHelpers.PieceAndCustomVehicleMask);
    VehicleManager? vehicleManager = null;

    if (localCast && TryGetVehicleManager(hitinfo.collider, out vehicleManager))
    {
      return vehicleManager;
    }

    // continue with heavier check if failed.
    var hits = Physics.RaycastNonAlloc(cameraPos, cameraDir, AllocatedRaycast, maxDistance, LayerHelpers.PieceAndCustomVehicleMask);
    if (hits == 0)
    {
      VehicleNotDetectedMessage();
      return null;
    }

    for (var index = 0; index < hits; index++)
    {
      var raycastHit = AllocatedRaycast[index];
      if (TryGetVehicleManager(raycastHit.collider, out vehicleManager)) break;
    }

    if (!vehicleManager)
    {
      VehicleNotDetectedMessage();
      return null;
    }

    return vehicleManager;
  }

  public static string GetPlayerPathInfo()
  {
    try
    {
      var playerFolderLocation =
        PlayerProfile.GetCharacterFolderPath(Game.instance.m_playerProfile
          .m_fileSource);
      var worldFolderLocation =
        World.GetWorldSavePath(Game.instance.m_playerProfile.m_fileSource);


      var logFile = PlayerProfile.GetPath(
                      Game.instance.m_playerProfile
                        .m_fileSource,
                      "Player.log") ??
                    $"Possible issue findingpath: guessing path is -> {Path.Combine(playerFolderLocation, "../Player.log")}";

      return string.Join("\n",
        $"PlayerProfile location: {playerFolderLocation}",
        $"Player.log location: {logFile}",
        $"WorldFolder location(may be N/A): {worldFolderLocation}");
    }
    catch
    {
      return "";
    }
  }

  private static void OnReportInfo()
  {
    var shipInstance =
      GetNearestVehicleManager();
    if (shipInstance == null)
      Logger.LogMessage(
        "No ship found, please run this command near the ship that needs to be reported.");

    var pieceController = shipInstance!.PiecesController;
    if (pieceController == null) return;

    var vehiclePendingPieces =
      pieceController?.GetCurrentPendingPieces();
    var vehiclePendingPiecesCount = vehiclePendingPieces?.Count ?? -1;
    var currentPendingState = pieceController!.PendingPiecesState;
    var pendingPiecesString =
      string.Join(",", vehiclePendingPieces?.Select(x => x.name) ?? []);
    if (pendingPiecesString == string.Empty) pendingPiecesString = "None";

    var piecesCount = pieceController.m_pieces.Count;
    var piecesString = string.Join(",",
      pieceController.m_pieces?.Select(x => x.name) ?? []);

    // todo swap all m_players to OnboardController.characterData check instead.
    var playersOnVehicle =
      string.Join(",", shipInstance?.OnboardController?.m_localPlayers.Select(
        (x) =>
          x?.GetPlayerName() ?? "Null Player") ?? []);

    var separatorDecorator = "================";
    var logSeparatorStart =
      $"{separatorDecorator} ValheimRaft/Vehicles report-info START {separatorDecorator}";
    var logSeparatorEnd =
      $"{separatorDecorator} ValheimRaft/Vehicles report-info END {separatorDecorator}";

    var charactersOnShip = shipInstance?.OnboardController?.GetCharactersOnShip() ?? [];

    Logger.LogMessage(string.Join("\n",
      logSeparatorStart,
      $"vehiclePieces, {piecesString}",
      $"vehiclePiecesCount, {piecesCount}",
      $"PendingPiecesState {currentPendingState}",
      $"vehiclePendingPieces: {pendingPiecesString}",
      $"vehiclePendingPiecesCount, {vehiclePendingPiecesCount}",
      $"playerPosition: {Player.m_localPlayer.transform.position}",
      $"PlayersOnVehicle: {playersOnVehicle}",
      $"vehiclePosition {shipInstance?.transform.position}",
      $"charactersOnboard: {charactersOnShip}",
      $"charactersOnboardCount: {charactersOnShip.Count}",
      GetPlayerPathInfo(),
      logSeparatorEnd
    ));
  }
  public static Stopwatch _creativeModeTimer = new();
  public static Coroutine? _creativeModeCoroutineInstance = null;

  public static void ToggleCreativeMode()
  {
    if (!CanRunEditCommand()) return;
    if (Game.instance == null)
    {
      _creativeModeCoroutineInstance = null;
      return;
    }

    if (_creativeModeCoroutineInstance != null && _creativeModeTimer.ElapsedMilliseconds < 5000f)
    {
      LoggerProvider.LogMessage("A creative-mode coroutine is already running. Please wait a second");
      return;
    }
    if (_creativeModeCoroutineInstance != null)
    {
      Game.instance.StopCoroutine(nameof(CreativeModeCoroutine));
      _creativeModeCoroutineInstance = null;
    }
    _creativeModeTimer.Restart();
    _creativeModeCoroutineInstance = Game.instance.StartCoroutine(CreativeModeCoroutine());
  }


  private static IEnumerator CreativeModeCoroutine()
  {
    var player = Player.m_localPlayer;
    if (!player)
    {
      Logger.LogWarning(
        $"Player does not exist, this command {VehicleCommandArgs.creative} cannot be run");
      _creativeModeCoroutineInstance = null;
      yield break;
    }

    var vehicleInstance = GetNearestVehicleManager();

    if (vehicleInstance == null || vehicleInstance.OnboardController == null || vehicleInstance.MovementController == null)
    {
      _creativeModeCoroutineInstance = null;
      _creativeModeTimer.Reset();
      yield break;
    }

    var nextCreativeMode = !vehicleInstance.isCreative;
    vehicleInstance.SetCreativeMode(nextCreativeMode);

    var isKinematic = vehicleInstance.isCreative;

    // creative mode is always kinematic and non-creative mode is not kinematic.
    // technically not needed as the FixedUpdate will auto set this. But since we are directly updating position, it's good to force set it in case fixedUpdate does not fire before some other physics is called.
    vehicleInstance.MovementController.rigidbody.isKinematic = isKinematic;

    if (nextCreativeMode == false)
    {
      _creativeModeCoroutineInstance = null;
      _creativeModeTimer.Reset();
      yield break;
    }


    var safeMoveData = SafeMovePlayerBefore(vehicleInstance.OnboardController);
    yield return MoveVehicleToCreativeTarget(vehicleInstance, GetCreativeModeTargetPosition(vehicleInstance), safeMoveData);

    // yield return SafeMovePlayer(vehicleInstance.OnboardController, true,
    //   () =>
    //   {
    //     var newPosition = GetCreativeModeTargetPosition(vehicleInstance);
    //     if (vehicleInstance.MovementController.m_body.isKinematic)
    //     {
    //       vehicleInstance.MovementController.m_body.MovePosition(newPosition);
    //     }
    //     else
    //     {
    //       vehicleInstance.MovementController.m_body.position = newPosition;
    //       vehicleInstance.MovementController.m_body.linearVelocity = Vector3.zero;
    //     }
    //     return newPosition;
    //   }, null, true);

    _creativeModeCoroutineInstance = null;
    _creativeModeTimer.Reset();
    LoggerProvider.LogMessage("Completed creative mode commands.");
  }
  public static float rotationLerp = 1f;
  public static float positionLerp = 1f;

  private static void SafeSyncSafeMovePlayerData(VehicleManager vehicleManager, SafeMoveData? safeMoveData)
  {
    if (!safeMoveData.HasValue) return;
    var MovementController = vehicleManager.MovementController;
    var PiecesController = vehicleManager.PiecesController;
    if (MovementController == null || PiecesController == null) return;
    foreach (var safeMoveCharacterData in safeMoveData.Value.charactersOnShip)
    {
      if (!safeMoveCharacterData.character) continue;
      if (!safeMoveCharacterData.character.m_nview.IsValid()) continue;

      var rb = safeMoveCharacterData.character.m_body;
      var localPosition = MovementController.m_body.position + safeMoveCharacterData.lastLocalOffset;
      if (rb.isKinematic)
      {
        rb.isKinematic = false;
      }
      else
      {
        rb.position = localPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
      }

      rb.linearVelocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;
      safeMoveCharacterData.character.m_nview.GetZDO()?.SetPosition(localPosition);
      safeMoveCharacterData.character.SyncVelocity();

      var colliders = safeMoveCharacterData.character.GetComponentsInChildren<Collider>();
      foreach (var collider in colliders)
      foreach (var collider1 in PiecesController.vehicleCollidersToIgnore)
      {
        if (!collider || !collider1) continue;
        Physics.IgnoreCollision(collider, collider1, true);
      }
    }
  }

  /// <summary>
  /// Safer way to move vehicle. This will keep players in vehicle as it animates to the position.
  /// </summary>
  private static IEnumerator MoveVehicleToCreativeTarget(VehicleManager vehicleInstance, Vector3 newPosition, SafeMoveData? safeMoveData)
  {
    var movementController = vehicleInstance.MovementController;
    if (movementController == null || vehicleInstance.PiecesController == null) yield break;

    var isNearby = false;

    var timer = 0f;

    while (!isNearby && timer < 5f)
    {
      timer += Time.deltaTime;
      var currentDistance = Vector3.Distance(movementController.m_body.position, newPosition);
      var isQuaternionNear = IsQuaternionNear(movementController.m_body.rotation, Quaternion.identity, 3f);

      isNearby = currentDistance < 0.1f && isQuaternionNear;

      LoggerProvider.LogDebugDebounced($"Distance: {currentDistance} isQuaternionNear {isQuaternionNear} isNearby {isNearby}");

      if (!isNearby)
      {
        SafeSyncSafeMovePlayerData(vehicleInstance, safeMoveData);
        var lerpTimePos = currentDistance < 0.5f ? 0.15f : Time.deltaTime * positionLerp;
        var lerpTimeRot = IsQuaternionNear(movementController.m_body.rotation, Quaternion.identity, 5f) ? 0.15f : Time.deltaTime * rotationLerp;

        var lerpPosition = Vector3.Lerp(movementController.m_body.position, newPosition, lerpTimePos);
        var lerpRotation = Quaternion.Lerp(movementController.m_body.rotation, Quaternion.identity, lerpTimeRot);

        movementController.m_body.Move(lerpPosition, lerpRotation);
        movementController.zsyncTransform.SyncNow();
        yield return null;
      }
    }

    // final movement.
    if (movementController.m_body.isKinematic)
    {
      movementController.m_body.Move(newPosition, Quaternion.identity);
    }
    else
    {
      movementController.m_body.position = newPosition;
      movementController.m_body.linearVelocity = Vector3.zero;
    }

    yield return null;
    SafeSyncSafeMovePlayerData(vehicleInstance, safeMoveData);

    yield return null;
  }

  public static bool IsQuaternionNear(Quaternion a, Quaternion b, float maxAngleDegrees = 1f)
  {
    // Calculates the angle in degrees between two rotations
    var angle = Quaternion.Angle(a, b);
    return angle < maxAngleDegrees;
  }

  private static Vector3 GetCreativeModeTargetPosition(VehicleManager vehicleInstance)
  {
    if (vehicleInstance == null || vehicleInstance.MovementController == null) return Vector3.zero;

    var position = vehicleInstance.MovementController.m_body.position;
    var creativeHeightOffset = VehicleGuiMenuConfig.VehicleCreativeHeight.Value;

    return new Vector3(position.x, position.y + creativeHeightOffset, position.z);
  }

  public static void ToggleColliderEditMode()
  {
    CreativeModeColliderComponent.ToggleEditMode();
    WaterZoneController.OnToggleEditMode(CreativeModeColliderComponent
      .IsEditMode);
  }

  public static void DestroyCurrentVehicle()
  {
    if (!CanRunCheatCommand()) return;
    if (!Player.m_localPlayer) return;
    var closestVehicle = GetNearestVehicleManager();
    if (closestVehicle == null || closestVehicle.PiecesController == null) return;

    var nvPiecesClone = closestVehicle.PiecesController.m_pieces.ToList();

    foreach (var piecesControllerMNviewPiece in nvPiecesClone)
    {
      if (piecesControllerMNviewPiece == null) continue;
      piecesControllerMNviewPiece.Destroy();
    }

    closestVehicle.Instance.m_nview.Destroy();

    LoggerProvider.LogMessage("Completed destroy vehicle command.");
  }

  public static void ClearAllVehicleBoundaryChunks()
  {
    if (!Player.m_localPlayer) return;
    var closestVehicle = GetNearestVehicleManager();
    if (closestVehicle == null || closestVehicle.PiecesController == null) return;
    closestVehicle.PiecesController.ClearAllBoundaryChunkData();
  }

  /// <summary>
  /// Clamps the Y position of a vehicle's ZDO to [minHeight, maxHeight].
  /// </summary>
  /// <returns>True if the position was changed, false if already within range or the ZDO was invalid.</returns>
  public static bool ClampVehicleZdoToSafeHeight(ZNetView vehicleNetView, float minHeight, float maxHeight)
  {
    if (!vehicleNetView || !vehicleNetView.IsValid()) return false;
    var vehicleZdo = vehicleNetView.GetZDO();
    if (vehicleZdo == null || !vehicleZdo.IsValid()) return false;

    var currentPos = vehicleZdo.GetPosition();

    var groundLevel = ZoneSystem.instance.GetGroundHeight(currentPos);
    var waterLevel = ZoneSystem.instance.m_waterLevel;

    var minGroundOrWaterHeight = Mathf.Max(groundLevel, waterLevel);

    var minGroundOrWaterOrClampedHeight = Mathf.Max(minGroundOrWaterHeight, minHeight);
    var maxGroundOrWaterOrClampedHeight = Mathf.Max(minGroundOrWaterHeight, maxHeight);

    Logger.LogDebug($"Clamping vehicle ZDO Y position. groundHeight {groundLevel}; waterLevel {waterLevel}; minHeight {minHeight}; maxHeight {maxHeight}. Got (min): minGroundOrWaterOrClampedHeight {minGroundOrWaterOrClampedHeight} (max): maxGroundOrWaterOrClampedHeight {maxGroundOrWaterOrClampedHeight}");

    var clampedY = Mathf.Clamp(currentPos.y, minGroundOrWaterOrClampedHeight, maxGroundOrWaterOrClampedHeight);
    if (Mathf.Approximately(clampedY, currentPos.y)) return false;

    vehicleZdo.SetPosition(new Vector3(currentPos.x, clampedY, currentPos.z));
    Logger.LogInfo(
      $"[{VehicleCommandArgs.repairAllVehiclePositions}] Vehicle ZDO {vehicleZdo.m_uid}: clamped Y from {currentPos.y:F2} to {clampedY:F2}.");
    return true;
  }

  /// <summary>
  /// Repairs a single vehicle: optionally clamps its ZDO Y position then force-syncs all its pieces.
  /// </summary>
  /// <returns>True if the vehicle height was clamped, false otherwise.</returns>
  public static bool RepairVehiclePosition(int persistentZdoId, float? minHeight, float? maxHeight)
  {
    VehiclePiecesController.ActiveInstances.TryGetValue(persistentZdoId, out var activeController);
    var vehicleNetView = activeController?.m_nview;

    var wasClamped = false;
    if (minHeight.HasValue && maxHeight.HasValue && vehicleNetView != null)
      wasClamped = ClampVehicleZdoToSafeHeight(vehicleNetView, minHeight.Value, maxHeight.Value);

    VehiclePiecesController.ForceSyncAllPrefabsToVehiclePosition(
      persistentZdoId,
      vehicleNetView,
      null);

    return wasClamped;
  }

  /// <summary>
  /// Iterates all tracked vehicles and repairs each one via <see cref="RepairVehiclePosition"/>.
  /// <para>
  /// Optional args: [minHeight] [maxHeight]
  /// When both are provided the vehicle ZDO Y position is clamped to [minHeight, maxHeight] before
  /// syncing its pieces. If only one or neither value is supplied the height constraint is skipped.
  /// </para>
  /// </summary>
  public static void RepairAllVehiclePositions(string[]? args)
  {
    float? minHeight = null;
    float? maxHeight = null;

    if (args != null && args.Length >= 2)
    {
      if (float.TryParse(args[0], out var parsedMin) && float.TryParse(args[1], out var parsedMax))
      {
        minHeight = parsedMin;
        maxHeight = parsedMax;
      }
      else
      {
        Logger.LogWarning(
          $"[{VehicleCommandArgs.repairAllVehiclePositions}] Could not parse minHeight/maxHeight from args '{args[0]}' / '{args[1]}'. " +
          "Both must be valid floats. Height constraint will be skipped.");
      }
    }

    var allPieces = VehiclePiecesController.m_allPieces;
    if (allPieces == null || allPieces.Count == 0)
    {
      Logger.LogMessage($"[{VehicleCommandArgs.repairAllVehiclePositions}] No vehicles found in m_allPieces.");
      return;
    }

    var repairedCount = 0;
    var clampedCount = 0;

    foreach (var kvp in allPieces)
    {
      if (RepairVehiclePosition(kvp.Key, minHeight, maxHeight))
        clampedCount++;
      repairedCount++;
    }

    Logger.LogMessage(
      $"[{VehicleCommandArgs.repairAllVehiclePositions}] Repaired {repairedCount} vehicle(s). " +
      (minHeight.HasValue && maxHeight.HasValue
        ? $"Height clamped [{minHeight:F2}, {maxHeight:F2}]: {clampedCount} vehicle(s) adjusted."
        : "No height constraint applied."));
  }

  public static void VehicleRecenter()
  {
    if (!Player.m_localPlayer)
    {
      Logger.LogWarning("Player does not exist, cannot run recenter command.");
      return;
    }

    var closestVehicle = GetNearestVehicleManager();
    if (closestVehicle == null || closestVehicle.PiecesController == null)
    {
      Logger.LogWarning("No vehicle nearby to recenter.");
      return;
    }

    var piecesController = closestVehicle.PiecesController;
    if (!piecesController.m_nview || !piecesController.m_nview.IsValid())
    {
      Logger.LogWarning("Vehicle network view is not valid.");
      return;
    }

    if (!piecesController.m_nview.IsOwner())
    {
      Logger.LogWarning("You must be the owner of the vehicle to recenter it.");
      return;
    }

    piecesController.ManualRecenterVehicleOrigin();
    Logger.LogMessage("Vehicle recenter initiated. Check logs for details.");
  }

  public override List<string> CommandOptionList()
  {
    return
    [
      // VehicleCommandArgs.locate, 
      VehicleCommandArgs.destroy,
#if DEBUG
      // config is only debug for now.
      VehicleCommandArgs.config,
#endif
      VehicleCommandArgs.debug,
      VehicleCommandArgs.rotate,
      VehicleCommandArgs.toggleOceanSway,
      VehicleCommandArgs.creative,
      VehicleCommandArgs.help,
      VehicleCommandArgs.recover,
      VehicleCommandArgs.reportInfo,
      VehicleCommandArgs.colliderEditMode,
      VehicleCommandArgs.move,
      VehicleCommandArgs.moveUp,
      VehicleCommandArgs.resetVehicleOwner,
      VehicleCommandArgs.clearBoundaryChunkData,
      VehicleCommandArgs.recenter,
      VehicleCommandArgs.repairAllVehiclePositions
    ];
  }
  public override string Name => "vehicle";
}