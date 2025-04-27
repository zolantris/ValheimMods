using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using ComfyGizmo;
using Components;
using HarmonyLib;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Assertions.Must;
using ValheimRAFT;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.SharedScripts;

using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Controllers;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace ValheimVehicles.ConsoleCommands;

public class VehicleCommands : ConsoleCommand
{
  private static class VehicleCommandArgs
  {
    // public const string locate = "locate";
    // public const string rotate = "rotate";
    // public const string destroy = "destroy";
    public const string reportInfo = "report-info";
    public const string debug = "debug";
    public const string config = "config";
    public const string creative = "creative";
    public const string colliderEditMode = "colliderEditMode";
    public const string help = "help";
    public const string recover = "recover";
    public const string rotate = "rotate";
    public const string moveUp = "moveUp";
    public const string move = "move";
    public const string toggleOceanSway = "toggleOceanSway";
    public const string upgradeToV2 = "upgradeShipToV2";
    public const string downgradeToV1 = "downgradeShipToV1";
    public const string resetVehicleOwner = "resetLocalOwnership";
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
      $"\n<{VehicleCommandArgs.debug}>: will show a menu with options like rotating or debugging vehicle colliders" +
      $"\n<{VehicleCommandArgs.recover}>: will recover any vehicles within range of 1000 and turn them into V2 Vehicles" +
      $"\n<{VehicleCommandArgs.rotate}>: defaults to zeroing x and z tilt. Can also provide 3 args: x y z" +
      $"\n<{VehicleCommandArgs.toggleOceanSway}>: stops the vehicle from swaying in the water. It will stay at 0 degrees (x and z) tilt and only allow rotating on y axis" +
      $"\n<{VehicleCommandArgs.reportInfo}>: outputs information related to the vehicle the player is on or near. This is meant for error reports" +
      $"\n<{VehicleCommandArgs.moveUp}>: Moves the vehicle within 50 units upwards by the value provided. Capped at 30 units to be safe. And Capped at 10 units lowest world position." +
      $"\n<{VehicleCommandArgs.move}>: Must provide 3 args: x y z, the movement is relative to those points" +
      $"\n<{VehicleCommandArgs.colliderEditMode}>: Lets the player toggle collider edit mode for all vehicles allowing editing water displacement masks and other hidden items";
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
      case VehicleCommandArgs.debug:
        ToggleVehicleCommandsHud();
        break;
#if DEBUG
      // config is not ready - only debug for now.
      case VehicleCommandArgs.config:
        ToggleVehicleGuiConfig();
        break;
#endif
      case VehicleCommandArgs.upgradeToV2:
        RunUpgradeToV2();
        break;
      case VehicleCommandArgs.reportInfo:
        OnReportInfo();
        break;
      case VehicleCommandArgs.downgradeToV1:
        RunDowngradeToV1();
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
    }
  }

  public void VehicleOwnerReset()
  {
    var currentVehicle =
      GetNearestVehicleShip(Player.m_localPlayer.transform.position);
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
      GetNearestVehicleShip(Player.m_localPlayer.transform.position);

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
    public bool isKinematic;
  }

  public struct SafeMoveData
  {
    public List<SafeMoveCharacterData> charactersOnShip;
    public bool IsLocalPlayerDebugFlying;
    public VehicleOnboardController OnboardController;
  }

  public static SafeMoveData? SafeMovePlayerBefore(
    VehicleOnboardController? vehicleOnboardController,
    bool shouldSkipPlayer = true)
  {
    if (vehicleOnboardController == null) return null;

    var charactersOnShip = vehicleOnboardController.GetCharactersOnShip();
    var characterData = new List<SafeMoveCharacterData>();

    if (!shouldSkipPlayer && !charactersOnShip.Contains(Player.m_localPlayer))
    {
      charactersOnShip.Add(Player.m_localPlayer);
    }

    // excludes current player to avoid double toggling.
    if (charactersOnShip.Count > 0)
      foreach (var character in charactersOnShip)
      {
        var isDebugFlying = character.IsDebugFlying();
        var isKinematic = character.m_body.isKinematic;

        if (!isKinematic && !isDebugFlying) character.m_body.isKinematic = true;

        var lastLocalOffset = character.transform.parent != null
          ? character.transform.localPosition
          : Vector3.zero;
        character.transform.SetParent(null);
        
        characterData.Add(new SafeMoveCharacterData()
        {
          character = character,
          isDebugFlying = isDebugFlying,
          lastLocalOffset = lastLocalOffset,
          isKinematic = isKinematic
        });
      }

    var wasDebugFlying = Player.m_localPlayer.IsDebugFlying();
    if (!wasDebugFlying) Player.m_localPlayer.m_body.isKinematic = true;

    return new SafeMoveData()
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
    yield return new WaitForFixedUpdate();

    if (coroutineFunc != null) yield return coroutineFunc;

    var nextPosition = GetPositionAfterMoveCallback();
    yield return SafeMoveCharacterAfter(safeMoveData, nextPosition, shouldProtectAgainstFallDamage);
    yield return new WaitForFixedUpdate();
  }

  public static void ResetPlayerVelocities(Character player)
  {
    if (player.m_body.isKinematic) player.m_body.isKinematic = false;

    player.m_body.angularVelocity = Vector3.zero;
    player.m_body.velocity = Vector3.zero;
    player.m_fallTimer = 0f;
    player.m_maxAirAltitude = -10000f;
  }

  public static void TeleportImmediately(Character character, Vector3 toPosition)
  {
    var player = character.GetComponent<Player>();
    if (player == null)
    {
      character.m_body.position = toPosition;
      character.m_nview.m_zdo.SetPosition(toPosition);
      character.m_nview.m_zdo.SetRotation(character.transform.rotation);
      return;
    }
    // reset teleporting
    player.m_teleporting = false;
    player.m_teleportTimer = 999f;
    player.TeleportTo(toPosition, character.transform.rotation, false);
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
    var piecesController =
      data.Value.OnboardController?.PiecesController?.transform;
    var zdo = data.Value.OnboardController?.vehicleShip?.NetView?.m_zdo;

    foreach (var safeMoveCharacterData in data.Value.charactersOnShip)
    {
      var targetLocation = nextPosition + safeMoveCharacterData.lastLocalOffset;
      if (piecesController != null && zdo != null)
      {
        var safeMoveCharacterPos = safeMoveCharacterData.character.transform.position;
        var piecesControllerPos = piecesController.transform.position;

        var deltaX = safeMoveCharacterPos.x -
                     piecesControllerPos.x;
        var deltaY = safeMoveCharacterPos.y -
                     piecesControllerPos.y;
        var deltaZ = safeMoveCharacterPos.z -
                     piecesControllerPos.z;

        if (Mathf.Abs(deltaX) > 50f || Mathf.Abs(deltaY) > 50f ||
            Mathf.Abs(deltaZ) > 50f)
          targetLocation = zdo
                             .GetPosition() +
                           safeMoveCharacterData.lastLocalOffset;
        
        TeleportImmediately(safeMoveCharacterData.character,
          targetLocation);
      }
    }

    yield return new WaitForFixedUpdate();

    var timer = Stopwatch.StartNew();
    var complete = false;
    while (timer.ElapsedMilliseconds < 5000 && complete == false)
    {
      if (complete) break;

      var isSuccess = true;
      foreach (var playerData in data.Value.charactersOnShip)
      {
        if (playerData.character.IsTeleporting())
        {
          isSuccess = false;
          break;
        }

        if (piecesController != null)
        {
          if (!playerData.isDebugFlying)
          {
            playerData.character.transform.SetParent(piecesController
              .transform);
            playerData.character.transform.localPosition =
              playerData.lastLocalOffset;
          }
          else
          {
            playerData.character.transform.position =
              piecesController.transform.position +
              playerData.lastLocalOffset;
          }
        }

        playerData.character.m_body.isKinematic = playerData.isKinematic;
        
        ResetPlayerVelocities(playerData.character);
      }

      yield return new WaitForFixedUpdate();

      complete = isSuccess;
    }

    if (!complete)
      foreach (var playerData in data.Value.charactersOnShip)
        ResetPlayerVelocities(playerData.character);

    if (shouldProtectAgainstFallDamage)
    {
      // keep running this for the first 5 seconds to prevent falldamage while the ship recovers it's physics and the player lands on the ship.
      while (timer.ElapsedMilliseconds < 5000)
      {
        foreach (var playerData in data.Value.charactersOnShip)
        {
          playerData.character.m_fallTimer = 0f;
          playerData.character.m_maxAirAltitude = -10000f;
        }

        yield return new WaitForFixedUpdate();
      }
    }
    timer.Restart();

    yield return null;
  }

  private static IEnumerator MoveVehicleIntoFarZone(VehicleShip vehicleInstance,
    Vector3 offset, Action<Vector3> onPositionReady)
  {
    var newLocation =
      VectorUtils.MergeVectors(vehicleInstance.transform.position, offset);

    // attempts to for the ship to move here.
    var zoneToMoveTo = ZoneSystem.GetZone(newLocation);
    if (!ZoneSystem.instance.PokeLocalZone(zoneToMoveTo))
      if (!ZoneSystem.instance.SpawnZone(zoneToMoveTo,
            ZoneSystem.SpawnMode.Full, out _))
        ZoneSystem.instance.CreateLocalZones(newLocation);

    var timer = Stopwatch.StartNew();
    yield return new WaitUntil(() =>
      ZoneSystem.instance.IsZoneLoaded(newLocation) ||
      timer.ElapsedMilliseconds > 5000);

    vehicleInstance.transform.position = newLocation;
    vehicleInstance.NetView.m_zdo.SetPosition(newLocation);
    VehiclePiecesController.ForceUpdateAllPiecePositions(
      vehicleInstance.PiecesController, newLocation);
    onPositionReady(vehicleInstance.transform.position);
  }

  /// <summary>
  /// Moves the vehicle based on the provided offset vector.
  /// - Must be only called via commands. This is meant to move the player even if they are not attached to the vehicle.
  /// </summary>
  /// <param name="vehicleInstance">The vehicle instance to move.</param>
  /// <param name="offset">The offset vector to apply.</param>
  private static IEnumerator MoveVehicle(VehicleShip? vehicleInstance,
    Vector3 offset)
  {
    if (vehicleInstance?.OnboardController == null)
      yield break;

    vehicleInstance.SetCreativeMode(true);
    yield return new WaitForFixedUpdate();

    Vector3? finalPosition = null;
    yield return SafeMovePlayer(vehicleInstance.OnboardController, true,
      () =>
      {
        return finalPosition ??
               vehicleInstance.OnboardController.transform.position;
      },
      MoveVehicleIntoFarZone(vehicleInstance, offset,
        (pos) => { finalPosition = pos; }));

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
    if (!VehicleDebugConfig.AllowDebugCommandsForNonAdmins.Value)
    {
      if (Player.m_localPlayer == null || ZNet.instance == false) return false;
      var playerId = Player.m_localPlayer.GetPlayerID();
      if (!ZNet.instance.IsAdmin(playerId))
      {
        Logger.LogMessage($"Player is not an admin. They cannot run this cheat command without setting configKeySection <{VehicleDebugConfig.AllowDebugCommandsForNonAdmins.Definition.Section}> ConfigKey: <{VehicleDebugConfig.AllowDebugCommandsForNonAdmins.Definition.Key}> to true in the raft config or being an Admin.");
        return false;
      }
    }

    return true;
  }

  public static bool CanRunEditCommand()
  {
    if (!VehicleDebugConfig.AllowDebugCommandsForNonAdmins.Value)
    {
      if (Player.m_localPlayer == null || ZNet.instance == false) return false;
      var playerId = Player.m_localPlayer.GetPlayerID();
      if (!ZNet.instance.IsAdmin(playerId))
      {
        Logger.LogMessage($"Player is not an admin. They cannot run this edit command without setting configKeySection <{VehicleDebugConfig.AllowEditCommandsForNonAdmins.Definition.Section}> ConfigKey: <{VehicleDebugConfig.AllowEditCommandsForNonAdmins.Definition.Key}> to true in the raft config or being an Admin.");
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
      GetNearestVehicleShip(Player.m_localPlayer.transform.position);
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
    var vehicleController = VehicleDebugHelpers.GetVehiclePiecesController();
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
    var vehicleController = VehicleDebugHelpers.GetVehiclePiecesController();
    if (!vehicleController)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 1)
      Game.instance.StartCoroutine(vehicleController?.VehicleInstance
        ?.MovementController?.FixShipRotation());

    if (args.Length == 4)
    {
      float.TryParse(args[1], out var x);
      float.TryParse(args[2], out var y);
      float.TryParse(args[3], out var z);
      vehicleController?.VehicleInstance?.Instance?.MovementController?.m_body
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

  private static void RunDowngradeToV1()
  {
    var vehicleController = VehicleDebugHelpers.GetVehiclePiecesController();
    if (!vehicleController)
    {
      Logger.LogMessage("No v1 raft detected");
      return;
    }

    var vehicleShip = vehicleController?.VehicleInstance;
    if (vehicleShip == null)
    {
      Logger.LogMessage("No VehicleShip detected exiting. Without downgrading");
      return;
    }

    var mbRaftPrefab = PrefabManager.Instance.GetPrefab(PrefabNames.MBRaft);
    var mbRaftPrefabInstance = Object.Instantiate(mbRaftPrefab,
      vehicleController.transform.position,
      vehicleController.transform.rotation, null);

    var mbShip = mbRaftPrefabInstance.GetComponent<MoveableBaseShipComponent>();
    var piecesInVehicleController = vehicleController.GetCurrentPendingPieces();

    foreach (var zNetView in piecesInVehicleController)
      zNetView.m_zdo.Set(MoveableBaseRootComponent.MBParentIdHash,
        mbShip.GetMbRoot().GetPersistentId());

    if (vehicleShip.Instance != null)
      ZNetScene.instance.Destroy(vehicleShip.Instance.gameObject);
  }

  private static void RunUpgradeToV2()
  {
    var mbRaft = VehicleDebugHelpers.GetMBRaftController();
    if (!mbRaft)
    {
      Logger.LogMessage("No v1 raft detected");
      return;
    }

    var vehiclePrefab =
      PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);

    if (mbRaft == null) return;

    var vehicleInstance = Object.Instantiate(vehiclePrefab,
      mbRaft.m_ship.transform.position,
      mbRaft.m_ship.transform.rotation, null);
    var vehicleShip = vehicleInstance.GetComponent<VehicleShip>();

    var piecesInMbRaft = mbRaft.m_pieces;
    foreach (var zNetView in piecesInMbRaft)
      zNetView.m_zdo.Set(VehicleZdoVars.MBParentIdHash,
        vehicleShip.PersistentZdoId);

    ZNetScene.instance.Destroy(mbRaft.m_ship.gameObject);
  }

  private static void ToggleVehicleGuiConfig()
  {
    if (!VehicleGui.Instance)
    {
      ValheimRaftPlugin.Instance.AddRemoveVehicleGui();
    }
    
    VehicleGui.ToggleConfigPanelState(true);
  }

  public static void ToggleVehicleCommandsHud()
  {
    if (!CanRunCheatCommand()) return;

    // must do this otherwise the commands panel will not cycle debug value if we need to enable it.
    VehicleGui.ToggleCommandsPanelState(true);
    VehicleShip.HasVehicleDebugger = VehicleGui.hasCommandsWindowOpened;
    
    foreach (var vehicleShip in VehicleShip.VehicleInstances)
    {
      if (vehicleShip.Value == null) return;
      vehicleShip.Value.AddOrRemoveVehicleDebugger();
    }
  }

  public static VehicleShip? GetNearestVehicleShip(Vector3 position)
  {
    if (!Physics.Raycast(
          GameCamera.instance.transform.position,
          GameCamera.instance.transform.forward,
          out var hitinfo, 50f,
          LayerMask.GetMask("piece") + LayerMask.GetMask("CustomVehicleLayer")))
    {
      Logger.LogWarning(
        $"boat not detected within 50f, get nearer to the boat and look directly at the boat");
      return null;
    }

    var vehiclePiecesController =
      hitinfo.collider.GetComponentInParent<VehiclePiecesController>();

    if (!(bool)vehiclePiecesController?.VehicleInstance?.Instance) return null;

    var vehicleShipController =
      vehiclePiecesController?.VehicleInstance?.Instance;
    return vehicleShipController;
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
      GetNearestVehicleShip(Player.m_localPlayer.transform.position);
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

    var piecesCount = pieceController.m_nviewPieces.Count;
    var piecesString = string.Join(",",
      pieceController.m_nviewPieces?.Select(x => x.name) ?? []);

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

  public static Coroutine? _creativeModeCoroutineInstance = null;

  public static void ToggleCreativeMode()
  {
    if (!CanRunEditCommand()) return;
    if (Game.instance == null)
    {
      _creativeModeCoroutineInstance = null;
      return;
    }

    if (_creativeModeCoroutineInstance != null)
    {
      LoggerProvider.LogMessage("A creative-mode coroutine is already running. Please wait a second");
      return;
    }
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

    var vehicleInstance = GetNearestVehicleShip(Player.m_localPlayer.transform.position);

    if (vehicleInstance == null || vehicleInstance.OnboardController == null || vehicleInstance.MovementController == null)
    {
      _creativeModeCoroutineInstance = null;
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
      yield break;
    }

    yield return SafeMovePlayer(vehicleInstance.OnboardController, true,
      () =>
      {
        var newPosition = GetCreativeModeTargetPosition(vehicleInstance);
        vehicleInstance.MovementController.m_body.position = newPosition;
        return newPosition;
      }, null, false);

    _creativeModeCoroutineInstance = null;
    LoggerProvider.LogMessage("Completed creative mode commands.");
  }

  private static Vector3 GetCreativeModeTargetPosition(VehicleShip vehicleInstance)
  {
    if (vehicleInstance == null || vehicleInstance.MovementController == null) return Vector3.zero;

    var position = vehicleInstance.MovementController.m_body.position;
    var creativeHeightOffset = VehicleDebugConfig.VehicleCreativeHeight.Value;

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
    var closestVehicle = GetNearestVehicleShip(Player.m_localPlayer.transform.position);
    if (!closestVehicle) return;

    var nvPiecesClone = closestVehicle.PiecesController.m_nviewPieces.ToList();

    foreach (var piecesControllerMNviewPiece in nvPiecesClone)
    {
      if (piecesControllerMNviewPiece == null) continue;
      piecesControllerMNviewPiece.Destroy();
    }

    closestVehicle.Instance.NetView.Destroy();

    LoggerProvider.LogMessage("Completed destroy vehicle command.");
  }

  public override List<string> CommandOptionList()
  {
    return
    [
      // VehicleCommandArgs.locate, 
      // VehicleCommandArgs.destroy,
#if DEBUG
      // config is only debug for now.
      VehicleCommandArgs.config,
#endif
      VehicleCommandArgs.debug,
      VehicleCommandArgs.rotate,
      VehicleCommandArgs.toggleOceanSway,
      VehicleCommandArgs.creative,
      VehicleCommandArgs.help,
      VehicleCommandArgs.upgradeToV2,
      VehicleCommandArgs.downgradeToV1,
      VehicleCommandArgs.recover,
      VehicleCommandArgs.reportInfo,
      VehicleCommandArgs.colliderEditMode,
      VehicleCommandArgs.move,
      VehicleCommandArgs.moveUp,
      VehicleCommandArgs.resetVehicleOwner
    ];
  }

  public override string Name => "vehicle";
}