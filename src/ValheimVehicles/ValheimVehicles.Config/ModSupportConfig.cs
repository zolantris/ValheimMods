using BepInEx.Configuration;

namespace ValheimVehicles.Config;

public class ModSupportConfig
{
  public static ConfigEntry<bool> DynamicLocationsShouldSkipMovingPlayerToBed =
    null!;
  public static ConfigEntry<bool> DebugRemoveStartMenuBackground { get; set; }

  public static ConfigEntry<string> PluginFolderName { get; set; }
  private static ConfigFile Config { get; set; } = null!;

  private const string ModSupportGenericKey = "ModSupport:Generic";
  private const string ModSupportAssetsKey = "ModSupport:Assets";
  private const string ModSupportDynamicLocationsKey = "ModSupport:DynamicLocations";
  private const string ModSupportDebugOptimizationsKey = "ModSupport:DebugOptimizations";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    PluginFolderName = Config.Bind<string>(ModSupportAssetsKey,
      "pluginFolderName", "", ConfigHelpers.CreateConfigDescription(
        "Users can leave this empty. If they do not, the mod will attempt to match the folder string. Allows users to set the folder search name if their" +
        $" manager renames the folder, r2modman has a fallback case added to search for the mod folder." +
        "Default search values are an ordered list first one is always matching non-empty strings from this pluginFolderName." +
        $"Folder Matches are: zolantris-ValheimRAFT, Zolantris-ValheimRAFT, and ValheimRAFT",
        false, false));
    
    
    DynamicLocationsShouldSkipMovingPlayerToBed = Config.Bind(
      ModSupportDynamicLocationsKey,
      "DynamicLocationLoginMovesPlayerToBed",
      true,
      ConfigHelpers.CreateConfigDescription(
        "login/logoff point moves player to last interacted bed or first bed on ship",
        true));

    DebugRemoveStartMenuBackground =
      Config.Bind(ModSupportDebugOptimizationsKey, "RemoveStartMenuBackground", false,
        ConfigHelpers.CreateConfigDescription(
          "Removes the start scene background, only use this if you want to speedup start time and lower GPU power cost significantly if you are idle on the start menu.",
          false, true));
  }
}
