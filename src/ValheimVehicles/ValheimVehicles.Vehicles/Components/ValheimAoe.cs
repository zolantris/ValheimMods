using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ValheimVehicles.Vehicles;

/// <summary>
/// ValheimVehicles Debuggable AOE from valheim base game. But might be modified to support vehicles better
/// </summary>
public class ValheimAoe : MonoBehaviour, IProjectile, IMonoUpdater
{
  public string m_name = "";

  [Header("Attack (overridden by item )")]
  public bool m_useAttackSettings = true;

  public HitData.DamageTypes m_damage;
  public bool m_scaleDamageByDistance;

  public AnimationCurve m_distanceScaleCurve =
    AnimationCurve.Linear(1f, 1f, 0.0f, 0.0f);

  public bool m_dodgeable;
  public bool m_blockable;
  public int m_toolTier;
  public float m_attackForce;
  public float m_backstabBonus = 4f;
  public string m_statusEffect = "";
  public string m_statusEffectIfBoss = "";
  public string m_statusEffectIfPlayer = "";
  public int m_statusEffectHash;
  public int m_statusEffectIfBossHash;
  public int m_statusEffectIfPlayerHash;
  [Header("Attack (other)")] public HitData.DamageTypes m_damagePerLevel;
  public bool m_attackForceForward;
  public GameObject m_spawnOnHitTerrain;
  public bool m_hitTerrainOnlyOnce;

  public FootStep.GroundMaterial m_spawnOnGroundType =
    FootStep.GroundMaterial.Everything;

  public float m_groundLavaValue = -1f;
  public float m_hitNoise;
  public bool m_placeOnGround;
  public bool m_randomRotation;
  public int m_maxTargetsFromCenter;
  [Header("Multi Spawn (Lava Bomb)")] public int m_multiSpawnMin;
  public int m_multiSpawnMax;
  public float m_multiSpawnDistanceMin;
  public float m_multiSpawnDistanceMax;
  public float m_multiSpawnScaleMin;
  public float m_multiSpawnScaleMax;
  public float m_multiSpawnSpringDelayMax;
  [Header("Chain Spawn")] public float m_chainStartChance;
  public float m_chainStartChanceFalloff = 0.8f;
  public float m_chainChancePerTarget;
  public GameObject m_chainObj;
  public float m_chainStartDelay;
  public int m_chainMinTargets;
  public int m_chainMaxTargets;
  public EffectList m_chainEffects = new();
  public float m_chainDelay;
  public float m_chainChance;
  [Header("Damage self")] public float m_damageSelf;
  [Header("Ignore targets")] public bool m_hitOwner;
  public bool m_hitParent = true;
  public bool m_hitSame;
  public bool m_hitFriendly = true;
  public bool m_hitEnemy = true;
  public bool m_hitCharacters = true;
  public bool m_hitProps = true;
  public bool m_hitTerrain;
  public bool m_ignorePVP;
  [Header("Launch Characters")] public bool m_launchCharacters;
  public Vector2 m_launchForceMinMax = Vector2.up;
  [Range(0.0f, 1f)] public float m_launchForceUpFactor = 0.5f;
  [Header("Other")] public Skills.SkillType m_skill;
  public bool m_canRaiseSkill = true;
  public bool m_useTriggers;
  public bool m_triggerEnterOnly;
  public BoxCollider m_useCollider;
  public float m_radius = 4f;

  [Tooltip("Wait this long before we start doing any damage")]
  public float m_activationDelay;

  public float m_ttl = 4f;

  [Tooltip("When set, ttl will be a random value between ttl and ttlMax")]
  public float m_ttlMax;

  public bool m_hitAfterTtl;
  public float m_hitInterval = 1f;
  public bool m_hitOnEnable;
  public bool m_attachToCaster;
  public EffectList m_hitEffects = new();
  public EffectList m_initiateEffect = new();
  public static Collider[] s_hits = new Collider[100];
  public static List<Collider> s_hitList = new();
  public static int s_hitListCount;
  public static List<GameObject> s_chainObjs = new();
  public ZNetView m_nview;
  public Character m_owner;
  public readonly List<GameObject> m_hitList = new();
  public float m_hitTimer;
  public float m_activationTimer;
  public Vector3 m_offset = Vector3.zero;
  public Quaternion m_localRot = Quaternion.identity;
  public int m_level;
  public int m_worldLevel = -1;
  public int m_rayMask;
  public bool m_gaveSkill;
  public bool m_hasHitTerrain;
  public bool m_initRun = true;
  public HitData m_hitData;
  public ItemDrop.ItemData m_itemData;
  public ItemDrop.ItemData m_ammo;
  public Rigidbody m_body;
  public static List<IMonoUpdater> Instances = new();

  public virtual void Awake()
  {
    m_nview = GetComponentInParent<ZNetView>();
    m_body = GetComponent<Rigidbody>();
    m_rayMask = 0;
    if (m_hitCharacters)
      m_rayMask |=
        LayerMask.GetMask("character", "character_net", "character_ghost");
    if (m_hitProps)
      m_rayMask |= LayerMask.GetMask("Default", "static_solid", "Default_small",
        "piece", "hitbox", "character_noenv", "vehicle");
    if (m_hitTerrain)
      m_rayMask |= LayerMask.GetMask("terrain");
    if (!string.IsNullOrEmpty(m_statusEffect))
      m_statusEffectHash = m_statusEffect.GetStableHashCode();
    if (!string.IsNullOrEmpty(m_statusEffectIfBoss))
      m_statusEffectIfBossHash = m_statusEffectIfBoss.GetStableHashCode();
    if (!string.IsNullOrEmpty(m_statusEffectIfPlayer))
      m_statusEffectIfPlayerHash = m_statusEffectIfPlayer.GetStableHashCode();
    m_activationTimer = m_activationDelay;
    if ((double)m_ttlMax > 0.0)
      m_ttl = UnityEngine.Random.Range(m_ttl, m_ttlMax);
    m_chainDelay = m_chainStartDelay;
    if ((double)m_chainChance != 0.0)
      return;
    m_chainChance = m_chainStartChance;
  }


  public virtual void OnEnable()
  {
    m_initRun = true;
    Instances.Add(this);
  }

  public virtual void OnDisable()
  {
    Instances.Remove(this);
  }

  public HitData.DamageTypes GetDamage()
  {
    return GetDamage(m_level);
  }

  public HitData.DamageTypes GetDamage(int itemQuality)
  {
    if (itemQuality <= 1)
      return m_damage;
    var damage = m_damage;
    var num = m_worldLevel >= 0 ? m_worldLevel : Game.m_worldLevel;
    if (num > 0)
      damage.IncreaseEqually(
        (float)(num * Game.instance.m_worldLevelGearBaseDamage), true);
    damage.Add(m_damagePerLevel, itemQuality - 1);
    return damage;
  }

  public string GetTooltipString(int itemQuality)
  {
    var stringBuilder = new StringBuilder(256);
    stringBuilder.Append("AOE");
    stringBuilder.Append(GetDamage(itemQuality).GetTooltipString());
    stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>",
      (object)m_attackForce);
    stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>",
      (object)m_backstabBonus);
    return stringBuilder.ToString();
  }

  public void CustomFixedUpdate(float fixedDeltaTime)
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
      transform.position = m_owner.transform.TransformPoint(m_offset);
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
      if ((double)m_chainDelay <= 0.0 && (double)UnityEngine.Random.value <
          (double)m_chainStartChance)
      {
        var position1 = transform.position;
        FindHits();
        SortHits();
        var num1 =
          UnityEngine.Random.Range(m_chainMinTargets, m_chainMaxTargets + 1);
        foreach (var hit in s_hitList)
        {
          if ((double)UnityEngine.Random.value < (double)m_chainChancePerTarget)
          {
            var position2 = hit.gameObject.transform.position;
            var flag = false;
            for (var index = 0; index < s_chainObjs.Count; ++index)
              if ((bool)(UnityEngine.Object)Aoe.s_chainObjs[index])
              {
                if ((double)Vector3.Distance(
                      s_chainObjs[index].transform.position, position2) <
                    0.10000000149011612)
                {
                  flag = true;
                  break;
                }
              }
              else
              {
                s_chainObjs.RemoveAt(index);
              }

            if (!flag)
            {
              var gameObject1 = Instantiate<GameObject>(m_chainObj, position2,
                hit.gameObject.transform.rotation);
              s_chainObjs.Add(gameObject1);
              var componentInChildren =
                gameObject1.GetComponentInChildren<IProjectile>();
              if (componentInChildren != null)
              {
                componentInChildren.Setup(m_owner, position1.DirTo(position2),
                  -1f, m_hitData, m_itemData, m_ammo);
                if (componentInChildren is Aoe aoe)
                  aoe.m_chainChance = m_chainChance * m_chainStartChanceFalloff;
              }

              --num1;
              var num2 = Vector3.Distance(position2, transform.position);
              foreach (var gameObject2 in m_chainEffects.Create(
                         position1 + Vector3.up,
                         Quaternion.LookRotation(
                           position1.DirTo(position2 + Vector3.up))))
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
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    if ((double)m_activationTimer > 0.0)
      m_activationTimer -= deltaTime;
    if ((double)m_hitInterval <= 0.0 || !m_useTriggers)
      return;
    m_hitTimer -= deltaTime;
    if ((double)m_hitTimer > 0.0)
      return;
    m_hitTimer = m_hitInterval;
    m_hitList.Clear();
  }

  public void CustomLateUpdate(float deltaTime)
  {
  }

  public void Initiate()
  {
    m_initiateEffect.Create(transform.position, Quaternion.identity);
    CheckHits();
  }

  public void CheckHits()
  {
    FindHits();
    if (m_maxTargetsFromCenter > 0)
    {
      SortHits();
      var targetsFromCenter = m_maxTargetsFromCenter;
      foreach (var hit in s_hitList)
      {
        if (OnHit(hit, hit.transform.position))
          --targetsFromCenter;
        if (targetsFromCenter <= 0)
          break;
      }
    }
    else
    {
      for (var index = 0; index < s_hitList.Count; ++index)
        OnHit(Aoe.s_hitList[index], s_hitList[index].transform.position);
    }
  }

  public void FindHits()
  {
    m_hitList.Clear();
    // var num = (UnityEngine.Object)m_useCollider != (UnityEngine.Object)null
    //   ? Physics.OverlapBoxNonAlloc(transform.position + m_useCollider.center,
    //     m_useCollider.size / 2f,ValheimAoe.s_hits, transform.rotation, m_rayMask)
    //   : Physics.OverlapSphereNonAlloc(transform.position, m_radius,ValheimAoe.s_hits,
    //     m_rayMask);

    // This must come from the point of impact, not the transform position of the rigidbody.
    var num = Physics.OverlapSphereNonAlloc(transform.position, m_radius,
      s_hits,
      m_rayMask);

    s_hitList.Clear();
    for (var index = 0; index < num; ++index)
    {
      var hit = s_hits[index];
      if (ShouldHit(hit))
        s_hitList.Add(hit);
    }
  }

  public bool ShouldHit(Collider collider)
  {
    var hitObject = Projectile.FindHitObject(collider);
    if ((bool)(UnityEngine.Object)hitObject)
    {
      var component = hitObject.GetComponent<Character>();
      if (component != null)
      {
        if ((UnityEngine.Object)m_nview == (UnityEngine.Object)null &&
            !component.IsOwner())
          return false;
        if ((UnityEngine.Object)m_owner != (UnityEngine.Object)null)
        {
          if ((!m_hitOwner && (UnityEngine.Object)component ==
                (UnityEngine.Object)m_owner) ||
              (!m_hitSame && component.m_name == m_owner.m_name))
            return false;
          var flag = BaseAI.IsEnemy(m_owner, component) ||
                     ((bool)(UnityEngine.Object)component.GetBaseAI() &&
                      component.GetBaseAI().IsAggravatable() &&
                      m_owner.IsPlayer());
          if ((!m_hitFriendly && !flag) || !m_hitEnemy & flag)
            return false;
        }

        if (!m_hitCharacters || (m_dodgeable && component.IsDodgeInvincible()))
          return false;
      }
    }

    return true;
  }

  public void SortHits()
  {
    s_hitList.Sort((Comparison<Collider>)((a, b) =>
      Vector3.Distance(a.transform.position, transform.position)
        .CompareTo(Vector3.Distance(b.transform.position,
          transform.position))));
  }

  public void Setup(
    Character owner,
    Vector3 velocity,
    float hitNoise,
    HitData hitData,
    ItemDrop.ItemData item,
    ItemDrop.ItemData ammo)
  {
    m_owner = owner;
    if (item != null)
    {
      m_level = item.m_quality;
      m_worldLevel = item.m_worldLevel;
      m_itemData = item;
    }

    if (m_attachToCaster &&
        (UnityEngine.Object)owner != (UnityEngine.Object)null)
    {
      m_offset = owner.transform.InverseTransformPoint(transform.position);
      m_localRot = Quaternion.Inverse(owner.transform.rotation) *
                   transform.rotation;
    }

    if (hitData != null && m_useAttackSettings)
    {
      m_damage = hitData.m_damage;
      m_blockable = hitData.m_blockable;
      m_dodgeable = hitData.m_dodgeable;
      m_attackForce = hitData.m_pushForce;
      m_backstabBonus = hitData.m_backstabBonus;
      if (m_statusEffectHash != hitData.m_statusEffectHash)
      {
        m_statusEffectHash = hitData.m_statusEffectHash;
        m_statusEffect = "<changed>";
      }

      m_toolTier = (int)hitData.m_toolTier;
      m_skill = hitData.m_skill;
    }

    m_ammo = ammo;
    m_hitData = hitData;
  }


  private void OnCollisionEnter(Collision collision)
  {
    OnCollisionEnterHandler(collision);
  }

  private void OnCollisionStay(Collision collision)
  {
    OnCollisionStayHandler(collision);
  }

  private void OnTriggerEnter(Collider collider)
  {
    OnTriggerEnterHandler(collider);
  }

  private void OnTriggerStay(Collider collider)
  {
    OnTriggerEnterHandler(collider);
  }

  // handlers for override methods.

  public virtual void OnCollisionStayHandler(Collision collision)
  {
    CauseTriggerDamage(collision.collider, false);
  }

  public virtual void OnCollisionEnterHandler(Collision collision)
  {
    CauseTriggerDamage(collision.collider, true);
  }

  public virtual void OnTriggerEnterHandler(Collider collider)
  {
    CauseTriggerDamage(collider, true);
  }

  public virtual void OnTriggerStayHandler(Collider collider)
  {
    CauseTriggerDamage(collider, false);
  }

  public Vector3 AverageContactPoint(Collision collision)
  {
    if (collision.contactCount <= 0)
    {
      var centerPoint = collision.collider.bounds.center;
      var closestPointToAoe = m_body.ClosestPointOnBounds(centerPoint);
      var colliderClosestPoint =
        collision.collider.ClosestPointOnBounds(closestPointToAoe);
      return m_body.ClosestPointOnBounds(colliderClosestPoint);
    }

    var averagePoint = Vector3.zero;

    foreach (var contact in collision.contacts) averagePoint += contact.point;

    averagePoint /= collision.contactCount;
    Debug.Log($"Average Collision Point: {averagePoint}");
    return averagePoint;
  }

  public void CauseCollisionTriggerDamage(Collision collision)
  {
    var collider = collision.collider;
    if (!ShouldHit(collider))
      return;
    OnHit(collider, AverageContactPoint(collision));
  }

  public void CauseTriggerDamage(Collider collider, bool onTriggerEnter)
  {
    if (m_triggerEnterOnly & onTriggerEnter || (double)m_activationTimer > 0.0)
      return;
    if (!m_useTriggers)
    {
      ZLog.LogWarning(
        (object)("AOE got OnTriggerStay but trigger damage is disabled in " +
                 gameObject.name));
    }
    else
    {
      if (!ShouldHit(collider))
        return;
      var centerPoint = collider.bounds.center;
      var closestPointToAoe = m_body.ClosestPointOnBounds(centerPoint);
      var colliderClosestPoint =
        collider.ClosestPointOnBounds(closestPointToAoe);
      OnHit(collider, colliderClosestPoint);
    }
  }

  public bool OnHit(Collider collider, Vector3 hitPoint)
  {
    var hitObject = Projectile.FindHitObject(collider);
    if (m_hitList.Contains(hitObject))
      return false;
    m_hitList.Add(hitObject);
    var multiplier1 = 1f;
    if ((bool)(UnityEngine.Object)m_owner && m_owner.IsPlayer() &&
        m_skill != Skills.SkillType.None)
      multiplier1 = m_owner.GetRandomSkillFactor(m_skill);
    var flag1 = false;
    var flag2 = false;
    var multiplier2 = 1f;
    if (m_scaleDamageByDistance)
      multiplier2 = m_distanceScaleCurve.Evaluate(Mathf.Clamp01(
        Vector3.Distance(hitObject.transform.position, transform.position) /
        m_radius));
    var component1 = hitObject.GetComponent<IDestructible>();
    if (component1 != null)
    {
      if (!m_hitParent)
      {
        if (!((UnityEngine.Object)gameObject.transform.parent !=
              (UnityEngine.Object)null) || !((UnityEngine.Object)hitObject ==
                                             (UnityEngine.Object)gameObject
                                               .transform.parent.gameObject))
        {
          var componentInParent =
            gameObject.GetComponentInParent<IDestructible>();
          if (componentInParent == null || componentInParent != component1)
            goto label_11;
        }

        return false;
      }

      label_11:
      var character = component1 as Character;
      if ((bool)(UnityEngine.Object)character)
      {
        if (m_launchCharacters)
        {
          var num =
            UnityEngine.Random.Range(m_launchForceMinMax.x,
              m_launchForceMinMax.y) * multiplier2;
          var a = hitPoint.DirTo(transform.position);
          if ((double)m_launchForceUpFactor > 0.0)
            a = Vector3.Slerp(a, Vector3.up, m_launchForceUpFactor);
          character.ForceJump(a.normalized * num);
        }

        flag2 = true;
      }
      else if (!m_hitProps)
      {
        return false;
      }

      var flag3 =
        (component1 is Destructible destructible &&
         (UnityEngine.Object)destructible.m_spawnWhenDestroyed !=
         (UnityEngine.Object)null) ||
        hitObject.GetComponent<MineRock5>() != null;
      var vector3 = m_attackForceForward
        ? transform.forward
        : (hitPoint - transform.position).normalized;
      var hit = new HitData();
      hit.m_hitCollider = collider;
      hit.m_damage = GetDamage();
      hit.m_pushForce = m_attackForce * multiplier1 * multiplier2;
      hit.m_backstabBonus = m_backstabBonus;
      hit.m_point = flag3 ? transform.position : hitPoint;
      hit.m_dir = vector3;
      hit.m_statusEffectHash = GetStatusEffect(character);
      var hitData = hit;
      var owner = m_owner;
      var num1 = owner != null ? (double)owner.GetSkillLevel(m_skill) : 0.0;
      hitData.m_skillLevel = (float)num1;
      hit.m_itemLevel = (short)m_level;
      hit.m_itemWorldLevel = m_worldLevel >= 0
        ? (byte)m_worldLevel
        : (byte)Game.m_worldLevel;
      hit.m_dodgeable = m_dodgeable;
      hit.m_blockable = m_blockable;
      hit.m_ranged = true;
      hit.m_ignorePVP =
        (UnityEngine.Object)m_owner == (UnityEngine.Object)character ||
        m_ignorePVP;
      hit.m_toolTier = (short)m_toolTier;
      hit.SetAttacker(m_owner);
      hit.m_damage.Modify(multiplier1);
      hit.m_damage.Modify(multiplier2);
      hit.m_hitType = hit.GetAttacker() is Player
        ? HitData.HitType.PlayerHit
        : HitData.HitType.EnemyHit;
      hit.m_radius = m_radius;

      if (hitObject.name.Contains("MineRock5"))
      {
        var minerock5 = hitObject.GetComponent<MineRock5>();
        if (minerock5 != null) minerock5.Damage(hit);
      }
      else
      {
        component1.Damage(hit);
      }

      if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
        Terminal.Log((object)("Damage AOE: hitting target" +
                              ((UnityEngine.Object)m_owner ==
                               (UnityEngine.Object)null
                                ? " without owner"
                                : " with owner: " + m_owner?.ToString())));
      if ((double)m_damageSelf > 0.0)
      {
        var componentInParent = GetComponentInParent<IDestructible>();
        if (componentInParent != null)
          componentInParent.Damage(new HitData()
          {
            m_damage =
            {
              m_damage = m_damageSelf
            },
            m_point = hitPoint,
            m_blockable = false,
            m_dodgeable = false,
            m_hitType = HitData.HitType.Self
          });
      }

      flag1 = true;
    }
    else
    {
      var component2 = hitObject.GetComponent<Heightmap>();
      if (component2 != null)
      {
        var groundMaterial1 = component2.GetGroundMaterial(Vector3.up,
          transform.position, m_groundLavaValue);
        var groundMaterial2 =
          component2.GetGroundMaterial(Vector3.up, transform.position);
        var flag4 = (double)m_groundLavaValue >= 0.0
          ? groundMaterial1
          : groundMaterial2;
        if ((bool)(UnityEngine.Object)m_spawnOnHitTerrain &&
            (m_spawnOnGroundType == FootStep.GroundMaterial.Everything ||
             m_spawnOnGroundType.HasFlag((Enum)flag4)) &&
            (!m_hitTerrainOnlyOnce || !m_hasHitTerrain))
        {
          m_hasHitTerrain = true;
          var num2 = m_multiSpawnMin == 0
            ? 1
            : UnityEngine.Random.Range(m_multiSpawnMin, m_multiSpawnMax);
          var position = transform.position;
          for (var index = 0; index < num2; ++index)
          {
            var gameObject = Attack.SpawnOnHitTerrain(position,
              m_spawnOnHitTerrain, m_owner, m_hitNoise, (ItemDrop.ItemData)null,
              (ItemDrop.ItemData)null, m_randomRotation);
            var num3 = num2 == 1 ? 0.0f : (float)index / (float)(num2 - 1);
            var num4 = UnityEngine.Random.Range(m_multiSpawnDistanceMin,
              m_multiSpawnDistanceMax);
            var insideUnitCircle = UnityEngine.Random.insideUnitCircle;
            position += new Vector3(insideUnitCircle.x * num4, 0.0f,
              insideUnitCircle.y * num4);
            if ((bool)(UnityEngine.Object)gameObject && index > 0)
              gameObject.transform.localScale = Utils.Vec3(
                (float)((1.0 - (double)num3) * ((double)m_multiSpawnScaleMax -
                                                (double)m_multiSpawnScaleMin)) +
                m_multiSpawnScaleMin);
            if ((double)m_multiSpawnSpringDelayMax > 0.0)
            {
              var componentInChildren =
                gameObject.GetComponentInChildren<ConditionalObject>();
              if (componentInChildren != null)
                componentInChildren.m_appearDelay =
                  num3 * m_multiSpawnSpringDelayMax;
            }

            if (m_placeOnGround)
              gameObject.transform.position = new Vector3(
                gameObject.transform.position.x,
                ZoneSystem.instance.GetGroundHeight(gameObject.transform
                  .position), gameObject.transform.position.z);
          }
        }

        flag1 = true;
      }
    }

    if (hitObject.GetComponent<MineRock5>() == null)
      m_hitEffects.Create(hitPoint, Quaternion.identity);
    if (((m_gaveSkill || !(bool)(UnityEngine.Object)m_owner ? 0 :
          m_skill != 0 ? 1 : 0) & (flag2 ? 1 : 0)) != 0 && m_canRaiseSkill)
    {
      m_owner.RaiseSkill(m_skill);
      m_gaveSkill = true;
    }

    return flag1;
  }

  public int GetStatusEffect(Character character)
  {
    if ((bool)(UnityEngine.Object)character)
    {
      if (character.IsBoss() && m_statusEffectIfBossHash != 0)
        return m_statusEffectIfBossHash;
      if (character.IsPlayer() && m_statusEffectIfPlayerHash != 0)
        return m_statusEffectIfPlayerHash;
    }

    return m_statusEffectHash;
  }

  public void OnDrawGizmos()
  {
    var num = m_useTriggers ? 1 : 0;
  }
}