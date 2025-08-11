using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core.Abilities
{
  public abstract class AbilityBase : IAbility
  {
    protected MonoBehaviour Mono { get; private set; }
    protected IXenoAI AI { get; private set; }
    protected IMovementController Movement { get; private set; }
    protected IAbilityManager AbilityManager { get; private set; }
    protected IXenoAnimationController Anim { get; private set; }

    public CoroutineHandle coroutineHandle { get; private set; }

    public virtual void Bind(
      MonoBehaviour mono,
      IXenoAI ai,
      IMovementController movement,
      IAbilityManager abilityManager,
      IXenoAnimationController anim)
    {
      Mono = mono;
      AI = ai;
      Movement = movement;
      AbilityManager = abilityManager;
      Anim = anim;
      coroutineHandle = new CoroutineHandle(mono);
    }

    public void Bind(MonoBehaviour mono, IXenoAI ai, IMovementController movement, Core.IAbilityManager abilityManager, IXenoAnimationController anim)
    {
      throw new System.NotImplementedException();
    }
    public virtual void Unbind()
    {
      coroutineHandle?.Stop();
      Mono = null;
      AI = null;
      Movement = null;
      AbilityManager = null;
      Anim = null;
    }

    public abstract void UpdateAbility();
    public abstract void OnAbilityRequest(params object[] args);
  }
}