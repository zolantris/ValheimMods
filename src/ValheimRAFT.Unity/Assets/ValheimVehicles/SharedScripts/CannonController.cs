// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
  public class CannonController : MonoBehaviour
  {
    [Header("Cannonball")]
    [Tooltip("Prefab asset for the cannonball projectile (assign in inspector or dynamically).")]
    // Will be set from asset and cached for instantiation
    private static Cannonball CannonballSolidPrefab;
    private static Cannonball CannonballExplosivePrefab;
    private static bool hasRunSetup;
    [Tooltip("Optional: prefab asset for this instance (overrides static for this controller only).")]
    [SerializeField] private GameObject CannonballSolidPrefabAssetLocal;
    [SerializeField] private GameObject CannonballExplosivePrefabAssetLocal;

    [Header("Transforms")]
    [Tooltip("Where the cannonball will be loaded and fired from.")]
    [SerializeField] private Transform projectileLoader;
    [Tooltip("Where muzzle flash and effects are triggered.")]
    [SerializeField] private Transform muzzleFlashPoint;
    [Tooltip("Transform to use for fire direction (usually barrel/shooter).")]
    [SerializeField] private Transform cannonShooterTransform;
    [SerializeField] private Transform cannonScalarTransform;
    [Tooltip("Speed (m/s) for cannonball launch.")]
    [SerializeField] private float cannonballSpeed = 90f; // 90m/s is standard.

    [Header("Ammunition")]
    [Tooltip("Maximum shells this cannon can hold.")]
    [SerializeField] public int maxAmmo = 12;
    [Tooltip("Current shells. This should be read-only, but exposed for testing")]
    [SerializeField] private int _currentAmmo;
    [Tooltip("How many shells are reloaded at once.")]
    [SerializeField] private int reloadQuantity = 1;
    [Tooltip("Time to reload (seconds).")]
    [SerializeField] private float reloadTime = 5f;
    [Tooltip("Automatically reload when fired?")]
    [SerializeField] private bool autoReload = true;
    [Tooltip("Ammunition type")]
    [SerializeField] private Cannonball.CannonballType ammoType = Cannonball.CannonballType.Solid;

    [Header("Effects & Sounds")]
    [Tooltip("Particle effect played at muzzle flash.")]
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [Tooltip("Sound when firing.")]
    [SerializeField] private AudioClip fireClip;
    [Tooltip("Sound when reloading.")]
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] public float fireClipPitch = 1f;
    [SerializeField] public float reloadClipPitch = 0.75f;

    [Header("Cannon Animations")]
    [Tooltip("Speed at which the cannon aims to adjust to target position.")]
    [SerializeField] public float aimingSpeed = 5f;

    [Header("Pooling")]
    [Tooltip("Minimum cannonballs kept in pool (typically 1 per cannon).")]
    [SerializeField] private int minPoolSize = 1;
    [SerializeField] public Transform cannonAudioSourceTransform;
    [SerializeField] public Transform muzzleFlashEffectTransform;


    [SerializeField] public bool hasLoaded = true;

    [Header("Cannon Targeting")]
    [Tooltip("Target To fire upon. Will aim for the position center.")]
    [SerializeField] public Transform firingTarget;
    [Tooltip("Generic firing coordinates")]
    [SerializeField] public Vector3 firingCoordinates;
    [Tooltip("Max Range Cannons can fire at.")]
    [SerializeField] public float maxFiringRange = 150f;
    [Tooltip("Max degrees a cannon can pivot to hit a object")]
    [SerializeField] public float maxFiringRotationY = 30f;
    [Tooltip("Max pitch is the degrees the barrel can aim upwards")]
    [SerializeField] public float maxFiringPitch = 25f;
    [Tooltip("Min pitch is the degrees the barrel can aim downwards")]
    [SerializeField] public float minFiringPitch = 10f;
    [SerializeField] public bool canRotateFiringRangeY = true;


    [Header("Barrel Ammo Logic")]
    [SerializeField] private bool hasNearbyPowderBarrel;
    [SerializeField] private float barrelSupplyRadius = 5f;

    // barrel check timers
    public float LastBarrelCheckTime;
    public float BarrelCheckInterval = 30f;

    // --- State ---
    private readonly HashSet<Cannonball> _activeCannonballs = new();
    private readonly List<Cannonball> _trackedLoadedCannonballs = new List<Cannonball>();
    private AudioSource _audioSource;
    private bool _canFire;
    private Queue<Cannonball> _cannonballPool = new Queue<Cannonball>();
    private Coroutine _cleanupCoroutine;

    private Collider[] _colliders;
    private Quaternion _defaultShooterLocalRotation;
    private float _lastAllowedYaw = float.NaN;
    private Cannonball _loadedCannonball;
    private Quaternion _recoilRotation;
    private Quaternion _reloadRotation;
    private Quaternion _targetShooterLocalRotation;
    public Func<Vector3?> GetFiringTargetPosition = () => null;

    public int CurrentAmmo { get => _currentAmmo; private set => _currentAmmo = value; }
    public bool IsReloading { get; private set; }
    public bool IsLoaded => _loadedCannonball != null;

    public Cannonball.CannonballType AmmoType
    {
      get => ammoType;
      set
      {
        ammoType = value;
        SetupCannonballPrefab();
      }
    }

    private void Awake()
    {
      #if  UNITY_EDITOR
      // Must only be run in Unity Editor. We reset static values so it's like first to load for this object.
      CleanupCannonballPrefab();
      #endif
      _colliders = GetComponentsInChildren<Collider>(true);

      SetupTransforms();
      SetupCannonballPrefab();

      _audioSource = cannonAudioSourceTransform.GetComponent<AudioSource>();

      if (cannonShooterTransform != null)
      {
        _defaultShooterLocalRotation = cannonShooterTransform.localRotation;
        _recoilRotation = Quaternion.Euler(-2f, 0f, 0f) * _defaultShooterLocalRotation;
        _reloadRotation = Quaternion.Euler(10f, 0f, 0f) * _defaultShooterLocalRotation;
        _targetShooterLocalRotation = _defaultShooterLocalRotation;
      }


      if (!IsLoaded && hasLoaded)
      {
        _loadedCannonball = GetPooledCannonball();
        _loadedCannonball.Load(projectileLoader, muzzleFlashPoint);
      }

      InitializePool();

      GetFiringTargetPosition = HandleFiringTargetPositionDefault;
      // --- Setup shooter recoil rotation values ---
    }

    internal virtual void Start()
    {
      SetupCannonballPrefab();
      TryReload();
    }

    private void FixedUpdate()
    {
      if (LastBarrelCheckTime == 0 || LastBarrelCheckTime + BarrelCheckInterval < Time.fixedTime)
      {
         hasNearbyPowderBarrel = PowderBarrel.FindNearbyBarrels(transform.position, barrelSupplyRadius)?.Count > 0;
         LastBarrelCheckTime = Time.fixedTime;;
      }
      // Sync "loaded" (not-in-flight) cannonballs to loader, never parented!
      foreach (var ball in _trackedLoadedCannonballs)
      {
        if (ball != null && !ball.IsInFlight)
        {
          ball.transform.position = projectileLoader.position;
          ball.transform.rotation = projectileLoader.rotation;
        }
      }

      if (firingTarget != null || firingCoordinates != null)
      {
        AdjustFiringAngle();
      }
    }

    private Vector3? HandleFiringTargetPositionDefault()
    {
      if (firingTarget != null) return firingTarget.position;
      return null;
    }

    public event Action<int> OnAmmoChanged;
    public event Action OnFired;
    public event Action OnReloaded;

    private void SetupTransforms()
    {
      if (cannonScalarTransform == null)
      {
        cannonScalarTransform = transform.Find("scalar");
      }

      if (cannonAudioSourceTransform == null)
      {
        cannonAudioSourceTransform = transform.Find("scalar/cannon_shooter/cannon_shot_audio");
      }

      if (muzzleFlashEffectTransform == null)
      {
        muzzleFlashEffectTransform = transform.Find("scalar/cannon_shooter/muzzle_flash_effect");
      }

      if (muzzleFlashEffect == null)
      {
        muzzleFlashEffect = muzzleFlashEffectTransform.GetComponent<ParticleSystem>();
      }

      if (cannonShooterTransform == null)
        cannonShooterTransform = transform.Find("scalar/cannon_shooter");
      if (projectileLoader == null)
        projectileLoader = transform.Find("scalar/cannon_shooter/shooting_part/points/projectile_loader");

      if (muzzleFlashPoint == null)
      {
        muzzleFlashPoint = transform.Find("scalar/cannon_shooter/shooting_part/points/muzzle_flash");
      }
    }

    private static void CleanupCannonballPrefab()
    {
      #if UNITY_EDITOR
      hasRunSetup = false;
      // Only clean up runtime-created prefabs, never destroy inspector assets
      if (CannonballSolidPrefab != null && Application.isPlaying)
      {
        var go = CannonballSolidPrefab.gameObject;
        if (go != null && go.scene.IsValid())
        {
          Destroy(go);
        }
        CannonballSolidPrefab = null;
      }
      
      if (CannonballExplosivePrefab != null && Application.isPlaying)
      {
        var go = CannonballExplosivePrefab.gameObject;
        if (go != null && go.scene.IsValid())
        {
          Destroy(go);
        }
        CannonballExplosivePrefab = null;
      }
      #endif
    }

    private Cannonball SelectCannonballType()
    {
      if (!hasRunSetup)
      {
        SetupCannonballPrefab();
      }
      
      switch(ammoType)
      {
        case Cannonball.CannonballType.Solid:
          return CannonballSolidPrefab;
        case Cannonball.CannonballType.Explosive:
          return CannonballExplosivePrefab;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void InitCannonballPrefabAssets()
    {
      if (CannonballExplosivePrefab == null)
      {
        if (CannonballExplosivePrefabAssetLocal != null)
        {
          var go = Instantiate(CannonballExplosivePrefabAssetLocal);
          CannonballExplosivePrefab = go.GetComponent<Cannonball>();
        }
        else
        {
          LoggerProvider.LogWarning("No cannonball explosive prefab asset set. Please set one in CannonballExplosivePrefabAsset or CannonballExplosivePrefabAssetLocal.");
        }
      }
      
      if (CannonballSolidPrefab == null)
      {
        if (CannonballSolidPrefabAssetLocal != null)
        {
          var go = Instantiate(CannonballSolidPrefabAssetLocal);
          CannonballSolidPrefab = go.GetComponent<Cannonball>();
        }
        else
        {
          LoggerProvider.LogWarning("No cannonball explosive prefab asset set. Please set one in CannonballExplosivePrefabAsset or CannonballExplosivePrefabAssetLocal.");
        }
      }
    }

    private void SetupCannonballPrefab()
    {
      hasRunSetup = true;
      
      InitCannonballPrefabAssets();
      var selectedCannonball = SelectCannonballType();

      // already setup.
      if (selectedCannonball != null)
      {
        return;
      }

      IgnoreLocalColliders(selectedCannonball);

      var go = selectedCannonball.gameObject;
      var goTransform = go.transform;
      goTransform.position = Vector3.zero;
      goTransform.rotation = Quaternion.identity;
      go.SetActive(false);

      var rb = go.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.isKinematic = true;
        rb.useGravity = false;
      }
    }

    private void InitializePool()
    {
      _cannonballPool = new Queue<Cannonball>(minPoolSize);
      var selectedCannonball = SelectCannonballType();
      if (!selectedCannonball) return;
      for (var i = 0; i < minPoolSize; i++)
      {
        var go = Instantiate(selectedCannonball.gameObject, projectileLoader.position, projectileLoader.rotation, null);
        go.name = $"cannonball_queue_{ammoType}_{i}";
        var obj = go.GetComponent<Cannonball>();
        IgnoreLocalColliders(obj);
        go.SetActive(false);
        _cannonballPool.Enqueue(obj);
      }
    }

    private void IgnoreLocalColliders(Cannonball cannonball)
    {
      if (cannonball == null || _colliders == null) return;
      foreach (var localCollider in _colliders)
      {
        if (localCollider == null) continue;
        foreach (var cannonballCollider in cannonball.Colliders)
        {
          if (cannonballCollider == null) continue;
          Physics.IgnoreCollision(localCollider, cannonballCollider, true);
        }
      }
    }

    private Cannonball GetPooledCannonball()
    {
      if (_cannonballPool.Count > 0)
      {
        var ball = _cannonballPool.Dequeue();

        while (ball == null && _cannonballPool.Count > 0)
        {
          ball = _cannonballPool.Dequeue();
        }

        if (ball)
        {
          ball.gameObject.name = $"cannonball_{ammoType}_active_{_cannonballPool.Count}";
          ball.gameObject.SetActive(true);
          _trackedLoadedCannonballs.Add(ball);
          return ball;
        }
      }
      var selectedCannonball = SelectCannonballType();
      if (!selectedCannonball)
      {
        LoggerProvider.LogWarning("No cannonball prefab set. Please set one in CannonballSolidPrefab or CannonballExplosivePrefab.");
        return null;
      }
      
      var go = Instantiate(selectedCannonball.gameObject, projectileLoader.position, projectileLoader.rotation, null);
      var localCannonball = go.GetComponent<Cannonball>();
      IgnoreLocalColliders(localCannonball);

      go.gameObject.SetActive(true);

      // ignore colliders for all cannonballs.
      foreach (var ball in _trackedLoadedCannonballs)
      foreach (var otherCollider in ball.Colliders)
      {
        if (otherCollider == null) continue;
        foreach (var localCollider in localCannonball.Colliders)
        {
          if (localCollider == null) continue;
          Physics.IgnoreCollision(otherCollider, localCollider, true);
        }
      }

      _trackedLoadedCannonballs.Add(localCannonball);
      go.name = $"cannonball_active_{_trackedLoadedCannonballs.Count}";
      return localCannonball;
    }

    private void ReturnCannonballToPool(Cannonball ball)
    {
      _cannonballPool.Enqueue(ball);
      _activeCannonballs.Remove(ball);
      _trackedLoadedCannonballs.Remove(ball);

      ball.gameObject.SetActive(false);

      StartCleanupCoroutine();
    }

    /// <summary>
    /// Numerically finds the firing angle to hit the target, accounting for drag.
    /// </summary>
    public static bool CalculateBallisticAimWithDrag(
      Vector3 fireOrigin,
      Vector3 targetPosition,
      float launchSpeed,
      float drag,
      out Vector3 fireDirection,
      out float angleDegrees,
      int maxIterations = 100,
      float tolerance = 0.05f)
    {
      Vector3 delta = targetPosition - fireOrigin;
      float xzDist = new Vector2(delta.x, delta.z).magnitude;
      float y = delta.y;

      if (xzDist < 0.01f)
      {
        fireDirection = Vector3.zero;
        angleDegrees = 0;
        return false;
      }

      Vector3 dirXZ = new Vector3(delta.x, 0, delta.z).normalized;

      // ALLOW NEGATIVE ANGLES (downward)
      float low = -Mathf.PI / 4f;         // -45°
      float high = Mathf.PI / 2f;         // +90°
      bool found = false;
      float angle = 0;

      for (int i = 0; i < maxIterations; i++)
      {
        angle = (low + high) * 0.5f;
        float yAtTarget = SimulateProjectileHeightAtXZ(launchSpeed, drag, xzDist, angle);

        float diff = yAtTarget - y;

        if (Mathf.Abs(diff) < tolerance)
        {
          found = true;
          break;
        }

        if (diff > 0)
          high = angle; // Too high
        else
          low = angle; // Too low
      }

      if (!found)
      {
        fireDirection = Vector3.zero;
        angleDegrees = 0;
        return false;
      }

      fireDirection = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, Vector3.Cross(Vector3.up, dirXZ)) * dirXZ;
      angleDegrees = angle * Mathf.Rad2Deg;
      return true;
    }

    /// <summary>
    /// Simulates the Y (vertical) position after traveling horizontalDistance at a given launch angle, speed, and drag.
    /// </summary>
    private static float SimulateProjectileHeightAtXZ(
      float speed, float drag, float horizontalDistance, float angle)
    {
      // Simulate flight in XZ plane only
      float dt = 0.01f; // Smaller for more accuracy!
      float vx = speed * Mathf.Cos(angle);
      float vy = speed * Mathf.Sin(angle);

      float x = 0;
      float y = 0;

      while (x < horizontalDistance && y > -500 && x < 10000)
      {
        // Apply drag to velocity (Unity's model)
        vx *= Mathf.Exp(-drag * dt);
        vy = (vy + Physics.gravity.y * dt) * Mathf.Exp(-drag * dt);

        x += vx * dt;
        y += vy * dt;

        // Early out if we've dropped far below the target
        if (y < -50) break;
      }

      return y;
    }

    public void AdjustFiringAngle()
    {
      if (IsReloading) return;

      var fireOrigin = projectileLoader.position;
      var targetPosition = GetFiringTargetPosition();
      if (!targetPosition.HasValue) return;

      // --- YAW ---
      Vector3 toTarget = targetPosition.Value - cannonScalarTransform.position;
      toTarget.y = 0f; // flatten to XZ
      if (toTarget.sqrMagnitude < 0.01f) return;

      Quaternion desiredYawWorld = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
      float currentYaw = cannonScalarTransform.eulerAngles.y;
      float targetYaw = desiredYawWorld.eulerAngles.y;
      float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

      bool isWithinYawRange = Mathf.Abs(deltaYaw) <= maxFiringRotationY;

      // Only update yaw if within bounds
      if (canRotateFiringRangeY && isWithinYawRange)
      {
        _lastAllowedYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.fixedDeltaTime * aimingSpeed);
        cannonScalarTransform.rotation = Quaternion.Euler(0f, _lastAllowedYaw, 0f);
      }
      else if (!float.IsNaN(_lastAllowedYaw))
      {
        // Stay at last allowed yaw, do not follow out-of-range targets
        cannonScalarTransform.rotation = Quaternion.Euler(0f, _lastAllowedYaw, 0f);
      }
      // else: if never aimed at a valid target, stay as-is

      // --- PITCH (X/Elevation) ---
      if (isWithinYawRange && CalculateBallisticAimWithDrag(fireOrigin, targetPosition.Value, cannonballSpeed, CannonballSolidPrefab.cannonBallDrag, out var fireDir, out var angle))
      {
        // min pitch aims cannon down. Max pitch which is negative aims the cannon upwards.
        _targetShooterLocalRotation = Quaternion.Euler(Mathf.Clamp(-angle, -maxFiringPitch, minFiringPitch), 0f, 0f);
        _canFire = true;
      }
      else
      {
        _canFire = false;
        _targetShooterLocalRotation = Quaternion.identity;
      }

      cannonShooterTransform.localRotation = Quaternion.Lerp(
        cannonShooterTransform.localRotation,
        _targetShooterLocalRotation,
        Time.fixedDeltaTime * aimingSpeed
      );
    }

    public bool Fire()
    {
      if (IsReloading || _loadedCannonball == null || CurrentAmmo <= 0 || !hasNearbyPowderBarrel || !_canFire) return false;
      
      var targetPosition = GetFiringTargetPosition();
      if (!targetPosition.HasValue) return false;
      if (Vector3.Distance(targetPosition.Value, projectileLoader.position) > maxFiringRange) return false;

      IgnoreLocalColliders(_loadedCannonball);
      _loadedCannonball.transform.position = projectileLoader.position;
#if UNITY_EDITOR
      // Debug.DrawRay(projectileLoader.position, cannonShooterTransform.forward * 150f, Color.green, 1f);
#endif

      _loadedCannonball.Fire(
        cannonShooterTransform.forward * cannonballSpeed,
        muzzleFlashPoint.position,
        ReturnCannonballToPool);

      OnCannonballExitedMuzzle();

      _activeCannonballs.Add(_loadedCannonball);
      _trackedLoadedCannonballs.Remove(_loadedCannonball);
      _loadedCannonball = null;
      CurrentAmmo--;
      OnAmmoChanged?.Invoke(CurrentAmmo);

      if (_audioSource)
      {
        PlayFireClip();
        // Invoke(nameof(PlayFireClipDelayed), Random.Range(0.001f, 0.1f));
      }
      OnFired?.Invoke();

      StartCoroutine(RecoilCoroutine());
      return true;
    }

    [ContextMenu("Reload Cannon")]
    public void TryReload()
    {
      if (IsReloading || CurrentAmmo <= 0 || _loadedCannonball != null)
        return;
      StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator RecoilCoroutine()
    {
      var elapsed = 0f;
      var recoilUpwardAnimationDuration = 0.15f;
      var recoilReturnAnimationDuration = 0.15f;
      while (elapsed < recoilReturnAnimationDuration + recoilUpwardAnimationDuration)
      {
        var t = Mathf.Clamp01(elapsed / 2f / 0.15f); // 0..1
        elapsed += Time.deltaTime;

        // move towards recoil.
        if (elapsed < 0.1f)
        {
          cannonScalarTransform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.forward * -0.1f, t);
          if (cannonShooterTransform != null)
          {
            cannonShooterTransform.localRotation = Quaternion.Lerp(_targetShooterLocalRotation, _recoilRotation, Mathf.Clamp01(t));
          }
        }
        else
        {
          // move back to target position. (and then will reload afterwards) 
          if (cannonShooterTransform != null)
          {
            cannonShooterTransform.localRotation = Quaternion.Lerp(_targetShooterLocalRotation * _recoilRotation, _targetShooterLocalRotation, Mathf.Clamp01(t));
          }
          cannonScalarTransform.localPosition = Vector3.Lerp(Vector3.forward * -0.1f, Vector3.zero, t);
        }
        yield return null;
      }

      if (autoReload && CurrentAmmo > 0)
      {
        yield return ReloadCoroutine();
      }
      else
      {
        LoggerProvider.LogDev("No ammo left, not reloading.");
      }
    }

    private void FindNearbyPowderBarrel()
    {
      
    }

    private IEnumerator ReloadCoroutine()
    {
      if (!hasNearbyPowderBarrel)
      {
        
      }
      IsReloading = true;
      var elapsed = 0f;

      var startRotation = _targetShooterLocalRotation;
      var endRotation = Quaternion.Euler(_targetShooterLocalRotation.x + _recoilRotation.x, 0, 0);

      var safeReloadTime = Mathf.Clamp(reloadTime, 0.1f, 5f);
      while (elapsed < reloadTime)
      {
        var t = Mathf.Clamp01(elapsed * 2f / safeReloadTime); // 0..1
        elapsed += Time.deltaTime;
        if (cannonShooterTransform != null)
        {
          cannonShooterTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, Mathf.Clamp01(t));
        }
        yield return null; // Wait one frame
      }

      // must wait for reload, then continue with audio. reload must be a higher number otherwise it will conflict with firing audio.
      if (_audioSource.isPlaying)
      {
        _audioSource.Stop();
      }
      if (_audioSource)
      {
        _audioSource.pitch = reloadClipPitch;
        _audioSource.PlayOneShot(reloadClip);
      }

      for (int i = 0; i < reloadQuantity && CurrentAmmo - i > 0; i++)
      {
        if (_loadedCannonball == null)
        {
          _loadedCannonball = GetPooledCannonball();
          _loadedCannonball.Load(projectileLoader, muzzleFlashPoint);
        }
      }
      IsReloading = false;
      OnReloaded?.Invoke();
    }

    private void OnCannonballExitedMuzzle()
    {
      if (muzzleFlashEffect)
      {
        muzzleFlashEffect.transform.position = muzzleFlashPoint.position;
        muzzleFlashEffect.transform.rotation = muzzleFlashPoint.rotation;
        muzzleFlashEffect.Play();
      }
    }

    private void PlayFireClip()
    {
      _audioSource.pitch = fireClipPitch + Random.Range(-0.2f, 0.2f);
      _audioSource.PlayOneShot(fireClip);
    }

    private void StartCleanupCoroutine()
    {
      if (_cleanupCoroutine != null)
      {
        StopCoroutine(_cleanupCoroutine);
      }
      _cleanupCoroutine = StartCoroutine(CleanupExtraCannonballsAfterDelay());
    }

    private IEnumerator CleanupExtraCannonballsAfterDelay()
    {
      yield return new WaitForSeconds(10f);
      while (_cannonballPool.Count > minPoolSize)
      {
        var ball = _cannonballPool.Dequeue();
        Destroy(ball.gameObject);
      }
    }
  }
}