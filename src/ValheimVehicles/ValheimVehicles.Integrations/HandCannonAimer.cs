using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;

public class CannonHandHeldController : CannonController, Hoverable
{
  [Header("Aiming")]
  [SerializeField] public float minPitch = -70f; // Looking down
  [SerializeField] public float maxPitch = 70f; // Looking up
  [SerializeField] public float minYaw = -30f; // Yaw limit left
  [SerializeField] public float maxYaw = 30f; // Yaw limit right
  [SerializeField] public float defaultElevationDegrees = 5f;
  [SerializeField] public bool debugDraw = false;

  private float _lastPitch;
  private float _lastYaw;
  private bool _hasLastAngles;
  private Quaternion _lastRotation;

  public static float SquareMagnitudeThreshold = 1e-6f;
  public static bool ShouldUpdate = true;

  private AmmoController _ammoController;

  protected internal override void Start()
  {
    _ammoController = GetComponent<AmmoController>();
    TryInitController();
    base.Start();
  }

  private bool TryInitController()
  {
    try
    {
      if (_ammoController == null)
        _ammoController = GetComponent<AmmoController>();
      return _ammoController != null;
    }
    catch (Exception ex)
    {
      LoggerProvider.LogError($"[HandCannonCameraPitchAiming] Failed to init controller: \n{ex.Message}");
      return false;
    }
  }

  protected internal override void FixedUpdate()
  {
    base.FixedUpdate();
    if (!ShouldUpdate) return;
    if (!IsHeldByLocalPlayer()) return;
    if (!TryInitController()) return;
    if (GameCamera.instance == null) return;
    var camera = GameCamera.instance.m_camera;
    if (camera == null) return;

    var parent = cannonRotationalTransform.parent;
    if (!parent) return;

    var localLook = parent.InverseTransformDirection(camera.transform.forward);

    // Check for degenerate input
    if (localLook.sqrMagnitude < SquareMagnitudeThreshold) return;

    var yaw = Mathf.Atan2(localLook.x, localLook.z) * Mathf.Rad2Deg;
    yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

    var pitch = -Mathf.Asin(Mathf.Clamp(localLook.y, -1f, 1f)) * Mathf.Rad2Deg;
    pitch -= defaultElevationDegrees;
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

    // Don't update if unchanged (floating point safe check)
    if (_hasLastAngles &&
        Mathf.Approximately(pitch, _lastPitch) &&
        Mathf.Approximately(yaw, _lastYaw))
    {
      return;
    }

    // Only update if we're within the valid aiming range (already clamped above)
    // If you want to prevent updating at *exact* clamp edges, check:
    // if (pitch == minPitch || pitch == maxPitch || yaw == minYaw || yaw == maxYaw) return;
    // Or, only skip update if the *input* before clamping was outside the range.

    var rotation = Quaternion.Euler(pitch, yaw, 0f);
    var forward = rotation * Vector3.forward;
    if (forward.sqrMagnitude < 1e-5f)
    {
      rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
#if UNITY_EDITOR
            Debug.LogWarning("HandCannonCameraPitchAiming: Aiming rotation would result in zero forward, using fallback.");
#endif
    }

    // Only update transform if rotation is actually different, to avoid dirtying transform unnecessarily
    if (!_hasLastAngles || Quaternion.Angle(rotation, _lastRotation) > 0.01f)
    {
      cannonRotationalTransform.localRotation = rotation;
      _lastRotation = rotation;
      _lastPitch = pitch;
      _lastYaw = yaw;
      _hasLastAngles = true;
    }

    if (debugDraw)
    {
      RuntimeDebugLineDrawer.DrawLine(cannonShooterTransform.position,
        cannonShooterTransform.position + cannonShooterTransform.forward * 5f,
        Color.green, 0.1f);
    }
  }

  public bool FireHandheld()
  {
    if (Fire(true, _ammoController.GetAmmoAmountFromCannonballVariant(AmmoVariant), out var deltaAmmo))
    {
      _ammoController.OnAmmoChangedFromVariant(AmmoVariant, deltaAmmo);
      return true;
    }
    return false;
  }

  private bool IsHeldByLocalPlayer()
  {
    if (Player.m_localPlayer == null) return false;
    return transform.IsChildOf(Player.m_localPlayer.transform);
  }

  public string GetHoverText()
  {
    return Localization.instance.Localize("$valheim_vehicles_cannon_handheld_item (H)");
  }
  public string GetHoverName()
  {
    return Localization.instance.Localize("$valheim_vehicles_cannon_handheld_item");
  }
}