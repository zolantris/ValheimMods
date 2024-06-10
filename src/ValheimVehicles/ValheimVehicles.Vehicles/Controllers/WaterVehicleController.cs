using System;
using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.Vehicles;
using Jotunn;
using Jotunn.Managers;
using SentryUnityWrapper;
using UnityEngine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles.Components;
using ValheimVehicles.Vehicles.Interfaces;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class WaterVehicleController : BaseVehicleController, IWaterVehicleController
{
  private VehicleShip? _vehicleInstance;

  public VehicleShip? VehicleInstance
  {
    get => _vehicleInstance;
    set => _vehicleInstance = value;
  }

  public WaterVehicleController Instance => this;

  internal ShipStats m_shipStats = new();


  /*
   * Must be called from the component that instantiates this Component's gameobject IE VehicleShip
   */
  public new void InitFromShip(VehicleShip vehicleShip)
  {
    waterVehicleController = this;
    instance = this;
    base.InitFromShip(vehicleShip);
  }

  public new void Awake()
  {
    waterVehicleController = this;
    base.Awake();
  }

  public override void Start()
  {
    if (BaseVehicleInitState == InitializationState.Pending || !(bool)waterVehicleController)
    {
      Logger.LogError("not initialized, exiting ship logic to prevent crash");
      return;
    }

    base.Start();
  }

  public void OnDisable()
  {
    m_nview.Unregister("SetAnchor");

    // todo this likely is not needed for boat v2. Maybe only used for water effects
    m_nview.Unregister("SetVisual");
  }

  public void UpdateVisual()
  {
    Logger.LogWarning("UpdateVisual Called but no longer does anything");
  }

  public ShipStats GetShipStats()
  {
    return m_shipStats;
  }

  public void SyncRigidbodyStats(bool flight)
  {
    var drag = (flight ? 1f : 0.5f);
    var angularDrag = (flight ? 1f : 0.5f);
    base.SyncRigidbodyStats(drag, angularDrag);
  }

  public void CalculateSailSpeed()
  {
  }

  public void GetFloatation()
  {
  }

  public void CalculateSway()
  {
  }


  public void CalculateRigidbodyForces()
  {
  }
}