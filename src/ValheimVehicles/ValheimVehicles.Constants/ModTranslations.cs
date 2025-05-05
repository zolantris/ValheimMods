#region

  using System;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Validation;

#endregion

  namespace ValheimVehicles.Constants;

  /// <summary>
  /// Localization/Translation controller for the entire valheim-vehicles mod.
  ///
  /// - All static translations should be added here. There should be no dynamic values added here that call Localization.
  /// </summary>
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

    public static string ValheimInput_KeyUse = null!;


    public static string ToggleSwitch_CommandsHudText = null!;
    public static string ToggleSwitch_MaskColliderEditMode = null!;
    public static string ToggleSwitch_NextActionString = null!;
    public static string ToggleSwitch_CurrentActionString = null!;
    public static string ToggleSwitch_SwitchName = null!;

    public static string VehicleConfig_CustomFloatationHeight = null!;

    public static string WheelControls_Name = null!;
    public static string WheelControls_Error = null!;

    // anchor
    public static string Anchor_WheelUse_EnableAnchor = null!;
    public static string Anchor_WheelUse_DisableAnchor = null!;
    public static string Anchor_WheelUse_UseText = null!;

    public static string AnchorPrefab_RecoveredAnchorText = null!;
    public static string AnchorPrefab_reelingText = null!;
    public static string AnchorPrefab_anchoredText = null!;
    public static string AnchorPrefab_loweringText = null!;
    public static string AnchorPrefab_breakingText = null!;
    public static string AnchorPrefab_idleText = null!;


    public static string Swivel_Edit = null!;
    public static string Swivel_Connected = null!;

    // full text string. This is computed from a few values.
    public static string Swivel_HoverText = null!;


    // generic/shared-keys
    public static string SharedKeys_Owner = null!;
    public static string SharedKeys_Hold = null!;

    // vehicle config
    public static string VehicleConfig_Beached = null!;

    public static string CurrentLocalizeLanguage = "";

    public static bool CanRunLocalization()
    {
      if (Localization.instance == null || ZInput.instance == null || string.IsNullOrEmpty(Localization.instance.GetSelectedLanguage())) return false;
      return true;
    }

    public static bool HasRunLocalizationOnCurrentLanguage()
    {
      // must call this otherwise other translation APIs could throw errors.
      if (!CanRunLocalization()) return false;
      if (CurrentLocalizeLanguage == "" || CurrentLocalizeLanguage != Localization.instance.GetSelectedLanguage())
      {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Looks for null values. If any string is null, it will return that it's not healthy.
    /// </summary>
    /// <returns></returns>
    public static bool IsHealthy()
    {
      try
      {
        if (!HasRunLocalizationOnCurrentLanguage()) return false;
        return ClassValidator.ValidateRequiredNonNullFields<ModTranslations>(null, null, null, false);
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"Problem while validating ModTranslations. \n{e}");
        return false;
      }
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
        return Localization.instance.Localize(key);
      }
      catch (Exception e)
      {
#if DEBUG
        LoggerProvider.LogWarning($"Failed to localize key {key}.\n{e}");
#endif
        return null; // fallback gracefully
      }
    }

    private static void UpdateAnchorTranslations()
    {
      AnchorPrefab_breakingText = SafeLocalize("$valheim_vehicles_land_state_breaking");

      AnchorPrefab_idleText = SafeLocalize("$valheim_vehicles_land_state_idle");

      AnchorPrefab_reelingText =
        SafeLocalize("$valheim_vehicles_anchor_state_reeling");

      AnchorPrefab_RecoveredAnchorText =
        SafeLocalize(
          "$valheim_vehicles_anchor_state_recovered");

      AnchorPrefab_anchoredText =
        SafeLocalize("$valheim_vehicles_anchor_state_anchored");

      AnchorPrefab_loweringText =
        SafeLocalize("$valheim_vehicles_anchor_state_lowering");

      Anchor_WheelUse_EnableAnchor = SafeLocalize("$valheim_vehicles_wheel_use_anchor_enable_detail");

      Anchor_WheelUse_DisableAnchor = SafeLocalize("$valheim_vehicles_wheel_use_anchor_disable_detail");


      Anchor_WheelUse_UseText = SafeLocalize("$valheim_vehicles_wheel_use");
    }

    private static void UpdateToggleSwitchTranslations()
    {
      ToggleSwitch_CurrentActionString = SafeLocalize(
        "[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_activate");
      ToggleSwitch_NextActionString = SafeLocalize(
        "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $valheim_vehicles_toggle");

      ToggleSwitch_MaskColliderEditMode = SafeLocalize(
        "$valheim_vehicles_commands_mask_edit_mode");
      ToggleSwitch_CommandsHudText = SafeLocalize(
        "$valheim_vehicles_commands_edit_menu");
      ToggleSwitch_SwitchName = SafeLocalize("$valheim_vehicles_toggle_switch");
    }

    private static void UpdateGuiEditMenuTranslations()
    {
      EditMenu = SafeLocalize("$valheim_vehicles_commands_edit_menu");
      CreativeMode = SafeLocalize("$valheim_vehicles_commands_creative_mode");
      EditMode = SafeLocalize("$valheim_vehicles_commands_mask_edit_mode");
      GuiShow = SafeLocalize("$valheim_vehicles_gui_show");
      GuiHide = SafeLocalize("$valheim_vehicles_gui_hide");
      GuiCommandsMenuTitle = SafeLocalize("$valheim_vehicles_gui_commands_menu_title");
      // basic states used to combine with other states.
      DisabledText = SafeLocalize("$valheim_vehicles_gui_disabled");
      EnabledText = SafeLocalize("$valheim_vehicles_gui_enabled");
    }

    private static void UpdateVehicleConfigTranslations()
    {
      VehicleConfig_CustomFloatationHeight = SafeLocalize("$valheim_vehicles_custom_floatation_height");
      VehicleConfig_Beached = SafeLocalize("$valheim_vehicles_gui_vehicle_is_beached");
    }

    private static void UpdateVehicleWheelTranslations()
    {
      WheelControls_Name = SafeLocalize("$valheim_vehicles_wheel");
      WheelControls_Error = SafeLocalize("<color=white><b>$valheim_vehicles_wheel_use_error</b></color>");
    }

    public static string WithBoldText(string text, string color = "white")
    {
      return $"<color={color}><b>{text}</b></color>";
    }

    public static void UpdateSharedTranslations()
    {
      SharedKeys_Owner = SafeLocalize("$valheim_vehicles_shared_keys_owner");
      SharedKeys_Hold = SafeLocalize("$valheim_vehicles_shared_keys_hold");
    }

    private static void SetCurrentLocalizedLanguage()
    {
      CurrentLocalizeLanguage = Localization.instance.GetSelectedLanguage();
    }

    public static bool CanLocalizeCurrentLanguage(bool forceUpdate = false)
    {
      if (!CanRunLocalization()) return false;
      try
      {
        if (forceUpdate || !HasRunLocalizationOnCurrentLanguage())
        {
          SetCurrentLocalizedLanguage();
          return true;
        }

        // Can exit, already ran this update.
        return false;
      }
      catch (Exception e)
      {
        return false;
      }
    }

    public static void UpdateValheimInputTranslations()
    {
      ValheimInput_KeyUse = SafeLocalize("$KEY_Use");
    }

    public static void UpdateSwivelTranslations()
    {
      Swivel_Edit = SafeLocalize("$valheim_vehicles_mechanism_swivel_edit");
      Swivel_Connected = SafeLocalize("$valheim_vehicles_mechanism_swivel_connected");
      Swivel_HoverText = $"{WithBoldText(SharedKeys_Hold)} {WithBoldText(ValheimInput_KeyUse, "yellow")} {Swivel_Edit}";
    }

    public static void ForceUpdateTranslations()
    {
      UpdateTranslations(true);
    }

    /// <summary>
    /// Possibly move to a localization generator to generate these on the fly based on the current english translations.
    /// </summary>
    public static void UpdateTranslations(bool forceUpdate = false)
    {
      if (!CanLocalizeCurrentLanguage(forceUpdate)) return;
      try
      {
        // only updates here.
        UpdateSharedTranslations();
        UpdateVehicleWheelTranslations();
        UpdateGuiEditMenuTranslations();
        UpdateVehicleConfigTranslations();
        UpdateToggleSwitchTranslations();
        UpdateValheimInputTranslations();
        UpdateAnchorTranslations();
        UpdateSwivelTranslations();
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"Problem while registering ModTranslations this call was likely too early.  \n{e}");
      }
    }
  }