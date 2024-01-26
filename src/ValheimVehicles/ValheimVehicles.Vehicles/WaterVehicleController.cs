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

public class WaterVehicleController : BaseVehicleController
{
  private VVShip _shipInstance;

  public VVShip ShipInstance
  {
    get => _shipInstance;
    set => _shipInstance = InitializeShipValues(value);
  }

  [Flags]
  public enum MBFlags
  {
    None = 0,
    IsAnchored = 1,
    HideMesh = 2
  }

  public bool isCreative = false;

  internal ShipStats m_shipStats = new ShipStats();

  internal ZSyncTransform m_zsync;

  public float m_targetHeight;

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  public MBFlags m_flags;
  private bool _initialized = false;

  /*
   * Must be called from
   */
  private VVShip InitializeShipValues(VVShip vvShip)
  {
    VehicleInstance = vvShip;

    // connect vvShip properties to this gameobject
    m_nview = vvShip.GetComponent<ZNetView>();
    m_zsync = vvShip.GetComponent<ZSyncTransform>();
    m_syncRigidbody = vvShip.GetComponent<Rigidbody>();

    // prevent mass from being set lower than 20f;
    m_syncRigidbody.mass = Math.Max(TotalMass, 20f);


    SetColliders(vvShip.gameObject);

    _initialized = true;
    return vvShip;
  }

  public new void Awake()
  {
    base.Awake();
    vehicleController = this;
    SentryUnityWrapperPlugin.BindToClient("zolantris.ValheimVehicles");
  }

  public new void Start()
  {
    base.Start();

    if (!_initialized || !(bool)vehicleController)
    {
      Logger.LogError("not initialized, exiting ship logic to prevent crash");
      return;
    }

    // transform.localPosition = Vector3.zero;
    // transform.localScale = Vector3.one;
    ZdoReadyStart();

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
    vehicleController = this;

    Logger.LogDebug($"NVIEW ZDO {m_nview.m_zdo}");

    UpdateVisual();

    if (!m_onboardcollider)
    {
      // m_onboardcollider = new BoxCollider();
      Logger.LogError(
        $"ONBOARD COLLIDER {m_onboardcollider}, collider must not be null");
    }

    // var blockingCollider = ship.gameObject.AddComponent<BoxCollider>();
    // blockingCollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    // m_blockingcollider = vehicleShip.m_blockingCollider;
    // if ((bool)baseVehicle.m_blockingcollider)
    // {
    //   baseVehicle.m_blockingcollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    //   baseVehicle.m_blockingcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    //   baseVehicle.m_blockingcollider.transform.localPosition = new Vector3(0f, 0.29f, 0f);
    // }
    // else
    // {
    //   Logger.LogError("No baseVehicle collider to update");
    // }

    // baseVehicle.m_blockingcollider = blockingCollider;
    Logger.LogDebug($"Made it to 164");
    FirstTimeCreation();
  }

  // private void OnEnable()
  // {
  //   if (!_initialized)
  //   {
  //     return;
  //   }
  //
  //   ZdoReadyStart();
  // }


  public void UpdateVisual()
  {
    if (m_nview.m_zdo != null)
    {
      m_flags = (MBFlags)m_nview.m_zdo.GetInt("MBFlags", (int)m_flags);
      // var newTransform = m_flags.HasFlag(MBFlags.HideMesh) ? Vector3.zero : Vector3.one;
      /*
       * hide with vector transform instead of active change to prevent NRE spam.
       * Previously these called gameobject SetActive(!m_flags.HasFlag(MBFlags.HideMesh));
       */
      // transform.Find("ship/visual").gameObject.transform.localScale = newTransform;
      // transform.Find("interactive").gameObject.transform.localScale = newTransform;
    }
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
    //  && m_nview.GetZDO() != null
    Logger.LogDebug(
      $"Calling FirstTimeCreation before getpiece check pieces count {GetPieceCount()}");
    if (GetPieceCount() != 0)
    {
      return;
    }

    Logger.LogDebug("Calling FirstTimeCreation, generating wood_floors");
    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    // var floor = ZNetScene.instance.GetPrefab("wood_floor");

    // var shipHullPrefab = PrefabController.prefabManager.GetPrefab(
    //   PrefabController.GetHullPrefabName(ShipHulls.HullMaterial.CoreWood,
    //     ShipHulls.HullOrientation.Horizontal));
    // var obj = Instantiate(shipHullPrefab);

    GameObject floor = ZNetScene.instance.GetPrefab("wood_floor");
    var wnt = floor.GetComponent<WearNTear>();
    wnt.m_supports = true;
    wnt.m_support = 2000f;
    wnt.m_noSupportWear = true;
    wnt.m_noRoofWear = true;
    for (float x = -1f; x < 1.01f; x += 2f)
    {
      for (float z = -2f; z < 2.01f; z += 2f)
      {
        Vector3 pt = transform.TransformPoint(new Vector3(x,
          ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
        var obj = Instantiate(floor, pt, transform.rotation);
        ZNetView netview = obj.GetComponent<ZNetView>();
        AddNewPiece(netview);
      }
    }

    // var netView = obj.GetComponent<ZNetView>();

    // if (!(bool)netView)
    // {
    //   Logger.LogError("No netView for wood_floor initialized floor");
    //   return;
    // }
    //
    // if (netView.GetZDO() == null)
    // {
    //   Logger.LogError("No ZDO for wood_floor initialized floor");
    //   return;
    // }
    //
    // AddNewPiece(netView);

    // for (float x = -1f; x < 1.01f; x += 2f)
    // {
    //   for (float z = -2f; z < 2.01f; z += 2f)
    //   {
    //     Vector3 pt = transform.TransformPoint(new Vector3(x,
    //       ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
    //     var obj = Instantiate(floor, pt, transform.rotation);
    //     var wnt = obj.GetComponent<WearNTear>();
    //     if ((bool)wnt)
    //     {
    //       wnt.m_noSupportWear = true;
    //     }
    //     else
    //     {
    //       Logger.LogError("No WNT for wood_floor initialized floor");
    //     }
    //
    //     ZNetView netView = obj.GetComponent<ZNetView>();
    //     if (!(bool)netView)
    //     {
    //       Logger.LogError("No netView for wood_floor initialized floor");
    //     }
    //
    //     if (netView.GetZDO() == null)
    //     {
    //       Logger.LogError("No ZDO for wood_floor initialized floor");
    //     }
    //
    //     AddNewPiece(netView);
    //   }
    // }
  }

  internal void Ascend()
  {
    if (m_flags.HasFlag(MBFlags.IsAnchored))
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

  internal void Descent()
  {
    if (m_flags.HasFlag(MBFlags.IsAnchored))
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

    if ((bool)ShipInstance)
    {
      ShipInstance.m_angularDamping = (flight ? 5f : 0.8f);
      ShipInstance.m_backwardForce = 1f;
      ShipInstance.m_damping = (flight ? 5f : 0.35f);
      ShipInstance.m_dampingSideway = (flight ? 3f : 0.3f);
      ShipInstance.m_force = 3f;
      ShipInstance.m_forceDistance = 5f;
      ShipInstance.m_sailForceFactor = (flight ? 0.2f : 0.05f);
      ShipInstance.m_stearForce = (flight ? 0.2f : 1f);
      ShipInstance.m_stearVelForceFactor = 1.3f;
      ShipInstance.m_waterImpactDamage = 0f;
      ImpactEffect impact = ShipInstance.GetComponent<ImpactEffect>();
      if ((bool)impact)
      {
        impact.m_interval = 0.1f;
        impact.m_minVelocity = 0.1f;
        impact.m_damages.m_damage = 100f;
      }
      else
      {
        Logger.LogDebug("No Ship ImpactEffect detected, this needs to be added to the custom ship");
      }
    }
  }

  internal void SetAnchor(bool state)
  {
    m_nview.InvokeRPC("SetAnchor", state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    m_flags = (state ? (m_flags | MBFlags.IsAnchored) : (m_flags & ~MBFlags.IsAnchored));
    m_nview.m_zdo.Set("MBFlags", (int)m_flags);
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
    m_flags = (state ? (m_flags | MBFlags.HideMesh) : (m_flags & ~MBFlags.HideMesh));
    m_nview.m_zdo.Set("MBFlags", (int)m_flags);
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