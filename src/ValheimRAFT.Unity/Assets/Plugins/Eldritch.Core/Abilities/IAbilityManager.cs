using UnityEngine;
namespace Eldritch.Core.Abilities
{
  public interface IAbilityManager
  {
    Transform transform { get; }
    Rigidbody _rb { get; }
  }
}