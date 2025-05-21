using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx;
using ValheimVehicles.Components;
using DynamicLocations.API;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;
using DynamicLocations.Structs;
using JetBrains.Annotations;
using Jotunn;
using UnityEngine;
using UnityEngine.UI;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using Zolantris.Shared.Debug;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.ModSupport;

[UsedImplicitly]
public class DynamicLocationsLoginIntegration : DynamicLoginIntegration
{
  /// <inheritdoc />
  public DynamicLocationsLoginIntegration(IntegrationConfig config) :
    base(config)
  {
  }

  protected override IEnumerator OnLoginMoveToZDO(ZDO zdo, Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    var localTimer = Stopwatch.StartNew();
    var onboardData =
      VehicleOnboardController.GetOnboardCharacterData(Player.m_localPlayer);

    // character is already onboard a vehicle. We assume it's the same one...
    if (onboardData != null)
    {
      if (onboardData.OnboardController)
        yield break;
    }

    yield return playerSpawnController.MovePlayerToZdo(zdo, offset, true, true);

    var vehicle = GetVehicleFromZdo(zdo);
    while (vehicle == null &&
           localTimer.ElapsedMilliseconds < 5000)
    {
      yield return new WaitForFixedUpdate();
      vehicle = GetVehicleFromZdo(zdo);
    }

    if (vehicle == null)
    {
      if (Player.m_localPlayer.IsDebugFlying())
      {
        Player.m_localPlayer.ToggleDebugFly();
      }

      yield break;
    }

    yield return new WaitUntil(() => vehicle.Instance != null && vehicle.Instance.PiecesController != null && (
                                       vehicle.Instance.PiecesController.isInitialPieceActivationComplete ||
                                       vehicle.Instance.PiecesController.IsActivationComplete) ||
                                     localTimer.ElapsedMilliseconds > 2000);

    if (ModSupportConfig.DynamicLocationsShouldSkipMovingPlayerToBed.Value)
    {
      MovePlayerToBedOnShip(vehicle);
    }

    if (Player.m_localPlayer != null)
    {
      Player.m_localPlayer.m_body.isKinematic = false;
    }

    var isActivationComplete = vehicle.Instance.PiecesController != null && vehicle.Instance.PiecesController.IsActivationComplete;

    Logger.LogDebug(
      $"Waiting completed, IsActivationComplete {isActivationComplete} timer: {localTimer.ElapsedMilliseconds}");
  }

  private static void PlayerMoveToTransformSafe(Transform bedTransform)
  {
    if (Player.m_localPlayer == null) return;
    if (Player.m_localPlayer.IsDebugFlying())
    {
      Player.m_localPlayer.ToggleDebugFly();
    }

    var offset = bedTransform.position + Vector3.up * 1.5f;

    if (PlayerSpawnController.Instance != null)
    {
      PlayerSpawnController.Instance.DynamicTeleport(offset,
        Player.m_localPlayer.transform.rotation);
    }
  }

  private bool MovePlayerToBedOnShip(VehicleManager vehicle)
  {
    var bedPieces = vehicle.Instance.PiecesController.GetBedPieces();
    if (bedPieces.Count < 1) return false;
    if (bedPieces.Count == 1)
    {
      PlayerMoveToTransformSafe(bedPieces[0].transform);
      return true;
    }

    var bedPlacedByPlayer = bedPieces.Find((p) => p.IsMine());
    var selectedPiece = bedPlacedByPlayer != null
      ? bedPlacedByPlayer
      : bedPieces.First();
    if (selectedPiece == null) return false;

    PlayerMoveToTransformSafe(selectedPiece.transform);
    return true;
  }

  // Internal Methods

  private VehicleManager? GetVehicleFromZdo(ZDO zdo)
  {
    var vehicleShipNetView = ZNetScene.instance.FindInstance(zdo);
    if (!vehicleShipNetView) return null;
    var vehicleShip = vehicleShipNetView.GetComponent<VehicleManager>();
    return vehicleShip;
  }
}