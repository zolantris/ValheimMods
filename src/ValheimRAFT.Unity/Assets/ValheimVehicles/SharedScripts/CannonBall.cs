// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
# if !UNITY_2022 && !UNITY_EDITOR
using ValheimVehicles.Controllers;
using StructLinq;
#endif

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

    [CanBeNull] public static AudioClip ImpactSoundOverride;

    // audio config.
    public static float ExplosionAudioVolume = 1f;
    public static bool HasExplosionAudio = true;

    public static float CannonballWindAudioVolume = 0.1f;
    public static bool HasCannonballWindAudio = true;

    public static float CannonballMass = 26f;

    [SerializeField] private ParticleSystem _explosionEffect;
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField] private AudioClip _impactSound;

    [SerializeField] private AudioSource _windAudioSource;
    public static float CannonBallDrag = 0.47f;

    [Header("Cannonball properties")]
    [Tooltip("Type of cannonball")]
    [SerializeField] public CannonballType cannonballType = CannonballType.Solid;
    [SerializeField] public bool CanPlayWindSound;

    [Header("Physics & Trajectory")]
    [SerializeField] private bool debugDrawTrajectory;

    [Header("Explosion Physics")]
    [SerializeField] private float explosionForce = 1200f;
    [CanBeNull] public Transform lastFireTransform;
    public Rigidbody m_body;
    private readonly float _explosionClipStartPosition = 0.1f;

    private readonly HashSet<(Collider, Collider)> _ignoredColliders = new();
    private readonly float _impactSoundEndTime = 1.2f;
    private readonly float _impactSoundStartTime = 0.02f;

    private readonly Collider[] allocatedColliders = new Collider[100];
    private readonly LayerMask explosionLayerMask = ~0;
    private readonly float explosionRadius = 6f;
    private bool _canHit = true;

    private bool _canUseEffect;

    private readonly List<Collider> _colliders = new();
    private CoroutineHandle _despawnCoroutine;

    private AudioSource _explosionAudioSource;
    private Transform _explosionParent;
    [CanBeNull] public Vector3? _fireOrigin;
    private bool _hasExitedMuzzle;
    private bool _hasExploded;
    private CoroutineHandle _impactSoundCoroutine;

    private Vector3 _lastVelocity; // the last velocity before unity physics mutates it.
    private CannonController controller;
    public HashSet<Transform> IgnoredTransformRoots = new();
    public SphereCollider sphereCollider;
    private GameObject meshGameObject;
    public List<Collider> Colliders => TryGetColliders();

#if !UNITY_2022 && !UNITY_EDITOR
    public VehicleZSyncTransform m_customZSyncTransform;
#endif

    public bool CanHit => _canHit && _fireOrigin != null && isActiveAndEnabled;

    public bool IsInFlight
    {
      get;
      private set;
    }

    private void Awake()
    {
      InitCoroutines();

      if (ImpactSoundOverride != null)
      {
        _impactSound = ImpactSoundOverride;
      }

#if UNITY_EDITOR
      HasCannonballWindAudio = CanPlayWindSound;
#endif
#if !UNITY_2022 && !UNITY_EDITOR
      m_customZSyncTransform = GetComponent<VehicleZSyncTransform>();
#endif
      meshGameObject = transform.Find("cannonball_mesh").gameObject;
      m_body = GetComponent<Rigidbody>();
      m_body.mass = CannonballMass;
      m_body.drag = CannonBallDrag;
      m_body.angularDrag = CannonBallDrag;
      TryGetColliders();

      _explosionParent = transform.Find("explosion");
      _explosionAudioSource = _explosionParent.GetComponent<AudioSource>();
      _explosionEffect = transform.Find("explosion/explosion_effect").GetComponent<ParticleSystem>();
      _windAudioSource = transform.Find("wind_sound").GetComponent<AudioSource>();

      foreach (var localCollider in Colliders)
      {
        localCollider.includeLayers = LayerHelpers.CannonHitLayers;
      }

      lastFireTransform = null;

      ResetCannonball();
    }

    private void FixedUpdate()
    {
      if (m_body.isKinematic) return;
      if (m_body.velocity == Vector3.zero) return;
      _lastVelocity = m_body.velocity;
    }

    private void OnEnable()
    {
      InitCoroutines();
    }

    private void OnDestroy()
    {
      StopAllCoroutines();
    }

    private void OnCollisionEnter(Collision other)
    {
      OnHitHandler(other);
    }

    private void InitCoroutines()
    {
      _despawnCoroutine ??= new CoroutineHandle(this);
      _impactSoundCoroutine ??= new CoroutineHandle(this);
    }

    public CoroutineHandle GetCoroutineHandle()
    {
      return new CoroutineHandle(this);
    }

    public void PlayWindSound()
    {
      if (!HasCannonballWindAudio) return;
      if (!_windAudioSource) return;
      _windAudioSource.volume = CannonballWindAudioVolume;
      var clipLength = _windAudioSource.clip.length;
      _windAudioSource.time = Mathf.Max(0, clipLength / 2f);
      _windAudioSource.Play((long)0.1f);
    }

    public void PlayExplosionSound()
    {
      if (!HasExplosionAudio) return;
      if (!_explosionAudioSource) return;
      if (_explosionAudioSource.isPlaying)
      {
        _explosionAudioSource.Stop();
      }

      _explosionAudioSource.volume = ExplosionAudioVolume;
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

    public bool IsColliderRootInIgnoredTransform(Transform colliderTransform)
    {
      var colliderRoot = ValheimCompatibility.GetPrefabRoot(colliderTransform);
      foreach (var ignoredTransformRoot in IgnoredTransformRoots)
      {
        if (colliderRoot == ignoredTransformRoot)
        {
          return true;
        }
        if (colliderTransform == ignoredTransformRoot)
        {
          return true;
        }
      }
      return false;
    }

    public HitMaterial GetHitMaterial(Collision other)
    {
      var rootPrefab = ValheimCompatibility.GetPrefabRoot(other.collider.transform);
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
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
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
    /// Higher speed will generate a larger impact h.
    ///
    /// Todo this might not be needed as MineRock5/MineRock can do AOE damage. But other things like NPCs need it. Doing a duplicate AOE would cause problems so maybe radius should not be included if a single minerock was hit.
    /// </summary>
    private void GetCollisionsFromExplosion(Vector3 explosionOrigin, float force)
    {
      var count = Physics.OverlapSphereNonAlloc(explosionOrigin, explosionRadius, allocatedColliders, explosionLayerMask);
      for (var i = 0; i < count; i++)
      {
        var col = allocatedColliders[i];
        // Closest point on the collider to explosion
        var hitPoint = col.ClosestPointOnBounds(explosionOrigin);
        var dir = (hitPoint - explosionOrigin).normalized;
        var dist = Vector3.Distance(explosionOrigin, hitPoint);

        RaycastHit hitInfo;
        // Raycast from explosion to object
        var hasHitMineRock = false;
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
            var damageInfo = CannonballHitScheduler.GetDamageInfoForHit(this, hitInfo.collider, hitPoint, dir, Vector3.up * 40f, Mathf.Max(30f, force), true);
            var hitMineRockLocal = damageInfo.isMineRock5Hit || damageInfo.isMineRockHit;

            // This logic prevents a cascade of Damage calls only one minerock should be damaged otherwise every hit triggers a minerock damage itself.
            if (!hasHitMineRock)
            {
              if (hitMineRockLocal)
              {
                hasHitMineRock = hitMineRockLocal;
              }
              CannonballHitScheduler.AddDamageToQueue(damageInfo);
            }
            else if (hasHitMineRock && !hitMineRockLocal)
            {
              CannonballHitScheduler.AddDamageToQueue(damageInfo);
            }
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

    public IEnumerator ImpactEffectCoroutine(float impactVelocity)
    {
      var timer = _impactSoundStartTime;
      var lerpedVolume = Mathf.Lerp(0f, 0.3f, impactVelocity / 90f);
      var lerpedPitch = Mathf.Lerp(1f, 1.2f, impactVelocity / 90f);

      _explosionAudioSource.time = _impactSoundStartTime;
      _explosionAudioSource.pitch = lerpedPitch;
      _explosionAudioSource.volume = lerpedVolume;
      _explosionAudioSource.PlayOneShot(_impactSound);
      while (timer < _impactSoundEndTime)
      {
        timer += Time.deltaTime;
        yield return null;
      }
      _explosionAudioSource.Stop();
    }

    // to be called by CannonballHitScheduler
    public void StartImpactEffectAudio(float impactVelocity)
    {
      _impactSoundCoroutine.Start(ImpactEffectCoroutine(impactVelocity));
    }

    public bool IsTrackedCannonball(Collision other)
    {
      return controller._trackedLoadedCannonballs.Any(controllerTrackedLoadedCannonball => controllerTrackedLoadedCannonball.sphereCollider == other.collider);
    }

    public bool IsCollidingWithRoot(Transform colliderTransform)
    {
      var isCollidingWithRoot = controller && colliderTransform.root == controller.transform.root;
      return isCollidingWithRoot;
    }

    public bool IsCollidingWithPrefabRoot(Transform colliderTransform)
    {
      return ValheimCompatibility.GetPrefabRoot(colliderTransform) == ValheimCompatibility.GetPrefabRoot(transform);
    }

    public bool IsCannonballCollider(Collider otherCollider)
    {
      if (otherCollider.name.StartsWith("cannonball"))
      {
        return true;
      }
      return false;
    }

    public bool TryBailAndIgnoreCollider(Collision other)
    {
      var colliderTransform = other.collider.transform;
      // allow ignoring parent colliders and do not restore.
      if (IsCannonballCollider(other.collider) || IsCollidingWithRoot(colliderTransform) || IsCollidingWithPrefabRoot(colliderTransform) || IsColliderRootInIgnoredTransform(colliderTransform) || IsTrackedCannonball(other))
      {
        foreach (var collider1 in Colliders)
        {
          Physics.IgnoreCollision(other.collider, collider1, true);
        }
        if (_lastVelocity != Vector3.zero)
        {
          m_body.velocity = _lastVelocity;
        }
        return true;
      }

      return false;
    }

    /// <summary>
    /// Handles penetration of cannonball. Uses some randomization to determine if the structure is penetrated.
    /// </summary>
    private void OnHitHandler(Collision other)
    {
      if (!CanHit || _hasExploded || !IsInFlight || !_fireOrigin.HasValue) return;
      if (!sphereCollider.gameObject.activeInHierarchy) return;
      if (TryBailAndIgnoreCollider(other)) return;

      // do nothing at lastvelocity zero. This is a issue likely.
      if (_lastVelocity == Vector3.zero) return;

      var otherCollider = other.collider;
      var hitPoint = sphereCollider.ClosestPoint(otherCollider.bounds.center);
      var direction = (otherCollider.bounds.center - hitPoint).normalized;
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
        if (!_impactSoundCoroutine.IsRunning)
        {
          CannonballHitScheduler.ScheduleImpactSound(this, relativeVelocityMagnitude);
        }

        if (canPenetrate)
        {
          AddCollidersToIgnoreList(other);
          CannonballHitScheduler.AddDamageToQueue(this, other.collider, hitPoint, direction, nextVelocity, relativeVelocityMagnitude, false);
          canMutateVelocity = true;
        }

        // Physics takes over (but we clamp some values)
        if (!canPenetrate)
        {
          if (_lastVelocity.magnitude > 20f)
          {
            CannonballHitScheduler.AddDamageToQueue(this, other.collider, hitPoint, direction, nextVelocity, relativeVelocityMagnitude, false);
          }
          // clamp velocity but allow physics for downwards velocity.
          // var velocity = m_body.velocity;
          // nextVelocity.y = velocity.y;
          // nextVelocity.x = Mathf.Clamp(velocity.x * 0.05f, -1f, 1f);
          // nextVelocity.z = Mathf.Clamp(m_body.velocity.z * 0.05f, -1f, 1f);
          m_body.velocity = Vector3.zero;
          // bail and prevent hits.
          IsInFlight = false;
          _canHit = false;
          return;
        }
      }

      if (cannonballType == CannonballType.Explosive)
      {
        if (!_hasExploded && Vector3.Distance(_fireOrigin.Value, transform.position) > 3f)
        {
          // addition impact damage first
          CannonballHitScheduler.AddDamageToQueue(this, other.collider, hitPoint, direction, nextVelocity, relativeVelocityMagnitude, true);
          // explosion next

          GetCollisionsFromExplosion(transform.position, relativeVelocityMagnitude);
          StartCoroutine(ActivateExplosionEffect(relativeVelocityMagnitude));
          _hasExploded = true;
          nextVelocity.x = 0f;
          nextVelocity.z = 0f;

          // bail and prevent hits.
          IsInFlight = false;
          _canHit = false;
        }
        else
        {
          // physics fully takes over.
          canMutateVelocity = false;
        }
      }

      if (canMutateVelocity)
      {
        m_body.velocity = nextVelocity;
      }
    }

    public List<Collider> TryGetColliders()
    {
      if (_colliders.Count > 0) return _colliders;
      GetComponentsInChildren(true, _colliders);
      sphereCollider = GetComponentInChildren<SphereCollider>(true);
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

      _hasExitedMuzzle = false;
      IsInFlight = false;

      ResetCannonball();
    }

    public IEnumerator FireCannonball(Vector3 velocity, Transform fireTransform, int firingIndex)
    {
      meshGameObject.SetActive(true);

      // must set booleans after fixedupdate which will prevent colliders from triggering earlier than wanted.
      yield return new WaitForFixedUpdate();


      // allows collisions now.
      _canHit = true;
      // allow effect for only first index. Otherwise it doubles up on hits with cannons firing from same-explosive.
      _canUseEffect = firingIndex == 0;

      if (_canUseEffect)
      {
        PlayWindSound();
      }

      lastFireTransform = fireTransform;
      _fireOrigin = fireTransform.position;


      // Enable all colliders (just in case)
      foreach (var col in Colliders) col.enabled = true;

      _hasExploded = false;

      // fix explosion effect position.   
      _explosionParent.SetParent(transform);
      _explosionParent.transform.localPosition = Vector3.zero;

      UpdateCannonballPhysics(true);

      if (m_body.isKinematic)
      {
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
      }
      IsInFlight = true;

      m_body.AddForce(velocity, ForceMode.VelocityChange);
      _lastVelocity = velocity; // For custom gravity

      m_body.includeLayers = LayerHelpers.CannonHitLayers;

      _hasExitedMuzzle = false;
      _despawnCoroutine.Start(AutoDespawnCoroutine());
    }

    public void Fire(Vector3 velocity, Transform fireTransform, CannonController cannonController, int firingIndex)
    {
      if (!m_body)
      {
        LoggerProvider.LogWarning("Cannonball has no rigidbody!");
        return;
      }
      controller = cannonController;
      StartCoroutine(FireCannonball(velocity, fireTransform, firingIndex));
    }

    private IEnumerator AutoDespawnCoroutine()
    {
      yield return new WaitForSeconds(10f);

      if (_explosionEffect.isPlaying)
      {
        yield return new WaitUntil(() => _explosionEffect.isStopped);
      }

      ReturnOrDestroyCannonball();
      // do not allow cancelling self while executing function.
      ResetCannonball(true);
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

    public void UpdateCannonballPhysics(bool isFiring)
    {
      if (!m_body) return;
      var hasKinematic = !isFiring;

#if !UNITY_2022 && !UNITY_EDITOR
      if (m_customZSyncTransform)
      {
        m_customZSyncTransform.SetKinematic(hasKinematic);
        m_customZSyncTransform.SetGravity(isFiring);
        return;
      }
#endif
      m_body.isKinematic = hasKinematic;
      m_body.useGravity = isFiring;
    }

    public void ResetCannonball(bool isFromDespawn = false)
    {
      StopExplosionSound();
      StopWindSound();

      RestoreCollisionsForTempIgnoredColliders();

      if (!isFromDespawn)
      {
        _despawnCoroutine.Stop();
      }

      _canHit = false;
      _fireOrigin = null;

      _hasExploded = false;
      _canUseEffect = true;
      IsInFlight = false;

      if (m_body)
      {
        if (!m_body.isKinematic)
        {
          m_body.velocity = Vector3.zero;
          m_body.angularVelocity = Vector3.zero;
        }
        m_body.isKinematic = true;
        UpdateCannonballPhysics(false);
        m_body.includeLayers = -1;
      }

      // disable the mesh object so it does not show the cannonball floating near the prefab.
      meshGameObject.SetActive(false);
      // Disable all colliders
      foreach (var col in Colliders) col.enabled = false;
    }
  }
}