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
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

[Flags]
public enum WaterVehicleFlags
{
  None = 0,
  IsAnchored = 1,
  HideMesh = 2
}

public class WaterVehicleController : BaseVehicleController, IWaterVehicleController
{
  private VehicleShip? _vehicleInstance;

  public VehicleShip? VehicleInstance
  {
    get => _vehicleInstance;
    set => _vehicleInstance = value;
  }

  public WaterVehicleController Instance => this;

  public bool isCreative = false;

  internal ShipStats m_shipStats = new ShipStats();

  public float m_targetHeight { get; set; }

  public WaterVehicleFlags VehicleFlags { get; set; }

  public ZSyncTransform m_zsyncTransform
  {
    get => VehicleInstance.m_zsyncTransform;
    set { }
  }

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
    m_zsyncTransform = vehicleShip.m_zsyncTransform;
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

    m_nview.Register("SetAnchor",
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
    m_nview.Register("SetVisual",
      delegate(long sender, bool state) { RPC_SetVisual(sender, state); });
  }

  public void UpdateVisual()
  {
    Logger.LogWarning("UpdateVisual Called but no longer does anything");
  }

  public ShipStats GetShipStats()
  {
    return m_shipStats;
  }

  public void Ascend()
  {
    if (VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
    {
      SendSetAnchor(state: false);
    }

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      m_targetHeight = 0f;
    }
    else
    {
      if (!m_floatcollider)
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(m_floatcollider.transform.position.y + 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
    }

    m_nview.m_zdo.Set("MBTargetHeight", m_targetHeight);
  }

  public void Descent()
  {
    if (VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
    {
      SendSetAnchor(state: false);
    }

    float oldTargetHeight = m_targetHeight;
    if (!ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      m_targetHeight = 0f;
    }
    else
    {
      if (!m_floatcollider)
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(m_floatcollider.transform.position.y - 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
      if (m_floatcollider.transform.position.y - 1f <=
          ZoneSystem.instance.m_waterLevel)
      {
        m_targetHeight = 0f;
      }
    }

    m_nview.m_zdo.Set("MBTargetHeight", m_targetHeight);
  }

  public void SyncRigidbodyStats(bool flight)
  {
    var drag = (flight ? 1f : 0f);
    var angularDrag = (flight ? 1f : 0f);

    if (flight && ValheimRaftPlugin.Instance.FlightNoAngularVelocity.Value)
    {
      angularDrag = 10f;
    }

    if (flight && ValheimRaftPlugin.Instance.FlightHasDrag.Value)
    {
      drag = 10f;
    }


    base.SyncRigidbodyStats(drag, angularDrag);
  }

/*
 * Toggle the ship anchor and emit the event to other players so their client can update
 */
  public void ToggleAnchor()
  {
    var isAnchored = waterVehicleController.VehicleFlags.HasFlag(
      WaterVehicleFlags.IsAnchored);
    VehicleFlags = isAnchored
      ? (VehicleFlags & ~WaterVehicleFlags.IsAnchored)
      : (VehicleFlags | WaterVehicleFlags.IsAnchored);
    m_nview.m_zdo.Set("MBFlags", (int)VehicleFlags);
    SendSetAnchor(waterVehicleController.VehicleFlags.HasFlag(
      WaterVehicleFlags.IsAnchored));
  }

  public void SendSetAnchor(bool state)
  {
    m_nview.InvokeRPC("SetAnchor", state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    // if (sender != Player.m_localPlayer.GetZDOID().UserID)
    // {
    VehicleFlags = (state
      ? (VehicleFlags | WaterVehicleFlags.IsAnchored)
      : (VehicleFlags & ~WaterVehicleFlags.IsAnchored));
    m_nview.m_zdo.Set("MBFlags", (int)VehicleFlags);
    // }
  }

  internal void SetVisual(bool state)
  {
    m_nview.InvokeRPC("SetVisual", state);
  }

  /**
   * deprecated, not needed
   */
  public void RPC_SetVisual(long sender, bool state)
  {
    VehicleFlags = (state
      ? (VehicleFlags | WaterVehicleFlags.HideMesh)
      : (VehicleFlags & ~WaterVehicleFlags.HideMesh));
    m_nview.m_zdo.Set("MBFlags", (int)VehicleFlags);
    UpdateVisual();
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