using BepInEx.Configuration;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class Mod_PieceOverlapConfig : BepInExBaseConfig<Mod_PieceOverlapConfig>
{
  public static ConfigEntry<bool> PieceOverlap_Enabled { get; set; }

  private const string SectionKey = "Mod: PieceOverlap";

  public override void OnBindConfig(ConfigFile config)
  {
    PieceOverlap_Enabled = config.BindUnique(SectionKey, "PieceOverlap_Enabled", true, "Prevents piece overlapping which causes flickering in valheim. This is applied on placing a piece, checks for overlapping piece visuals (meshes) and modify the current piece so that it's position is not overlapping with any other pieces. This position update is extremely small and synced in multiplayer. This does not fix complex shaders which can transform or act similarly to meshes");
  }
}