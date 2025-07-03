// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using Random = UnityEngine.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
     [RequireComponent(typeof(Rigidbody))]
    public class RandomWanderer : MonoBehaviour
    {
        [Header("Wander Settings")]
        [Tooltip("Size of allowed area (half extents per axis)")]
        public Vector3 AreaHalfExtents = new Vector3(100f, 100f, 100f);
        [Tooltip("Wandering impulse force magnitude")]
        public float WanderForce = 10f;
        [Tooltip("How often to apply wander force (seconds)")]
        public float WanderInterval = 1.5f;
        [Tooltip("Max velocity before suppressing further wander force")]
        public float MaxWanderVelocity = 4f;

        [Header("Return Settings")]
        [Tooltip("If object is outside this much from center, force return")]
        public Vector3 ReturnHalfExtents = new Vector3(200f, 200f, 200f);
        [Tooltip("Strong force to return object to center if outside return bounds")]
        public float ReturnForce = 100f;

        private Rigidbody _body;
        private float _nextWanderTime;
        private Vector3 _spawnPosition;

        private bool IsRespawning;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = false; // Defensive: always ensure non-kinematic
            _spawnPosition = _body.position; // Capture spawn point
            IsRespawning = false;
        }

        private void FixedUpdate()
        {
            var pos = _body.position;
            var delta = pos - _spawnPosition;

            if (delta.sqrMagnitude > 100)
            {
                if (!IsRespawning)
                {
                    IsRespawning = true;
                     Invoke(nameof(DelayedRespawn), 5f);
                }
                return;
            }

            // If we're way out, forcefully nudge back (return force overrides wander)
            if (Mathf.Abs(delta.x) > ReturnHalfExtents.x ||
                Mathf.Abs(delta.y) > ReturnHalfExtents.y ||
                Mathf.Abs(delta.z) > ReturnHalfExtents.z)
            {
                // Direction back to center
                var dir = (_spawnPosition - pos).normalized;
                _body.AddForce(dir * ReturnForce, ForceMode.Acceleration);
                return; // skip wandering
            }

            // Otherwise, only nudge randomly if inside normal bounds
            if (Time.time >= _nextWanderTime && _body.velocity.sqrMagnitude < MaxWanderVelocity * MaxWanderVelocity)
            {
                // If we're at edge of area, bias force back inward
                Vector3 randomDir = Random.onUnitSphere;
                Vector3 bias = Vector3.zero;

                // For each axis, bias back if near edge
                for (int i = 0; i < 3; ++i)
                {
                    float axisDelta = delta[i];
                    float axisLimit = AreaHalfExtents[i] * 0.95f; // 95% of edge
                    if (Mathf.Abs(axisDelta) > axisLimit)
                    {
                        bias[i] = -Mathf.Sign(axisDelta); // bias toward center
                    }
                }

                Vector3 wanderDir = (randomDir + bias).normalized;
                _body.AddForce(wanderDir * WanderForce, ForceMode.Acceleration);
                _nextWanderTime = Time.time + WanderInterval * Random.Range(0.7f, 1.3f); // randomize interval
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = new Color(0, 1, 0, 0.18f);
            Gizmos.DrawCube(_spawnPosition, AreaHalfExtents * 2f);
            Gizmos.color = new Color(1, 0, 0, 0.08f);
            Gizmos.DrawCube(_spawnPosition, ReturnHalfExtents * 2f);
        }
#endif

        public void DelayedRespawn()
        {
            _body.position = _spawnPosition; // Reset position if too far away
            _body.velocity = Vector3.zero; // Reset velocity
            _body.angularVelocity = Vector3.zero; // Reset angular velocity
        }
    }
}
