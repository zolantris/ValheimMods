using BepInEx.Configuration;

namespace ValheimVehicles.Config;

/// <summary>
/// Todo add support for >= C# 11 for static abstract which should be used for config sections
/// </summary>
public interface IConfigSection
{
  public ConfigFile? Config { get; set; }

  public void BindConfig(ConfigFile config);
}