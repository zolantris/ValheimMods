using System.Collections;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class TailAttackAbility
  {
    private readonly XenoAnimationController _animationController;
    private readonly XenoDroneAI _ai;
    private readonly CoroutineHandle _coroutineHandle;
    private float _lastAttackTime;
    private bool _prevIsKinematic;
    private bool _prevUseGravity;

    private Vector3 _start, _end;

    public TailAttackAbility(MonoBehaviour monoBehaviour, XenoDroneAI ai, XenoAnimationController animationController)
    {
      _ai = ai;
      _animationController = animationController;
      _coroutineHandle = new CoroutineHandle(monoBehaviour);
    }

    public bool IsAttacking => _coroutineHandle.IsRunning;
    public bool CanAttack => !_coroutineHandle.IsRunning && _lastAttackTime + 0.2f < Time.time;

    private IEnumerator Attack(int attackType = 0, bool isSingle = true, float attackSpeed = 1f, float delay = 0f)
    {
      if (delay > 0f)
      {
        yield return new WaitForSeconds(delay);
      }
      if (!Mathf.Approximately(attackSpeed, 1f))
      {
        _animationController.SetAttackSpeed(attackSpeed, true);
      }
      _animationController.PlayAttack(attackType, false, isSingle);

      var wasNothing = false;
      var wasTailAttack = false;

      var timer = 0f;

      while (timer < 3f && (wasTailAttack == false || wasNothing == false))
      {
        timer += Time.deltaTime;
        _ai.RotateTowardPrimaryTarget();

        var stateInfo = _animationController.animator.GetCurrentAnimatorStateInfo(1);
        var isNothing = stateInfo.IsName("nothing");
        var isTailAttack = stateInfo.IsName("attack_tail");

        if (!wasNothing)
        {
          wasNothing = isNothing;
        }
        if (!wasTailAttack)
        {
          wasTailAttack = isTailAttack;
        }
        yield return null;
      }

      yield return new WaitForSeconds(0.5f);
      yield return new WaitForFixedUpdate();
    }

    public void StartAttack(int attackType = 0, bool isSingle = true, float attackSpeed = 1f, float delay = 0f)
    {
      if (!CanAttack)
      {
        return;
      }
      _lastAttackTime = Time.time;
      _coroutineHandle.Start(Attack(attackType, isSingle, attackSpeed, delay));
    }
  }
}