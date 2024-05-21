using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Prefabs;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Vehicles;

/*
 * Mostly vanilla Valheim However this is safe from other mods overriding valheim ships directly
 */
public class ValheimBaseGameShip : MonoBehaviour
{
  internal float m_sendRudderTime;

  [Header("Objects")] public GameObject m_sailObject;

  public GameObject m_mastObject;

  public GameObject m_rudderObject;

  public VehicleMovementController m_shipControlls;

  public Transform? m_controlGuiPos;

  public BoxCollider m_floatcollider;

  // base game sets default of 1.5f
  public float m_waterLevelOffset = 1.5f;

  public float m_forceDistance = 1f;

  public float m_force = 0.5f;

  public float m_damping = 0.05f;

  public float m_dampingSideway = 0.05f;

  public float m_dampingForward = 0.01f;

  public float m_angularDamping = 0.01f;

  public float m_disableLevel = -0.5f;

  public float m_sailForceOffset;

  public float m_sailForceFactor = 0.1f;

  public float m_stearForceOffset = -10f;

  public float m_stearForce = 0.5f;

  public float m_stearVelForceFactor = 0.1f;

  public float m_backwardForce = 50f;

  public float m_rudderRotationMax = 30f;

  public float m_minWaterImpactForce = 2.5f;

  public float m_minWaterImpactInterval = 2f;

  public float m_waterImpactDamage = 10f;

  public float m_upsideDownDmgInterval = 1f;

  public float m_upsideDownDmg = 20f;

  public EffectList m_waterImpactEffect = new EffectList();

  internal bool m_sailWasInPosition;

  internal Vector3 m_windChangeVelocity = Vector3.zero;

  internal Vector3 m_sailForce = Vector3.zero;

  internal List<Player> m_players = [];

  internal WaterVolume m_previousCenter;

  internal WaterVolume m_previousLeft;

  internal WaterVolume m_previousRight;

  internal WaterVolume m_previousForward;

  internal WaterVolume m_previousBack;

  internal static readonly List<ValheimBaseGameShip> s_currentShips = new();

  public Rigidbody m_body;

  internal ZNetView m_nview;

  internal Cloth m_sailCloth;

  internal float m_lastDepth = -9999f;

  internal float m_lastWaterImpactTime;

  internal float m_upsideDownDmgTimer;

  internal float m_rudderPaddleTimer;

  public static List<ValheimBaseGameShip> Instances { get; } = new();

  public virtual void OnEnable()
  {
    Instances.Add(this);
  }

  public virtual void OnDisable()
  {
    Instances.Remove(this);
  }


  internal void Awake()
  {
    m_nview = GetComponent<ZNetView>();

    if (!m_nview)
    {
      Logger.LogError(
        "ValheimBaseShip initialized without NetView, or netview is not available yet (ghost mode?)");
    }

    var collider = transform.Find(PrefabNames.WaterVehicleFloatCollider);

    if (collider != null)
    {
      collider.SetParent(transform);
      var boxColliders = collider.GetComponentsInChildren<BoxCollider>();

      var floatBoxCollider =
        boxColliders?.FirstOrDefault((k) =>
          k.gameObject.name == PrefabNames.WaterVehicleFloatCollider);
      if (floatBoxCollider != null)
      {
        m_floatcollider = floatBoxCollider;
      }
    }


    if (!m_floatcollider)
    {
      Logger.LogError("No float collider exists for ship, this is a prefab setup issue.");
    }

    var wnt = GetComponent<WearNTear>();
    if ((bool)wnt)
    {
      wnt.m_onDestroyed =
        (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
    }

    if (!(bool)m_body)
    {
      m_body = GetComponent<Rigidbody>();
      Logger.LogError("No rigidbody detected, ship must have a Rigidbody to work");
    }

    m_body.mass = 2000f;
    m_body.useGravity = true;
    m_body.maxDepenetrationVelocity = 2f;

    if (m_nview?.GetZDO() == null)
    {
      enabled = false;
    }


    Heightmap.ForceGenerateAll();

    m_sailCloth = m_sailObject.GetComponentInChildren<Cloth>();
  }

  /**
   * TODO this could be set to false for the ship as an override to allow the ship to never un-render
   */
  public bool CanBeRemoved()
  {
    return m_players.Count == 0;
  }

  internal void Start()
  {
    InvokeRepeating("UpdateOwner", 2f, 2f);
  }

  internal void PrintStats()
  {
    if (m_players.Count != 0)
    {
      Logger.LogDebug("Vel:" + m_body.velocity.magnitude.ToString("0.0"));
    }
  }

  internal static float GetUpwardsForce(float targetY, float currentY, float maxForce)
  {
    var dist = targetY - currentY;
    if (dist == 0f) return 0f;
    var force = 1f / (25f / (dist * dist));
    force *= dist > 0f ? maxForce : 0f - maxForce;
    return Mathf.Clamp(force, 0f - maxForce, maxForce);
  }

  public void UpdateUpsideDmg(float dt)
  {
    if (base.transform.up.y >= 0f)
    {
      return;
    }

    m_upsideDownDmgTimer += dt;
    if (!(m_upsideDownDmgTimer <= m_upsideDownDmgInterval))
    {
      m_upsideDownDmgTimer = 0f;
      IDestructible component = GetComponent<IDestructible>();
      if (component != null)
      {
        HitData hitData = new HitData();
        hitData.m_damage.m_blunt = m_upsideDownDmg;
        hitData.m_point = base.transform.position;
        hitData.m_dir = Vector3.up;
        component.Damage(hitData);
      }
    }
  }

  public Vector3 GetSailForce(float sailSize, float dt)
  {
    Vector3 windDir = EnvMan.instance.GetWindDir();
    float windIntensity = EnvMan.instance.GetWindIntensity();
    float num = Mathf.Lerp(0.25f, 1f, windIntensity);
    float windAngleFactor = GetWindAngleFactor();
    windAngleFactor *= num;
    Vector3 target = Vector3.Normalize(windDir + base.transform.forward) * windAngleFactor *
                     m_sailForceFactor * sailSize;
    m_sailForce = Vector3.SmoothDamp(m_sailForce, target, ref m_windChangeVelocity, 1f, 99f);
    return m_sailForce;
  }

  public float GetWindAngleFactor()
  {
    float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -base.transform.forward);
    float num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
    float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
    return num2 * num3;
  }

  public void UpdateWaterForce(float depth, float dt)
  {
    if (m_lastDepth == -9999f)
    {
      m_lastDepth = depth;
      return;
    }

    float num = depth - m_lastDepth;
    m_lastDepth = depth;
    float num2 = num / dt;
    if (num2 > 0f || !(Mathf.Abs(num2) > m_minWaterImpactForce) ||
        !(Time.time - m_lastWaterImpactTime > m_minWaterImpactInterval))
    {
      return;
    }

    m_lastWaterImpactTime = Time.time;
    m_waterImpactEffect.Create(base.transform.position, base.transform.rotation);
    if (m_players.Count > 0)
    {
      IDestructible component = GetComponent<IDestructible>();
      if (component != null)
      {
        HitData hitData = new HitData();
        hitData.m_damage.m_blunt = m_waterImpactDamage;
        hitData.m_point = base.transform.position;
        hitData.m_dir = Vector3.up;
        component.Damage(hitData);
      }
    }
  }

  /// <summary>
  /// This is the base game ship EdgeForce. Needs more information about what it does
  /// </summary>
  /// Todo figure out what this does
  /// <param name="dt"></param>
  public void ApplyEdgeForce(float dt)
  {
    var magnitude = base.transform.position.magnitude;
    var num = 10420f;

    if (!(magnitude > num)) return;

    var vector = Vector3.Normalize(base.transform.position);
    var num2 = Utils.LerpStep(num, 10500f, magnitude) * 8f;
    var vector2 = vector * num2;

    m_body.AddForce(vector2 * dt, ForceMode.VelocityChange);
  }

  internal void FixTilt()
  {
    float num = Mathf.Asin(base.transform.right.y);
    float num2 = Mathf.Asin(base.transform.forward.y);
    if (Mathf.Abs(num) > (float)Math.PI / 6f)
    {
      if (num > 0f)
      {
        base.transform.RotateAround(base.transform.position, base.transform.forward,
          (0f - Time.fixedDeltaTime) * 20f);
      }
      else
      {
        base.transform.RotateAround(base.transform.position, base.transform.forward,
          Time.fixedDeltaTime * 20f);
      }
    }

    if (Mathf.Abs(num2) > (float)Math.PI / 6f)
    {
      if (num2 > 0f)
      {
        base.transform.RotateAround(base.transform.position, base.transform.right,
          (0f - Time.fixedDeltaTime) * 20f);
      }
      else
      {
        base.transform.RotateAround(base.transform.position, base.transform.right,
          Time.fixedDeltaTime * 20f);
      }
    }
  }

  internal void UpdateOwner()
  {
    if (m_nview.IsValid() && m_nview.IsOwner() && (bool)Player.m_localPlayer &&
        m_players.Count > 0 && !IsPlayerInBoat(Player.m_localPlayer))
    {
      long owner = m_players[0].GetOwner();
      m_nview.GetZDO().SetOwner(owner);
      Logger.LogDebug("Changing ship owner to " + owner);
    }
  }

  public void OnTriggerEnter(Collider collider)
  {
    var component = collider.GetComponent<Player>();
    if ((bool)component)
    {
      if (m_players.Contains(component))
      {
        return;
      }

      m_players.Add(component);
      Logger.LogDebug("Player onboard, total onboard " + m_players.Count);
      if (component == Player.m_localPlayer)
      {
        s_currentShips.Add(this);
      }
    }

    var component2 = collider.GetComponent<Character>();
    if ((bool)component2)
    {
      component2.InNumShipVolumes++;
    }
  }

  public void OnTriggerExit(Collider collider)
  {
    Player component = collider.GetComponent<Player>();
    if ((bool)component)
    {
      m_players.Remove(component);
      Logger.LogDebug("Player over board, players left " + m_players.Count);
      if (component == Player.m_localPlayer)
      {
        s_currentShips.Remove(this);
      }
    }

    Character component2 = collider.GetComponent<Character>();
    if ((bool)component2)
    {
      component2.InNumShipVolumes--;
    }
  }

  public bool IsPlayerInBoat(ZDOID zdoid)
  {
    foreach (Player player in m_players)
    {
      if (player.GetZDOID() == zdoid)
      {
        return true;
      }
    }

    return false;
  }

  public bool IsPlayerInBoat(Player player)
  {
    var currentPlayerOnBoat = m_players.Contains(player);
    return currentPlayerOnBoat;
  }

  public bool IsPlayerInBoat(long playerID)
  {
    foreach (Player player in m_players)
    {
      if (player.GetPlayerID() == playerID)
      {
        return true;
      }
    }

    return false;
  }

  public bool HasPlayerOnboard()
  {
    return m_players.Count > 0;
  }

  public void OnDestroyed()
  {
    if (m_nview.IsValid() && m_nview.IsOwner())
    {
      Gogan.LogEvent("Game", "ShipDestroyed", base.gameObject.name, 0L);
    }

    s_currentShips.Remove(this);
  }

  public bool IsWindControllActive()
  {
    foreach (Player player in m_players)
    {
      if (player.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
      {
        return true;
      }
    }

    return false;
  }

  public static ValheimBaseGameShip GetLocalShip()
  {
    if (s_currentShips.Count != 0)
    {
      return s_currentShips[s_currentShips.Count - 1];
    }

    return null;
  }

  public bool IsOwner()
  {
    if (!m_nview) return false;
    if (m_nview.IsValid())
    {
      return m_nview.IsOwner();
    }

    return false;
  }

  public float GetSpeed()
  {
    return Vector3.Dot(m_body.velocity, base.transform.forward);
  }

  public float GetShipYawAngle()
  {
    Camera mainCamera = Utils.GetMainCamera();
    if (mainCamera == null)
    {
      return 0f;
    }

    return 0f -
           Utils.YawFromDirection(
             mainCamera.transform.InverseTransformDirection(base.transform.forward));
  }
}