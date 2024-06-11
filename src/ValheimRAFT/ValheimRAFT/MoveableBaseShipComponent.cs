using System;
using System.Linq;
using UnityEngine;
using ValheimRAFT.Util;
using ValheimVehicles.Vehicles;
using ZdoWatcher;
using Logger = Jotunn.Logger;

namespace ValheimRAFT;

public class MoveableBaseShipComponent : MonoBehaviour
{
  [Flags]
  public enum MBFlags
  {
    None = 0,
    IsAnchored = 1,
    HideMesh = 2
  }

  public VehicleDebugHelpers VehicleDebugHelpersInstance;
  public static bool HasVehicleDebugger;

  public MoveableBaseRootComponent m_baseRoot;

  public bool isCreative = false;

  internal Rigidbody m_rigidbody;

  internal Ship m_ship;

  internal ShipStats m_shipStats = new ShipStats();

  internal ZNetView m_nview;

  internal GameObject m_baseRootObject;

  internal ZSyncTransform m_zsync;

  public float m_targetHeight;

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  public MBFlags m_flags;
  public bool IsAnchored => m_flags.HasFlag(MBFlags.IsAnchored);

  public MoveableBaseRootComponent GetMbRoot()
  {
    return m_baseRoot;
  }

  public void Awake()
  {
    Ship ship = GetComponent<Ship>();
    m_nview = GetComponent<ZNetView>();
    m_zsync = GetComponent<ZSyncTransform>();
    m_ship = GetComponent<Ship>();
    m_baseRootObject = new GameObject
    {
      name = "MovableBase",
      layer = 0
    };
    m_baseRoot = m_baseRootObject.AddComponent<MoveableBaseRootComponent>();
    m_nview.Register("SetAnchor",
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
    m_nview.Register("SetVisual",
      delegate(long sender, bool state) { RPC_SetVisual(sender, state); });
    m_baseRoot.shipController = this;
    m_baseRoot.m_nview = m_nview;
    m_baseRoot.m_ship = ship;
    m_baseRoot.m_id = ZdoWatchManager.Instance.GetOrCreatePersistentID(m_nview.m_zdo);
    m_rigidbody = GetComponent<Rigidbody>();
    m_baseRoot.m_syncRigidbody = m_rigidbody;
    m_rigidbody.maxAngularVelocity = ValheimRaftPlugin.Instance.MaxPropulsionSpeed.Value;
    m_rigidbody.mass = m_baseRoot.TotalMass;
    m_baseRootObject.transform.SetParent(null);
    Logger.LogDebug("Set baseRoot params from BaseShipComponent");

    m_baseRootObject.transform.position = base.transform.position;
    m_baseRootObject.transform.rotation = base.transform.rotation;
    ship.transform.Find("ship/visual/mast")?.gameObject.SetActive(value: false);
    ship.transform.Find("ship/colliders/log")?.gameObject.SetActive(value: false);
    ship.transform.Find("ship/colliders/log (1)")?.gameObject.SetActive(value: false);
    ship.transform.Find("ship/colliders/log (2)")?.gameObject.SetActive(value: false);
    ship.transform.Find("ship/colliders/log (3)")?.gameObject.SetActive(value: false);
    UpdateVisual();
    BoxCollider[] colliders = base.transform.GetComponentsInChildren<BoxCollider>();
    m_baseRoot.m_onboardcollider =
      colliders.FirstOrDefault((BoxCollider k) => k.gameObject.name == "OnboardTrigger");

    if (!m_baseRoot.m_onboardcollider)
    {
      Logger.LogError(
        $"ONBOARD COLLIDER {m_baseRoot.m_onboardcollider}, collider must not be null");
    }
    else
    {
      if (m_baseRoot.m_onboardcollider != null)
        m_baseRoot.m_onboardcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    m_baseRoot.m_floatcollider = ship.m_floatCollider;
    m_baseRoot.m_floatcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    m_baseRoot.m_blockingcollider = ship.transform.Find("ship/colliders/Cube")
      .GetComponentInChildren<BoxCollider>();
    m_baseRoot.m_blockingcollider.transform.localScale = new Vector3(1f, 1f, 1f);
    m_baseRoot.m_blockingcollider.gameObject.layer = ValheimRaftPlugin.CustomRaftLayer;
    m_baseRoot.m_blockingcollider.transform.parent.gameObject.layer =
      ValheimRaftPlugin.CustomRaftLayer;
    m_baseRoot.ActivatePendingPiecesCoroutine();
    FirstTimeCreation();
  }

  public void UpdateVisual()
  {
    if (m_nview.m_zdo != null)
    {
      m_flags = (MBFlags)m_nview.m_zdo.GetInt(VehicleZdoVars.VehicleFlags, (int)m_flags);
      var newTransform = m_flags.HasFlag(MBFlags.HideMesh) ? Vector3.zero : Vector3.one;
      /*
       * hide with vector transform instead of active change to prevent NRE spam.
       * Previously these called gameobject SetActive(!m_flags.HasFlag(MBFlags.HideMesh));
       */
      transform.Find("ship/visual").gameObject.transform.localScale = newTransform;
      transform.Find("interactive").gameObject.transform.localScale = newTransform;
    }
  }

  public void OnDestroy()
  {
    if ((bool)m_baseRoot)
    {
      m_baseRoot.CleanUp();
      Destroy(m_baseRoot.gameObject);
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
    if (m_baseRoot.GetPieceCount() != 0)
    {
      return;
    }

    /*
     * @todo turn the original planks into a Prefab so boat floors can be larger
     */
    GameObject floor = ZNetScene.instance.GetPrefab("wood_floor");
    for (float x = -1f; x < 1.01f; x += 2f)
    {
      for (float z = -2f; z < 2.01f; z += 2f)
      {
        Vector3 pt = base.transform.TransformPoint(new Vector3(x,
          ValheimRaftPlugin.Instance.InitialRaftFloorHeight.Value, z));
        var obj = Instantiate(floor, pt, transform.rotation);
        ZNetView netview = obj.GetComponent<ZNetView>();
        m_baseRoot.AddNewPiece(netview);
      }
    }
  }

  public void Ascend()
  {
    if (IsAnchored)
    {
      SetAnchor(state: false);
    }

    if (!ValheimRaftPlugin.Instance.AllowFlight.Value)
    {
      m_targetHeight = 0f;
    }
    else
    {
      if (!m_baseRoot || !m_baseRoot.m_floatcollider)
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(m_baseRoot.m_floatcollider.transform.position.y + 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
    }

    m_nview.m_zdo.Set("MBTargetHeight", m_targetHeight);
  }

  public void Descent()
  {
    if (IsAnchored)
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
      if (!m_baseRoot || !m_baseRoot.m_floatcollider)
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(m_baseRoot.m_floatcollider.transform.position.y - 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
      if (m_baseRoot.m_floatcollider.transform.position.y - 1f <= ZoneSystem.instance.m_waterLevel)
      {
        m_targetHeight = 0f;
      }
    }

    m_nview.m_zdo.Set("MBTargetHeight", m_targetHeight);
  }


  public void UpdateStats(bool flight)
  {
    if (!m_rigidbody || !m_baseRoot || m_baseRoot.m_statsOverride)
    {
      return;
    }

    m_rigidbody.mass = m_baseRoot.TotalMass;
    m_rigidbody.angularDrag = (flight ? 1f : 0f);
    m_rigidbody.drag = (flight ? 1f : 0f);

    if ((bool)m_ship)
    {
      m_ship.m_angularDamping = (flight ? 5f : 0.8f);
      m_ship.m_backwardForce = 1f;
      m_ship.m_damping = (flight ? 5f : 0.35f);
      m_ship.m_dampingSideway = (flight ? 3f : 0.3f);
      m_ship.m_force = 3f;
      m_ship.m_forceDistance = 5f;
      m_ship.m_sailForceFactor = (flight ? 0.2f : 0.05f);
      m_ship.m_stearForce = (flight ? 0.2f : 1f);
      m_ship.m_stearVelForceFactor = 1.3f;
      m_ship.m_waterImpactDamage = 0f;
      ImpactEffect impact = m_ship.GetComponent<ImpactEffect>();
      if ((bool)impact)
      {
        impact.m_interval = 0.1f;
        impact.m_minVelocity = 0.1f;
        impact.m_damages.m_damage = 100f;
      }
    }
  }

  public void SetAnchor(bool state)
  {
    m_nview.InvokeRPC("SetAnchor", state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    m_flags = (state ? (m_flags | MBFlags.IsAnchored) : (m_flags & ~MBFlags.IsAnchored));
    m_nview.m_zdo.Set(VehicleZdoVars.VehicleFlags, (int)m_flags);
  }

  internal void SetVisual(bool state)
  {
    m_nview.InvokeRPC("SetVisual", state);
  }

  public void RPC_SetVisual(long sender, bool state)
  {
    m_flags = (state ? (m_flags | MBFlags.HideMesh) : (m_flags & ~MBFlags.HideMesh));
    m_nview.m_zdo.Set(VehicleZdoVars.VehicleFlags, (int)m_flags);
    UpdateVisual();
  }
}