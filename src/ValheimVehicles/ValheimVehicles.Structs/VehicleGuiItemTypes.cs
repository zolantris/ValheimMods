using System;
using UnityEngine.Events;
using UnityEngine.UI;
namespace ValheimVehicles.Structs;

public struct GenericInputAction()
{
  public string title = "";
  public Action OnButtonPress = () => {};
  public InputType inputType = InputType.Button;
  public Action<Dropdown>? OnCreateDropdown = null;
  public UnityAction<int>? OnDropdownChanged = null;
}

public enum InputType
{
  Button,
  Input,
  Dropdown
}

public struct InputAction
{
  public string title;
  public string description;
  public Action saveAction;
  public Action resetAction;
}