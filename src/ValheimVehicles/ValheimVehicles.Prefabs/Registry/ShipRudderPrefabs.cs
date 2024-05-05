using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Prefabs;

public class ShipRudderPrefabs : IRegisterPrefab
{
  public static readonly ShipRudderPrefabs Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterShipRudderBasic(prefabManager, pieceManager);
    RegisterShipRudderAdvanced(prefabManager, pieceManager);
  }

  private static void RegisterShipRudderBasic(PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        PrefabNames.ShipRudderBasic, LoadValheimVehicleAssets.ShipRudderBasicAsset);

    SharedSetup(prefab);

    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  private static void SharedSetup(GameObject prefab)
  {
    // prefab.layer = 0;
    // prefab.gameObject.layer = 0;
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(prefab.name, prefab);
    var rudderComponent = prefab.AddComponent<RudderComponent>();
    rudderComponent.PivotPoint = prefab.transform.Find("rudder_rotation");

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
  }

  /**
 * A new ship rudder that pivots 90 degrees starboard/port.
 * - Rudder controls the ship direction.
 * - @TODO Rudder may only work with v2 ships
 */
  private static void RegisterShipRudderAdvanced(PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var prefab =
      prefabManager.CreateClonedPrefab(
        PrefabNames.ShipRudderAdvanced, LoadValheimVehicleAssets.ShipRudderAdvancedAsset);
    SharedSetup(prefab);


    pieceManager.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "FineWood",
          Recover = true
        },
      ]
    }));
  }
}