using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn;
using UnityEngine;
using ValheimRAFT.Util;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

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


  public static IEnumerator SetPlayerOnBoat(ZDO zdo, Player player)
  {
    var zdoSector = zdo.GetSector();
    var zdoPosition = zdo.GetPosition();
    var playerOffsetHash = BaseVehicleController.GetDynamicParentOffset(player.m_nview);

    ZoneSystem.instance.PokeLocalZone(zdoSector);
    yield return new WaitUntil(() => ZoneSystem.instance.IsZoneLoaded(zdoSector));

    var newPosition = zdoPosition + playerOffsetHash;
    player.transform.position = newPosition;
    player.m_nview.GetZDO().SetPosition(newPosition);

    yield return null;
  }

  private static void PreventTimeFreezeOnShip()
  {
    if (!Player.m_localPlayer) return;
    var baseVehicleShip = Player.m_localPlayer?.GetComponentInParent<BaseVehicleController>();

    // not on ship, do nothing
    if (!baseVehicleShip) return;

    var hasPeerConnections = ZNet.instance?.GetPeerConnections() > 0 ||
                             ValheimRaftPlugin.Instance.ShipPausePatchSinglePlayer.Value;
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