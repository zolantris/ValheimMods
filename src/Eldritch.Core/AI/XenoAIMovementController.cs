// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using System.Collections;
using UnityEngine;

namespace Eldritch.Core
{
    public class XenoAIMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Transform _leftToe;
        [SerializeField] private Transform _rightToe;
        [SerializeField] private Transform _groundCheck;

        [Header("Movement Parameters")]
        public float MoveSpeed = 1f;
        public float CloseMoveSpeed = 0.3f;
        public float Acceleration = 90f;
        public float CloseAcceleration = 20f;
        public float TurnSpeed = 720f;
        public float CloseTurnSpeed = 540f;
        public float CloseRange = 3f;
        public float WanderSpeed = 0.5f;
        public float WanderRadius = 8f;
        public float WanderCooldown = 5f;
        public float WanderMinDistance = 3f;

        [Header("Jump Parameters")]
        public float MaxJumpDistance = 4f;
        public float MaxJumpHeightArc = 2.5f;
        public float JumpCooldown = 0.5f;

        [Header("Grounding")]
        public LayerMask GroundMask = ~0;
        public float GroundedThreshold = 0.5f;
        public float MaxStepHeight = 2f;
        public float MaxDrop = 3f;

        // Internal state
        private float _moveLerpVel;
        private float _lastJumpTime;
        private float _lastTouchedLand;
        private float _lastLowestPointCheck;
        private Vector3 _cachedLowestPoint;
        private float _nextWanderTime;
        private Vector3 _currentWanderTarget;
        private bool _hasWanderTarget;

        private void Awake()
        {
            if (!_rb) _rb = GetComponent<Rigidbody>();
        }

        // ---- Movement Core ----
        public void MoveTowards(Vector3 targetPos, float speed, float accel, float turnSpeed)
        {
            Vector3 toTarget = targetPos - transform.position;
            float distance = toTarget.magnitude;

            // Always rotate
            RotateTowardsDirection(toTarget, turnSpeed);

            // Check for gap ahead, jump if needed
            if (distance > 2f && IsGapAhead())
            {
                if (FindJumpableLanding(out var jumpTarget))
                {
                    JumpTo(jumpTarget);
                    _hasWanderTarget = false;
                    return;
                }
                else
                {
                    BrakeHard();
                    _hasWanderTarget = false;
                    return;
                }
            }

            // Move forward
            _moveLerpVel = Mathf.MoveTowards(_moveLerpVel, speed, accel * 0.5f * Time.deltaTime);
            _rb.AddForce(transform.forward * _moveLerpVel, ForceMode.Acceleration);
        }

        public void BrakeHard()
        {
            if (!IsGrounded()) return;
            var v = _rb.velocity;
            v.x = 0f;
            v.z = 0f;
            _rb.velocity = v;
            _rb.angularVelocity = Vector3.zero;
        }

        // ---- Gap/Jump Logic ----
        public bool IsGapAhead(float distance = 1.0f, float maxStepHeight = -1, float maxDrop = -1)
        {
            if (!IsGrounded()) return false;
            if (maxStepHeight < 0) maxStepHeight = MaxStepHeight;
            if (maxDrop < 0) maxDrop = MaxDrop;
            var checkOrigin = GetFurthestToe();
            if (!checkOrigin) return false;
            if (Physics.Raycast(checkOrigin.position + Vector3.up, Vector3.down, out var hit, maxDrop + 1.2f, GroundMask))
            {
                var yDiff = checkOrigin.position.y - hit.point.y;
                if (yDiff < maxStepHeight) return false;
            }
            return true;
        }

        public bool FindJumpableLanding(out Vector3 landingPoint, float maxDrop = -1, float fanAngle = 60f, int numChecks = 7)
        {
            if (maxDrop < 0) maxDrop = MaxDrop;
            landingPoint = Vector3.zero;
            if (!CanJump() || !IsGrounded()) return false;

            var jumpOrigin = GetFurthestToe()?.position ?? transform.position;
            float bestDistance = 0f;
            Vector3 bestLanding = Vector3.zero;
            bool found = false;

            for (int i = 0; i < numChecks; i++)
            {
                float angle = -fanAngle / 2f + fanAngle / (numChecks - 1) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
                var checkStart = jumpOrigin + dir;
                for (float dist = 1.0f; dist <= MaxJumpDistance; dist += 0.5f)
                {
                    Vector3 rayOrigin = checkStart + dir * dist;
                    Debug.DrawRay(rayOrigin, Vector3.down * (maxDrop + 0.5f), Color.cyan, 2f);
                    Ray downRay = new Ray(rayOrigin, Vector3.down);
                    if (Physics.Raycast(downRay, out var hit, maxDrop + 0.5f, GroundMask))
                    {
                        float verticalDrop = jumpOrigin.y - hit.point.y;
                        float slope = Vector3.Angle(hit.normal, Vector3.up);
                        float landingDist = Vector3.Distance(jumpOrigin, hit.point);
                        if (verticalDrop < maxDrop && slope < 40f && landingDist <= MaxJumpDistance)
                        {
                            if (!found || landingDist > bestDistance)
                            {
                                bestDistance = landingDist;
                                bestLanding = hit.point;
                                found = true;
                            }
                            Debug.DrawLine(rayOrigin, hit.point, Color.magenta, 2f);
                        }
                    }
                }
            }

            if (found)
            {
                landingPoint = bestLanding;
                Debug.DrawLine(jumpOrigin + Vector3.up * 0.2f, bestLanding + Vector3.up * 0.2f, Color.green, 2f);
                return true;
            }
            return false;
        }

        public void JumpTo(Vector3 landingPoint)
        {
            if (_lastJumpTime > Time.fixedTime) return;
            _lastJumpTime = Time.fixedTime + JumpCooldown;
            Vector3 origin = GetLowestPointOnRigidbody();
            Vector3 jumpVec = landingPoint - origin;
            float dist = jumpVec.magnitude;
            if (dist > MaxJumpDistance)
            {
                jumpVec = jumpVec.normalized * MaxJumpDistance;
                landingPoint = origin + jumpVec;
            }
            Debug.DrawLine(origin + Vector3.up * 0.2f, landingPoint + Vector3.up * 0.2f, Color.red, 2f);

            float gravity = Mathf.Abs(Physics.gravity.y);
            float timeToApex = Mathf.Sqrt(2 * MaxJumpHeightArc / gravity);
            float vy = Mathf.Sqrt(2 * gravity * MaxJumpHeightArc);
            float timeTotal = timeToApex + Mathf.Sqrt(2 * Mathf.Max(0, (landingPoint.y - origin.y)) / gravity);
            Vector3 horiz = jumpVec; horiz.y = 0;
            Vector3 vxz = horiz / Mathf.Max(0.01f, timeTotal);
            _rb.velocity = vxz + Vector3.up * vy;
        }

        public bool CanJump() => true;

        // ---- Ground/Lowest Point ----
        public bool IsGrounded()
        {
            if (_groundCheck)
            {
                return Physics.Raycast(_groundCheck.position, Vector3.down, GroundedThreshold, GroundMask);
            }
            // fallback: check by rigidbody y-velocity small and low y delta
            return Mathf.Abs(_rb.velocity.y) < 0.1f;
        }

        public Vector3 GetLowestPointOnRigidbody()
        {
            if (_lastLowestPointCheck > Time.fixedTime)
            {
                return _cachedLowestPoint;
            }
            _lastLowestPointCheck = Time.fixedTime + 0.5f;
            float minY = float.MaxValue;
            foreach (var col in GetComponentsInChildren<Collider>())
            {
                if (!col) continue;
                var bounds = col.bounds;
                if (bounds.min.y < minY)
                    minY = bounds.min.y;
            }
            var pos = transform.position;
            _cachedLowestPoint = new Vector3(pos.x, minY, pos.z);
            return _cachedLowestPoint;
        }

        // ---- Wandering Logic ----
        public void StartWander()
        {
            _hasWanderTarget = false;
        }

        public void UpdateWander()
        {
            if (Time.time < _nextWanderTime) return;
            if (!_hasWanderTarget || Vector3.Distance(transform.position, _currentWanderTarget) < 1.2f)
            {
                if (TryPickRandomWanderTarget(out _currentWanderTarget))
                {
                    _hasWanderTarget = true;
                    _nextWanderTime = Time.time + WanderCooldown;
                }
                else
                {
                    _hasWanderTarget = false;
                    _nextWanderTime = Time.time + 2f;
                    return;
                }
            }
            MoveTowards(_currentWanderTarget, WanderSpeed, Acceleration, TurnSpeed);
        }

        public bool TryPickRandomWanderTarget(out Vector3 wanderTarget)
        {
            var origin = transform.position;
            for (int attempts = 0; attempts < 12; attempts++)
            {
                float angle = UnityEngine.Random.Range(0, 360f);
                float dist = UnityEngine.Random.Range(WanderMinDistance, WanderRadius);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 candidate = origin + dir * dist;
                Ray down = new Ray(candidate + Vector3.up * 3f, Vector3.down);
                if (Physics.Raycast(down, out var hit, 10f, GroundMask))
                {
                    if (Vector3.Angle(hit.normal, Vector3.up) < 45f)
                    {
                        wanderTarget = hit.point;
                        return true;
                    }
                }
            }
            wanderTarget = Vector3.zero;
            return false;
        }

        // ---- Helper: Furthest Toe ----
        public Transform GetFurthestToe()
        {
            if (!_leftToe && !_rightToe) return null;
            if (_leftToe && !_rightToe) return _leftToe;
            if (_rightToe && !_leftToe) return _rightToe;
            Vector3 forward = transform.forward.normalized;
            float leftProj = Vector3.Dot(_leftToe.position - transform.position, forward);
            float rightProj = Vector3.Dot(_rightToe.position - transform.position, forward);
            return leftProj > rightProj ? _leftToe : _rightToe;
        }

        // ---- Rotation ----
        public void RotateTowardsDirection(Vector3 dir, float customTurnSpeed)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) return;
            var targetRot = Quaternion.LookRotation(dir);
            var turnLerp = customTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
            var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            if (Mathf.Abs(angle) > 170f)
            {
                transform.rotation = targetRot;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(turnLerp));
            }
        }
    }
}
