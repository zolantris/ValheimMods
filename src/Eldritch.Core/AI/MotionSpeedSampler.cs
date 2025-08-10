// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;

namespace Eldritch.Core
{
  public struct MotionSampleState
  {
    public Vector3 PrevPos;
    public Quaternion PrevRot;
    public bool HasPrev;
    public float FwdEma;
  }

  public static class MotionSpeedSampler
  {
    /// Returns normalized signed forward speed (-1..1) from displacement.
    public static float SampleNormalizedMoveSpeed(
      Transform motionRoot,
      ref MotionSampleState state,
      float dt,
      out bool turningInPlace,
      out float yawDegPerSec,
      float maxForwardSpeed = 6f,
      float deadzoneSpeed = 0.05f,
      float smoothingTau = 0.08f,
      float teleportDist = 2f)
    {
      turningInPlace = false;
      yawDegPerSec = 0f;
      if (!motionRoot || dt <= 0f) return 0f;

      var posNow = motionRoot.position;
      var rotNow = motionRoot.rotation;

      if (!state.HasPrev)
      {
        state.PrevPos = posNow;
        state.PrevRot = rotNow;
        state.HasPrev = true;
        state.FwdEma = 0f;
        return 0f;
      }

      var worldDelta = posNow - state.PrevPos;
      if (worldDelta.sqrMagnitude > teleportDist * teleportDist)
      {
        state.PrevPos = posNow;
        state.PrevRot = rotNow;
        state.FwdEma = 0f;
        return 0f;
      }

      // Displacement in previous local frame; ignore Y
      var localDelta = Quaternion.Inverse(state.PrevRot) * worldDelta;
      localDelta.y = 0f;

      var fwd = localDelta.z / dt;
      if (Mathf.Abs(fwd) < deadzoneSpeed) fwd = 0f;

      // Signed yaw rate (deg/s)
      var dRot = rotNow * Quaternion.Inverse(state.PrevRot);
      dRot.ToAngleAxis(out var angDeg, out var axis);
      if (angDeg > 180f) angDeg -= 360f;
      var yawSign = Mathf.Sign(Vector3.Dot(axis, Vector3.up));
      yawDegPerSec = angDeg * yawSign / dt;

      // EMA smoothing
      var a = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, smoothingTau));
      state.FwdEma = Mathf.Lerp(state.FwdEma, fwd, a);

      state.PrevPos = posNow;
      state.PrevRot = rotNow;

      // Normalize to -1..1
      return Mathf.Clamp(state.FwdEma / Mathf.Max(0.01f, maxForwardSpeed), -1f, 1f);
    }
  }
}