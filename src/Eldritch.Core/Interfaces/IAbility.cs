using UnityEngine;
using Zolantris.Shared;
namespace Eldritch.Core
{

  public interface IAbility
  {
    /// <summary>
    ///   Expose a coroutine handler for safe coroutines.
    /// </summary>
    CoroutineHandle coroutineHandle { get; }
    /// <summary>
    ///   Called to bind all required dependencies.
    /// </summary>
    void Bind(
      MonoBehaviour mono, // For coroutine launching
      IXenoAI ai, // AI/brain context
      IMovementController movement, // Movement control context
      IAbilityManager abilityManager, // Orchestrator
      IXenoAnimationController anim // Animation context
    );

    /// <summary>
    ///   Unbind and clean up references.
    /// </summary>
    void Unbind();

    /// <summary>
    ///   Per-frame logic, if needed.
    /// </summary>
    void UpdateAbility();

    /// <summary>
    ///   Called to request ability execution (params allow for type
    ///   safety/flexibility).
    /// </summary>
    void OnAbilityRequest(params object[] args);
  }
}