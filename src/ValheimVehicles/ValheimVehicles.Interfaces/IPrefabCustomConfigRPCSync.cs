using System.Collections.Generic;
namespace ValheimVehicles.Interfaces;

public interface IPrefabCustomConfigRPCSync<T> : IPrefabConfig<T>, INetView, ISuppressableConfigReceiver, IPrefabSyncRPCSubscribeActions
{
  // SyncPrefabConfig is called after the SetPrefabConfig RPC is called.
  internal void RPC_Load(long sender, ZPackage pkg);


  void CommitConfigChange(T newConfig);
  void Request_CommitConfigChange(T newConfig);

  // request methods to be invoked by the parent.
  internal void Request_Load();
  // booleans
  internal bool hasRegisteredRPCListeners { get; set; }

  // registration methods
  internal void UnregisterRPCListeners();
  internal void RegisterRPCListeners();
}