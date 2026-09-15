using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Prefabs.Registry;

namespace ValheimVehicles.Patches;

public static class BuildUi_VehicleCategories_Patch
{
  [HarmonyPatch(typeof(BuildUi), nameof(BuildUi.OpenBuildMenu))]
  [HarmonyPrefix]
  private static void InstallVehicleCategories(BuildUi __instance)
  {
    if (!Player.m_localPlayer || !IsVehicleTable(Player.m_localPlayer.GetBuildTool())) return;
    if (__instance.m_pieceLists.Any(list => list is VehicleCategoryPieceList)) return;

    // Keep the game's four menu modes and any other mod's custom list intact.
    // Only wrap the stock usage list; the wrapper delegates for every other tool.
    for (var index = 0; index < __instance.m_pieceLists.Count; index++)
    {
      var list = __instance.m_pieceLists[index];
      if (list?.GetType() != typeof(ByUsagePieceList)) continue;
      __instance.m_pieceLists[index] = new VehicleCategoryPieceList(list);
      break;
    }
  }

  internal static bool IsVehicleTable(PieceTable table)
  {
    return table && VehicleHammerTableRegistry.VehicleHammerTable != null &&
           table == VehicleHammerTableRegistry.VehicleHammerTable.PieceTable;
  }
}

/// <summary>
/// Presents existing RAFT category IDs as tags in Valheim 1.0's usage view.
/// Availability, recipes, search, favorites, and material grouping stay with the game.
/// </summary>
internal sealed class VehicleCategoryPieceList : IPieceList
{
  private readonly IPieceList _stock;
  private readonly List<Piece.PieceCategory> _categories = new();
  private readonly List<string> _labels = new();
  private bool _vehicleMode;

  public VehicleCategoryPieceList(IPieceList stock)
  {
    _stock = stock;
  }

  public string DisplayName => _stock.DisplayName;
  public bool ShowTags => _stock.ShowTags;
  public bool CanCustomizeTags => !_vehicleMode && _stock.CanCustomizeTags;
  public int TagCount => _vehicleMode ? _categories.Count : _stock.TagCount;
  public int TagSeparatorIndex => _vehicleMode ? -1 : _stock.TagSeparatorIndex;
  public string GetTagDisplayName(int index) => _vehicleMode ? _labels[index] : _stock.GetTagDisplayName(index);
  public int GetTagIdByIndex(int index) => _vehicleMode ? (int)_categories[index] : _stock.GetTagIdByIndex(index);

  public void UpdateAvailableTags(PieceTable table)
  {
    _vehicleMode = BuildUi_VehicleCategories_Patch.IsVehicleTable(table);
    _categories.Clear();
    _labels.Clear();
    if (!_vehicleMode)
    {
      _stock.UpdateAvailableTags(table);
      return;
    }

    var remaining = new HashSet<Piece.PieceCategory>(table.m_availablePieces
      .Where(piece => piece && !piece.m_repairPiece && !piece.m_removePiece && piece.m_category != Piece.PieceCategory.All)
      .Select(piece => piece.m_category));

    foreach (var canonical in PrefabConfig.GetVehicleHammerCategoryOrder())
    {
      var category = PieceManager.Instance.GetPieceCategory(canonical);
      if (!category.HasValue || !remaining.Remove(category.Value)) continue;
      AddCategory(category.Value, VehicleHammerTableCategories.ToLocalizedLabel(canonical));
    }

    // Preserve categories that another mod added to this vehicle table, using
    // their existing display order and labels when available.
    for (var index = 0; index < table.m_categories.Count; index++)
    {
      var category = table.m_categories[index];
      if (!remaining.Remove(category)) continue;
      AddCategory(category, index < table.m_categoryLabels.Count ? table.m_categoryLabels[index] : category.ToString());
    }
    foreach (var category in remaining.OrderBy(category => (int)category))
      AddCategory(category, category.ToString());
  }

  private void AddCategory(Piece.PieceCategory category, string label)
  {
    _categories.Add(category);
    _labels.Add(label);
  }

  public void GetAvailablePiecesWithTag(int tagId, PieceTable table, IList<Piece> result)
  {
    if (!BuildUi_VehicleCategories_Patch.IsVehicleTable(table))
    {
      _stock.GetAvailablePiecesWithTag(tagId, table, result);
      return;
    }

    foreach (var piece in table.m_availablePieces)
    {
      if (!piece) continue;
      if (tagId == -1 || piece.m_repairPiece || piece.m_removePiece ||
          piece.m_category == Piece.PieceCategory.All || (int)piece.m_category == tagId)
        result.Add(piece);
    }
  }
}
