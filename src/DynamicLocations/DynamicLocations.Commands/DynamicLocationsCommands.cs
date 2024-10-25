using System.Collections.Generic;
using System.Linq;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Commands;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class KeyValueSerializer
{
  public static string Serialize(object obj, int indentLevel = 0)
  {
    if (obj == null) return string.Empty;

    var sb = new StringBuilder();
    var indent =
      new string(' ', indentLevel * 4); // 4 spaces per indentation level

    var type = obj.GetType();
    var properties =
      type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

    foreach (var prop in properties)
    {
      var value = prop.GetValue(obj);
      SerializeValue(sb, prop.Name, value, indent, indentLevel);
    }

    foreach (var field in fields)
    {
      var value = field.GetValue(obj);
      SerializeValue(sb, field.Name, value, indent, indentLevel);
    }

    return sb.ToString();
  }

  private static void SerializeValue(StringBuilder sb, string name,
    object value, string indent, int indentLevel)
  {
    if (value == null)
    {
      sb.AppendLine($"{indent}{name}: null");
    }
    else if (value is string || value.GetType().IsPrimitive) // Basic types
    {
      sb.AppendLine($"{indent}{name}: {value}");
    }
    else if (value is IEnumerable enumerable) // Collection types
    {
      sb.AppendLine($"{indent}{name}:");
      foreach (var item in enumerable)
      {
        SerializeValue(sb, "- Item", item, indent + "  ", indentLevel + 1);
      }
    }
    else // Complex types (nested objects)
    {
      sb.AppendLine($"{indent}{name}:");
      sb.Append(Serialize(value, indentLevel + 1));
    }
  }
}

// todo see if this is cleaner approach for other mod configs
// public readonly struct GenericKeyCommands(string commandPrefix)
// {
//   public string Limitations(string command)
//   {
//     if (Command == MoveTo)
//     {
//     }
//   }
//
//   public string MoveTo => $"{commandPrefix}MoveTo";
//
//
//   public string Help(string command)
//   {
//     var output = "";
//     if (command == Clear)
//     {
//       return
//         $"Clears all logouts of the current world related to {commandPrefix}";
//     }
//
//     if (command == Remove)
//     {
//       return
//         $"Removes all of <type> related to {commandPrefix}{Limitations}";
//     }
//
//     if (Limitations(command))
//     {
//       output += Limitations;
//     }
//
//     return output;
//     return "<" + command + ">";
//   }
//
//   public string Clear => $"{commandPrefix}Clear";
//   public string Remove => $"{commandPrefix}Remove";
//   public string Add => $"{commandPrefix}Add";
// }
//
// public readonly struct AdminCommands(string commandPrefix)
// {
//   public string MoveToSpawn => $"{commandPrefix}Clear";
//   public string MoveToLogout => $"{commandPrefix}Remove";
// }

// private static GenericKeyCommands PlayerCommands =
//   new GenericKeyCommands("player");
//
// private static GenericKeyCommands ServerCommands =
//   new GenericKeyCommands("server");

// private static GenericKeyCommands PlayerAdminCommands =
//   new GenericKeyCommands("player");

public class ListAllKeysCommand : ConsoleCommand
{
  public override void Run(string[] args)
  {
    if (args.Length == 0) return;
    LocationController.DEBUGCOMMAND_ListAllKeys();
  }

  public override string Name => "list-all-keys";
  public override string Help { get; } = "list all keys";
}

public class ModIntegrationsCommand : ConsoleCommand
{
  private static void ModIntegrationsCommands(bool isVerbose)
  {
    Logger.LogMessage("Listing all integrations by order");
    var index = 1;
    foreach (var dynamicLoginIntegration in LoginAPIController
               .loginIntegrationsByPriority)
    {
      if (isVerbose)
      {
        var serializedString =
          KeyValueSerializer.Serialize(dynamicLoginIntegration);
        Logger.LogMessage(serializedString);
      }
      else
      {
        Logger.LogMessage(
          $"[LoginIntegration]: [index: {index}] -> ModName: {dynamicLoginIntegration.Name} ModVersion {dynamicLoginIntegration.Version}, priority {dynamicLoginIntegration.Priority}, guid: {dynamicLoginIntegration.Guid}");
      }

      index++;
    }
  }

  private static bool IsVerboseFlag(string arg)
  {
    return arg is "-v" or "-verbose" or "--verbose";
  }

  public override void Run(string[] args)
  {
    if (args.Length == 0) return;
    var firstArgString = args.First();
    var isVerbose = IsVerboseFlag(firstArgString);
    ModIntegrationsCommands(isVerbose);
  }

  public override List<string> CommandOptionList() =>
  [
    "-v",
  ];

  public override string Name => "mod-integrations";

  private const string Indent = "\n-  ";

  public override string Help { get; } =
    $"Lists all mods using this plugin, e.g. ValheimRaft/Vehicles. {Indent}Name + version will be output only.{Indent}To see everything add -v will output the whole object per integration.";
}

public class MoveToCommand : ConsoleCommand
{
  public override void Run(string[] args)
  {
    if (args.Length == 0)
    {
      Logger.LogMessage(
        $"Requires an argument of {string.Join(",", CommandOptionList().ToArray())}");
      return;
    }

    var firstArg = args.First();
    var locationType = LocationVariationUtils.ToLocationVaration(firstArg);

    if (locationType == null)
    {
      Logger.LogMessage(
        $"Invalid input {firstArg}");
      return;
    }

    PlayerSpawnController.Instance?.DEBUG_MoveTo(locationType.Value);
  }

  public override List<string> CommandOptionList() =>
  [
    LocationVariationUtils.LogoutString,
    LocationVariationUtils.SpawnString,
  ];

  public override bool IsCheat => true;

  public override string Name => "move-to";

  public override string Help { get; } =
    "Moves to logout point. Requires Admin privileges";
}

/// <summary>
/// This is a JotunnCommand embedder. It embeds the commands within here. Though due to this it might not properly validate args run.
/// </summary>
public class DynamicLocationsCommands : ConsoleCommand
{
  public override string Help => OnHelp();

  // clear commands
  private const string playerClearAllCommand = "playerClearAll";

  private const string playerClearSpawnCommand =
    "playerClearSpawn";

  private const string playerClearLogoutCommand = "playerClearLogout";

  private const string serverClearAllCommand = "serverClearAll";
  private const string helpCommand = "help";

  private static readonly ListAllKeysCommand ListAllKeysCommandInstance = new();

  private static readonly ModIntegrationsCommand
    ModIntegrationsCommandInstance =
      new();

  private static readonly MoveToCommand MoveToCommandInstance = new();

  private static string FormatCommand(string? command) =>
    command == null ? "" : $"<{command}>:";

  private static string FormatCommand(string command, List<string>? args,
    string description) =>
    $"<{command}>: {string.Join("", args?.Select(arg => $" {FormatCommand(arg)}") ?? Array.Empty<string>())} {description}";

  private static string FormatCommand(ConsoleCommand consoleCommand) =>
    FormatCommand(consoleCommand.Name,
      consoleCommand.CommandOptionList(), consoleCommand.Help);

  private static string helpCommandItems =
    string.Join("\n", "DynamicLocationsCLI Mod CLI: ",
      $"{FormatCommand(playerClearLogoutCommand)} removes logout for the current world, will not change other worlds",
      $"{FormatCommand(playerClearAllCommand)} clears all dynamicLogin and dynamicSpawn points for a player for the corresponding world",
      FormatCommand(MoveToCommandInstance),
      FormatCommand(ListAllKeysCommandInstance),
      FormatCommand(ModIntegrationsCommandInstance));

  private enum CommandsEnum
  {
    Help,
    MoveToLocation,
    ClearAll,
    ClearSpawn,
    ClearLogout,
    ListAllKeys,
    ListIntegrations,
  }

  private static string OnHelp()
  {
    return helpCommandItems;
  }

  private static CommandsEnum? GetCommandArg(string commandString)
  {
    if (ModIntegrationsCommandInstance.Name == commandString)
      return CommandsEnum.ListIntegrations;

    if (ListAllKeysCommandInstance.Name == commandString)
      return CommandsEnum.ListAllKeys;

    if (MoveToCommandInstance.Name == commandString)
      return CommandsEnum.MoveToLocation;

    return commandString switch
    {
      playerClearAllCommand => CommandsEnum.ClearAll,
      playerClearLogoutCommand => CommandsEnum.ClearLogout,
      helpCommand => CommandsEnum.Help,
      _ => null
    };
  }

  public override void Run(string[] args)
  {
    if (args.Length == 0) return;
    ParseFirstArg(args);
  }

  private static void ParseFirstArg(string[]? args)
  {
    var commandString = args?.First() ?? "";
    if (commandString == "")
    {
      Logger.LogMessage(
        $"Must provide a argument for a command. Please run help for more information.");
    }

    var commandArg = GetCommandArg(commandString);
    if (commandArg == null)
    {
      Logger.LogMessage(
        $"Command {commandString} not recognized. Please run help for more information.");
      return;
    }

    // next arg can be null so we absolutely need to default to empty array if this happens
    var nextArgs = args?.Skip(1)?.ToArray() ?? [];

    switch (commandArg)
    {
      case CommandsEnum.ClearAll:
        PlayerClearAll(nextArgs);
        break;
      case CommandsEnum.ClearLogout:
      case CommandsEnum.ClearSpawn:
      case CommandsEnum.ListAllKeys:
        ListAllKeysCommandInstance.Run(nextArgs);
        break;
      case CommandsEnum.ListIntegrations:
        ModIntegrationsCommandInstance.Run(nextArgs);
        break;
      case CommandsEnum.Help:
        OnHelp();
        break;
      case null:
        break;
      case CommandsEnum.MoveToLocation:
        MoveToCommandInstance.Run(nextArgs);
        break;
      default:
        throw new ArgumentOutOfRangeException();
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

  public void PlayerListAllDynamicLocationKeys()
  {
    LocationController.DEBUGCOMMAND_ListAllKeys();
  }

  public static void PlayerClearLogout()
  {
    LocationController.DEBUGCOMMAND_RemoveLogout();
  }

  public override List<string> CommandOptionList() =>
  [
    // possibly new way to do things.
    ListAllKeysCommandInstance.Name,
    MoveToCommandInstance.Name,
    ModIntegrationsCommandInstance.Name,
    playerClearLogoutCommand,
    playerClearAllCommand,
    // playerClearDynamicLoginCommand,
    // playerClearDynamicSpawnCommand
  ];

  public override string Name => "dynamic-locations";
}