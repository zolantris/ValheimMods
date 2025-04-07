using System;
using UnityEngine;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Vehicles.Components;

namespace Components;

public class CustomToggleSwitch : MonoBehaviour, Interactable, Hoverable
{
  /// <summary>
  /// Actions for VehicleCommmands
  /// </summary>
  /// todo
  /// Add actions for Non-vehicle commands to open a panel or just add a panel toggle as another command. Also retain the last position of the command.
  public enum ActivationActions
  {
    CommandsHud,
    CreativeMode,
    ColliderEditMode
  }


  private void HandleToggleCreativeMode()
  {
    VehicleCommands.ToggleCreativeMode();
  }

  private void HandleToggleCommandsHud()
  {
    VehicleCommands.ToggleVehicleCommandsHud();
  }

  public ActivationActions m_activationType = ActivationActions.CreativeMode;

  public void OnPressHandler(CustomToggleSwitch toggleSwitch, Humanoid humanoid)
  {
    switch (m_activationType)
    {
      case ActivationActions.CommandsHud:
        HandleToggleCommandsHud();
        break;
      case ActivationActions.CreativeMode:
        HandleToggleCreativeMode();
        break;
      case ActivationActions.ColliderEditMode:
        VehicleCommands.ToggleColliderEditMode();
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public ActivationActions GetNextAction()
  {
    return m_activationType switch
    {
      ActivationActions.CommandsHud => ActivationActions.CreativeMode,
      ActivationActions.CreativeMode => ActivationActions.ColliderEditMode,
      ActivationActions.ColliderEditMode => ActivationActions.CommandsHud,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public void SwapHandlerToNextAction()
  {
    m_activationType = GetNextAction();
  }

  public void OnAltPressHandler()
  {
    SwapHandlerToNextAction();
  }

  public bool Interact(Humanoid character, bool hold, bool alt)
  {
    if (hold)
      return false;
    if (!alt)
    {
      OnPressHandler(this, character);
    }
    else
    {
      OnAltPressHandler();
    }
    return true;
  }

  public string GetLocalizedActionText(ActivationActions action)
  {
    return action switch
    {
      ActivationActions.CommandsHud => localizedCommandsHudText,
      ActivationActions.CreativeMode => localizedCreativeModeText,
      ActivationActions.ColliderEditMode => localizedMaskColliderEditMode,
      _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public static string localizedCommandsHudText = "";
  public static string localizedMaskColliderEditMode = "";
  public static string localizedCreativeModeText = "";
  public static string nextActionString = "";
  public static string currentActionString = "";
  public static string switchName = "";

  public void GetToggleSwitchText()
  {
    currentActionString = Localization.instance.Localize(
      "[<color=yellow><b>$KEY_Use</b></color>] To Toggle:");
    nextActionString = Localization.instance.Localize(
      "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] To Switch To:");

    localizedCreativeModeText = Localization.instance.Localize(
      "$valheim_vehicles_commands_creative_mode");
    localizedMaskColliderEditMode = Localization.instance.Localize(
      "$valheim_vehicles_commands_mask_edit_mode");
    localizedCommandsHudText = Localization.instance.Localize(
      "$valheim_vehicles_commands_edit_menu");
  }


  public string GetHoverName()
  {
    if (switchName == string.Empty)
    {
      switchName = Localization.instance.Localize("$valheim_vehicles_toggle_switch");
    }
    return switchName;
  }

  public string GetHoverText()
  {
    if (localizedCreativeModeText == string.Empty || localizedCommandsHudText == string.Empty || currentActionString == string.Empty || nextActionString == string.Empty || localizedMaskColliderEditMode == string.Empty)
    {
      GetToggleSwitchText();
    }

    return $"{currentActionString} {GetLocalizedActionText(m_activationType)}\n{nextActionString} {GetLocalizedActionText(GetNextAction())}";
  }
}