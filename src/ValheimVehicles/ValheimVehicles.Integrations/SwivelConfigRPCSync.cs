using ValheimVehicles.Components;
using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.Integrations;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class SwivelConfigRPCSync : PrefabConfigRPCSync<SwivelCustomConfig, ISwivelConfig>
{
  public void Request_NextMotionState(MotionState nextMotionState)
  {
    // must update this before saving or invoking the RPC to serialize the sync.
    CustomConfig.MotionState = nextMotionState;
    Request_Save();
  }
}