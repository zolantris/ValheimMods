using UnityEngine;
namespace Eldritch.Core
{
  public interface IMovementController
  {
    Transform Transform { get; }
    float MoveSpeed { get; }
    bool IsDodging { get; }
    void Move(Vector3 direction, float speedMultiplier = 1f);
    void Jump(float force);
    // Add: TryDodge, TrySprint, TryClimb, etc., if you want
  }
}