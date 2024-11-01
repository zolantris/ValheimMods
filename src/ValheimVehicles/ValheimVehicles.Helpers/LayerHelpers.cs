using UnityEngine;

// todo figure out where utils is coming from that causes a conflict.
// Looks like a static class from valheim
namespace ValheimVehicles.LayerUtils;

public static class LayerHelpers
{
  public static LayerMask PhysicalLayers = LayerMask.GetMask("Default",
    "character", "piece",
    "terrain",
    "static_solid", "Default_small", "character_net", "vehicle",
    LayerMask.LayerToName(29));

  public static int NonSolidLayer =
    LayerMask.NameToLayer("piece_nonsolid");

  public static int IgnoreRaycastLayer =
    LayerMask.NameToLayer("Ignore Raycast");

  public static int CharacterLayer = LayerMask.NameToLayer("character");
}