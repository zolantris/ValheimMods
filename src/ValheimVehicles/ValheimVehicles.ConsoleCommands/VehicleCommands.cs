using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
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

    var currentPrefab = vehicleController?.m_nview?.m_zdo?.m_prefab;
    if (currentPrefab.Equals(PrefabNames.WaterVehicleShip.GetStableHashCode()))
    {
      vehicleController.m_nview.m_zdo.SetPrefab(PrefabNames.MBRaft.GetStableHashCode());
      Logger.LogMessage(
        "Downgraded raft to V1, please reload the sector or log out and return to the game");
    }
  }

  private static void RunUpgradeToV2()
  {
    var mbRaft = VehicleDebugHelpers.GetMBRaftController();
    if (!mbRaft)
    {
      Logger.LogMessage("No v1 raft detected");
      return;
    }

    var currentPrefab = mbRaft.m_nview.m_zdo.m_prefab;
    if (currentPrefab.Equals(PrefabNames.MBRaft.GetStableHashCode()))
    {
      mbRaft.m_nview.m_zdo.SetPrefab(PrefabNames.WaterVehicleShip.GetStableHashCode());
      Logger.LogMessage(
        "Updated raft to V2, please reload the sector or log out and return to the game");
    }
    else
    {
      Logger.LogMessage("RaftPrefab does not match expected name, skipping upgrade");
    }
  }

  private static void ToggleVehicleDebugComponent()
  {
    ValheimRaftPlugin.Instance.AddRemoveVehicleDebugGui();
    foreach (var vehicleShip in VehicleShip.AllVehicles)
    {
      vehicleShip.InitializeVehicleDebugger();
    }
  }

  // private ParseDebugArgs(string[] args)
  // {
  //   if (args.Length < 2)
  //   {
  //     return;
  //   }
  //
  //   var secondArg = args[1];
  //
  //   if (secondArg == VehicleCommandArgs.colliders)
  //   {
  //     
  //   }
  // }

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