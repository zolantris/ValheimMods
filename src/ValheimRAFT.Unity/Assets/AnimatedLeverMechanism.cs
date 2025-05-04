#region

using System;
using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
    [RequireComponent(typeof(LineRenderer))]
    public class AnimatedLeverMechanism : MonoBehaviour
    {
        private const float ToggleDuration = 1f;
        private static readonly Vector3 _gearBackRotationAxis = new Vector3(1, 0, 0);
        private static readonly Vector3 _gearFrontRotationAxis = new Vector3(0, 0, 1);

        private static readonly Quaternion LeverDisableTargetRotation = Quaternion.Euler(0, 0, 0);
        private static readonly Quaternion LeverEnabledTargetRotation = Quaternion.Euler(90, 0, 0);

        public Transform attachPoint; // used for player grab
        public Transform leverHandle;
        public Transform leverGearBack;
        public Transform leverGearFront;
        public Transform wireConnector;
        public Transform wireTargetPoint;

        public float SpeedFactor = 10f;
        public float GearRotationSpeed = 360f;
        public float GearMinRunTime = 1.5f;
        public float GearMaxRunTime = 5f;
        public float GearStartDelay;
        private readonly Vector3[] _curvePoints = new Vector3[20];
        private float _emissionPulseTime;
        private float _gearElapsed;
        private bool _isGearRunning;

        private bool _isToggleInProgress;

        private LineRenderer _lineRenderer;
        private bool _mToggleState;
        private bool _pendingDelayedGearStart;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;

        private float _toggleElapsed;

        /// <summary>
        /// Callback when lever toggle completes. Parameter is the new state: true = enabled, false = disabled.
        /// </summary>
        public Action<bool>? OnToggleCompleted;

        /// <summary>
        /// Callback to allow external code to stop gear spinning early.
        /// </summary>
        public Func<bool>? ShouldStopGearEarly;

        private void Awake()
        {
            attachPoint = transform.Find("lever/lever_face/attach_point");
            wireConnector = transform.Find("lever/lever_face/wire_connector");
            wireTargetPoint = transform.Find("lever/lever_face/wire_target") ?? wireConnector;
            leverHandle = transform.Find("lever/lever_handle");
            leverGearBack = transform.Find("lever/lever_gear_back");
            leverGearFront = transform.Find("lever/lever_gear_front");

            _lineRenderer = GetComponent<LineRenderer>();
            if (wireTargetPoint == null) Debug.LogWarning("Wire target point is not assigned or found.");
            _lineRenderer.positionCount = _curvePoints.Length;
            _lineRenderer.startWidth = 0.02f;
            _lineRenderer.endWidth = 0.02f;

            if (_lineRenderer.material == null)
            {
                var defaultMaterial = new Material(Shader.Find("Sprites/Default"));
                defaultMaterial.color = Color.black;
                _lineRenderer.material = defaultMaterial;
            }
            else
            {
                _lineRenderer.material.color = Color.black;
            }

            // Add a glowing pulse effect
            _lineRenderer.material.EnableKeyword("_EMISSION");
            _lineRenderer.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _lineRenderer.material.SetColor("_EmissionColor", Color.black * 1.5f);
        }

#if UNITY_EDITOR
        private void Start()
        {
            InvokeRepeating(nameof(ToggleActivationState), 0f, 3f);
        }
#endif

        private void FixedUpdate()
        {
            if (_isToggleInProgress)
            {
                _toggleElapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(_toggleElapsed / ToggleDuration);

                leverHandle.localRotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

                if (t >= 1f)
                {
                    leverHandle.localRotation = _targetRotation;
                    _isToggleInProgress = false;
                    _toggleElapsed = 0f;
                    OnToggleCompleted?.Invoke(_mToggleState);
                    _pendingDelayedGearStart = true;
                }
            }

            if (_pendingDelayedGearStart)
            {
                _gearElapsed += Time.fixedDeltaTime;
                if (_gearElapsed >= GearStartDelay)
                {
                    StartGearRotation();
                    _pendingDelayedGearStart = false;
                }
            }

            if (_isGearRunning)
            {
                _gearElapsed += Time.fixedDeltaTime;

                RotateGearLocal(leverGearFront, _gearFrontRotationAxis, Time.fixedDeltaTime);
                RotateGearLocal(leverGearBack, _gearBackRotationAxis, Time.fixedDeltaTime);

                bool forceStop = ShouldStopGearEarly?.Invoke() ?? false;
                if ((_gearElapsed >= GearMinRunTime && forceStop) || _gearElapsed >= GearMaxRunTime)
                {
                    _isGearRunning = false;
                    _gearElapsed = 0f;
                }
            }

            UpdateWire();
            UpdateWireGlow();
        }

        public void ToggleActivationState()
        {
            SetActivationState(!_mToggleState);
        }

        public void SetActivationState(bool newState)
        {
            // Start gears immediately (pending delay) when toggling
            _gearElapsed = 0f;
            _pendingDelayedGearStart = true;
            if (_mToggleState == newState && !_isToggleInProgress) return;

            _mToggleState = newState;
            _isToggleInProgress = true;

            _startRotation = leverHandle.localRotation;
            _targetRotation = _mToggleState ? LeverEnabledTargetRotation : LeverDisableTargetRotation;
            _toggleElapsed = 0f;
        }

        private void RotateGearLocal(Transform gear, Vector3 axis, float deltaTime)
        {
            if (!gear) return;

            float angle = GearRotationSpeed * deltaTime;
            gear.localRotation *= Quaternion.AngleAxis(angle, axis);
        }

        private void StartGearRotation()
        {
            _isGearRunning = true;
            _gearElapsed = 0f;
        }

        public void SetWireEndpoints(Transform connector, Transform target)
        {
            wireConnector = connector;
            wireTargetPoint = target;
            _lineRenderer.positionCount = _curvePoints.Length;
            UpdateWire();
            wireConnector = connector;
            wireTargetPoint = target;
        }

        public void ClearWireEndpoints()
        {
            _lineRenderer.positionCount = 0;
            wireConnector = null;
            wireTargetPoint = null;
            _lineRenderer.positionCount = 0;
        }

        private void UpdateWire()
        {
            if (_lineRenderer == null || wireConnector == null || wireTargetPoint == null) return;

            Vector3 start = wireConnector.position;
            Vector3 end = wireTargetPoint.position;

            for (int i = 0; i < _curvePoints.Length; i++)
            {
                float t = i / (float)(_curvePoints.Length - 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                float arcHeight = Mathf.Sin(t * Mathf.PI) * 0.2f; // bump height
                point += Vector3.up * arcHeight;
                _curvePoints[i] = point;
            }

            _lineRenderer.positionCount = _curvePoints.Length;
            _lineRenderer.SetPositions(_curvePoints);
        }

        public static Transform GetSnappointsContainer(AnimatedLeverMechanism lever)
        {
            return lever.transform.Find("lever/lever_face/snappoints");
        }

        private void UpdateWireGlow()
        {
            if (_lineRenderer?.material == null) return;

            _emissionPulseTime += Time.fixedDeltaTime;
            float pulse = Mathf.PingPong(_emissionPulseTime * 2f, 1f); // pulsate between 0 and 1
            Color baseColor = Color.black;
            Color emission = baseColor * (0.75f + pulse * 0.75f); // range: 0.75–1.5 multiplier
            _lineRenderer.material.SetColor("_EmissionColor", emission);
        }

        public void UpdateRotation() { } // Stub for compatibility
    }
}
