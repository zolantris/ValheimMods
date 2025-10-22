#region

using System;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Magic
{
  /// <summary>
  /// Gravity Flight with:
  ///  - SHIFT+Space (hold) enable, SHIFT+CTRL (hold) disable
  ///  - Hover PD (two hover-target modes: SHIFT+X hold to toggle)
  ///  - Mouse-facing + velocity alignment, reverse (S), hold-brake (SHIFT+S)
  ///  - New: Energy/Eitr usage tiers (Low/Medium/High) + overridable consumption hooks
  ///
  /// Default keys (editor test):
  ///  Enable: SHIFT+Space (2s)   Disable: SHIFT+CTRL (2s)
  ///  Ascend: Space              Descend: CTRL
  ///  Lock Hover: X              Toggle Hover Mode: SHIFT+X (1s)
  ///  Forward/Reverse: W/S       Strafe: A/D
  ///  Brake: SHIFT+S (hold)
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class GravityMagicController : MonoBehaviour
  {
    // ---------- Config & Serialized ----------
    [Header("Activation")]
    [SerializeField] private float holdToToggleSeconds = 2.0f; // SHIFT+Space to enable, SHIFT+CTRL to disable
    [SerializeField] private float brakeOnEnableTime = 0.25f;
    [SerializeField] private float brakeStrength = 20f;

    [Header("Up/Down & Hover (PD)")]
    [SerializeField] private float ascendAcceleration = 30f;
    [SerializeField] private float descendAcceleration = 35f;
    [SerializeField] private float hoverKp = 12f; // accel = Kp*err - Kd*velY
    [SerializeField] private float hoverKd = 6f;
    [SerializeField] private float hoverMaxAccel = 30f;
    [SerializeField] private float hoverDeadZone = 0.03f;
    [SerializeField] private float maxVerticalSpeed = 12f;

    [Header("Hover Target Update Mode")]
    [SerializeField] private HoverUpdateMode hoverUpdateMode = HoverUpdateMode.AutoWhileVerticalInput; // Mode 1 default
    [SerializeField] private float holdToToggleHoverModeSeconds = 1.0f; // SHIFT+X hold
    [SerializeField] private bool logHoverModeToggles = false;

    [Header("Propulsion")]
    [SerializeField] private float forwardAcceleration = 40f; // forward & reverse
    [SerializeField] private float strafeAcceleration = 20f;
    [SerializeField] private float maxHorizontalSpeed = 18f;
    [SerializeField] private float baseDrag = 0.25f;

    [Header("Turn Damping")]
    [SerializeField] private float turnDampExtra = 8f;
    [SerializeField] private float turnDampAngleStart = 25f;
    [SerializeField] private float turnDampAngleMax = 80f;

    [Header("Quality/Feel")]
    [SerializeField] private float forceModeScalar = 1f;
    [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

    [Header("Ground Snap on Enable")]
    [SerializeField] private float groundRayDistance = 3.0f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Mouse Look Turning (Editor test)")]
    [SerializeField] private bool alignBodyToCameraYaw = true;
    [SerializeField] private float bodyTurnSpeed = 12f;
    [SerializeField] private bool bankOnTurn = true;
    [SerializeField] private float bankMaxDegrees = 12f;
    [SerializeField] private float bankResponsiveness = 6f;

    [Header("Velocity Alignment to Camera")]
    [SerializeField] private bool forceAlignVelocityToCamera = true;
    [SerializeField] private float steerGain = 6f;
    [SerializeField] private float steerMaxAccel = 30f;
    [SerializeField] private float alignMinSpeed = 0.2f;

    [Header("Hold Brake (SHIFT+S)")]
    [SerializeField] private float holdBrakeAccel = 60f;
    [SerializeField] private bool brakeAffectsVertical = true;
    [SerializeField] private float brakeStopSpeed = 0.2f;

    [Header("Energy / Eitr (per second)")]
    [Tooltip("Cost per second when hovering (idle PD).")]
    [SerializeField] private float energyLowRate = 1.0f;
    [Tooltip("Cost per second when descending.")]
    [SerializeField] private float energyMediumRate = 3.0f;
    [Tooltip("Cost per second when ascending or doing horizontal thrust / significant horizontal motion.")]
    [SerializeField] private float energyHighRate = 6.0f;

    [Tooltip("Extra cost added proportionally with horizontal speed (High state only). 0 = disabled.")]
    [SerializeField] private float highSpeedExtraCostPerMS = 0.0f;

    [Tooltip("Horizontal speed above which we consider it 'significant motion' for High tier.")]
    [SerializeField] private float highMotionSpeedThreshold = 2.0f;

    [SerializeField] public bool IsInitialActive = false;

    // ---------- Public (runtime) ----------
    public bool IsActive { get; private set; }
    public float HoverHeight { get; private set; }

    public HoverUpdateMode CurrentHoverMode => hoverUpdateMode;

    public enum HoverUpdateMode
    {
      /// <summary>Mode 1: While ascending/descending, continuously set HoverHeight = current Y.</summary>
      AutoWhileVerticalInput,
      /// <summary>Mode 2: Only set HoverHeight when the lock key (X) is pressed.</summary>
      ManualOnLock
    }

    public enum EnergyState
    {
      Low,
      Medium,
      High
    }

    public EnergyState CurrentEnergyState { get; private set; } = EnergyState.Low;
    public float CurrentEnergyRatePerSecond { get; private set; } = 0f;

    // ---------- Private state ----------
    private Rigidbody _rb;
    private Camera _cam;

    // Toggle timers
    private float _toggleHoldTimer;
    private float _hoverModeHoldTimer;

    // Quick brake on enable
    private bool _isBraking;
    private float _brakeTimer;

    // Visual bank
    private float _bank;

    // Last inputs observed (for energy classification)
    private bool _lastAscend;
    private bool _lastDescend;
    private float _lastLongitudinal; // W/S
    private float _lastStrafe; // A/D
    private bool _lastBrakeHeld;

    // ---------- Unity ----------
    private void Awake()
    {
      _rb = GetComponent<Rigidbody>();
      _cam = Camera.main;

      _rb.useGravity = true;
      _rb.linearDamping = 0f;
      _rb.angularDamping = 0.5f;

      if (IsInitialActive)
      {
        IsActive = true;
      }
    }

    private void Update()
    {
      HandleToggleFlightHold();
      HandleToggleHoverModeHold();

      if (IsActive) HandleHoverSetPress();
      if (IsActive && alignBodyToCameraYaw) AlignBodyYawToCamera(Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
      if (!IsActive) return;

      if (GetBrakeHeld())
      {
        _lastBrakeHeld = true;
        ApplyHoldBrake(Time.fixedDeltaTime);
      }
      else
      {
        _lastBrakeHeld = false;
        ApplyFlightForces(Time.fixedDeltaTime);
        if (forceAlignVelocityToCamera) AlignVelocityToCamera(Time.fixedDeltaTime);
      }

      CapSpeeds();
      ApplyTurnDamping(Time.fixedDeltaTime);
      HandleQuickBrake(Time.fixedDeltaTime);

      // ---- Energy usage (after physics step so speeds are current) ----
      UpdateEnergy(Time.fixedDeltaTime);
    }

    // ---------- Holds / Toggles ----------
    private void HandleToggleFlightHold()
    {
      // Separate chords: SHIFT+Space to enable, SHIFT+CTRL to disable
      if (!IsActive && GetEnableFlightChordHeld())
      {
        _toggleHoldTimer += Time.unscaledDeltaTime;
        if (_toggleHoldTimer >= holdToToggleSeconds)
        {
          _toggleHoldTimer = 0f;
          EnableFlight();
        }
      }
      else if (IsActive && GetDisableFlightChordHeld())
      {
        _toggleHoldTimer += Time.unscaledDeltaTime;
        if (_toggleHoldTimer >= holdToToggleSeconds)
        {
          _toggleHoldTimer = 0f;
          DisableFlight();
        }
      }
      else
      {
        _toggleHoldTimer = 0f;
      }
    }

    private void HandleToggleHoverModeHold()
    {
      if (GetHoverModeToggleChordHeld())
      {
        _hoverModeHoldTimer += Time.unscaledDeltaTime;
        if (_hoverModeHoldTimer >= holdToToggleHoverModeSeconds)
        {
          _hoverModeHoldTimer = 0f;
          hoverUpdateMode = hoverUpdateMode == HoverUpdateMode.AutoWhileVerticalInput
            ? HoverUpdateMode.ManualOnLock
            : HoverUpdateMode.AutoWhileVerticalInput;

          OnHoverModeChanged(hoverUpdateMode);
          if (logHoverModeToggles)
            Debug.Log($"[GravityMagicController] Hover mode: {hoverUpdateMode}");
        }
      }
      else
      {
        _hoverModeHoldTimer = 0f;
      }
    }

    // ---------- Flight enable/disable ----------
    private void EnableFlight()
    {
      IsActive = true;

      // Snap hover height to ground if close; else current Y
      var pos = transform.position;
      if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        HoverHeight = hit.point.y;
      else
        HoverHeight = pos.y;

      // Kill vertical drift
      var v = _rb.linearVelocity;
      v.y = 0f;
      _rb.linearVelocity = v;

      _rb.useGravity = false; // PD hover
      _rb.linearDamping = baseDrag;
      StartQuickBrake();
      OnFlightEnabled();
    }

    private void DisableFlight()
    {
      IsActive = false;
      _rb.useGravity = true;
      _rb.linearDamping = 0f;
      _isBraking = false;
      _bank = 0f;
      OnFlightDisabled();
    }

    // ---------- Hover lock (X) ----------
    private void HandleHoverSetPress()
    {
      if (GetSetHoverPressed())
      {
        SetHoverToGroundOrCurrent();
      }
    }

    private void SetHoverToGroundOrCurrent()
    {
      var pos = transform.position;
      if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        HoverHeight = hit.point.y;
      else
        HoverHeight = pos.y;

      OnHoverPointChanged(HoverHeight);
    }

    // ---------- Forces ----------
    private void ApplyFlightForces(float dt)
    {
      // Vertical
      var upHeld = GetAscendHeld();
      var dnHeld = GetDescendHeld();

      _lastAscend = upHeld;
      _lastDescend = dnHeld;

      if (upHeld)
      {
        _rb.AddForce(Vector3.up * (ascendAcceleration * forceModeScalar), forceMode);
        if (hoverUpdateMode == HoverUpdateMode.AutoWhileVerticalInput) HoverHeight = transform.position.y;
      }
      else if (dnHeld)
      {
        _rb.AddForce(Vector3.down * (descendAcceleration * forceModeScalar), forceMode);
        if (hoverUpdateMode == HoverUpdateMode.AutoWhileVerticalInput) HoverHeight = transform.position.y;
      }
      else
      {
        ApplyHoverPD(dt);
      }

      // Horizontal (forward/reverse + strafe)
      var look = GetLookDirection();
      if (look.sqrMagnitude > 0.0001f)
      {
        var longAxis = GetLongitudinalAxis(); // W=+1, S=-1
        var strafe = GetStrafeAxis(); // A/D

        _lastLongitudinal = longAxis;
        _lastStrafe = strafe;

        if (Mathf.Abs(longAxis) > 0.0001f)
          _rb.AddForce(look * (forwardAcceleration * longAxis * forceModeScalar), forceMode);

        if (Mathf.Abs(strafe) > 0.0001f)
        {
          var right = Vector3.Cross(Vector3.up, look).normalized;
          _rb.AddForce(right * (strafe * strafeAcceleration * forceModeScalar), forceMode);
        }
      }
      else
      {
        _lastLongitudinal = 0f;
        _lastStrafe = 0f;
      }
    }

    /// <summary>Apply a strong opposing accel to current velocity. Takes priority over thrust/strafe.</summary>
    private void ApplyHoldBrake(float dt)
    {
      var v = _rb.linearVelocity;

      if (!brakeAffectsVertical)
      {
        v.y = 0f; // ignore vertical when braking horizontally
      }

      var speed = v.magnitude;
      if (speed <= brakeStopSpeed)
      {
        var cur = _rb.linearVelocity;
        if (!brakeAffectsVertical)
        {
          cur.x = 0f;
          cur.z = 0f;
        }
        else { cur = Vector3.zero; }
        _rb.linearVelocity = cur;
        return;
      }

      var oppose = -v.normalized * holdBrakeAccel;
      _rb.AddForce(oppose, ForceMode.Acceleration);
      _rb.angularVelocity *= 0.92f;
    }

    /// <summary>PD toward HoverHeight. No fake gravity; useGravity=false.</summary>
    private void ApplyHoverPD(float dt)
    {
      var y = transform.position.y;
      var vy = _rb.linearVelocity.y;
      var err = HoverHeight - y;

      if (Mathf.Abs(err) < hoverDeadZone)
        return;

      var accel = hoverKp * err - hoverKd * vy;
      accel = Mathf.Clamp(accel, -hoverMaxAccel, hoverMaxAccel);

      _rb.AddForce(Vector3.up * (accel * forceModeScalar), ForceMode.Acceleration);
    }

    /// <summary>Rotate the body smoothly to match the camera yaw. Optionally bank while turning.</summary>
    private void AlignBodyYawToCamera(float dt)
    {
      var look = GetLookDirectionRaw(); // full forward
      var planar = new Vector3(look.x, 0f, look.z);
      if (planar.sqrMagnitude < 0.0001f) return;

      var targetRot = Quaternion.LookRotation(planar.normalized, Vector3.up);
      transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-bodyTurnSpeed * dt));

      if (bankOnTurn)
      {
        var fwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        var dot = Mathf.Clamp(Vector3.Dot(fwd, planar.normalized), -1f, 1f);
        var crossY = Vector3.Cross(fwd, planar.normalized).y;
        var turnAmt = Mathf.Acos(dot) * Mathf.Rad2Deg;
        var desiredBank = Mathf.Clamp(turnAmt / 45f, 0f, 1f) * bankMaxDegrees * Mathf.Sign(crossY) * -1f;
        _bank = Mathf.Lerp(_bank, desiredBank, 1f - Mathf.Exp(-bankResponsiveness * dt));

        var yawOnly = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        transform.rotation = yawOnly * Quaternion.Euler(0f, 0f, _bank);
      }
    }

    /// <summary>Steer current horizontal velocity to align with camera forward.</summary>
    private void AlignVelocityToCamera(float dt)
    {
      var vel = _rb.linearVelocity;
      var hVel = new Vector3(vel.x, 0f, vel.z);
      var speed = hVel.magnitude;
      if (speed < alignMinSpeed) return;

      var look = GetLookDirection();
      if (look.sqrMagnitude < 0.0001f) return;

      var desired = look * speed;
      var steer = (desired - hVel) * steerGain;
      var steerMag = steer.magnitude;
      if (steerMag > steerMaxAccel) steer = steer * (steerMaxAccel / Mathf.Max(steerMag, 0.0001f));

      _rb.AddForce(new Vector3(steer.x, 0f, steer.z), ForceMode.Acceleration);
    }

    private void CapSpeeds()
    {
      var v = _rb.linearVelocity;
      v.y = Mathf.Clamp(v.y, -maxVerticalSpeed, maxVerticalSpeed);

      var h = new Vector3(v.x, 0f, v.z);
      var hMag = h.magnitude;
      if (hMag > maxHorizontalSpeed)
      {
        h = h.normalized * maxHorizontalSpeed;
        v.x = h.x;
        v.z = h.z;
      }
      _rb.linearVelocity = v;
    }

    private void ApplyTurnDamping(float dt)
    {
      var look = GetLookDirection();
      var vel = _rb.linearVelocity;
      var hVel = new Vector3(vel.x, 0f, vel.z);

      if (hVel.sqrMagnitude < 0.0001f || look.sqrMagnitude < 0.0001f) return;

      var angle = Vector3.Angle(hVel, look);
      if (angle <= turnDampAngleStart) return;

      var t = Mathf.InverseLerp(turnDampAngleStart, turnDampAngleMax, angle);
      var extra = Mathf.Lerp(0f, turnDampExtra, t);

      var oppose = -hVel.normalized * extra;
      _rb.AddForce(new Vector3(oppose.x, 0f, oppose.z), forceMode);
    }

    private void StartQuickBrake()
    {
      _isBraking = true;
      _brakeTimer = brakeOnEnableTime;
    }

    private void HandleQuickBrake(float dt)
    {
      if (!_isBraking) return;
      _brakeTimer -= dt;

      var oppose = -_rb.linearVelocity * brakeStrength;
      _rb.AddForce(oppose, ForceMode.Acceleration);

      if (_brakeTimer <= 0f) _isBraking = false;
    }

    // ---------- Energy / Eitr ----------
    private void UpdateEnergy(float dt)
    {
      var vel = _rb.linearVelocity;
      var hSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
      var vSpeed = Mathf.Abs(vel.y);

      // Choose state (virtual so Valheim can change classification rules)
      var state = ComputeEnergyState(hSpeed, vSpeed, _lastAscend, _lastDescend, _lastBrakeHeld, _lastLongitudinal, _lastStrafe);

      var rate = state switch
      {
        EnergyState.Low => energyLowRate,
        EnergyState.Medium => energyMediumRate,
        EnergyState.High => energyHighRate,
        _ => energyLowRate
      };

      if (state == EnergyState.High && highSpeedExtraCostPerMS > 0f)
      {
        rate += hSpeed * highSpeedExtraCostPerMS;
      }

      CurrentEnergyState = state;
      CurrentEnergyRatePerSecond = rate;

      // Hook to actually apply/decrement energy (Eitr, battery, etc.)
      ConsumeEnergy(state, rate, dt);
    }

    /// <summary>
    /// Decide Low / Medium / High energy tiers.
    /// Default:
    ///  - High: ascending OR braking OR (|WASD| input) OR horizontal speed >= threshold
    ///  - Medium: descending (without braking & not High)
    ///  - Low: otherwise (hovering)
    /// </summary>
    protected virtual EnergyState ComputeEnergyState(
      float horizontalSpeed, float verticalSpeedAbs,
      bool ascendHeld, bool descendHeld, bool brakeHeld,
      float longitudinalAxis, float strafeAxis)
    {
      var hasMoveInput = Mathf.Abs(longitudinalAxis) > 0.01f || Mathf.Abs(strafeAxis) > 0.01f;

      if (brakeHeld) return EnergyState.High;
      if (ascendHeld) return EnergyState.High;
      if (hasMoveInput) return EnergyState.High;
      if (horizontalSpeed >= highMotionSpeedThreshold) return EnergyState.High;

      if (descendHeld) return EnergyState.Medium;

      return EnergyState.Low;
    }

    /// <summary>
    /// Consume energy at 'ratePerSecond' for dt seconds.
    /// Override this to drain Eitr from player or a power-storage component.
    /// Default: no-op.
    /// </summary>
    protected virtual void ConsumeEnergy(EnergyState state, float ratePerSecond, float dt)
    {
      // Example (pseudo):
      // var cost = ratePerSecond * dt;
      // MyPowerStore.TryConsume(cost);
      // if (!MyPowerStore.HasEnergy) DisableFlight();
    }

    // ---------- Input layer (override in Valheim integration) ----------
    /// <summary>SHIFT + Space (Jump) to ENABLE flight.</summary>
    protected virtual bool GetEnableFlightChordHeld()
    {
      return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
             && Input.GetKey(KeyCode.Space);
    }

    /// <summary>SHIFT + CTRL (Down) to DISABLE flight.</summary>
    protected virtual bool GetDisableFlightChordHeld()
    {
      return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
             && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
    }

    /// <summary>SHIFT + X hold to toggle hover mode.</summary>
    protected virtual bool GetHoverModeToggleChordHeld()
    {
      return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
             && Input.GetKey(KeyCode.X);
    }

    protected virtual bool GetAscendHeld()
    {
      return Input.GetKey(KeyCode.Space);
    }

    protected virtual bool GetDescendHeld()
    {
      return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    /// <summary>X pressed to lock hover (Mode 2), or to intentionally reset hover in Mode 1.</summary>
    protected virtual bool GetSetHoverPressed()
    {
      return Input.GetKeyDown(KeyCode.X);
    }

    /// <summary>[-1..1] longitudinal: W = +1 (forward), S = -1 (reverse).</summary>
    protected virtual float GetLongitudinalAxis()
    {
      var f = Input.GetKey(KeyCode.W) ? 1f : 0f;
      var b = Input.GetKey(KeyCode.S) ? -1f : 0f;
      return f + b;
    }

    /// <summary>Brake chord: SHIFT + S (takes priority over thrust).</summary>
    protected virtual bool GetBrakeHeld()
    {
      var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
      return shift && Input.GetKey(KeyCode.S);
    }

    /// <summary>[-1..1] strafe axis (A/D)</summary>
    protected virtual float GetStrafeAxis()
    {
      var a = Input.GetKey(KeyCode.A) ? -1f : 0f;
      var d = Input.GetKey(KeyCode.D) ? +1f : 0f;
      return a + d;
    }

    /// <summary>Planar look direction from camera (normalized, y=0).</summary>
    protected virtual Vector3 GetLookDirection()
    {
      var f = GetLookDirectionRaw();
      var planar = new Vector3(f.x, 0f, f.z);
      return planar.sqrMagnitude < 0.0001f ? Vector3.zero : planar.normalized;
    }

    /// <summary>Raw camera forward in world space (pitch included).</summary>
    protected virtual Vector3 GetLookDirectionRaw()
    {
      if (_cam && _cam.transform) return _cam.transform.forward;
      return transform.forward;
    }

    // ---------- Virtual hooks ----------
    protected virtual void OnFlightEnabled() {}
    protected virtual void OnFlightDisabled() {}
    protected virtual void OnHoverPointChanged(float newHoverY) {}
    protected virtual void OnHoverModeChanged(HoverUpdateMode newMode) {}
  }

  internal static class Vector3Extensions
  {
    public static Vector3 WithYZero(this Vector3 v)
    {
      return new Vector3(v.x, 0f, v.z);
    }
  }
}