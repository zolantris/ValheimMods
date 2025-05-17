// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using UnityEngine;
using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.Controllers
{
  [Serializable]
  public class SwivelMotionStateTracker
  {
    public Vector3 FromPosition;
    public Vector3 ToPosition;

    public Quaternion FromRotation;
    public Quaternion ToRotation;

    public long StartTimeTicks;
    public float DurationSeconds;

    public bool IsInitialized => DurationSeconds > 0 && StartTimeTicks > 0;

    public void UpdateMotion(Vector3 currentPosition, Vector3 targetPosition,
      Quaternion currentRotation, Quaternion targetRotation,
      float interpolationSpeed)
    {
      FromPosition = currentPosition;
      ToPosition = targetPosition;

      FromRotation = currentRotation;
      ToRotation = targetRotation;

      var posDist = Vector3.Distance(FromPosition, ToPosition);
      var rotAngle = Quaternion.Angle(FromRotation, ToRotation);
      DurationSeconds = Mathf.Max(
        posDist / interpolationSpeed,
        rotAngle / interpolationSpeed
      );

      StartTimeTicks = ZNet.instance.GetTime().Ticks;
    }

    public Vector3 GetPredictedPosition()
    {
      if (!IsInitialized) return ToPosition;
      var t = GetLerpFraction();
      return Vector3.Lerp(FromPosition, ToPosition, t);
    }

    public Quaternion GetPredictedRotation()
    {
      if (!IsInitialized) return ToRotation;
      var t = GetLerpFraction();
      return Quaternion.Slerp(FromRotation, ToRotation, t);
    }

    private float GetLerpFraction()
    {
      if (DurationSeconds <= 0f) return 1f;

      var now = ZNet.instance.GetTime();
      var startTime = new DateTime(StartTimeTicks);
      var elapsed = (float)(now - startTime).TotalSeconds;
      return Mathf.Clamp01(elapsed / DurationSeconds);
    }

    public void SaveToZDO(ZDO zdo, string prefix)
    {
      zdo.Set($"{prefix}_FromPosition", FromPosition);
      zdo.Set($"{prefix}_ToPosition", ToPosition);
      zdo.Set($"{prefix}_FromRotation", FromRotation);
      zdo.Set($"{prefix}_ToRotation", ToRotation);
      zdo.Set($"{prefix}_StartTicks", StartTimeTicks);
      zdo.Set($"{prefix}_Duration", DurationSeconds);
    }

    public void LoadFromZDO(ZDO zdo, string prefix)
    {
      FromPosition = zdo.GetVec3($"{prefix}_FromPosition", Vector3.zero);
      ToPosition = zdo.GetVec3($"{prefix}_ToPosition", Vector3.zero);
      FromRotation = zdo.GetQuaternion($"{prefix}_FromRotation", Quaternion.identity);
      ToRotation = zdo.GetQuaternion($"{prefix}_ToRotation", Quaternion.identity);
      StartTimeTicks = zdo.GetLong($"{prefix}_StartTicks", 0L);
      DurationSeconds = zdo.GetFloat($"{prefix}_Duration", 0f);
    }

    public void Reset()
    {
      FromPosition = ToPosition = Vector3.zero;
      FromRotation = ToRotation = Quaternion.identity;
      StartTimeTicks = 0;
      DurationSeconds = 0f;
    }
  }
}