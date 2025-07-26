using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.SharedScripts.Modules;

namespace ValheimVehicles.BepInExConfig;

public class PrefabRecipeConfig : BepInExBaseConfig<PrefabRecipeConfig>
{
  private const string BaseSectionName = "RecipeConfig";
  private const string SectionNameHullMaterial = "RecipeConfig: HullMaterial";
  public static ConfigEntry<float> HullMaterialIronRatio = null!;
  public static ConfigEntry<float> HullMaterialBronzeRatio = null!;
  public static ConfigEntry<float> HullMaterialWoodRatio = null!;
  public static ConfigEntry<float> HullMaterialYggdrasilWoodRatio = null!;
  public static ConfigEntry<float> HullMaterialNailsRatio = null!;

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
      PrefabNames.CannonControlCenter, [
        new RequirementConfig
        {
          Amount = 2,
          Item = "Bronze",
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
          Recover = true,
          AmountPerLevel = 1
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Chain",
          Recover = true
        }
      ]
    },
    {
      PrefabNames.PowderBarrel, [
        new RequirementConfig { Item = "Wood", Amount = 4, Recover = true },
        new RequirementConfig { Item = "Coal", Amount = 20, Recover = true }
      ]
    },
    // hull materials
    {
      GetHullMaterialRecipe(HullMaterial.Iron), [
        new RequirementConfig
        {
          Amount = 1,
          Item = "Iron",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 1,
          Item = "Bronze",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 2, Item = "BronzeNails", Recover = true
        },
        new RequirementConfig
          { Amount = 1, Item = "YggdrasilWood", Recover = true }
      ]
    },
    {
      GetHullMaterialRecipe(HullMaterial.Wood), [
        new RequirementConfig
          { Amount = 2, Item = "Wood", Recover = true }
      ]
    }
    // end <hull-materials>
  };

  public static string GetHullMaterialRecipe(string hullMaterialVariant)
  {
    return $"hull_base_recipe_{hullMaterialVariant}";
  }

  public static int GetHullMaterialAmountByItem(string ItemId, int materialCount)
  {
    return ItemId switch
    {
      "Iron" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialIronRatio.Value, 0, 100)),
      "Bronze" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialBronzeRatio.Value, 0, 100)),
      "YggdrasilWood" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialYggdrasilWoodRatio.Value, 0, 100)),
      "BronzeNails" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialNailsRatio.Value, 0, 100)),
      "Wood" => Mathf.RoundToInt(Mathf.Clamp(materialCount * HullMaterialWoodRatio.Value, 0, 100)),
      _ => 1
    };
  }

  public static RequirementConfig[] GetHullMaterialRecipeConfig(string hullMaterialVariant, int materialCount)
  {
    var materialRecipeName = GetHullMaterialRecipe(hullMaterialVariant);
    var baseRequirements = GetRequirements(materialRecipeName);
    if (baseRequirements.Length == 0)
    {
      LoggerProvider.LogError($"No base requirements set for hull material {materialRecipeName} of variant <{hullMaterialVariant}>");
      return baseRequirements;
    }

    return baseRequirements.Select(r => new RequirementConfig
    {
      Item = r.Item,
      Amount = GetHullMaterialAmountByItem(r.Item, materialCount),
      Recover = r.Recover,
      AmountPerLevel = r.AmountPerLevel
    }).ToArray();
  }

  // Map: prefabName => config entry
  public static Dictionary<string, ConfigEntry<string>> RecipeRequirementConfigs { get; } = new();
  private const string ratioDescription = "For configuring hull size ratio. EG materialValue 2x2=4 but ratio 1/4 would get 1 of <itemName>. (rounds to lowets 0 or nearest int). This is meant for the default recipe. Customize the base recipe if you want to override things.";
  private const string ratioDescriptionShort = "For configuring hull size ratio";

  public void AddHullRecipeModifierConfig(ConfigFile config)
  {

    HullMaterialIronRatio = config.Bind(
      SectionNameHullMaterial,
      "IronRatio",
      0.25f,
      ratioDescription
    );
    HullMaterialBronzeRatio = config.Bind(
      SectionNameHullMaterial,
      "BronzeRatio",
      0.25f,
      ratioDescriptionShort
    );
    HullMaterialWoodRatio = config.Bind(
      SectionNameHullMaterial,
      "WoodRatio",
      2f,
      ratioDescriptionShort
    );
    HullMaterialYggdrasilWoodRatio = config.Bind(
      SectionNameHullMaterial,
      "YggdrasilWoodRatio",
      1f,
      ratioDescriptionShort
    );

    HullMaterialNailsRatio = config.Bind(
      SectionNameHullMaterial,
      "NailsRatio",
      1f,
      ratioDescriptionShort
    );
  }

  public override void OnBindConfig(ConfigFile config)
  {
    AddHullRecipeModifierConfig(config);

    var isFirst = true;
    foreach (var kvp in DefaultRequirements)
    {
      var prefabName = kvp.Key;
      var defaultArray = kvp.Value;
      var defaultString = ToConfigString(defaultArray);

      var description = isFirst
        ? $"Recipe requirements for {prefabName}.\n" +
          "Format: ItemName,Amount[,Recover][,AmountPerLevel]|... (e.g., BlackPowder,2,true|Bronze,1,true)\n" +
          "Recover is optional (defaults true). AmountPerLevel is optional (defaults 0). Amount is clamped between 0 and 100. No decimals are allowed."
        : $"Recipe requirements for {prefabName}.";

      RecipeRequirementConfigs[prefabName] = config.Bind(
        BaseSectionName,
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
    return DefaultRequirements.TryGetValue(prefabName, out var def) ? def : [];
  }

  // Parser supporting: Item,Amount[,Recover][,AmountPerLevel]
  public static RequirementConfig?[] ParseRequirements(string configValue)
  {
    if (string.IsNullOrEmpty(configValue))
      return [];

    return configValue.Split('|')
      .Where(req =>
      {
        var parts = req.Split(',');
        if (parts.Length < 2)
        {
          LoggerProvider.LogWarning($"Invalid requirement entry: \"{req}\". Format should be Item,Amount[,Recover][,AmountPerLevel]");
          return false;
        }
        return true;
      })
      .Select(req =>
      {
        var parts = req.Split(',');
        return new RequirementConfig
        {
          Item = parts[0].Trim(),
          Amount = MathX.Clamp(int.TryParse(parts[1], out var amt) ? amt : 1, 0, 100),
          Recover = parts.Length <= 2 || !bool.TryParse(parts[2], out var recover) || recover,
          AmountPerLevel = parts.Length > 3 && int.TryParse(parts[3], out var perLvl) ? perLvl : 0
        };
      })
      .ToArray();
  }

  // Serializes RequirementConfig[] to a config string
  private static string ToConfigString(RequirementConfig[] reqs)
  {
    return string.Join("|", reqs.Select(r =>
      $"{r.Item},{r.Amount},{r.Recover.ToString().ToLower()}{(r.AmountPerLevel > 0 ? $",{r.AmountPerLevel}" : "")}"));
  }
}