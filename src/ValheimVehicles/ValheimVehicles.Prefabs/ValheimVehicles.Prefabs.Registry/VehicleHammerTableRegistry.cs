// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.BepInExConfig;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry
{
  public class VehicleHammerTableRegistry : RegisterPrefab<VehicleHammerTableRegistry>
  {
    public static CustomPieceTable? VehicleHammerTable { get; private set; }

    public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

    /// <summary>
    /// Apply BOTH the canonical category list (drives grouping/index) and the localized labels (display) in lockstep.
    /// This prevents index/label drift that causes wrong items to appear under tabs.
    /// </summary>
    public static void RefreshCategoriesAndLabels()
    {
      RefreshCategoriesAndLabels(false);
    }

    private static void RefreshCategoriesAndLabels(bool initializeCategories)
    {
      var table = PieceManager.Instance.GetPieceTable(VehicleHammerTableName);
      if (!table) return;

      // Valheim stores enum IDs here, not the canonical strings used by config.
      // Keep those IDs stable so pieces and the selected category retain their
      // meaning when only the display order or language changes.
      var categories = new List<Piece.PieceCategory>();
      var labels = new List<string>();

      foreach (var canonical in PrefabConfig.GetVehicleHammerCategoryOrder())
      {
        var category = PieceManager.Instance.GetPieceCategory(canonical);
        if (!category.HasValue || categories.Contains(category.Value)) continue;
        // Jotunn removes empty categories. A language/order refresh must not
        // resurrect those tabs; only table creation initializes the full set.
        if (!initializeCategories && !table.m_categories.Contains(category.Value)) continue;
        categories.Add(category.Value);
        labels.Add(VehicleHammerTableCategories.ToLocalizedLabel(canonical));
      }

      // Preserve any additional categories another mod added to this table,
      // including their labels. Never modify the vanilla hammer's table.
      for (var index = 0; index < table.m_categories.Count; index++)
      {
        var category = table.m_categories[index];
        if (categories.Contains(category)) continue;
        categories.Add(category);
        labels.Add(index < table.m_categoryLabels.Count
          ? table.m_categoryLabels[index]
          : category.ToString());
      }

      table.m_categories = categories;
      table.m_categoryLabels = labels;
    }

    /// <summary>
    /// Register the custom piece table using CANONICAL categories.
    /// Labels are applied right after creation and kept in sync thereafter.
    /// </summary>
    private static void RegisterVehicleHammerTable()
    {
      // IMPORTANT: Use canonical (English) IDs as the CustomCategories (index driver)
      var canonical = PrefabConfig.GetVehicleHammerCategoryOrder().ToArray();

      var vehicleHammerTableConfig = new PieceTableConfig
      {
        CanRemovePieces = true,
        UseCategories = false,
        UseCustomCategories = true,
        CustomCategories = canonical // <-- canonical keys ONLY
      };

      VehicleHammerTable = new CustomPieceTable(VehicleHammerTableName, vehicleHammerTableConfig);

      var success = PieceManager.Instance.AddPieceTable(VehicleHammerTable);

      if (!success)
      {
        LoggerProvider.LogError(
          "VehicleHammerTable failed to be added. Falling back to original hammer table for all items. " +
          "This is a bug and could break your game. Please report this.");
        VehicleHammerTable = null;
        return;
      }

      // Keep labels & category indexes in sync on language or order changes.
      Localization.OnLanguageChange += RefreshCategoriesAndLabels;
      PrefabConfig.VehicleHammerOrder.OnOrderChanged += _ => RefreshCategoriesAndLabels();

      // AddPieceTable has registered canonical names with Jotunn at this point.
      RefreshCategoriesAndLabels(true);
    }

    public override void OnRegister()
    {
      RegisterVehicleHammerTable();
    }
  }
}
