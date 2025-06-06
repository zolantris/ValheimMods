#region

#region

using System;
using UnityEngine;

#endregion

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

  namespace ValheimVehicles.SharedScripts
  {
    [RequireComponent(typeof(DynamicLineConnector))]
    public class AnimatedLeverMechanism : MonoBehaviour
    {
      private const float ToggleDuration = 1f;
      private static readonly Vector3 _gearBackRotationAxis = new(1, 0, 0);
      private static readonly Vector3 _gearFrontRotationAxis = new(0, 0, 1);

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
      public float GearMinRunTime = ToggleDuration;
      public float GearMaxRunTime = ToggleDuration;
      public float GearStartDelay;

      private DynamicLineConnector _dynamicWire;
      private float _gearElapsed;
      private bool _isGearRunning;

      private bool _mToggleState;
      private bool _pendingDelayedGearStart;
      private Quaternion _startRotation;
      private Quaternion _targetRotation;

      private float _toggleElapsed;


      public Action<bool>? OnToggleCompleted;
      public Func<bool>? ShouldStopGearEarly;

      public bool IsToggleInProgress
      {
        get;
        private set;
      }

      public virtual void Awake()
      {
        attachPoint = transform.Find("lever/lever_handle/attach_point");
        wireConnector = transform.Find("lever/lever_face/wire_connector");

        leverHandle = transform.Find("lever/lever_handle");
        leverGearBack = transform.Find("lever/lever_gear_back");
        leverGearFront = transform.Find("lever/lever_gear_front");

        _dynamicWire = GetComponent<DynamicLineConnector>();
        if (_dynamicWire != null)
        {
          _dynamicWire.useCurvedWire = false;
          _dynamicWire.ShouldRender = () => wireConnector != null && wireTargetPoint != null;
          _dynamicWire.startTransform = wireConnector; // set to actual wire start
          _dynamicWire.endTransform = wireTargetPoint; // set via prefab or runtime
        }
      }

#if UNITY_2022
      private void Start()
        {
            // InvokeRepeating(nameof(ToggleActivationState), 0f, 3f);
        }
#endif

      public virtual void FixedUpdate()
      {
        if (IsToggleInProgress)
        {
          _toggleElapsed += Time.fixedDeltaTime;
          var t = Mathf.Clamp01(_toggleElapsed / ToggleDuration);
          leverHandle.localRotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

          if (t >= 1f)
          {
            leverHandle.localRotation = _targetRotation;
            IsToggleInProgress = false;
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

          var forceStop = ShouldStopGearEarly?.Invoke() ?? false;
          if (_gearElapsed >= GearMinRunTime && forceStop || _gearElapsed >= GearMaxRunTime)
          {
            _isGearRunning = false;
            _gearElapsed = 0f;
          }
        }
      }

      public void ToggleVisualActivationState()
      {
        SetActivationState(!_mToggleState);
      }

      public void SetActivationState(bool newState)
      {
        _gearElapsed = 0f;
        _pendingDelayedGearStart = true;
        if (_mToggleState == newState && !IsToggleInProgress) return;

        _mToggleState = newState;
        IsToggleInProgress = true;

        _startRotation = leverHandle.localRotation;
        _targetRotation = _mToggleState ? LeverEnabledTargetRotation : LeverDisableTargetRotation;
        _toggleElapsed = 0f;
      }

      private void RotateGearLocal(Transform gear, Vector3 axis, float deltaTime)
      {
        if (!gear) return;
        var angle = GearRotationSpeed * deltaTime;
        gear.localRotation *= Quaternion.AngleAxis(angle, axis);
      }

      private void StartGearRotation()
      {
        _isGearRunning = true;
        _gearElapsed = 0f;
      }

      public static Transform GetSnappointsContainer(GameObject lever)
      {
        return lever.transform.Find("lever/lever_face/snappoints");
      }

      public void UpdateRotation() {} // Stub for compatibility
    }
  }