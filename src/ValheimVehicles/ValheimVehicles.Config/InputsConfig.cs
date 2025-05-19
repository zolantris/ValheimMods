using BepInEx.Configuration;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.UI;
using Zolantris.Shared;
namespace ValheimVehicles.Config;

public class InputsConfig : BepInExBaseConfig<InputsConfig>
{

#if DEBUG
  public static ConfigEntry<KeyboardShortcut> AnchorKeyboardShortcut = null!;
#endif
  public static ConfigEntry<bool> PanelsCanUseKeyboardOrControllerInputs = null!;

  private static void OnPanelNavigatorDisable()
  {
    SwivelUIHelpers.CanNavigatorInteractWithPanel = PanelsCanUseKeyboardOrControllerInputs.Value;
  }

  public override void OnBindConfig(ConfigFile config)
  {

    PanelsCanUseKeyboardOrControllerInputs = config.BindUnique("Input", "PanelsCanUseKeyboardOrControllerInputs", true, ConfigHelpers.CreateConfigDescription("This will allow the keyboard/controller to interact with the UI when selected. You can toggle with direction keys, press tab or up down to move to the next section and enter for button submit. Turning this off disable this feature requiring mouse to directly select.", false, false));

    SwivelUIHelpers.CanNavigatorInteractWithPanel = PanelsCanUseKeyboardOrControllerInputs.Value;

#if DEBUG
    AnchorKeyboardShortcut =
      config.BindUnique("Input", "AnchorKeyboardShortcut",
        new KeyboardShortcut(KeyCode.LeftShift),
        new ConfigDescription(
          "Anchor keyboard hotkey. Only works for DEBUG currently"));
#endif
  }
}