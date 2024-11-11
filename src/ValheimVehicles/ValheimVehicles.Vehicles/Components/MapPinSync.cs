using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using DynamicLocations.Constants;
using DynamicLocations.Controllers;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ZdoWatcher;

namespace ValheimVehicles.Vehicles.Components;

public class MapPinSync : MonoBehaviour
{
  public struct CustomPinZdoData
  {
    public Minimap.PinData pinData;
    public ZDO zdo;
  }

  Dictionary<Vector3, CustomPinZdoData> _vehiclePins = new();
  public static MapPinSync Instance;

  public string GetOwnerNameFromZdo(ZDO zdo)
  {
    return CensorShittyWords.FilterUGC(zdo.GetString(ZDOVars.s_ownerName),
      UGCType.CharacterName, Player.m_localPlayer.GetOwner().ToString());
  }

  public void Awake()
  {
    Instance = this;
    if (ZNet.instance.IsDedicated()) return;
    MinimapManager.OnVanillaMapDataLoaded += OnMapReady;
    ZdoWatchController.Instance.GetAllZdoGuids();
  }

  private void OnDestroy()
  {
    CancelInvoke(nameof(RefreshVehiclePins));
    StopCoroutine(nameof(RefreshDynamicSpawnPin));
  }

  private void OnMapReady()
  {
    StartVehiclePinSync();
    StartSpawnPinSync();
  }

  public void StartVehiclePinSync()
  {
    CancelInvoke(nameof(RefreshVehiclePins));
    InvokeRepeating(nameof(RefreshVehiclePins), 0f,
      MinimapConfig.VehiclePinSyncInterval.Value);
  }

  public void StartSpawnPinSync()
  {
    StopCoroutine(nameof(RefreshDynamicSpawnPin));
    StartCoroutine(RefreshDynamicSpawnPin());
  }

  private bool IsWithinVisibleRadius(Vector3 point)
  {
    if (MinimapConfig.ShowAllVehiclesOnMap.Value) return true;
    if (Player.m_localPlayer == null) return false;
    var distanceBetweenPlayerAndPoint =
      Vector3.Distance(Player.m_localPlayer.transform.position, point);
    var isWithinVisibleRadius = distanceBetweenPlayerAndPoint <=
                                MinimapConfig.VisibleVehicleRadius.Value;
    return isWithinVisibleRadius;
  }

  private ZDO? cachedPlayerSpawnZdo = null;
  private Vector3? cachedLastBedVector = null;
  private Minimap.PinData? cachedLastBedPinData = null;

  private void ClearSpawnPin(Minimap.PinData? pinData)
  {
    if (pinData != null)
    {
      Minimap.instance.RemovePin(pinData);
    }

    cachedLastBedPinData = null;
    cachedLastBedVector = null;
  }

  private IEnumerator UpdatePlayerSpawnPin()
  {
    if (PlayerSpawnController.Instance == null) yield break;
    if (cachedPlayerSpawnZdo == null)
    {
      yield return PlayerSpawnController.Instance.FindDynamicZdo(
        LocationVariation.Spawn,
        onComplete: (
          data => { cachedPlayerSpawnZdo = data; }));
    }

    if (cachedPlayerSpawnZdo == null)
    {
      ClearSpawnPin(cachedLastBedPinData);
      yield break;
    }

    var nextPosition = cachedPlayerSpawnZdo.GetPosition();

    // if the previous key exists, it needs to be removed.
    if (cachedLastBedVector != nextPosition)
    {
      ClearSpawnPin(cachedLastBedPinData);
    }

    // make sure the key does not already exist/not point to update it if so.
    if (Minimap.instance.m_pins.Contains(cachedLastBedPinData))
    {
      yield break;
    }

    cachedLastBedPinData = Minimap.instance.AddPin(nextPosition,
      Minimap.PinType.Bed,
      "Spawn", false, false, Player.m_localPlayer.GetOwner());
    cachedLastBedVector = nextPosition;
  }

  public IEnumerator RefreshDynamicSpawnPin()
  {
    while (isActiveAndEnabled)
    {
      yield return UpdatePlayerSpawnPin();
      yield return new WaitForSeconds(MinimapConfig.BedPinSyncInterval.Value);
    }
  }

  private void UpdateVehiclePins()
  {
    var guids = ZdoWatchController.Instance.GetAllZdoGuids();
    var vehicleZdos = guids.Select(x => x.Value).Where(x =>
      ZNetScene.instance.GetPrefab(x.GetPrefab()).name
        .Contains(PrefabNames.WaterVehicleShip)).ToHashSet();

    var allPins = Minimap.instance.m_pins;
    var pinZdosToSkip = new HashSet<ZDO>();

    // Update existing pins
    foreach (var vehiclePin in _vehiclePins)
    {
      var getPin = allPins.Find(pin => pin.m_pos == vehiclePin.Key);
      if (getPin != null)
      {
        var zdoPosition = vehiclePin.Value.zdo.GetPosition();
        getPin.m_pos = zdoPosition;
        var isVisible = IsWithinVisibleRadius(zdoPosition);
        // Update the key in _vehiclePins without removing and re-adding
        if (!vehiclePin.Key.Equals(zdoPosition))
        {
          if (isVisible)
          {
            _vehiclePins[zdoPosition] = vehiclePin.Value;
          }

          _vehiclePins.Remove(vehiclePin.Key);
        }

        pinZdosToSkip.Add(vehiclePin.Value.zdo);
      }
    }

    // Add new pins for ZDOs not already processed
    foreach (var zdo in vehicleZdos)
    {
      if (pinZdosToSkip.Contains(zdo)) continue;

      var position = zdo.GetPosition();
      var isVisible = IsWithinVisibleRadius(position);
      if (isVisible)
      {
        var zdoOwner = zdo.GetOwner();
        var pinData = Minimap.instance.AddPin(position,
          Minimap.PinType.Icon4,
          $"Vehicle", false, false, zdoOwner);

        _vehiclePins[position] = new CustomPinZdoData
          { pinData = pinData, zdo = zdo };
      }
    }
  }

  public void RefreshVehiclePins()
  {
    if (ZNet.instance.IsDedicated()) return;
    if (Player.m_localPlayer == null) return;
    // Clear existing pins from both dictionaries
    ClearAllVehiclePins();

    // Regenerate all pins
    UpdateVehiclePins();
  }

  private void ClearAllVehiclePins()
  {
    // Remove each pin in _vehiclePins from m_locationPins
    foreach (var vehiclePin in _vehiclePins.Where(vehiclePin =>
               Minimap.instance.m_pins.Contains(vehiclePin.Value.pinData)))
    {
      Minimap.instance.RemovePin(vehiclePin.Value.pinData);
    }

    // Clear the local _vehiclePins dictionary
    _vehiclePins.Clear();
  }
}