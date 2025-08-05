using System.Collections;
using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public class TailAttackAbility
  {
    private readonly XenoAnimationController _animationController;
    private readonly CoroutineHandle _coroutineHandle;
    private readonly Transform _owner;
    private float _lastAttackTime;
    private bool _prevIsKinematic;
    private bool _prevUseGravity;

    private Vector3 _start, _end;

    public TailAttackAbility(MonoBehaviour monoBehaviour, XenoAnimationController animationController)
    {
      _animationController = animationController;
      _coroutineHandle = new CoroutineHandle(monoBehaviour);
    }

    public bool CanAttack => !_coroutineHandle.IsRunning && _lastAttackTime + 0.2f < Time.time;

    public IEnumerator Attack(int attackType = 0, bool isSingle = true, float attackSpeed = 1f, float delay = 0f)
    {
      yield return new WaitForSeconds(delay);
      if (!Mathf.Approximately(attackSpeed, 1f))
      {
        _animationController.SetAttackSpeed(attackSpeed, true);
      }
      _animationController.PlayAttack(attackType, false, isSingle);
      yield return null;
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