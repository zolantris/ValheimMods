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


    public static string MechanismSwitch_CommandsHudText = null!;
    public static string MechanismSwitch_MaskColliderEditMode = null!;
    public static string MechanismSwitch_SwitchName = null!;
    public static string MechanismSwitch_CurrentActionString = null!;
    public static string MechanismSwitch_AltActionString = null!;

    // modes
    public static string MechanismMode_None = null!;
    public static string MechanismMode_Swivel_Edit = null!;

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
    public static string PowerState_Conduit_Active = null!;
    public static string PowerState_Conduit_Inactive = null!;
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

    public static string PowerPylon_Name = null!;
    public static string PowerConduit_DrainPlate_Name = null!;
    public static string PowerPylon_NetworkInformation_Show = null!;
    public static string PowerPylon_NetworkInformation_Hide = null!;


    public static string PowerSource_Message_AddedFromPlayer = null!;
    public static string PowerSource_Message_AddedFromContainer = null!;

    public static string PowerSource_Interact_AddOne = null!;
    public static string PowerSource_Interact_AddMany = null!;
    public static string PowerSource_FuelNameEitr = null!;
    public static string PowerSource_NotEnoughFuel = null!;

    // full text string. This is computed from a few values.
    public static string Swivel_HoverText = null!;


    // generic/shared-keys
    public static string SharedKeys_Owner = null!;
    public static string SharedKeys_Hold = null!;

    // vehicle config
    public static string VehicleConfig_Beached = null!;

    public static string CurrentLocalizeLanguage = "";

    public static string WithBoldText(string text, string color = "white")
    {
      return $"<color={color}><b>{text}</b></color>";
    }
  }
}