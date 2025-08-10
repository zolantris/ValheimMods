using Eldritch.Core.Abilities;
using UnityEngine;
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{
  public class AbilityManager : MonoBehaviour, IAbilityManager
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
    public CamouflageAbilityConfig camouflageAbilityConfig = new()
    {
      cooldown = 10f,
      duration = 5f
    };

    public static Material camouflageMat_Static;
    public Material camouflageMat;

    // public TailAttackAbility TailAttackAbility { get; private set; }
    public XenoAIMovementController movementController;
    public XenoAnimationController animationController;
    public XenoDroneAI AI;
    public Rigidbody _rb;

    public CamouflageAbility camouflageAbility { get; private set; }
    public DodgeAbility dodgeAbility { get; private set; }
    public TailAttackAbility tailAttackAbility { get; private set; }
    public bool IsDodging => dodgeAbility.IsDodging;
    private void Awake()
    {
      _rb = GetComponent<Rigidbody>();

      // Assign all abilities (pass required refs, configs)
      AI = GetComponent<XenoDroneAI>();
      movementController = GetComponent<XenoAIMovementController>();

      if (!animationController)
      {
        animationController = GetComponentInChildren<XenoAnimationController>();
      }
      InitAbilities();
      // todo might have to decouple init to be first
    }

    private void OnEnable()
    {
      InitAbilities();
    }

    public void RequestDodge(Vector2 dir)
    {
      // High-level orchestration logic
      if (dodgeAbility.CanDodge)
      {
        if (AI.IsAttacking())
        {
          animationController.StopAttack();
        }


        var hasRun = dodgeAbility.TryDodge(dir);

        if (hasRun)
        {
          // todo rig jump + tail attack. Otherwise we have to use running animation + attack
          var skipTransforms = new[] { "Tail" };
          animationController.PlayJump(skipTransforms);
          // animationController.StopMovement()
          if (IsForwardDodge(dir))
          {
            RequestAttack(1, true, 2f, Mathf.Max(0f, dodgeAbilityConfig.dodgeDuration - 1f));
          }
        }
      }
    }

    public void RequestAttack(int attackType = 0, bool isSingle = true, float attackSpeed = 1f, float delay = 0f)
    {
      if (attackType == 1)
      {
        tailAttackAbility.StartAttack(attackType, isSingle, attackSpeed, delay);
      }
    }

    [ContextMenu("Dodge (Default dir)")]
    public void RequestDodge()
    {
      RequestDodge(dodgeAbilityConfig.defaultDodgeDirection);
    }

    private void InitAbilities()
    {
      if (!animationController)
      {
        animationController = GetComponentInChildren<XenoAnimationController>();
      }

      if (!camouflageMat && camouflageMat_Static)
      {
        camouflageMat = camouflageMat_Static;
      }

      dodgeAbility ??= new DodgeAbility(this, dodgeAbilityConfig, transform, _rb);
      tailAttackAbility ??= new TailAttackAbility(this, animationController);
      camouflageAbility ??= new CamouflageAbility(this, camouflageAbilityConfig, camouflageMat, animationController);
    }

    // Add more orchestration: combo windows, cancels, priorities, etc.

    private bool IsForwardDodge(Vector3 dir)
    {
      return dir.x < dir.y && dir.y > 0;
    }
  }
}