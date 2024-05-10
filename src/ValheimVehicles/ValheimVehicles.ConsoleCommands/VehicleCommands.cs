using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using ValheimRAFT;
using ValheimVehicles.Vehicles;

namespace ValheimVehicles.ConsoleCommands;

public class VehicleCommands : ConsoleCommand
{
  private class VehicleCommandArgs
  {
    public const string locate = "locate";
    public const string rotate = "rotate";
    public const string move = "move";
    public const string destroy = "destroy";
    public const string debug = "debug";
    public const string help = "help";
  }

  public override string Help => OnHelp();

  public string OnHelp()
  {
    // if (args)
    return
      "Runs vehicle commands, each command will require parameters to run use help to see the input values";
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
      case VehicleCommandArgs.locate:
        LocateVehicle.LocateAllVehicles();
        break;
      case VehicleCommandArgs.debug:
        ToggleVehicleDebugComponent();
        break;
    }
  }

  private static void ToggleVehicleDebugComponent()
  {
    ValheimRaftPlugin.Instance.ToggleVehicleDebugGui();
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
    VehicleCommandArgs.locate, VehicleCommandArgs.debug, VehicleCommandArgs.destroy,
    VehicleCommandArgs.help
  ];


  public override string Name => "vehicle";
}