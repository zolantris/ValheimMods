using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class CustomMeshConfig : BepInExBaseConfig<CustomMeshConfig>
{
  public static ConfigEntry<bool> EnableCustomWaterMeshCreators =
    null!;

  public static ConfigEntry<bool> EnableCustomWaterMeshTestPrefabs =
    null!;

  private const string SectionKey = "CustomMesh";


  public override void OnBindConfig(ConfigFile config)
  {
    EnableCustomWaterMeshCreators = config.BindUnique(
      SectionKey,
      "Water Mask Prefabs Enabled",
      true,
      ConfigHelpers.CreateConfigDescription(
        "Allows placing a dynamically sized cube that removes all water meshes intersecting with it. This also removes all water meshes when looking through it. So use it wisely, it's not perfect",
        true));
    EnableCustomWaterMeshTestPrefabs = config.BindUnique(
      SectionKey,
      "Enable Testing 4x4 Water Mask Prefabs, these are meant for demoing water obstruction.",
      false,
      ConfigHelpers.CreateConfigDescription(
        "login/logoff point moves player to last interacted bed or first bed on ship",
        true));
  }
}