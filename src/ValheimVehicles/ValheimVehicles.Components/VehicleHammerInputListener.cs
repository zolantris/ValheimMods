using System;
using UnityEngine;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Components;

public class VehicleHammerInputListener : MonoBehaviour
{
  private bool _lastMiddleMouseDown = false;

  private void Update()
  {
    if (ZNet.instance == null || ZNetScene.instance == null) return;
    var player = Player.m_localPlayer;
    if (player == null)
      return;

    if (!IsHoldingCustomHammer(player))
      return;

    var middleMouseDown = ZInput.GetMouseButton(2); // Middle mouse (button index 2)
    var middleMousePressed = !_lastMiddleMouseDown && middleMouseDown;

    if (middleMousePressed)
    {
      LoggerProvider.LogDev("[HammerInputListener] Middle mouse pressed while holding hammer");
      OnVehicleHammerSecondaryInput();
    }

    _lastMiddleMouseDown = middleMouseDown;
  }

  private static bool IsHoldingCustomHammer(Player player)
  {
    var item = player.GetCurrentWeapon();
    return item != null && item.m_shared.m_name.StartsWith(PrefabNames.VehicleHammer);
  }

  private static void OnVehicleHammerSecondaryInput()
  {
    // Add your custom logic here
    LoggerProvider.LogDev("🔥 Custom hammer action fired!");
    VehicleCommands.ToggleVehicleCommandsHud();
  }
}