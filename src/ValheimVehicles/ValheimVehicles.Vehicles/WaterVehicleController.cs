using System;
using System.Collections.Generic;
using System.Linq;
using ValheimVehicles.Vehicles;
using Jotunn;
using UnityEngine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimRAFT.Util;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

public class WaterVehicleController : BaseVehicleController
{
  public BaseVehicleController baseVehicleController;
  public VVShip vehicleShip;
  public const string ControllerID = "WaterVehicle";

  [Flags]
  public enum MBFlags
  {
    None = 0,
    IsAnchored = 1,
    HideMesh = 2
  }

  public bool isCreative = false;

  internal Rigidbody m_rigidbody => vehicleShip.m_body;

  internal ShipStats m_shipStats = new ShipStats();

  internal ZNetView m_nview;

  internal ZSyncTransform m_zsync;

  public float m_targetHeight;

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  public MBFlags m_flags;

  public void Awake()
  {
    // vikingShipPrefab.AddComponent();

    // baseVehicleController = gameObject.AddComponent<BaseVehicleController>();
    m_nview = GetComponent<ZNetView>();
    if (!(bool)m_nview)
    {
      Logger.LogDebug("WaterVehicleController.Awake() NetView does not exist, creating new one");
      m_nview = gameObject.AddComponent<ZNetView>();
    }

    if (m_nview.GetZDO() == null)
    {
      Logger.LogDebug("WaterVehicleController.Awake() ZDO was null, creating new one");
      m_nview.m_zdo = new ZDO()
      {
        Persistent = true,
      };
    }

    Logger.LogDebug($"WaterVehicleController.Awake() NetView {m_nview}");

    m_zsync = GetComponent<ZSyncTransform>();

    if (!(bool)m_zsync)
    {
      m_zsync = gameObject.AddComponent<ZSyncTransform>();
    }

    // ship = gameObject.GetComponent<ValheimShip>();
    if (!(bool)vehicleShip)
    {
      Logger.LogError(
        "no 1 component detected, this could mean the WaterVehicleController is orphaned from a ship script which initialized it");
      return;
    }

    // m_baseRootObject = new GameObject
    // {
    //   name = ControllerID,
    //   layer = 0
    // };
    m_nview.Register("SetAnchor",
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
    m_nview.Register("SetVisual",
      delegate(long sender, bool state) { RPC_SetVisual(sender, state); });
    // vehicleController = this;
    // m_nview = m_nview;

    Logger.LogDebug($"NVIEW ZDO {m_nview.m_zdo}");

    m_id = ZDOPersistantID.Instance.GetOrCreatePersistantID(m_nview.m_zdo);

    if (!(bool)vehicleShip.m_body)
    {
      Logger.LogError("Rigidbody not detected for WaterVehicleController ship");
    }

    // m_baseRootObject.transform.SetParent(null);
    // m_baseRootObject.transform.position = transform.position;
    // m_baseRootObject.transform.rotation = transform.rotation;
    UpdateVisual();

    // BoxCollider[] colliders = base.transform.GetComponentsInChildren<BoxCollider>();

    // baseVehicle.m_onboardcollider =
    //   colliders.FirstOrDefault((BoxCollider k) => k.gameObject.name == "OnboardTrigger");

    // Instantiate(, m_baseRootObject.transform);
    // vehicleShip.m_floatcollider = PrefabController.shipFloatCollider;
    // m_onboardcollider = PrefabController.shipOnboardCollider;

    if (!m_onboardcollider)
    {
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

    // blockingCollider.transform.parent.gameObject.layer =
    //   ValheimRaftPlugin.CustomRaftLayer;


    // baseVehicle.m_blockingcollider = blockingCollider;
    Logger.LogDebug($"Made it to 164");
    ActivatePendingPiecesCoroutine();

    FirstTimeCreation();
  }

  public void FixedUpdate()
  {
  }

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

  public void OnDestroy()
  {
    if ((bool)baseVehicleController)
    {
      CleanUp();
      UnityEngine.Object.Destroy(gameObject);
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
    if (GetPieceCount() != 0)
    {
      return;
    }

    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    var floor = ZNetScene.instance.GetPrefab("wood_floor");
    for (float x = -1f; x < 1.01f; x += 2f)
    {
      for (float z = -2f; z < 2.01f; z += 2f)
      {
        Vector3 pt = base.transform.TransformPoint(new Vector3(x,
          ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
        var obj = Instantiate(floor, pt, transform.rotation);
        ZNetView netView = obj.GetComponent<ZNetView>();
        AddNewPiece(netView);
      }
    }
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
      if (!baseVehicleController || !m_floatcollider)
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
      if (!baseVehicleController || !m_floatcollider)
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
    if (!m_rigidbody || !baseVehicleController || m_statsOverride)
    {
      return;
    }

    m_rigidbody.mass = TotalMass;
    m_rigidbody.angularDrag = (flight ? 1f : 0f);
    m_rigidbody.drag = (flight ? 1f : 0f);

    if ((bool)vehicleShip)
    {
      vehicleShip.m_angularDamping = (flight ? 5f : 0.8f);
      vehicleShip.m_backwardForce = 1f;
      vehicleShip.m_damping = (flight ? 5f : 0.35f);
      vehicleShip.m_dampingSideway = (flight ? 3f : 0.3f);
      vehicleShip.m_force = 3f;
      vehicleShip.m_forceDistance = 5f;
      vehicleShip.m_sailForceFactor = (flight ? 0.2f : 0.05f);
      vehicleShip.m_stearForce = (flight ? 0.2f : 1f);
      vehicleShip.m_stearVelForceFactor = 1.3f;
      vehicleShip.m_waterImpactDamage = 0f;
      ImpactEffect impact = vehicleShip.GetComponent<ImpactEffect>();
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