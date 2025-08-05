using UnityEngine;
namespace Eldritch.Core
{
  public interface IAbilityManager
  {
    // Expose access to all registered abilities, if needed:
    // e.g. IAbility GetAbility<T>() or DodgeAbility DodgeAbility { get; }
    void RequestDodge(Vector3 direction);
    void RequestAttack(int attackType = 0); // Tail, claw, bite, etc.
    // Add combo/chain logic as needed
  }
}