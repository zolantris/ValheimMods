using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
namespace ValheimVehicles.ValheimVehicles.Components;

public class TargetControlsInteractive : MonoBehaviour, Hoverable, Interactable,
  IDoodadController
{
  public TargetController targetController;
  public ZNetView m_nview;

  private Dictionary<Player, CannonFiringHotkeys> playerHotkeyControllers = new();

  public void Start()
  {
    this.WaitForZNetView((nv) =>
    {
      m_nview = nv;
      targetController = GetComponent<TargetController>();
    });
  }

  public string GetHoverText()
  {
    var controlCenter = Localization.instance.Localize("$valheim_vehicles_cannon_control_center");
    return $"{controlCenter}\n{ModTranslations.SharedKeys_InteractPrimary}";
  }
  public string GetHoverName()
  {
    return "";
  }

  public void AddOrRemoveHotKeyControllers(Player player, bool shouldRemove)
  {
    if (shouldRemove || !targetController)
    {
      if (!targetController && m_nview != null)
      {
        LoggerProvider.LogWarning($"TargetController is null for {name}. Removing player from controls but netview is valid.");
      }
      if (playerHotkeyControllers.TryGetValue(player, out var hotkeyController))
      {
        Destroy(hotkeyController);
        playerHotkeyControllers.Remove(player);
      }
      return;
    }

    if (!shouldRemove)
    {
      var cannonFiringHotkeys = player.gameObject.GetOrAddComponent<CannonFiringHotkeys>();
      cannonFiringHotkeys.SetTargetController(targetController);
      playerHotkeyControllers[player] = cannonFiringHotkeys;
      targetController.OnDetectionModeChange();
    }
  }

  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    var player = user.GetComponent<Player>();
    if (!player) return false;
    var shouldRemovePreviousDoodad = player.m_doodadController != null;

    if (player.IsAttached())
    {
      player.AttachStop();
      return false;
    }
    targetController.OnDetectionModeChange();
    AddOrRemoveHotKeyControllers(player, shouldRemovePreviousDoodad);

    player.m_doodadController = shouldRemovePreviousDoodad ? null : this;
    return true;
  }

  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    LoggerProvider.LogDev($"UseItem called to {name}");
    return false;
  }

  public void OnUseStop(Player player)
  {
    LoggerProvider.LogDev($"OnUseStop called to {name}");
  }

  public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
  {
    LoggerProvider.LogDebugDebounced($"{moveDir}");
  }

  public Component GetControlledComponent()
  {
    return this;
  }

  public Vector3 GetPosition()
  {
    return transform.position;
  }

  public bool IsValid()
  {
    return m_nview && m_nview.IsValid();
  }
}