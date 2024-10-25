using BepInEx.Configuration;
using ValheimVehicles.Config;

namespace ValheimVehicles.ModSupport;

public static class QuickStartWorldConfig
{
  public static ConfigFile? Config { get; private set; }

  private const string QuickStartSection = "Quick Start (DEBUG-ONLY)";

  public static ConfigEntry<string> QuickStartWorldName { get; private set; } =
    null!;

  public static ConfigEntry<bool> QuickStartEnabled { get; private set; } =
    null!;

  public static ConfigEntry<string>
    QuickStartWorldPassword { get; private set; } =
    null!;

  public static ConfigEntry<string>
    QuickStartWorldPlayerName { get; private set; } = null!;

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    QuickStartWorldName = config.Bind(QuickStartSection, "QuickStartWorldName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start World Name",
        false, false));

    // QuickStartProfileBackend
    QuickStartWorldPassword = config.Bind(QuickStartSection,
      "QuickStartWorldPassword",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Set the quick start world password",
        false, false));

    QuickStartEnabled = config.Bind(QuickStartSection,
      "QuickStartEnabled",
      false,
      ConfigHelpers.CreateConfigDescription(
        "Enable Quick start",
        false, false));

    QuickStartWorldPlayerName = config.Bind(QuickStartSection,
      "QuickStartWorldPlayerName",
      "",
      ConfigHelpers.CreateConfigDescription(
        "Quick start player name. Must be valid to start the quick start",
        false, false));
  }
}