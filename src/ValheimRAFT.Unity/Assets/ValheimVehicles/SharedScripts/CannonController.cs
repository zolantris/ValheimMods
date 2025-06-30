// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    [RequireComponent(typeof(AudioSource))]
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
        [SerializeField] private Transform shooterTransform;
        [Tooltip("Speed (m/s) for cannonball launch.")]
        [SerializeField] private float cannonballSpeed = 50f;

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

        [Header("Pooling")]
        [Tooltip("Minimum cannonballs kept in pool (typically 1 per cannon).")]
        [SerializeField] private int minPoolSize = 1;

        // --- Cannon Recoil Fields ---
        [Header("Cannon Recoil")]
        [Tooltip("Speed the muzzle returns after firing.")]
        [SerializeField] private float recoilReturnSpeed = 6f;
        [Tooltip("Speed the muzzle returns after reload tilt up.")]
        [SerializeField] private float reloadReturnSpeed = 3f;


        // --- State ---
        private readonly HashSet<Cannonball> _activeCannonballs = new();
        private readonly List<Cannonball> _trackedLoadedCannonballs = new List<Cannonball>();
        private AudioSource _audioSource;
        private Queue<Cannonball> _cannonballPool;
        private Coroutine _cleanupCoroutine;

        private float _currentReturnSpeed;
        private Quaternion _defaultShooterLocalRotation;
        private Cannonball _loadedCannonball;
        private Quaternion _recoilRotation;
        private Quaternion _reloadRotation;
        private Quaternion _targetShooterLocalRotation;

        public int CurrentAmmo { get => _currentAmmo; private set => _currentAmmo = value; }
        public bool IsReloading { get; private set; }
        public bool IsLoaded => _loadedCannonball != null;

        private void Awake()
        {
            SetupTransforms();
            CleanupCannonballPrefab();
            SetupCannonballPrefab();

            _audioSource = GetComponent<AudioSource>();
            InitializePool();
            TryReload();

            // --- Setup shooter recoil rotation values ---
            if (shooterTransform != null)
            {
                _defaultShooterLocalRotation = shooterTransform.localRotation;
                _recoilRotation = Quaternion.Euler(-2f, 0f, 0f) * _defaultShooterLocalRotation;
                _reloadRotation = Quaternion.Euler(10f, 0f, 0f) * _defaultShooterLocalRotation;
                _targetShooterLocalRotation = _defaultShooterLocalRotation;
                _currentReturnSpeed = recoilReturnSpeed;
            }
        }

        private void Update()
        {
            // Smoothly lerp shooter recoil/tilt
            if (shooterTransform != null && shooterTransform.localRotation != _targetShooterLocalRotation)
            {
                shooterTransform.localRotation = Quaternion.Lerp(
                    shooterTransform.localRotation,
                    _targetShooterLocalRotation,
                    Time.deltaTime * _currentReturnSpeed
                );
                if (Quaternion.Angle(shooterTransform.localRotation, _targetShooterLocalRotation) < 0.1f)
                    shooterTransform.localRotation = _targetShooterLocalRotation;
            }
        }

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
        }

        public event Action<int> OnAmmoChanged;
        public event Action OnFired;
        public event Action OnReloaded;

        private void SetupTransforms()
        {
            if (shooterTransform == null)
                shooterTransform = transform.Find("scalar/cannon_shooter");
            if (projectileLoader == null)
                projectileLoader = transform.Find("scalar/cannon_shooter/shooting_part/points/projectile_loader");
            if (muzzleFlashPoint == null)
                muzzleFlashPoint = transform.Find("scalar/cannon_shooter/shooting_part/points/muzzle_flash");
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
            if (CannonballPrefabAsset != null)
                sourcePrefab = CannonballPrefabAsset;
            else if (CannonballPrefabAssetLocal != null)
                sourcePrefab = CannonballPrefabAssetLocal;

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

            var go = cannonballComponent.gameObject;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(false);

            var collider = go.GetComponentInChildren<Collider>(true);
            if (collider != null)
                collider.enabled = false;

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
                go.SetActive(false);
                _cannonballPool.Enqueue(obj);
            }
        }

        private Cannonball GetPooledCannonball()
        {
            if (_cannonballPool.Count > 0)
            {
                var ball = _cannonballPool.Dequeue();
                ball.gameObject.name = $"cannonball_active_{_cannonballPool.Count}";
                ball.gameObject.SetActive(true);
                _trackedLoadedCannonballs.Add(ball);
                return ball;
            }
            var go = Instantiate(CannonballPrefab.gameObject, projectileLoader.position, projectileLoader.rotation, null);
            var localCannonball = go.GetComponent<Cannonball>();
           
            go.gameObject.SetActive(true);

            // ignore colliders for all cannonballs.
            foreach (var ball in _trackedLoadedCannonballs)
            foreach (var otherCollider in ball.colliders)
            {
                if (otherCollider == null) continue;
                foreach (var localCollider in localCannonball.colliders)
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

        public bool Fire()
        {
            if (IsReloading || _loadedCannonball == null || CurrentAmmo <= 0) return false;

            if (shooterTransform != null)
            {
                shooterTransform.localRotation = _recoilRotation;
                _targetShooterLocalRotation = _defaultShooterLocalRotation;
                _currentReturnSpeed = recoilReturnSpeed;
            }
            
            _loadedCannonball.transform.position = projectileLoader.position;

            var fireDirection = shooterTransform != null
                ? shooterTransform.forward
                : projectileLoader.forward;

            _loadedCannonball.Fire(
                fireDirection * cannonballSpeed,
                muzzleFlashPoint.position,
                OnCannonballExitedMuzzle,
                ReturnCannonballToPool);

            _activeCannonballs.Add(_loadedCannonball);
            _trackedLoadedCannonballs.Remove(_loadedCannonball);
            _loadedCannonball = null;
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo);

            _audioSource?.PlayOneShot(fireClip);
            OnFired?.Invoke();

            if (autoReload && CurrentAmmo > 0)
            {
                StartCoroutine(ReloadCoroutine());
                
            }
            else
            {
                LoggerProvider.LogDev("No ammo left, not reloading.");
            }
            return true;
        }

        [ContextMenu("Reload Cannon")]
        public void TryReload()
        {
            if (IsReloading || CurrentAmmo <= 0 || _loadedCannonball != null)
                return;
            StartCoroutine(ReloadCoroutine());
        }

        private IEnumerator ReloadCoroutine()
        {
            IsReloading = true;
            _audioSource?.PlayOneShot(reloadClip);

            if (shooterTransform != null)
            {
                shooterTransform.localRotation = _reloadRotation;
                _targetShooterLocalRotation = _defaultShooterLocalRotation;
                _currentReturnSpeed = reloadReturnSpeed;
            }

            yield return new WaitForSeconds(reloadTime);

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
