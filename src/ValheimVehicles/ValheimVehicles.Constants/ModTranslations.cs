using System;
using ValheimRAFT;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Constants;

public class ModTranslations
{
  public static string GuiShow = "";
  public static string GuiHide = "";
  public static string GuiCommandsMenuTitle = "";

  public static string EditMenu = "";
  public static string CreativeMode = "";
  public static string EditMode = "";

  public static string ToggleSwitch_CommandsHudText = "";
  public static string ToggleSwitch_MaskColliderEditMode = "";
  public static string ToggleSwitch_NextActionString = "";
  public static string ToggleSwitch_CurrentActionString = "";
  public static string ToggleSwitch_SwitchName = "";

  /// <summary>
  /// Check a couple keys and ensure this object is healthy
  ///
  /// todo use the object validator that runs on all keys
  /// </summary>
  /// <returns></returns>
  public static bool IsHealthy()
  {
    if (GuiHide == string.Empty || GuiShow == string.Empty) return false;
    return true;
  }
  
  /// <summary>
  /// Possibly move to a localization generator to generate these on the fly based on the current english translations.
  /// </summary>
  public static void UpdateTranslations()
  {
    if (Localization.instance == null) return;
    try
    {
      ToggleSwitch_CurrentActionString = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_Use</b></color>] To Toggle:");
      ToggleSwitch_NextActionString = Localization.instance.Localize(
        "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] To Switch To:");

      ToggleSwitch_MaskColliderEditMode = Localization.instance.Localize(
        "$valheim_vehicles_commands_mask_edit_mode");
      ToggleSwitch_CommandsHudText = Localization.instance.Localize(
        "$valheim_vehicles_commands_edit_menu");
      ToggleSwitch_SwitchName = Localization.instance.Localize("$valheim_vehicles_toggle_switch");

      EditMenu = Localization.instance.Localize("$valheim_vehicles_commands_edit_menu");
      CreativeMode = Localization.instance.Localize("$valheim_vehicles_commands_creative_mode");
      EditMode = Localization.instance.Localize("$valheim_vehicles_commands_mask_edit_mode");
      GuiShow = Localization.instance.Localize("$valheim_vehicles_gui_show");
      GuiHide = Localization.instance.Localize("$valheim_vehicles_gui_hide");
      GuiCommandsMenuTitle = Localization.instance.Localize("$valheim_vehicles_gui_commands_menu_title");
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning($"error while registering ModTranslations. This could break some item interactivity \n{e}");
    }
  }
}