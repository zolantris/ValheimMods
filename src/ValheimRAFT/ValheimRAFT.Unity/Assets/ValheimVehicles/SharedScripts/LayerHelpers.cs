using System.Collections.Generic;
using UnityEngine;

// todo figure out where utils is coming from that causes a conflict.
// Looks like a static class from valheim
namespace ValheimVehicles.SharedScripts
{
  public static class LayerHelpers
  {
    public const int CustomRaftLayer = 29;

    public static LayerMask PhysicalLayers = LayerMask.GetMask("Default",
      "character", "piece",
      "terrain",
      "static_solid", "Default_small", "character_net", "vehicle",
      LayerMask.LayerToName(CustomRaftLayer));

    public static LayerMask BlockingColliderExcludeLayers = LayerMask.GetMask(
      "character", "character_net", "character_trigger", "viewbox",
      "character_net", "character_nonenv",
      LayerMask.LayerToName(CustomRaftLayer));

    public static List<string> ActiveLayersForBlockingMask = new();

    public static int NonSolidLayer =
      LayerMask.NameToLayer("piece_nonsolid");

    public static int IgnoreRaycastLayer =
      LayerMask.NameToLayer("Ignore Raycast");

    public static int CharacterLayer = LayerMask.NameToLayer("character");

    public static bool IsContainedWithinMask(int layer, LayerMask mask)
    {
      return (mask.value & (1 << layer)) != 0;
    }

    // todo fix jitters with low headroom at water level
    // [HarmonyPostfix(typeof(GameCamera), nameof(GameCamera.UpdateNearClipping))]
    public static List<int> GetActiveLayers(LayerMask mask)
    {
      ActiveLayersForBlockingMask.Clear();
      var activeLayers = new List<int>();

      // Iterate through all 32 possible layers
      for (var i = 0; i < 32; i++)
        // Check if the i-th bit in the mask is set
        if ((mask.value & (1 << i)) != 0)
        {
          var name = LayerMask.LayerToName(i);
          ActiveLayersForBlockingMask.Add(name);
          activeLayers.Add(i); // Add the layer index to the list
        }

#if DEBUG
      Debug.Log(string.Join(",", ActiveLayersForBlockingMask));
#endif

      return activeLayers;
    }

    /// <summary>
    ///   Shortcut to combining masks
    /// </summary>
    /// <param name="originalMask"></param>
    /// <param name="layerToAdd"></param>
    /// <returns></returns>
    public static LayerMask CombineLayerMask(LayerMask originalMask,
      int layerToAdd)
    {
      return originalMask | (1 << layerToAdd);
    }
  }
}