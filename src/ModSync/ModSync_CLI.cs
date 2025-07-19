// File: Program.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModSync.Programs;

namespace ModSync;

public static class ModPostInstall_Program
{
  // Main entrypoint for EXE
  // Public method to copy to multiple directories based on feature flags

  // Arg types
  public const string Arg_Sync = "sync";
  public const string Arg_Deploy = "deploy";
  public const string Arg_Run = "run";
  public const string Arg_Help = "--help";
  public const string DefaultConfigPath = "modSync.json5";

  public static int Main(string[] args)
  {
    if (args.Length == 0 || args.Length == 1 && (args[0] == Arg_Help || args[0] == "-h"))
    {
      PrintUsage();
      return 0;
    }

    var mode = args[0].ToLower();
    var options = ParseArgs(args);

    if (options.ContainsKey("help") || mode == Arg_Help)
    {
      PrintUsage();
      return 0;
    }

    // Select config file (can be overridden by --config=myconfig.json5)
    options.TryGetValue("config", out var configPath);
    configPath = string.IsNullOrWhiteSpace(configPath) ? DefaultConfigPath : configPath;
    if (!File.Exists(configPath))
    {
      Console.Error.WriteLine($"Config file not found: {configPath}");
      return 1;
    }

    // Load and parse JSON5 config
    dynamic configRoot = Json5Document.Parse(File.ReadAllText(configPath)).ToDynamic();

    // Dispatch command
    switch (mode)
    {
      case Arg_Sync:
        HandleSync(options, configRoot);
        break;
      case Arg_Deploy:
        HandleDeploy(options, configRoot);
        break;
      case Arg_Run:
        HandleRun(options, configRoot);
        break;
      default:
        PrintUsage();
        return 1;
    }

    return 0;
  }

  private static void HandleSync(Dictionary<string, string> options, dynamic config)
  {
    // Parse comma-separated deployTargets (by deployName)
    if (!options.TryGetValue("deployTargets", out var deployTargetNames) || string.IsNullOrWhiteSpace(deployTargetNames))
    {
      Console.WriteLine("No deployTargets specified. Available targets:");
      foreach (var target in config.deployTargets)
      {
        Console.WriteLine($"- {target.deployName}");
      }
      return;
    }

    var targetList = deployTargetNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var tName in targetList)
    {
      var match = FindByName(config.deployTargets, "deployName", tName);
      if (match == null)
      {
        Console.WriteLine($"No deploy target found for '{tName}'");
        continue;
      }

      var folderPath = (string)match.pluginFolderPath;
      var folderName = (string)match.folderName;
      Console.WriteLine($"[SYNC] Would sync to: {folderPath}\\{folderName}");
      // ... Your sync logic here (e.g. CopyDirectory)
    }
  }

  private static void HandleDeploy(Dictionary<string, string> options, dynamic config)
  {
    // You could run actual deploy logic here
    Console.WriteLine("Deploy not implemented. Available deployTargets:");
    foreach (var target in config.deployTargets)
    {
      Console.WriteLine($"- {target.deployName}");
    }
  }

  private static void HandleRun(Dictionary<string, string> options, dynamic config)
  {
    if (!options.TryGetValue("runTargets", out var runNames) || string.IsNullOrWhiteSpace(runNames))
    {
      Console.WriteLine("No runTargets specified. Available targets:");
      foreach (var rt in config.runTargets)
      {
        Console.WriteLine($"- {rt.name}");
      }
      return;
    }

    var runList = runNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var rName in runList)
    {
      var match = FindByName(config.runTargets, "name", rName);
      if (match == null)
      {
        Console.WriteLine($"No run target found for '{rName}'");
        continue;
      }
      Console.WriteLine($"[RUN] Would run: {match.binaryTarget} {string.Join(" ", match.args)}");
      // ... Your launch logic here
    }
  }

  private static dynamic FindByName(dynamic arr, string key, string value)
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
    Console.WriteLine("  ModSync <sync|deploy|run> [--deployTargets=foo,bar] [--runTargets=run1,run2] [--config=deploy.config.json5]");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  ModSync sync --deployTargets=default,test-server");
    Console.WriteLine("  ModSync run --runTargets=build-server");
    Console.WriteLine("  ModSync deploy --deployTargets=default");
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
      assemblyName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
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