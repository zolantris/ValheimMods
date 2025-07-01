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
    /// <summary>
    /// Cannonball manages its own firing, physics, trigger, and returns itself to pool.
    /// </summary>
    public class Cannonball : MonoBehaviour
    {

        public enum HitMaterial
        {
            None,
            Wood,
            Metal,
            Stone,
            Terrain,
        }

        public enum ProjectileHitType
        {
            Explosion,
            Penetration,
            None
        }

        /// <summary>
        /// Gets the prefab root. This should be overridable.
        /// </summary>
        /// <returns></returns>
        public static Func<Transform, Transform> GetPrefabRoot = transform => transform.root;

        [Header("Explosion Physics")]
        [SerializeField] private float explosionForce = 1200f;
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private LayerMask explosionLayerMask = ~0;

        [Header("Physics & Trajectory")]
        [SerializeField] private bool useCustomGravity;
        [SerializeField] private float customGravity = 9.81f;
        [SerializeField] private bool debugDrawTrajectory;

        private readonly Collider[] allocatedColliders = new Collider[100];
        private List<Collider> _colliders = new();
        private Vector3 _currentVelocity;

        private Coroutine _despawnCoroutine;
        private bool _hasExitedMuzzle;
        private Transform _muzzleFlashPoint;
        private Action<Cannonball> _onDeactivate;
        private Rigidbody _rb;

        private List<DamageInfo> CollisionToHit;

        public List<Collider> Colliders => GetColliders();

        public bool IsInFlight
        {
            get;
            private set;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetColliders();
        }

        private void OnCollisionEnter(Collision other)
        {
            OnHitHandleHitType(other, out _);
        }

        private void StopDespawnRoutine()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
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
            var layer = obj.layer;;
            if (LayerHelpers.TerrainLayer == layer) return HitMaterial.Terrain;
            if (LayerHelpers.PieceLayer == layer) return HitMaterial.Wood;
            return HitMaterial.Wood;
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
            switch(hitMaterial)
            {
                case HitMaterial.None:
                    return 1f;
                case HitMaterial.Wood:
                    return .75f;
                case HitMaterial.Metal:
                    return 0.1f;
                case HitMaterial.Stone:
                    return 0.2f;
                case HitMaterial.Terrain:
                    return 0f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hitMaterial), hitMaterial, null);
            }
        }

        /// <summary>
        /// Todo might not need this. Additionally this would need to be handled on a singleton layer to be optimized/iterated off of to apply damages.
        /// </summary>
        /// <param name="collider"></param>
        private void AddDamageToQueue(Collider collider)
        {
            var damageInfo = new DamageInfo
            {
                collider = collider,
                velocity = _currentVelocity,
                damage = 10f
            };
            CollisionToHit.Add(damageInfo);
        }

        /// <summary>
        /// Higher speed will generate a larger impact area.
        /// </summary>
        /// <param name="speed"></param>
        private void GetCollisionsFromExplosion( Vector3 explosionOrigin, float force)
        {
            var count = Physics.OverlapSphereNonAlloc(explosionOrigin, explosionRadius,allocatedColliders, explosionLayerMask);
         for (var i = 0; i < count; i++ )
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
                            rb.AddExplosionForce(force, explosionOrigin, explosionRadius);
                    }
                }
            }
        }

        /// <summary>
        /// Handles penetration of cannonball. Uses some randomization to determine if the structure is penetrated.
        /// </summary>
        private void OnHitHandleHitType(Collision other, out ProjectileHitType projectileHitType)
        {
            projectileHitType = ProjectileHitType.None;
            var relativeVelocity = other.relativeVelocity;
            var relativeVelocityZ = relativeVelocity.z;
            var currentVelocity = _rb.velocity;
            
            var hitMaterial = GetHitMaterial(other);
            var hitMaterialVelocityMultiplier = GetHitMaterialVelocityMultiplier(hitMaterial);
            var nextZVelocity = currentVelocity.z * hitMaterialVelocityMultiplier;
            
            var newVelocity = new Vector3(currentVelocity.x, currentVelocity.y, nextZVelocity);
            // nullify velocity on terrain hits. The ground should soak all impact.
            if (other.collider.gameObject.layer == LayerHelpers.TerrainLayer)
            {
                _rb.velocity = new Vector3(currentVelocity.x, currentVelocity.y, 0);
            }
            else if (relativeVelocityZ >= 40f)
            {
                projectileHitType = ProjectileHitType.Penetration;
            }
            else if (relativeVelocityZ < 40f)
            {
                projectileHitType = ProjectileHitType.Explosion;
                GetCollisionsFromExplosion(transform.position, relativeVelocityZ);
                
                // explosions deactivate our cannonball.
                _onDeactivate?.Invoke(this);
            }
            else
            {
                projectileHitType = ProjectileHitType.None;
            }

            _rb.velocity = newVelocity;
        }

        public List<Collider> GetColliders()
        {
            if (_colliders == null || _colliders.Count == 0)
            {
                GetComponentsInChildren(true, _colliders);
            }
            return _colliders;
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
            foreach (var col in Colliders) col.enabled = true;

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
            _onDeactivate = null;
            IsInFlight = false;
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }

        public void Fire(Vector3 velocity, Vector3 muzzlePoint, Action<Cannonball> onDeactivate)
        {
            if (!_rb)
            {
                LoggerProvider.LogWarning("Cannonball has no rigidbody!");
                return;
            }
            // Enable all colliders (just in case)
            foreach (var col in _colliders) col.enabled = true;

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
            foreach (var col in _colliders) col.enabled = false;


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

        public struct DamageInfo
        {
            public Collider collider;
            public Vector3 velocity;
            public float damage;
        }
    }
}