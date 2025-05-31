using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

/// ZDO integration
public partial class PowerPylonData : IPowerComputeZdoSync
{
  public PowerPylonData(ZDO zdo)
  {
    this.zdo = zdo;
    ConnectionRange = PowerSystemConfig.PowerPylonRange.Value;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    Load();
  }

  public void OnLoadZDOSync()
  {
    // shared config
    OnSharedConfigSync(true);
  }
}