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
        private Vector3 _currentVelocity;
        private Coroutine _despawnCoroutine;
        private bool _hasExitedMuzzle;
        private bool _isInFlight;
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
            // if (useCustomGravity && _isInFlight)
            // {
            //     // Simple custom gravity, always world-down
            //     _currentVelocity += Vector3.down * customGravity * Time.fixedDeltaTime;
            //     _rb.velocity = _currentVelocity;
            //
            //     // Optionally debug-draw trajectory
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
            if (other.gameObject.name.StartsWith("cannon_ball")) return;
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

        public void Fire(
            Vector3 velocity,
            Vector3 muzzlePoint,
            Action onExitMuzzle,
            Action<Cannonball> onDeactivate)
        {
            transform.parent = null;
            _rb.isKinematic = false;
            _rb.useGravity = false; // Only use built-in gravity if not custom
            // _rb.useGravity = !useCustomGravity; // Only use built-in gravity if not custom
            // _currentVelocity = velocity;
            _isInFlight = true;
            
            _rb.AddForceAtPosition(velocity, muzzlePoint, ForceMode.Impulse);

            _muzzleFlashPoint = null;
            _hasExitedMuzzle = false;
            _onExitMuzzle = onExitMuzzle;
            _onDeactivate = onDeactivate;
            _despawnCoroutine = StartCoroutine(AutoDespawnCoroutine());
        }

        private IEnumerator AutoDespawnCoroutine()
        {
            yield return new WaitForSeconds(10f);
            _onDeactivate?.Invoke(this); // Return to pool
        }

        public void ResetCannonball()
        {
            _isInFlight = false;
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