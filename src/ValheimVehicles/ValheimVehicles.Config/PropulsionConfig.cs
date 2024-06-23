using System;
using BepInEx.Configuration;
using ComfyLib;
using ValheimRAFT;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Config;

public static class PropulsionConfig
{
  public static ConfigFile? Config { get; private set; }


  /// <summary>
  /// Todo migrate ValheimRaftPlugin.CreatePropulsionConfig to here
  /// </summary>
  /// <param name="config"></param>
  public static void BindConfig(ConfigFile config)
  {
    Config = config;
  }
}