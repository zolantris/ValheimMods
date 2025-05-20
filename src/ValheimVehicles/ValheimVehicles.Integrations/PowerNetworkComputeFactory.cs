// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;

namespace ValheimVehicles.Integrations.PowerSystem
{
  public static class PowerComputeFactory
  {
    public static bool TryCreateSource(ZDO zdo, int prefab, out PowerSourceData result)
    {
      result = CreateSource(zdo, prefab);
      return true;
    }

    public static bool TryCreateStorage(ZDO zdo, int prefab, out PowerStorageData result)
    {
      result = CreateStorage(zdo, prefab);
      return true;
    }

    public static bool TryCreateConduit(ZDO zdo, int prefab, out PowerConduitData result)
    {
      result = CreateConduit(zdo, prefab, false);
      return true;
    }

    public static bool TryCreatePylon(ZDO zdo, int prefab, out PowerPylonData result)
    {
      result = CreatePylon(zdo, prefab);
      return true;
    }

    public static bool TryCreateComputeModel(ZDO zdo, out object computeModel)
    {
      computeModel = null;
      var hash = zdo.m_prefab;

      if (hash == PrefabNameHashes.Mechanism_Power_Pylon)
      {
        computeModel = CreatePylon(zdo, hash);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Source_Coal ||
          hash == PrefabNameHashes.Mechanism_Power_Source_Eitr)
      {
        computeModel = CreateSource(zdo, hash);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Storage_Eitr)
      {
        computeModel = CreateStorage(zdo, hash);
        return true;
      }

      if (hash == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate ||
          hash == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate)
      {
        computeModel = CreateConduit(zdo, hash, hash == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate);
        return true;
      }

      return false;
    }

    private static PowerPylonData CreatePylon(ZDO zdo, int prefabHash)
    {
      return new PowerPylonData(
        zdo.GetString(VehicleZdoVars.Power_NetworkId),
        PowerSystemConfig.PowerPylonRange.Value,
        prefabHash
      );
    }

    private static PowerSourceData CreateSource(ZDO zdo, int prefabHash)
    {
      return new PowerSourceData
      {
        PrefabHash = prefabHash,
        NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId),
        Fuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuel),
        MaxFuel = zdo.GetFloat(VehicleZdoVars.Power_StoredFuelCapacity),
        OutputRate = zdo.GetFloat(VehicleZdoVars.Power_FuelOutputRate)
      };
    }

    private static PowerStorageData CreateStorage(ZDO zdo, int prefabHash)
    {
      return new PowerStorageData
      {
        PrefabHash = prefabHash,
        NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId),
        StoredEnergy = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergy),
        MaxCapacity = zdo.GetFloat(VehicleZdoVars.Power_StoredEnergyCapacity)
      };
    }

    private static PowerConduitData CreateConduit(ZDO zdo, int prefabHash, bool isCharging)
    {
      return new PowerConduitData
      {
        PrefabHash = prefabHash,
        NetworkId = zdo.GetString(VehicleZdoVars.Power_NetworkId),
        IsCharging = isCharging
      };
    }
  }
}