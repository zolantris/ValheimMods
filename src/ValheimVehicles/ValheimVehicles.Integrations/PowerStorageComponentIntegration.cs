using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;
namespace ValheimVehicles.Integrations;

public class PowerStorageComponentIntegration : PowerStorageComponent, INetView
{
  protected override void Awake()
  {
    base.Awake();
    m_nview = GetComponent<ZNetView>();
  }

  public void Start()
  {
    LoadInitialData();
  }

  public void LoadInitialData()
  {
    if (this.IsNetViewValid(out var netView))
    {
      storedPower = netView.GetZDO().GetFloat(VehicleZdoVars.Power_StoredPower, storedPower);
      hasLoadedInitialData = true;
    }
  }

  public override void SyncNetworkedData()
  {
    if (!hasLoadedInitialData)
    {
      LoadInitialData();
      return;
    }
    if (this.IsNetViewValid(out var netView) && (netView.IsOwner() || ZNet.instance && ZNet.instance.IsDedicated()))
    {
      netView.GetZDO().Set(VehicleZdoVars.Power_StoredPower, storedPower);
    }
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
}