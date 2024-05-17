using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.ConsoleCommands;

public class VehicleCommands : ConsoleCommand
{
  private class VehicleCommandArgs
  {
    // public const string locate = "locate";
    // public const string rotate = "rotate";
    // public const string move = "move";
    // public const string destroy = "destroy";
    public const string debug = "debug";
    public const string creative = "creative";
    public const string help = "help";
    public const string upgradeToV2 = "upgradeShipToV2";
    public const string downgradeToV1 = "downgradeShipToV1";
  }

  public override string Help => OnHelp();

  public string OnHelp()
  {
    // if (args)
    return
      "Runs vehicle commands, each command will require parameters to run use help to see the input values. <debug> will show a menu with options like rotating or debugging vehicle colliders";
  }

  private class RotateArgs
  {
    public const string rotateX = "x";
    public const string rotateY = "y";
    public const string rotateZ = "z";
  }

  public override void Run(string[] args)
  {
    ParseFirstArg(args);
  }

  private void ParseFirstArg(string[] args)
  {
    var firstArg = args.First();
    if (firstArg == null)
      return;

    switch (firstArg)
    {
      // case VehicleCommandArgs.locate:
      //   LocateVehicle.LocateAllVehicles();
      //   break;
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
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui();
    foreach (var vehicleShip in VehicleShip.AllVehicles)
    {
      vehicleShip.InitializeVehicleDebugger();
    }
  }

  public override List<string> CommandOptionList() =>
  [
    // VehicleCommandArgs.locate, 
    // VehicleCommandArgs.destroy,
    VehicleCommandArgs.creative,
    VehicleCommandArgs.debug,
    VehicleCommandArgs.help,
    VehicleCommandArgs.upgradeToV2,
    VehicleCommandArgs.downgradeToV1,
  ];


  public override string Name => "vehicle";
}