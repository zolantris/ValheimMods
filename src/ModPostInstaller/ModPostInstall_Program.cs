// File: Program.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModPostInstaller;

public static class ModPostInstall_Program
{
  // Main entrypoint for EXE
  public static int Main(string[] args)
  {
    var options = ParseArgs(args);

    Console.WriteLine("=== ModPostInstaller Arguments ===");
    foreach (var kv in options)
    {
      Console.WriteLine($"{kv.Key}: {kv.Value}");
    }

    // TODO: Implement your post-install logic here.
    // Example: Copy files, convert PDB to MDB, etc.

    // Example: Run your core logic based on options
    try
    {
      RunPostInstall(options);
      Console.WriteLine("ModPostInstaller completed successfully.");
      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine("ModPostInstaller FAILED: " + ex);
      return 1;
    }
  }

  // Simple parser: --key value with support for environment variables and quoted values
  private static Dictionary<string, string> ParseArgs(string[] args)
  {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    string key = null;

    foreach (var arg in args)
    {
      if (arg.StartsWith("--"))
      {
        key = arg.Substring(2);
      }
      else if (key != null)
      {
        result[key] = arg;
        key = null;
      }
    }
    // Handle last arg if value is empty
    if (key != null && !result.ContainsKey(key))
      result[key] = "true";
    return result;
  }

  // Your core post-install logic goes here
  private static void RunPostInstall(Dictionary<string, string> opts)
  {
    // Example: Check required paths
    if (!opts.TryGetValue("solutionDir", out var solutionDir))
      throw new ArgumentException("--solutionDir not provided");
    if (!opts.TryGetValue("outDir", out var outDir))
      throw new ArgumentException("--outDir not provided");

    // (You may want to implement your logic using all passed options.)

    // EXAMPLE: File operations
    // var pluginDeployPath = opts.GetValueOrDefault("pluginDeployPath");
    // if (!string.IsNullOrWhiteSpace(pluginDeployPath))
    // {
    //     Console.WriteLine($"Would copy output to {pluginDeployPath}...");
    //     // File.Copy(...);
    // }

    // Add your custom logic for post-build steps here.
  }
}