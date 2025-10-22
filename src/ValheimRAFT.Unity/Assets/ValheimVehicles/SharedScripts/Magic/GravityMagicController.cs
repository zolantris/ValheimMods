#region

using System;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts.Magic
{
  /// <summary>
  /// Gravity Flight: negate gravity, add up/down thrust, hover "balance point", and directional propulsion.
  /// - Toggle by holding SHIFT + Jump (Space) for 2s (also disables with the same hold).
  /// - While active:
  ///   * Hold Space to go up
  ///   * Hold LeftCtrl to go down
  ///   * Press X to set a new hover balance (freezes vertical velocity around this height)
  ///   * Propulsion follows look/camera direction; rapid damp when you turn sharply
  /// - All input methods are virtual so you can override for Valheim later.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class GravityMagicController : MonoBehaviour
  {
    // ---------- Static ----------
    private static readonly int HoverLayerMask = ~0; // everything, tweak later if needed

    // ---------- Serialized (private) ----------
    [Header("Activation")]
    [SerializeField] private float holdToToggleSeconds = 2.0f; // Hold SHIFT+Jump for this long to toggle
    [SerializeField] private float brakeOnEnableTime = 0.25f; // Quick velocity brake when enabling
    [SerializeField] private float brakeStrength = 20f; // How strong the quick brake is

    [Header("Up/Down & Hover")]
    [SerializeField] private float ascendAcceleration = 30f; // Upward accel while holding Jump
    [SerializeField] private float descendAcceleration = 35f; // Downward accel while holding Ctrl
    [SerializeField] private float hoverGravityCompensation = 1.0f; // 1.0 ~ cancel gravity at hover point
    [SerializeField] private float hoverSnapWindow = 0.2f; // If vertical speed magnitude below this, we "stick" to hover
    [SerializeField] private float hoverVDrag = 4.0f; // Vertical damping near hover
    [SerializeField] private float maxVerticalSpeed = 12f; // Clamp for vertical speed while flying

    [Header("Propulsion")]
    [SerializeField] private float forwardAcceleration = 40f; // thrust along look
    [SerializeField] private float strafeAcceleration = 20f; // A/D or lateral stick (optional)
    [SerializeField] private float maxHorizontalSpeed = 18f; // clamp for planar speed
    [SerializeField] private float baseDrag = 0.4f; // baseline drag while active
    [SerializeField] private float turnDampExtra = 8f; // extra drag when camera turns sharply
    [SerializeField] private float turnDampAngleStart = 25f; // degrees where extra damping begins
    [SerializeField] private float turnDampAngleMax = 80f; // degrees where extra damping is full

    [Header("Quality/Feel")]
    [SerializeField] private float forceModeScalar = 1f; // global scalar if you need quick tuning
    [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

    // ---------- Public (runtime) ----------
    public bool IsActive { get; private set; }
    public float HoverHeight { get; private set; } // world Y where we try to balance when hovering

    // ---------- Private state ----------
    private Rigidbody _rb;
    private Camera _cam;
    private float _toggleHoldTimer;
    private bool _isBraking;
    private float _brakeTimer;

    // ---------- Unity lifecycle ----------
    private void Awake()
    {
      _rb = GetComponent<Rigidbody>();
      _cam = Camera.main;
      // Baseline physically sane defaults for a cube test
      _rb.useGravity = true;
      _rb.linearDamping = 0f;
      _rb.angularDamping = 0.5f;
    }

    private void Update()
    {
      HandleToggle();
      if (IsActive) HandleHoverSet();
    }

    private void FixedUpdate()
    {
      if (!IsActive) return;

      ApplyFlightForces(Time.fixedDeltaTime);
      CapSpeeds();
      ApplyTurnDamping(Time.fixedDeltaTime);
      HandleQuickBrake(Time.fixedDeltaTime);
    }

    // ---------- Core mechanics ----------
    private void HandleToggle()
    {
      // Hold SHIFT + Jump to toggle on/off
      var wantsToggleChord = GetToggleChordHeld(); // virtual
      if (wantsToggleChord)
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
      HoverHeight = transform.position.y;
      _rb.useGravity = false;
      _rb.linearDamping = baseDrag;
      StartQuickBrake();
      OnFlightEnabled(); // virtual hook
    }

    private void DisableFlight()
    {
      IsActive = false;
      _rb.useGravity = true;
      _rb.linearDamping = 0f;
      _isBraking = false;
      OnFlightDisabled(); // virtual hook
    }

    private void HandleHoverSet()
    {
      if (GetSetHoverPressed()) // virtual (default: X)
      {
        HoverHeight = transform.position.y;
        OnHoverPointChanged(HoverHeight);
      }
    }

    private void ApplyFlightForces(float dt)
    {
      // 1) Vertical control: ascend / descend / hover compensation
      var upInput = GetAscendHeld() ? 1f : 0f; // Space
      var dnInput = GetDescendHeld() ? 1f : 0f; // LeftCtrl

      if (upInput > 0f)
        _rb.AddForce(Vector3.up * (ascendAcceleration * forceModeScalar), forceMode);
      else if (dnInput > 0f)
        _rb.AddForce(Vector3.down * (descendAcceleration * forceModeScalar), forceMode);
      else
        ApplyHoverBalance(dt);

      // 2) Propulsion along look direction (WASD optional strafe)
      var look = GetLookDirection();
      if (look.sqrMagnitude > 0.0001f)
      {
        // Forward thrust when holding forward or mouse button — default: hold Right Mouse (or just always-on if you prefer)
        var thrust = GetForwardThrustScalar(); // virtual: default RMB or W
        if (thrust > 0f)
          _rb.AddForce(look * (forwardAcceleration * thrust * forceModeScalar), forceMode);

        var strafe = GetStrafeAxis(); // A/D
        if (Mathf.Abs(strafe) > 0.0001f)
        {
          var right = Vector3.Cross(Vector3.up, look).normalized;
          _rb.AddForce(right * (strafe * strafeAcceleration * forceModeScalar), forceMode);
        }
      }
    }

    private void ApplyHoverBalance(float dt)
    {
      // Cancel gravity near the hover height; softly stick there when vertical speed is small
      var y = transform.position.y;
      var vy = _rb.linearVelocity.y;

      // Gravity compensation (negation-ish)
      var gComp = Physics.gravity.y * -1f * hoverGravityCompensation;
      _rb.AddForce(new Vector3(0f, gComp, 0f), forceMode);

      // Gentle stick when very close/small speed
      if (Mathf.Abs(vy) < hoverSnapWindow)
      {
        var toHover = Mathf.Clamp(HoverHeight - y, -1f, 1f);
        _rb.AddForce(Vector3.up * (toHover * hoverVDrag), forceMode);
      }
    }

    private void CapSpeeds()
    {
      // Vertical cap
      var v = _rb.linearVelocity;
      v.y = Mathf.Clamp(v.y, -maxVerticalSpeed, maxVerticalSpeed);

      // Horizontal cap
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
      // Extra damping when you change look direction vs current velocity
      var look = GetLookDirection();
      var vel = _rb.linearVelocity;
      var hVel = new Vector3(vel.x, 0f, vel.z);

      if (hVel.sqrMagnitude < 0.0001f || look.sqrMagnitude < 0.0001f) return;

      var angle = Vector3.Angle(hVel, look);
      if (angle <= turnDampAngleStart) return;

      var t = Mathf.InverseLerp(turnDampAngleStart, turnDampAngleMax, angle);
      var extra = Mathf.Lerp(0f, turnDampExtra, t);

      // Apply extra drag-like force opposite horizontal velocity
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
      // Strong opposing force to quickly kill existing velocity (feels snappy on enable)
      var oppose = -_rb.linearVelocity * brakeStrength;
      _rb.AddForce(oppose, ForceMode.Acceleration);
      if (_brakeTimer <= 0f) _isBraking = false;
    }

    // ---------- Virtual input layer (override these for Valheim integration) ----------
    protected virtual bool GetToggleChordHeld()
    {
      // SHIFT + Jump (Space)
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

    /// <summary>Return normalized look direction in world space.</summary>
    protected virtual Vector3 GetLookDirection()
    {
      if (_cam && _cam.transform) return _cam.transform.forward.WithYZero().normalized;
      return transform.forward.WithYZero().normalized;
    }

    /// <summary>Scalar [0..1] for forward thrust (default: hold Right Mouse or W).</summary>
    protected virtual float GetForwardThrustScalar()
    {
      var held = Input.GetMouseButton(1) || Input.GetKey(KeyCode.W);
      return held ? 1f : 0f;
    }

    /// <summary>[-1..1] strafe axis (A/D)</summary>
    protected virtual float GetStrafeAxis()
    {
      var a = Input.GetKey(KeyCode.A) ? -1f : 0f;
      var d = Input.GetKey(KeyCode.D) ? +1f : 0f;
      return a + d;
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