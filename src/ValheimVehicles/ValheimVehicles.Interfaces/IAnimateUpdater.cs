using UnityEngine;
namespace ValheimVehicles.Interfaces;

/// <summary>
/// For all animator calls.
/// </summary>
public interface IAnimateUpdater
{
  public void UpdateIK(Animator animator);
}