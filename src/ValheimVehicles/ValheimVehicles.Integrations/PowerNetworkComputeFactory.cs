// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.PowerSystem
{
  public static class PowerComputeFactory
  {
    public static bool TryCreateSource(ZDO zdo, out PowerSourceData result)
    {
      result = CreateSource(zdo);
      return true;
    }

    public static bool TryCreateStorage(ZDO zdo, out PowerStorageData result)
    {
      result = CreateStorage(zdo);
      return true;
    }

    public static bool TryCreateConduit(ZDO zdo, out PowerConduitData result)
    {
      result = CreateConduit(zdo);
      return true;
    }

    public static bool TryCreateConsumer(ZDO zdo, out PowerConsumerData result)
    {
      result = CreateConsumer(zdo);
      return true;
    }

    public static bool TryCreatePylon(ZDO zdo, int prefab, out PowerPylonData result)
    {
      result = CreatePylon(zdo);
      return true;
    }

    public static bool TryCreateComputeModel(ZDO zdo, out object computeModel)
    {
      computeModel = null;
      var hash = zdo.m_prefab;

      if (hash == PrefabNameHashes.Mechanism_Power_Pylon)
      {
        computeModel = CreatePylon(zdo);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Source_Coal ||
          hash == PrefabNameHashes.Mechanism_Power_Source_Eitr)
      {
        computeModel = CreateSource(zdo);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
      {
        computeModel = CreateStorage(zdo);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate ||
          hash == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate)
      {
        computeModel = CreateConduit(zdo);
        return true;
      }

      return false;
    }


    private static PowerPylonData CreatePylon(ZDO zdo)
    {
      return new PowerPylonData(zdo);
    }

    private static PowerConsumerData CreateConsumer(ZDO zdo)
    {
      return new PowerConsumerData(zdo);
    }

    private static PowerSourceData CreateSource(ZDO zdo)
    {
      return new PowerSourceData(zdo);
    }

    private static PowerStorageData CreateStorage(ZDO zdo)
    {
      return new PowerStorageData(zdo);
    }

    private static PowerConduitData CreateConduit(ZDO zdo)
    {
      return new PowerConduitData(zdo);
    }
  }
}