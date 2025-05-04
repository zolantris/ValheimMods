#region

using System;
using UnityEngine;
using UnityEngine.Serialization;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public class AnimatedLeverMechanism : MonoBehaviour
    {
        private const float ToggleDuration = 1f;
        private static readonly Vector3 _gearBackRotationAxis = new Vector3(1, 0, 0);
        private static readonly Vector3 _gearFrontRotationAxis = new Vector3(0, 0, 1);

        private static readonly Quaternion LeverDisableTargetRotation = Quaternion.Euler(0, 0, 0);
        private static readonly Quaternion LeverEnabledTargetRotation = Quaternion.Euler(90, 0, 0);

        public Transform attachPoint;
        [FormerlySerializedAs("lever")] public Transform leverHandle;
        public Transform leverGearBack;
        public Transform leverGearFront;
        public Transform wireConnector;

        public float SpeedFactor = 10f;
        public float GearRotationSpeed = 360f;

        private bool _isToggleInProgress;
        private bool _mToggleState;
        private Quaternion _startRotation;
        private Quaternion _targetRotation;

        private float _toggleElapsed;

        /// <summary>
        /// Callback when lever toggle completes. Parameter is the new state: true = enabled, false = disabled.
        /// </summary>
        public Action<bool>? OnToggleCompleted;

        private void Awake()
        {
            attachPoint = GetSnappointsContainer(this);
            wireConnector = transform.Find("lever/lever_face/wire_connector");
            leverHandle = transform.Find("lever/lever_handle");
            leverGearBack = transform.Find("lever/lever_gear_back");
            leverGearFront = transform.Find("lever/lever_gear_front");
        }

#if UNITY_EDITOR
        private void Start()
        {
            InvokeRepeating(nameof(ToggleActivationState), 0f, 3f);
        }
#endif

        private void FixedUpdate()
        {
            if (!_isToggleInProgress) return; // Skip if no active animation

            _toggleElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_toggleElapsed / ToggleDuration);

            leverHandle.localRotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            RotateGearLocal(leverGearFront, _gearFrontRotationAxis, t);
            RotateGearLocal(leverGearBack, _gearBackRotationAxis, t);

            if (t >= 1f)
            {
                leverHandle.localRotation = _targetRotation;
                _isToggleInProgress = false;
                _toggleElapsed = 0f;
                OnToggleCompleted?.Invoke(_mToggleState);
            }
        }

        public void ToggleActivationState()
        {
            SetActivationState(!_mToggleState);
        }

        public void SetActivationState(bool newState)
        {
            if (_mToggleState == newState && !_isToggleInProgress) return;

            _mToggleState = newState;
            _isToggleInProgress = true;

            _startRotation = leverHandle.localRotation;
            _targetRotation = _mToggleState ? LeverEnabledTargetRotation : LeverDisableTargetRotation;
            _toggleElapsed = 0f;
        }

        private void RotateGearLocal(Transform gear, Vector3 axis, float t)
        {
            if (!gear) return;

            float angle = GearRotationSpeed * t;
            gear.localRotation = Quaternion.AngleAxis(angle, axis);
        }

        public static Transform GetSnappointsContainer(AnimatedLeverMechanism lever)
        {
            return lever.transform.Find("lever/lever_face/snappoints");
        }

        public void UpdateRotation() { } // Stub for compatibility
    }
}
