// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{

  /// <summary>
  /// Localization/Translation controller for the entire valheim-vehicles mod.
  ///
  /// - All static translations should be added here. There should be no dynamic values added here that call Localization.
  /// </summary>
  // ReSharper disable once PartialTypeWithSinglePart
  public partial class ModTranslations
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
    public static string ValheimInput_KeyAltPlace = null!;

    public static string SailVariant = null!;
    public static string CustomSail = null!;
    public static string DrakkalSail = null!;
    public static string RaftSail = null!;
    public static string KarveSail = null!;


    public static string MechanismSwitch_CommandsHudText = null!;
    public static string MechanismSwitch_MaskColliderEditMode = null!;
    public static string MechanismSwitch_SwitchName = null!;
    public static string MechanismSwitch_CurrentActionString = null!;
    public static string MechanismSwitch_AltActionString = null!;
    public static string MechanismSwitch_AltHoldActionString = null!;

    // modes
    public static string MechanismMode_None = null!;
    public static string MechanismMode_VehicleDock = null!;
    public static string MechanismMode_FireCannonGroup = null!;
    public static string MechanismMode_VehicleConfig = null!;
    public static string MechanismMode_Swivel_Edit = null!;
    public static string MechanismMode_Hold_To_Edit = null!;

    public static string DockingMessages_Undocked = null!;
    public static string DockingMessages_NoVehicleToDockFound = null!;
    public static string DockingMessages_NotAttachedToVehicle = null!;
    public static string DockingMessages_Docked = null!;

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


    public static string Swivel_Connected = null!;
    public static string Swivel_Name = null!;
    public static string NoMechanismNearby = null!;

    public static string PowerState_HasPower = null!;
    public static string PowerState_NoPower = null!;
    public static string PowerState_ConnectToGrid = null!;
    public static string PowerState_Active = null!;
    public static string PowerState_Inactive = null!;
    public static string PowerState_Inactive_NoEitrOnPlayers = null!;
    public static string Power_NetworkInfo_NetworkData = null!;
    public static string Power_NetworkInfo_NetworkId = null!;
    public static string Power_NetworkInfo_NetworkPower = null!;
    public static string Power_NetworkInfo_NetworkFuel = null!;
    public static string Power_NetworkInfo_NetworkDemand = null!;
    public static string Power_NetworkInfo_NetworkFuelCapacity = null!;
    public static string Power_NetworkInfo_NetworkPowerCapacity = null!;
    public static string Power_NetworkInfo_NetworkLowPower = null!;
    public static string Power_NetworkInfo_NetworkPartialPower = null!;
    public static string Power_NetworkInfo_NetworkFullPower = null!;
    public static string Power_NetworkInfo_NetworkStatus = null!;

    public static string PowerPylon_Name = null!;
    public static string PowerConduit_DrainPlate_Name = null!;
    public static string PowerConduit_ChargePlate_Name = null!;
    public static string PowerPylon_NetworkInformation_Show = null!;
    public static string PowerPylon_NetworkInformation_Hide = null!;


    public static string PowerSource_Message_AddedFromPlayer = null!;
    public static string PowerSource_Message_FailedToAdd = null!;
    public static string PowerSource_Message_AddedFromContainer = null!;

    public static string PowerSource_Interact_AddOne = null!;
    public static string PowerSource_Interact_AddMany = null!;
    public static string PowerSource_FuelNameEitr = null!;
    public static string PowerSource_NotEnoughFuel = null!;

    // full text string. This is computed from a few values.
    public static string Swivel_HoverText = null!;
    public static string Mechanism_Swivel_MotionState_AtStart;
    public static string Mechanism_Swivel_MotionState_ToStart;
    public static string Mechanism_Swivel_MotionState_AtTarget;
    public static string Mechanism_Swivel_MotionState_ToTarget;

    // generic/shared-keys
    public static string SharedKeys_Owner = null!;
    public static string SharedKeys_Hold = null!;
    public static string SharedKeys_Block = null!;
    public static string SharedKeys_Reset = null!;
    public static string SharedKeys_AddMany = null!;
    public static string SharedKeys_InteractAlt = null!;
    public static string SharedKeys_InteractPrimary = null!;
    public static string SharedKeys_Tutorial = null!;

    public static string CannonGroup_Forward = null!;
    public static string CannonGroup_Backward = null!;
    public static string CannonGroup_Left = null!;
    public static string CannonGroup_Right = null!;

    public static string Cannon_TutorialShort = null!;

    public static string WheelControls_TutorialFlight = null!;
    public static string WheelControls_FlightActivation = null!;
    public static string WheelControls_BallastActivation = null!;

    // vehicle config
    public static string VehicleConfig_Beached = null!;

    public static string CurrentLocalizeLanguage = "";

    // Swivel UI Panel Strings
    public static string Swivel_Saved = null!;
    public static string Swivel_Save = null!;
    public static string Swivel_Config = null!;
    public static string Swivel_Mode = null!;
    public static string Swivel_MotionState = null!;
    public static string Swivel_InterpolationSpeed = null!;
    public static string Swivel_RotationSettings = null!;
    public static string Swivel_HingeAxes = null!;
    public static string Swivel_MaxXAngle = null!;
    public static string Swivel_MaxYAngle = null!;
    public static string Swivel_MaxZAngle = null!;
    public static string Swivel_MovementSettings = null!;
    public static string Swivel_TargetXOffset = null!;
    public static string Swivel_TargetYOffset = null!;
    public static string Swivel_TargetZOffset = null!;

    public static string Mechanism_Switch_Swivel_SelectedSwivel = "Selected Swivel";
    public static string SharedKey_Mode = "Mode";

// SwivelMode Enum Values
    public static string SwivelMode_None = null!;
    public static string SwivelMode_Rotate = null!;
    public static string SwivelMode_Move = null!;
    public static string SwivelMode_TargetEnemy = null!;
    public static string SwivelMode_TargetWind = null!;

    public static string VehicleCommand_RaftCreative;
    public static string VehicleCommand_SaveVehicle;
    public static string VehicleCommand_OpenSelector;
    public static string VehicleCommand_SpawnSelected;
    public static string VehicleCommand_ZeroRotation;
    public static string VehicleCommand_ToggleMaskEditor;
    public static string VehicleCommand_ToggleOceanSway;
    public static string VehicleCommand_RebuildBounds;
    public static string VehicleCommand_HullDebugger;
    public static string VehicleCommand_PhysicsDebugger;
    public static string VehicleCommand_DestroyVehicle;
    public static string VehicleCommand_ConfigPanel;
    public static string VehicleCommand_Message_VehicleNotFound;

    public static string VehicleCannon_FireCannons;
    public static string VehicleCannon_FireCannonGroup;
    public static string VehicleCannon_ToggleAutoFire;
    public static string Vehicle_Cannon_Controls_Tutorial;
    public static string VehicleCannon_SwapCannonBallType;
    public static string VehicleCannon_AmmoText;
    public static string VehicleCannon_CannonBallItemExplosive;
    public static string VehicleCannon_CannonBallItemSolid;
    public static string VehicleCannon_CannonMissingNearbyPowderBarrel;

    // vehicle config
    public static string VehicleConfig_TreadsDistance;
    public static string VehicleConfig_TreadsScale;
    public static string VehicleConfig_TreadsMaxLength;
    // vehicle config sections
    public static string VehicleConfig_LandVehicle_Section;
    public static string VehicleConfig_WaterVehicle_Section;

    public static string WithBoldText(string text, string color = "white")
    {
      return $"<color={color}><b>{text}</b></color>";
    }
  }
}