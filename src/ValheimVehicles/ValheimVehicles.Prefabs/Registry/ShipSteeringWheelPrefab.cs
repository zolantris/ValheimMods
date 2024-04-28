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
    var prefab =
      prefabManager.CreateClonedPrefab(PrefabNames.ShipSteeringWheel,
        LoadValheimRaftAssets.steeringWheel);

    var mbRudderPrefabPiece =
      PrefabPieceHelper.AddPieceForPrefab(PrefabNames.ShipSteeringWheel, prefab);
    mbRudderPrefabPiece.m_placeEffect = LoadValheimAssets.woodFloorPiece.m_placeEffect;

    PrefabRegistryController.AddToRaftPrefabPieces(mbRudderPrefabPiece);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    var rudderWheelComponent = prefab.AddComponent<RudderWheelComponent>();

    rudderWheelComponent.wheelTransform = prefab.transform.Find("controls/wheel");
    rudderWheelComponent.UpdateSpokes();

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);

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
}