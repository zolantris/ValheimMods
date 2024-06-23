using System;
using BepInEx;
using Jotunn.Utils;

namespace DynamicLocations;

[BepInPlugin(BepInGuid, ModName, Version)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public class DynamicLocationsPlugin : BaseUnityPlugin
{
  public const string Author = "zolantris";
  public const string Version = "1.0.0";
  public const string ModName = "DynamicLocations";
  public const string BepInGuid = $"{Author}.{ModName}";
  public const string HarmonyGuid = $"{Author}.{ModName}";

  public const string ModDescription =
    "Valheim Mod made to attach to an item and place a player or object near the item wherever it is in the current game. Meant for ValheimVehicles but could be used for any movement mod";

  public const string CopyRight = "Copyright © 2023-2024, GNU-v3 licensed";
}