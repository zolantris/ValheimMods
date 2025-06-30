// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.Collections;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    /// <summary>
    /// Cannonball manages its own firing, physics, trigger, and returns itself to pool.
    /// </summary>
    public class Cannonball : MonoBehaviour
    {
        [Header("Explosion Physics")]
        [SerializeField] private float explosionForce = 1200f;
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private LayerMask explosionLayerMask = ~0;

        [Header("Physics & Trajectory")]
        [SerializeField] private bool useCustomGravity;
        [SerializeField] private float customGravity = 9.81f;
        [SerializeField] private bool debugDrawTrajectory;
        public Collider[] colliders;
        private Vector3 _currentVelocity;

        private Coroutine _despawnCoroutine;
        private bool _hasExitedMuzzle;
        private Transform _muzzleFlashPoint;
        private Action<Cannonball> _onDeactivate;
        private Action _onExitMuzzle;
        private Rigidbody _rb;

        public bool IsInFlight
        {
            get;
            private set;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>(true); // Cache all colliders, including nested!
        }

        private void FixedUpdate()
        {
            // if (useCustomGravity && IsInFlight)
            // {
            //     _currentVelocity += Vector3.down * customGravity * Time.fixedDeltaTime;
            //     _rb.velocity = _currentVelocity;
            //     if (debugDrawTrajectory)
            //         Debug.DrawRay(transform.position, _rb.velocity.normalized * 5, Color.green, 0.1f);
            // }

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
            if (other.collider.gameObject.layer == LayerHelpers.TerrainLayer) return;
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            var relativeVelocity = other.relativeVelocity;

            Vector3 explosionPos = transform.position;
            var colliders = Physics.OverlapSphere(explosionPos, explosionRadius, explosionLayerMask);
            var actualForce = explosionForce * relativeVelocity.magnitude;
            foreach (var collider in colliders)
            {
                var rb = collider.attachedRigidbody;
                if (rb != null && rb != _rb)
                    rb.AddExplosionForce(actualForce, explosionPos, explosionRadius);
            }
            _onDeactivate?.Invoke(this); // Return to pool
        }

        public void Load(Transform loader, Transform muzzleFlash)
        {
            if (loader == null)
            {
                LoggerProvider.LogWarning("Cannonball loader is null!");
                return;
            }

            if (muzzleFlash == null)
            {
                LoggerProvider.LogWarning("Cannonball muzzle flash is null!");
                return;
            }
            transform.position = loader.position;
            transform.rotation = loader.rotation;
            // Enable all cached colliders
            foreach (var col in colliders) col.enabled = true;

            // todo figure out why the heck this is suddenly null.
            if (!_rb)
            {
                _rb = GetComponent<Rigidbody>();
            }

            if (_rb)
            {
                _rb.isKinematic = true;
                _rb.useGravity = false;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _hasExitedMuzzle = false;
            _muzzleFlashPoint = muzzleFlash;
            _onExitMuzzle = null;
            _onDeactivate = null;
            IsInFlight = false;
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }

        public void Fire(Vector3 velocity, Vector3 muzzlePoint, Action onExitMuzzle, Action<Cannonball> onDeactivate)
        {
            if (!_rb)
            {
                LoggerProvider.LogWarning("Cannonball has no rigidbody!");
                return;
            }
            // Enable all colliders (just in case)
            foreach (var col in colliders) col.enabled = true;

            _rb.isKinematic = false;
            _rb.useGravity = true;
            // _rb.useGravity = !useCustomGravity;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            IsInFlight = true;

            _rb.AddForce(velocity, ForceMode.VelocityChange);
            _currentVelocity = _rb.velocity; // For custom gravity
            
            _rb.includeLayers = LayerHelpers.PhysicalLayerMask;

            _muzzleFlashPoint = null;
            _hasExitedMuzzle = false;
            _onExitMuzzle = onExitMuzzle;
            _onDeactivate = onDeactivate;
            _despawnCoroutine = StartCoroutine(AutoDespawnCoroutine());
        }

        private IEnumerator AutoDespawnCoroutine()
        {
            yield return new WaitForSeconds(10f);
            _onDeactivate?.Invoke(this);
        }

        public void ResetCannonball()
        {
            if (!_rb)
            {
                LoggerProvider.LogWarning("Cannonball has no rigidbody!");
                return;
            }
            
            IsInFlight = false;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            // transform.position = Vector3.one * 9999;
            // Disable all colliders
            foreach (var col in colliders) col.enabled = false;


            _rb.isKinematic = true;
            _rb.includeLayers = LayerMask.GetMask();
            _rb.useGravity = false;
            gameObject.SetActive(false);

            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }
    }
}