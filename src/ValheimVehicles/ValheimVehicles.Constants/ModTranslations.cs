using System;

using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Validation;
namespace ValheimVehicles.Constants;

public class ModTranslations
{
  public static string GuiShow = null!;
  public static string GuiHide = null!;
  public static string GuiCommandsMenuTitle = null!;

  public static string EditMenu = null!;
  public static string CreativeMode = null!;
  public static string EditMode = null!;

  public static string EnabledText = null!;
  public static string DisabledText = null!;
  

  public static string ToggleSwitch_CommandsHudText = null!;
  public static string ToggleSwitch_MaskColliderEditMode = null!;
  public static string ToggleSwitch_NextActionString = null!;
  public static string ToggleSwitch_CurrentActionString = null!;
  public static string ToggleSwitch_SwitchName = null!;

  public static string VehicleConfig_CustomFloatationHeight = null!;

  public static string WheelControls_Name = null!;

  /// <summary>
  /// Looks for null values. If any string is null, it will return that it's not healthy.
  /// </summary>
  /// <returns></returns>
  public static bool IsHealthy()
  {
    return StaticFieldValidator.ValidateRequiredNonNullFields<ModTranslations>(null, null, false);
  }

  /// <summary>
  /// We load localize too soon sometimes. Guard it.
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  private static string SafeLocalize(string key)
  {
    try
    {
      if (Localization.instance == null) return null; // fallback
      return Localization.instance.Localize(key) ?? null;
    }
    catch (Exception e)
    {
#if DEBUG
      LoggerProvider.LogWarning($"Failed to localize key {key}.\n{e}");
#endif
      return null; // fallback gracefully
    }
  }
  
  
  
  /// <summary>
  /// Possibly move to a localization generator to generate these on the fly based on the current english translations.
  /// </summary>
  public static void UpdateTranslations()
  {
    if (Localization.instance == null || ZInput.instance == null || string.IsNullOrEmpty(Localization.instance.GetSelectedLanguage())) return;
    try
    {
      WheelControls_Name = SafeLocalize("$valheim_vehicles_wheel");
      ToggleSwitch_CurrentActionString = SafeLocalize(
        "[<color=yellow><b>$KEY_Use</b></color>] To Toggle:");
      ToggleSwitch_NextActionString = SafeLocalize(
        "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] To Switch To:");

      ToggleSwitch_MaskColliderEditMode = SafeLocalize(
        "$valheim_vehicles_commands_mask_edit_mode");
      ToggleSwitch_CommandsHudText = SafeLocalize(
        "$valheim_vehicles_commands_edit_menu");
      ToggleSwitch_SwitchName = SafeLocalize("$valheim_vehicles_toggle_switch");

      EditMenu = SafeLocalize("$valheim_vehicles_commands_edit_menu");
      CreativeMode = SafeLocalize("$valheim_vehicles_commands_creative_mode");
      EditMode = SafeLocalize("$valheim_vehicles_commands_mask_edit_mode");
      GuiShow = SafeLocalize("$valheim_vehicles_gui_show");
      GuiHide = SafeLocalize("$valheim_vehicles_gui_hide");
      GuiCommandsMenuTitle = SafeLocalize("$valheim_vehicles_gui_commands_menu_title");

      VehicleConfig_CustomFloatationHeight = SafeLocalize("$valheim_vehicles_custom_floatation_height");

      // basic states used to combine with other states.
      DisabledText = SafeLocalize("$valheim_vehicles_gui_disabled");
      EnabledText = SafeLocalize("$valheim_vehicles_gui_enabled");
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning($"Problem while registering ModTranslations this call was likely too early.  \n{e}");
    }
  }
}