using BepInEx.Configuration;
using UnityEngine;
namespace ValheimVehicles.Config;

public class InputsConfig
{
  private static ConfigFile Config = null!;

#if DEBUG
  public static ConfigEntry<KeyboardShortcut> AnchorKeyboardShortcut = null!;
#endif

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

#if DEBUG
    AnchorKeyboardShortcut =
      Config.Bind("Config", "AnchorKeyboardShortcut",
        new KeyboardShortcut(KeyCode.LeftShift),
        new ConfigDescription(
          "Anchor keyboard hotkey. Only works for DEBUG currently"));
#endif
  }
}