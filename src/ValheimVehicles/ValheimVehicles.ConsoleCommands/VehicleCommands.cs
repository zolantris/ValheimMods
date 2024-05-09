using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;

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
    }
  }

  public override List<string> CommandOptionList() =>
  [
    VehicleCommandArgs.locate, VehicleCommandArgs.debug, VehicleCommandArgs.destroy,
    VehicleCommandArgs.help
  ];


  public override string Name => "vehicle";
}