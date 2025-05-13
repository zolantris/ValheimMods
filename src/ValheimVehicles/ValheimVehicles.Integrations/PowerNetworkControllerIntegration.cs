using System.Collections;
using Jotunn;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerNetworkControllerIntegration : PowerNetworkController
{
  protected override void FixedUpdate()
  {
    // can run on hosts only.
    if (!isActiveAndEnabled || !ZNet.instance)
    {
      return;
    }


    // only run on servers.
    // may need to run on a client if dedicated server is used.
    if (ZNet.instance.IsServer())
    {
      base.FixedUpdate();
      SyncPowerOnHost();
    }
    else
    {
      SyncPowerToNonHostClients();
    }
  }

  /// <summary>
  /// For setting the power via ZDO on hosts.
  ///
  /// This will force override the value for whoever is in control of the PowerNetworkControllerIntegration.
  /// </summary>
  public void SyncPowerOnHost()
  {
    foreach (var s in _sources)
      s.UpdateNetworkedData();

    foreach (var b in _storage)
      b.UpdateNetworkedData();
  }

  public void SyncPowerToNonHostClients()
  {
    if (!isActiveAndEnabled)
    {
      return;
    }

    foreach (var s in _sources)
      s.UpdateNetworkedData();

    foreach (var b in _storage)
      b.SyncNetworkedData();
  }
}