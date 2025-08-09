using System;
using Jotunn.Managers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;
namespace ValheimVehicles.Prefabs.Registry;

public interface IPrefabRegistryController
{
  public void OnRegister();
}

/// <summary>
/// Keys can be static, but the BindConfig and some helper methods should be always used.
///
/// TSelf gets the parent extended class and sends it into the BindConfig's StaticFieldValidator.ValidateRequiredNonNullFields
/// </summary>
public class RegisterPrefab<TSelf> : IPrefabRegistryController
  where TSelf : RegisterPrefab<TSelf>, new()
{
  internal static readonly TSelf Instance = new();

  /// <summary>
  /// Always call register.
  /// </summary>
  public static void Register()
  {
    try
    {
      if (PrefabManager.Instance == null || PieceManager.Instance == null)
      {
        LoggerProvider.LogError($"Register called in {typeof(TSelf).FullName} but PrefabManager or PieceManager instances were null");
        return;
      }
      Instance.OnRegister();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"Failed to register prefab {e}");
    }
  }

  public virtual void OnRegister() {}
}