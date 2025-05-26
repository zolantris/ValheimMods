using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using ValheimVehicles.SharedScripts.Modules;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations.PowerSystem;
using ValheimVehicles.Integrations.PowerSystem.Interfaces;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.Structs;

// must be same namespace to override.
namespace ValheimVehicles.SharedScripts.PowerSystem.Compute;

/// <summary>
/// All overrides for integrations of PowerSystemComputeData. This injects ZDO data into the loaders allowing for the data to be populated when calling Load()
/// </summary>
public abstract partial class PowerSystemComputeData
{
  public ZDO? zdo;

  public bool IsValid = false;

  /// <summary>
  /// Guard for validation so we do not continue a bad sync without evaluating some values again.
  /// </summary>
  /// short-circuits until an error occurs
  /// <returns></returns>
  public bool TryValidate([NotNullWhen(true)] out ZDO? validZdo)
  {
    validZdo = zdo;
    if (IsValid) return true;
    if (!IsValid)
    {
      IsValid = zdo != null && zdo.IsValid();
    }
    return IsValid;
  }

  public void WithIsValidCheck(Action<ZDO> action)
  {
    if (TryValidate(out var validZdo))
    {
      try
      {
        action.Invoke(validZdo);
      }
      catch (Exception e)
      {
#if DEBUG
        LoggerProvider.LogDebug($"Error when calling {action.Method.Name} on {validZdo.m_uid} \n {e.Message} \n {e.StackTrace}");
#endif
        IsValid = false;
      }
    }
  }

  public void HandleNetworkIdUpdate(string val)
  {
    if (!zdo.IsOwner())
    {
      zdo.SetOwner(ZDOMan.GetSessionID());
    }
    ValheimExtensions.TrySetZDOStringOnChange(zdo, VehicleZdoVars.PowerSystem_NetworkId, val);
  }

  /// <summary>
  /// Should be run inside the validator.
  /// </summary>
  /// <param name="isPylon"></param>
  public void OnSharedConfigSync(bool isPylon = false)
  {
    _isActive = zdo.GetBool(VehicleZdoVars.PowerSystem_IsActive, true);
    NetworkId = zdo.GetString(VehicleZdoVars.PowerSystem_NetworkId, "");
    // config sync.
    ConnectionRange = isPylon ? PowerSystemConfig.PowerPylonRange.Value : PowerSystemConfig.PowerMechanismRange.Value;
  }

  /// <summary>
  /// Must be run last in Save method.
  /// </summary>
  public void OnSharedConfigSave()
  {
    foreach (var dirtyField in _dirtyFields)
    {
      switch (dirtyField)
      {
        case VehicleZdoVars.PowerSystem_IsActive:
          ValheimExtensions.TrySetZDOBoolOnChange(zdo, VehicleZdoVars.PowerSystem_IsActive, IsActive);
          break;
        case VehicleZdoVars.PowerSystem_NetworkId:
          if (!IsTempNetworkId)
          {
            ValheimExtensions.TrySetZDOStringOnChange(zdo, VehicleZdoVars.PowerSystem_NetworkId, NetworkId);
          }
          break;
      }
    }

    // always clears. Must be run last otherwise it will clear other config checks.
    ClearDirty();
  }
}

/// ZDO integration
public partial class PowerPylonData : IPowerComputeZdoSync
{
  public PowerPylonData(ZDO zdo)
  {
    this.zdo = zdo;
    ConnectionRange = PowerSystemConfig.PowerPylonRange.Value;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    Load();
  }

  public void OnLoadZDOSync()
  {
    // shared config
    OnSharedConfigSync(true);
  }
}

public partial class PowerConduitData : IPowerComputeZdoSync
{
  // for integration
  public readonly List<Player> Players = new();

  public PowerConduitData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;
    OnNetworkIdChange += HandleNetworkIdUpdate;
    Mode = GetConduitVariant(PrefabHash);
    Load();
  }

  /// <summary>
  /// Todo might have to abstract this to a peer system if the player is accessible via server and just send the increment from the player.
  /// </summary>
  public class PlayerEitrDataBridge : PlayerEitrData
  {
    private Player player;

    public PlayerEitrDataBridge(Player player, PowerConduitData conduitData) : base(conduitData)
    {
      this.player = player;
      PlayerId = player.GetPlayerID();

      // getters.
      GetEitr = () => WithAutoRemove(player.GetEitr);
      GetEitrCapacity = () => WithAutoRemove(player.GetMaxEitr);

      // player updaters
      // Request_UseEitr = player.UseEitr;
      Request_UseEitr = PlayerEitrRPC.Request_UseEitr;
      Request_AddEitr = PlayerEitrRPC.Request_AddEitr;
    }

    private void OnErrorPlayerRemoveById()
    {
      if (!player)
      {
        conduitData.PlayerDataById.Remove(PlayerId);
      }
    }

    private float WithAutoRemove(Func<float> action)
    {
      try
      {
        return action();
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"Player errored. Removing them from Conduit. {e}");
        OnErrorPlayerRemoveById();
      }
      return 0f;
    }
  }

  public void SanitizePlayerDataDictionary()
  {
    var keysToRemove = new List<long>();
    foreach (var playerEitrData in PlayerDataById)
    {
      if (PlayerDataById.ContainsKey(playerEitrData.Key))
      {
        keysToRemove.Add(playerEitrData.Key);
      }
    }
    keysToRemove.ForEach(x => PlayerDataById.Remove(x));
  }

  public bool AddPlayer(long playerId)
  {
    var player = Player.GetPlayer(playerId);
    if (!player)
    {
      SanitizePlayerDataDictionary();
      return false;
    }
    return AddPlayer(player);
  }
  public bool AddPlayer(Player player)
  {
    SanitizePlayerDataDictionary();

    var playerData = new PlayerEitrDataBridge(player, this);
    Players.Add(player);

    if (PlayerDataById.ContainsKey(player.GetPlayerID()))
    {
      PlayerDataById[playerData.PlayerId] = playerData;
    }
    else
    {
      PlayerDataById.Add(playerData.PlayerId, playerData);
    }

    return true;
  }

  public void RemovePlayer(long playerId)
  {
    var player = Player.GetPlayer(playerId);
    if (!player)
    {
      SanitizePlayerDataDictionary();
    }
    else
    {
      RemovePlayer(player);
    }
  }

  public void RemovePlayer(Player player)
  {
    if (!player)
    {
      LoggerProvider.LogWarning("Player could not be found or is null. This means the conduits could have null values");
      SanitizePlayerDataDictionary();
      return;
    }
    Players.Remove(player);
    PlayerDataById.Remove(player.GetPlayerID());

    SanitizePlayerDataDictionary();
  }

  /// <summary>
  /// Main simulation method for Drain Mode.
  /// </summary>
  public float DrainSimulate(float requestedEnergy)
  {
    TryRemoveEitrFromPlayers(requestedEnergy);

    return 0f;
  }

  /// <summary>
  /// Main simulation method for Charge Mode. Not ready.
  /// TODO implement this. 
  /// </summary>
  public float ChargeSimulate(float availableEnergy)
  {
    return 0f;
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();
    });
  }

  public static float GetAverageEitr(List<Player> playersWithinZone)
  {
    playersWithinZone.RemoveAll(x => !x);

    var total = 0f;
    var count = 0;

    foreach (var player in playersWithinZone)
    {
      total += player.m_eitr;
      count++;
    }

    return count > 0 ? total / count : 0f;
  }

  public static PowerConduitMode GetConduitVariant(ZDO zdo)
  {
    return GetConduitVariant(zdo.m_prefab);
  }

  public static PowerConduitMode GetConduitVariant(int prefabHash)
  {
    if (prefabHash == PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate)
      return PowerConduitMode.Charge;

    if (prefabHash == PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate)
      return PowerConduitMode.Drain;

    LoggerProvider.LogWarning($"[PowerConduitData] Unexpected prefabHash: {prefabHash}");
    return PowerConduitMode.Drain;
  }

  public bool HasPlayersWithEitr => GetAllPlayerEitr() > 0f;

  // ----------------------------------------
  // Lifecycle
  // ----------------------------------------

  // public void ResolvePlayersFromIds()
  // {
  //   Players.Clear();
  //   foreach (var id in PlayerIds)
  //   {
  //     var player = Player.GetPlayer(id);
  //     if (player != null)
  //       Players.Add(player);
  //   }
  // }

  // public float HandleEstimateDemand()
  // {
  //   if (Mode != PowerConduitMode.Charge || Players.Count == 0)
  //     return 0f;
  //
  //   var total = 0f;
  //   foreach (var player in Players)
  //   {
  //     total += MathX.Max(0f, player.m_maxEitr - player.m_eitr);
  //   }
  //
  //   return total / EitrVaporToEnergyRatio;
  // }

  public float AddEitrToPlayers(float energyBudget)
  {
    Players.RemoveAll(x => !x);
    if (Players.Count == 0 || energyBudget <= 0f) return 0f;

    List<Player> validReceivers = new(Players.Count);
    foreach (var player in Players)
    {
      if (player.m_eitr < player.m_maxEitr - MaxEitrCapMargin)
        validReceivers.Add(player);
    }

    if (validReceivers.Count == 0) return 0f;

    var perPlayer = energyBudget / validReceivers.Count;
    var totalUsed = 0f;

    foreach (var player in validReceivers)
    {
      PlayerEitrRPC.Request_AddEitr(player, perPlayer);
      totalUsed += perPlayer;
    }

    return totalUsed;
  }

  // public float SubtractEitrFromPlayers(float maxEnergyDrainable)
  // {
  //   Players.RemoveAll(x => !x);
  //   if (Players.Count == 0 || maxEnergyDrainable <= 0f) return 0f;
  //
  //   var maxDrainEitr = maxEnergyDrainable * EitrVaporToEnergyRatio;
  //   var remainingEitrToDrain = maxDrainEitr;
  //
  //   List<Player> validPlayers = new(Players.Count);
  //   foreach (var player in Players)
  //   {
  //     if (player.HaveEitr(0.01f))
  //       validPlayers.Add(player);
  //   }
  //
  //   if (validPlayers.Count == 0) return 0f;
  //
  //   var attempts = 0;
  //   while (remainingEitrToDrain > 0f && attempts++ < 5)
  //   {
  //     var perPlayer = remainingEitrToDrain / validPlayers.Count;
  //     List<Player> stillValid = new(validPlayers.Count);
  //
  //     foreach (var player in validPlayers)
  //     {
  //       if (player.HaveEitr(perPlayer))
  //       {
  //         player.UseEitr(perPlayer);
  //         remainingEitrToDrain -= perPlayer;
  //         stillValid.Add(player);
  //       }
  //     }
  //
  //     if (stillValid.Count == 0) break;
  //     validPlayers = stillValid;
  //   }
  //
  //   var totalEnergyGained = (maxDrainEitr - remainingEitrToDrain) * EitrVaporToEnergyRatio;
  //   return totalEnergyGained;
  // }
}

public partial class PowerConsumerData : IPowerComputeZdoSync
{

  public PowerConsumerData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    CanRunConsumerForDeltaTime = HandleCanRunConsumerForDeltaTime;

    Load();
  }

  public bool HandleCanRunConsumerForDeltaTime(float deltaTime)
  {
    var nextRequiredEnergyAllotment = GetWattsForLevel(powerIntensityLevel) * deltaTime;
    if (!PowerNetworkController.TryNetworkPowerData(NetworkId, out var powerSystemDisplayData))
    {
      return true;
    }
    return nextRequiredEnergyAllotment <= powerSystemDisplayData.NetworkPowerSupply;
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_BasePowerConsumption:
            ValheimExtensions.TrySetZDOFloatOnChange(validZdo, key, BasePowerConsumption);
            break;
          case VehicleZdoVars.PowerSystem_Intensity_Level:
            ValheimExtensions.TrySetZDOIntOnChange(validZdo, key, (int)powerIntensityLevel);
            break;
          case VehicleZdoVars.PowerSystem_IsDemanding:
            ValheimExtensions.TrySetZDOBoolOnChange(validZdo, key, IsDemanding);
            break;
        }
      }
      // shared config
      OnSharedConfigSave();
    });
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck((validZdo) =>
    {
      // shared config
      OnSharedConfigSync();

      IsDemanding = validZdo.GetBool(VehicleZdoVars.PowerSystem_IsDemanding, true);
      var intensityInt = validZdo.GetInt(VehicleZdoVars.PowerSystem_Intensity_Level, 0);
      powerIntensityLevel = (PowerIntensityLevel)intensityInt;
    });
  }
}

public partial class PowerSourceData : IPowerComputeZdoSync
{
  public PowerSourceData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    Load();

    OnPropertiesUpdate();
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_IsRunning:
            ValheimExtensions.TrySetZDOBoolOnChange(validZdo, key, IsRunning);
            break;
          case VehicleZdoVars.PowerSystem_Fuel:
            ConsolidateFuel();
            ValheimExtensions.TrySetZDOFloatOnChange(validZdo, key, Fuel);
            break;
          case VehicleZdoVars.PowerSystem_FuelOutputRate:
            ValheimExtensions.TrySetZDOFloatOnChange(validZdo, key, OutputRate);
            break;
        }
      }
      OnSharedConfigSave();
    });
  }

  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();

      Fuel = validZdo.GetFloat(VehicleZdoVars.PowerSystem_Fuel);
      FuelCapacity = validZdo.GetFloat(VehicleZdoVars.PowerSystem_StoredFuelCapacity, FuelCapacityDefault);
      OutputRate = validZdo.GetFloat(VehicleZdoVars.PowerSystem_FuelOutputRate, OutputRateDefault);
    });
  }
}

/// <summary>
/// Integration level class.
/// </summary>
public partial class PowerStorageData : IPowerComputeZdoSync
{
  public PowerStorageData(ZDO zdo)
  {
    this.zdo = zdo;
    PrefabHash = zdo.m_prefab;

    OnNetworkIdChange += HandleNetworkIdUpdate;
    OnLoad += OnLoadZDOSync;
    OnSave += OnSaveZDOSync;
    Load();
  }

  public void OnSaveZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      foreach (var key in _dirtyFields)
      {
        switch (key)
        {
          case VehicleZdoVars.PowerSystem_Energy:
            ValheimExtensions.TrySetZDOFloatOnChange(validZdo, key, Energy);
            break;
          case VehicleZdoVars.PowerSystem_EnergyCapacity:
            ValheimExtensions.TrySetZDOFloatOnChange(validZdo, key, EnergyCapacity);
            break;
        }
      }
      // shared config
      OnSharedConfigSave();
    });
  }

  // This is meant for integration
  public void OnLoadZDOSync()
  {
    WithIsValidCheck(validZdo =>
    {
      // shared config
      OnSharedConfigSync();
      Energy = validZdo.GetFloat(VehicleZdoVars.PowerSystem_Energy, 0);
      EnergyCapacity = validZdo.GetFloat(VehicleZdoVars.PowerSystem_EnergyCapacity, EnergyCapacityDefault);
    });
  }
}