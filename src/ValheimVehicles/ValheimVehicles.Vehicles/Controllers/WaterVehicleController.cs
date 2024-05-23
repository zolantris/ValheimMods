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

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  /*
   * Must be called from
   */
  public void InitializeShipValues(VehicleShip vehicleShip)
  {
    VehicleInstance = vehicleShip;
    base.VehicleInstance = VehicleInstance;

    if (!(bool)m_syncRigidbody)
    {
      m_syncRigidbody = vehicleShip.m_body;
    }

    if (!(bool)m_rigidbody)
    {
      m_rigidbody = GetComponent<Rigidbody>();
    }

    // connect vvShip properties to this gameobject
    m_nview = vehicleShip.m_nview;
    instance = this;

    // prevent mass from being set lower than 20f;
    m_rigidbody.mass = Math.Max(TotalMass, 2000f);

    SetColliders(vehicleShip);
    ZdoReadyStart();
    LoadInitState();
  }

  public new void Awake()
  {
    waterVehicleController = this;
    base.Awake();

    ZdoReadyStart();
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

  public void OnEnable()
  {
    ZdoReadyStart();
  }

  public void OnDisable()
  {
    m_nview.Unregister("SetAnchor");

    // todo this likely is not needed for boat v2. Maybe only used for water effects
    m_nview.Unregister("SetVisual");
  }

  private void ZdoReadyStart()
  {
    if (!(bool)m_nview) return;

    Logger.LogDebug($"ZdoReadyAwake called, zdo is: {m_nview.GetZDO()}");
    if (m_nview.GetZDO() == null)
    {
      return;
    }

    // this may get called twice.
    GetPersistentID();

    // vital for vehicle
    InitializeBaseVehicleValuesWhenReady();

    if (base.VehicleInstance == null)
    {
      Logger.LogError(
        "No ShipInstance detected");
    }
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