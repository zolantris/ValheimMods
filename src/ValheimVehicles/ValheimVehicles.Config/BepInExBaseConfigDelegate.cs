using System.Collections.Generic;
using BepInEx.Configuration;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Config;

public class BepInExBaseConfigDelegate
{
  internal BepInExBaseConfigDelegate(IBepInExBaseConfig instance)
  {
    Instances.Add(instance);
  }

  public static List<IBepInExBaseConfig> Instances = new();

  public static void BindAllConfig(ConfigFile config)
  {
    LoggerProvider.LogDev($"BindAllConfigCalled discovered {Instances.Count} instances.");
    // foreach (var bepInExBaseConfig in Instances)
    // {
    //   // BindAllConfig(config);
    // }
  }
}