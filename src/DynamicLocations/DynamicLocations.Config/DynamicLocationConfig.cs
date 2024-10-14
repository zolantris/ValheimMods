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

  public static ConfigEntry<bool> FreezePlayerPosition { get; private set; } =
    null!;


  public static ConfigEntry<bool> Debug { get; private set; } = null!;
  public static bool IsDebug => Debug.Value;

  public static ConfigEntry<bool> DebugDistancePortal { get; private set; } =
    null!;

  public static ConfigEntry<float> DebugForceUpdatePositionDelay
  {
    get;
    private set;
  } =
    null!;

  public static ConfigEntry<bool> DebugForceUpdatePositionAfterTeleport
  {
    get;
    private set;
  } =
    null!;

  private const string MainSection = "Main";
  private const string DebugSection = "Debug";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    FreezePlayerPosition = config.Bind(MainSection,
      "FreezePlayerPosition",
      false,
      new ConfigDescription(
        $"Freezes the player position until the teleport and vehicle is fully loaded, prevents falling through",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = false, IsAdvanced = false }));

    DebugDistancePortal = config.Bind(DebugSection,
      "DebugDistancePortal",
      false,
      new ConfigDescription(
        $"distance portal enabled, disabling this could break portals",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));


    DebugForceUpdatePositionDelay = config.Bind(DebugSection,
      "DebugForceUpdatePositionDelay",
      0f,
      new ConfigDescription(
        $"distance portal enabled, disabling this could break portals",
        new AcceptableValueRange<float>(0, 5f),
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));


    DebugForceUpdatePositionAfterTeleport = config.Bind(DebugSection,
      "DebugForceUpdatePositionAfterTeleport",
      false,
      new ConfigDescription(
        $"distance portal enabled, disabling this could break portals",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    EnableDynamicSpawnPoint = config.Bind(MainSection,
      "enableDynamicSpawnPoints",
      true,
      new ConfigDescription(
        $"Enable dynamic spawn points. This will allow the user to re-spawn in a new area of the map if a vehicle has moved.",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    EnableDynamicLogoutPoint = config.Bind(MainSection,
      "enableDynamicLogoutPoints",
      true,
      new ConfigDescription(
        $"Enable dynamic logout points. This will allow the user to login to a new area of the map if a vehicle has moved",
        null,
        new ConfigurationManagerAttributes()
          { IsAdminOnly = true, IsAdvanced = true }));

    RespawnHeightOffset = config.Bind(MainSection,
      "respawnHeightOffset",
      0,
      new ConfigDescription(
        $"Sets the respawn height for beds. Useful if the player is spawning within the bed instead of above it",
        new AcceptableValueRange<int>(-5, 10),
        new ConfigurationManagerAttributes()
          { IsAdminOnly = false, IsAdvanced = true }));

    Debug = config.Bind(MainSection,
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