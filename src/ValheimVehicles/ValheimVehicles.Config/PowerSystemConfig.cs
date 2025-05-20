using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using Zolantris.Shared;
namespace ValheimVehicles.Config;

public class PowerSystemConfig : BepInExBaseConfig<PowerSystemConfig>
{
  public static ConfigEntry<float> PowerPylonRange;

  public const string SectionKey = "PowerSystem";
  public override void OnBindConfig(ConfigFile config)
  {
    PowerPylonRange = config.BindUnique(SectionKey, "PowerRangePerPowerItem", 10f, ConfigHelpers.CreateConfigDescription("The power range per power item. Large values will make huge networks. Max range is 50. But this could span entire continents as ZDOs are not limited to render distance.", true, false, new AcceptableValueRange<float>(0, 50f)));
  }
}