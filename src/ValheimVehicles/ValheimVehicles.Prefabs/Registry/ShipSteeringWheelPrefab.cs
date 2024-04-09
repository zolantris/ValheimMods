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

    // for older components
    // m_controls and valheimShipControls are dynamically enabled/disabled
    // rudder.m_controls = mbRudderPrefab.AddComponent<ShipControlls>();
    // rudder.m_controls.m_hoverText = "$mb_rudder_use";
    // rudder.m_controls.m_attachPoint = mbRudderPrefab.transform.Find("attachpoint");
    // rudder.m_controls.m_attachAnimation = "Standing Torch Idle right";
    // rudder.m_controls.m_detachOffset = new Vector3(0f, 0f, 0f);

    // for newer vehicle components
    // rudder.valheimShipControls = mbRudderPrefab.AddComponent<ValheimShipControls>();
    // rudder.valheimShipControls.m_hoverText = "$mb_rudder_use";
    // rudder.valheimShipControls.m_attachPoint = mbRudderPrefab.transform.Find("attachpoint");
    // rudder.valheimShipControls.m_attachAnimation = "Standing Torch Idle right";
    // rudder.valheimShipControls.m_detachOffset = new Vector3(0f, 0f, 0f);

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