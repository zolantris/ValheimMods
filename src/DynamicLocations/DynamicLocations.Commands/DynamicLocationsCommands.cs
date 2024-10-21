using System.Collections.Generic;
using System.Linq;
using DynamicLocations.Controllers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Commands;

public class DynamicLocationsCommands : ConsoleCommand
{
  public override string Help => OnHelp();

  private const string playerClearAllCommand = "playerClearAll";

  private const string playerClearDynamicLoginCommand =
    "playerClearDynamicLogin";

  private const string playerClearDynamicSpawnCommand =
    "playerClearDynamicSpawn";

  private const string serverClearAllCommand = "serverClearAll";

  private static string OnHelp()
  {
    return
      "DynamicLocationsCLI Mod CLI: " +
      $"\n<{playerClearAllCommand}>: clears all dynamicLogin and dynamicSpawn points for a player for the corresponding world";
    // $"\n<{playerClearDynamicSpawnCommand}>: clears dynamicSpawn points for a player for the corresponding world" +
    // $"\n<{playerClearDynamicLoginCommand}>: clears dynamicLogin points for a player for the corresponding world" +
    // $"\n<{serverClearAllCommand}>: clears all dynamicLogin and dynamicSpawn points for all players";
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
      case playerClearAllCommand:
        PlayerClearAll(args.Skip(1).ToArray());
        break;
    }
  }

  private bool IsAdmin()
  {
    if (!SynchronizationManager.Instance.PlayerIsAdmin)
    {
      Logger.LogMessage(
        "Player is not admin, must be admin to run this command");
      return false;
    }

    return true;
  }

  /// <summary>
  /// Handles teleporting the player to fixed areas of the branch, provided the player is an admin. Eventually this could be used to calculate nearest section of the branch and teleport there
  /// </summary>
  /// <param name="args"></param>
  public static void PlayerClearAll(string[] args)
  {
    if (args.Length == 0)
    {
      LocationController.DEBUG_RemoveAllDynamicLocationKeys();
    }
  }

  public override List<string> CommandOptionList() =>
  [
    playerClearAllCommand,
    // playerClearDynamicLoginCommand,
    // playerClearDynamicSpawnCommand
  ];

  public override string Name => "dynamic-locations";
}