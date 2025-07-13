using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
using ValheimVehicles.UI;
namespace ValheimVehicles.Integrations;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class CannonConfigSync : PrefabConfigSync<CannonPersistentConfig, ICannonPersistentConfig>
{
}