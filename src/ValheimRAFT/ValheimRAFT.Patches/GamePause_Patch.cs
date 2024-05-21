using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Vehicles;

namespace ValheimRAFT.Patches;

public class GamePause_Patch
{
  // does not need transpiler, should always set to 1f on a ship
  // This could be applied to other ships for QOL fixes...would be good for base valheim
  [HarmonyPatch(typeof(Game), "UpdatePause")]
  [HarmonyPostfix]
  private static void UpdatePausePostfix()
  {
    PreventTimeFreezeOnShip();
  }

  private static void PreventTimeFreezeOnShip()
  {
    if (!Player.m_localPlayer) return;
    var baseVehicleShip = Player.m_localPlayer?.GetComponentInParent<BaseVehicleController>();

    // not on ship, do nothing
    if (!baseVehicleShip) return;

    var hasPeerConnections = ZNet.instance?.GetPeerConnections() > 0;

    // Previously onPause the time was set to 0 regarless if using multiplayer, which borks ZDOs and Physics updates that are reliant on the controlling player.
    // Also causes issues with Players that are on a ship controlled by another player and the ship moves out of range. The player is forced through the wall or smashed up into the air.
    if (Game.IsPaused())
    {
      Time.timeScale = hasPeerConnections ? 1f : 0f;
    }
    else
    {
      Time.timeScale = hasPeerConnections ? 1f : Game.m_timeScale;
    }
  }
}