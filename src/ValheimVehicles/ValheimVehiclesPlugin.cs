using System;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.VehicleUtils;

namespace ValheimVehicles;

[BepInPlugin(Guid, ModName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
public class ValheimVehiclesPlugin : MonoBehaviour
{
  public const string Author = "Zolantris";
  private const string Version = "1.0.0";
  internal const string ModName = "ValheimVehicles";
  public const string Guid = $"{Author}.{ModName}";

  public static ValheimVehiclesPlugin Instance { get; private set; }

  private void Awake()
  {
    Instance = this;

    Logger.LogDebug("ValheimVehicles called awake");
    PrefabManager.OnVanillaPrefabsAvailable += new Action(PrefabRegistry.CreateCustomPrefabs);
  }
}