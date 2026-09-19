using System.Collections.Generic;
using UnityEngine;

namespace ValheimScalability.Rendering.Clustering;

/// <summary>
/// Creates isolated material copies with GPU instancing enabled.
/// Source Valheim/mod materials are never modified.
/// </summary>
public static class InstancedMaterialCache
{
  private static readonly Dictionary<int, Material> Cache = new();
  private static readonly HashSet<int> Unsupported = new();

  public static Material GetOrCreate(Material source)
  {
    if (!source ||
        !SystemInfo.supportsInstancing)
    {
      return null;
    }

    var id = source.GetInstanceID();

    if (Unsupported.Contains(id))
    {
      return null;
    }

    if (Cache.TryGetValue(id, out var cached))
    {
      return cached;
    }

    Material clone = null;

    try
    {
      clone = new Material(source)
      {
        name = source.name + "_ValheimScalability_Instanced",
        hideFlags = HideFlags.DontSave
      };

      clone.enableInstancing = true;

      if (!clone.enableInstancing)
      {
        Object.Destroy(clone);
        Unsupported.Add(id);
        return null;
      }

      Cache[id] = clone;
      return clone;
    }
    catch
    {
      if (clone)
      {
        Object.Destroy(clone);
      }

      Unsupported.Add(id);
      return null;
    }
  }

  public static void MarkUnsupported(Material source)
  {
    if (!source)
    {
      return;
    }

    var id = source.GetInstanceID();
    Unsupported.Add(id);

    if (Cache.TryGetValue(id, out var cached))
    {
      if (cached)
      {
        Object.Destroy(cached);
      }

      Cache.Remove(id);
    }
  }

  public static void Clear()
  {
    foreach (var material in Cache.Values)
    {
      if (material)
      {
        Object.Destroy(material);
      }
    }

    Cache.Clear();
    Unsupported.Clear();
  }
}
