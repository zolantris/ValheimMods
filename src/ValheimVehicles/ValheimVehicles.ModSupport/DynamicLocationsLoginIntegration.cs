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
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
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

    ZNet.instance.SetReferencePosition(zdo.GetPosition());

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
      $"Waiting completed, IsActivationComplete {vehicle.Instance.PiecesController.IsActivationComplete} timer: {localTimer.ElapsedMilliseconds}");

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