using System.Collections.Generic;
using System.Linq;
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
      forwardDistance = 6.5f,
      backwardDistance = 5f,
      sideDistance = 5f,
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
    public bool IsTailAttacking => tailAttackAbility.IsAttacking;
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

    /// <summary>
    /// Leap toward AI.PrimaryTarget and land just in front (no collision with target).
    /// Returns true if a leap was started.
    /// </summary>
    public bool RequestLeapTowardEnemy(float minGapOverride = -0.1f)
    {
      if (tailAttackAbility.IsAttacking) return false;
      if (!dodgeAbility.CanDodge || AI == null || !AI.PrimaryTarget) return false;

      if (AI.IsAttacking())
        animationController.StopAttack();

      var target = AI.PrimaryTarget;

      // Prefer RB; only fetch colliders if NO rigidbody is available.
      var targetRB = AI.PrimaryTargetRB ? AI.PrimaryTargetRB : target.GetComponentInChildren<Rigidbody>();
      Collider[] targetCols = null;
      if (!targetRB)
      {
        targetCols = target.GetComponentsInChildren<Collider>().Where(x => x && !x.isTrigger).ToArray();
      }

      // Our hull (keep this – we use colliders to estimate our half-extent along travel dir)
      var selfCols =
        animationController.allColliders != null && animationController.allColliders.Count > 0
          ? new List<Collider>(animationController.allColliders).Where(x => x && !x.isTrigger).ToArray()
          : GetComponentsInChildren<Collider>()?.Where(x => x && !x.isTrigger);

      var ok = dodgeAbility.TryLeapAt(
        targetRB,
        _rb
      );

      if (!ok) return false;

      var skip = new[] { "Tail" };
      animationController.PlayJump(skip);


      RequestAttack(1, true, 2f, Mathf.Max(0f, dodgeAbilityConfig.dodgeDuration - 1f));

      return true;
    }

    public void RequestDodge(Vector2 dir)
    {
      if (tailAttackAbility.IsAttacking) return;
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
        if (camouflageAbility.IsActive)
        {
          camouflageAbility.Deactivate();
        }
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

      if (!_rb)
      {
        _rb = GetComponent<Rigidbody>();
      }

      if (!AI)
      {
        AI = GetComponent<XenoDroneAI>();
      }

      dodgeAbility ??= new DodgeAbility(this, dodgeAbilityConfig, transform, _rb);
      dodgeAbility.config ??= dodgeAbilityConfig;

      tailAttackAbility ??= new TailAttackAbility(this, AI, animationController);


      camouflageAbility ??= new CamouflageAbility(this, camouflageAbilityConfig, camouflageMat, animationController);
      camouflageAbility.config ??= camouflageAbilityConfig;
    }

    // Add more orchestration: combo windows, cancels, priorities, etc.

    private bool IsForwardDodge(Vector3 dir)
    {
      return dir.x < dir.y && dir.y > 0;
    }
  }
}