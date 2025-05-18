using System.Collections.Generic;
using ValheimVehicles.Components;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
namespace ValheimVehicles.UI;

/// <summary>
/// Nit-pick: rename GUI to different value. It makes names messy
/// </summary>
public static class VehicleGUIItems
{
  public static readonly List<GenericInputAction> configSections =
  [
    new()
    {
      title = "Treads Max Width"
      // description = "Set the max width of treads",
      // saveAction = () =>
      // {
      // },
      // resetAction = () => {}
      // onSubmit: () => 
      // onChange: () => 
    }
  ];


  // todo translate all of this.
  public static readonly List<GenericInputAction> commandButtonActions =
  [
    new()
    {
      title = ModTranslations.Swivel_Name ?? "Raft Creative",
      OnButtonPress = VehicleCommands.ToggleCreativeMode
    },
    new()
    {
      title = ModTranslations.Swivel_Save ?? "Save Vehicle",
      OnButtonPress = VehicleStorageController.SaveClosestVehicle
    },
    new()
    {
      title = ModTranslations.Swivel_Saved ?? "Open Save Vehicle Selector",
      inputType = InputType.Dropdown,
      OnCreateDropdown = (dropdown) =>
      {
        VehicleGui.VehicleSelectDropdown = dropdown;
        VehicleStorageController.RefreshVehicleSelectionGui(dropdown);
      },
      OnDropdownChanged = VehicleGui.VehicleSelectOnDropdownChanged
    },
    new()
    {
      title = ModTranslations.PowerPylon_NetworkInformation_Show ?? "[Admin] Spawn Selected Vehicle",
      OnButtonPress = VehicleStorageController.SpawnSelectedVehicle,
      IsAdminOnly = true
    },
    new()
    {
      title = ModTranslations.SwivelMode_Rotate ?? "Zero Ship Rotation X/Z",
      OnButtonPress = () =>
      {
        var onboardHelpers = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
        if (onboardHelpers != null) onboardHelpers.FlipShip();
      }
    },
    new()
    {
      title = ModTranslations.MechanismSwitch_MaskColliderEditMode ?? "Toggle WaterMask Editor",
      OnButtonPress = VehicleCommands.ToggleColliderEditMode
    },
    new()
    {
      title = ModTranslations.SwivelMode_TargetWind ?? "Toggle Ocean Sway",
      OnButtonPress = VehicleCommands.VehicleToggleOceanSway
    },
    new()
    {
      title = ModTranslations.PowerState_ConnectToGrid ?? "Rebuild Bounds",
      IsAdminOnly = true,
      OnButtonPress = () =>
      {
        var vpc = VehicleDebugHelpers.GetVehiclePiecesController();
        if (vpc == null)
          return;
        vpc.ForceRebuildBounds();
      }
    },
    new()
    {
      title = ModTranslations.SwivelMode_Move ?? "Hull debugger",
      OnButtonPress = VehicleGui.ToggleConvexHullDebugger
    },
    new()
    {
      title = ModTranslations.SwivelMode_TargetEnemy ?? "Physics Debugger",
      OnButtonPress = VehicleGui.ToggleColliderDebugger
    },
#if DEBUG
    new()
    {
      title = ModTranslations.PowerState_NoPower ?? "Destroy Current Vehicle",
      OnButtonPress = VehicleCommands.DestroyCurrentVehicle
    },
    new()
    {
      title = ModTranslations.Swivel_Config ?? "Config",
      OnButtonPress = () =>
      {
        VehicleGui.ToggleConfigPanelState(true);
      }
    }
#endif
  ];
}