using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
namespace ValheimVehicles.Structs;

public struct GenericInputAction()
{
  public string title = "";
  public bool IsAdminOnly = false;
  public Action OnButtonPress = () => {};
  public InputType inputType = InputType.Button;
  public Action<TMP_Dropdown>? OnCreateDropdown = null;
  public UnityAction<int>? OnDropdownChanged = null;
  public Action<TMP_Dropdown>? OnPointerEnterAction = null;
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