using BepInEx.Configuration;
using UnityEngine;
namespace ValheimVehicles.Config;

public class InputsConfig : BepInExBaseConfig<InputsConfig>
{

#if DEBUG
  public static ConfigEntry<KeyboardShortcut> AnchorKeyboardShortcut = null!;
#endif

  public override void OnBindConfig(ConfigFile config)
  {

#if DEBUG
    AnchorKeyboardShortcut =
      config.Bind("Input", "AnchorKeyboardShortcut",
        new KeyboardShortcut(KeyCode.LeftShift),
        new ConfigDescription(
          "Anchor keyboard hotkey. Only works for DEBUG currently"));
#endif
  }
}