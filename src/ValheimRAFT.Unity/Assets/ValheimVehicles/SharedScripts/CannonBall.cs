// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// Cannonball manages its own firing, physics, trigger, and returns itself to pool.
  /// </summary>
  public class Cannonball : MonoBehaviour
  {

    public enum CannonballType
    {
      Solid,
      Explosive
    }

    public enum HitMaterial
    {
      None,
      Wood,
      Metal,
      Stone,
      Terrain
    }

    public enum ProjectileHitType
    {
      Explosion,
      Bounce,
      Penetration,
      None
    }

    /// <summary>
    /// Gets the prefab root. This should be overridable.
    /// </summary>
    /// <returns></returns>
    public static Func<Transform, Transform> GetPrefabRoot = localTransform => localTransform.root;

    public static Action<DamageInfo> ApplyDamage = damageInfo =>
    {
#if !UNITY_2022 && !UNITY_EDITOR
      if (!damageInfo.collider) return;
      var destructible = damageInfo.collider.GetComponentInParent<IDestructible>();
      if (destructible == null) return;

      var hitData = new HitData();
      hitData.m_damage.m_damage = 20f;
      hitData.m_damage.m_blunt = 25f;
      hitData.m_point = damageInfo.collider.bounds.center;
      hitData.m_dir = damageInfo.velocity.normalized;
      hitData.m_hitType = HitData.HitType.Impact;
      hitData.m_pushForce = 0f;
      hitData.m_attacker = Player.m_localPlayer.GetZDOID();
      destructible.Damage(hitData);
#else
        LoggerProvider.LogWarning("Cannonball damage not implemented!");
#endif
    };

    [Header("Cannonball properties")]
    [Tooltip("Type of cannonball")]
    [SerializeField] public CannonballType cannonballType = CannonballType.Solid;

    [Header("Explosion Physics")]
    [SerializeField] private float explosionForce = 1200f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private LayerMask explosionLayerMask = ~0;

    [Header("Physics & Trajectory")]
    [SerializeField] private bool debugDrawTrajectory;
    [SerializeField] private ParticleSystem _explosionEffect;
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField] public float cannonBallDrag = 0.47f;
    [SerializeField] public bool CanPlayWindSound;
    public Rigidbody m_body;
    [CanBeNull] public Transform lastFireTransform;

    [SerializeField] private AudioSource _windAudioSource;
    [SerializeField] private float _explosionClipStartPosition = 0.1f;

    private readonly HashSet<(Collider, Collider)> _ignoredColliders = new();
    private readonly List<DamageInfo> _queuedDamageInfo = new();

    private readonly Collider[] allocatedColliders = new Collider[100];

    private CoroutineHandle _applyDamageCoroutine;
    private bool _canUseEffect;
    private List<Collider> _colliders = new();
    private Vector3 _currentVelocity;

    private CoroutineHandle _despawnCoroutine;

    private AudioSource _explosionAudioSource;
    private Transform _explosionParent;
    private bool _hasExitedMuzzle;
    private bool _hasExploded;

    private Vector3 _lastVelocity; // the last velocity before unity physics mutates it.
    private CannonController controller;


    public List<Collider> Colliders => GetColliders();

    public bool IsInFlight
    {
      get;
      private set;
    }

    private void Awake()
    {
      m_body = GetComponent<Rigidbody>();
      m_body.drag = cannonBallDrag;
      m_body.angularDrag = cannonBallDrag;
      _colliders = GetColliders();
      _explosionParent = transform.Find("explosion");
      _explosionAudioSource = _explosionParent.GetComponent<AudioSource>();
      _explosionEffect = transform.Find("explosion/explosion_effect").GetComponent<ParticleSystem>();
      _windAudioSource = transform.Find("wind_sound").GetComponent<AudioSource>();

      foreach (var localCollider in _colliders)
      {
        localCollider.includeLayers = LayerHelpers.CannonHitLayers;
      }

      lastFireTransform = null;
    }

    private void FixedUpdate()
    {
      _lastVelocity = m_body.velocity;
    }

    private void OnEnable()
    {
      _applyDamageCoroutine = new CoroutineHandle(this);
      _despawnCoroutine = new CoroutineHandle(this);
    }

    private void OnDestroy()
    {
      StopAllCoroutines();
    }

    private void OnCollisionEnter(Collision other)
    {
      OnHitHandleHitType(other, out _);
    }

    public CoroutineHandle GetCoroutineHandle() => new CoroutineHandle(this);

    public void PlayWindSound()
    {
      if (!CanPlayWindSound) return;
      if (!_windAudioSource) return;
      var clipLength = _windAudioSource.clip.length;
      _windAudioSource.time = Mathf.Max(0, clipLength / 2f);
      _windAudioSource.Play((long)0.1);
    }

    public void PlayExplosionSound()
    {
      if (!_explosionAudioSource) return;
      _explosionAudioSource.time = _explosionClipStartPosition;
      _explosionAudioSource.Play();
    }

    public void StopWindSound()
    {
      if (!CanPlayWindSound) return;
      if (!_windAudioSource || !_windAudioSource.isPlaying) return;
      _windAudioSource.Stop();
    }

    public void StopExplosionSound()
    {
      if (!_explosionAudioSource || !_explosionAudioSource.isPlaying) return;
      _explosionAudioSource.Stop();
    }

    public HitMaterial GetHitMaterialFromTransformName(Transform materialNameRoot)
    {
      var materialName = materialNameRoot.name;
      if (materialName.Contains("wood")) return HitMaterial.Wood;
      if (materialName.Contains("stone")) return HitMaterial.Stone;
      if (materialName.Contains("metal")) return HitMaterial.Metal;
      if (materialName.Contains("land")) return HitMaterial.Terrain;

      return HitMaterial.None;
    }

    /// <summary>
    /// Fallback for unhandled collisions we use the layer directly.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public HitMaterial GetHitMaterialFromLayer(GameObject obj)
    {
      var layer = obj.layer;
      if (LayerHelpers.TerrainLayer == layer) return HitMaterial.Terrain;
      if (LayerHelpers.DefaultLayer == layer) return HitMaterial.Wood;
      if (LayerHelpers.DefaultSmallLayer == layer) return HitMaterial.Wood;
      return HitMaterial.None;
    }

    public HitMaterial GetHitMaterial(Collision other)
    {
      var rootPrefab = GetPrefabRoot(other.collider.transform);
      var hitMaterial = GetHitMaterialFromTransformName(rootPrefab);

      if (hitMaterial == HitMaterial.None)
      {
        hitMaterial = GetHitMaterialFromLayer(other.collider.gameObject);
      }

      return hitMaterial;
    }

    /// <summary>
    /// A velocity multiplier that is between 0 and 1. 0 being complete loss of velocity and 1 being no change.
    /// </summary>
    /// <param name="hitMaterial"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public float GetHitMaterialVelocityMultiplier(HitMaterial hitMaterial)
    {
      switch (hitMaterial)
      {
        case HitMaterial.Wood:
          return 0.75f;
        case HitMaterial.Metal:
          return 0.1f;
        case HitMaterial.Stone:
          return 0.2f;
        case HitMaterial.Terrain:
          return 1f;
        case HitMaterial.None:
        default:
          return 0.75f;
      }
    }

    /// <summary>
    /// Todo might not need this. Additionally this would need to be handled on a singleton layer to be optimized/iterated off of to apply damages.
    /// </summary>
    private void AddDamageToQueue(Collider otherCollider, Vector3 velocity, float force)
    {
      var damageInfo = new DamageInfo
      {
        collider = otherCollider,
        velocity = velocity,
        force = force,
        damage = Mathf.Clamp(10f * force, 10f, 60f)
      };
      _queuedDamageInfo.Add(damageInfo);
    }

    /// <summary>
    /// Higher speed will generate a larger impact area.
    /// </summary>
    private void GetCollisionsFromExplosion(Vector3 explosionOrigin, float force)
    {
      var count = Physics.OverlapSphereNonAlloc(explosionOrigin, explosionRadius, allocatedColliders, explosionLayerMask);
      for (var i = 0; i < count; i++)
      {
        var col = allocatedColliders[i];
        // Closest point on the collider to explosion
        var hitPoint = col.ClosestPoint(explosionOrigin);
        var dir = (hitPoint - explosionOrigin).normalized;
        var dist = Vector3.Distance(explosionOrigin, hitPoint);

        RaycastHit hitInfo;
        // Raycast from explosion to object
        if (Physics.Raycast(explosionOrigin, dir, out hitInfo, dist + 0.1f, explosionLayerMask))
        {
          if (hitInfo.collider == col)
          {
            var rb = col.attachedRigidbody;
            if (rb != null)
            {
              if (!PrefabNames.IsVehicle(rb.name) && !PrefabNames.IsVehiclePiecesCollider(rb.name))
              {
                rb.AddExplosionForce(force, explosionOrigin, explosionRadius);
              }
            }
            AddDamageToQueue(hitInfo.collider, Vector3.up, force);
          }
        }
      }
    }

    public IEnumerator ActivateExplosionEffect(float velocity)
    {
      if (!isActiveAndEnabled || !_explosionEffect) yield break;
      _explosionParent.SetParent(null);

      var explosionScalar = Vector3.Lerp(Vector3.one * 0.25f, Vector3.one * 2f, velocity / 90f);
      _explosionEffect.transform.localScale = explosionScalar;

      if (_canUseEffect)
      {
        _explosionEffect.Play();
        PlayExplosionSound();
        yield return new WaitUntil(() => _explosionEffect.isStopped);
      }
      else
      {
        yield return new WaitForSeconds(0.2f);
      }
      
      _explosionParent.SetParent(transform);
      _explosionParent.transform.localPosition = Vector3.zero;

      ResetCannonball();
      ReturnOrDestroyCannonball();
    }

    private void RestoreCollisionsForTempIgnoredColliders()
    {
      foreach (var localCollider in Colliders)
      foreach (var (thisCollider, otherCollider) in _ignoredColliders)
      {
        if (thisCollider == null || otherCollider == null) continue;
        if (localCollider != null)
        {
          Physics.IgnoreCollision(localCollider, thisCollider, false);
          Physics.IgnoreCollision(localCollider, otherCollider, false);
        }
        Physics.IgnoreCollision(thisCollider, otherCollider, false);
      }

      _ignoredColliders.Clear();
    }

    private void AddCollidersToIgnoreList(Collision other)
    {
      for (var index = 0; index < other.contactCount; index++)
      {
        var contactPoint = other.contacts[index];
        Physics.IgnoreCollision(contactPoint.thisCollider, contactPoint.otherCollider, true);
        _ignoredColliders.Add((contactPoint.thisCollider, contactPoint.otherCollider));
      }
    }

    public IEnumerator ScheduleDamageCoroutine()
    {
      yield return new WaitForFixedUpdate();
      foreach (var damageInfo in _queuedDamageInfo)
      {
        if (damageInfo.collider == null) continue;
        ApplyDamage?.Invoke(damageInfo);
      }
      _queuedDamageInfo.Clear();
    }

    private static bool CanPenetrateMaterial(float relativeVelocityMagnitude, HitMaterial hitMaterial)
    {
      var randomValue = Random.value;
      var velocityRandomizer = relativeVelocityMagnitude / 80f - randomValue;
      var canPenetrate = velocityRandomizer > 0.25f;

      if (hitMaterial == HitMaterial.Terrain)
      {
        canPenetrate = false;
      }

      return canPenetrate;
    }

    public static bool TryTriggerBarrelExplosion(Collider otherCollider)
    {
      var colliderName = otherCollider.name;
      if (colliderName != "barrel_explosion_collider") return false;
      var powderBarrel = otherCollider.GetComponentInParent<PowderBarrel>();
      if (powderBarrel == null) return false;
      powderBarrel.StartExplosion();

      return true;
    }

    /// <summary>
    /// Handles penetration of cannonball. Uses some randomization to determine if the structure is penetrated.
    /// </summary>
    private void OnHitHandleHitType(Collision other, out ProjectileHitType projectileHitType)
    {
      projectileHitType = ProjectileHitType.None;
      var isCollidingWithParent = controller && other.collider.transform.root == controller.transform.root;

      // allow ignoring parent colliders and do not restore.
      if (isCollidingWithParent)
      {
        foreach (var collider1 in _colliders)
        {
          Physics.IgnoreCollision(other.collider, collider1 , true);
        }
        m_body.velocity = _lastVelocity;
        return;
      }

      // var colliderName = other.collider.name;
      

      var relativeVelocity = other.relativeVelocity;
      var relativeVelocityMagnitude = relativeVelocity.magnitude;

      var hitMaterial = GetHitMaterial(other);
      var hitMaterialVelocityMultiplier = GetHitMaterialVelocityMultiplier(hitMaterial);

      // makes penetration through a collider random.
      var canPenetrate = cannonballType == CannonballType.Solid && CanPenetrateMaterial(relativeVelocityMagnitude, hitMaterial);
      var nextVelocity = _lastVelocity;

      var wasBarrel = TryTriggerBarrelExplosion(other.collider);
      if (wasBarrel)
      {
        canPenetrate = true;
      }

      if (canPenetrate)
      {
        nextVelocity.z *= hitMaterialVelocityMultiplier;
        nextVelocity.x *= hitMaterialVelocityMultiplier;
      }

      // allows bailing on velocity mutation.
      var canMutateVelocity = true;

      if (cannonballType == CannonballType.Solid)
      {
        // do nothing.
        if (hitMaterial == HitMaterial.Terrain)
        {
          return;
        }

        if (canPenetrate)
        {
          projectileHitType = ProjectileHitType.Penetration;
          AddCollidersToIgnoreList(other);
          AddDamageToQueue(other.collider, nextVelocity, relativeVelocityMagnitude);
        }

        if (!canPenetrate)
        {
          if (_lastVelocity.magnitude > 50f)
          {
            projectileHitType = ProjectileHitType.Bounce;
            AddDamageToQueue(other.collider, nextVelocity, relativeVelocityMagnitude);
          }
          else
          {
            canMutateVelocity = false;
          }
        }
      }

      if (cannonballType == CannonballType.Explosive)
      {
        if (!_hasExploded && Vector3.Distance(controller.transform.position, transform.position) > 3f)
        {
          projectileHitType = ProjectileHitType.Explosion;
          GetCollisionsFromExplosion(transform.position, relativeVelocityMagnitude);
          StartCoroutine(ActivateExplosionEffect(relativeVelocityMagnitude));
          _hasExploded = true;
          nextVelocity.x = 0f;
          nextVelocity.z = 0f;
        }
        else
        {
          canMutateVelocity = false;
        }
      }

      // schedule the damage task.
      // TODO migrate to a singleton scheduler
      if (!_applyDamageCoroutine.IsRunning && (projectileHitType == ProjectileHitType.Explosion || projectileHitType == ProjectileHitType.Penetration))
      {
        _applyDamageCoroutine.Start(ScheduleDamageCoroutine());
      }

      if (canMutateVelocity)
      {
        m_body.velocity = nextVelocity;
      }
    }

    public List<Collider> GetColliders()
    {
      if (_colliders == null || _colliders.Count == 0)
      {
        GetComponentsInChildren(true, _colliders);
      }
      return _colliders;
    }

    public void Load(Transform loader)
    {
      if (loader == null)
      {
        LoggerProvider.LogWarning("Cannonball loader is null!");
        return;
      }

      transform.position = loader.position;
      transform.rotation = loader.rotation;
      // Enable all cached colliders
      foreach (var col in Colliders) col.enabled = true;

      // todo figure out why the heck this is suddenly null.
      if (!m_body)
      {
        m_body = GetComponent<Rigidbody>();
      }

      if (m_body)
      {
        m_body.isKinematic = true;
        m_body.useGravity = false;
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
      }

      _hasExitedMuzzle = false;
      IsInFlight = false;

      ResetCannonball();
    }

    public void Fire(Vector3 velocity, Transform fireTransform, CannonController cannonController, int firingIndex)
    {
      if (!m_body)
      {
        LoggerProvider.LogWarning("Cannonball has no rigidbody!");
        return;
      }
      _canUseEffect = firingIndex == 0;
      
      if (_canUseEffect)
      {
        PlayWindSound();
      }
      
      lastFireTransform = fireTransform;

      controller = cannonController;

      // Enable all colliders (just in case)
      foreach (var col in _colliders) col.enabled = true;

      _hasExploded = false;

      // fix explosion effect position.   
      _explosionParent.SetParent(transform);
      _explosionParent.transform.localPosition = Vector3.zero;

      m_body.isKinematic = false;
      m_body.useGravity = true;
      m_body.velocity = Vector3.zero;
      m_body.angularVelocity = Vector3.zero;
      IsInFlight = true;

      m_body.AddForce(velocity, ForceMode.VelocityChange);
      _currentVelocity = m_body.velocity; // For custom gravity

      m_body.includeLayers = LayerHelpers.CannonHitLayers;

      _hasExitedMuzzle = false;
      _despawnCoroutine.Start(AutoDespawnCoroutine());
    }

    private IEnumerator AutoDespawnCoroutine()
    {
      yield return new WaitForSeconds(10f);

      yield return new WaitUntil(() => _explosionEffect.isStopped);
      ResetCannonball();
      ReturnOrDestroyCannonball();
    }

    public void ReturnOrDestroyCannonball()
    {
      if (controller != null && controller.isActiveAndEnabled)
      {
        controller.ReturnCannonballToPool(this);
      }
      else
      {
        // destroy self if the cannon gets broken.
        Destroy(gameObject);
      }
    }

    public void ResetCannonball()
    {
      StopExplosionSound(); 
      StopWindSound();

      if (_queuedDamageInfo.Count > 0)
      {
        _queuedDamageInfo.Clear();
      }

      RestoreCollisionsForTempIgnoredColliders();

      _applyDamageCoroutine.Stop();
      _despawnCoroutine.Stop();
      _applyDamageCoroutine.Stop();

      _hasExploded = false;
      _canUseEffect = true;
      IsInFlight = false;

      if (m_body)
      {
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
        m_body.isKinematic = true;
        m_body.includeLayers = -1;
        m_body.useGravity = false;
      }

      // Disable all colliders
      foreach (var col in _colliders) col.enabled = false;
    }

    public struct DamageInfo
    {
      public Collider collider;
      public float force;
      public Vector3 velocity;
      public float damage;
    }
  }
}