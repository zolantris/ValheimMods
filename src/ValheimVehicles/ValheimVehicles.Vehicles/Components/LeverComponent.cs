using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles.Components;

namespace Components;

public class LeverComponent : MonoBehaviour, Hoverable, Interactable
{
  public struct LeverOption
  {
    public string Name;
    public Action Callback;
  }

  public enum LeverState
  {
    ON,
    OFF,
  }

  public enum LeverAction
  {
    Open,
    Close,
    ToggleWaterMaskController,
    Trigger,
  }

  // public static Dictionary<LeverAction, LeverOption> LeverOptions = new Dictionary<LeverAction, LeverOption>()
  // {
  //   { LeverAction.Toggle, new LeverOption()
  //   {
  //     Name = "Vehicle WaterMask Control Panel",
  //     Action = new Action()
  //   }}
  // };
  //


  public LeverAction selectedAction;
  public LeverState leverState;

  public LeverOption? SelectedOption;
  public ZNetView m_nview;

  private void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    leverState = LeverState.ON;
  }

  public void SetOption()
  {
  }

  public bool OnActionTrigger()
  {
    return selectedAction switch
    {
      LeverAction.ToggleWaterMaskController => VehicleMeshMaskManager.OnTriggerPanelFromLever(
        gameObject, leverState == LeverState.ON),
      LeverAction.Open or LeverAction.Close or LeverAction.Trigger => false,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    var output = OnActionTrigger();
    return output;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    return false;
  }

  public string GetHoverText()
  {
    return "Open menu";
  }

  public string GetHoverName()
  {
    return "Water Mask Point";
  }
}