namespace ValheimVehicles.Interfaces;

internal interface IPrefabCustomConfigRPCSync<T> : IPrefabConfig<T>, IPrefabConfigActions, INetView
{
  // SyncPrefabConfig is called after the SetPrefabConfig RPC is called.
  internal void RPC_Load(long sender);
  internal void RPC_Save(long sender, ZPackage pkg);

  // for owners only
  void Owner_Save();

  // request methods to be invoked by the parent.
  internal void Request_Load();
  internal void Request_Save();

  // booleans
  internal bool hasRegisteredRPCListeners { get; set; }

  // registration methods
  internal void UnregisterRPCListeners();
  internal void RegisterRPCListeners();
}