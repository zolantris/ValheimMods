using ValheimVehicles.Constants;
using ValheimVehicles.Helpers;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
namespace ValheimVehicles.Integrations;

public class PowerPylonComponentIntegration : PowerPylon, Hoverable, Interactable, INetView
{
  protected override void Awake()
  {
    m_nview = GetComponent<ZNetView>();
    // don't do anything when we aren't initialized.
    base.Awake();
  }
  public string GetHoverText()
  {
    var baseString = "";

    baseString += ModTranslations.PowerPylon_Name;

    baseString += PowerNetworkController.CanShowNetworkData ? $"\n{ModTranslations.PowerPylon_NetworkInformation_Show}" : $"\n{ModTranslations.PowerPylon_NetworkInformation_Hide}";

    if (PowerNetworkController.CanShowNetworkData)
    {
      baseString += PowerNetworkController.GetNetworkPowerStatusString(NetworkId);
    }

    return baseString;
  }

  public string GetHoverName()
  {
    return $"Power Pylon (HoverName) on Network: {NetworkId}";
  }
  public ZNetView? m_nview
  {
    get;
    set;
  }
  public bool Interact(Humanoid user, bool hold, bool alt)
  {
    if (!hold)
    {
      PowerNetworkController.CanShowNetworkData = !PowerNetworkController.CanShowNetworkData;
      return true;
    }
    return false;
  }
  public bool UseItem(Humanoid user, ItemDrop.ItemData item)
  {
    throw new System.NotImplementedException();
  }
}