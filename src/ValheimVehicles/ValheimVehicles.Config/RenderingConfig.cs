using BepInEx.Configuration;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Config;

public class RenderingConfig : BepInExBaseConfig<RenderingConfig>
{
  public static ConfigEntry<bool> EnableVehicleClusterMeshRendering = null!;
  public static ConfigEntry<int> ClusterRenderingPieceThreshold = null!;
#if DEBUG
  public static ConfigEntry<bool> EnableWorldClusterMeshRendering = null!;
#endif

  private const string SectionKey = "Hud";

  public override void OnBindConfig(ConfigFile config)
  {
    EnableVehicleClusterMeshRendering = config.BindUnique(SectionKey,
      "EnableVehicleClusterRendering", false,
      ConfigHelpers.CreateConfigDescription("Cluster rendering efficiently improves how the raft renders. It will offer 50% boost in FPS for larger ships. You can reach upwards of 90 FPS on a 3000 piece ship vs 40-45fps. It does this by combining meshes so editing and damaging these components is a bit more abrupt. WearNTear animations go away, but the items can still be broken. Updates require re-building the meshes affected so this can be a bit heavy, but not as heavy as bounds collider rebuild.", true, false)
    );

    ClusterRenderingPieceThreshold = config.BindUnique(SectionKey,
      "ClusterRenderingPieceThreshold", 500,
      ConfigHelpers.CreateConfigDescription($"Set the number of pieces to render threshold for using cluster rendering. smaller ships will not have cluster rendering apply. Lowest number of items possible is 10 as it's less efficient to run this on smaller vehicles. Recommended range is above 100 and max is 1000 which will significant improve the ship. If you do not want it enable turn off the feature via the key: <{EnableVehicleClusterMeshRendering.Definition.Key}>.", true, false, new AcceptableValueRange<int>(10, 1000)));

    ClusterRenderingPieceThreshold.SettingChanged += (_, _) =>
    {
      BasePiecesController.clusterThreshold = ClusterRenderingPieceThreshold.Value;
    };

#if DEBUG
    EnableWorldClusterMeshRendering = config.BindUnique(SectionKey,
      "EnableWorldClusterMeshRendering", false,
      "Cluster rendering efficiently improves how the whole world renders and shares meshes. It will allow for significantly higher FPS at the potential cost of wearNTear latency. It is debug only provided and will not be enabled until wearNtear can be optimize with this.");
#endif
  }
}