using BepInEx.Configuration;

namespace ZdoWatcher.ZdoWatcher.Config;

public static class ZdoWatcherConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool>? GuardAgainstInvalidZNetSceneSpam { get; private set; }

  public static void BindConfig(ConfigFile config)
  {
    Config = config;
    GuardAgainstInvalidZNetSceneSpam = config.Bind("Patches", "GuardAgainstInvalidZNetScene", true,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.",
        true, true));
  }
}