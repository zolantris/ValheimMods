using System;
using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.Vehicles;
using Jotunn;
using SentryUnityWrapper;
using UnityEngine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using ValheimVehicles.Prefabs;
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
  private VVShip _shipInstance;

  public IVehicleShip ShipInstance
  {
    get => _shipInstance;
  }

  public WaterVehicleController Instance => this;

  public bool isCreative = false;

  internal ShipStats m_shipStats = new ShipStats();

  public float m_targetHeight { get; set; }

  public WaterVehicleFlags VehicleFlags { get; set; }
  public ZSyncTransform m_zsync { get; set; }

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  private bool _initialized = false;
  private ImpactEffect _impactEffect;

  /*
   * Must be called from
   */
  public VVShip InitializeShipValues(VVShip vvShip)
  {
    _shipInstance = vvShip;

    // connect vvShip properties to this gameobject
    m_nview = vvShip.GetComponent<ZNetView>();
    m_zsync = vvShip.GetComponent<ZSyncTransform>();
    m_syncRigidbody = vvShip.GetComponent<Rigidbody>();
    instance = this;
    _impactEffect = vvShip.GetComponent<ImpactEffect>();

    // prevent mass from being set lower than 20f;
    m_syncRigidbody.mass = Math.Max(TotalMass, 2000f);


    SetColliders(vvShip.gameObject);

    _initialized = true;
    return vvShip;
  }

  public new void Awake()
  {
    waterVehicleController = this;
    base.Awake();
    SentryUnityWrapperPlugin.BindToClient("zolantris.ValheimVehicles");
    ZdoReadyStart();
  }

  public new void Start()
  {
    base.Start();

    if (!_initialized || !(bool)waterVehicleController)
    {
      Logger.LogError("not initialized, exiting ship logic to prevent crash");
      return;
    }

    // transform.localPosition = Vector3.zero;
    // transform.localScale = Vector3.one;

    // if (!m_zsync || !m_nview || !shipInstance)
    // {
    //   Logger.LogDebug(
    //     "Awake called, but no netview or zsync, this should only call for a prefab ghost");
    //   enabled = false;
    //   return;
    // }
    //
    // if (m_nview.GetZDO() == null)
    // {
    //   Logger.LogDebug(
    //     "Awake() ZDO is null, disabling component for now");
    //   enabled = false;
    // }
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

    if (VehicleInstance == null)
    {
      Logger.LogError(
        "No ShipInstance detected");
    }

    if (VehicleInstance == null)
    {
      Logger.LogError(
        "No VehicleInstance detected");
    }

    m_nview.Register("SetAnchor",
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
    m_nview.Register("SetVisual",
      delegate(long sender, bool state) { RPC_SetVisual(sender, state); });
    waterVehicleController = this;

    FirstTimeCreation();
    ActivatePendingPiecesCoroutine();
  }

  public void UpdateVisual()
  {
    Logger.LogWarning("UpdateVisual Called but no longer does anything");
  }

  public ShipStats GetShipStats()
  {
    return m_shipStats;
  }

  /**
   * this creates the Raft 2x3 area
   */
  private void FirstTimeCreation()
  {
    var pieceCount = GetPieceCount();

    if (pieceCount != 0)
    {
      return;
    }

    Logger.LogDebug("Calling FirstTimeCreation, generating wood_floors");
    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    var pt = transform.TransformPoint(new Vector3(0f,
      ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, 0f));
    var shipHullPrefab = PrefabController.prefabManager.GetPrefab(
      ShipHulls.GetHullPrefabName(ShipHulls.HullMaterial.CoreWood,
        ShipHulls.HullOrientation.Horizontal));
    var obj = Instantiate(shipHullPrefab, pt, transform.rotation);
    var wnt = obj.GetComponent<WearNTear>();
    if ((bool)wnt)
    {
      wnt.m_supports = true;
      wnt.m_support = 2000f;
      wnt.m_noSupportWear = true;
      wnt.m_noRoofWear = true;
    }

    var netView = obj.GetComponent<ZNetView>();
    if ((bool)netView)
    {
      AddNewPiece(netView);
    }
    else
    {
      Logger.LogError("called destroy on obj, due to netview not existing");
      Destroy(obj);
    }
  }

  public void Ascend()
  {
    if (VehicleFlags.HasFlag(WaterVehicleFlags.IsAnchored))
    {
      SetAnchor(state: false);
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
      SetAnchor(state: false);
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


  public void UpdateStats(bool flight)
  {
    if (!m_rigidbody || m_statsOverride)
    {
      return;
    }

    // m_rigidbody.mass = TotalMass;
    m_rigidbody.angularDrag = (flight ? 1f : 0f);
    m_rigidbody.drag = (flight ? 1f : 0f);

    if ((bool)_shipInstance)
    {
      _shipInstance.m_angularDamping = (flight ? 5f : 0.8f);
      _shipInstance.m_backwardForce = 1f;
      _shipInstance.m_damping = (flight ? 5f : 0.35f);
      _shipInstance.m_dampingSideway = (flight ? 3f : 0.3f);
      _shipInstance.m_force = 3f;
      _shipInstance.m_forceDistance = 5f;
      _shipInstance.m_sailForceFactor = (flight ? 0.2f : 0.05f);
      _shipInstance.m_stearForce = (flight ? 0.2f : 1f);
      _shipInstance.m_stearVelForceFactor = 1.3f;
      _shipInstance.m_waterImpactDamage = 0f;
      /*
       * this may be unstable and require a getter each time...highly doubt it though.
       */
      // ImpactEffect impact = ShipInstance.GetComponent<ImpactEffect>();
      if ((bool)_impactEffect)
      {
        _impactEffect.m_interval = 0.1f;
        _impactEffect.m_minVelocity = 0.1f;
        _impactEffect.m_damages.m_damage = 100f;
      }
      else
      {
        Logger.LogDebug("No Ship ImpactEffect detected, this needs to be added to the custom ship");
      }
    }
  }

  public void SetAnchor(bool state)
  {
    m_nview.InvokeRPC("SetAnchor", state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    VehicleFlags = (state
      ? (VehicleFlags | WaterVehicleFlags.IsAnchored)
      : (VehicleFlags & ~WaterVehicleFlags.IsAnchored));
    m_nview.m_zdo.Set("MBFlags", (int)VehicleFlags);
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

  public void Init()
  {
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