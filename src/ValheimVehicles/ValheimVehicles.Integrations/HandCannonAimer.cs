// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Integrations
{
  /// <summary>
  /// Rotates the assigned barrel (or this transform) to aim at the GameCamera crosshair hit point,
  /// but will not rotate more than 'maxYaw' from the player's forward (Y) direction.
  /// Attach to the hand cannon or the barrel GameObject.
  /// </summary>
  public class HandCannonCameraPitchAiming : MonoBehaviour
  {
    [Header("Aiming")]
    [SerializeField] public float minPitch = -70f;
    [SerializeField] public float maxPitch = 70f;
    [SerializeField] public float maxYawFromPlayer = 30f; // Degrees, left/right from player forward
    [SerializeField] public bool debugDraw = false;

    private CannonController _controller;
    private Vector3 _lastValidLook = Vector3.forward;

    private void Start()
    {
      TryInitController();
    }

    private bool TryInitController()
    {
      if (_controller == null)
      {
        _controller = GetComponentInChildren<CannonController>(true);
      }
      return _controller != null;
    }

    private void LateUpdate()
    {
      if (!IsHeldByLocalPlayer()) return;
      if (!TryInitController()) return;
      if (GameCamera.instance == null) return;
      var camera = GameCamera.instance.m_camera;
      if (camera == null) return;

      // 1. Reference transform for aiming
      var cannonRoot = _controller.cannonRotationalTransform;
      var parent = cannonRoot.parent;
      if (!parent) return;

      // 2. Compute desired look dir (camera forward, but in cannon's parent space)
      var targetWorldFwd = camera.transform.forward;
      if (targetWorldFwd.sqrMagnitude < 1e-4f)
        targetWorldFwd = _lastValidLook;
      if (targetWorldFwd.sqrMagnitude < 1e-4f)
        targetWorldFwd = parent.forward; // fallback
      _lastValidLook = targetWorldFwd;

      var localLook = parent.InverseTransformDirection(targetWorldFwd).normalized;

      // 3. Compute player forward in local space
      var player = Player.m_localPlayer;
      if (!player) return;
      var playerFwdLocal = parent.InverseTransformDirection(player.transform.forward).normalized;

      // 4. Clamp YAW (Y axis) within allowed arc of player forward
      //    Project both vectors to XZ plane
      var targetXZ = new Vector3(localLook.x, 0, localLook.z).normalized;
      var playerXZ = new Vector3(playerFwdLocal.x, 0, playerFwdLocal.z).normalized;

      if (targetXZ.sqrMagnitude < 1e-4f) targetXZ = playerXZ;
      if (playerXZ.sqrMagnitude < 1e-4f) playerXZ = Vector3.forward;

      var yawFromPlayer = Vector3.SignedAngle(playerXZ, targetXZ, Vector3.up);
      var clampedYaw = Mathf.Clamp(yawFromPlayer, -maxYawFromPlayer, maxYawFromPlayer);

      // Reconstruct local aim dir with clamped yaw
      var yawRotation = Quaternion.AngleAxis(clampedYaw, Vector3.up);
      var clampedXZ = yawRotation * playerXZ;

      // 5. Clamp pitch based on the direction from the cannon
      var pitch = -Mathf.Asin(Mathf.Clamp(localLook.y, -1f, 1f)) * Mathf.Rad2Deg;
      pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

      // 6. Final aim direction: combine clamped XZ with desired (but clamped) pitch
      // Build rotation first in yaw, then pitch (X axis)
      var finalRot = Quaternion.LookRotation(clampedXZ, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);

      // Apply to localRotation
      _controller.cannonRotationalTransform.localRotation = finalRot;

      if (debugDraw)
      {
        RuntimeDebugLineDrawer.DrawLine(
          _controller.cannonShooterTransform.position,
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