using UnityEngine;
namespace Eldritch.Core
{
  public interface IXenoAnimationController
  {
    void SetMoveSpeed(float speed, bool shouldBypass = false);
    void PlayJump(string[] skipTransformNames = null);
    void PlaySleepingAnimation(bool isSleeping);
    void PlayDodgeAnimation(Vector3 dodgeDir);
    void StopDodgeAnimation();
    // Add more: PlayAttack, SetTrigger(string), etc.
  }
}