using System;
namespace Eldritch.Core.Abilities
{
  [Serializable]
  public struct DodgeAbilityConfig
  {
    public float forwardDistance;
    public float backwardDistance;
    public float sideDistance;
    public float jumpHeight;
    public float dodgeDuration;
    public float cooldown;

    // Optional: constructor for easy runtime init
    public DodgeAbilityConfig(float f, float b, float s, float j, float d, float c)
    {
      forwardDistance = f;
      backwardDistance = b;
      sideDistance = s;
      jumpHeight = j;
      dodgeDuration = d;
      cooldown = c;
    }
  }
}