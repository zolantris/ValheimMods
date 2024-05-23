using System;
using System.Collections.Generic;
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
    public const string debug = "debug";
    public const string creative = "creative";
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
      // $"\n<{VehicleCommandArgs.rotate}>: defaults to zeroing x and z tilt. Can also provide 3 args: x y z" +
      // $"\n<{VehicleCommandArgs.move}>: Must provide 3 args: x y z, the movement is relative to those points" +
      $"\n<{VehicleCommandArgs.toggleOceanSway}>: stops the vehicle from swaying in the water. It will stay at 0 degrees (x and z) tilt and only allow rotating on y axis";
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
      // case VehicleCommandArgs.locate:
      //   LocateVehicle.LocateAllVehicles();
      //   break;
      // case VehicleCommandArgs.move:
      //   VehicleMove(args);
      //   break;
      // case VehicleCommandArgs.rotate:
      //   VehicleRotate(args);
      //   break;
      case VehicleCommandArgs.toggleOceanSway:
        VehicleStopOceanSway();
        break;
      case VehicleCommandArgs.recover:
        RecoverRaftConsoleCommand.RecoverRaftWithoutDryRun($"{Name} {VehicleCommandArgs.recover}");
        break;
      case VehicleCommandArgs.creative:
        CreativeModeConsoleCommand.RunCreativeModeCommand($"{Name} {VehicleCommandArgs.creative}");
        break;
      case VehicleCommandArgs.debug:
        ToggleVehicleDebugComponent();
        break;
      case VehicleCommandArgs.upgradeToV2:
        RunUpgradeToV2();
        break;
      case VehicleCommandArgs.downgradeToV1:
        RunDowngradeToV1();
        break;
    }
  }

  public void VehicleMove(string[] args)
  {
    var vehicleController = VehicleDebugHelpers.GetVehicleController();
    if (vehicleController == null)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 4)
    {
      float.TryParse(args[1], out var x);
      float.TryParse(args[2], out var y);
      float.TryParse(args[3], out var z);
      var offsetVector = new Vector3(x, y, z);
      vehicleController.VehicleInstance.Instance.transform.position += offsetVector;
    }
    else
    {
      Logger.LogMessage("Must provide x y z parameters, IE: vehicle rotate 0.5 0 10");
    }
  }

  /// <summary>
  /// Freezes the Vehicle rotation permenantly until the boat is unloaded similar to raftcreative 
  /// </summary>
  private static void VehicleStopOceanSway()
  {
    var vehicleController = VehicleDebugHelpers.GetVehicleController();
    if (!vehicleController)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    vehicleController?.VehicleInstance.Instance.MovementController.SendToggleOceanSway();
  }

  private static void VehicleRotate(string[] args)
  {
    var vehicleController = VehicleDebugHelpers.GetVehicleController();
    if (!vehicleController)
    {
      Logger.LogMessage("No VehicleController Detected");
      return;
    }

    if (args.Length == 1)
    {
      vehicleController.VehicleInstance.Instance.FixShipRotation();
    }

    if (args.Length == 4)
    {
      float.TryParse(args[1], out var x);
      float.TryParse(args[2], out var y);
      float.TryParse(args[3], out var z);
      vehicleController.VehicleInstance.Instance.transform.rotation = Quaternion.Euler(
        Mathf.Approximately(0f, x) ? 0 : x, Mathf.Approximately(0f, y) ? 0 : y,
        Mathf.Approximately(0f, z) ? 0 : z);
    }
    else
    {
      Logger.LogMessage("Must provide x y z parameters, IE: vehicle rotate 0.5 0 10");
    }
  }

  private static void RunDowngradeToV1()
  {
    var vehicleController = VehicleDebugHelpers.GetVehicleController();
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
    var piecesInVehicleController = vehicleController.GetCurrentPieces();

    foreach (var zNetView in piecesInVehicleController)
    {
      zNetView.m_zdo.Set(MoveableBaseRootComponent.MBParentIdHash,
        mbShip.GetMbRoot().GetPersistentId());
    }

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

    var vehiclePrefab = PrefabManager.Instance.GetPrefab(PrefabNames.WaterVehicleShip);
    var vehicleInstance = Object.Instantiate(vehiclePrefab, mbRaft.m_ship.transform.position,
      mbRaft.m_ship.transform.rotation, null);
    var vehicleShip = vehicleInstance.GetComponent<VehicleShip>();
    var vehicleController = vehicleShip.VehicleController;

    var piecesInMBRaft = mbRaft.m_pieces;
    foreach (var zNetView in piecesInMBRaft)
    {
      zNetView.m_zdo.Set(BaseVehicleController.MBParentIdHash, vehicleController.PersistentZdoId);
    }

    ZNetScene.instance.Destroy(mbRaft.m_ship.gameObject);
  }

  private static void ToggleVehicleDebugComponent()
  {
    var debugGui = ValheimRaftPlugin.Instance.GetComponent<VehicleDebugGui>();
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui(!(bool)debugGui);
    foreach (var vehicleShip in VehicleShip.AllVehicles)
    {
      vehicleShip.InitializeVehicleDebugger();
    }
  }

  public override List<string> CommandOptionList() =>
  [
    // VehicleCommandArgs.locate, 
    // VehicleCommandArgs.destroy,
    // VehicleCommandArgs.rotate,
    // VehicleCommandArgs.move,
    VehicleCommandArgs.toggleOceanSway,
    VehicleCommandArgs.creative,
    VehicleCommandArgs.debug,
    VehicleCommandArgs.help,
    VehicleCommandArgs.upgradeToV2,
    VehicleCommandArgs.downgradeToV1,
    VehicleCommandArgs.recover
  ];


  public override string Name => "vehicle";
}