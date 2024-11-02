using System;
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
    // public const string move = "move";
    // public const string destroy = "destroy";
    public const string listCachedMethods = "listCachedMethods";
    public const string reportInfo = "report-info";
    public const string debug = "debug";
    public const string creative = "creative";
    public const string colliderEditMode = "colliderEditMode";
    public const string help = "help";
    public const string recover = "recover";
    public const string rotate = "rotate";
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
      $"\n<{VehicleCommandArgs.move}>: Must provide 3 args: x y z, the movement is relative to those points" +
      $"\n<{VehicleCommandArgs.toggleOceanSway}>: stops the vehicle from swaying in the water. It will stay at 0 degrees (x and z) tilt and only allow rotating on y axis" +
      $"\n<{VehicleCommandArgs.reportInfo}>: outputs information related to the vehicle the player is on or near. This is meant for error reports" +
      $"\n<{VehicleCommandArgs.colliderEditMode}>: Lets the player toggle collider edit mode for all vehicles allowing editing water displacement masks and other hidden items" +
      $"\n<{VehicleCommandArgs.listCachedMethods}>: lists cached methods. Mostly for development debugging";
  }

  public override void Run(string[] args)
  {
    ParseFirstArg(args);
  }

  private void ParseFirstArg(string[] args)
  {
    var firstArg = args.First();
    if (firstArg == null)
    {
      Logger.LogMessage("Must provide a argument for `vehicle` command");
      return;
    }

    switch (firstArg)
    {
      case VehicleCommandArgs.move:
        VehicleMove(args);
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
      case VehicleCommandArgs.listCachedMethods:
        GameCacheController.ListCachedMethods();
        break;
    }
  }

  public void VehicleMove(string[] args)
  {
    var vehicleController = VehicleDebugHelpers.GetVehiclePiecesController();
    if (vehicleController == null)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 4 && vehicleController.VehicleInstance.Instance != null)
    {
      float.TryParse(args[1], out var x);
      float.TryParse(args[2], out var y);
      float.TryParse(args[3], out var z);
      var offsetVector = new Vector3(x, y, z);
      vehicleController.VehicleInstance.Instance.MovementController.m_body
        .isKinematic = true;
      vehicleController.VehicleInstance.Instance.transform.position +=
        offsetVector;
      Physics.SyncTransforms();
      vehicleController.VehicleInstance.Instance.MovementController.m_body
        .isKinematic = false;
    }
    else
    {
      Logger.LogMessage(
        "Must provide x y z parameters, IE: vehicle rotate 0.5 0 10");
    }
  }

  /// <summary>
  /// Freezes the Vehicle rotation permenantly until the boat is unloaded similar to raftcreative 
  /// </summary>
  public static void VehicleToggleOceanSway()
  {
    var vehicleController = VehicleDebugHelpers.GetVehiclePiecesController();
    if (!vehicleController)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    vehicleController?.VehicleInstance.Instance.MovementController
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
      return;
    }

    var pieceController = shipInstance.VehiclePiecesController;
    if (pieceController == null)
    {
      Logger.LogMessage(
        "No valid PieceController found on ship, report cannot be generated.");
      return;
    }

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
    VehicleCommandArgs.listCachedMethods,
  ];


  public override string Name => "vehicle";
}