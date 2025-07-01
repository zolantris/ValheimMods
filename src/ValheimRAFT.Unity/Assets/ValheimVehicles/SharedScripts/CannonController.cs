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
     public class CannonController : MonoBehaviour
    {
        [Header("Cannonball")]
        [Tooltip("Prefab asset for the cannonball projectile (assign in inspector or dynamically).")]
        [CanBeNull] public static GameObject CannonballPrefabAsset;

        // Will be set from asset and cached for instantiation
        private static Cannonball CannonballPrefab;
        [Tooltip("Optional: prefab asset for this instance (overrides static for this controller only).")]
        [SerializeField] private GameObject CannonballPrefabAssetLocal;

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
        [SerializeField] public bool canRotateFiringRangeY = true;

        // --- State ---
        private readonly HashSet<Cannonball> _activeCannonballs = new();
        private readonly List<Cannonball> _trackedLoadedCannonballs = new List<Cannonball>();
        private AudioSource _audioSource;
        private Queue<Cannonball> _cannonballPool = new Queue<Cannonball>();
        private Coroutine _cleanupCoroutine;

        private Collider[] _colliders;
        private Quaternion _defaultShooterLocalRotation;
        private Cannonball _loadedCannonball;
        private Quaternion _recoilRotation;
        private Quaternion _reloadRotation;
        private Quaternion _targetShooterLocalRotation;
        public Func<Vector3?> GetFiringTargetPosition = () => null;

        public int CurrentAmmo { get => _currentAmmo; private set => _currentAmmo = value; }
        public bool IsReloading { get; private set; }
        public bool IsLoaded => _loadedCannonball != null;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>(true);

            SetupTransforms();
            CleanupCannonballPrefab();
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
            TryReload();
        }

        // private void Update()
        // {
        //     // Smoothly lerp shooter recoil/tilt
        //     if (shooterTransform != null && shooterTransform.localRotation != _targetShooterLocalRotation)
        //     {
        //         shooterTransform.localRotation = Quaternion.Lerp(
        //             shooterTransform.localRotation,
        //             _targetShooterLocalRotation,
        //             Time.deltaTime * _currentReturnSpeed
        //         );
        //         if (Quaternion.Angle(shooterTransform.localRotation, _targetShooterLocalRotation) < 0.1f)
        //             shooterTransform.localRotation = _targetShooterLocalRotation;
        //     }
        // }

        private void FixedUpdate()
        {
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
            
            if (cannonAudioSourceTransform ==null)
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
            // Only clean up runtime-created prefabs, never destroy inspector assets
            if (CannonballPrefab != null && Application.isPlaying)
            {
                var go = CannonballPrefab.gameObject;
                if (go != null && go.scene.IsValid())
                {
                    Destroy(go);
                }
                CannonballPrefab = null;
            }
        }

        private void SetupCannonballPrefab()
        {
            if (CannonballPrefab != null && CannonballPrefab.gameObject != null)
                return;

            GameObject sourcePrefab = null;
            var isLocalPrefab = false;
            
            // do not mutate the original prefab always create a copy.
            if (CannonballPrefabAsset != null)
                sourcePrefab = Instantiate(CannonballPrefabAsset);
            else if (CannonballPrefabAssetLocal != null)
            {
                sourcePrefab = Instantiate(CannonballPrefabAssetLocal);
                isLocalPrefab = true;
            }

            if (sourcePrefab == null)
            {
                LoggerProvider.LogWarning("No cannonball prefab asset set. Please set one in CannonballPrefabAsset or CannonballPrefabAssetLocal.");
                return;
            }

            var cannonballComponent = sourcePrefab.GetComponent<Cannonball>();
            if (cannonballComponent == null)
            {
                cannonballComponent = sourcePrefab.AddComponent<Cannonball>();
            }

            IgnoreLocalColliders(cannonballComponent);

            var go = cannonballComponent.gameObject;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

          
            go.SetActive(false);
            

            // if (cannonballComponent != null && cannonballComponent.Colliders.Length > 0)
            // {
            //     foreach (var cannonballComponentCollider in cannonballComponent.Colliders)
            //     {
            //         if (cannonballComponentCollider == null) continue;
            //         cannonballComponentCollider.gameObject.SetActive(false);
            //     }
            // }

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            CannonballPrefab = cannonballComponent;
        }

        private void InitializePool()
        {
            _cannonballPool = new Queue<Cannonball>(minPoolSize);
            for (int i = 0; i < minPoolSize; i++)
            {
                var go = Instantiate(CannonballPrefab.gameObject, projectileLoader.position, projectileLoader.rotation, null);
                go.name = $"cannonball_queue_{i}";
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
                    ball.gameObject.name = $"cannonball_active_{_cannonballPool.Count}";
                    ball.gameObject.SetActive(true);
                    _trackedLoadedCannonballs.Add(ball);
                    return ball;
                }
            }
            var go = Instantiate(CannonballPrefab.gameObject, projectileLoader.position, projectileLoader.rotation, null);
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
            ball.ResetCannonball();
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

    // Fail fast if too close
    if (xzDist < 0.01f)
    {
        fireDirection = Vector3.zero;
        angleDegrees = 0;
        return false;
    }

    // Direction in XZ plane
    Vector3 dirXZ = new Vector3(delta.x, 0, delta.z).normalized;

    // Search for an angle (in radians) that lands within 'tolerance' of the target height
    float low = 0f;
    float high = Mathf.PI / 2f;
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

    // Build the firing direction
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
            
            if (targetPosition.HasValue && CalculateBallisticAimWithDrag(fireOrigin, targetPosition.Value, cannonballSpeed, CannonballPrefab.cannonBallDrag, out var fireDir, out var angle))
            {
                var lookDirection = Quaternion.LookRotation(fireDir, Vector3.up);
                var isWithinRange = Mathf.Abs(transform.rotation.eulerAngles.y + lookDirection.eulerAngles.y) <= maxFiringRotationY;
                
                // a negative angle as this angle must be facing the direction.
                _targetShooterLocalRotation = isWithinRange ? Quaternion.Euler(-angle,0f, 0f) : Quaternion.identity;

                if (!canRotateFiringRangeY && cannonScalarTransform.localRotation != Quaternion.identity)
                {
                    cannonScalarTransform.localRotation = Quaternion.identity;
                }
                else if (canRotateFiringRangeY)
                {
                     // rotates whole cannon (internal, not prefab) towards firing point.
                    var rotationTarget = isWithinRange ? lookDirection : Quaternion.identity;
                    // var rotationTarget = lookDirection;
                    var scalarRotation = cannonScalarTransform.rotation;
                    scalarRotation = Quaternion.Lerp(scalarRotation, Quaternion.Euler(scalarRotation.eulerAngles.x, rotationTarget.eulerAngles.y, scalarRotation.eulerAngles.z), Time.fixedDeltaTime * aimingSpeed);
                    cannonScalarTransform.rotation = scalarRotation;
                }
            }
            else
            {
                _targetShooterLocalRotation = Quaternion.identity;
            }
            
            cannonShooterTransform.localRotation = Quaternion.Lerp(cannonShooterTransform.localRotation, _targetShooterLocalRotation, Time.fixedDeltaTime * aimingSpeed);
        }

        public bool Fire()
        {
            if (IsReloading || _loadedCannonball == null || CurrentAmmo <= 0) return false;
            var targetPosition = GetFiringTargetPosition();
            if (!targetPosition.HasValue) return false;
            if (Vector3.Distance(targetPosition.Value, projectileLoader.position) > maxFiringRange) return false;
            
            IgnoreLocalColliders(_loadedCannonball);
            _loadedCannonball.transform.position = projectileLoader.position;
#if UNITY_EDITOR
            Debug.DrawRay(projectileLoader.position, cannonShooterTransform.forward * 150f, Color.green, 20.0f);
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
                var t = Mathf.Clamp01(elapsed /2f / 0.15f ); // 0..1
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
                    cannonScalarTransform.localPosition = Vector3.Lerp(Vector3.forward * -0.1f,Vector3.zero, t);
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

        private IEnumerator ReloadCoroutine()
        {
            // yield return new WaitUntil(() => _audioSource.isPlaying == false);
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
