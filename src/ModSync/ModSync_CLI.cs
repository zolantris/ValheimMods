// File: Program.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModSync.Programs;

namespace ModSync;

public static class ModSyncCli
{
  // Main entrypoint for EXE
  // Public method to copy to multiple directories based on feature flags

  // Arg types
  public const string Arg_Sync = "sync";
  public const string Arg_Deploy = "deploy";
  public const string Arg_Run = "run";

  // option types
  public const string Opt_Help = "help";
  public const string Opt_Config = "config";
  public const string Opt_Verbose = "verbose";
  public const string Opt_DryRun = "dry-run";
  public const string Opt_Targets = "targets"; // required.

  public const string configFileName = "modSync.json5";


  public static int Main(string[] args)
  {
    if (args.Length == 0 || args.Length == 1 && (args[0] == Opt_Help || args[0] == "-h"))
    {
      PrintUsage();
      return 0;
    }


    var mode = args[0].ToLower();
    var options = ParseArgs(args);

    var isDefaultConfigPath = false;
    var currentDir = Environment.CurrentDirectory;

    if (!options.TryGetValue(Opt_Config, out var configPath))
    {
      isDefaultConfigPath = true;
      if (string.IsNullOrWhiteSpace(currentDir))
      {
        Console.Error.WriteLine("Could not determine EXE directory");
        return 1;
      }
      configPath = Path.Combine(currentDir, configFileName);
    }

    ModSyncConfig.UpdateConfigBooleans(options);

    if (options.ContainsKey("help") || mode == Opt_Help)
    {
      PrintUsage();
      return 0;
    }

    if (string.IsNullOrWhiteSpace(configPath))
    {
      Console.Error.WriteLine($"Config file not specified or could not be found at <{configPath}>. Please provide a --config or add a modSync.json in the directory of the modSync.exe file.");
      return 1;
    }

    if (!File.Exists(configPath))
    {
      var msg = $"Config file not found: {configPath}";
      if (isDefaultConfigPath)
      {
        msg += $"\nExecutingAssemblyPath: {currentDir}";
      }
      Console.Error.WriteLine(msg);
      return 1;
    }

    // Load and parse JSON5 config
    var json5String = File.ReadAllText(configPath);
    var configRoot = Json5Core.Json5.Deserialize<ModSyncConfig.ModSyncConfigObject>(json5String);

    var targets = GetTargets(options, configRoot);

    // Dispatch command
    switch (mode)
    {
      case Arg_Sync:
        SyncToTarget.HandleSync(targets, configRoot.syncTargets, configRoot);
        break;
      // case Arg_Deploy:
      //   HandleDeploy(options, configRoot.deployTargets);
      //   break;
      case Arg_Run:
        HandleRun(targets, configRoot.runTargets);
        break;
      default:
        PrintUsage();
        return 1;
    }

    return 0;
  }

  private static string[] GetTargets(Dictionary<string, string> options, ModSyncConfig.ModSyncConfigObject config)
  {
    if (!options.TryGetValue(Opt_Targets, out var targetNames) || string.IsNullOrWhiteSpace(targetNames))
    {
      Console.WriteLine("No targets specified. Available sync targets:");
      throw new Exception("You must specify at least one target via --targets=...");
    }

    var targetList = targetNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

    return targetList;
  }

  private static void HandleDeploy(Dictionary<string, string> options, ModSyncConfig.ModSyncConfigObject config)
  {
    // if (!options.TryGetValue("targets", out var targetNames) || string.IsNullOrWhiteSpace(targetNames))
    // {
    //   Console.WriteLine("No targets specified. Available deploy targets:");
    //   foreach (var target in config.runTargets)
    //   {
    //     Console.WriteLine($"- {target.deployName}");
    //   }
    //   throw new Exception("You must specify at least one target via --targets=...");
    // }
    //
    // var targetList = targetNames.Split(',', StringSplitOptions.RemoveEmptyEntries);
    //
    // foreach (var tName in targetList)
    // {
    //   var match = FindByName(config.deployTargets, "target", tName);
    //   if (match == null)
    //   {
    //     Console.WriteLine($"No deploy target found for '{tName}'");
    //     continue;
    //   }
    //
    //   var folderPath = (string)match.pluginFolderPath;
    //   var folderName = (string)match.folderName;
    //   Console.WriteLine($"[DEPLOY] Would deploy to: {folderPath}\\{folderName}");
    //   // ... Deploy logic here
    // }
  }

  private static void HandleRun(string[] targets, Dictionary<string, ModSyncConfig.RunTargetItem>? runTargets)
  {
    if (runTargets == null)
    {
      Console.WriteLine("No runTargets found in config");
      return;
    }

    foreach (var targetName in targets)
    {
      if (!runTargets.TryGetValue(targetName, out var match))
      {
        Console.WriteLine($"No runTarget found for '{targetName}'");
        continue;
      }

      var binary = match.binaryTarget;
      var args = string.Join(" ", match.args);

      if (ModSyncConfig.IsVerbose)
      {
        Console.WriteLine($"[SYNC] Would sync to: binary <{binary}> \\ args <{args}>");
      }
      if (ModSyncConfig.IsDryRun) continue;

      // ... Run logic here
    }
  }

  private static Dictionary<string, string> ParseArgs(string[] args)
  {
    var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 1; i < args.Length; i++)
    {
      var arg = args[i];
      if (arg.StartsWith("--") && arg.Contains('='))
      {
        var split = arg.Substring(2).Split('=', 2);
        opts[split[0]] = split[1].Trim('"');
      }
      else if (arg.StartsWith("--"))
      {
        var key = arg.Substring(2);
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
        {
          opts[key] = args[++i].Trim('"');
        }
        else
        {
          opts[key] = "true";
        }
      }
    }
    return opts;
  }

  private static void PrintUsage()
  {
    Console.WriteLine("Usage:");
    Console.WriteLine("  ModSync <sync|deploy|run> [--targets=foo,bar] [--config=./path/to/config/modSync.json5]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  ModSync sync --targets=default,test-server");
    Console.WriteLine("  ModSync run --targets=build-server");
    Console.WriteLine("  ModSync deploy --targets=default");
    Console.WriteLine();
    Console.WriteLine("Use --help for this message.");
  }
}