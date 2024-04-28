using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Propulsion.Rudder;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Prefabs.Registry;

public class ShipSteeringWheelPrefab : IRegisterPrefab
{
  public static readonly ShipSteeringWheelPrefab Instance = new();

  public void Register(PrefabManager prefabManager, PieceManager pieceManager)
  {
    var mbRudderPrefab =
      prefabManager.CreateClonedPrefab(PrefabNames.ShipSteeringWheel,
        LoadValheimRaftAssets.steeringWheel);

    var mbRudderPrefabPiece = mbRudderPrefab.AddComponent<Piece>();
    mbRudderPrefabPiece.m_name = "$mb_rudder";
    mbRudderPrefabPiece.m_description = "$mb_rudder_desc";
    mbRudderPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(mbRudderPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(mbRudderPrefab);

    var rudderWheelComponent = mbRudderPrefab.AddComponent<RudderWheelComponent>();

    rudderWheelComponent.wheelTransform = mbRudderPrefab.transform.Find("controls/wheel");
    rudderWheelComponent.UpdateSpokes();

    PrefabRegistryHelpers.SetWearNTear(mbRudderPrefab);

    PrefabRegistryHelpers.FixCollisionLayers(mbRudderPrefab);
    pieceManager.AddPiece(new CustomPiece(mbRudderPrefab, false, new PieceConfig
    {
      PieceTable = "Hammer",
      Description = "$mb_rudder_desc",
      Icon = LoadValheimVehicleSharedAssets.Sprites.GetSprite("steering_wheel"),
      Category = PrefabNames.ValheimRaftMenuName,
      Enabled = true,
      Requirements = new RequirementConfig[1]
      {
        new()
        {
          Amount = 10,
          Item = "Wood",
          Recover = true
        }
      }
    }));
  }
}