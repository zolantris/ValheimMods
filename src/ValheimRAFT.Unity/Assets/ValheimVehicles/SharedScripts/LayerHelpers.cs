#region

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

#endregion

// todo figure out where utils is coming from that causes a conflict.
// Looks like a static class from valheim
// ReSharper disable ArrangeNamespaceBody

// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts
  {
    public static class LayerHelpers
    {
      public const int CustomRaftLayer = 29;

      public static int TerrainLayer = LayerMask.NameToLayer("terrain");
      public static int UILayer = LayerMask.NameToLayer("UI");
      public static int PieceLayer = LayerMask.NameToLayer("piece");
      public static int DefaultLayer = LayerMask.NameToLayer("Default");
      public static int DefaultSmallLayer = LayerMask.NameToLayer("Default_small");
      public static int ItemLayer = LayerMask.NameToLayer("item"); // should be 12
      public static int HitboxLayer = LayerMask.NameToLayer("hitbox"); // should be 12

      public static LayerMask CustomRaftLayerMask =
        LayerMask.GetMask(LayerMask.LayerToName(CustomRaftLayer));

      public static LayerMask PieceLayerMask = LayerMask.GetMask("piece");
      public static LayerMask PieceAndCustomVehicleMask = LayerMask.GetMask("piece", LayerMask.LayerToName(CustomRaftLayer));
      public static LayerMask PhysicalLayerMask = LayerMask.GetMask("Default",
        "character", "piece",
        "terrain",
        "static_solid", "Default_small",
        "character_net", "vehicle",
        LayerMask.LayerToName(CustomRaftLayer));

      public static LayerMask OnboardLayers = LayerMask.GetMask("item", "character");

      public static string SmokeLayerString = LayerMask.LayerToName(31);

      public static LayerMask BlockingColliderExcludeLayers = LayerMask.GetMask(
        "character", "character_net", "character_trigger", "viewbox",
        "character_nonenv",
        LayerMask.LayerToName(CustomRaftLayer), SmokeLayerString);

      public static LayerMask CannonHitLayers = LayerMask.GetMask("character", "character_net", "character_ghost", "character_noenv", "Default", "Default_small", "hitbox", "piece", "static_solid", "terrain", "vehicle");
      public static LayerMask CannonBlockingSiteHitLayers = LayerMask.GetMask("Default", "Default_small", "piece", "terrain", "character", "character_net", "character_noenv");

      public static List<string> ActiveLayersForBlockingMask = new();

      public static int PieceNonSolidLayer =
        LayerMask.NameToLayer("piece_nonsolid");

      public static int IgnoreRaycastLayer =
        LayerMask.NameToLayer("Ignore Raycast");

      public static LayerMask RamColliderExcludeLayers = LayerMask.GetMask(
        "character_trigger", "viewbox", "character_nonenv", "UI",
        "effect", "ghost", "piece_nonsolid", "Water", "WaterVolume", "skybox",
        "hitbox",
        "character_ghost");

      public static int CharacterLayer = LayerMask.NameToLayer("character");
      public static int CharacterTriggerLayer = LayerMask.NameToLayer("character_trigger");
      public static int CharacterLayerMask = LayerMask.GetMask("character", "character_net", "character_nonenv");

      public static bool IsItemLayer(int layer)
      {
        return layer == ItemLayer;
      }

      public static bool IsContainedWithinLayerMask(int layer, LayerMask mask)
      {
        return (mask.value & 1 << layer) != 0;
      }

      // Returns a predicate that checks if a collider's GameObject is in the LayerMask
      public static Func<Collider, bool> IsContainedWithinLayerMaskPredicate(LayerMask mask)
      {
        return c =>
        {
          return c != null && (1 << c.gameObject.layer & mask.value) != 0;
        };
      }


      [UsedImplicitly]
      public static List<int> GetActiveLayers(LayerMask mask)
      {
        ActiveLayersForBlockingMask.Clear();
        var activeLayers = new List<int>();

        // Iterate through all 32 possible layers
        for (var i = 0; i < 32; i++)
          // Check if the i-th bit in the mask is set
          if ((mask.value & 1 << i) != 0)
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

      [UsedImplicitly]
      public static void PrintAllLayers()
      {
        LoggerProvider.LogDebug("Listing All Runtime game layers");
        for (var i = 0; i < 32; i++)
        {
          var layerName = LayerMask.LayerToName(i);
          if (!string.IsNullOrEmpty(layerName))
          {
            LoggerProvider.LogDebugDebounced($"Layer {i}: {layerName}");
          }
        }
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
        return originalMask | 1 << layerToAdd;
      }
    }
  }