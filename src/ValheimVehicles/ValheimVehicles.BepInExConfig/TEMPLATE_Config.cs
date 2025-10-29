using BepInEx.Configuration;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class TEMPLATE_Config : BepInExBaseConfig<TEMPLATE_Config>
{
  private const string SectionName = "TEMPLATE_Config NAME";

  public static ConfigEntry<bool>? IsEnabled { get; private set; }

  public override void OnBindConfig(ConfigFile config)
  {
    // all methods add here.
  }
}