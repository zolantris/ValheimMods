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

  public static string[] GetTargets(Dictionary<string, string> options, dynamic config)
  {
    if (!options.TryGetValue(Opt_Targets, out var targetNames) || string.IsNullOrWhiteSpace(targetNames))
    {
      Console.WriteLine("No targets specified. Available sync targets:");
      foreach (var target in config.deployTargets)
      {
        Console.WriteLine($"- {target.deployName}");
      }
      throw new Exception("You must specify at least one target via --targets=...");
    }

    var targetList = targetNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return targetList;
  }

  private static void HandleDeploy(Dictionary<string, string> options, dynamic config)
  {
    if (!options.TryGetValue("targets", out var targetNames) || string.IsNullOrWhiteSpace(targetNames))
    {
      Console.WriteLine("No targets specified. Available deploy targets:");
      foreach (var target in config.deployTargets)
      {
        Console.WriteLine($"- {target.deployName}");
      }
      throw new Exception("You must specify at least one target via --targets=...");
    }

    var targetList = targetNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    foreach (var tName in targetList)
    {
      var match = FindByName(config.deployTargets, "target", tName);
      if (match == null)
      {
        Console.WriteLine($"No deploy target found for '{tName}'");
        continue;
      }

      var folderPath = (string)match.pluginFolderPath;
      var folderName = (string)match.folderName;
      Console.WriteLine($"[DEPLOY] Would deploy to: {folderPath}\\{folderName}");
      // ... Deploy logic here
    }
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

  internal static dynamic FindByName(dynamic arr, string key, string value)
  {
    foreach (var obj in arr)
      if ((string)obj[key] == value)
        return obj;
    return null;
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

  // Your core post-install logic goes here
  private static void RunPostInstall(Dictionary<string, string> opts)
  {
    Console.WriteLine("=== All Option Key-Value Pairs ===");
    foreach (var pair in opts)
    {
      Console.WriteLine($"{pair.Key}: {pair.Value}");
    }
    Console.WriteLine("==============================");
    // Check required paths
    if (!opts.TryGetValue("solutionDir", out var solutionDir))
      throw new ArgumentException("--solutionDir not provided");
    if (!opts.TryGetValue("outDir", out var outDir))
      throw new ArgumentException("--outDir not provided");

    // Get path overrides directly from MSBuild
    opts.TryGetValue("modPostInstallerDir", out var modPostInstallerDir);
    opts.TryGetValue("modOutputDir", out var modOutputDir);

    Console.WriteLine($"modPostInstallerDir: {modPostInstallerDir}");
    Console.WriteLine($"modOutputDir: {modOutputDir}");

    // Get assembly name
    opts.TryGetValue("assemblyName", out var assemblyName);
    if (string.IsNullOrEmpty(assemblyName))
    {
      assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
    }

    // Get configuration
    opts.TryGetValue("config", out var configuration);
    var isDebug = configuration?.Contains("Debug") ?? true;
    var isRelease = configuration?.Contains("Release") ?? false;
    var isArchive = configuration?.Contains("Archive") ?? false;

    // Get deployment paths
    opts.TryGetValue("pluginDeployPath", out var pluginDeployPath);
    opts.TryGetValue("valheimServerPath", out var valheimServerPath);
    opts.TryGetValue("assetsDir", out var assetsDir);

    // Get flags
    opts.TryGetValue("isRunnable", out var isRunnableStr);
    opts.TryGetValue("isRunnableServer", out var isRunnableServerStr);
    var isRunnable = !string.IsNullOrEmpty(isRunnableStr) && isRunnableStr.ToLower() != "false";
    var isRunnableServer = !string.IsNullOrEmpty(isRunnableServerStr) && isRunnableServerStr.ToLower() != "false";

    // Exit codes for file locks
    opts.TryGetValue("serverFileExitCode", out var serverFileExitCodeStr);
    opts.TryGetValue("clientFileExitCode", out var clientFileExitCodeStr);
    opts.TryGetValue("sandboxieFileExitCode", out var sandboxieFileExitCodeStr);

    int.TryParse(serverFileExitCodeStr, out var serverFileExitCode);
    int.TryParse(clientFileExitCodeStr, out var clientFileExitCode);
    int.TryParse(sandboxieFileExitCodeStr, out var sandboxieFileExitCode);

    Console.WriteLine($"Running post-install for {assemblyName} in {configuration} mode");

    // Calculate paths
    var targetPath = Path.Combine(solutionDir, outDir);
    var pluginDeployTarget = "BepInEx/plugins";

    // Execute post-build steps based on the MSBuild file
    try
    {
      // Convert PDB to MDB
      PdbToMdbConverter.ConvertPdbToMdb(solutionDir, targetPath, assemblyName);

      // Copy to Valheim server if applicable
      if (isRunnable && !string.IsNullOrEmpty(valheimServerPath) && serverFileExitCode == 0)
      {
        SyncToTarget.CopyToValheimServer(solutionDir, targetPath, valheimServerPath, pluginDeployTarget, assemblyName, assetsDir);
      }

      // Copy to R2ModMan if applicable
      if (isRunnable && !string.IsNullOrEmpty(pluginDeployPath) && clientFileExitCode == 0 && !isRunnableServer)
      {
        SyncToTarget.CopyToR2ModMan(solutionDir, targetPath, pluginDeployPath, assemblyName, assetsDir);
      }

      // Copy to Sandboxie if applicable
      if (isRunnable && sandboxieFileExitCode == 0)
      {
        opts.TryGetValue("sandboxiePluginDeployPath", out var sandboxiePluginDeployPath);
        if (!string.IsNullOrEmpty(sandboxiePluginDeployPath))
        {
          SyncToTarget.CopyToSandboxie(solutionDir, targetPath, sandboxiePluginDeployPath, assemblyName, assetsDir);
        }
      }

      // Generate mod archive if applicable
      if (isArchive)
      {
        opts.TryGetValue("applicationVersion", out var applicationVersion);

        // Pass modOutputDir directly from MSBuild
        SyncToTarget.GenerateModArchive(solutionDir, targetPath, assemblyName, applicationVersion, isRelease, pluginDeployPath, modOutputDir);
      }
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error during post-install: {ex.Message}");
      throw;
    }
  }
}