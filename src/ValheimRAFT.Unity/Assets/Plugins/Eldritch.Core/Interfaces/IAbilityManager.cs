using UnityEngine;
namespace Eldritch.Core
{
  public interface IAbilityManager
  {
    // Expose access to all registered abilities, if needed:
    // e.g. IAbility GetAbility<T>() or DodgeAbility DodgeAbility { get; }
    void RequestDodge(Vector2 direction);
    void RequestAttack(int attackType = 0, bool isSingle = true, float attackSpeed = 1f, float delay = 0f);
  }
}