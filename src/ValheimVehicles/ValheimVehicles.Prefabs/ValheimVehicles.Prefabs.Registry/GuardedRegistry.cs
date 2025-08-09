using System;
using Jotunn.Managers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Prefabs.Registry;

internal interface IGuardedRegistryInternal
{
  internal abstract bool IsValid();
  internal abstract void Register();
}

internal interface IGuardedRegistry
{
  /// <summary>
  /// Main method meant to be overriden
  /// </summary>
  public abstract void OnRegister();
}

/// <summary>
/// Test class to see if it's worth making this shareable
/// </summary>
public abstract class GuardedRegistry<T> : IGuardedRegistry, IGuardedRegistryInternal
{
  public static bool IsValid()
  {
    // return PrefabManager.Instance != null && PieceManager.Instance != null;
    return PieceManager.Instance != null;
  }

  // Internal methods only.
  bool IGuardedRegistryInternal.IsValid()
  {
    return IsValid();
  }

  // Must be overriden by class extending this.
  public abstract void OnRegister();

  public void Register()
  {
    if (!IsValid())
    {
      return;
    }

    try
    {
      OnRegister();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError(
        $"An error occurred while registering component of type '{typeof(T).FullName}':\n{e}"
      );
    }
  }
}