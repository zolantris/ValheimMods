using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.BepInExConfig;

public class PrefabRecipeConfig : BepInExBaseConfig<PrefabRecipeConfig>
{
  private const string SectionName = "RecipeConfig";

  public static readonly Dictionary<string, RequirementConfig[]> DefaultRequirements = new()
  {
    {
      PrefabNames.CannonballExplosive, new[]
      {
        new RequirementConfig { Item = "BlackMetal", Amount = 1, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 1, Recover = true }
      }
    },
    {
      PrefabNames.CannonballSolid, new[]
      {
        new RequirementConfig { Item = "Bronze", Amount = 1, Recover = true }
      }
    },
    {
      PrefabNames.CannonFixedTier1, [
        new RequirementConfig
        {
          Amount = 4,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 6,
          Item = "Wood",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.CannonTurretTier1, [
        new RequirementConfig
        {
          Amount = 4,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Chain",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "Iron",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.CannonHandHeldItem, [
        new RequirementConfig
        {
          Amount = 4,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Chain",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2,
          Item = "Iron",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.PowderBarrel, [
        new RequirementConfig { Item = "Wood", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 20, Recover = true }
      ]
    }
  };

  // Map: prefabName => config entry
  public static Dictionary<string, ConfigEntry<string>> RecipeRequirementConfigs { get; } = new();

  public override void OnBindConfig(ConfigFile config)
  {
    var isFirst = true;
    foreach (var kvp in DefaultRequirements)
    {
      var prefabName = kvp.Key;
      var defaultArray = kvp.Value;
      var defaultString = ToConfigString(defaultArray);

      var description = isFirst
        ? $"Recipe requirements for {prefabName}.\n" +
          "Format: ItemName,Amount[,Recover][,AmountPerLevel]|... (e.g., BlackPowder,2,true|Bronze,1,true)\n" +
          "Recover is optional (defaults true). AmountPerLevel is optional (defaults 0)."
        : $"Recipe requirements for {prefabName}.";

      RecipeRequirementConfigs[prefabName] = config.Bind(
        SectionName,
        prefabName,
        defaultString,
        description
      );
      isFirst = false;
    }
  }

  public static RequirementConfig[] GetRequirements(string prefabName)
  {
    if (RecipeRequirementConfigs.TryGetValue(prefabName, out var entry))
    {
      var val = entry.Value?.Trim();
      if (!string.IsNullOrEmpty(val))
      {
        var parsed = ParseRequirements(val);
        if (parsed.Length > 0)
          return parsed;
      }
    }
    // Fallback to in-memory default array
    return DefaultRequirements.TryGetValue(prefabName, out var def) ? def : Array.Empty<RequirementConfig>();
  }

  // Parser supporting: Item,Amount[,Recover][,AmountPerLevel]
  public static RequirementConfig[] ParseRequirements(string configValue)
  {
    if (string.IsNullOrEmpty(configValue))
      return Array.Empty<RequirementConfig>();

    return configValue.Split('|')
      .Select(req =>
      {
        var parts = req.Split(',');
        if (parts.Length < 2)
        {
          UnityEngine.Debug.LogWarning($"Invalid requirement entry: \"{req}\". Format should be Item,Amount[,Recover][,AmountPerLevel]");
          return null;
        }
        return new RequirementConfig
        {
          Item = parts[0].Trim(),
          Amount = int.TryParse(parts[1], out var amt) ? amt : 1,
          Recover = parts.Length > 2 && bool.TryParse(parts[2], out var recover) ? recover : true,
          AmountPerLevel = parts.Length > 3 && int.TryParse(parts[3], out var perLvl) ? perLvl : 0
        };
      })
      .OfType<RequirementConfig>()
      .ToArray();
  }

  // Serializes RequirementConfig[] to a config string
  private static string ToConfigString(RequirementConfig[] reqs)
  {
    return string.Join("|", reqs.Select(r =>
      $"{r.Item},{r.Amount},{r.Recover.ToString().ToLower()}{(r.AmountPerLevel > 0 ? $",{r.AmountPerLevel}" : "")}"));
  }
}