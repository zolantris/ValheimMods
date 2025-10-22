#region

using System;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Magic
{
  /// <summary>
  /// Gravity Flight with mouse-driven facing, velocity alignment, reverse, and a hold-brake.
  /// Toggle: hold SHIFT+Space (2s). While active:
  ///  - Space ascend, Ctrl descend, X set hover, W forward, S reverse, SHIFT+S brake, A/D strafe
  ///  - Body aligns to camera yaw; velocity is steered toward camera forward.
  /// All inputs are virtual for Valheim integration.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class GravityMagicController : MonoBehaviour
  {
    // ---------- Serialized (private) ----------
    [Header("Activation")]
    [SerializeField] private float holdToToggleSeconds = 2.0f;
    [SerializeField] private float brakeOnEnableTime = 0.25f;
    [SerializeField] private float brakeStrength = 20f; // quick brake when enabling

    [Header("Up/Down & Hover (PD)")]
    [SerializeField] private float ascendAcceleration = 30f;
    [SerializeField] private float descendAcceleration = 35f;
    [SerializeField] private float hoverKp = 12f; // accel = Kp*err - Kd*velY
    [SerializeField] private float hoverKd = 6f;
    [SerializeField] private float hoverMaxAccel = 30f;
    [SerializeField] private float hoverDeadZone = 0.03f;
    [SerializeField] private float maxVerticalSpeed = 12f;

    [Header("Propulsion")]
    [SerializeField] private float forwardAcceleration = 40f; // used for both forward and reverse
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
    [Tooltip("Acceleration applied opposite to current velocity while brake held.")]
    [SerializeField] private float holdBrakeAccel = 60f;
    [Tooltip("If true, brake affects vertical velocity too; otherwise horizontal only.")]
    [SerializeField] private bool brakeAffectsVertical = true;
    [Tooltip("If speed goes below this while braking, we hard-stop to zero to prevent creep.")]
    [SerializeField] private float brakeStopSpeed = 0.2f;

    // ---------- Public (runtime) ----------
    public bool IsActive { get; private set; }
    public float HoverHeight { get; private set; }

    // ---------- Private state ----------
    private Rigidbody _rb;
    private Camera _cam;
    private float _toggleHoldTimer;
    private bool _isBraking;
    private float _brakeTimer;
    private float _bank; // visual roll for banking

    // ---------- Unity ----------
    private void Awake()
    {
      _rb = GetComponent<Rigidbody>();
      _cam = Camera.main;

      _rb.useGravity = true;
      _rb.drag = 0f;
      _rb.angularDrag = 0.5f;
    }

    private void Update()
    {
      HandleToggle();
      if (IsActive) HandleHoverSet();
      if (IsActive && alignBodyToCameraYaw) AlignBodyYawToCamera(Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
      if (!IsActive) return;

      if (GetBrakeHeld())
      {
        ApplyHoldBrake(Time.fixedDeltaTime);
      }
      else
      {
        ApplyFlightForces(Time.fixedDeltaTime);
        if (forceAlignVelocityToCamera) AlignVelocityToCamera(Time.fixedDeltaTime);
      }

      CapSpeeds();
      ApplyTurnDamping(Time.fixedDeltaTime);
      HandleQuickBrake(Time.fixedDeltaTime);
    }

    // ---------- Core ----------
    private void HandleToggle()
    {
      if (GetToggleChordHeld())
      {
        _toggleHoldTimer += Time.unscaledDeltaTime;
        if (_toggleHoldTimer >= holdToToggleSeconds)
        {
          _toggleHoldTimer = 0f;
          if (!IsActive) EnableFlight();
          else DisableFlight();
        }
      }
      else
      {
        _toggleHoldTimer = 0f;
      }
    }

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
      var v = _rb.velocity;
      v.y = 0f;
      _rb.velocity = v;

      _rb.useGravity = false; // PD hover handles vertical
      _rb.drag = baseDrag;
      StartQuickBrake();
      OnFlightEnabled();
    }

    private void DisableFlight()
    {
      IsActive = false;
      _rb.useGravity = true;
      _rb.drag = 0f;
      _isBraking = false;
      _bank = 0f;
      OnFlightDisabled();
    }

    private void HandleHoverSet()
    {
      if (GetSetHoverPressed())
      {
        var pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 0.1f, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
          HoverHeight = hit.point.y;
        else
          HoverHeight = pos.y;

        OnHoverPointChanged(HoverHeight);
      }
    }

    private void ApplyFlightForces(float dt)
    {
      // Vertical
      var upHeld = GetAscendHeld();
      var dnHeld = GetDescendHeld();

      if (upHeld)
        _rb.AddForce(Vector3.up * (ascendAcceleration * forceModeScalar), forceMode);
      else if (dnHeld)
        _rb.AddForce(Vector3.down * (descendAcceleration * forceModeScalar), forceMode);
      else
        ApplyHoverPD(dt);

      // Horizontal (thrust forward/reverse + strafe)
      var look = GetLookDirection();
      if (look.sqrMagnitude > 0.0001f)
      {
        // W/S: forward (+1) or reverse (-1)
        var longAxis = GetLongitudinalAxis(); // [-1..1]
        if (Mathf.Abs(longAxis) > 0.0001f)
          _rb.AddForce(look * (forwardAcceleration * longAxis * forceModeScalar), forceMode);

        var strafe = GetStrafeAxis(); // A/D
        if (Mathf.Abs(strafe) > 0.0001f)
        {
          var right = Vector3.Cross(Vector3.up, look).normalized;
          _rb.AddForce(right * (strafe * strafeAcceleration * forceModeScalar), forceMode);
        }
      }
    }

    /// <summary>Apply a strong opposing accel to current velocity. Takes priority over thrust/strafe.</summary>
    private void ApplyHoldBrake(float dt)
    {
      var v = _rb.velocity;

      if (!brakeAffectsVertical)
      {
        v.y = 0f; // ignore vertical when braking horizontally
      }

      var speed = v.magnitude;
      if (speed <= brakeStopSpeed)
      {
        // Hard stop to avoid endless tiny drift
        var cur = _rb.velocity;
        if (!brakeAffectsVertical)
        {
          cur.x = 0f;
          cur.z = 0f;
        }
        else { cur = Vector3.zero; }
        _rb.velocity = cur;
        return;
      }

      var oppose = -v.normalized * holdBrakeAccel;
      _rb.AddForce(oppose, ForceMode.Acceleration);

      // optional: mild angular damping while braking
      _rb.angularVelocity *= 0.92f;
    }

    /// <summary>PD toward HoverHeight. No fake gravity; useGravity=false.</summary>
    private void ApplyHoverPD(float dt)
    {
      var y = transform.position.y;
      var vy = _rb.velocity.y;
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
        var crossY = Vector3.Cross(fwd, planar.normalized).y; // sign for left/right
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
      var vel = _rb.velocity;
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
      var v = _rb.velocity;
      v.y = Mathf.Clamp(v.y, -maxVerticalSpeed, maxVerticalSpeed);

      var h = new Vector3(v.x, 0f, v.z);
      var hMag = h.magnitude;
      if (hMag > maxHorizontalSpeed)
      {
        h = h.normalized * maxHorizontalSpeed;
        v.x = h.x;
        v.z = h.z;
      }
      _rb.velocity = v;
    }

    private void ApplyTurnDamping(float dt)
    {
      var look = GetLookDirection();
      var vel = _rb.velocity;
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

      var oppose = -_rb.velocity * brakeStrength;
      _rb.AddForce(oppose, ForceMode.Acceleration);

      if (_brakeTimer <= 0f) _isBraking = false;
    }

    // ---------- Virtual input layer (override in Valheim integration) ----------
    protected virtual bool GetToggleChordHeld()
    {
      // SHIFT + Space (Jump) to toggle flight
      return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
             && Input.GetKey(KeyCode.Space);
    }

    protected virtual bool GetAscendHeld()
    {
      return Input.GetKey(KeyCode.Space);
    }
    protected virtual bool GetDescendHeld()
    {
      return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

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
  }

  internal static class Vector3Extensions
  {
    public static Vector3 WithYZero(this Vector3 v)
    {
      return new Vector3(v.x, 0f, v.z);
    }
  }
}