using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace YggdrasilTerrain.Commands;

public class YggdrasilTerrainCommands : ConsoleCommand
{
  public override string Help => OnHelp();

  private const string teleportCommand = "teleport";

  private static string OnHelp()
  {
    return
      "Yggdrassil Terrain Mod CLI: " +
      $"\n<{teleportCommand}>: teleports the player to the yggdrasil branch section nearest to them requires admin access";
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
      case teleportCommand:
        Teleport(args.Skip(1).ToArray());
        break;
    }
  }

  private Vector3 nearYggLocation = new Vector3(10018.89f, 1911.092f, -9.4584f);

  /// <summary>
  /// Handles teleporting the player to fixed areas of the branch, provided the player is an admin. Eventually this could be used to calculate nearest section of the branch and teleport there
  /// </summary>
  /// <param name="args"></param>
  public void Teleport(string[] args)
  {
    if (!SynchronizationManager.Instance.PlayerIsAdmin)
    {
      Logger.LogMessage(
        "Player is not admin, cannot teleport to Yggdrasil branch");
      return;
    }

    if (args.Length == 0)
    {
      Player.m_localPlayer.TeleportTo(nearYggLocation,
        Player.m_localPlayer.transform.rotation, true);
      return;
    }

    if (args.Length == 3)
    {
      float.TryParse(args[0], out var x);
      float.TryParse(args[1], out var y);
      float.TryParse(args[2], out var z);
      if (x == 0 || y == 0 || z == 0)
      {
        Logger.LogMessage(
          "Invalid vectors, must be 3 arguments that are floats or integers IE 1 1 1");
        return;
      }

      Player.m_localPlayer.TeleportTo(
        new Vector3(x, y, z),
        Player.m_localPlayer.transform.rotation, true);
    }
    else
    {
      Logger.LogMessage(
        $"Must provide x y z parameters, IE: {Name} {teleportCommand}  0.5 0 10");
    }
  }

  public override List<string> CommandOptionList() =>
  [
    teleportCommand
  ];

  public override string Name => "yggdrasilTerrain";
}