using System;
using System.Collections.Generic;
using UnityEngine.UI;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Structs;
using ValheimVehicles.ValheimVehicles.API;
namespace ValheimVehicles.Vehicles.Components;

/// <summary>
/// Nit-pick: rename GUI to different value. It makes names messy
/// </summary>
public class VehicleGUIItems
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


  public static readonly List<GenericInputAction> commandButtonActions =
  [
    new()
    {
      title = "Hull debugger",
      OnButtonPress = VehicleGui.ToggleConvexHullDebugger
    },
    new()
    {
      title = "Physics Debugger",
      OnButtonPress = VehicleGui.ToggleColliderDebugger
    },
    new()
    {
      title = "Raft Creative",
      OnButtonPress = VehicleCommands.ToggleCreativeMode
    },
    new()
    {
      title = "Save Vehicle",
      OnButtonPress = VehicleStorageAPI.SaveClosestVehicle
    },
    new()
    {
      title = "Open Save Vehicle Selector",
      // action = VehicleGui.OpenVehicleSelectorGUi,
      inputType = InputType.Dropdown,
      OnCreateDropdown = (dropdown) =>
      {
        VehicleGui.VehicleSelectDropdown = dropdown;
      },
      OnDropdownChanged = VehicleGui.VehicleSelectOnDropdownChanged
    },
    new()
    {
      title = "Spawn Selected Vehicle",
      OnButtonPress = VehicleStorageAPI.SpawnSelectedVehicle
    },
    new()
    {
      title = "Zero Ship Rotation X/Z",
      OnButtonPress = () =>
      {
        var onboardHelpers = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
        if (onboardHelpers != null) onboardHelpers.FlipShip();
      }
    },
    new()
    {
      title = "Toggle Ocean Sway",
      OnButtonPress = VehicleCommands.VehicleToggleOceanSway
    },
    new()
    {
      title = "Rebuild Bounds",
      OnButtonPress = () =>
      {
        var vpc = VehicleDebugHelpers.GetVehiclePiecesController();
        if (vpc == null)
          return;
        vpc.ForceRebuildBounds();
      }
    },
#if DEBUG
    new()
    {
      title = "Config",
      OnButtonPress = () =>
      {
        VehicleGui.ToggleConfigPanelState(true);
      }
    }
#endif
  ];
}