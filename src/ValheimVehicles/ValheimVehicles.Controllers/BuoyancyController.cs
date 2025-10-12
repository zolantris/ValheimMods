// SharedScripts/UprightTorque.cs
// Pure data upright (roll/pitch) torque solver. Call ComputeTorque(...) and AddTorque() its return.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SharedScripts;

[Serializable]
public struct UprightTorqueConfig
{
  // Righting PD (simple: a few sliders for non-programmers)
  public float Kp; // N·m/rad   (stiffness)
  public float Kd; // N·m/(rad/s) (damping)
  public float MaxTorque; // N·m per-axis clamp
  public float TorqueSlewPerSec; // N·m/s (0 = disabled)
  public float DeadAngleDeg; // ignore tiny tilt (deg)
  public float FollowTime; // s, smoothing of target-up (0 = instant)

  // Sampling (how we look at hull points)
  public int GridX; // >=1
  public int GridZ; // >=1
  public float LocalYOffset; // meters (sample plane offset)

  // Submergence weighting (how much “water” influences target)
  public float MinDepthToCount; // meters
  public float MaxDepthForWeight; // meters
  public float BottomBias; // 1 = none, 1.2–1.8 typical

  // Normalize gains by inertia so feel is consistent across hulls
  public bool InertiaAware;

  public static UprightTorqueConfig Default()
  {
    return new UprightTorqueConfig
    {
      Kp = 1800f,
      Kd = 260f,
      MaxTorque = 12000f,
      TorqueSlewPerSec = 30000f,
      DeadAngleDeg = 0.1f,
      FollowTime = 0.35f,

      GridX = 2,
      GridZ = 3,
      LocalYOffset = 0f,

      MinDepthToCount = 0.03f,
      MaxDepthForWeight = 0.8f,
      BottomBias = 1.3f,

      InertiaAware = true
    };
  }
}

/// Keep one per vehicle.
public sealed class UprightTorqueRuntime
{
  public Vector3 SmoothedTargetUp = Vector3.up;
  public Vector3 LastTorque = Vector3.zero;
  public readonly List<Vector3> LocalSamplePoints = new();
  public Bounds LastLocalBounds;

  public void Clear()
  {
    SmoothedTargetUp = Vector3.up;
    LastTorque = Vector3.zero;
    LocalSamplePoints.Clear();
    LastLocalBounds = default;
  }
}

public static class UprightTorqueSolver
{
  /// Build a uniform XZ grid of local sample points from local hull bounds.
  public static void BuildLocalSamplePoints(in Bounds localHullBounds, in UprightTorqueConfig cfg, List<Vector3> outLocalPoints)
  {
    outLocalPoints.Clear();

    var gx = Mathf.Max(1, cfg.GridX);
    var gz = Mathf.Max(1, cfg.GridZ);

    var size = localHullBounds.size * 0.98f; // slight inset
    var center = localHullBounds.center + new Vector3(0f, cfg.LocalYOffset, 0f);

    for (var ix = 0; ix < gx; ix++)
    {
      var tx = gx == 1 ? 0.5f : ix / (float)(gx - 1);
      var lx = (tx - 0.5f) * size.x;

      for (var iz = 0; iz < gz; iz++)
      {
        var tz = gz == 1 ? 0.5f : iz / (float)(gz - 1);
        var lz = (tz - 0.5f) * size.z;

        outLocalPoints.Add(center + new Vector3(lx, 0f, lz));
      }
    }
  }

  /// Compute a single world-space righting torque (roll/pitch only).
  /// sampleWaterLevel: worldPos -> water Y. Pass null to use world-up as reference.
  /// sampleWaterNormal: worldPos -> water normal. Pass null to use Vector3.up.
  public static Vector3 ComputeTorque(
    in UprightTorqueConfig cfg,
    float dt,
    Transform transform,
    in Bounds localHullBounds,
    Func<Vector3, float> sampleWaterLevel, // nullable
    Func<Vector3, Vector3> sampleWaterNormal, // nullable
    Vector3 angularVelocityWorld,
    Vector3 inertiaTensorLocal,
    Quaternion inertiaTensorRotationLocal,
    UprightTorqueRuntime runtime)
  {
    if (runtime == null) runtime = new UprightTorqueRuntime();

    // Lazy grid rebuild on bounds change
    if (runtime.LocalSamplePoints.Count == 0 || !runtime.LastLocalBounds.Equals(localHullBounds))
    {
      BuildLocalSamplePoints(localHullBounds, cfg, runtime.LocalSamplePoints);
      runtime.LastLocalBounds = localHullBounds;
    }

    var tf = transform;
    var n = runtime.LocalSamplePoints.Count;
    if (n == 0) return SlewToTorque(Vector3.zero, cfg, dt, runtime);

    // ---- 1) Submergence weighting & target normal ----
    var minDepth = Mathf.Max(0f, cfg.MinDepthToCount);
    var maxDepth = Mathf.Max(minDepth + 0.01f, cfg.MaxDepthForWeight);
    var biasK = Mathf.Max(0f, cfg.BottomBias - 1f);

    var weightSum = 0f;
    var waterNormalWeighted = Vector3.zero;

    for (var i = 0; i < n; i++)
    {
      var wp = tf.TransformPoint(runtime.LocalSamplePoints[i]);

      float depth;
      if (sampleWaterLevel != null)
      {
        var waterY = sampleWaterLevel(wp);
        depth = waterY - wp.y;
      }
      else
      {
        depth = 1f; // world-up mode: pretend submerged for weighting
      }

      if (depth <= minDepth) continue;

      var d = Mathf.Min(depth, maxDepth);
      var liftK = SmoothStep01(d / maxDepth);

      var w = d * liftK * (1f + biasK * Mathf.Clamp01(depth / maxDepth));
      weightSum += w;

      if (sampleWaterNormal != null)
      {
        var nW = sampleWaterNormal(wp);
        if (nW.sqrMagnitude > 0.25f) waterNormalWeighted += nW.normalized * w;
      }
    }

    var nWater = sampleWaterNormal != null && weightSum > 1e-5f
      ? (waterNormalWeighted / (weightSum + 1e-6f)).normalized
      : Vector3.up;

    if (weightSum <= 1e-5f)
    {
      // no reliable water info → fall back to world up
      nWater = Vector3.up;
    }

    if (nWater.sqrMagnitude < 0.5f) nWater = Vector3.up;

    // Smooth follow for target-up
    var a = cfg.FollowTime <= 0f ? 1f : 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, cfg.FollowTime));
    runtime.SmoothedTargetUp = Vector3.Slerp(runtime.SmoothedTargetUp, nWater, a).normalized;
    if (runtime.SmoothedTargetUp.sqrMagnitude < 0.5f) runtime.SmoothedTargetUp = Vector3.up;

    // Fade with how much “water say” we have (keeps calm behavior graceful)
    var weightFade = Mathf.Clamp01(weightSum / (n * maxDepth));

    // ---- 2) PD torque (roll/pitch only) ----
    var upHull = tf.up;
    var tiltAxis = Vector3.Cross(upHull, runtime.SmoothedTargetUp);
    var sinTheta = tiltAxis.magnitude;
    var angle = Mathf.Asin(Mathf.Clamp(sinTheta, -1f, 1f)); // radians

    if (Mathf.Abs(angle) < cfg.DeadAngleDeg * Mathf.Deg2Rad)
      return SlewToTorque(Vector3.zero, cfg, dt, runtime);

    var axisN = tiltAxis / (sinTheta + 1e-6f);
    var angNoYaw = Vector3.ProjectOnPlane(angularVelocityWorld, runtime.SmoothedTargetUp);

    float Kp = cfg.Kp, Kd = cfg.Kd;
    if (cfg.InertiaAware)
    {
      // Effective inertia about axisN (project onto local principal moments)
      var worldToLocalPrincipal = Quaternion.Inverse(inertiaTensorRotationLocal * transform.rotation);
      var axisLocal = (worldToLocalPrincipal * axisN).normalized;
      var Ieff = Mathf.Max(0.01f,
        inertiaTensorLocal.x * axisLocal.x * axisLocal.x +
        inertiaTensorLocal.y * axisLocal.y * axisLocal.y +
        inertiaTensorLocal.z * axisLocal.z * axisLocal.z);
      var invI = 1f / Ieff;
      Kp *= invI;
      Kd *= invI;
    }

    var tauP = axisN * (Kp * angle);
    var tauD = -Kd * angNoYaw;
    // use fade to soften on very light immersion, but never fully disable
    var fade = Mathf.Max(0.2f, Mathf.Clamp01(weightSum / (n * maxDepth)));
    var tau = (tauP + tauD) * fade;

    // remove yaw torque component
    tau -= Vector3.Project(tau, runtime.SmoothedTargetUp);

    // clamp in roll/pitch plane
    tau = ClampInPlane(tau, runtime.SmoothedTargetUp, cfg.MaxTorque);

    return SlewToTorque(tau, cfg, dt, runtime);
  }

  // ---- helpers ----
  private static Vector3 SlewToTorque(Vector3 desired, in UprightTorqueConfig cfg, float dt, UprightTorqueRuntime rt)
  {
    var maxStep = cfg.TorqueSlewPerSec <= 0f ? float.PositiveInfinity : cfg.TorqueSlewPerSec * dt;
    var tau = Vector3.MoveTowards(rt.LastTorque, desired, maxStep);
    rt.LastTorque = tau;
    return tau;
  }

  private static Vector3 ClampInPlane(Vector3 tau, Vector3 planeNormal, float maxPerAxis)
  {
    var a1 = Vector3.Cross(planeNormal, Vector3.right).normalized;
    if (a1.sqrMagnitude < 0.5f) a1 = Vector3.Cross(planeNormal, Vector3.forward).normalized;
    var a2 = Vector3.Cross(planeNormal, a1).normalized;

    var t1 = Mathf.Clamp(Vector3.Dot(tau, a1), -maxPerAxis, maxPerAxis);
    var t2 = Mathf.Clamp(Vector3.Dot(tau, a2), -maxPerAxis, maxPerAxis);
    return a1 * t1 + a2 * t2;
  }

  private static float SmoothStep01(float x)
  {
    var t = Mathf.Clamp01(x);
    return t * t * (3f - 2f * t);
  }
}