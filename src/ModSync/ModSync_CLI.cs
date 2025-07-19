// File: Program.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModSync;

public static class ModPostInstall_Program
{
  // Main entrypoint for EXE
  // Public method to copy to multiple directories based on feature flags
  public static void copyToDir(string sourceDir, string targetDir, Dictionary<string, string> flags)
  {
    if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
    {
      Console.WriteLine("Warning: Source or target directory is null or empty");
      return;
    }

    Console.WriteLine($"Copying from {sourceDir} to {targetDir}");
    CopyDirectory(sourceDir, targetDir);

    // Copy to additional locations based on flags
    if (flags.TryGetValue("clientDeployPath", out var clientPath) && !string.IsNullOrEmpty(clientPath) && flags.ContainsKey("isClient"))
    {
      Console.WriteLine($"Copying to client path: {clientPath}");
      CopyDirectory(sourceDir, clientPath);
    }

    if (flags.TryGetValue("serverDeployPath", out var serverPath) && !string.IsNullOrEmpty(serverPath) && flags.ContainsKey("isServer"))
    {
      Console.WriteLine($"Copying to server path: {serverPath}");
      CopyDirectory(sourceDir, serverPath);
    }

    if (flags.TryGetValue("sandboxieDeployPath", out var sandboxiePath) && !string.IsNullOrEmpty(sandboxiePath) && flags.ContainsKey("isSandboxie"))
    {
      Console.WriteLine($"Copying to sandboxie path: {sandboxiePath}");
      CopyDirectory(sourceDir, sandboxiePath);
    }
  }

  public const string Arg_Sync = "sync";
  public const string Arg_Deploy = "sync";
  public const string Arg_Run = "run";

  public static int Main(string[] args)
  {
    var options = ParseArgs(args);

    Console.WriteLine("=== ModPostInstaller Arguments ===");
    foreach (var kv in options)
    {
      Console.WriteLine($"{kv.Key}: {kv.Value}");
    }

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

  // Simple parser: --key value or --key=value with support for environment variables and quoted values
  private static Dictionary<string, string> ParseArgs(string[] args)
  {
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    string key = null;

    foreach (var arg in args)
    {
      // Handle --key=value format
      if (arg.StartsWith("--") && arg.Contains('='))
      {
        var parts = arg.Substring(2).Split(new[] { '=' }, 2);
        if (parts.Length == 2)
        {
          result[parts[0]] = parts[1].Trim('"'); // Remove quotes if present
          key = null;
          continue;
        }
      }

      // Handle --key value format
      if (arg.StartsWith("--"))
      {
        key = arg.Substring(2);
      }
      else if (key != null)
      {
        result[key] = arg.Trim('"'); // Remove quotes if present
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
      ConvertPdbToMdb(solutionDir, targetPath, assemblyName);

      // Copy to Valheim server if applicable
      if (isRunnable && !string.IsNullOrEmpty(valheimServerPath) && serverFileExitCode == 0)
      {
        CopyToValheimServer(solutionDir, targetPath, valheimServerPath, pluginDeployTarget, assemblyName, assetsDir);
      }

      // Copy to R2ModMan if applicable
      if (isRunnable && !string.IsNullOrEmpty(pluginDeployPath) && clientFileExitCode == 0 && !isRunnableServer)
      {
        CopyToR2ModMan(solutionDir, targetPath, pluginDeployPath, assemblyName, assetsDir);
      }

      // Copy to Sandboxie if applicable
      if (isRunnable && sandboxieFileExitCode == 0)
      {
        opts.TryGetValue("sandboxiePluginDeployPath", out var sandboxiePluginDeployPath);
        if (!string.IsNullOrEmpty(sandboxiePluginDeployPath))
        {
          CopyToSandboxie(solutionDir, targetPath, sandboxiePluginDeployPath, assemblyName, assetsDir);
        }
      }

      // Generate mod archive if applicable
      if (isArchive)
      {
        opts.TryGetValue("applicationVersion", out var applicationVersion);

        // Pass modOutputDir directly from MSBuild
        GenerateModArchive(solutionDir, targetPath, assemblyName, applicationVersion, isRelease, pluginDeployPath, modOutputDir);
      }
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine($"Error during post-install: {ex.Message}");
      throw;
    }
  }

  public static void ConvertPdbToMdb(string solutionDir, string targetPath, string assemblyName)
  {
    Console.WriteLine("Converting PDB to MDB...");
    var targetDll = Path.Combine(targetPath, $"{assemblyName}.dll");
    var pdb2mdbPath = Path.Combine(solutionDir, "pdb2mdb.exe");

    if (!File.Exists(pdb2mdbPath))
    {
      Console.WriteLine($"Warning: pdb2mdb.exe not found at {pdb2mdbPath}, skipping conversion");
      return;
    }

    try
    {
      // Run pdb2mdb.exe
      var psi = new System.Diagnostics.ProcessStartInfo
      {
        FileName = pdb2mdbPath,
        Arguments = $"\"{targetDll}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = System.Diagnostics.Process.Start(psi))
      {
        if (process != null)
        {
          process.WaitForExit();
        }
        else
        {
          Console.WriteLine("Warning: pdb2mdb.exe process is null. It exited way to early.");
        }

        Console.WriteLine(process.StandardOutput.ReadToEnd());

        if (process.ExitCode != 0)
        {
          Console.WriteLine($"Warning: pdb2mdb.exe exited with code {process.ExitCode}");
        }
      }

      // Rename the .dll.mdb to .mdb
      var sourceMdb = Path.Combine(targetPath, $"{assemblyName}.dll.mdb");
      var targetMdb = Path.Combine(targetPath, $"{assemblyName}.mdb");

      if (File.Exists(sourceMdb))
      {
        if (File.Exists(targetMdb))
          File.Delete(targetMdb);

        File.Move(sourceMdb, targetMdb);
        Console.WriteLine($"Renamed {sourceMdb} to {targetMdb}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error during PDB to MDB conversion: {ex.Message}");
    }
  }

  private static void CopyToValheimServer(string solutionDir, string targetPath, string valheimServerPath,
    string pluginDeployTarget, string assemblyName, string assetsDir)
  {
    Console.WriteLine($"Copying files to Valheim server at {valheimServerPath}...");

    if (!Directory.Exists(valheimServerPath))
    {
      Console.WriteLine($"Warning: Valheim server path does not exist: {valheimServerPath}");
      return;
    }

    var serverPluginPath = Path.Combine(valheimServerPath, pluginDeployTarget);
    Directory.CreateDirectory(serverPluginPath);

    // Copy dependencies
    var dependenciesPath = Path.Combine(solutionDir, "Dependencies");
    if (Directory.Exists(dependenciesPath))
    {
      CopyDirectory(dependenciesPath, serverPluginPath);
    }

    // Copy main files
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.dll"), Path.Combine(serverPluginPath, $"{assemblyName}.dll"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.pdb"), Path.Combine(serverPluginPath, $"{assemblyName}.pdb"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.mdb"), Path.Combine(serverPluginPath, $"{assemblyName}.mdb"));

    // Copy assets if they exist
    if (!string.IsNullOrEmpty(assetsDir) && Directory.Exists(assetsDir))
    {
      var serverAssetsPath = Path.Combine(serverPluginPath, "Assets");
      CopyDirectory(assetsDir, serverAssetsPath);

      // Copy translations if they exist
      var translationsDir = Path.Combine(assetsDir, "Translations", "English");
      if (Directory.Exists(translationsDir))
      {
        var serverTranslationsPath = Path.Combine(serverPluginPath, "Assets", "Translations", "English");
        CopyDirectory(translationsDir, serverTranslationsPath);
      }
    }
    else
    {
      Console.WriteLine($"Warning: Assets directory does not exist: {assetsDir}. Please make the directly. This code does not create directories for the top level plugins as a safety precaution.");
    }
  }

  private static void CopyToR2ModMan(string solutionDir, string targetPath, string pluginDeployPath, string assemblyName, string assetsDir)
  {
    Console.WriteLine($"Copying files to R2ModMan at {pluginDeployPath}...");

    if (!Directory.Exists(pluginDeployPath))
    {
      Directory.CreateDirectory(pluginDeployPath);
    }

    // Copy dependencies
    var dependenciesPath = Path.Combine(solutionDir, "Dependencies");
    if (Directory.Exists(dependenciesPath))
    {
      CopyDirectory(dependenciesPath, pluginDeployPath);
    }

    // Copy main files
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.dll"), Path.Combine(pluginDeployPath, $"{assemblyName}.dll"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.pdb"), Path.Combine(pluginDeployPath, $"{assemblyName}.pdb"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.mdb"), Path.Combine(pluginDeployPath, $"{assemblyName}.mdb"));

    // Handle SentryUnityWrapper
    if (assemblyName == "SentryUnityWrapper")
    {
      var sentryPath = Path.Combine(solutionDir, "SentryUnity", "1.8.0", "runtime");
      if (Directory.Exists(sentryPath))
      {
        CopyDirectory(sentryPath, pluginDeployPath);
      }
    }

    // Copy assets if they exist
    if (!string.IsNullOrEmpty(assetsDir) && Directory.Exists(assetsDir))
    {
      var clientAssetsPath = Path.Combine(pluginDeployPath, "Assets");
      CopyDirectory(assetsDir, clientAssetsPath);

      // Copy translations if they exist
      var translationsDir = Path.Combine(assetsDir, "Translations", "English");
      if (Directory.Exists(translationsDir))
      {
        var clientTranslationsPath = Path.Combine(pluginDeployPath, "Assets", "Translations", "English");
        CopyDirectory(translationsDir, clientTranslationsPath);
      }
    }
  }

  private static void CopyToSandboxie(string solutionDir, string targetPath, string sandboxiePluginDeployPath, string assemblyName, string assetsDir)
  {
    Console.WriteLine($"Copying files to Sandboxie at {sandboxiePluginDeployPath}...");

    if (!Directory.Exists(sandboxiePluginDeployPath))
    {
      Directory.CreateDirectory(sandboxiePluginDeployPath);
    }

    // Copy dependencies
    var dependenciesPath = Path.Combine(solutionDir, "Dependencies");
    if (Directory.Exists(dependenciesPath))
    {
      CopyDirectory(dependenciesPath, sandboxiePluginDeployPath);
    }

    // Copy main files
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.dll"), Path.Combine(sandboxiePluginDeployPath, $"{assemblyName}.dll"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.pdb"), Path.Combine(sandboxiePluginDeployPath, $"{assemblyName}.pdb"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.mdb"), Path.Combine(sandboxiePluginDeployPath, $"{assemblyName}.mdb"));

    // Copy assets if they exist
    if (!string.IsNullOrEmpty(assetsDir) && Directory.Exists(assetsDir))
    {
      var sandboxieAssetsPath = Path.Combine(sandboxiePluginDeployPath, "Assets");
      CopyDirectory(assetsDir, sandboxieAssetsPath);

      // Copy translations if they exist
      var translationsDir = Path.Combine(assetsDir, "Translations", "English");
      if (Directory.Exists(translationsDir))
      {
        var sandboxieTranslationsPath = Path.Combine(sandboxiePluginDeployPath, "Assets", "Translations", "English");
        CopyDirectory(translationsDir, sandboxieTranslationsPath);
      }
    }
  }

  private static void GenerateModArchive(string solutionDir, string targetPath, string assemblyName, string applicationVersion, bool isRelease, string pluginDeployPath, string outputDir)
  {
    var suffix = isRelease ? "" : "-beta";
    var repoDir = Path.Combine(solutionDir, "src", "ValheimRAFT");

    // Use the directory directly from MSBuild
    var outDir = outputDir;
    if (string.IsNullOrEmpty(outDir))
    {
      // Fallback to targetPath directory if outputDir is not provided
      outDir = Path.GetDirectoryName(targetPath);
      Console.WriteLine($"Warning: outputDir is not provided, using targetPath directory: {outDir}");
    }

    var modOutputDir = Path.Combine(outDir, "ModVersions");
    var tmpDir = Path.Combine(outDir, "tmp");

    // Ensure all directories exist
    Directory.CreateDirectory(outDir);
    Directory.CreateDirectory(modOutputDir);
    Directory.CreateDirectory(tmpDir);
    var thunderStoreDir = Path.Combine(repoDir, isRelease ? "ThunderStore" : "ThunderStoreBeta");

    var modNameVersion = $"{assemblyName}-{applicationVersion}{suffix}.zip";
    var autoDocName = $"{assemblyName}_AutoDoc.md";

    // Use pluginDeployPath from parameters if provided, otherwise get it from environment variables
    var effectivePluginPath = !string.IsNullOrEmpty(pluginDeployPath)
      ? pluginDeployPath
      : Environment.GetEnvironmentVariable("VALHEIM_PLUGIN_PATH") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
          "r2modmanPlus-local", "Valheim", "profiles", "Default", "BepInEx", "plugins");

    Console.WriteLine($"Using plugin path: {effectivePluginPath}");

    var autoDocPath = Path.Combine(effectivePluginPath, autoDocName);
    var localAutoDocPath = Path.Combine(solutionDir, "src", assemblyName, "docs", autoDocName);

    Console.WriteLine($"Generating mod archive: {modNameVersion}");

    // Clean up temporary directories
    if (Directory.Exists(Path.Combine(tmpDir, "plugins", "Assets")))
      Directory.Delete(Path.Combine(tmpDir, "plugins", "Assets"), true);

    if (Directory.Exists(tmpDir))
      Directory.Delete(tmpDir, true);

    // Create necessary directories
    Directory.CreateDirectory(modOutputDir);
    Directory.CreateDirectory(tmpDir);
    Directory.CreateDirectory(Path.Combine(tmpDir, "plugins"));
    Directory.CreateDirectory(Path.Combine(tmpDir, assemblyName));

    // Copy plugins
    var pluginsDir = Path.Combine(solutionDir, "plugins");
    if (Directory.Exists(pluginsDir))
      CopyDirectory(pluginsDir, Path.Combine(tmpDir, "plugins"));

    // Copy main files
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.dll"), Path.Combine(tmpDir, "plugins", $"{assemblyName}.dll"));
    CopyFile(Path.Combine(targetPath, $"{assemblyName}.pdb"), Path.Combine(tmpDir, "plugins", $"{assemblyName}.pdb"));
    CopyFile(Path.Combine(repoDir, "README.md"), Path.Combine(tmpDir, "README.md"));

    // Copy auto-doc if it exists
    if (File.Exists(autoDocPath) && File.Exists(localAutoDocPath))
    {
      CopyFile(autoDocPath, localAutoDocPath);
    }

    // Look for assets directory
    var assetsDir = Path.Combine(solutionDir, "src", assemblyName, "Assets");
    if (!Directory.Exists(assetsDir))
    {
      assetsDir = Path.Combine(solutionDir, "Assets");
    }

    // Copy assets
    if (!string.IsNullOrEmpty(assetsDir) && Directory.Exists(assetsDir))
    {
      CopyDirectory(assetsDir, Path.Combine(tmpDir, "plugins", "Assets"));

      // Delete .meta files
      foreach (var metaFile in Directory.GetFiles(Path.Combine(tmpDir, "plugins", "Assets"), "*.png.meta", SearchOption.AllDirectories))
      {
        File.Delete(metaFile);
      }
    }

    // Copy all files from output directory
    CopyDirectory(targetPath, Path.Combine(tmpDir, "plugins"));

    // Copy ThunderStore files
    if (Directory.Exists(thunderStoreDir))
      CopyDirectory(thunderStoreDir, tmpDir);

    // Delete existing archives if they exist
    var thunderstoreZip = Path.Combine(modOutputDir, $"Thunderstore-{modNameVersion}");
    var nexusZip = Path.Combine(modOutputDir, $"Nexus-{modNameVersion}");
    var libsZip = Path.Combine(modOutputDir, $"libs-{applicationVersion}.zip");

    if (File.Exists(thunderstoreZip))
      File.Delete(thunderstoreZip);

    if (File.Exists(nexusZip))
      File.Delete(nexusZip);

    if (File.Exists(libsZip))
      File.Delete(libsZip);

    // Create ThunderStore archive
    CreateZipArchive(tmpDir, thunderstoreZip);

    // Create Nexus archive
    CopyDirectory(Path.Combine(tmpDir, "plugins"), Path.Combine(tmpDir, assemblyName));
    CreateZipArchive(Path.Combine(tmpDir, assemblyName), nexusZip);

    // Create libs archive
    var libsDir = Path.Combine(solutionDir, "libs");
    if (Directory.Exists(libsDir))
      CreateZipArchive(libsDir, libsZip);
  }

  private static void CopyFile(string source, string destination)
  {
    if (File.Exists(source))
    {
      Directory.CreateDirectory(Path.GetDirectoryName(destination));
      File.Copy(source, destination, true);
      Console.WriteLine($"Copied {source} to {destination}");
    }
    else
    {
      Console.WriteLine($"Warning: Source file does not exist: {source}");
    }
  }

  private static void CopyDirectory(string sourceDir, string destinationDir)
  {
    if (!Directory.Exists(sourceDir))
    {
      Console.WriteLine($"Warning: Source directory does not exist: {sourceDir}");
      return;
    }

    // Create destination directory if it doesn't exist
    Directory.CreateDirectory(destinationDir);

    // Copy files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
      var fileName = Path.GetFileName(file);
      var destFile = Path.Combine(destinationDir, fileName);
      File.Copy(file, destFile, true);
    }

    // Copy subdirectories recursively
    foreach (var dir in Directory.GetDirectories(sourceDir))
    {
      var dirName = Path.GetFileName(dir);
      var destDir = Path.Combine(destinationDir, dirName);
      CopyDirectory(dir, destDir);
    }
  }

  private static void CreateZipArchive(string sourceDir, string destinationZipFile)
  {
    try
    {
      var psi = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "powershell",
        Arguments = $"Compress-Archive '{sourceDir}/*' '{destinationZipFile}'",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
      };

      using (var process = System.Diagnostics.Process.Start(psi))
      {
        process.WaitForExit();
        Console.WriteLine(process.StandardOutput.ReadToEnd());

        if (process.ExitCode != 0)
        {
          Console.WriteLine($"Warning: Compress-Archive exited with code {process.ExitCode}");
        }
      }

      Console.WriteLine($"Created archive: {destinationZipFile}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error creating zip archive: {ex.Message}");
    }
  }
}