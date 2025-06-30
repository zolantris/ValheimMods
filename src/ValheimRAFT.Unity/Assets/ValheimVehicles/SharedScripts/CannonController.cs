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
        [Tooltip("Speed (m/s) for cannonball launch.")]
        [SerializeField] private float cannonBallSpeed = 50f;

        [Header("Ammunition")]
        [Tooltip("Maximum shells this cannon can hold.")]
        [SerializeField] private int maxAmmo = 12;
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
        [Tooltip("The object that visually rotates for recoil/tilt. Assign your cannon_shooter here.")]
        [SerializeField] private Transform shooterTransform;
        [Tooltip("Speed the muzzle returns after firing.")]
        [SerializeField] private float recoilReturnSpeed = 6f;
        [Tooltip("Speed the muzzle returns after reload tilt up.")]
        [SerializeField] private float reloadReturnSpeed = 3f;
        private readonly HashSet<Cannonball> _activeCannonballs = new();
        private AudioSource _audioSource;
        private Queue<Cannonball> _cannonballPool;
        private Coroutine _cleanupCoroutine;

        // --- Private Fields ---
        private float _currentReturnSpeed;
        private Quaternion _defaultShooterLocalRotation;
        private Cannonball _loadedCannonball;
        private Quaternion _recoilRotation;
        private Quaternion _reloadRotation;
        private Quaternion _targetShooterLocalRotation;

        public int CurrentAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsLoaded => _loadedCannonball != null;

        private void Awake()
        {
            SetupTransforms();
            SetupCannonballPrefab();

            _audioSource = GetComponent<AudioSource>();
            InitializePool();
            CurrentAmmo = maxAmmo;
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

        // --- Events for UI, networking, etc ---
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
            if (CannonballPrefab != null && Application.isPlaying)
            {
                if (CannonballPrefab.gameObject != null)
                {
                    GameObject go = CannonballPrefab.gameObject;
                    // Destroy if it's a runtime-generated prefab (not a persistent asset)
                    if (go.scene.IsValid()) // Only runtime objects have a scene assigned
                    {
                        Destroy(go);
                    }
                }
                CannonballPrefab = null;
            }
        }

        /// <summary>
        /// Ensures CannonballPrefab is set to a valid prefab with a Cannonball component. NEVER destroyed!
        /// </summary>
        private void SetupCannonballPrefab()
        {
            CleanupCannonballPrefab(); // as above

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

            // Static prototype setup
            var go = cannonballComponent.gameObject;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(false);

            // Hide physics/colliders on the static prototype
            var collider = go.GetComponent<Collider>();
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

        // --- Object Pooling for Cannonballs ---
        private void InitializePool()
        {
            _cannonballPool = new Queue<Cannonball>(minPoolSize);
            for (int i = 0; i < minPoolSize; i++)
            {
                var go = Instantiate(CannonballPrefab.gameObject, Vector3.one * 9999, Quaternion.identity, transform);
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
                ball.gameObject.SetActive(true);
                return ball;
            }
            var go = Instantiate(CannonballPrefab.gameObject, projectileLoader.position, projectileLoader.rotation, transform);
            var obj = go.GetComponent<Cannonball>();
            return obj;
        }

        private void ReturnCannonballToPool(Cannonball ball)
        {
            ball.ResetCannonball();
            ball.gameObject.SetActive(false);
            _cannonballPool.Enqueue(ball);
            _activeCannonballs.Remove(ball);
            StartCleanupCoroutine();
        }

        public bool Fire()
        {
            if (IsReloading || _loadedCannonball == null || CurrentAmmo <= 0) return false;

            // --- Trigger Recoil ---
            if (shooterTransform != null)
            {
                shooterTransform.localRotation = _recoilRotation;
                _targetShooterLocalRotation = _defaultShooterLocalRotation;
                _currentReturnSpeed = recoilReturnSpeed;
            }

            var fireDirection = shooterTransform.forward;
            _loadedCannonball.Fire(
                fireDirection * cannonBallSpeed,
                muzzleFlashPoint.position,
                OnCannonballExitedMuzzle,
                ReturnCannonballToPool);

            // _loadedCannonball.Fire(
            //     projectileLoader.forward * cannonBallSpeed,
            //     muzzleFlashPoint.position,
            //     OnCannonballExitedMuzzle,
            //     ReturnCannonballToPool);

            _activeCannonballs.Add(_loadedCannonball);
            _loadedCannonball = null;
            CurrentAmmo--;
            OnAmmoChanged?.Invoke(CurrentAmmo);

            _audioSource?.PlayOneShot(fireClip);
            OnFired?.Invoke();

            if (autoReload && CurrentAmmo > 0)
                StartCoroutine(ReloadCoroutine());
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

            // --- Trigger Reload "tilt up" effect ---
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

        // Cleanup any excess balls after 10s
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
            // Remove excess inactive balls, keep at least minPoolSize
            while (_cannonballPool.Count > minPoolSize)
            {
                var ball = _cannonballPool.Dequeue();
                Destroy(ball.gameObject);
            }
        }
    }
}
