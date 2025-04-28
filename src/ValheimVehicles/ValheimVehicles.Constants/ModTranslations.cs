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
  /// Possibly move to a localization generator to generate these on the fly based on the current english translations.
  /// </summary>
  public static void UpdateTranslations()
  {
    if (Localization.instance == null || string.IsNullOrEmpty(Localization.instance.GetSelectedLanguage())) return;
    try
    {
      WheelControls_Name = Localization.instance.Localize("$valheim_vehicles_wheel");
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

      VehicleConfig_CustomFloatationHeight = Localization.instance.Localize("$valheim_vehicles_custom_floatation_height");

      // basic states used to combine with other states.
      DisabledText = Localization.instance.Localize("$valheim_vehicles_gui_disabled");
      EnabledText = Localization.instance.Localize("$valheim_vehicles_gui_enabled");
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning($"Problem while registering ModTranslations this call was likely too early.  \n{e}");
    }
  }
}