using System.Collections;
using System.Collections.Generic;
using BepInEx;
using ValheimVehicles.Vehicles.Components;
using DynamicLocations.API;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.ModSupport;

public class DynamicLocationsLoginIntegration : IModLoginAPI
{
  public PluginInfo PluginInfo { get; }
  public bool UseDefaultCallbacks { get; }
  public int MovementTimeout { get; }
  public bool ShouldFreezePlayer { get; }

  public int LoginPrefabHashCode { get; } = Prefabs.PrefabNames
    .WaterVehicleShip
    .GetHashCode();

  public int Priority { get; }
  public List<string> RunBeforePlugins { get; }
  public List<string> RunAfterPlugins { get; }

  public IEnumerator OnLoginMoveToZDO(
    PlayerSpawnController playerSpawnController)
  {
    throw new System.NotImplementedException();
  }

  public IEnumerator OnLoginMoveToZDO(ZDO zdo, Vector3? offset,
    PlayerSpawnController playerSpawnController)
  {
    var spawnZdo = playerSpawnController.PlayerSpawnPointZDO;
    if (spawnZdo == null)
    {
      var pendingZdo = playerSpawnController.FindDynamicZdo(
        PlayerSpawnController
          .LocationTypes.Logout, true);
      yield return pendingZdo;
      spawnZdo = pendingZdo.Current;
    }

    var vehicle = GetVehicleFromZdo(zdo);
    while (vehicle == null || playerSpawnController.HasExpiredTimer)
    {
      yield return new WaitForFixedUpdate();
      vehicle = GetVehicleFromZdo(zdo);
    }

    if (vehicle == null) yield break;


    yield return new WaitUntil(() =>
      vehicle.Instance.PiecesController.IsActivationComplete ||
      playerSpawnController.HasExpiredTimer);
    Logger.LogDebug(
      $"Waiting completed, IsActivationComplete {vehicle.Instance.PiecesController.IsActivationComplete} HasExpiredTimer: {playerSpawnController.HasExpiredTimer}");

    // resetting timer gives the player a bit more time to get to their location if there are slowdowns.
    playerSpawnController.RestartTimer();
    yield return playerSpawnController.MovePlayerToZdo(zdo, offset);
  }

  public bool OnLoginMatchZdoPrefab(ZDO zdo)
  {
    var isMatch = zdo.GetPrefab() == LoginPrefabHashCode;
    if (!isMatch)
    {
      Logger.LogDebug("VehicleShip not detected for Login ZDO");
    }

    return isMatch;
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