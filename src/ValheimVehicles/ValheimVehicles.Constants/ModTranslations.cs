namespace ValheimVehicles.Constants;

public static class ModTranslations
{
  public static string GuiShow = "";
  public static string GuiHide = "";
  public static string GuiCommandsMenuTitle = "";

  public static string EditMenu = "";
  public static string CreativeMode = "";
  public static string EditMode = "";

  /// <summary>
  /// Possibly move to a localization generator to generate these on the fly based on the current english translations.
  /// </summary>
  public static void UpdateTranslations()
  {
    if (Localization.instance == null) return;
    EditMenu = Localization.instance.Localize("$valheim_vehicles_commands_edit_menu");
    CreativeMode = Localization.instance.Localize("$valheim_vehicles_commands_creative_mode");
    EditMode = Localization.instance.Localize("$valheim_vehicles_commands_mask_edit_mode");
    GuiShow = Localization.instance.Localize("$valheim_vehicles_gui_show");
    GuiHide = Localization.instance.Localize("$valheim_vehicles_gui_hide");
    GuiCommandsMenuTitle = Localization.instance.Localize("$valheim_vehicles_gui_commands_menu_title");
  }
}