using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Storage.Serialization;
using ValheimVehicles.ValheimVehicles.Components;
using Zolantris.Shared;

namespace ValheimVehicles.BepInExConfig;

public class PrefabConfig : BepInExBaseConfig<PrefabConfig>
{
  public static ConfigEntry<VehicleShipInitPiece>? StartingPiece
  {
    get;
    private set;
  }

  public static ConfigEntry<bool> ProtectVehiclePiecesOnErrorFromWearNTearDamage
  {
    get;
    set;
  }

  public static ConfigEntry<bool> SortModePieceMenuItemsByMaterial = null!;

  public static ConfigEntry<int> VehicleLandMaxTreadWidth = null!;
  public static ConfigEntry<int> VehicleLandMaxTreadLength = null!;
  public static ConfigEntry<float> VehicleDockVerticalHeight = null!;
  public static ConfigEntry<float> VehicleDockSphericalRadius = null!;
  public static ConfigEntry<float> VehicleDockPositionChangeSpeed = null!;

  public static ConfigEntry<bool> MakeAllPiecesWaterProof { get; set; }
  public static ConfigEntry<bool> AllowTieredMastToRotate { get; set; }
  public static ConfigEntry<bool> AllowExperimentalPrefabs { get; set; }
  public static ConfigEntry<bool> AdminsCanOnlyBuildRaft { get; set; }
  public static ConfigEntry<Color> GlassDefaultColor { get; private set; } =
    null!;

  public static ConfigEntry<int> RopeLadderRunMultiplier { get; private set; } =
    null!;

  public static ConfigEntry<bool> RopeLadderHints { get; private set; } = null!;

  public static ConfigEntry<Vector3>
    RopeLadderEjectionOffset { get; private set; } = null!;

  public static ConfigEntry<bool> EnableLandVehicles { get; private set; } =
    null!;

  public static ConfigEntry<float> VehicleStaminaHaulingCost = null!;
  public static ConfigEntry<bool> VehicleHaulingSnapsOnStaminaZero = null!;
  public static ConfigEntry<bool> Graphics_AllowSailsFadeInFog { get; set; } = null!;


  public enum VehicleShipInitPiece
  {
    Hull4X8,
    HullFloor2X2,
    HullFloor4X4,
    WoodFloor2X2,
    Nautilus
  }

  public static ConfigEntry<float> ExperimentalTreadScaleX = null!;

  internal const string PrefabConfigKey = "PrefabConfig";

  public static void UpdatePrefabEnabled(string prefabName, bool enabled)
  {
    if (PieceManager.Instance == null) return;
    var prefab = PieceManager.Instance.GetPiece(prefabName);
    if (prefab == null) return;
    prefab.Piece.m_enabled = enabled;
  }

  /// <summary>
  /// Handles user-configurable hammer category ordering via a single CSV string.
  /// </summary>
  public static class VehicleHammerOrder
  {
    // Canonical default order (frozen snapshot) to preserve default append behavior.
    public static readonly string[] DefaultOrder =
      VehicleHammerTableCategories.AllVehicleHammerCategoriesFallbackNames.ToArray();

    /// <summary>The effective, validated order after parsing user CSV.</summary>
    public static IReadOnlyList<string> EffectiveOrder => _effectiveOrder;

    /// <summary>Raised whenever the effective order changes (initial bind or user edit).</summary>
    public static event Action<IReadOnlyList<string>> OnOrderChanged;

    /// <summary>CSV config entry (string only for maximum compatibility with BepInEx).</summary>
    public static ConfigEntry<string> VehicleHammerCategoryOrderCsv { get; private set; }

    /// <summary>An IComparer you can feed into LINQ or sort APIs.</summary>
    public static IComparer<string> CategoryComparer { get; } = new CategoryIndexComparer();

    private static List<string> _effectiveOrder = new(DefaultOrder);

    /// <summary>Bind the CSV config and wire SettingChanged. Must be called during config bind.</summary>
    public static void Bind(ConfigFile config, string sectionName, string keyName = "VehicleHammerCategoryOrder")
    {
      var defaultCsv = string.Join(",", DefaultOrder);
      VehicleHammerCategoryOrderCsv = config.Bind(
        sectionName,
        keyName,
        defaultCsv,
        "Custom order for the Valheim Vehicles hammer categories. " +
        "Comma/semicolon/pipe separated. Unknown values are ignored; missing categories are appended."
      );

      // Compute once at bind.
      RecomputeEffectiveOrder(writeBackNormalizedCsv: true);

      // React to user edits hot.
      VehicleHammerCategoryOrderCsv.SettingChanged += (_, __) =>
      {
        RecomputeEffectiveOrder(writeBackNormalizedCsv: false);
      };
    }

    /// <summary>Return a stable index for a category (unknown -> large index so they sort last).</summary>
    public static int GetIndex(string category)
    {
      var idx = _effectiveOrder.IndexOf(category);
      return idx < 0 ? int.MaxValue : idx;
    }

    /// <summary>Sorts a list of categories in-place per the effective order.</summary>
    public static void SortInPlace(List<string> categories)
    {
      categories.Sort(CategoryComparer);
    }

    /// <summary>Produces a normalized CSV string of the current effective order.</summary>
    public static string GetNormalizedCsv()
    {
      return string.Join(",", _effectiveOrder);
    }

    /// <summary>Parse, validate, dedupe, append-missing, and publish.</summary>
    private static void RecomputeEffectiveOrder(bool writeBackNormalizedCsv)
    {
      var parsed = ParseCsv(VehicleHammerCategoryOrderCsv.Value);

      // Keep only known categories, preserving user order, removing duplicates
      var knownSet = new HashSet<string>();
      var cleaned = new List<string>(parsed.Count);
      foreach (var c in parsed)
      {
        if (!VehicleHammerTableCategories.IsHammerTableCategory(c)) continue;
        if (knownSet.Add(c)) cleaned.Add(c);
      }

      // Append any missing ones in default order
      foreach (var c in DefaultOrder)
      {
        if (!knownSet.Contains(c)) cleaned.Add(c);
      }

      // Publish if changed
      var changed = !SequenceEqual(_effectiveOrder, cleaned);
      if (changed)
      {
        _effectiveOrder = cleaned;
        OnOrderChanged?.Invoke(_effectiveOrder);
      }

      // Optionally normalize the CSV in the config file to keep it tidy
      if (writeBackNormalizedCsv)
      {
        var normalized = string.Join(",", _effectiveOrder);
        if (!string.Equals(VehicleHammerCategoryOrderCsv.Value, normalized, StringComparison.Ordinal))
        {
          VehicleHammerCategoryOrderCsv.Value = normalized;
        }
      }
    }

    private static List<string> ParseCsv(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return new List<string>(DefaultOrder);

      // Accept commas, semicolons, pipes as separators
      var parts = raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
      var list = new List<string>(parts.Length);
      foreach (var p in parts)
      {
        var s = p.Trim();
        if (s.Length > 0) list.Add(s);
      }
      return list;
    }

    private static bool SequenceEqual(List<string> a, List<string> b)
    {
      if (a.Count != b.Count) return false;
      for (var i = 0; i < a.Count; i++)
        if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
          return false;
      return true;
    }

    private sealed class CategoryIndexComparer : IComparer<string>
    {
      public int Compare(string x, string y)
      {
        // Unknowns sort last but maintain relative order by name
        var ix = GetIndex(x);
        var iy = GetIndex(y);
        if (ix == iy) return string.CompareOrdinal(x, y);
        return ix.CompareTo(iy);
      }
    }
  }

  /// <summary>
  /// Expose the current normalized order list to consumers (UI builders, sorters, etc.).
  /// </summary>
  public static IReadOnlyList<string> GetVehicleHammerCategoryOrder()
  {
    return VehicleHammerOrder.EffectiveOrder;
  }


  public override void OnBindConfig(ConfigFile config)
  {
    SortModePieceMenuItemsByMaterial = config.BindUnique(PrefabConfigKey, "SortModePieceMenuItemsByMaterial", true, "Sorts by material instead of type. This means all wood pieces come before iron pieces. The other approach is each prefab type is cluster together alongside other material variants in same type.");


    VehicleHammerOrder.Bind(config, PrefabConfigKey, "VehicleHammerCategoryOrder");



    AllowTieredMastToRotate = config.BindUnique(PrefabConfigKey, "AllowTieredMastToRotateInWind", true, "allows the tiered mast to rotate in wind");
    RopeLadderEjectionOffset = config.BindUnique(PrefabConfigKey,
      "RopeLadderEjectionPoint", Vector3.zero,
      ConfigHelpers.CreateConfigDescription("The place the player is placed after they leave the ladder. Defaults to Y +0.25 and Z +0.5 meaning you are placed forwards of the ladder."));


    StartingPiece = config.BindUnique(PrefabConfigKey, "Vehicle Hull Starting Piece",
      VehicleShipInitPiece.Hull4X8,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize what piece the raft initializes with. Admins only as this can be overpowered.",
        true, true));
    RopeLadderRunMultiplier = config.BindUnique(PrefabConfigKey,
      "ropeLadderRunClimbSpeedMult", 2,
      ConfigHelpers.CreateConfigDescription(
        "Allows you to customize how fast you can climb a ladder when in run mode",
        false, true, new AcceptableValueRange<int>(1, 10)));
    RopeLadderHints = config.BindUnique(PrefabConfigKey, "ropeLadderHints", true,
      ConfigHelpers.CreateConfigDescription(
        "Shows the controls required to auto ascend/descend and run to speedup ladder"));

    ProtectVehiclePiecesOnErrorFromWearNTearDamage = config.BindUnique(
      PrefabConfigKey,
      "Protect Vehicle pieces from breaking on Error", true,
      ConfigHelpers.CreateConfigDescription(
        "Protects against crashes breaking raft/vehicle initialization causing raft/vehicles to slowly break pieces attached to it. This will make pieces attached to valid raft ZDOs unbreakable from damage, but still breakable with hammer",
        true, true));

    GlassDefaultColor = config.BindUnique(PrefabConfigKey,
      "GlassDefaultColor",
      new Color(0.60f, 0.60f, 0.60f, 0.05f),
      ConfigHelpers.CreateConfigDescription(
        "Set the experimental glass color for your vehicle. This will be used for most glass meshes. This is the default color. Eventually players can customize the color of the glass.",
        true, true));

    EnableLandVehicles = config.BindUnique(PrefabConfigKey, "enableLandVehicles", false,
      ConfigHelpers.CreateConfigDescription(
        "Vehicles land vehicle prefab will be enabled. LandVehicles will be available for all version above V3.0.0",
        true));

    VehicleStaminaHaulingCost = config.BindUnique(PrefabConfigKey,
      "VehicleStaminaHaulingCost",
      5f,
      ConfigHelpers.CreateConfigDescription(
        "The cost per 1 meter of hauling a vehicle. This cost is on incurred if the vehicle is being pulled towards the player. When stamina runs out, the player is damaged by this amount until they release the vehicle.", true, false, new AcceptableValueRange<float>(0, 10f)));
    VehicleHaulingSnapsOnStaminaZero = config.BindUnique(PrefabConfigKey,
      "VehicleHaulingSnapsOnStaminaZero", false,
      ConfigHelpers.CreateConfigDescription(
        "Instead of allowing the viking to use health. The vehicle hauling line will snap when you have zero stamina doing a single one-time damage.", true, false));

    ExperimentalTreadScaleX = config.BindUnique(PrefabConfigKey,
      "Experimental_TreadScaleX", 1f,
      ConfigHelpers.CreateConfigDescription(
        "Set the tank per tread piece X scale (width). This will make the treads larger or smaller allowing more/less grip.", true, false, new AcceptableValueRange<float>(0.5f, 5f)));

    AdminsCanOnlyBuildRaft = config.BindUnique("Server config",
      "AdminsCanOnlyBuildRaft", false,
      ConfigHelpers.CreateConfigDescription(
        "ValheimRAFT hammer menu pieces are registered as disabled unless the user is an Admin, allowing only admins to create rafts. This will update automatically make sure to un-equip the hammer to see it apply (if your remove yourself as admin). Server / client does not need to restart",
        true, true));

    AllowExperimentalPrefabs = config.BindUnique(PrefabConfigKey,
      "AllowExperimentalPrefabs", false,
      ConfigHelpers.CreateConfigDescription(
        "Allows >=v2.0.0 experimental prefabs such as Iron variants of slabs, hulls, and ribs. They do not look great so they are disabled by default",
        true, false));



    Graphics_AllowSailsFadeInFog = config.BindUnique("Graphics", "Sails Fade In Fog",
      true,
      "Allow sails to fade in fog. Unchecking this will be slightly better FPS but less realistic. Should be fine to keep enabled");


    MakeAllPiecesWaterProof = config.BindUnique<bool>("Server config",
      "MakeAllPiecesWaterProof", true, ConfigHelpers.CreateConfigDescription(
        "Makes it so all building pieces (walls, floors, etc) on the ship don't take rain damage.",
        true
      ));

    const string customSettingMessage = "This is just a default. Any vehicle can be configured directly via config menu.";


    VehicleLandMaxTreadWidth = config.BindUnique(PrefabConfigKey,
      "LandVehicle Max Tread Width",
      8,
      ConfigHelpers.CreateConfigDescription(
        $"Max width the treads can expand to. Lower values will let you make motor bikes. This affects all vehicles. {customSettingMessage}", true, false, new AcceptableValueRange<int>(1, 20)));

    VehicleLandMaxTreadLength = config.BindUnique(PrefabConfigKey,
      "LandVehicle Max Tread Length",
      20,
      ConfigHelpers.CreateConfigDescription(
        $"Max length the treads can expand to. {customSettingMessage}", true, false, new AcceptableValueRange<int>(4, 100)));

    VehicleDockPositionChangeSpeed = config.BindUnique(PrefabConfigKey,
      "VehicleDockPositionChangeSpeed",
      1f,
      ConfigHelpers.CreateConfigDescription(
        $"Dock position change speed. Higher values will make the vehicle move faster but could cause physics problems.", true, false, new AcceptableValueRange<float>(0.001f, 100f)));

    VehicleDockVerticalHeight = config.BindUnique(PrefabConfigKey,
      "VehicleDockVerticalHeight",
      200f,
      ConfigHelpers.CreateConfigDescription(
        $"MaxTowing height where a landvehicle can be grabbed/towed by a ship or flying ship. This is cast from the vehicle's upper most bounds and continues directly upwards without any rotation.", true, false, new AcceptableValueRange<float>(50f, 2000f)));
    VehicleDockSphericalRadius = config.BindUnique(PrefabConfigKey,
      "VehicleDockSphericalRadius",
      20f,
      ConfigHelpers.CreateConfigDescription(
        $"MaxTowing radius where a landvehicle can be grabbed/towed by a ship or flying ship. Spheres are significantly less accurate so a higher value could result in accidental matches with wrong vehicle", true, false, new AcceptableValueRange<float>(1f, 50f)));


    VehicleLandMaxTreadWidth.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();
    VehicleLandMaxTreadLength.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();

    AllowExperimentalPrefabs.SettingChanged +=
      ExperimentalPrefabRegistry.OnExperimentalPrefabSettingsChange;

    ExperimentalTreadScaleX.SettingChanged += (sender, args) => VehicleManager.UpdateAllLandMovementControllers();
    VehicleStaminaHaulingCost.SettingChanged += (_, __) =>
    {
      VehicleMovementController.staminaHaulCost = VehicleStaminaHaulingCost.Value;
    };
    VehicleHaulingSnapsOnStaminaZero.SettingChanged += (_, __) =>
    {
      VehicleMovementController.ShouldHaulingLineSnapOnZeroStamina = VehicleHaulingSnapsOnStaminaZero.Value;
    };
    EnableLandVehicles.SettingChanged += (_, __) => UpdatePrefabEnabled(PrefabNames.LandVehicle, EnableLandVehicles.Value);
  }
}