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

namespace ValheimVehicles.Components;

public class MapPinSync : MonoBehaviour
{
  public struct CustomPinZdoData
  {
    public Minimap.PinData pinData;
    public ZDO zdo;
  }

  private Dictionary<Vector3, CustomPinZdoData> _vehiclePins = new();
  public static MapPinSync Instance;
  private ZDO? cachedPlayerSpawnZdo = null;
  private Vector3? cachedLastBedVector = null;
  private Minimap.PinData? cachedLastBedPinData = null;
  private Coroutine? refreshDynamicSpawnPinRoutine;
  private Coroutine? refreshVehiclePinsRoutine;

  public string GetOwnerNameFromZdo(ZDO zdo)
  {
    return CensorShittyWords.FilterUGC(zdo.GetString(ZDOVars.s_ownerName),
      UGCType.CharacterName, Player.m_localPlayer.GetOwner());
  }

  public bool hasInitialized = false;

  public void Awake()
  {
    Instance = this;
    if (ZNet.instance == null) return;
    if (ZNet.instance.IsDedicated()) return;
    ZdoWatchController.Instance.GetAllZdoGuids();
  }

  private void OnEnable()
  {
    MinimapManager.OnVanillaMapDataLoaded += OnMapReady;
  }

  private void OnDisable()
  {
    MinimapManager.OnVanillaMapDataLoaded -= OnMapReady;
    StopAllCoroutines();
    refreshVehiclePinsRoutine = null;
    hasInitialized = false;
  }

  private void OnMapReady()
  {
    if (ZNet.instance == null || Minimap.instance == null) return;
    StartVehiclePinSync();
    StartSpawnPinSync();
    hasInitialized = true;
  }

  public void StartVehiclePinSync()
  {
    if (refreshVehiclePinsRoutine != null)
    {
      StopCoroutine(refreshVehiclePinsRoutine);
      refreshVehiclePinsRoutine = null;
    }

    refreshVehiclePinsRoutine = StartCoroutine(RefreshVehiclePins());
  }

  public void StartSpawnPinSync()
  {
    if (refreshDynamicSpawnPinRoutine != null)
    {
      StopCoroutine(refreshDynamicSpawnPinRoutine);
      refreshDynamicSpawnPinRoutine = null;
    }
    refreshDynamicSpawnPinRoutine = StartCoroutine(RefreshDynamicSpawnPin());
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

  private void ClearSpawnPin(Minimap.PinData? pinData)
  {
    if (pinData != null) Minimap.instance.RemovePin(pinData);

    cachedLastBedPinData = null;
    cachedLastBedVector = null;
  }

  private IEnumerator UpdatePlayerSpawnPin()
  {
    if (Minimap.instance == null) yield break;
    if (PlayerSpawnController.Instance == null) yield break;
    if (cachedPlayerSpawnZdo == null)
      yield return PlayerSpawnController.Instance.FindDynamicZdo(
        LocationVariation.Spawn,
        data => { cachedPlayerSpawnZdo = data; });

    if (cachedPlayerSpawnZdo == null)
    {
      ClearSpawnPin(cachedLastBedPinData);
      yield break;
    }

    var nextPosition = cachedPlayerSpawnZdo.GetPosition();

    // if the previous key exists, it needs to be removed.
    if (cachedLastBedVector != nextPosition)
      ClearSpawnPin(cachedLastBedPinData);

    // make sure the key does not already exist/not point to update it if so.
    if (Minimap.instance.m_pins.Contains(cachedLastBedPinData)) yield break;

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
    if (Minimap.instance == null) return;
    if (Minimap.instance.m_pins == null) return;
    if (ZdoWatchController.Instance == null) return;
    var guids = ZdoWatchController.Instance.GetAllZdoGuids();
    var vehicleZdos = guids.Select(x => x.Value).Where(x =>
    {
      var prefab = ZNetScene.instance.GetPrefab(x.GetPrefab());
      if (prefab == null) return false;
      return PrefabNames.IsVehicle(prefab.name);
    }).ToHashSet();

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
          if (isVisible) _vehiclePins[zdoPosition] = vehiclePin.Value;

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

  public IEnumerator RefreshVehiclePins()
  {
    while (isActiveAndEnabled)
    {
      yield return new WaitForSeconds(MinimapConfig.VehiclePinSyncInterval.Value);
      if (ZNet.instance == null) yield return null;
      if (ZNet.instance != null && ZNet.instance.IsDedicated()) yield break;
      if (Player.m_localPlayer == null) yield return null;
      // Clear existing pins from both dictionaries
      ClearAllVehiclePins();

      // Regenerate all pins
      UpdateVehiclePins();
    }
  }

  private void ClearAllVehiclePins()
  {
    if (Minimap.instance == null) return;
    if (Minimap.instance.m_pins == null) return;
    // Remove each pin in _vehiclePins from m_locationPins
    foreach (var vehiclePin in _vehiclePins.Where(vehiclePin =>
               Minimap.instance.m_pins.Contains(vehiclePin.Value.pinData)))
      Minimap.instance.RemovePin(vehiclePin.Value.pinData);

    // Clear the local _vehiclePins dictionary
    _vehiclePins.Clear();
  }
}