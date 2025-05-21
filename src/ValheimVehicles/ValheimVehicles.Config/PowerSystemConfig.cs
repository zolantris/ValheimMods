using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using Zolantris.Shared;
namespace ValheimVehicles.Config;

public class PowerSystemConfig : BepInExBaseConfig<PowerSystemConfig>
{
  public static ConfigEntry<float> PowerPylonRange = null!;
  public static ConfigEntry<float> PowerMechanismRange = null!;

  public const string SectionKey = "PowerSystem";
  public override void OnBindConfig(ConfigFile config)
  {
    PowerPylonRange = config.BindUnique(SectionKey, "PowerRangePerPowerItem", 10f, ConfigHelpers.CreateConfigDescription("The power range per power pylon prefab. Large values will make huge networks. Max range is 50. But this could span entire continents as ZDOs are not limited to render distance.", true, false, new AcceptableValueRange<float>(0, 50f)));

    PowerMechanismRange = config.BindUnique(SectionKey, "PowerMechanismRange", 4f, ConfigHelpers.CreateConfigDescription("The power range per mechanism power item. This excludes pylons and is capped at a lower number. These items are meant to be connected to pylons but at higher values could connect together.", true, false, new AcceptableValueRange<float>(0, 20f)));
  }
}