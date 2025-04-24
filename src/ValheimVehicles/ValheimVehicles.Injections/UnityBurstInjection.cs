using System.Runtime.InteropServices;
namespace ValheimVehicles.Injections;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// The BurstInjector is experimental and the code is only compiled in DEBUG variants.
///
/// This class is supposed to be able to inject the full Unity Burst DLL into the Valheim or other mod game. However, the burst dll file is not easy to find or is named something different.
/// </summary>
/// 
/// todo find out what UnityBurst.dll is needed to allow injection of burst within games that could support it but do not include it.
public class BurstInjector
{
#if DEBUG
    // public static void LoadBurst()
    // {
    //     var burstDllPath = "";
    //     foreach (var possibleModFolderName in ValheimRaftPlugin.Instance
    //                  .possibleModFolderNames)
    //     {
    //         if (!Directory.Exists(Path.Combine(Paths.PluginPath,
    //                 possibleModFolderName))) continue;
    //         var managedPath = Path.Combine(Paths.PluginPath,
    //             possibleModFolderName, "Managed");
    //         burstDllPath = Path.Combine(managedPath, "burst-llvm-18.dll");
    //         break;
    //     }
    //
    //     if (File.Exists(burstDllPath))
    //     {
    //         try
    //         {
    //             var assembly = Assembly.LoadFile(burstDllPath);
    //             Debug.Log($"✅ Loaded Unity Burst: {assembly.FullName}");
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"❌ Failed to load Burst: {e.Message}");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogWarning("⚠ Unity.Burst.dll not found in Managed folder.");
    //     }
    // }
    private static bool _isBurstLoaded = false;

    // 🔹 Change this to your actual Burst runtime path
    private static readonly string BurstRuntimePath = @"C:\Users\fre\dev\repos\ValheimMods\src\ValheimRAFT.Unity\Library\PackageCache\com.unity.burst@1.8.19\.Runtime";

    public static void LoadBurst()
    {
        if (_isBurstLoaded)
        {
            Debug.Log("✅ Unity Burst is already loaded.");
            return;
        }

        try
        {
            // Load the main Burst assembly
            var burstDll = Path.Combine(BurstRuntimePath, "Unity.Burst.dll");
            if (File.Exists(burstDll))
            {
                Assembly.LoadFile(burstDll);
                Debug.Log($"✅ Loaded: {burstDll}");
            }
            else
            {
                Debug.LogError($"❌ Missing: {burstDll}");
            }

            // Load the Burst backend (JIT/AOT compiler)
            var burstBinary = GetBurstBinaryPath();
            if (File.Exists(burstBinary))
            {
                LoadLibraryWindows(burstBinary);
                Debug.Log($"✅ Loaded: {burstBinary}");
            }
            else
            {
                Debug.LogError($"❌ Missing: {burstBinary}");
            }

            _isBurstLoaded = true;
            Debug.Log("🚀 Unity Burst successfully loaded!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ BurstLoader encountered an error: {e.Message}");
        }
    }

    /// <summary>
    /// Gets the correct Burst binary depending on OS
    /// </summary>
    private static string GetBurstBinaryPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Path.Combine(BurstRuntimePath, "burst-llvm.dll");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Path.Combine(BurstRuntimePath, "libburst-llvm.so");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Path.Combine(BurstRuntimePath, "libburst-llvm.dylib");

        throw new PlatformNotSupportedException("Burst is not supported on this OS.");
    }

    /// <summary>
    /// Loads a native binary into the process (Windows/Linux/macOS)
    /// </summary>
    private static void LoadNativeLibrary(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LoadLibraryWindows(path);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            LoadLibraryUnix(path);
        else
            throw new PlatformNotSupportedException("Unsupported OS.");
    }

// Windows DLL Import (Rename this to avoid conflicts)
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string lpFileName); // Renamed

    private static void LoadLibraryWindows(string path)
    {
        var handle = LoadLibraryW(path); // Use renamed function
        if (handle == IntPtr.Zero)
            throw new Exception($"Failed to load library: {path}");
    }


    // Unix (Linux/macOS) DllImport
    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr dlopen(string filename, int flags);

    private static void LoadLibraryUnix(string path)
    {
        var handle = dlopen(path, 1); // 1 = RTLD_LAZY
        if (handle == IntPtr.Zero)
            throw new Exception($"Failed to load library: {path}");
    }

#endif
}