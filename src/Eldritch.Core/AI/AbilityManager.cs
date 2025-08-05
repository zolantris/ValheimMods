using Eldritch.Core.Abilities;
using UnityEngine;
namespace Eldritch.Core
{
  public class AbilityManager : MonoBehaviour
  {

    public DodgeAbilityConfig dodgeAbilityConfig = new()
    {
      forwardDistance = 6f,
      backwardDistance = 3f,
      sideDistance = 4.5f,
      jumpHeight = 1f,
      dodgeDuration = 0.18f,
      cooldown = 1f
    };
    public DodgeAbility dodgeAbility { get; private set; }
    // public TailAttackAbility TailAttackAbility { get; private set; }
    public XenoAIMovementController Movement { get; private set; }
    public XenoAnimationController AnimationController { get; private set; }
    public XenoDroneAI AI { get; private set; }

    private void Awake()
    {
      // Assign all abilities (pass required refs, configs)
      AI = GetComponent<XenoDroneAI>();
      Movement = GetComponent<XenoAIMovementController>();
      AnimationController = GetComponent<XenoAnimationController>();
      InitAbilities();
    }

    private void OnEnable()
    {
      InitAbilities();
    }

    public void InitAbilities()
    {
      dodgeAbility ??= new DodgeAbility(this, dodgeAbilityConfig, transform, Movement.Rb);
    }

    public void TryDodge(Vector3 dir)
    {
      // High-level orchestration logic
      if (dodgeAbility.CanDodge)
      {
        dodgeAbility.TryDodge(dir);
        if (IsForwardDodge(dir))
        {
          AnimationController.PlayAttack(1);
        }
      }
    }

    public void TryAttack() { ; }

    // Add more orchestration: combo windows, cancels, priorities, etc.

    private bool IsForwardDodge(Vector3 dir)
    {
      // Use same angle logic as before
      var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
      return Mathf.Abs(angle) < 45f;
    }
  }
}