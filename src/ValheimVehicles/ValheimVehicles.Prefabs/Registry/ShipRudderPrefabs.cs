using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Vehicles.Components;

namespace ValheimVehicles.Prefabs;

public class ShipRudderPrefabs : IRegisterPrefab
{
  public static ShipRudderPrefabs Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    RegisterShipRudderBasic(prefabManager, pieceManager);
    RegisterShipRudderAdvanced(prefabManager, pieceManager);
  }


  // todo share most of the registry logic 
  public static void RegisterShipRudderBasic(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var rudderPrefab =
      prefabManager.CreateClonedPrefab(
        PrefabNames.ShipRudderBasic, LoadValheimVehicleAssets.ShipRudderBasicAsset);
    PrefabRegistryHelpers.AddNetViewWithPersistence(rudderPrefab);
    rudderPrefab.layer = 0;
    rudderPrefab.gameObject.layer = 0;
    var piece = rudderPrefab.AddComponent<Piece>();

    piece.m_icon = LoadValheimVehicleAssets.Sprites.GetSprite(SpriteNames.ShipRudderBasic);

    PrefabRegistryHelpers.SetWearNTear(rudderPrefab);
    PrefabRegistryHelpers.FixCollisionLayers(rudderPrefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(rudderPrefab);

    pieceManager.AddPiece(new CustomPiece(rudderPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      /*
       * @todo make the name dynamic getter from HullMaterial
       */
      Name = "$valheim_vehicles_rudder_basic",
      Description = "$valheim_vehicles_rudder_basic_desc",
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[2]
      {
        new()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        },
        new()
        {
          Amount = 2,
          Item = "RoundLog",
          Recover = true
        }
      }
    }));
  }

  /**
 * A new ship rudder that pivots 90 degrees starboard/port.
 * - Rudder controls the ship direction.
 * - @TODO Rudder may only work with v2 ships
 */
  public static void RegisterShipRudderAdvanced(PrefabManager prefabManager,
    PieceManager pieceManager)
  {
    var rudderPrefab =
      prefabManager.CreateClonedPrefab(
        PrefabNames.ShipRudderAdvanced, LoadValheimVehicleAssets.ShipRudderAdvancedAsset);
    PrefabRegistryHelpers.AddNetViewWithPersistence(rudderPrefab);
    rudderPrefab.layer = 0;
    rudderPrefab.gameObject.layer = 0;
    PrefabPieceHelper.AddPieceForPrefab(PrefabNames.ShipRudderAdvanced, rudderPrefab);


    var rudderComponent = rudderPrefab.AddComponent<RudderComponent>();
    rudderComponent.PivotPoint = rudderPrefab.transform.Find("rudder_advanced");

    PrefabRegistryHelpers.SetWearNTear(rudderPrefab);
    PrefabRegistryHelpers.FixCollisionLayers(rudderPrefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(rudderPrefab);

    pieceManager.AddPiece(new CustomPiece(rudderPrefab, false, new PieceConfig
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
        new RequirementConfig
        {
          Amount = 2,
          Item = "Copper",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 5,
          Item = "DeerHide",
          Recover = true
        }
      ]
    }));
  }
}