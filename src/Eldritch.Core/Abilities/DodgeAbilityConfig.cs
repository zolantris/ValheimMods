using System;
using UnityEngine;
namespace Eldritch.Core.Abilities
{
  // class for now to debug the object data live without requiring full ref refresh
  [Serializable]
  public class DodgeAbilityConfig
  {
    public float forwardDistance;
    public float backwardDistance;
    public float sideDistance;
    public float jumpHeight;
    public float dodgeDuration;
    public float cooldown;
    public Vector3 defaultDodgeDirection;
  }
}