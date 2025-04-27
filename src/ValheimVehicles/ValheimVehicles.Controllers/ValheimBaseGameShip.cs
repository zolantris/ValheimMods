using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles.Components;
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

  public Transform? m_controlGuiPos;

  public BoxCollider m_floatcollider;

  // base game sets default of 1.5f
  public float m_waterLevelOffset = 1.5f;

  public float m_forceDistance = 5f;

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

  public EffectList m_waterImpactEffect = new();

  internal bool m_sailWasInPosition;

  internal Vector3 m_windChangeVelocity = Vector3.zero;

  internal Vector3 m_sailForce = Vector3.zero;

  /// <summary>
  /// todo This field must be migrated to VehicleOnboardController. Does not make sense to decouple the logic from OnboardController and have to sync two interfaces, especially if the character api is better for most things and has been extended.
  /// </summary>
  // public List<Player> m_players = [];
  internal WaterVolume m_previousCenter;

  internal WaterVolume m_previousLeft;

  internal WaterVolume m_previousRight;

  internal WaterVolume m_previousForward;

  internal WaterVolume m_previousBack;

  internal static readonly List<ValheimBaseGameShip> s_currentShips = new();

  public Rigidbody m_body { get; set; }

  internal ZNetView m_nview;

  internal Cloth m_sailCloth;

  public Cloth? GetSailCloth()
  {
    if (!m_sailObject) return null;
    if (!m_sailCloth) m_sailCloth = m_sailObject.GetComponent<Cloth>();

    if (!m_sailCloth) m_sailCloth = m_sailObject.AddComponent<Cloth>();

    return m_sailCloth;
  }

  internal float m_lastDepth = -9999f;

  internal float m_lastWaterImpactTime;

  internal float m_upsideDownDmgTimer;

  internal float m_rudderPaddleTimer;


  internal virtual void Awake()
  {
    m_nview = GetComponent<ZNetView>();

    if (!m_nview)
      Logger.LogError(
        "ValheimBaseShip initialized without NetView, or netview is not available yet (ghost mode?)");

    var wnt = GetComponent<WearNTear>();
    if ((bool)wnt)
      wnt.m_onDestroyed =
        (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));

    m_body = GetComponent<Rigidbody>();
    if (!m_body)
      Logger.LogError(
        "No rigidbody detected, ship must have a Rigidbody to work");

    m_body.mass = 1000f;
    m_body.useGravity = true;
    m_body.maxDepenetrationVelocity = 2f;

    if (m_nview?.GetZDO() == null) enabled = false;


    Heightmap.ForceGenerateAll();

    // m_sailCloth = m_sailObject.GetComponent<Cloth>();
  }

  internal static float GetUpwardsForce(float targetY, float currentY,
    float maxForce)
  {
    var dist = targetY - currentY;
    if (dist == 0f) return 0f;
    var force = 1f / (25f / (dist * dist));
    force *= dist > 0f ? maxForce : 0f - maxForce;
    return Mathf.Clamp(force, 0f - maxForce, maxForce);
  }

  public void UpdateUpsideDmg(float dt)
  {
    if (transform.up.y >= 0f) return;

    m_upsideDownDmgTimer += dt;
    if (!(m_upsideDownDmgTimer <= m_upsideDownDmgInterval))
    {
      m_upsideDownDmgTimer = 0f;
      var component = GetComponent<IDestructible>();
      if (component != null)
      {
        var hitData = new HitData();
        hitData.m_damage.m_blunt = m_upsideDownDmg;
        hitData.m_point = transform.position;
        hitData.m_dir = Vector3.up;
        component.Damage(hitData);
      }
    }
  }

  public Vector3 GetSailForce(float sailSize, float dt)
  {
    var windDir = EnvMan.instance.GetWindDir();
    var windIntensity = EnvMan.instance.GetWindIntensity();
    var num = Mathf.Lerp(0.25f, 1f, windIntensity);
    var windAngleFactor = GetWindAngleFactor();
    windAngleFactor *= num;
    var target = Vector3.Normalize(windDir + transform.forward) *
                 windAngleFactor *
                 m_sailForceFactor * sailSize;
    m_sailForce = Vector3.SmoothDamp(m_sailForce, target,
      ref m_windChangeVelocity, 1f, 99f);
    return m_sailForce;
  }

  public float GetWindAngleFactor()
  {
    var num =
      Vector3.Dot(EnvMan.instance.GetWindDir(), -transform.forward);
    var num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
    var num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
    return num2 * num3;
  }

  /// <summary>
  /// This is the base game ship EdgeForce. Needs more information about what it does
  /// </summary>
  /// Todo figure out what this does
  /// <param name="dt"></param>
  public void ApplyEdgeForce(float dt)
  {
    var magnitude = transform.position.magnitude;
    var num = 10420f;

    if (!(magnitude > num)) return;

    var vector = Vector3.Normalize(transform.position);
    var num2 = Utils.LerpStep(num, 10500f, magnitude) * 8f;
    var vector2 = vector * num2;

    m_body.AddForce(vector2 * dt, PhysicsConfig.floatationVelocityMode.Value);
  }

  internal void FixTilt()
  {
    var num = Mathf.Asin(transform.right.y);
    var num2 = Mathf.Asin(transform.forward.y);
    if (Mathf.Abs(num) > (float)Math.PI / 6f)
    {
      if (num > 0f)
        transform.RotateAround(transform.position,
          transform.forward,
          (0f - Time.fixedDeltaTime) * 20f);
      else
        transform.RotateAround(transform.position,
          transform.forward,
          Time.fixedDeltaTime * 20f);
    }

    if (Mathf.Abs(num2) > (float)Math.PI / 6f)
    {
      if (num2 > 0f)
        transform.RotateAround(transform.position,
          transform.right,
          (0f - Time.fixedDeltaTime) * 20f);
      else
        transform.RotateAround(transform.position,
          transform.right,
          Time.fixedDeltaTime * 20f);
    }
  }

  // internal void UpdateOwner()
  // {
  //   if (m_nview.IsValid() && m_nview.IsOwner() && (bool)Player.m_localPlayer &&
  //       m_players.Count > 0 && !IsPlayerInBoat(Player.m_localPlayer))
  //   {
  //     var owner = m_players[0].GetOwner();
  //     m_nview.GetZDO().SetOwner(owner);
  //     Logger.LogDebug("Changing ship owner to " + owner +
  //                     $", name: {m_players[0].GetPlayerName()}");
  //   }
  // }

  // public bool IsPlayerInBoat(ZDOID zdoid)
  // {
  //   foreach (var player in m_players)
  //     if (player.GetZDOID() == zdoid)
  //       return true;
  //
  //   return false;
  // }

  // public bool IsPlayerInBoat(Player player)
  // {
  //   var currentPlayerOnBoat = m_players.Contains(player);
  //   if (currentPlayerOnBoat) return true;
  //
  //   if (player.transform.root != null &&
  //       player.transform.root.name.Contains(PrefabNames
  //         .VehiclePiecesContainer))
  //     return true;
  //
  //   return WaterZoneUtils.IsOnboard(player);
  // }

  // public bool IsPlayerInBoat(long playerId)
  // {
  //   var playerFromId = Player.GetPlayer(playerId);
  //   return playerFromId != null && IsPlayerInBoat(playerFromId);
  // }

  // public bool HasPlayerOnboard()
  // {
  //   return m_players.Count > 0;
  // }

  public void OnDestroyed()
  {
    if (m_nview.IsValid() && m_nview.IsOwner())
      Gogan.LogEvent("Game", "ShipDestroyed", gameObject.name, 0L);

    s_currentShips.Remove(this);
  }

  public static ValheimBaseGameShip GetLocalShip()
  {
    if (s_currentShips.Count != 0)
      return s_currentShips[s_currentShips.Count - 1];

    return null;
  }

  public bool IsOwner()
  {
    if (!m_nview) return false;
    if (m_nview.IsValid()) return m_nview.IsOwner();

    return false;
  }

  public float GetSpeed()
  {
    return Vector3.Dot(m_body.velocity, transform.forward);
  }

  public float GetShipYawAngle()
  {
    var mainCamera = Utils.GetMainCamera();
    if (mainCamera == null) return 0f;

    return 0f -
           Utils.YawFromDirection(
             mainCamera.transform.InverseTransformDirection(transform
               .forward));
  }
}