using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;
namespace ValheimVehicles.Integrations;

public class PowerSourceComponentIntegration : PowerSourceComponent, INetView
{
  protected override void Awake()
  {
    base.Awake();
    m_nview = GetComponent<ZNetView>();
  }

  protected void Start()
  {
    LoadInitialData();
  }

  public void LoadInitialData()
  {
    if (hasLoadedInitialData) return;
    if (m_nview && m_nview.IsValid() && m_nview.GetZDO() != null)
    {
      if (!m_nview.IsOwner() && ZNet.instance.IsServer())
      {
        // Host claims ownership if none exists
        m_nview.ClaimOwnership();
      }
      currentFuel = m_nview.GetZDO().GetFloat(VehicleZdoVars.Power_StoredFuel, currentFuel);
      hasLoadedInitialData = true;
    }
  }

  public override void SyncNetworkedData()
  {
    if (m_nview != null)
    {
      m_nview.GetZDO().Set(VehicleZdoVars.Power_StoredFuel, currentFuel);
    }
  }

  public override void UpdateNetworkedData()
  {
    if (!hasLoadedInitialData)
    {
      LoadInitialData();
      return;
    }
    if (this.IsNetViewValid(out var netView) && (netView.IsOwner() || ZNet.instance.IsServer()))
    {
      netView.GetZDO().Set(VehicleZdoVars.Power_StoredFuel, currentFuel);
    }
  }

  public ZNetView? m_nview
  {
    get;
    set;
  }
}