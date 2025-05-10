using UnityEngine;
namespace ValheimVehicles.Helpers;

public class SmoothToggleLerp
{
  public float Value { get; private set; }
  public float Speed { get; set; }

  public SmoothToggleLerp(float initialValue = 0f, float speed = 10f)
  {
    Value = Mathf.Clamp01(initialValue);
    Speed = speed;
  }

  public void Update(bool isToggled, float deltaTime)
  {
    var target = isToggled ? 1f : 0f;
    var next = Mathf.Lerp(Value, target, deltaTime * Speed);
    Value = isToggled
      ? Mathf.Clamp01(Value + (next - Value))
      : next;

    // Snap to 0 or 1 when close
    if (!isToggled && Value <= 0.001f) Value = 0f;
    if (isToggled && Value >= 0.999f) Value = 1f;
  }
}