using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ValheimVehicles.Prefabs;

public abstract class PrefabPieceHelper
{
  public struct PrefabNameDesc
  {
    public string Name;
    public string Description;
    public Sprite Icon;
  }

  public static readonly Dictionary<string, PrefabNameDesc> Values = new();

  public static void Init()
  {
    Values.Add(PrefabNames.ShipSteeringWheel, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_wheel",
      Description = "valheim_vehicles_wheel_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.ShipSteeringWheel)
    });

    Values.Add(PrefabNames.ShipKeel, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_ship_keel",
      Description = "valheim_vehicles_ship_keel_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.ShipKeel)
    });

    Values.Add(PrefabNames.ShipHullPrefabName, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_ship_hull",
      Description = "valheim_vehicles_ship_hull_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.ShipHull)
    });

    Values.Add(PrefabNames.ShipRudderBasic, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_rudder_basic",
      Description = "valheim_vehicles_rudder_basic_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.ShipRudderBasic)
    });

    Values.Add(PrefabNames.ShipRudderAdvanced, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_rudder_advanced",
      Description = "valheim_vehicles_rudder_advanced_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.ShipRudderAdvanced)
    });

    Values.Add(PrefabNames.VehicleToggleSwitch, new PrefabNameDesc()
    {
      Name = "valheim_vehicles_toggle_switch",
      Description = "valheim_vehicles_toggle_switch_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite(SpriteNames.Switch)
    });
  }

  public static Piece AddPieceForPrefab(string prefabName, GameObject prefab)
  {
    var pieceInformation = Values.GetValueSafe(prefabName);

    var piece = prefab.AddComponent<Piece>();

    // dollar sign added for translation reference
    piece.m_name = $"${pieceInformation.Name}";
    piece.m_description = $"${pieceInformation.Description}";

    piece.m_icon = pieceInformation.Icon;

    return piece;
  }
}