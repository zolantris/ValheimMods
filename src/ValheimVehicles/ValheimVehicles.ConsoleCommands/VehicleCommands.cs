using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Components;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Assertions.Must;
using ValheimRAFT;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Controllers;
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
  }

  public override string Help => OnHelp();

  public static string OnHelp()
  {
    return
      "Runs vehicle commands, each command will require parameters to run use help to see the input values." +
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
        VehicleMove(nextArgs);
        break;
      case VehicleCommandArgs.toggleOceanSway:
        VehicleToggleOceanSway();
        break;
      case VehicleCommandArgs.rotate:
        VehicleRotate(args);
        break;
      case VehicleCommandArgs.recover:
        RecoverRaftConsoleCommand.RecoverRaftWithoutDryRun(
          $"{Name} {VehicleCommandArgs.recover}");
        break;
      case VehicleCommandArgs.creative:
        CreativeModeConsoleCommand.RunCreativeModeCommand(
          $"{Name} {VehicleCommandArgs.creative}");
        break;
      case VehicleCommandArgs.debug:
        ToggleVehicleDebugComponent();
        break;
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
        VehicleMoveVertically(nextArgs);
        break;
      case VehicleCommandArgs.help:
        Logger.LogMessage(OnHelp());
        break;
    }
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

    Player.m_localPlayer.StartCoroutine(MoveVehicle(vehicleInstance,
      Vector3.up * Mathf.Clamp(offset, -100f, 100f)));
  }

  public static SafeMovePlayerData? SafeMovePlayerBefore(
    VehicleOnboardController? vehicleOnboardController)
  {
    if (vehicleOnboardController == null)
    {
      return null;
    }

    var playersOnShip = vehicleOnboardController.GetPlayersOnShip();
    var wasDebugFlyingPlayers = new List<Player>();
    if (playersOnShip.Count > 0)
    {
      foreach (var player in playersOnShip)
      {
        if (player.IsDebugFlying())
        {
          wasDebugFlyingPlayers.Add(player);
          continue;
        }

        player.ToggleDebugFly();
      }
    }

    var wasDebugFlying = Player.m_localPlayer.IsDebugFlying();
    if (!wasDebugFlying)
    {
      Player.m_localPlayer.ToggleDebugFly();
    }

    return new SafeMovePlayerData()
    {
      PlayersOnShip = playersOnShip,
      PlayersWithDebugOnShip = wasDebugFlyingPlayers,
      IsLocalPlayerDebugFlying = wasDebugFlying,
    };
  }

  public struct SafeMovePlayerData
  {
    public List<Player> PlayersOnShip;
    public List<Player> PlayersWithDebugOnShip;
    public bool IsLocalPlayerDebugFlying;
  }

  public static void SafeMovePlayerAfter(SafeMovePlayerData? data)
  {
    if (data == null) return;
    if (data.Value.PlayersOnShip.Count > 0)
    {
      foreach (var player in data.Value.PlayersOnShip)
      {
        if (data.Value.PlayersWithDebugOnShip.Contains(player))
        {
          continue;
        }

        if (!player.IsDebugFlying())
        {
          continue;
        }

        player.ToggleDebugFly();
      }
    }

    if (!data.Value.IsLocalPlayerDebugFlying &&
        Player.m_localPlayer.IsDebugFlying())
    {
      Player.m_localPlayer.ToggleDebugFly();
    }
  }

  // todo add a wrapper for this so we can call before first then when the callback is called after
  // public static IEnumerator SafeMovePlayer(Action<bool> continueCallback)
  // {
  //   var result = false;
  //   yield return new WaitUntil()
  // }

  /// <summary>
  /// Moves the vehicle based on the provided offset vector.
  /// </summary>
  /// <param name="vehicleInstance">The vehicle instance to move.</param>
  /// <param name="offset">The offset vector to apply.</param>
  private static IEnumerator MoveVehicle(VehicleShip? vehicleInstance,
    Vector3 offset)
  {
    if (vehicleInstance == null) yield break;

    vehicleInstance.SetCreativeMode(true);

    var safeMoveData = SafeMovePlayerBefore(vehicleInstance.OnboardController);

    // Ensure Y position does not go below 1
    // vehicleInstance.transform.position.y + offset.y;
    vehicleInstance.transform.position =
      VectorUtils.MergeVectors(vehicleInstance.transform.position, offset);

    yield return new WaitForFixedUpdate();

    SafeMovePlayerAfter(safeMoveData);

    vehicleInstance.SetCreativeMode(false);
    yield return null;
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
      var offsetVector =
        VectorUtils.ClampVector(new Vector3(x, y, z), -100f, 100f);

      Player.m_localPlayer.StartCoroutine(MoveVehicle(shipInstance,
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
    {
      vehicleController?.VehicleInstance?.MovementController?.FixShipRotation();
    }

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
    {
      zNetView.m_zdo.Set(MoveableBaseRootComponent.MBParentIdHash,
        mbShip.GetMbRoot().GetPersistentId());
    }

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
    {
      zNetView.m_zdo.Set(VehicleZdoVars.MBParentIdHash,
        vehicleShip.PersistentZdoId);
    }

    ZNetScene.instance.Destroy(mbRaft.m_ship.gameObject);
  }

  private static void ToggleVehicleDebugComponent()
  {
    var debugGui = ValheimRaftPlugin.Instance.GetComponent<VehicleDebugGui>();
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui(!(bool)debugGui);
    foreach (var vehicleShip in VehicleShip.AllVehicles)
    {
      vehicleShip.Value?.InitializeVehicleDebugger();
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
    {
      Logger.LogMessage(
        "No ship found, please run this command near the ship that needs to be reported.");
    }

    var pieceController = shipInstance!.VehiclePiecesController;
    if (pieceController == null) return;

    var vehiclePendingPieces =
      pieceController?.GetCurrentPendingPieces();
    var vehiclePendingPiecesCount = vehiclePendingPieces?.Count ?? -1;
    var currentPendingState = pieceController!.PendingPiecesState;
    var pendingPiecesString =
      string.Join(",", vehiclePendingPieces?.Select(x => x.name) ?? []);
    if (pendingPiecesString == string.Empty)
    {
      pendingPiecesString = "None";
    }

    var piecesString = string.Join(",",
      pieceController.m_pieces?.Select(x => x.name) ?? []);

    // todo swap all m_players to OnboardController.characterData check instead.
    var playersOnVehicle =
      string.Join(",", shipInstance?.MovementController?.m_players.Select((x) =>
        x?.GetPlayerName() ?? "Null Player") ?? []);

    var separatorDecorator = "================";
    var logSeparatorStart =
      $"{separatorDecorator} ValheimRaft/Vehicles report-info START {separatorDecorator}";
    var logSeparatorEnd =
      $"{separatorDecorator} ValheimRaft/Vehicles report-info END {separatorDecorator}";

    Logger.LogMessage(string.Join("\n",
      logSeparatorStart,
      vehiclePendingPiecesCount,
      $"vehiclePieces, {vehiclePendingPiecesCount}",
      $"vehiclePiecesCount, {piecesString}",
      $"PendingPiecesState {currentPendingState}",
      $"vehiclePendingPieces: {pendingPiecesString}",
      $"vehiclePendingPiecesCount, {vehiclePendingPiecesCount}",
      $"playerPosition: {Player.m_localPlayer.transform.position}",
      $"PlayersOnVehicle: {playersOnVehicle}",
      $"vehiclePosition {shipInstance?.transform.position}",
      GetPlayerPathInfo(),
      logSeparatorEnd
    ));
  }

  public static void ToggleColliderEditMode()
  {
    CreativeModeColliderComponent.ToggleEditMode();
    WaterZoneController.OnToggleEditMode(CreativeModeColliderComponent
      .IsEditMode);
  }

  public override List<string> CommandOptionList() =>
  [
    // VehicleCommandArgs.locate, 
    // VehicleCommandArgs.destroy,
    VehicleCommandArgs.rotate,
    VehicleCommandArgs.toggleOceanSway,
    VehicleCommandArgs.creative,
    VehicleCommandArgs.debug,
    VehicleCommandArgs.help,
    VehicleCommandArgs.upgradeToV2,
    VehicleCommandArgs.downgradeToV1,
    VehicleCommandArgs.recover,
    VehicleCommandArgs.reportInfo,
    VehicleCommandArgs.colliderEditMode,
    VehicleCommandArgs.move,
    VehicleCommandArgs.moveUp,
  ];


  public override string Name => "vehicle";
}