// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class CannonController : MonoBehaviour
    {
        [Header("Transforms")]
        [Tooltip("Where the cannonball will be loaded and fired from.")]
        [SerializeField] private Transform projectileLoader;
        [Tooltip("Where muzzle flash and effects are triggered.")]
        [SerializeField] private Transform muzzleFlashPoint;

        [Header("Cannonball")]
        [Tooltip("Prefab for the cannonball projectile.")]
        [SerializeField] private Cannonball cannonballPrefab;
        [Tooltip("How fast the cannonball leaves the cannon.")]
        [SerializeField] private float muzzleVelocity = 50f;

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
        private readonly HashSet<Cannonball> _activeCannonballs = new();
        private AudioSource _audioSource;
        private Queue<Cannonball> _cannonballPool;
        private Coroutine _cleanupCoroutine;

        // --- Private Fields ---
        private Cannonball _loadedCannonball;

        public int CurrentAmmo
        {
            get;
            private set;
        }

        public bool IsReloading
        {
            get;
            private set;
        }

        public bool IsLoaded => _loadedCannonball != null;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            InitializePool();
            CurrentAmmo = maxAmmo;
            TryReload();
        }

        // --- Events for UI, networking, etc ---
        public event Action<int> OnAmmoChanged;
        public event Action OnFired;
        public event Action OnReloaded;

        private void InitializePool()
        {
            _cannonballPool = new Queue<Cannonball>(minPoolSize);
            for (int i = 0; i < minPoolSize; i++)
            {
                var obj = Instantiate(cannonballPrefab, Vector3.one * 9999, Quaternion.identity, transform);
                obj.gameObject.SetActive(false);
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
            // Grow only as needed; will cleanup later
            return Instantiate(cannonballPrefab, projectileLoader.position, projectileLoader.rotation, transform);
        }

        private void ReturnCannonballToPool(Cannonball ball)
        {
            ball.ResetCannonball();
            ball.gameObject.SetActive(false);
            _cannonballPool.Enqueue(ball);
            _activeCannonballs.Remove(ball);
            // Start/refresh the cleanup routine after a return
            StartCleanupCoroutine();
        }

        public bool Fire()
        {
            if (IsReloading || _loadedCannonball == null || CurrentAmmo <= 0) return false;

            _loadedCannonball.Fire(
                projectileLoader.forward * muzzleVelocity,
                muzzleFlashPoint.position,
                OnCannonballExitedMuzzle,
                ReturnCannonballToPool);

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
            // Remove excess inactive balls, keep at least minPoolSize
            while (_cannonballPool.Count > minPoolSize)
            {
                var ball = _cannonballPool.Dequeue();
                Destroy(ball.gameObject);
            }
        }
    }

    public class Cannonball : MonoBehaviour
    {
        [Header("Explosion Physics")]
        [SerializeField] private float explosionForce = 1200f;
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private LayerMask explosionLayerMask = ~0;
        private Coroutine _despawnCoroutine;
        private bool _hasExitedMuzzle;
        private Transform _muzzleFlashPoint;
        private Action<Cannonball> _onDeactivate;
        private Action _onExitMuzzle;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_hasExitedMuzzle && _muzzleFlashPoint)
            {
                var dist = Vector3.Distance(transform.position, _muzzleFlashPoint.position);
                if (dist > 0.3f)
                {
                    _hasExitedMuzzle = true;
                    _onExitMuzzle?.Invoke();
                    _muzzleFlashPoint = null;
                }
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            Vector3 explosionPos = transform.position;
            var colliders = Physics.OverlapSphere(explosionPos, explosionRadius, explosionLayerMask);
            foreach (var collider in colliders)
            {
                var rb = collider.attachedRigidbody;
                if (rb != null && rb != _rb)
                    rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius);
            }
            _onDeactivate?.Invoke(this); // Return to pool
        }

        public void Load(Transform loader, Transform muzzleFlash)
        {
            transform.position = loader.position;
            transform.rotation = loader.rotation;
            transform.parent = loader;
            _rb.isKinematic = true;
            _rb.velocity = Vector3.zero;
            _hasExitedMuzzle = false;
            _muzzleFlashPoint = muzzleFlash;
            _onExitMuzzle = null;
            _onDeactivate = null;
            // Stop any previous despawn timer, just in case
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }

        public void Fire(Vector3 velocity, Vector3 muzzlePoint, Action onExitMuzzle, Action<Cannonball> onDeactivate)
        {
            transform.parent = null;
            _rb.isKinematic = false;
            _rb.velocity = velocity;
            _muzzleFlashPoint = null;
            _hasExitedMuzzle = false;
            _onExitMuzzle = onExitMuzzle;
            _onDeactivate = onDeactivate;
            // Start 10s auto-despawn timer
            _despawnCoroutine = StartCoroutine(AutoDespawnCoroutine());
        }

        private IEnumerator AutoDespawnCoroutine()
        {
            yield return new WaitForSeconds(10f);
            _onDeactivate?.Invoke(this); // Return to pool
        }

        public void ResetCannonball()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.position = Vector3.one * 9999; // Move out of world
            // Stop timer in case it's pooled before 10s
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }
    }
}
