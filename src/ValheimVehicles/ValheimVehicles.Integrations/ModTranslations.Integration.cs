using System;
using ValheimVehicles.SharedScripts.Validation;
namespace ValheimVehicles.SharedScripts;

/// <summary>
/// src/ValheimRAFT.Unity/Assets/ValheimVehicles/SharedScripts/ModTranslations.Shared.cs
/// </summary>
public partial class ModTranslations
{
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

  private static void UpdateMechanismSwitchTranslations()
  {
    MechanismSwitch_MaskColliderEditMode = SafeLocalize(
      "$valheim_vehicles_commands_mask_edit_mode");
    MechanismSwitch_CommandsHudText = SafeLocalize(
      "$valheim_vehicles_commands_edit_menu");
    MechanismSwitch_SwitchName = SafeLocalize("$valheim_vehicles_toggle_switch");
    MechanismMode_None = SafeLocalize("$valheim_vehicles_mechanism_mode_none");
    MechanismSwitch_CurrentActionString = SafeLocalize(
      "[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_activate");
    MechanismSwitch_AltActionString = SafeLocalize($"[<color=yellow><b>{ValheimInput_KeyAltPlace}+{ValheimInput_KeyUse}</b></color>] $valheim_vehicles_mechanism_mode_configure");

    MechanismSwitch_AltHoldActionString = SafeLocalize($"[<color=yellow><b>{SharedKeys_Hold}+{ValheimInput_KeyAltPlace}+{ValheimInput_KeyUse}</b></color>] $valheim_vehicles_mechanism_swivel_edit");

    MechanismMode_VehicleDock = SafeLocalize("$valheim_vehicles_mechanism_mode_vehicle_dock");
    MechanismMode_VehicleDock = SafeLocalize("$valheim_vehicles_mechanism_mode_fire_cannon_group");

    DockingMessages_NotAttachedToVehicle = SafeLocalize("$valheim_vehicles_vehicle_dock_messages_not_attached_to_vehicle");
    DockingMessages_NoVehicleToDockFound = SafeLocalize("$valheim_vehicles_vehicle_dock_messages_no_dockable_vehicle_found");
    DockingMessages_Undocked = SafeLocalize("$valheim_vehicles_vehicle_dock_messages_undock_success");
    DockingMessages_Docked = SafeLocalize("$valheim_vehicles_vehicle_dock_messages_dock_success");

    // debug exposed for now.
    MechanismMode_VehicleConfig = SafeLocalize($"Vehicle Config");
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

    SailVariant = SafeLocalize("$valheim_vehicles_sail_variant");

    CustomSail = SafeLocalize("$valheim_vehicles_sail_custom");
    RaftSail = SafeLocalize("$valheim_vehicles_sail_raft");
    KarveSail = SafeLocalize("$valheim_vehicles_sail_karve");
    DrakkalSail = SafeLocalize("$valheim_vehicles_sail_drakkal");
  }

  private static void UpdateVehicleCannonTranslations()
  {
    VehicleCannon_FireCannons = SafeLocalize("$valheim_vehicles_cannons_fire");
    VehicleCannon_FireCannonGroup = SafeLocalize("$valheim_vehicles_cannons_fire_group");
    VehicleCannon_ToggleAutoFire = SafeLocalize("$valheim_vehicles_cannons_toggle_auto_fire");
    VehicleCannon_SwapCannonBallType = SafeLocalize("$valheim_vehicles_cannons_swap_cannonball_type");
    VehicleCannon_AmmoText = SafeLocalize("$valheim_vehicles_cannons_ammo_text");

    VehicleCannon_CannonBallItemSolid = SafeLocalize(PrefabItemNameToken.CannonSolidAmmo);
    VehicleCannon_CannonBallItemExplosive = SafeLocalize(PrefabItemNameToken.CannonExplosiveAmmo);

    VehicleCannon_CannonMissingNearbyPowderBarrel = SafeLocalize(WithBoldText("$valheim_vehicles_cannons_missing_nearby_powder_barrels", "red"));
    Vehicle_Cannon_Controls_Tutorial = SafeLocalize("$valheim_vehicles_cannon_control_center_tutorial");

    CannonGroup_Forward = SafeLocalize("$valheim_vehicles_cannon_control_center_select_group_forward");
    CannonGroup_Backward = SafeLocalize("$valheim_vehicles_cannon_control_center_select_group_backward");
    CannonGroup_Left = SafeLocalize("$valheim_vehicles_cannon_control_center_select_group_left");
    CannonGroup_Right = SafeLocalize("$valheim_vehicles_cannon_control_center_select_group_right");

    Cannon_TutorialShort = SafeLocalize(WithBoldText("$valheim_vehicles_cannon_controls_tutorial_short"));
  }

  private static void UpdateVehicleConfigTranslations()
  {
    VehicleConfig_CustomFloatationHeight = SafeLocalize("$valheim_vehicles_custom_floatation_height");
    VehicleConfig_Beached = SafeLocalize("$valheim_vehicles_gui_vehicle_is_beached");

    // treads
    VehicleConfig_TreadsDistance = SafeLocalize("$valheim_vehicles_config_tread_distance");
    VehicleConfig_TreadsScale = SafeLocalize("$valheim_vehicles_config_treads_scale");
    VehicleConfig_TreadsMaxLength = SafeLocalize("$valheim_vehicles_treads_max_length");


    // section titles
    VehicleConfig_LandVehicle_Section = SafeLocalize("$valheim_vehicles_land_vehicle");

    // water vehicle
    VehicleConfig_WaterVehicle_Section = SafeLocalize("$valheim_vehicles_water_vehicle");
  }

  private static void UpdateVehicleWheelTranslations()
  {
    WheelControls_Name = SafeLocalize("$valheim_vehicles_wheel");
    WheelControls_Error = SafeLocalize("<color=white><b>$valheim_vehicles_wheel_use_error</b></color>");
    WheelControls_FlightActivation = SafeLocalize($"{SharedKeys_Hold} {WithBoldText("Jump", "yellow")} $valheim_vehicles_wheel_flight_controls");
    WheelControls_BallastActivation = SafeLocalize($"{SharedKeys_Hold} [{WithBoldText("Jump", "yellow")}] [{WithBoldText("Crouch", "yellow")}] $valheim_vehicles_wheel_ballast_controls");
    WheelControls_TutorialFlight = SafeLocalize("$valheim_vehicles_wheel_tutorial_flight");
  }

  public static void UpdateSharedTranslations()
  {
    SharedKeys_Owner = SafeLocalize("$valheim_vehicles_shared_keys_owner");
    SharedKeys_Hold = SafeLocalize("$valheim_vehicles_shared_keys_hold");
    SharedKeys_Reset = SafeLocalize("$valheim_vehicles_shared_keys_reset");
    SharedKeys_AddMany = SafeLocalize("$valheim_vehicles_mechanism_interact_add_many");

    SharedKeys_InteractPrimary = SafeLocalize($"[<color=yellow><b>{ValheimInput_KeyUse}</b></color>]");
    SharedKeys_InteractAlt = SafeLocalize($"[<color=yellow><b>{ValheimInput_KeyAltPlace} + {ValheimInput_KeyUse}</b></color>]");
    SharedKeys_Tutorial = SafeLocalize("$valheim_vehicles_shared_keys_tutorial");
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
    ValheimInput_KeyAltPlace = SafeLocalize("$KEY_AltPlace");
  }

  public static void UpdateSwivelTranslations()
  {
    MechanismMode_Swivel_Edit = SafeLocalize("$valheim_vehicles_mechanism_swivel_edit");
    Swivel_Name = SafeLocalize("$valheim_vehicles_mechanism_swivel");
    Swivel_Connected = SafeLocalize("$valheim_vehicles_mechanism_swivel_connected");
    // Swivel_HoverText = $"{WithBoldText(SharedKeys_Hold)} {WithBoldText(ValheimInput_KeyUse, "yellow")} {Swivel_Edit}";
    Swivel_HoverText = $"{WithBoldText(SharedKeys_Hold)} {WithBoldText(ValheimInput_KeyUse, "yellow")} {Swivel_Name}";
  }

  public static void UpdatePowerTranslations()
  {
    // For hud
    PowerPylon_Name = SafeLocalize("$valheim_vehicles_mechanism_power_pylon");
    PowerConduit_DrainPlate_Name = SafeLocalize("$valheim_vehicles_mechanism_power_drain_plate");
    PowerConduit_ChargePlate_Name = SafeLocalize("$valheim_vehicles_mechanism_power_charge_plate");

    PowerState_HasPower = SafeLocalize("$valheim_vehicles_mechanism_power_state_has_power");
    PowerState_NoPower = SafeLocalize("$valheim_vehicles_mechanism_power_state_no_power");
    PowerState_ConnectToGrid = SafeLocalize("$valheim_vehicles_mechanism_power_state_connect_to_grid");

    Swivel_Connected = SafeLocalize("$valheim_vehicles_mechanism_swivel_connected");
    NoMechanismNearby = SafeLocalize("$valheim_vehicles_no_mechanism_nearby");

    PowerState_Active = SafeLocalize("$valheim_vehicles_mechanism_power_state_active");
    PowerState_Inactive = SafeLocalize("$valheim_vehicles_mechanism_power_state_inactive");
    PowerState_Inactive_NoEitrOnPlayers = SafeLocalize("$valheim_vehicles_mechanism_power_state_no_eitr_detected");

    Power_NetworkInfo_NetworkData = SafeLocalize("$valheim_vehicles_mechanism_network_data");
    Power_NetworkInfo_NetworkPower = SafeLocalize("$valheim_vehicles_mechanism_network_power");
    Power_NetworkInfo_NetworkPowerCapacity = SafeLocalize("$valheim_vehicles_mechanism_network_power_capacity");
    Power_NetworkInfo_NetworkFuel = SafeLocalize("$valheim_vehicles_mechanism_network_fuel");
    Power_NetworkInfo_NetworkFuelCapacity = SafeLocalize("$valheim_vehicles_mechanism_network_fuel_capacity");
    Power_NetworkInfo_NetworkDemand = SafeLocalize("$valheim_vehicles_mechanism_network_demand");
    Power_NetworkInfo_NetworkId = SafeLocalize("$valheim_vehicles_mechanism_network_network_id");

    // States for determining if the power system is healthy.
    Power_NetworkInfo_NetworkLowPower = SafeLocalize("$valheim_vehicles_mechanism_network_low_power");
    Power_NetworkInfo_NetworkPartialPower = SafeLocalize("$valheim_vehicles_mechanism_network_partial_power");
    Power_NetworkInfo_NetworkFullPower = SafeLocalize("$valheim_vehicles_mechanism_network_full_power");

    PowerPylon_NetworkInformation_Show = SafeLocalize("[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_mechanism_show_network_data");
    PowerPylon_NetworkInformation_Hide = SafeLocalize("[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_mechanism_show_network_data");


    PowerSource_Interact_AddOne = SafeLocalize("[<color=yellow><b>$KEY_Use</b></color>] $valheim_vehicles_mechanism_interact_add_one");
    PowerSource_Interact_AddMany = SafeLocalize($"[<color=yellow><b>{ValheimInput_KeyAltPlace}+{ValheimInput_KeyUse}</b></color>] $valheim_vehicles_mechanism_interact_add_many");


    PowerSource_NotEnoughFuel = SafeLocalize("$valheim_vehicles_mechanism_not_enough_fuel_in_inventory");
    PowerSource_FuelNameEitr = SafeLocalize("$valheim_vehicles_mechanism_fuel_name_eitr");

    PowerSource_Message_AddedFromContainer = SafeLocalize("$valheim_vehicles_mechanism_added_from_container");
    PowerSource_Message_FailedToAdd = SafeLocalize("$valheim_vehicles_mechanism_failed_to_add");

    PowerSource_Message_AddedFromPlayer = SafeLocalize("$valheim_vehicles_mechanism_added_from_player");
    Power_NetworkInfo_NetworkStatus = SafeLocalize("$valheim_vehicles_mechanism_network_status");
  }

  public static void UpdateSwivelUITranslations()
  {
    Swivel_Saved = SafeLocalize("$valheim_vehicles_swivel_saved");
    Swivel_Save = SafeLocalize("$valheim_vehicles_swivel_save");
    Swivel_Config = SafeLocalize("$valheim_vehicles_swivel_config");
    Swivel_Mode = SafeLocalize("$valheim_vehicles_swivel_mode");
    Swivel_MotionState = SafeLocalize("$valheim_vehicles_swivel_motion_state ($valheim_vehicles_ui_read_only)");
    Swivel_InterpolationSpeed = SafeLocalize("$valheim_vehicles_swivel_interpolation_speed");
    Swivel_RotationSettings = SafeLocalize("$valheim_vehicles_swivel_rotation_settings");
    Swivel_HingeAxes = SafeLocalize("$valheim_vehicles_swivel_hinge_axes");
    Swivel_MaxXAngle = SafeLocalize("$valheim_vehicles_swivel_max_x_angle");
    Swivel_MaxYAngle = SafeLocalize("$valheim_vehicles_swivel_max_y_angle");
    Swivel_MaxZAngle = SafeLocalize("$valheim_vehicles_swivel_max_z_angle");
    Swivel_MovementSettings = SafeLocalize("$valheim_vehicles_swivel_movement_settings");
    Swivel_TargetXOffset = SafeLocalize("$valheim_vehicles_swivel_target_x_offset");
    Swivel_TargetYOffset = SafeLocalize("$valheim_vehicles_swivel_target_y_offset");
    Swivel_TargetZOffset = SafeLocalize("$valheim_vehicles_swivel_target_z_offset");

// Enum values
    SwivelMode_None = SafeLocalize("$valheim_vehicles_swivel_mode_none");
    SwivelMode_Rotate = SafeLocalize("$valheim_vehicles_swivel_mode_rotate");
    SwivelMode_Move = SafeLocalize("$valheim_vehicles_swivel_mode_move");
    SwivelMode_TargetEnemy = SafeLocalize("$valheim_vehicles_swivel_mode_target_enemy");
    SwivelMode_TargetWind = SafeLocalize("$valheim_vehicles_swivel_mode_target_wind");

    Mechanism_Switch_Swivel_SelectedSwivel = SafeLocalize("$valheim_vehicles_mechanism_switch_swivel_target_swivel");
    SharedKey_Mode = SafeLocalize("$valheim_vehicles_shared_keys_mode");
  }

  private static void UpdateVehicleCommandsTranslations()
  {
    VehicleCommand_RaftCreative = SafeLocalize("$valheim_vehicles_commands_toggle_creative");
    VehicleCommand_SaveVehicle = SafeLocalize("$valheim_vehicles_commands_save_vehicle");
    VehicleCommand_OpenSelector = SafeLocalize("$valheim_vehicles_commands_open_vehicle_selector");
    VehicleCommand_SpawnSelected = SafeLocalize("$valheim_vehicles_commands_spawn_selected_vehicle");
    VehicleCommand_ZeroRotation = SafeLocalize("$valheim_vehicles_commands_zero_ship_rotation");
    VehicleCommand_ToggleMaskEditor = SafeLocalize("$valheim_vehicles_commands_toggle_mask_editor");
    VehicleCommand_ToggleOceanSway = SafeLocalize("$valheim_vehicles_commands_toggle_ocean_sway");
    VehicleCommand_RebuildBounds = SafeLocalize("$valheim_vehicles_commands_rebuild_bounds");
    VehicleCommand_HullDebugger = SafeLocalize("$valheim_vehicles_commands_hull_debugger");
    VehicleCommand_PhysicsDebugger = SafeLocalize("$valheim_vehicles_commands_physics_debugger");
    VehicleCommand_DestroyVehicle = SafeLocalize("$valheim_vehicles_commands_destroy_vehicle");
    VehicleCommand_ConfigPanel = SafeLocalize("$valheim_vehicles_commands_config_panel");

    // messages to player/console.
    VehicleCommand_Message_VehicleNotFound = SafeLocalize("$valheim_vehicles_commands_message_vehicle_not_found");
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
      // these values can be used in other translations.
      UpdateValheimInputTranslations();
      // only updates here.
      UpdateSharedTranslations();
      UpdateVehicleWheelTranslations();
      UpdateGuiEditMenuTranslations();
      UpdateVehicleConfigTranslations();
      UpdateMechanismSwitchTranslations();
      UpdateAnchorTranslations();
      UpdateSwivelTranslations();
      UpdatePowerTranslations();
      UpdateSwivelUITranslations();
      UpdateVehicleCommandsTranslations();
      UpdateVehicleCannonTranslations();
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning($"Problem while registering ModTranslations this call was likely too early.  \n{e}");
      CurrentLocalizeLanguage = "";
    }
  }
}