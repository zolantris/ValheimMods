using System.Collections.Generic;

namespace ZdoWatcher;

public static class ZdoVarManager
{
  public static Dictionary<string, string> ZdoVariables = new();

  // This is the same UUID hash from ValheimRAFT FYI
  public static readonly int PersistentUidHash =
    "PersistentID".GetStableHashCode();
}