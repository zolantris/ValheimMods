using System;
using UnityEngine;
namespace Eldritch.Core.Abilities
{
  // class for now to debug the object data live without requiring full ref refresh
  [Serializable]
  public class DodgeAbilityConfig
  {
    public float forwardDistance = 10f;
    public float backwardDistance = 5f;
    public float sideDistance = 8f;
    public float jumpHeight = 1f;
    public float dodgeDuration = 0.5f;
    public float cooldown = 3f;
    public Vector3 defaultDodgeDirection;
  }
}