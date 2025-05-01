using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
using ValheimVehicles.Components;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Helpers;

public class VehicleRamAoe : ValheimAoe, IDeferredTrigger
{
  // Typeof PrefabTiers
  public string materialTier;
  public static List<VehicleRamAoe> RamInstances = [];

  public static HitData.DamageTypes baseDamage;

  [FormerlySerializedAs("minimumVelocityToTriggerHit")]
  public float MinimumVelocityToTriggerHit = 0.5f;

  public VehicleBaseController? m_vehicle;
  public RamPrefabs.RamType m_RamType;

  [FormerlySerializedAs("m_isVehicleRam")]
  public bool IsVehicleRamType = false;
  private float RamDamageOverallMultiplier = 1f;

  // damages
  public int RamDamageToolTier;
  public float PercentageDamageToSelf = 0f;
  public float RamBasePickAxeDamage = 0f;
  public float RamBaseSlashDamage = 0f;
  public float RamBasePierceDamage = 0f;
  public float RamBaseChopDamage = 0f;
  public float RamBaseBluntDamage = 0f;
  public float RamBaseMaximumDamage = 0f;

  // damage ratios
  public float chopDamageRatio;
  public float pickaxeDamageRatio;
  public float slashDamageRatio;
  public float pierceDamageRatio;
  public float bluntDamageRatio;

  public bool CanDamageSelf = true;
  public bool CanHitWhileHauling = true;
  public float DamageIncreasePercentagePerTier = 1;
  public float MaxVelocityMultiplier = 1f;
  public bool HasMaxDamageCap = RamConfig.HasMaximumDamageCap.Value;
  public HitData.DamageTypes selfDamage;
  public Collider[] selfHitColliders = new Collider[50];

  private float _disableTime = 0f;
  private const float _disableTimeMax = 0.5f;
  
  public bool isReadyForCollisions { get; set; }
  public bool isRebuildingCollisions
  {
    get;
    set;
  }

  private Rigidbody rigidbody;

  public static List<string> PiecesToMoveOnToVehicle = ["Cart", "Catapult", PrefabNames.LandVehicle];
  public static Regex ExclusionPattern;

  public float GetMinimumVelocityToTriggerRam()
  {
    var val = IsVehicleRamType ? RamConfig.VehicleMinimumVelocityToTriggerHit.Value : RamConfig.MinimumVelocityToTriggerHit.Value;
    var variantMultiplier = m_RamType == RamPrefabs.RamType.Stake ? 0.5f : 1f;
    return val * variantMultiplier;
  }

  public float GetPercentageDamageToVehicle()
  {
    if (m_RamType is RamPrefabs.RamType.LandVehicle or RamPrefabs.RamType.WaterVehicle)
    {
      return RamConfig.VehiclePercentageDamageToCollisionArea.Value;
    }
    return RamConfig.PercentageDamageToSelf.Value;
  }

  public int GetToolTier()
  {
    if (m_RamType == RamPrefabs.RamType.LandVehicle)
    {
      return RamConfig.LandVehicleRamToolTier.Value;
    }
    if (m_RamType == RamPrefabs.RamType.WaterVehicle)
    {
      return RamConfig.WaterVehicleRamToolTier.Value;
    }
    return RamConfig.RamDamageToolTier.Value;
  }

  public void InitializeFromConfig()
  {
    // must set this first.
    IsVehicleRamType = m_RamType is RamPrefabs.RamType.LandVehicle or RamPrefabs.RamType.WaterVehicle;
    // local config values
    MinimumVelocityToTriggerHit = GetMinimumVelocityToTriggerRam();
    DamageIncreasePercentagePerTier = RamConfig.DamageIncreasePercentagePerTier.Value;
    PercentageDamageToSelf = GetPercentageDamageToVehicle();
    CanHitWhileHauling = RamConfig.CanHitWhileHauling.Value;
    RamDamageToolTier = GetToolTier();
    CanDamageSelf = IsVehicleRamType ? RamConfig.VehicleRamCanDamageSelf.Value : RamConfig.CanDamageSelf.Value;

    switch (m_RamType)
    {
      case RamPrefabs.RamType.LandVehicle:
      case RamPrefabs.RamType.WaterVehicle:
        RamBaseMaximumDamage = RamConfig.RamBaseMaximumDamage.Value;
        MaxVelocityMultiplier = RamConfig.VehicleMaxVelocityMultiplier.Value;
        RamBaseSlashDamage = RamConfig.VehicleRamBaseSlashDamage.Value;
        RamBasePierceDamage = RamConfig.VehicleRamBasePierceDamage.Value;
        RamBaseBluntDamage = RamConfig.VehicleRamBaseBluntDamage.Value;
        RamBasePickAxeDamage = RamConfig.VehicleRamBasePickAxeDamage.Value;
        RamBaseChopDamage = RamConfig.VehicleRamBaseChopDamage.Value;
        m_hitTerrain = RamConfig.VehicleRamCanHitEnvironmentOrTerrain.Value;
        m_hitProps = RamConfig.VehicleRamCanHitEnvironmentOrTerrain.Value;
        m_hitCharacters = RamConfig.VehicleRamCanHitCharacters.Value;
        m_hitFriendly = RamConfig.VehicleRamCanHitFriendly.Value;
        m_hitEnemy = RamConfig.VehicleRamCanHitEnemies.Value;
        m_hitParent = RamConfig.VehicleRamCanDamageSelf.Value;
        break;
      case RamPrefabs.RamType.Stake:
      case RamPrefabs.RamType.Blade:
        RamBaseMaximumDamage = RamConfig.VehicleRamBaseMaximumDamage.Value;
        MaxVelocityMultiplier = RamConfig.MaxVelocityMultiplier.Value;
        RamBaseSlashDamage = RamConfig.RamBaseSlashDamage.Value;
        RamBasePierceDamage = RamConfig.RamBasePierceDamage.Value;
        RamBaseBluntDamage = RamConfig.RamBaseBluntDamage.Value;
        RamBasePickAxeDamage = RamConfig.RamBasePickAxeDamage.Value;
        RamBaseChopDamage = RamConfig.RamBaseChopDamage.Value;

        m_hitTerrain = RamConfig.CanHitEnvironmentOrTerrain.Value;
        m_hitProps = RamConfig.CanHitEnvironmentOrTerrain.Value;
        m_hitCharacters = RamConfig.CanHitCharacters.Value;
        m_hitFriendly = RamConfig.CanHitFriendly.Value;
        m_hitEnemy = RamConfig.CanHitEnemies.Value;
        m_hitParent = RamConfig.CanDamageSelf.Value;
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }

    m_blockable = false;
    m_dodgeable = false;
    m_hitInterval = Mathf.Clamp(RamConfig.RamHitInterval.Value, 0.5f, 20f);

    // todo need to tweak this
    m_damageSelf = !RamConfig.CanDamageSelf.Value ? 0 : 1;
    m_toolTier = RamDamageToolTier;
    m_attackForce = 5;

    m_radius = Mathf.Clamp(IsVehicleRamType ? RamConfig.VehicleRamHitRadius.Value : RamConfig.HitRadius.Value, 0.1f, 50f);
    m_radius *= m_RamType == RamPrefabs.RamType.Stake ? 0.5f : 1f;
   
    m_useTriggers = true;
    m_triggerEnterOnly = !IsVehicleRamType && RamConfig.AllowContinuousDamage.Value;
    m_useCollider = null;
    m_useAttackSettings = true;
    m_ttl = 0;

    // must be called otherwise nothing will happen when this InitializeConfig is called
    SetBaseDamageFromConfig();
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
    m_attackForce = 0f;
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
    rigidbody = GetComponent<Rigidbody>();

    // Prevents problems with items and other objects. Letting collisions hit every layer is not optimal so scoping to only includes/excludes layers is better.
    // rigidbody.excludeLayers = LayerHelpers.RamColliderExcludeLayers;
    // rigidbody.includeLayers = LayerHelpers.PhysicalLayers;
  }

  public void OnBoundsRebuildStart()
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
    // must initialize after Awake so we can set these values after AddComponent is called.
    InitializeFromConfig();
    InitAoe();
    
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

    var isVehicleChild = m_nview.GetZDO().GetInt(VehicleZdoVars.MBParentId);
    if (isVehicleChild == 0)
    {
      isReadyForCollisions = true;
      return;
    }

    // Must be within the BaseVehiclePieces after initialization otherwise this AOE could attempt to damage items within the raft ball
    var root = transform.root;
    var rootName = root.name;
    
    var isChildOfBaseVehicle =
      PrefabNames.IsVehicle(rootName) ||
      rootName.StartsWith(PrefabNames.VehiclePiecesContainer) ||
      rootName.StartsWith(
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

    return UpdateDamageFromVelocity(relativeVelocity, collider);
  }

  public bool UpdateDamageFromVelocity(float relativeVelocityMagnitude, Collider collider)
  {
    // exits if the velocity is not within expected damage ranges
    if (relativeVelocityMagnitude < MinimumVelocityToTriggerHit) return false;

    if (relativeVelocityMagnitude == 0)
    {
      relativeVelocityMagnitude = 0.1f;
    }

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

    if (IsVehicleRamType)
    {
      slashDamage = baseDamage.m_slash * multiplier;
      chopDamage = baseDamage.m_chop * multiplier;
      pierceDamage = baseDamage.m_pierce * multiplier;
    }

    var nextTotalDamage = 0f;

    if (HasMaxDamageCap)
    {
      nextTotalDamage =
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

        slashDamage = RamBaseMaximumDamage * slashDamageRatio;
        bluntDamage = RamBaseMaximumDamage * bluntDamageRatio;
        chopDamage = RamBaseMaximumDamage * chopDamageRatio;
        pickaxeDamage = RamBaseMaximumDamage * pickaxeDamageRatio;
        pierceDamage = RamBaseMaximumDamage * pierceDamageRatio;
      }
    }

    var damageSum = slashDamage + bluntDamage + pierceDamage + chopDamage + pickaxeDamage;
    if (damageSum < 1)
    {
      m_damage = new HitData.DamageTypes()
      {
        m_blunt = 0,
        m_pierce = 0,
        m_slash = 0,
        m_chop = 0,
        m_pickaxe = 0
      };
      return false;
    }
    
    m_damage = new HitData.DamageTypes()
    {
      m_blunt = bluntDamage,
      m_pierce = pierceDamage,
      m_slash = slashDamage,
      m_chop = chopDamage,
      m_pickaxe = pickaxeDamage
    };

    selfDamage = new HitData.DamageTypes()
    {
      m_blunt = bluntDamage * PercentageDamageToSelf,
      m_pierce = pierceDamage * PercentageDamageToSelf,
      m_slash = slashDamage * PercentageDamageToSelf,
      m_chop = chopDamage * PercentageDamageToSelf,
      m_pickaxe = pickaxeDamage * PercentageDamageToSelf
    };

    // do not damage self for land vehicles, instead we apply damage directly to the area.
    if (!CanDamageSelf || m_RamType == RamPrefabs.RamType.LandVehicle || m_RamType == RamPrefabs.RamType.WaterVehicle)
    {
      m_damageSelf = 0;
    }
    else
    {
      m_damageSelf =
        nextTotalDamage *
      PercentageDamageToSelf;
    }

    return true;
  }

  public HitData? cachedSelfDamageHitData = new();
  // todo might need to set this to Boat (but I saw no damage)
  public HitData.HitType damageSelfHitType = HitData.HitType.EnemyHit;

  public HitData GetHitData(Collider collider, Vector3 collisionPoint)
  {
    if (cachedSelfDamageHitData != null) return cachedSelfDamageHitData;
    var collisionDir = collider.bounds.center.DirTo(collisionPoint);
    var hitData = new HitData()
    {
      m_point = collisionPoint,
      m_hitCollider = collider,
      m_damage = selfDamage,
      m_dir = collisionDir,
      m_blockable = false,
      m_dodgeable = false,
      m_pushForce = 0,
      m_backstabBonus = 1,
      m_ranged = false,
      m_ignorePVP = true,
      m_toolTier = (short)RamDamageToolTier,
      m_hitType = damageSelfHitType,
      m_radius = m_radius // todo might not be necessary
    };

    cachedSelfDamageHitData = hitData;
    return hitData;
  }

  /// <summary>
  /// For vehicle ram variants
  /// </summary>
  /// <param name="collider"></param>
  public void OnHitSelfVehiclePieces(Collider collider)
  {
    if (m_vehicle == null || m_vehicle.PiecesController == null) return;
    var approximateTotalDamage = selfDamage.m_blunt + selfDamage.m_pierce + selfDamage.m_slash + selfDamage.m_chop + selfDamage.m_pickaxe + selfDamage.m_pierce;
    if (approximateTotalDamage < 5f)
    {
      return;
    }

    // todo possibly check collider type and use closestPoint
    var collisionPoint = collider.ClosestPointOnBounds(transform.position);

    // only hit piece layer to avoid hitting player and other layers that might not be a piece but could be on the vehicle.
    var hitCount = Physics.OverlapSphereNonAlloc(collisionPoint, m_radius, selfHitColliders, LayerHelpers.PieceLayer);
    cachedSelfDamageHitData = null;

    if (hitCount < 1) return;

    for (var i = 0; i < hitCount && i < selfHitColliders.Length; i++)
    {
      var hitCollider = selfHitColliders[i];
      if (hitCollider == null) continue;
      var isPieceController = hitCollider.transform.root == m_vehicle.PiecesController.transform;
      if (!isPieceController) continue;
      var wnt = hitCollider.GetComponentInParent<WearNTear>();
      if (!wnt) continue;
      var hitData = GetHitData(collider, collisionPoint);
      wnt.Damage(hitData);
    }
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
    var colliderObj = collider.gameObject;
    
    var character = collider.GetComponentInParent<Character>();
    if (WaterZoneUtils.IsOnboard(character))
    {
      IgnoreCollider(collider);
      return false;
    }

    var relativeVelocity = GetRelativeVelocity(collider);
    if (relativeVelocity < MinimumVelocityToTriggerHit) return false;

    return base.ShouldHit(collider);
  }

  private bool ShouldMoveToPieceController(Collider collider)
  {
    if (m_vehicle == null) return false;
    if (m_vehicle.PiecesController == null) return false;

    var root = collider.transform.root;
    if (PrefabNames.IsVehicle(root.name)) return false;

    if (ExclusionPattern.IsMatch(root.name) || LayerHelpers.IsItemLayer(collider.gameObject.layer))
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
    if (!collider) return true;

    VehiclePiecesController? vehiclePiecesController = null;

    var colliderObj = collider.gameObject;

    if (colliderObj.layer == LayerHelpers.ItemLayer)
    {
      LoggerProvider.LogDev($"Ignoring itemLayer {colliderObj.layer} for gameobject {colliderObj.name} because items are not allowed to be collider by vehicle ram colliders.");
      if (ComponentSelectors.TryGetVehiclePiecesController(m_vehicle, out var piecesController) && collider.transform.root != piecesController.transform)
      {
        vehiclePiecesController = piecesController;

        var rootPrefabNetView = GetComponentInParent<ZNetView>();
        if (rootPrefabNetView != null)
        {
          piecesController.AddTemporaryPiece(rootPrefabNetView);
        }
      }
      IgnoreCollider(collider);
      return true;
    }

    if (!LayerHelpers.IsContainedWithinLayerMask(colliderObj.layer, LayerHelpers.PhysicalLayers))
    {
#if DEBUG
      LoggerProvider.LogDebug($"Ignoring layer {colliderObj.layer} for gameobject {colliderObj.name} because it is not within PhysicalLayer mask.");
#endif
      IgnoreCollider(collider);
      return false;
    }

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
    if (character != null)
    {
      if (character.IsTeleporting())
      {
        m_vehicle.MovementController.StartPlayerCollisionAfterTeleport(collider, character);
        return true;
      }

      // Animal/Tameable support.
      // confirm this works
      if (!character.IsPlayer() && character.IsTamed() && vehiclePiecesController != null)
      {
        var nv = character.GetComponent<ZNetView>();
        vehiclePiecesController.AddTemporaryPiece(nv);
        return false;
      }

      if (WaterZoneUtils.IsOnboard(character))
      {
        IgnoreCollider(collider);
        return true;
      }
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
    if (m_vehicle == null) return false;

    if (m_vehicle.isCreative) return false;
    if (!CanHitWhileHauling && m_vehicle.MovementController != null && m_vehicle.MovementController.isPlayerHaulingVehicle)
    {
      return false;
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
          Vector3.Magnitude(collision.relativeVelocity), collision.collider)) return;
    base.OnCollisionEnterHandler(collision);
  }

  public override void OnCollisionStayHandler(Collision collision)
  {
    if (!IsReady()) return;
    if (ShouldIgnore(collision.collider)) return;
    if (!UpdateDamageFromVelocity(
          Vector3.Magnitude(collision.relativeVelocity), collision.collider)) return;
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

  public override bool OnHit(Collider collider, Vector3 hitPoint)
  {
    var hasHit = base.OnHit(collider, hitPoint);
    if (!hasHit) return false;
    if (!IsVehicleRamType) return true;
    // additional call.
    // do not run for excluded self hit collisions.
    if (!m_hitSelfExcludeList.Contains(collider))
    {
      OnHitSelfVehiclePieces(collider);
    }
    return true;
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
}