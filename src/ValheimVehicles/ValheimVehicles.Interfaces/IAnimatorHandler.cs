using UnityEngine;
namespace ValheimVehicles.Interfaces;

/// <summary>
/// For all animator calls.
/// </summary>
public interface IAnimatorHandler
{
  public void UpdateIK(Animator animator);
}