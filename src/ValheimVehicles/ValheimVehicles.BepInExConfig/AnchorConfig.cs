using BepInEx.Configuration;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class AnchorConfig : BepInExBaseConfig<AnchorConfig>
{
  public static ConfigEntry<bool> HorizontalLock = null!;
  public static ConfigEntry<bool> VerticalLock = null!;

  public override void OnBindConfig(ConfigFile config)
  {
    HorizontalLock = config.BindUnique("Anchor", "HorizontalLock", true,
      ConfigHelpers.CreateConfigDescription(
        "Keep anchored ships at their parked horizontal position. Rotation remains locked.", true, true));
    VerticalLock = config.BindUnique("Anchor", "VerticalLock", false,
      ConfigHelpers.CreateConfigDescription(
        "Keep anchored ships at their parked height. Leave disabled to allow vertical wave bobbing.", true, true));
  }
}
