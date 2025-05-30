using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Validation;
using ValheimVehicles.ValheimVehicles.Plugins;
namespace ValheimVehicles.BepInExConfig;

public interface IBepInExBaseConfig
{
  public void OnBindConfig(ConfigFile config);
}

/// <summary>
/// Keys can be static, but the BindConfig and some helper methods should be always used.
///
/// TSelf gets the parent extended class and sends it into the BindConfig's StaticFieldValidator.ValidateRequiredNonNullFields
/// </summary>
public class BepInExBaseConfig<TSelf> : IBepInExBaseConfig
  where TSelf : BepInExBaseConfig<TSelf>, new()
{
  private static readonly TSelf Instance = new();

  public static bool ShouldSkipSyncOnBind = false;
  /// <summary>
  /// We must always validate the Config class for requires null values. 
  /// </summary>
  /// <param name="config"></param>
  public static void BindConfig(ConfigFile config)
  {
    Instance.OnBindConfig(config);
    ClassValidator.ValidateRequiredNonNullFields<TSelf>();

    if (!ShouldSkipSyncOnBind)
    {
      ServerSyncConfigSyncUtil.RegisterAllConfigEntries(ValheimVehiclesPlugin.ModConfigSync, typeof(TSelf));
    }
    ShouldSkipSyncOnBind = false;
  }

  /// <summary>
  /// Meant for all configs to be initialized.
  /// </summary>
  public virtual void OnBindConfig(ConfigFile config)
  {
    LoggerProvider.LogError($"No implementation of OnBindConfig in parent class {typeof(TSelf).FullName}");
  }
}