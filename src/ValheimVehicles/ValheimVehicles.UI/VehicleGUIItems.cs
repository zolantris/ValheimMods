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
      title = ModTranslations.VehicleCommand_RaftCreative ?? "Raft Creative",
      OnButtonPress = VehicleCommands.ToggleCreativeMode
    },
    new()
    {
      title = ModTranslations.VehicleCommand_SaveVehicle ?? "Save Vehicle",
      OnButtonPress = VehicleStorageController.SaveClosestVehicle
    },
    new()
    {
      title = ModTranslations.VehicleCommand_OpenSelector ?? "Open Save Vehicle Selector",
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
      title = ModTranslations.VehicleCommand_SpawnSelected ?? "[Admin] Spawn Selected Vehicle",
      OnButtonPress = VehicleStorageController.SpawnSelectedVehicle,
      IsAdminOnly = true
    },
    new()
    {
      title = ModTranslations.VehicleCommand_ZeroRotation ?? "Zero Ship Rotation X/Z",
      OnButtonPress = () =>
      {
        var onboardHelpers = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
        if (onboardHelpers != null) onboardHelpers.FlipShip();
      }
    },
    new()
    {
      title = ModTranslations.VehicleCommand_ToggleMaskEditor ?? "Toggle WaterMask Editor",
      OnButtonPress = VehicleCommands.ToggleColliderEditMode
    },
    new()
    {
      title = ModTranslations.VehicleCommand_ToggleOceanSway ?? "Toggle Ocean Sway",
      OnButtonPress = VehicleCommands.VehicleToggleOceanSway
    },
    new()
    {
      title = ModTranslations.VehicleCommand_RebuildBounds ?? "Rebuild Bounds",
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
      title = ModTranslations.VehicleCommand_HullDebugger ?? "Hull debugger",
      OnButtonPress = VehicleGui.ToggleConvexHullDebugger
    },
    new()
    {
      title = ModTranslations.VehicleCommand_PhysicsDebugger ?? "Physics Debugger",
      OnButtonPress = VehicleGui.ToggleColliderDebugger
    },
#if DEBUG
    new()
    {
      title = ModTranslations.VehicleCommand_DestroyVehicle ?? "Destroy Current Vehicle",
      OnButtonPress = VehicleCommands.DestroyCurrentVehicle
    },
    new()
    {
      title = ModTranslations.VehicleCommand_ConfigPanel ?? "Config",
      OnButtonPress = () =>
      {
        VehicleGui.ToggleConfigPanelState(true);
      }
    }
#endif
  ];
}