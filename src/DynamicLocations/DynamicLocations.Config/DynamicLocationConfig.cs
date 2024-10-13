using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace DynamicLocations.Config;

public static class DynamicLocationsConfig
{
  public static ConfigFile? Config { get; private set; }

  // TODO add a list to be searched for an upcon interacting with the item add a popup to allow binding a spawn to the item
  // public static ConfigEntry<bool>
  //   DynamicSpawnPointGameObjects { get; private set; } = null!;

  public static ConfigEntry<int> RespawnHeightOffset { get; set; } = null!;

  public static ConfigEntry<bool>
    EnableDynamicSpawnPoint { get; private set; } = null!;

  public static ConfigEntry<bool>
    EnableDynamicLogoutPoint { get; private set; } = null!;

  public static ConfigEntry<bool> Debug { get; private set; } = null!;

  private const string ConfigSection = "Main";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    EnableDynamicSpawnPoint = config.Bind(ConfigSection,
      "enableDynamicSpawnPoints",
      true,
      new ConfigDescription(
        $"Enable dynamic spawn points. This will allow the user to re-spawn in a new area of the map if a vehicle has moved.",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    EnableDynamicLogoutPoint = config.Bind(ConfigSection,
      "enableDynamicLogoutPoints",
      true,
      new ConfigDescription(
        $"Enable dynamic logout points. This will allow the user to login to a new area of the map if a vehicle has moved",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    RespawnHeightOffset = config.Bind(ConfigSection,
      "respawnHeightOffset",
      0,
      new ConfigDescription(
        $"Sets the respawn height for beds. Useful if the player is spawning within the bed instead of above it",
        new AcceptableValueRange<int>(-5, 10),
        new ConfigurationManagerAttributes()
          { IsAdminOnly = false, IsAdvanced = true }));

    Debug = config.Bind(ConfigSection,
      "debug",
      false,
      new ConfigDescription(
        $"Enable additional logging and debug drawing around spawn and logout points. Useful for debugging this mod",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    // Debug.SettingChanged += 
    //   .OnBranchCollisionChange;

    if (Debug.Value)
    {
    }
  }
}