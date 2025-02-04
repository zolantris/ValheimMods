using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimRAFT;
using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Vehicles;
using ValheimVehicles.Vehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : ValheimAoe, IDeferredTrigger
{
  // Typeof PrefabTiers
  public string materialTier;
  public static List<VehicleRamAoe> RamInstances = [];

  public static HitData.DamageTypes baseDamage;

  public float minimumVelocityToTriggerHit =>
    RamConfig.minimumVelocityToTriggerHit.Value *
    (m_RamType == RamPrefabs.RamType.Blade ? 1 : 0.5f);

  public VehicleShip? m_vehicle;
  public RamPrefabs.RamType m_RamType;

  public bool m_isVehicleRam = false;
  private float RamDamageOverallMultiplier = 1f;

  // damages
  public static int RamDamageToolTier;

  public static float PercentageDamageToSelf =
    RamConfig.PercentageDamageToSelf.Value;

  public static float RamBaseSlashDamage => RamConfig.RamBaseSlashDamage.Value;
  public static float RamBaseBluntDamage => RamConfig.RamBaseBluntDamage.Value;
  public static float RamBaseChopDamage => RamConfig.RamBaseChopDamage.Value;

  public static float RamBasePickAxeDamage =>
    RamConfig.RamBasePickAxeDamage.Value;

  public static float RamBasePierceDamage =>
    RamConfig.RamBasePierceDamage.Value;

  public static float RamBaseMaximumDamage =>
    RamConfig.RamBaseMaximumDamage.Value;

  public static bool AllowContinuousDamage =
    RamConfig.AllowContinuousDamage.Value;

  // hit booleans
  public static bool RamsCanHitEnvironmentOrTerrain =
    RamConfig.CanHitEnvironmentOrTerrain.Value;

  public float chopDamageRatio;
  public float pickaxeDamageRatio;
  public float slashDamageRatio;
  public float pierceDamageRatio;
  public float bluntDamageRatio;

  public static float DamageIncreasePercentagePerTier =>
    RamConfig.DamageIncreasePercentagePerTier.Value;

  public static float MaxVelocityMultiplier =>
    RamConfig.MaxVelocityMultiplier.Value;

  public static bool HasMaxDamageCap =>
    RamConfig.HasMaximumDamageCap.Value;

  public bool isReadyForCollisions { get; set; }
  public bool isRebuildingCollisions
  {
    get;
    set;
  }

  private Rigidbody rigidbody;

  public static List<string> PiecesToMoveOnToVehicle = ["Cart"];
  public static Regex ExclusionPattern;

  public void InitializeFromConfig()
  {
    m_blockable = false;
    m_dodgeable = false;
    m_hitTerrain = RamConfig.CanHitEnvironmentOrTerrain.Value;
    m_hitProps = RamConfig.CanHitEnvironmentOrTerrain.Value;
    m_hitCharacters = RamConfig.CanHitCharacters.Value;
    m_hitFriendly = RamConfig.CanHitFriendly.Value;
    m_hitEnemy = RamConfig.CanHitEnemies.Value;
    m_hitParent = RamConfig.CanDamageSelf.Value;
    m_hitInterval = Mathf.Clamp(RamConfig.RamHitInterval.Value, 0.5f, 20f);

    // todo need to tweak this
    m_damageSelf = !RamConfig.CanDamageSelf.Value ? 0 : 1;
    m_toolTier = RamConfig.RamDamageToolTier.Value;
    m_attackForce = 5;
    m_attackForce = 50;

    m_radius = Mathf.Clamp(RamConfig.HitRadius.Value, 0.1f, 150f);
    m_radius *= m_RamType == RamPrefabs.RamType.Blade ? 1 : 0.5f;

    m_useTriggers = true;
    m_triggerEnterOnly = RamConfig.AllowContinuousDamage.Value;
    m_useCollider = null;
    m_useAttackSettings = true;
    m_ttl = 0;

    if (m_isVehicleRam) SetVehicleRamModifier(m_isVehicleRam);
  }

  public float GetTotalDamage(float slashDamage, float bluntDamage,
    float chopDamage,
    float pickaxeDamage, float pierceDamage)
  {
    return slashDamage + bluntDamage + chopDamage + pickaxeDamage +
           pierceDamage;
  }

  public void InitAoe()
  {
    base.Awake();
  }

  public static Regex GenerateRegexFromList(List<string> prefixes)
  {
    // Escape special characters in the strings and join them with a pipe (|) for OR condition
    var escapedPrefixes = new List<string>();
    foreach (var prefix in prefixes)
    {
      escapedPrefixes.Add(Regex.Escape(prefix));
    }

    // Create a regex pattern that matches the start of the string (^)
    // It will match any of the provided prefixes at the start of the string
    var pattern = "^(" + string.Join("|", escapedPrefixes) + ")";
    return new Regex(pattern);
  }

  public override void Awake()
  {
    if (!RamInstances.Contains(this)) RamInstances.Add(this);
    ExclusionPattern = GenerateRegexFromList(PiecesToMoveOnToVehicle);

    InitializeFromConfig();
    SetBaseDamageFromConfig();

    InitAoe();

    rigidbody = GetComponent<Rigidbody>();
    // very important otherwise this rigidbody will interfere with physics of the Watervehicle controller due to nesting.
    // todo to move this rigidbody into a joint and make it a sibling of the WaterVehicle or PieceContainer (doing this would be a large refactor to structure, likely requiring a new prefab)
    // if (rigidbody) rigidbody.includeLayers = m_rayMask;
  }

  private float _disableTime = 0f;
  private const float _disableTimeMax = 0.5f;

  public void OnBoundsRebuild()
  {
    isRebuildingCollisions = true;
    _disableTime = Time.fixedTime + _disableTimeMax;
  }

  public override void OnDisable()
  {
    if (RamInstances.Contains(this)) RamInstances.Remove(this);

    base.OnDisable();
  }

  public void Start()
  {
    Invoke(nameof(UpdateReadyForCollisions), 1f);
  }

  public override void OnEnable()
  {
    if (!RamInstances.Contains(this)) RamInstances.Add(this);

    Invoke(nameof(UpdateReadyForCollisions), 1f);
    base.OnEnable();
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

    // Must be within the BaseVehiclePieces after initialization otherwise this AOE could attempt to damage items within the raft ball
    var isChildOfBaseVehicle =
      PrefabNames.IsVehicle(transform.root.name) ||
      transform.root.name.StartsWith(PrefabNames.VehiclePiecesContainer) ||
      transform.root.name.StartsWith(
        PrefabNames
          .VehicleMovingPiecesContainer);
    if (!isChildOfBaseVehicle)
    {
      isReadyForCollisions = false;
      Invoke(nameof(UpdateReadyForCollisions), 1);
      return;
    }

    isReadyForCollisions = true;
  }

  public float GetRelativeVelocity(Collider collider)
  {
    var colliderVelocity = collider.attachedRigidbody != null ? collider.attachedRigidbody.velocity : Vector3.zero;
    if (m_vehicle == null || m_vehicle.MovementController == null)
      return colliderVelocity.magnitude;

    var vehicleVelocity = m_vehicle.MovementController.m_body
      .velocity;
    var relativeVelocity = Vector3.Magnitude(colliderVelocity - vehicleVelocity);

    return relativeVelocity;
  }

  public bool UpdateDamageFromVelocityCollider(Collider collider)
  {
    if (!collider) return false;
    var relativeVelocity = GetRelativeVelocity(collider);
    // reset damage to base damage if one of these is not available, will still recalculate later
    // exit to apply damage that has no velocity
    if (m_vehicle == null &&
        collider.attachedRigidbody == null)
    {
      m_damage = baseDamage;
      return false;
    }

    return UpdateDamageFromVelocity(relativeVelocity);
  }

  public bool UpdateDamageFromVelocity(float relativeVelocityMagnitude)
  {
    // exits if the velocity is not within expected damage ranges
    if (relativeVelocityMagnitude < minimumVelocityToTriggerHit) return false;

    var multiplier = Mathf.Min(relativeVelocityMagnitude * 0.5f,
      MaxVelocityMultiplier) * RamDamageOverallMultiplier;

    if (materialTier == PrefabTiers.Tier3)
      multiplier *= Mathf.Clamp(1 + DamageIncreasePercentagePerTier * 2, 1, 4);

    // todo add a minimum damage for a vehicle crushing an object.
    // if (RamConfig.VehicleHullMassMultiplierDamage.Value != 0 && m_vehicle != null && m_vehicle.MovementControllerRigidbody != null)
    // {
    //   multiplier += Mathf.Clamp(m_vehicle.MovementControllerRigidbody.mass * RamConfig.VehicleHullMassMultiplierDamage.Value * relativeVelocityMagnitude, 0, 3);
    // }

    if (Mathf.Approximately(multiplier, 0)) multiplier = 0;

    var bluntDamage = baseDamage.m_blunt * multiplier;
    var pickaxeDamage = baseDamage.m_pickaxe * multiplier;
    float slashDamage = 0;
    float chopDamage = 0;
    float pierceDamage = 0;

    if (m_RamType == RamPrefabs.RamType.Stake)
      pierceDamage = baseDamage.m_pierce * multiplier;

    if (m_RamType == RamPrefabs.RamType.Blade)
    {
      slashDamage = baseDamage.m_slash * multiplier;
      chopDamage = baseDamage.m_chop * multiplier;
    }


    if (HasMaxDamageCap)
    {
      var nextTotalDamage =
        GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage,
          pierceDamage);

      if (nextTotalDamage > RamBaseMaximumDamage)
      {
        if (nextTotalDamage <= 0) return false;
        if (chopDamageRatio == 0)
          chopDamageRatio = chopDamage / nextTotalDamage;

        if (pickaxeDamageRatio == 0)
          pickaxeDamageRatio = pickaxeDamage / nextTotalDamage;

        if (slashDamageRatio == 0)
          slashDamageRatio = slashDamage / nextTotalDamage;

        if (bluntDamageRatio == 0)
          bluntDamageRatio = bluntDamage / nextTotalDamage;

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
      m_pickaxe = pickaxeDamage
    };

    if (!RamConfig.CanDamageSelf.Value)
    {
      m_damageSelf = 0;
      return true;
    }

    m_damageSelf =
      GetTotalDamage(slashDamage, bluntDamage, chopDamage, pickaxeDamage,
        pierceDamage) *
      PercentageDamageToSelf;

    return true;
  }

  private void IgnoreCollider(Collider collider)
  {
    var childColliders = GetComponentsInChildren<Collider>();
    foreach (var childCollider in childColliders)
      Physics.IgnoreCollision(childCollider, collider, true);
  }

  public void UpdateReloadingTime()
  {
    isRebuildingCollisions = Time.fixedTime < _disableTime;
    if (!isRebuildingCollisions)
    {
      _disableTime = 0f;
    }
  }

  public override bool ShouldHit(Collider collider)
  {
    if (!IsReady()) return false;

    var character = collider.GetComponentInParent<Character>();
    if (WaterZoneUtils.IsOnboard(character))
    {
      IgnoreCollider(collider);
      return false;
    }

    var relativeVelocity = GetRelativeVelocity(collider);
    if (relativeVelocity < minimumVelocityToTriggerHit) return false;

    return base.ShouldHit(collider);
  }

  private bool ShouldMoveToPieceController(Collider collider)
  {
    if (m_vehicle == null) return false;
    if (m_vehicle.PiecesController == null) return false;

    var root = collider.transform.root;
    if (PrefabNames.IsVehicle(root.name)) return false;

    if (ExclusionPattern.IsMatch(root.name))
    {
      var netView = root.GetComponent<ZNetView>();
      if (!netView) return false;
      m_vehicle.PiecesController.AddTemporaryPiece(netView);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Ignores anything within the current vehicle and other vehicle movement/float/onboard colliders 
  /// </summary>
  /// <param name="collider"></param>
  /// <returns></returns>
  private bool ShouldIgnore(Collider collider)
  {
    if (!collider) return false;
    if (PrefabNames.IsVehicleCollider(collider.name))
    {
      IgnoreCollider(collider);
      return true;
    }

    if (ShouldMoveToPieceController(collider))
    {
      IgnoreCollider(collider);
      return true;
    }

    var character = collider.GetComponentInParent<Character>();
    if (character != null && WaterZoneUtils.IsOnboard(character))
    {
      IgnoreCollider(collider);
      return true;
    }

    if (m_vehicle != null)
    {
      if (m_vehicle.PiecesController != null &&
          m_vehicle.PiecesController.transform == collider.transform.root)
      {
        IgnoreCollider(collider);
        return true;
      }

      // allows for hitting other vehicles, excludes hitting current vehicle
      if (collider.transform.root == m_vehicle.transform.root)
      {
        IgnoreCollider(collider);
        return true;
      }

      return false;
    }

    if (collider.transform.root != transform.root) return false;

    IgnoreCollider(collider);
    return true;
  }

  /// <summary>
  /// Prevent rams from working until colliders are working on the OnboardVehicle.
  /// If no vehicle, Do nothing for rams outside the vehicle.
  /// </summary>
  /// <returns></returns>
  public bool IsWaitingForOnboardCollider()
  {
    if (m_vehicle == null) return false;
    return m_vehicle.OnboardController != null && (!m_vehicle.OnboardController.isReadyForCollisions || m_vehicle.OnboardController.isRebuildingCollisions);
  }
  /// <summary>
  /// Same logic as (VehicleRamAOE,VehicleOnboardController)
  /// </summary>
  /// todo share logic
  /// <returns></returns>
  public bool IsReady()
  {
    if (!isReadyForCollisions) return false;
    if (isRebuildingCollisions)
    {
      UpdateReloadingTime();
    }

    if (IsWaitingForOnboardCollider())
    {
      return false;
    }

    return !isRebuildingCollisions;
  }

  public override void OnCollisionEnterHandler(Collision collision)
  {
    if (!IsReady()) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(
          Vector3.Magnitude(collision.relativeVelocity))) return;
    base.OnCollisionEnterHandler(collision);
  }

  public override void OnCollisionStayHandler(Collision collision)
  {
    if (!IsReady()) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(
          Vector3.Magnitude(collision.relativeVelocity))) return;

    base.OnCollisionStayHandler(collision);
  }

  public override void OnTriggerEnterHandler(Collider collider)
  {
    if (!IsReady()) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerEnterHandler(collider);
  }

  public override void OnTriggerStayHandler(Collider collider)
  {
    if (!IsReady()) return;
    if (ShouldIgnore(collider)) return;
    if (!UpdateDamageFromVelocityCollider(collider)) return;
    base.OnTriggerStayHandler(collider);
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
      m_pickaxe = RamBasePickAxeDamage
    });
  }

  public void SetVehicleRamModifier(bool isRamEnabled)
  {
    m_isVehicleRam = isRamEnabled;
    RamDamageOverallMultiplier = 0.5f;
    m_triggerEnterOnly = false;

    // overrides for vehicles.
    m_toolTier = RamConfig.HullToolTier.Value;

    // vehicles need much more radius to effectively hit
    m_radius = Mathf.Clamp(m_radius, 10f, 50f);
    InitAoe();
  }

  public static void OnBaseSettingsChange(object sender, EventArgs eventArgs)
  {
    foreach (var instance in RamInstances.ToList())
    {
      if (!instance)
      {
        RamInstances.Remove(instance);
        continue;
      }

      if (RamConfig.RamDamageEnabled.Value)
      {
        instance.InitializeFromConfig();
        instance.InitAoe();
        instance.gameObject.SetActive(true);
      }
      else
      {
        instance.gameObject.SetActive(false);
      }
    }
  }

  public static void OnSettingsChanged(object sender, EventArgs eventArgs)
  {
    foreach (var instance in RamInstances.ToArray())
    {
      if (!instance)
      {
        RamInstances.Remove(instance);
        continue;
      }

      instance.SetBaseDamageFromConfig();
      instance.InitAoe();
    }
  }
}