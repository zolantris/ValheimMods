using Jotunn.Managers;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Prefabs.Registry;

/**
 * example registry of a prefab
 */
public class ExamplePrefab : RegisterPrefab<ExamplePrefab>
{
  private void RegisterPrefabXYZ()
  {

  }
  public override void OnRegister()
  {
    RegisterPrefabXYZ();
  }
}