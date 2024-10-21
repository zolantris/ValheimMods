using System.Collections;
using System.Collections.Generic;
using BepInEx;
using Jotunn;
using ValheimVehicles.Vehicles.Components;
using DynamicLocations.API;
using DynamicLocations.Controllers;
using DynamicLocations.Interfaces;

namespace ValheimVehicles.ModSupport;

public class DynamicLocationsLoginIntegration : IModLoginAPI
{
  public IEnumerator OnLoginMoveToZDO()
  {
    throw new System.NotImplementedException();
  }

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

  public bool IsLoginZdo(ZDO zdo)
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