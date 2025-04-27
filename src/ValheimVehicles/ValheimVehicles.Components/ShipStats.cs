using System.Collections.Generic;
using Jotunn;

namespace ValheimVehicles.Components;

public class ShipStats
{
  public float baseShipMass = 500f;
  public float shipMassPerWoodFloorItem = 50f;
  public float shipMassPerStoneItem = 200f;

  // floating
  public float baseShipFloatation = 500f;
  public float shipFloationPerWoodFloorItem = 100f;
  public float shipFloatationPerStoneItem = -200f;

  float cachedShipFloatation = 0f;

  public float GetShipFloatation(List<ZNetView> pieces)
  {
    if (cachedShipFloatation != 0f) return cachedShipFloatation;
    var currentFloatation = 1000f;

    var floatationPieces = new List<ZNetView>();
    foreach (var zNetView in pieces)
    {
      var prefabName = zNetView.GetPrefabName();
      Logger.LogDebug($"prefabName {prefabName}");
      switch (prefabName)
      {
        case "":
          break;
        default:
          break;
      }
    }

    cachedShipFloatation = currentFloatation;
    return currentFloatation;
  }
}