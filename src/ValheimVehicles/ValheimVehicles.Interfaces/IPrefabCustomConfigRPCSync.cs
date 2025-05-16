namespace ValheimVehicles.Interfaces;

public interface IPrefabCustomConfigRPCSync<T> : INetView
{
  T CustomConfig { get; set; }
  // SyncPrefabConfig is called after the SetPrefabConfig RPC is called.
  public void RPC_Load(long sender);
  public void RPC_Save(long sender, ZPackage pkg);

  // sync methods (that RPC calls)
  // This should never be called directly outside of Start/Awake.
  internal void Load(bool forceUpdate = false);
  internal void Save();

  // request methods to be invoked by the parent.
  public void Request_Load();
  public void Request_Save();

  // booleans
  public bool hasRegisteredRPCListeners { get; set; }

  // registration methods
  public void UnregisterRPCListeners();
  public void RegisterRPCListeners();
}