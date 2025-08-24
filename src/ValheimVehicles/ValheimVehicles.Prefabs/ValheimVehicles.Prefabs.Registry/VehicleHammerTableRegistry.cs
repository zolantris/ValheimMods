// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry
{
  public class VehicleHammerTableRegistry : RegisterPrefab<VehicleHammerTableRegistry>
  {
    public static CustomPieceTable? VehicleHammerTable { get; private set; }

    public const string VehicleHammerTableName = "ValheimVehicles_HammerTable";

    /// <summary>
    /// Canonical order from config, converted to localized labels (same index order).
    /// </summary>
    private static string[] BuildLocalizedLabelsFromCanon()
    {
      var canon = PrefabConfig.GetVehicleHammerCategoryOrder();
      // Map canonicals -> localized labels in the SAME order
      return VehicleHammerTableCategories.ToLocalizedLabels(canon).ToArray();
    }

    /// <summary>
    /// Apply BOTH the canonical category list (drives grouping/index) and the localized labels (display) in lockstep.
    /// This prevents index/label drift that causes wrong items to appear under tabs.
    /// </summary>
    public static void RefreshCategoriesAndLabels()
    {
      var table = PieceManager.Instance.GetPieceTable(VehicleHammerTableName);
      if (!table) return;

      var canonicalOrder = PrefabConfig.GetVehicleHammerCategoryOrder().ToList(); // e.g., ["Tools","Hull",...]
      var localizedLabels = BuildLocalizedLabelsFromCanon().ToList(); // e.g., ["工具","船体",...]

      // 2) Set the underlying canonical categories list (index driver)
      //    Different Valheim/JVL versions used different field names; support both.
      if (!TrySetStringListField(table, "m_customCategories", canonicalOrder))
      {
        // Older/newer fallback name
        TrySetStringListField(table, "m_categories", canonicalOrder);
      }

      // 3) Set the UI labels for those categories in the SAME index order
      table.m_categoryLabels = localizedLabels;
    }

    /// <summary>
    /// Utility: tries to set a List&lt;string&gt; field on PieceTable via reflection if present.
    /// </summary>
    private static bool TrySetStringListField(PieceTable table, string fieldName, List<string> value)
    {
      var f = typeof(PieceTable).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (f == null) return false;

      // Some game builds store a List<string>, others an array of strings. Handle both.
      if (f.FieldType == typeof(List<string>))
      {
        f.SetValue(table, value);
        return true;
      }
      if (f.FieldType == typeof(string[]))
      {
        f.SetValue(table, value.ToArray());
        return true;
      }
      return false;
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

      // Keep labels & category indexes in sync on language or order changes
      Localization.OnLanguageChange += RefreshCategoriesAndLabels;
      PrefabConfig.VehicleHammerOrder.OnOrderChanged += _ => RefreshCategoriesAndLabels();

      var success = PieceManager.Instance.AddPieceTable(VehicleHammerTable);

      // Apply localized labels now (after the table exists)
      RefreshCategoriesAndLabels();

      if (!success)
      {
        LoggerProvider.LogError(
          "VehicleHammerTable failed to be added. Falling back to original hammer table for all items. " +
          "This is a bug and could break your game. Please report this.");
        VehicleHammerTable = null;
      }
    }

    public override void OnRegister()
    {
      RegisterVehicleHammerTable();
    }
  }
}