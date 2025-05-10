using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.Components;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class SwivelConfigRPCSync : PrefabConfigRPCSync<SwivelCustomConfig, ISwivelConfig>
{
}