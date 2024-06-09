using System;
using System.Collections.Generic;
using System.Security.Policy;
using Microsoft.Win32;
using UnityEngine;
using ValheimRAFT;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : Aoe
{
  public static List<VehicleRamAoe> RamInstances = [];

  public static HitData.DamageTypes baseDamage;
  public float velocityMultiplier = 0;
  public float massMultiplier = RamConfig.MaxVelocityMultiplier.Value;
  public float minimumVelocityToTriggerHit = RamConfig.minimumVelocityToTriggerHit.Value;
  public VehicleShip? vehicle;
  public RamPrefabs.RamType RamType;

  // damages
  public static int RamDamageToolTier = RamConfig.RamDamageToolTier.Value;
  public static float RamHitArea = RamConfig.HitRadius.Value;
  public static float PercentageDamageToSelf = RamConfig.PercentageDamageToSelf.Value;
  public static float RamBaseSlashDamage => RamConfig.RamBaseSlashDamage.Value;
  public static float RamBaseBluntDamage => RamConfig.RamBaseBluntDamage.Value;
  public static float RamBaseChopDamage => RamConfig.RamBaseChopDamage.Value;
  public static float RamBasePickAxeDamage => RamConfig.RamBasePickAxeDamage.Value;
  public static float RamBasePierceDamage => RamConfig.RamBasePierceDamage.Value;
  public static float RamBaseMaximumDamage => RamConfig.RamBaseMaximumDamage.Value;

  public static float RamHitInterval = RamConfig.RamHitInterval.Value;

  public static bool AllowContinuousDamage = RamConfig.AllowContinuousDamage.Value;

  // hit booleans
  public static bool RamsCanHitEnvironmentOrTerrain =
    RamConfig.CanHitEnvironmentOrTerrain.Value;

  public static bool RamsCanHitEnemies = RamConfig.CanHitEnemies.Value;
  public static bool CanDamageSelf = RamConfig.CanDamageSelf.Value;
  public static bool RamsCanHitCharacters = RamConfig.CanHitCharacters.Value;
  public static bool RamsCanHitFriendly = RamConfig.CanHitFriendly.Value;

  public float chopDamageRatio;
  public float pickaxeDamageRatio;
  public float slashDamageRatio;
  public float pierceDamageRatio;
  public float bluntDamageRatio;


  public static float MaxVelocityMultiplier =>
    RamConfig.MaxVelocityMultiplier.Value;

  public static bool HasMaxDamageCap =>
    RamConfig.HasMaximumDamageCap.Value;

  public bool isReadyForCollisions = false;

  private Rigidbody rigidbody;

  public void InitializeFromConfig()
  {
    m_blockable = false;
    m_dodgeable = false;
    m_hitTerrain = RamsCanHitEnvironmentOrTerrain;
    m_hitProps = RamsCanHitEnvironmentOrTerrain;
    m_hitCharacters = RamsCanHitCharacters;
    m_hitFriendly = RamsCanHitFriendly;
    m_hitEnemy = RamsCanHitEnemies;

    // todo may need this to do damage to wearntear prefab of the ram
    m_hitParent = CanDamageSelf;
    m_hitInterval = Mathf.Clamp(RamHitInterval, 0.5f, 5f);

    // todo need to tweak this
    m_damageSelf = !CanDamageSelf ? 0 : 1;
    m_toolTier = RamDamageToolTier;
    m_attackForce = 5;
    m_radius = Mathf.Clamp(RamHitArea, 0.1f, 10f);
    m_useTriggers = true;
    m_triggerEnterOnly = AllowContinuousDamage;
    m_useCollider = null;
    m_useAttackSettings = true;
    m_ttl = 0;
    m_canRaiseSkill = true;
    m_skill = Skills.SkillType.None;
    m_backstabBonus = 1;

    SetBaseDamageFromConfig();
  }

  public float GetTotalDamage(float slashDamage, float bluntDamage, float chopDamage,
    float pickaxeDamage, float pierceDamage)
  {
    return slashDamage + bluntDamage + chopDamage + pickaxeDamage + pierceDamage;
  }

  public new void Awake()
  {
    if (!RamInstances.Contains(this))
    {
      RamInstances.Add(this);
    }

    InitializeFromConfig();
    SetBaseDamageFromConfig();
    base.Awake();

    rigidbody = GetComponent<Rigidbody>();
    // very important otherwise this rigidbody will interfere with physics of the Watervehicle controller due to nesting.
    // todo to move this rigidbody into a joint and make it a sibling of the WaterVehicle or PieceContainer (doing this would be a large refactor to structure, likely requiring a new prefab)
    if (rigidbody)
    {
      rigidbody.includeLayers = m_rayMask;
    }
  }

  private void OnDisable()
  {
    if (RamInstances.Contains(this))
    {
      RamInstances.Remove(this);
    }

    base.OnDisable();
  }

  public void Start()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
  }

  public override void OnEnable()
  {
    if (!RamInstances.Contains(this))
    {
      RamInstances.Add(this);
    }

    Invoke(nameof(UpdateReadyForCollisions), 1f);
    base.OnEnable();
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
    var isChildOfBaseVehicle = transform.root.name.StartsWith(PrefabNames.WaterVehicleShip) ||
                               transform.root.name.StartsWith(PrefabNames.PiecesContainer);
    if (!(bool)isChildOfBaseVehicle)
    {
      isReadyForCollisions = false;
      return;
    }

    isReadyForCollisions = true;
  }

  public bool UpdateDamageFromVelocityCollider(Collider collider)
  {
    if (!collider) return false;
    // reset damage to base damage if one of these is not available, will still recalculate later
    if (!vehicle?.MovementController.m_body || !collider.attachedRigidbody)
    {
      m_damage = baseDamage;
    }

    // early exit if both are not valid
    if (!vehicle?.MovementController.m_body && !collider.attachedRigidbody) return false;

    // Velocity will significantly increase if the object is moving towards the other object IE collision
    float relativeVelocity;
    if (!vehicle?.MovementController.m_body)
    {
      relativeVelocity = collider.attachedRigidbody.velocity.magnitude;
    }
    else
    {
      relativeVelocity =
        Vector3.Magnitude(collider?.attachedRigidbody?.velocity ??
                          Vector3.zero - vehicle?.MovementController.m_body?.velocity ??
                          Vector3.zero);
    }

    return UpdateDamageFromVelocity(relativeVelocity);
  }

  public bool UpdateDamageFromVelocity(float relativeVelocityMagnitude)
  {
    // exits if the velocity is not within expected damage ranges
    if (relativeVelocityMagnitude < minimumVelocityToTriggerHit) return false;

    var multiplier = Mathf.Min(relativeVelocityMagnitude * 0.5f, MaxVelocityMultiplier);


    if (Mathf.Approximately(multiplier, 0))
    {
      multiplier = 0;
    }

    var bluntDamage = baseDamage.m_blunt * multiplier;
    var pickaxeDamage = baseDamage.m_pickaxe * multiplier;
    float slashDamage = 0;
    float chopDamage = 0;
    float pierceDamage = 0;

    if (RamType == RamPrefabs.RamType.Stake)
    {
      pierceDamage = baseDamage.m_pierce * multiplier;
    }

    if (RamType == RamPrefabs.RamType.Blade)
    {
      slashDamage = baseDamage.m_slash * multiplier;
      chopDamage = baseDamage.m_chop * multiplier;
    }


    if (HasMaxDamageCap)
    {
      var nextTotalDamage =
        GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage, pierceDamage);

      if (nextTotalDamage > RamBaseMaximumDamage)
      {
        if (nextTotalDamage <= 0) return false;
        if (chopDamageRatio == 0)
        {
          chopDamageRatio = chopDamage / nextTotalDamage;
        }

        if (pickaxeDamageRatio == 0)
        {
          pickaxeDamageRatio = pickaxeDamage / nextTotalDamage;
        }

        if (slashDamageRatio == 0)
        {
          slashDamageRatio = slashDamage / nextTotalDamage;
        }

        if (bluntDamageRatio == 0)
        {
          bluntDamageRatio = bluntDamage / nextTotalDamage;
        }

        slashDamage = baseDamage.m_slash * slashDamageRatio;
        bluntDamage = baseDamage.m_blunt * bluntDamageRatio;
        chopDamage = baseDamage.m_chop * chopDamageRatio;
        pickaxeDamage = baseDamage.m_pickaxe * pickaxeDamageRatio;
        pierceDamage = baseDamage.m_pierce * pierceDamageRatio;
      }
    }

    m_damage = new HitData.DamageTypes()
    {
      m_blunt = bluntDamage,
      m_pierce = pierceDamage,
      m_slash = slashDamage,
      m_chop = chopDamage,
      m_pickaxe = pickaxeDamage,
    };

    if (!CanDamageSelf)
    {
      return true;
    }

    m_damageSelf =
      GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage, pierceDamage) *
      PercentageDamageToSelf;

    return true;
  }

  private bool ShouldIgnore(Collider collider)
  {
    if (!collider) return false;
    if ((!collider.transform.root.name.StartsWith(PrefabNames.PiecesContainer) ||
         !collider.transform.root.name.StartsWith(PrefabNames.WaterVehicleShip)) &&
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
    if (!UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity))) return;
    base.OnCollisionEnter(collision);
  }

  public new void OnCollisionStay(Collision collision)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(Vector3.Magnitude(collision.relativeVelocity))) return;
    base.OnCollisionStay(collision);
  }

  public new void OnTriggerEnter(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerEnter(collider);
  }

  public new void OnTriggerStay(Collider collider)
  {
    if (!isReadyForCollisions) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerStay(collider);
  }

  public void SetBaseDamage(HitData.DamageTypes data)
  {
    chopDamageRatio = 0;
    pickaxeDamageRatio = 0;
    slashDamageRatio = 0;
    pierceDamageRatio = 0;
    bluntDamageRatio = 0;
    baseDamage = data;
  }

  public void SetBaseDamageFromConfig()
  {
    Logger.LogDebug("Setting Damage config for Ram");
    SetBaseDamage(new HitData.DamageTypes()
    {
      m_slash = RamBaseSlashDamage,
      m_pierce = RamBasePierceDamage,
      m_blunt = RamBaseBluntDamage,
      m_chop = RamBaseChopDamage,
      m_pickaxe = RamBasePickAxeDamage,
    });
  }

  public static void OnBaseSettingsChange(object sender, EventArgs eventArgs)
  {
    foreach (var vehicleRamAoe in RamInstances)
    {
      vehicleRamAoe.InitializeFromConfig();
    }
  }

  public static void OnSettingsChanged(object sender, EventArgs eventArgs)
  {
    foreach (var instance in RamInstances.ToArray())
    {
      instance.SetBaseDamageFromConfig();
    }
  }
}