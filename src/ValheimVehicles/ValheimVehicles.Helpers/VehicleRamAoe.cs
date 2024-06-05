using System;
using System.Security.Policy;
using UnityEngine;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : Aoe
{
  public HitData.DamageTypes baseDamage;
  public float velocityMultiplier = 0;
  public float massMultiplier = 3;
  public float velocityThreshold = 0.5f;
  public VehicleShip? vehicle;
  public static bool MultiplicativeCollisionVelocity = true;
  public const float MaxVelocityMultiplier = 20f;
  public bool isReadyForCollisions = false;


  private const float minimumHitInterval = 0.5f;

  /// <summary>
  /// @todo make this a Config JSON or Value in Vehicles config 
  /// </summary>
  private const float hitInterval = 2.0f;

  private const float hitRadius = 3;

  public void Awake()
  {
    m_blockable = false;
    m_dodgeable = false;
    m_hitTerrain = true;
    m_hitProps = true;
    m_hitCharacters = true;
    m_hitFriendly = true;
    m_hitEnemy = true;
    m_hitParent = false;
    m_hitInterval = Mathf.Max(minimumHitInterval, hitInterval);

    // todo need to tweak this
    m_damageSelf = 10;
    m_toolTier = 100;
    m_attackForce = 5;
    m_radius = hitRadius;
    m_useTriggers = true;
    m_triggerEnterOnly = true;
    m_useCollider = null;
    m_useAttackSettings = true;
    m_ttl = 0;
    m_canRaiseSkill = true;
    m_skill = Skills.SkillType.None;
    m_backstabBonus = 1;

    base.Awake();
  }

  public new void CustomFixedUpdate(float fixedDeltaTime)
  {
    if ((UnityEngine.Object)m_nview != (UnityEngine.Object)null &&
        !m_nview.IsOwner())
      return;
    if (m_initRun && !m_useTriggers && !m_hitAfterTtl &&
        (double)m_activationTimer <= 0.0)
    {
      m_initRun = false;
      if ((double)m_hitInterval <= 0.0)
        Initiate();
    }

    if ((UnityEngine.Object)m_owner != (UnityEngine.Object)null &&
        m_attachToCaster)
    {
      transform.position =
        m_owner.transform.TransformPoint(m_offset);
      transform.rotation = m_owner.transform.rotation * m_localRot;
    }

    if ((double)m_activationTimer > 0.0)
      return;
    if ((double)m_hitInterval > 0.0 && !m_useTriggers)
    {
      m_hitTimer -= fixedDeltaTime;
      if ((double)m_hitTimer <= 0.0)
      {
        m_hitTimer = m_hitInterval;
        Initiate();
      }
    }

    if ((double)m_chainStartChance > 0.0 && (double)m_chainDelay >= 0.0)
    {
      m_chainDelay -= fixedDeltaTime;
      if ((double)m_chainDelay <= 0.0 &&
          (double)UnityEngine.Random.value < (double)m_chainStartChance)
      {
        Vector3 position1 = transform.position;
        FindHits();
        SortHits();
        int num1 = UnityEngine.Random.Range(m_chainMinTargets,
          m_chainMaxTargets + 1);
        foreach (Collider hit in Aoe.s_hitList)
        {
          if ((double)UnityEngine.Random.value < (double)m_chainChancePerTarget)
          {
            Vector3 position2 = hit.gameObject.transform.position;
            bool flag = false;
            for (int index = 0; index < Aoe.s_chainObjs.Count; ++index)
            {
              if ((bool)(UnityEngine.Object)Aoe.s_chainObjs[index])
              {
                if ((double)Vector3.Distance(Aoe.s_chainObjs[index].transform.position, position2) <
                    0.10000000149011612)
                {
                  flag = true;
                  break;
                }
              }
              else
                Aoe.s_chainObjs.RemoveAt(index);
            }

            if (!flag)
            {
              GameObject gameObject1 =
                UnityEngine.Object.Instantiate<GameObject>(m_chainObj, position2,
                  hit.gameObject.transform.rotation);
              Aoe.s_chainObjs.Add(gameObject1);
              IProjectile componentInChildren = gameObject1.GetComponentInChildren<IProjectile>();
              if (componentInChildren != null)
              {
                componentInChildren.Setup(m_owner, position1.DirTo(position2), -1f,
                  m_hitData, m_itemData, m_ammo);
                if (componentInChildren is Aoe aoe)
                  aoe.m_chainChance =
                    m_chainChance * m_chainStartChanceFalloff;
              }

              --num1;
              float num2 = Vector3.Distance(position2, transform.position);
              foreach (GameObject gameObject2 in m_chainEffects.Create(
                         position1 + Vector3.up,
                         Quaternion.LookRotation(position1.DirTo(position2 + Vector3.up))))
                gameObject2.transform.localScale = Vector3.one * num2;
            }
          }

          if (num1 <= 0)
            break;
        }
      }
    }

    if ((double)m_ttl <= 0.0)
      return;
    m_ttl -= fixedDeltaTime;
    if ((double)m_ttl > 0.0)
      return;
    if (m_hitAfterTtl)
      Initiate();
    if (!(bool)(UnityEngine.Object)ZNetScene.instance)
      return;
    ZNetScene.instance.Destroy(gameObject);
    return;
  }

  public void UpdateReadyForCollisions()
  {
    CancelInvoke(nameof(UpdateReadyForCollisions));
    if (!m_nview)
    {
      isReadyForCollisions = false;
      Invoke(nameof(UpdateReadyForCollisions), 1);
      return;
    }

    var isVehicleChild = m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentIdHash);
    if (isVehicleChild == 0)
    {
      isReadyForCollisions = true;
      return;
    }

    // Must be within the BaseVehicleController otherwise this AOE could attempt to damage items within the raft ball
    var isChildOfBaseVehicle = transform.root.GetComponent<BaseVehicleController>();
    if (!(bool)isChildOfBaseVehicle)
    {
      isReadyForCollisions = false;
      return;
    }

    isReadyForCollisions = true;
  }

  public void Start()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
  }

  public override void OnEnable()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
    base.OnEnable();
  }

  public void UpdateDamageFromVelocityCollider(Collider collider)
  {
    if (!collider) return;
    // reset damage to base damage if one of these is not available, will still recalculate later
    if (!vehicle?.m_body || !collider.attachedRigidbody)
    {
      m_damage = baseDamage;
    }

    // early exit if both are not valid
    if (!vehicle?.m_body && !collider.attachedRigidbody) return;

    // Velocity will significantly increase if the object is moving towards the other object IE collision
    float relativeVelocity;
    if (!vehicle?.m_body)
    {
      relativeVelocity = collider.attachedRigidbody.velocity.magnitude;
    }
    else
    {
      relativeVelocity =
        Vector3.Magnitude(collider?.attachedRigidbody?.velocity ??
                          Vector3.zero - vehicle?.m_body?.velocity ??
                          Vector3.zero);
    }

    UpdateDamageFromVelocity(relativeVelocity);
  }

  public void UpdateDamageFromVelocity(float relativeVelocityMagnitude)
  {
    var multiplier = Mathf.Min(relativeVelocityMagnitude * 0.5f, MaxVelocityMultiplier);

    if (Mathf.Approximately(multiplier, 0))
    {
      multiplier = 0;
    }

    m_damage = new HitData.DamageTypes()
    {
      m_damage = baseDamage.m_damage * multiplier,
      m_blunt = baseDamage.m_blunt * multiplier,
      m_slash = baseDamage.m_slash * multiplier,
      m_pierce = baseDamage.m_pierce * multiplier,
      m_chop = baseDamage.m_chop * multiplier,
      m_pickaxe = baseDamage.m_pickaxe * multiplier,
      m_fire = baseDamage.m_fire * multiplier,
      m_frost = baseDamage.m_frost * multiplier,
      m_lightning = baseDamage.m_lightning * multiplier,
      m_poison = baseDamage.m_poison * multiplier,
      m_spirit = baseDamage.m_spirit * multiplier,
    };
  }

  private bool ShouldIgnore(Collider collider)
  {
    if (!collider) return false;
    if (!collider.transform.root.name.StartsWith(PrefabNames.PiecesContainer) &&
        collider.transform.root != transform.root) return false;

    var childColliders = GetComponentsInChildren<Collider>();
    foreach (var childCollider in childColliders)
    {
      Physics.IgnoreCollision(childCollider, collider, true);
    }

    return true;
  }

  public new void OnCollisionEnter(Collision collision)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collision.collider)) return;
    UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity));
    base.OnCollisionEnter(collision);
  }

  public new void OnCollisionStay(Collision collision)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collision.collider)) return;
    UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity));
    base.OnCollisionStay(collision);
  }

  public new void OnTriggerEnter(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    UpdateDamageFromVelocityCollider(collider);
    base.OnTriggerEnter(collider);
  }

  public new void OnTriggerStay(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    UpdateDamageFromVelocityCollider(collider);
    base.OnTriggerStay(collider);
  }

  public void SetBaseDamage(HitData.DamageTypes hitData)
  {
    baseDamage = hitData;
  }
}