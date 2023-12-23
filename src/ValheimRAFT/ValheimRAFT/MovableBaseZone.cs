using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimRAFT;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class MovableBaseZone : MonoBehaviour
{
  public MovableBaseZone instance;
  public CustomLocation boatZoneLocation;

  public void Awake()
  {
    instance = this;
    Logger.LogInfo("Loaded boat zone");
  }

  // public ZoneSystem.ZoneLocation GetBoatZone()
  // {
  // }


  public void RegisterBoatZone()
  {
    var locationConfig = new LocationConfig();
    locationConfig.Group = "boat";
    boatZoneLocation = new CustomLocation(this.gameObject, locationConfig);
    boatZoneLocation.Location.transform.SetParent(transform);
    // Logger.LogInfo($"BOATZONELOCATION: {boatZoneLocation.m_location}");
    // boatZoneLocation.m_location.transform.SetParent(transform);
    // boatZoneLocation.m,
    ZoneManager.Instance.AddCustomLocation(boatZoneLocation);

    // ZoneManager.GetMatchingBiomes("")
    // ZoneSystem.instance.PokeLocalZone(
    //   new Vector2i(transform.position));

    Logger.LogInfo(
      $"BOATZONELOCATION After registry: {boatZoneLocation} {boatZoneLocation}");
  }
}