using System.Collections.Generic;
using System.Linq;
namespace ValheimVehicles.Interfaces;

public interface IPrefabSyncRPCSubscribeActions
{
  void Load(ZDO zdo, string[]? filterKeys = null);
  void Save(ZDO zdo, string[]? filterKeys = null);
}