// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System.ComponentModel;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    /// <summary>
    /// Core swivel component for doors or moving pieces on vehicles.
    /// Integrates with SwivelPlatformController for kinematic collider-driven rotation.
    /// </summary>
    [RequireComponent(typeof(SwivelPlatformController))]
    public partial class SwivelComponent : MonoBehaviour
    {
        public enum DoorHingeMode { ZOnly, YOnly, Both }

        public enum HingeDirection { Forward, Backward }

        public const string SNAPPOINT_TAG = "snappoint";
        public const string AnimatedContainerName = "animated";

        [Header("Swivel Settings")]
        [SerializeField] private SwivelMode mode = SwivelMode.DoorMode;
        [SerializeField] private Transform animatedTransform;
        [SerializeField] private float maxTurnAnglePerSecond = 90f;
        [SerializeField] private float maxTurnAngle = 90f;
        [SerializeField] private float maxInclineZ = 90f;
        [SerializeField] public float turningLerpSpeed = 50f;

        [Header("Enemy Tracking Settings")]
        [SerializeField] private float minTrackingRange = 5f;
        [SerializeField] private float maxTrackingRange = 50f;
        [SerializeField] internal GameObject nearestTarget;

        [Header("Door Mode Settings")]
        [SerializeField] private bool isDoorOpen;
        [SerializeField] public float doorLerpSpeed = 100f;
        [SerializeField] private DoorHingeMode hingeMode = DoorHingeMode.YOnly;
        [SerializeField] private HingeDirection zHingeDirection = HingeDirection.Forward;
        [SerializeField] private HingeDirection yHingeDirection = HingeDirection.Forward;
        [SerializeField] private float maxYAngle = 90f;

        [Description("Piece component meant for containing all pieces that will be swiveled.")]
        public Transform piecesContainer;

        [Description("_connectorContainer is meant to be shown until an object is connected to the swivel component.")]
        public Transform connectorContainer;
        public Transform directionDebuggerArrow;

        public Vector3 startPosition = Vector3.zero;
        public Vector3 targetPosition = Vector3.zero;
        public Quaternion m_startPieceRotation = Quaternion.identity;
        public bool CanUpdate;

        // door interpolation state
        private Vector3 _hingeEndPosition = Vector3.zero;
        private float _hingeLerpProgress;
        private SwivelPlatformController _platformController;

        private Transform _snappoint;

        // rotation tracking
        private Quaternion _targetRotation;

        public DoorHingeMode CurrentHingeMode => hingeMode;
        public HingeDirection CurrentZHingeDirection => zHingeDirection;
        public HingeDirection CurrentYHingeDirection => yHingeDirection;
        public SwivelMode Mode => mode;
        public bool IsDoorOpen => isDoorOpen;
        public float MaxInclineZ => maxInclineZ;
        public float MaxYAngle => maxYAngle;

        public virtual void Awake()
        {
            // find animated container
            FindSnappoint();

            animatedTransform = transform.Find(AnimatedContainerName);
            if (animatedTransform == null)
            {
                throw new MissingComponentException($"{nameof(SwivelComponent)} missing animated container.");
            }

            piecesContainer = animatedTransform.Find("piece_container");
            directionDebuggerArrow = piecesContainer.Find("direction_debugger_arrow");
            connectorContainer = transform.Find("connector_container");

            // setup platform controller
            _platformController = GetComponent<SwivelPlatformController>();
            if (_platformController == null)
            {
                _platformController = gameObject.AddComponent<SwivelPlatformController>();
            }
            _platformController.SetActive(isDoorOpen);

            if (piecesContainer == null || connectorContainer == null)
            {
                Debug.LogError($"{nameof(SwivelComponent)} missing required transforms.");
            }
        }

        public virtual void Start()
        {
            SetInitialLocalRotation();
        }

        public virtual void FixedUpdate()
        {
            if (!CanUpdate)
                return;

            UpdateTargetRotation();
            ApplyRotation();
            SyncSnappoint();
        }

        public virtual void ToggleDebugger(bool val)
        {
            if (directionDebuggerArrow != null)
            {
                directionDebuggerArrow.gameObject.SetActive(val);
            }
        }

        public virtual void ToggleDebugger()
        {
            bool isActive = false;
            if (directionDebuggerArrow != null)
            {
                isActive = directionDebuggerArrow.gameObject.activeSelf;
            }
            ToggleDebugger(!isActive);
        }

        public virtual void SetInitialLocalRotation()
        {
            m_startPieceRotation = transform.localRotation;
        }

        public void TogglePlacementContainer(bool isActive)
        {
            if (connectorContainer != null)
            {
                connectorContainer.gameObject.SetActive(isActive);
            }
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.enabled = isActive;
            }
        }

        public void SyncSnappoint()
        {
            if (_snappoint != null && connectorContainer != null)
            {
                _snappoint.position = connectorContainer.position;
            }
        }

        public void FindSnappoint()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.CompareTag(SNAPPOINT_TAG))
                {
                    _snappoint = child;
                    break;
                }
            }
        }

        private void UpdateTargetRotation()
        {
            switch (mode)
            {
                case SwivelMode.TargetEnemy:
                    _targetRotation = CalculateTargetNearestEnemyRotation();
                    break;
                case SwivelMode.TargetWind:
                    _targetRotation = CalculateTargetWindDirectionRotation();
                    break;
                case SwivelMode.DoorMode:
                    _targetRotation = CalculateDoorModeRotation();
                    break;
                default:
                    _targetRotation = Quaternion.identity;
                    break;
            }
        }

        private void ApplyRotation()
        {
            if (animatedTransform != null)
            {
                animatedTransform.localRotation = Quaternion.Slerp(
                    animatedTransform.localRotation,
                    _targetRotation,
                    turningLerpSpeed * Time.fixedDeltaTime
                );
            }
        }

        public void ResetRotation()
        {
            if (animatedTransform != null)
            {
                animatedTransform.localRotation = m_startPieceRotation;
            }
            _hingeLerpProgress = 0f;
        }

        public void SetMode(SwivelMode newMode)
        {
            ResetRotation();
            mode = newMode;
        }

        public void SetDoorOpen(bool open)
        {
            isDoorOpen = open;
            if (_platformController != null)
            {
                _platformController.SetFromSwivel(open);
            }
        }

        public void SetHingeMode(DoorHingeMode mode)
        {
            ResetRotation();
            hingeMode = mode;
        }

        public void SetZHingeDirection(HingeDirection dir)
        {
            ResetRotation();
            zHingeDirection = dir;
        }

        public void SetYHingeDirection(HingeDirection dir)
        {
            ResetRotation();
            yHingeDirection = dir;
        }

        public void SetMaxInclineZ(float v)
        {
            ResetRotation();
            maxInclineZ = v;
        }

        public void SetMaxYAngle(float v)
        {
            ResetRotation();
            maxYAngle = v;
        }

        public void ToggleDoorState()
        {
            SetDoorOpen(!isDoorOpen);
        }

        protected virtual Quaternion CalculateDoorModeRotation()
        {
            if (piecesContainer == null)
            {
                return m_startPieceRotation;
            }
            _hingeEndPosition = Vector3.zero;
            if (hingeMode == DoorHingeMode.ZOnly || hingeMode == DoorHingeMode.Both)
            {
                float dirZ = zHingeDirection == HingeDirection.Forward ? 1f : -1f;
                _hingeEndPosition.z = dirZ * maxInclineZ;
            }
            if (hingeMode == DoorHingeMode.YOnly || hingeMode == DoorHingeMode.Both)
            {
                float dirY = yHingeDirection == HingeDirection.Forward ? 1f : -1f;
                _hingeEndPosition.y = dirY * maxYAngle;
            }
            float target = isDoorOpen ? 0f : 1f;
            _hingeLerpProgress = Mathf.MoveTowards(_hingeLerpProgress, target, doorLerpSpeed * Time.fixedDeltaTime);
            Vector3 euler = Vector3.Lerp(Vector3.zero, _hingeEndPosition, _hingeLerpProgress);
            return Quaternion.Euler(euler);
        }

        protected virtual Quaternion CalculateTargetNearestEnemyRotation()
        {
            if (nearestTarget == null || piecesContainer == null)
                return transform.localRotation;

            Vector3 toTarget = nearestTarget.transform.position - piecesContainer.position;
            float distance = toTarget.magnitude;
            if (distance < minTrackingRange || distance > maxTrackingRange)
                return transform.localRotation;

            Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
            flat.Normalize();
            if (flat.sqrMagnitude < 0.001f)
                return transform.localRotation;

            Quaternion target = Quaternion.LookRotation(flat, Vector3.up);
            return ClampYawOnly(target);
        }

        protected virtual Quaternion CalculateTargetWindDirectionRotation()
        {
            Vector3 windDir = Vector3.forward;
            Quaternion target = Quaternion.LookRotation(windDir, Vector3.up);
            return ClampYawOnly(target);
        }

        private static Quaternion ClampYawOnly(Quaternion rotation)
        {
            Vector3 e = rotation.eulerAngles;
            return Quaternion.Euler(0f, e.y, 0f);
        }
    }
}
