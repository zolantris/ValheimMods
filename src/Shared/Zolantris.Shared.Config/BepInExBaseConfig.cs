using BepInEx.Configuration;

#if !UNITY_RUNTIME && !UNITY_EDITOR
using ServerSync;
#endif
// ReSharper disable ArrangeNamespaceBody
namespace Zolantris.Shared
{


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
#if !UNITY_RUNTIME && !UNITY_EDITOR
    public static void BindConfig(ConfigFile config, ConfigSync configSync)
    {
      Instance.OnBindConfig(config);
      ClassValidator.ValidateRequiredNonNullFields<TSelf>();

      if (!ShouldSkipSyncOnBind)
      {
        ServerSyncConfigSyncUtil.RegisterAllConfigEntries(configSync, typeof(TSelf));
      }
      ShouldSkipSyncOnBind = false;
    }
#endif

    /// <summary>
    /// Meant for all configs to be initialized.
    /// </summary>
    public virtual void OnBindConfig(ConfigFile config)
    {
      LoggerProvider.LogError($"No implementation of OnBindConfig in parent class {typeof(TSelf).FullName}");
    }
  }

}