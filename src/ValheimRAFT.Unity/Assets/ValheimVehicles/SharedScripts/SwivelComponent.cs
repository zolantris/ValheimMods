// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;

#endregion

namespace ValheimVehicles.SharedScripts
{
    [Flags]
    public enum HingeAxis { None = 0, X = 1, Y = 2, Z = 4 }

    public class SwivelComponent : MonoBehaviour
    {
        public enum HingeDirection { Forward, Backward }

        public const string SNAPPOINT_TAG = "snappoint";
        public const string AnimatedContainerName = "animated";
        private const float positionThreshold = 0.01f;
        private const float angleThreshold = 0.1f;

        [Header("Swivel General Settings")]
        [SerializeField] public SwivelMode mode = SwivelMode.Rotate;
        [SerializeField] private float movementLerpSpeed = 2f;
        [SerializeField] private Transform animatedTransform;
        [SerializeField] private float maxTurnAnglePerSecond = 90f;

        [Header("Enemy Tracking Settings")]
        [SerializeField] private float minTrackingRange = 5f;
        [SerializeField] private float maxTrackingRange = 50f;
        [SerializeField] internal GameObject nearestTarget;

        [Header("Rotation Mode Settings")]
        [SerializeField] private bool rotationReturning = true;
        [SerializeField] private HingeAxis hingeAxes = HingeAxis.Y;
        [SerializeField] private HingeDirection xHingeDirection = HingeDirection.Forward;
        [SerializeField] private HingeDirection yHingeDirection = HingeDirection.Forward;
        [SerializeField] private HingeDirection zHingeDirection = HingeDirection.Forward;
        [SerializeField] private Vector3 maxRotationEuler = new(45f, 90f, 45f);
        [SerializeField] private UnityEvent onRotationReachedTarget;
        [SerializeField] private UnityEvent onRotationReturned;

        [Header("Movement Mode Settings")]
        [SerializeField] private bool isReturning;
        [SerializeField] private Vector3 movementOffset = new(0f, 2f, 0f);
        [SerializeField] private bool useWorldPosition;
        [SerializeField] private UnityEvent onMovementReachedTarget;
        [SerializeField] private UnityEvent onMovementReturned;

        [Description("Piece container containing all children to be rotated or moved.")]
        public Transform piecesContainer;

        [Description("Shown until an object is connected to the swivel.")]
        public Transform connectorContainer;

        public Transform directionDebuggerArrow;
        private Rigidbody animatedRigidbody;

        private bool hasReachedTarget;
        private bool hasReturned;
        private bool hasRotatedReturn;
        private bool hasRotatedTarget;
        private Vector3 hingeEndEuler;
        private float hingeLerpProgress;
        private Transform snappoint;

        private Vector3 startLocalPosition;
        private Quaternion startRotation;
        private Vector3 targetMovementPosition;
        private Quaternion targetRotation;
        // [SerializeField] private float turningLerpSpeed = 50f;
        public float MovementLerpSpeed => movementLerpSpeed;

        public HingeAxis HingeAxes => hingeAxes;
        public SwivelMode Mode => mode;
        public bool IsRotationReturning => rotationReturning;
        public Vector3 MaxEuler => maxRotationEuler;

        public virtual void Awake()
        {
            snappoint = transform.Find(SNAPPOINT_TAG);

            animatedTransform = transform.Find(AnimatedContainerName);
            if (!animatedTransform) throw new MissingComponentException("Missing animated container");

            piecesContainer = animatedTransform.Find("piece_container");
            directionDebuggerArrow = piecesContainer?.Find("direction_debugger_arrow");
            connectorContainer = transform.Find("connector_container");

            startRotation = animatedTransform.localRotation;
            startLocalPosition = animatedTransform.localPosition;

            animatedRigidbody = animatedTransform.GetComponent<Rigidbody>();
            if (!animatedRigidbody)
            {
                animatedRigidbody = animatedTransform.gameObject.AddComponent<Rigidbody>();
            }
            animatedRigidbody.isKinematic = true;
            animatedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public virtual void Start() => SyncSnappoint();

        public virtual void FixedUpdate()
        {
            if (!piecesContainer || !animatedRigidbody || !animatedTransform.parent) return;

            bool didMove = false;

            switch (mode)
            {
                case SwivelMode.Rotate:
                    targetRotation = CalculateRotationTarget();
                    float angleToTarget = Quaternion.Angle(animatedTransform.localRotation, targetRotation);

                    animatedRigidbody.Move(transform.position, transform.rotation * targetRotation);
                    didMove = true;

                    if (!rotationReturning && !hasRotatedTarget && angleToTarget < angleThreshold)
                    {
                        hasRotatedTarget = true;
                        onRotationReachedTarget?.Invoke();
                    }
                    else if (rotationReturning && !hasRotatedReturn && angleToTarget < angleThreshold)
                    {
                        hasRotatedReturn = true;
                        onRotationReturned?.Invoke();
                    }

                    if (rotationReturning && hasRotatedTarget) hasRotatedReturn = false;
                    if (!rotationReturning && hasRotatedReturn) hasRotatedTarget = false;
                    break;

                case SwivelMode.Move:
                    targetMovementPosition = isReturning ? startLocalPosition : startLocalPosition + movementOffset;
                    Vector3 currentLocal = animatedTransform.localPosition;
                    Vector3 nextLocal = Vector3.Lerp(
                        currentLocal,
                        targetMovementPosition,
                        movementLerpSpeed * Time.fixedDeltaTime
                    );
                    Vector3 worldTarget = transform.TransformPoint(nextLocal);
                    Quaternion moveWorldRot = transform.rotation;
                    animatedRigidbody.Move(worldTarget, moveWorldRot);
                    didMove = true;

                    float distanceToTarget = Vector3.Distance(currentLocal, targetMovementPosition);
                    if (!isReturning && !hasReachedTarget && distanceToTarget < positionThreshold)
                    {
                        hasReachedTarget = true;
                        onMovementReachedTarget?.Invoke();
                    }
                    else if (isReturning && !hasReturned && distanceToTarget < positionThreshold)
                    {
                        hasReturned = true;
                        onMovementReturned?.Invoke();
                    }
                    if (isReturning && hasReachedTarget) hasReturned = false;
                    if (!isReturning && hasReturned) hasReachedTarget = false;
                    break;

                case SwivelMode.TargetEnemy:
                    targetRotation = CalculateTargetNearestEnemyRotation();
                    animatedRigidbody.MoveRotation(Quaternion.Slerp(
                        animatedRigidbody.rotation,
                        targetRotation,
                        movementLerpSpeed * Time.fixedDeltaTime
                    ));
                    didMove = true;
                    break;

                case SwivelMode.TargetWind:
                    targetRotation = CalculateTargetWindDirectionRotation();
                    animatedRigidbody.MoveRotation(Quaternion.Slerp(
                        animatedRigidbody.rotation,
                        targetRotation,
                        movementLerpSpeed * Time.fixedDeltaTime
                    ));
                    didMove = true;
                    break;
            }

            if (!didMove)
            {
                Vector3 syncPos = animatedTransform.position;
                Quaternion syncRot = transform.rotation;
                if ((animatedRigidbody.position - syncPos).sqrMagnitude > 0.0001f ||
                    Quaternion.Angle(animatedRigidbody.rotation, syncRot) > 0.01f)
                {
                    animatedRigidbody.Move(syncPos, syncRot);
                }
            }

            SyncSnappoint();
        }

        public void SetMovementLerpSpeed(float speed)
        {
            movementLerpSpeed = Mathf.Clamp(speed, 1f, 100f);
        }

        public void SetMode(SwivelMode newMode) => mode = newMode;
        public void SetReturning(bool returning)
        {
            isReturning = returning;
            hasReachedTarget = false;
            hasReturned = false;
        }
        public void SetRotationReturning(bool returning)
        {
            rotationReturning = returning;
            hasRotatedTarget = false;
            hasRotatedReturn = false;
        }
        public void SetHingeAxes(HingeAxis axes) => hingeAxes = axes;
        public void SetMaxEuler(Vector3 maxEuler) => maxRotationEuler = maxEuler;

        private Quaternion CalculateRotationTarget()
        {
            hingeEndEuler = Vector3.zero;

            if ((hingeAxes & HingeAxis.X) != 0)
                hingeEndEuler.x = (xHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.x;
            if ((hingeAxes & HingeAxis.Y) != 0)
                hingeEndEuler.y = (yHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.y;
            if ((hingeAxes & HingeAxis.Z) != 0)
                hingeEndEuler.z = (zHingeDirection == HingeDirection.Forward ? 1f : -1f) * maxRotationEuler.z;

            float target = rotationReturning ? 0f : 1f;
            hingeLerpProgress = Mathf.MoveTowards(hingeLerpProgress, target, movementLerpSpeed * Time.fixedDeltaTime);
            var euler = Vector3.Lerp(Vector3.zero, hingeEndEuler, hingeLerpProgress);
            return Quaternion.Euler(euler);
        }

        private Quaternion CalculateTargetNearestEnemyRotation()
        {
            if (!nearestTarget || !piecesContainer) return animatedTransform.localRotation;
            var toTarget = nearestTarget.transform.position - piecesContainer.position;
            if (toTarget.magnitude is < 5f or > 50f) return animatedTransform.localRotation;
            var flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flat.sqrMagnitude < 0.001f) return animatedTransform.localRotation;
            return Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        private Quaternion CalculateTargetWindDirectionRotation()
        {
            var windDir = Vector3.forward;
            return Quaternion.LookRotation(windDir, Vector3.up);
        }

        private void SyncSnappoint()
        {
            if (snappoint && connectorContainer)
                snappoint.position = connectorContainer.position;
        }
    }
}
