using System;
using UnityEngine;
namespace Eldritch.Core.Abilities
{
  // class for now to debug the object data live without requiring full ref refresh
  [Serializable]
  public class DodgeAbilityConfig
  {
    public float forwardDistance = 6f;
    public float backwardDistance = 3f;
    public float sideDistance = 4.5f;
    public float jumpHeight = 1f;
    public float dodgeDuration = 0.18f;
    public float cooldown = 1f;

    // Optional (used by TryLeapAt; defaults applied if you don't set these):
    public float minGapFromTarget = 0.35f; // desired clearance at landing
    public float landingClearanceProbe = 0.4f; // sphere radius to avoid landing in geometry
    // Default dodge direction for the [ContextMenu] helper
    public Vector2 defaultDodgeDirection = new(0, 1);
  }
}