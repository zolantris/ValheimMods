using System;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Constants;
using ValheimVehicles.Components;
using ValheimVehicles.Structs;

namespace Components;

public class CustomToggleSwitch : AnimatedLeverMechanism, Interactable, Hoverable
{
  /// <summary>
  /// Actions for VehicleCommmands
  /// </summary>
  /// todo
  /// Add actions for Non-vehicle commands to open a panel or just add a panel toggle as another command. Also retain the last position of the command.
  public enum ToggleSwitchAction
  {
    CommandsHud,
    CreativeMode,
    ColliderEditMode
  }

  public ToggleSwitchAction mToggleSwitchType = ToggleSwitchAction.CreativeMode;
  private ZNetView netView;

  public void Awake()
  {
    netView = GetComponent<ZNetView>();
  }
  public void Start()
  {
    SyncSwitchMode();
  }

  public void OnEnable()
  {
    netView.Register(nameof(RPC_UpdateSwitch), RPC_UpdateSwitch);
  }

  public void OnDisable()
  {
    netView.Unregister(nameof(RPC_UpdateSwitch));
  }

  public void RPC_UpdateSwitch(long sender)
  {
    SyncSwitchMode();
  }

  /// <summary>
  /// This must be run by the client that needs to update the switch
  /// </summary>
  public void UpdateSwitch()
  {
    netView.m_zdo.Set(VehicleZdoVars.ToggleSwitchAction, mToggleSwitchType.ToString());
    // todo may want to just send the string to other clients.
    netView.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_UpdateSwitch));
  }

  public ToggleSwitchAction GetActivationActionFromString(string activationActionString)
  {
    if (!Enum.TryParse<ToggleSwitchAction>(activationActionString, out var result))
    {
      result = ToggleSwitchAction.CommandsHud;
    }

    return result;
  }

  public void SyncSwitchMode()
  {
    if (!netView || netView.GetZDO() == null || !isActiveAndEnabled) return;
    var activationActionString = netView.GetZDO().GetString(VehicleZdoVars.ToggleSwitchAction, nameof(ToggleSwitchAction.CreativeMode));
    mToggleSwitchType = GetActivationActionFromString(activationActionString);
  }

  private void HandleToggleCreativeMode()
  {
    VehicleCommands.ToggleCreativeMode();
  }

  private void HandleToggleCommandsHud()
  {
    VehicleCommands.ToggleVehicleCommandsHud();
  }

  public void OnPressHandler(CustomToggleSwitch toggleSwitch, Humanoid humanoid)
  {
    switch (mToggleSwitchType)
    {
      case ToggleSwitchAction.CommandsHud:
        HandleToggleCommandsHud();
        break;
      case ToggleSwitchAction.CreativeMode:
        HandleToggleCreativeMode();
        break;
      case ToggleSwitchAction.ColliderEditMode:
        VehicleCommands.ToggleColliderEditMode();
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public ToggleSwitchAction GetNextAction()
  {
    return mToggleSwitchType switch
    {
      ToggleSwitchAction.CommandsHud => ToggleSwitchAction.CreativeMode,
      ToggleSwitchAction.CreativeMode => ToggleSwitchAction.ColliderEditMode,
      ToggleSwitchAction.ColliderEditMode => ToggleSwitchAction.CommandsHud,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public void SwapHandlerToNextAction()
  {
    mToggleSwitchType = GetNextAction();
  }

  public void OnAltPressHandler()
  {
    SwapHandlerToNextAction();
    UpdateSwitch();
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

  public string GetLocalizedActionText(ToggleSwitchAction action)
  {
    return action switch
    {
      ToggleSwitchAction.CommandsHud => ModTranslations.ToggleSwitch_CommandsHudText,
      ToggleSwitchAction.CreativeMode => ModTranslations.CreativeMode,
      ToggleSwitchAction.ColliderEditMode => ModTranslations.ToggleSwitch_MaskColliderEditMode,
      _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }


  public string GetHoverName()
  {
    return ModTranslations.ToggleSwitch_SwitchName;
  }

  public string GetHoverText()
  {
    return $"{ModTranslations.ToggleSwitch_CurrentActionString} {GetLocalizedActionText(mToggleSwitchType)}\n{ModTranslations.ToggleSwitch_NextActionString} {GetLocalizedActionText(GetNextAction())}";
  }
}