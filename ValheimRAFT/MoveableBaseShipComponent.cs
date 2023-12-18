// ValheimRAFT, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// ValheimRAFT.MoveableBaseShipComponent

using System;
using System.Linq;
using Jotunn;
using UnityEngine;
using ValheimRAFT.Util;
using Main = ValheimRAFT.Main;
using ValheimRAFT.MoveableBaseRootComponent;

public class MoveableBaseShipComponent : MonoBehaviour
{
  [Flags]
  public enum MBFlags
  {
    None = 0,
    IsAnchored = 1,
    HideMesh = 2
  }

  internal ValheimRAFT.MoveableBaseRootComponent.Delegate m_baseRootDelegate;

  internal Rigidbody m_rigidbody;

  internal Ship m_ship;

  internal ZNetView m_nview;

  internal GameObject m_baseRootObject;

  internal ZSyncTransform m_zsync;

  public float m_targetHeight;

  public float m_balanceForce = 0.03f;

  public float m_liftForce = 20f;

  public MBFlags m_flags;

  public void Awake()
  {
    Ship ship = GetComponent<Ship>();
    m_nview = GetComponent<ZNetView>();
    m_zsync = GetComponent<ZSyncTransform>();
    m_ship = GetComponent<Ship>();
    m_baseRootObject = new GameObject
    {
      name = "MoveableBase",
      layer = 0
    };
    m_baseRootDelegate =
      m_baseRootObject.AddComponent<ValheimRAFT.MoveableBaseRootComponent.Delegate>();
    m_nview.Register("SetAnchor",
      delegate(long sender, bool state) { RPC_SetAnchor(sender, state); });
    m_nview.Register("SetVisual",
      delegate(long sender, bool state) { RPC_SetVisual(sender, state); });


    m_rigidbody = GetComponent<Rigidbody>();
    m_rigidbody.mass = 1000f;

    /*
     * @todo find out if the server guard is needed
     */
    // if (ZNet.instance.IsServer())
    // {
    m_baseRootDelegate.Instance.InitializeShipComponent(this, m_nview, ship, m_rigidbody);
    // }

    m_baseRootObject.transform.SetParent(null);
    m_baseRootObject.transform.position = base.transform.position;
    m_baseRootObject.transform.rotation = base.transform.rotation;

    // This looks odd that the older ship is being transformed.
    // switching ship to m_ship
    m_ship.transform.Find("ship/visual/mast")?.gameObject.SetActive(value: false);
    m_ship.transform.Find("ship/colliders/log")?.gameObject.SetActive(value: false);
    m_ship.transform.Find("ship/colliders/log (1)")?.gameObject.SetActive(value: false);
    m_ship.transform.Find("ship/colliders/log (2)")?.gameObject.SetActive(value: false);
    m_ship.transform.Find("ship/colliders/log (3)")?.gameObject.SetActive(value: false);


    UpdateVisual();

    BoxCollider[] colliders = base.transform.GetComponentsInChildren<BoxCollider>();
    m_baseRootDelegate.Instance.InitializeShipColliders(colliders);
  }

  public void UpdateVisual()
  {
    if (m_nview.m_zdo != null)
    {
      m_flags = (MBFlags)m_nview.m_zdo.GetInt("MBFlags", (int)m_flags);
      base.transform.Find("ship/visual").gameObject.SetActive(!m_flags.HasFlag(MBFlags.HideMesh));
      base.transform.Find("interactive").gameObject.SetActive(!m_flags.HasFlag(MBFlags.HideMesh));
    }
  }

  public void OnDestroy()
  {
    if ((bool)m_baseRootDelegate)
    {
      m_baseRootDelegate.Instance.CleanUp();
      UnityEngine.Object.Destroy(m_baseRootDelegate.gameObject);
    }
  }

  // Moved to MoveableBaseRootComponent Server
  // private void FirstTimeCreation()
  // {
  //   if (m_baseRootDelegate.GetPieceCount() != 0)
  //   {
  //     return;
  //   }
  //
  //   GameObject floor = ZNetScene.instance.GetPrefab("wood_floor");
  //   for (float x = -1f; x < 1.01f; x += 2f)
  //   {
  //     for (float z = -2f; z < 2.01f; z += 2f)
  //     {
  //       Vector3 pt = base.transform.TransformPoint(new Vector3(x, 0.45f, z));
  //       GameObject obj = UnityEngine.Object.Instantiate(floor, pt, base.transform.rotation);
  //       ZNetView netview = obj.GetComponent<ZNetView>();
  //       m_baseRootDelegate.AddNewPiece(netview);
  //     }
  //   }
  // }

  internal void Accend()
  {
    if (m_flags.HasFlag(MBFlags.IsAnchored))
    {
      SetAnchor(state: false);
    }

    if (!global::ValheimRAFT.Main.Instance.AllowFlight.Value)
    {
      m_targetHeight = 0f;
    }
    else
    {
      if (!m_baseRootDelegate || !m_baseRootDelegate.Instance.GetFloatCollider())
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(
        m_baseRootDelegate.Instance.GetFloatCollider().transform.position.y + 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
    }

    m_nview.m_zdo.Set("MBTargetHeight", m_targetHeight);
  }

  /**
   * @todo this may belong in the BaseRootComponent
   */
  internal void Descent()
  {
    if (m_flags.HasFlag(MBFlags.IsAnchored))
    {
      SetAnchor(state: false);
    }

    float oldTargetHeight = m_targetHeight;
    if (!Main.Instance.AllowFlight.Value)
    {
      m_targetHeight = 0f;
    }
    else
    {
      if (!m_baseRootDelegate || !m_baseRootDelegate.Instance.GetFloatCollider())
      {
        return;
      }

      m_targetHeight = Mathf.Clamp(
        m_baseRootDelegate.Instance.GetFloatCollider().transform.position.y - 1f,
        ZoneSystem.instance.m_waterLevel, 200f);
      if (m_baseRootDelegate.Instance.GetFloatCollider().transform.position.y - 1f <=
          ZoneSystem.instance.m_waterLevel)
      {
        m_targetHeight = 0f;
      }
    }

    m_nview.GetZDO().Set("MBTargetHeight", m_targetHeight);
  }

  internal void UpdateStats(bool flight)
  {
    if (!m_rigidbody || !m_baseRootDelegate ||
        (ZNet.instance.IsServer() && m_baseRootDelegate.Instance.GetStatsOverride()))
    {
      return;
    }

    m_rigidbody.mass = 3000f;
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

  internal void SetAnchor(bool state)
  {
    m_nview.InvokeRPC("SetAnchor", state);
  }

  public void RPC_SetAnchor(long sender, bool state)
  {
    ZLog.Log("RPC_SET_ANCHOR called");
    m_flags = (state ? (m_flags | MBFlags.IsAnchored) : (m_flags & ~MBFlags.IsAnchored));
    m_nview.m_zdo.Set("MBFlags", (int)m_flags);
  }

  internal void SetVisual(bool state)
  {
    m_nview.InvokeRPC("SetVisual", state);
  }

  public void RPC_SetVisual(long sender, bool state)
  {
    m_flags = (state ? (m_flags | MBFlags.HideMesh) : (m_flags & ~MBFlags.HideMesh));
    m_nview.m_zdo.Set("MBFlags", (int)m_flags);
    UpdateVisual();
  }
}