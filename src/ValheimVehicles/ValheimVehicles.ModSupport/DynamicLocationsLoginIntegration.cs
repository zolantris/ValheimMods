using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx;
using ValheimVehicles.Vehicles.Components;
using DynamicLocations.API;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;
using DynamicLocations.Structs;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.ModSupport;

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
    var localTimer = Zolantris.Shared.Debug.DebugSafeTimer.StartNew();
    var spawnZdo =
      LocationController
        .GetCachedDynamicLocation(DynamicLocationVariation.Logout)
        ?.Zdo;
    if (spawnZdo == null)
    {
      var pendingZdo = playerSpawnController.FindDynamicZdo(
        DynamicLocationVariation.Logout, true);
      yield return new WaitUntil(() =>
        pendingZdo.Current is ZDO || localTimer.ElapsedMilliseconds > 5000);
      spawnZdo = pendingZdo.Current as ZDO;
    }

    var vehicle = GetVehicleFromZdo(zdo);
    while (vehicle == null ||
           PlayerSpawnController.HasExpiredTimer(localTimer, 5000))
    {
      yield return new WaitForFixedUpdate();
      vehicle = GetVehicleFromZdo(zdo);
    }

    if (vehicle == null) yield break;


    yield return new WaitUntil(() =>
      vehicle.Instance.PiecesController.IsActivationComplete ||
      localTimer.ElapsedMilliseconds > 5000);
    Logger.LogDebug(
      $"Waiting completed, IsActivationComplete {vehicle.Instance.PiecesController.IsActivationComplete} HasExpiredTimer: {PlayerSpawnController.HasExpiredTimer}");
    ;
    yield return playerSpawnController.MovePlayerToZdo(zdo, offset);
  }

  // Internal Methods

  private VehicleShip? GetVehicleFromZdo(ZDO zdo)
  {
    var vehicleShipNetView = ZNetScene.instance.FindInstance(zdo);
    if (!vehicleShipNetView) return null;
    var vehicleShip = vehicleShipNetView.GetComponent<VehicleShip>();
    return vehicleShip;
  }
}