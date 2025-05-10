using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.Integrations;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs;

public class SwivelPrefab : RegisterPrefab<SwivelPrefab>
{
  private void RegisterSwivelPrefabPieceData()
  {
    PrefabRegistryHelpers.PieceDataDictionary.Add(PrefabNames.SwivelPrefabName, new PrefabRegistryHelpers.PieceData
    {
      Name = "$valheim_vehicles_mechanism_swivel",
      Description = "$valheim_vehicles_mechanism_swivel_desc",
      Icon = LoadValheimVehicleAssets.VehicleSprites.GetSprite(SpriteNames.MechanismSwivel)
    });
  }

  /// <summary>
  /// This prefab does not have WearNTear as it would just further complicate the component.  
  /// </summary>
  private void RegisterSwivelComponent()
  {
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.SwivelPrefabName, LoadValheimVehicleAssets.Mechanism_Swivel);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);

    prefab.AddComponent<SwivelComponentIntegration>();
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.SwivelPrefabName, prefab);

    PieceManager.Instance.AddPiece(new CustomPiece(prefab, true,
      new PieceConfig
      {
        PieceTable = PrefabRegistryController.GetPieceTableName(),
        Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Tools),
        Enabled = true,
        Requirements =
        [
          new RequirementConfig
          {
            Amount = 10,
            Item = "BronzeNails",
            Recover = true
          },
          new RequirementConfig
          {
            Amount = 1,
            Item = "Bronze",
            Recover = true
          }
        ]
      }));
  }

  public override void OnRegister()
  {
    RegisterSwivelPrefabPieceData();
    RegisterSwivelComponent();
  }
}