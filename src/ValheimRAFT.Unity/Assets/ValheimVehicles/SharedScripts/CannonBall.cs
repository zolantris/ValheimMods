// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

#endregion

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
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

    public static float ExplosionAudioVolume = 1f;
    public static bool HasExplosionAudio = true;

    private readonly HashSet<(Collider, Collider)> _ignoredColliders = new();

    private readonly Collider[] allocatedColliders = new Collider[100];
    private bool _canHit = true;

    private bool _canUseEffect;
    private Vector3 _currentVelocity;

    private CoroutineHandle _despawnCoroutine;

    private AudioSource _explosionAudioSource;
    [SerializeField] private readonly float _explosionClipStartPosition = 0.1f;
    [SerializeField] private ParticleSystem _explosionEffect;
    private Transform _explosionParent;
    [SerializeField] private AudioClip _explosionSound;
    [CanBeNull] public Vector3? _fireOrigin = null;
    private bool _hasExitedMuzzle;
    private bool _hasExploded;
    [SerializeField] private AudioClip _impactSound;
    private CoroutineHandle _impactSoundCoroutine;
    private readonly float _impactSoundEndTime = 0.2f;
    private readonly float _impactSoundStartTime = 0.02f;

    private Vector3 _lastVelocity; // the last velocity before unity physics mutates it.

    [SerializeField] private AudioSource _windAudioSource;
    [SerializeField] public float cannonBallDrag = 0.47f;

    [Header("Cannonball properties")]
    [Tooltip("Type of cannonball")]
    [SerializeField] public CannonballType cannonballType = CannonballType.Solid;
    [SerializeField] public bool CanPlayWindSound;
    private CannonController controller;

    [Header("Physics & Trajectory")]
    [SerializeField] private bool debugDrawTrajectory;

    [Header("Explosion Physics")]
    [SerializeField] private float explosionForce = 1200f;
    [SerializeField] private readonly LayerMask explosionLayerMask = ~0;
    [SerializeField] private readonly float explosionRadius = 6f;
    [CanBeNull] public Transform lastFireTransform;
    public Rigidbody m_body;
    private SphereCollider sphereCollider;

    public List<Collider> Colliders
    {
      get;
    } = new();

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

      m_body = GetComponent<Rigidbody>();
      m_body.drag = cannonBallDrag;
      m_body.angularDrag = cannonBallDrag;
      TrySetColliders();

      _explosionParent = transform.Find("explosion");
      _explosionAudioSource = _explosionParent.GetComponent<AudioSource>();
      _explosionEffect = transform.Find("explosion/explosion_effect").GetComponent<ParticleSystem>();
      _windAudioSource = transform.Find("wind_sound").GetComponent<AudioSource>();

      foreach (var localCollider in Colliders)
      {
        localCollider.includeLayers = LayerHelpers.CannonHitLayers;
      }

      lastFireTransform = null;
    }

    private void FixedUpdate()
    {
      if (m_body.isKinematic) return;
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
      if (!CanPlayWindSound) return;
      if (!_windAudioSource) return;
      _windAudioSource.volume = ExplosionAudioVolume;
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
            CannonballHitScheduler.AddDamageToQueue(this, hitInfo.collider, hitPoint, dir, Vector3.up * 40f, Mathf.Max(30f, force), true);
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

    public IEnumerator ImpactEffectCoroutine()
    {
      var timer = _impactSoundStartTime;
      _explosionAudioSource.time = _impactSoundStartTime;
      _explosionAudioSource.PlayOneShot(_impactSound, 0.1f);
      while (timer < _impactSoundEndTime)
      {
        timer += Time.deltaTime;
        yield return null;
      }
      _explosionAudioSource.Stop();
    }

    /// <summary>
    /// Handles penetration of cannonball. Uses some randomization to determine if the structure is penetrated.
    /// </summary>
    private void OnHitHandler(Collision other)
    {
      if (!CanHit || !_fireOrigin.HasValue) return;
      if (!sphereCollider.gameObject.activeInHierarchy) return;
      var isCollidingWithParent = controller && other.collider.transform.root == controller.transform.root;

      // allow ignoring parent colliders and do not restore.
      if (isCollidingWithParent)
      {
        foreach (var collider1 in Colliders)
        {
          Physics.IgnoreCollision(other.collider, collider1, true);
        }
        m_body.velocity = _lastVelocity;
        return;
      }

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
          _impactSoundCoroutine.Start(ImpactEffectCoroutine());
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

          canMutateVelocity = true;
          // clamp velocity but allow physics for downwards velocity.
          var velocity = m_body.velocity;
          nextVelocity.y = velocity.y;
          nextVelocity.x = Mathf.Clamp(velocity.x * 0.05f, -1f, 1f);
          nextVelocity.z = Mathf.Clamp(m_body.velocity.z * 0.05f, -1f, 1f);
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

    public void TrySetColliders()
    {
      GetComponentsInChildren(true, Colliders);
      sphereCollider = GetComponentInChildren<SphereCollider>(true);
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

      controller = cannonController;

      // Enable all colliders (just in case)
      foreach (var col in Colliders) col.enabled = true;

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
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
        m_body.isKinematic = true;
        m_body.includeLayers = -1;
        m_body.useGravity = false;
      }

      // Disable all colliders
      foreach (var col in Colliders) col.enabled = false;
    }
  }
}