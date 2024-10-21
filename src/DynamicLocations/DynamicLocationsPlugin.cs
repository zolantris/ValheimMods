using BepInEx;
using DynamicLocations.Config;
using DynamicLocations.Commands;
using DynamicLocations.Patches;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace DynamicLocations;

[BepInPlugin(BepInGuid, ModName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency(ZdoWatcher.ZdoWatcherPlugin.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod,
  VersionStrictness.Minor)]
public class DynamicLocationsPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.1.0";
  public const string ModName = "DynamicLocations";
  public const string BepInGuid = $"{Author}.{ModName}";
  public const string HarmonyGuid = $"{Author}.{ModName}";
  private static Harmony _harmony;

  public const string ModDescription =
    "Valheim Mod made to attach to an item/prefab such as a bed and place a player or object near the item wherever it is in the current game. Meant for ValheimVehicles but could be used for any movement mod. Requires Jotunn and ZdoWatcher";

  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";

  public void Awake()
  {
    DynamicLocationsConfig.BindConfig(Config);
    _harmony = new Harmony(HarmonyGuid);
    _harmony.PatchAll(typeof(DynamicLocationsPatches));
    RegisterCommands();
  }

  public void RegisterCommands()
  {
    CommandManager.Instance.AddConsoleCommand(new DynamicLocationsCommands());
  }
}