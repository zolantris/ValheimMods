namespace ValheimVehicles.Interfaces;

public interface IPrefabCustomConfigRPCSync<T> : INetView
{
  T CustomConfig { get; set; }
  // SyncPrefabConfig is called after the SetPrefabConfig RPC is called.
  public void RPC_SyncPrefabConfig(long sender);
  public void RPC_SetPrefabConfig(long sender, ZPackage pkg);

  // sync methods (that RPC calls)
  public void SyncPrefabConfig(bool forceUpdate = false);
  public void SendPrefabConfig();

  // booleans
  public bool hasRegisteredRPCListeners { get; set; }

  // registration methods
  public void UnregisterRPCListeners();
  public void RegisterRPCListeners();
}