using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace YggdrasilTerrain.Config;

public static class YggdrasilConfig
{
  public static ConfigFile? Config { get; private set; }

  public static ConfigEntry<bool> AllowCollisionsOnYggdrasilBranch
  {
    get;
    private set;
  } = null!;

  public static ConfigEntry<int>
    BranchCollisionLayerMask { get; private set; } = null!;

  public static ConfigEntry<int> BranchLayer { get; private set; } = null!;
  public static ConfigEntry<bool> Debug { get; private set; } = null!;

  public static ConfigEntry<bool>
    OverridesSectionEnabled { get; private set; } =
    null!;


  public static List<string> collisionLayerNames =
  [
    "Default",
    "character",
    "piece", "terrain",
    "static_solid", "Default_small", "character_net", "vehicle"
  ];

  private static readonly int DefaultCollisionMask =
    LayerMask.GetMask(collisionLayerNames.ToArray());

  private const string DefaultCollisionLayer = "piece";

  private const string ConfigSection = "Main";
  private const string LayersSection = "Layers";

  private const string LayerOverridesName = "LayerOverridesEnabled";

  private const string LayersSectionSharedDescription =
    $"Will not apply an override unless {LayersSection}.${LayerOverridesName} = true";

  public static void BindConfig(ConfigFile config)
  {
    Config = config;

    var defaultCollisionLayerInt = LayerMask.NameToLayer(DefaultCollisionLayer);

    if (defaultCollisionLayerInt < 0)
    {
      Logger.LogError(
        $"The default collision layer {DefaultCollisionLayer} call of LayerMask.NameToLayer failed to return a valid result, due to this issue it will default the layer to 0. Please consider overriding the layer or contacting the mod owner to fix this. This error should not happen and likely something has destructively re-named a fundamental layer value.");
    }

    AllowCollisionsOnYggdrasilBranch = config.BindUnique(ConfigSection,
      "AllowCollisionsOnYggdrasilBranch", true,
      ConfigHelpers.CreateConfigDescription(
        "Allows collisions on the Yggdrasil branch. Toggling will add/remove this behavior.",
        true, false));

    Debug = config.BindUnique(ConfigSection,
      "Debug", false,
      ConfigHelpers.CreateConfigDescription(
        "Enables debug logging and features, useful for debugging the mod, does NOT allow exploits.",
        true, true));

    OverridesSectionEnabled = config.BindUnique(LayersSection,
      LayerOverridesName, false,
      ConfigHelpers.CreateConfigDescription(
        "Enables allows overrides for collisions and other values for mod compatibility. Do not enable this unless you need to otherwise values set within overrides will require a reset of the config.",
        true, true));


    BranchCollisionLayerMask = config.BindUnique(LayersSection,
      "BranchCollisionLayerMask", DefaultCollisionMask,
      ConfigHelpers.CreateConfigDescription(
        $"Controls what game layers can be set as collisions. {LayersSectionSharedDescription}",
        true, true));
    BranchLayer = config.BindUnique(LayersSection,
      "BranchLayer", LayerMask.NameToLayer(DefaultCollisionLayer),
      new ConfigDescription(
        $"Controls what layer the branch is on. This will affect building, do not change this unless you know what you are doing. Default should be {DefaultCollisionLayer}. {LayersSectionSharedDescription}",
        new AcceptableValueRange<int>(0, 31),
        new ConfigurationManagerAttributes
          { IsAdminOnly = true, IsAdvanced = true }));

    if (!OverridesSectionEnabled.Value)
    {
      BranchCollisionLayerMask.Value = DefaultCollisionMask;

      BranchLayer.Value =
        defaultCollisionLayerInt >= 0 ? defaultCollisionLayerInt : 10;
    }

    AllowCollisionsOnYggdrasilBranch.SettingChanged += YggdrasilBranch
      .OnBranchCollisionChange;

    if (Debug.Value)
    {
      Logger.LogDebug(
        $"collisionLayer defaultLayerNames: {collisionLayerNames} CollisionMask: ${DefaultCollisionMask}");
      Logger.LogDebug(
        $"BranchLayer NameToLayer {BranchLayer.Value}, defaultLayerName: {DefaultCollisionLayer} defaultLayerValue: {LayerMask.NameToLayer(DefaultCollisionLayer)}");
    }
  }
}