// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Integrations
{
  /// <summary>
  /// Rotates the assigned barrel (or this transform) to aim at the GameCamera crosshair hit point,
  /// ignoring the player’s own body and equipment.
  /// Attach to the hand cannon or the barrel GameObject.
  /// </summary>
  public class HandCannonCameraPitchAiming : MonoBehaviour
  {
    [Header("Aiming")]
    [SerializeField] public float minPitch = -70f; // Looking down
    [SerializeField] public float maxPitch = 70f; // Looking up
    [SerializeField] public float minYaw = -30f; // Yaw limit left
    [SerializeField] public float maxYaw = 30f; // Yaw limit right
    [SerializeField] public float defaultElevationDegrees = 10f; // This tilts barrel upwards by default
    [SerializeField] public bool debugDraw = false;

    private CannonController _controller;

    private void Start()
    {
      TryInitController();
    }

    private bool TryInitController()
    {
      try
      {
        if (_controller == null)
        {
          _controller = GetComponentInChildren<CannonController>(true);
        }
        return _controller != null;
      }
      catch (Exception ex)
      {
        LoggerProvider.LogError($"[HandCannonCameraPitchAiming] Failed to init controller: \n{ex.Message}");
        return false;
      }
    }

    private void LateUpdate()
    {
      if (!IsHeldByLocalPlayer()) return;
      if (!TryInitController()) return;
      if (GameCamera.instance == null) return;
      var camera = GameCamera.instance.m_camera;
      if (camera == null) return;

      // 1. Get the cannon's parent space
      var parent = _controller.cannonRotationalTransform.parent;
      if (!parent) return;

      // 2. Transform camera forward into the cannon’s parent space
      var localLook = parent.InverseTransformDirection(camera.transform.forward);

      // Check for degenerate input
      if (localLook.sqrMagnitude < 1e-6f) return;

      // 3. Calculate desired yaw (Y) and pitch (X)
      var yaw = Mathf.Atan2(localLook.x, localLook.z) * Mathf.Rad2Deg;
      yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

      var pitch = -Mathf.Asin(Mathf.Clamp(localLook.y, -1f, 1f)) * Mathf.Rad2Deg;
      pitch += defaultElevationDegrees; // <--- add the "skyward" bias
      pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

      // 4. Apply as a single local rotation. (Assume Z is roll, unused)
      _controller.cannonRotationalTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

      if (debugDraw)
      {
        RuntimeDebugLineDrawer.DrawLine(_controller.cannonShooterTransform.position,
          _controller.cannonShooterTransform.position + _controller.cannonShooterTransform.forward * 5f,
          Color.magenta, 0.1f);
      }
    }

    private bool IsHeldByLocalPlayer()
    {
      if (Player.m_localPlayer == null) return false;
      return transform.IsChildOf(Player.m_localPlayer.transform);
    }
  }
}