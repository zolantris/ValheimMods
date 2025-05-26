using System.Collections.Generic;
using System.Linq;
namespace ValheimVehicles.Interfaces;

public interface IPrefabSyncRPCSubscribers
{
  void Load(ZDO zdo, string[]? filterKeys = null);
  void Save(ZDO zdo, string[]? filterKeys = null);
}