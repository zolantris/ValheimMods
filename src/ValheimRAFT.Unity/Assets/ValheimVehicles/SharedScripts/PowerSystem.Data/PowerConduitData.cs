// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using ValheimVehicles.SharedScripts.Modules;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  /// <summary>
  /// Simulates behavior of Eitr charging/draining for a power conduit.
  /// </summary>
  public partial class PowerConduitData : PowerSystemComputeData
  {
    public PowerConduitData() {}
    public PowerConduitMode Mode = PowerConduitMode.Drain;

    public static float MaxEitrCapMargin = 0.1f;
    public readonly Dictionary<long, PlayerEitrData> PlayerPeerToData = new();

    // Eitr regen is the player Eitr that replenishes. This is much more diluted compared to Refined Eitr.
    public static float EitrRegenCostPerInterval = 10f;
    public static float EnergyChargePerInterval = 1f;
    public static float RechargeRate = 10f;

    // overrides (in partial method for integrations)

    public float DeltaTime { get; set; } = 0.01f;
    public float DeltaTimeRechargeRate => DeltaTime * RechargeRate;

    public void UpdateActiveStatus()
    {
      if (PlayerPeerToData.Count > 0)
      {
        _isActive = true;
      }
      else
      {
        _isActive = false;
      }
    }

    public static PowerConduitMode GetVariant(int prefabHash)
    {
      if (prefabHash == PrefabNameHashes.Mechanism_Power_Conduit_Charge_Plate) return PowerConduitMode.Charge;
      if (prefabHash == PrefabNameHashes.Mechanism_Power_Conduit_Drain_Plate) return PowerConduitMode.Drain;
      LoggerProvider.LogError("Unknown prefabHash. Returning <Charge> conduit as default mode");
      return PowerConduitMode.Charge;
    }

    public float ConvertPlayerEitrToEnergy(float playerEitr)
    {
      return playerEitr * EitrRegenCostPerInterval / EnergyChargePerInterval;
    }

    /// <summary>
    /// An abstraction for Player object which allows for binding player getters to this struct and making it testable via Nunit.
    /// </summary>
    public class PlayerEitrData
    {
      public readonly PowerConduitData conduitData;
      public float Eitr = 0f;
      public float EitrCapacity = 0f;
      public long PlayerId;
      // methods that fire RPC_For Integration. But can be used to test via the stub.
      public Action<long, float> Request_AddEitr = (_, _) => {};
      public Action<long, float> Request_UseEitr = (_, _) => {};

      public PlayerEitrData(PowerConduitData conduitData)
      {
        this.conduitData = conduitData;
      }

      public PlayerEitrData(long playerId, float eitr, float eitrCapacity, PowerConduitData conduitData)
      {
        PlayerId = playerId;
        Eitr = eitr;
        EitrCapacity = eitrCapacity;
        this.conduitData = conduitData;
      }
    }


    /// <summary>
    /// Returns the total eitr of all players in the zone.
    /// </summary>
    public float GetAllPlayerEitr(float threshold = 0.9f)
    {
      var total = 0f;
      foreach (var playerEitrData in PlayerPeerToData.Values)
      {
        total += MathX.Min(playerEitrData.EitrCapacity, playerEitrData.Eitr);
      }
      return total;
    }

    /// <summary>
    /// Returns the remaining eitr required to fill a player bar to the threshold capacity.
    /// </summary>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public float GetAllPlayerRemainingEitrCapacity(float threshold = 0.9f)
    {
      var total = 0f;
      foreach (var playerEitrData in PlayerPeerToData.Values)
      {
        total += MathX.Clamp(playerEitrData.EitrCapacity * threshold - playerEitrData.Eitr, 0, playerEitrData.EitrCapacity);
      }
      return total;
    }

    private float lastUpdateTime = 0f;
    private static float nextUpdateInterval = 1f;

    private bool UpdateSimulationTime(float dt)
    {
      DeltaTime = dt;
      lastUpdateTime += DeltaTime;

      // 1 second interval will allow other methods to run.
      if (lastUpdateTime >= nextUpdateInterval)
      {
        lastUpdateTime = 0f;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Peeks at delta time to see if the next update will allow a full simulation.
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public bool CanRunSimulation(float dt)
    {
      return lastUpdateTime + dt >= nextUpdateInterval;
    }

    /// <summary>
    /// Get the next deltaTime Eitr to remove.
    /// </summary>
    public bool TryGetNeededEitr(float remainingEnergy, float eitrRegenAvailable, out float deltaEitr, out float deltaEnergy)
    {
      var eitrProcessable = MathX.Min(eitrRegenAvailable, EitrRegenCostPerInterval);
      var energyProduced = EnergyChargePerInterval;
      if (remainingEnergy < energyProduced)
      {
        // Clamp to remainingEnergy
        deltaEitr = remainingEnergy / EnergyChargePerInterval * EitrRegenCostPerInterval;
        deltaEnergy = remainingEnergy;
        return true;
      }
      deltaEnergy = energyProduced;
      deltaEitr = eitrProcessable;
      return true;
    }

    /// <summary>
    /// Must convert from energy to eitr regen. Then back to energy to see how much energy was consumed
    /// </summary>
    /// <param name="energyBudget"></param>
    /// <returns></returns>
    public float AddEitrToPlayers(float energyBudget)
    {
      if (PlayerPeerToData.Count == 0 || energyBudget <= 0f) return 0f;

      var maxEitrRecharge = energyBudget * EitrRegenCostPerInterval / EnergyChargePerInterval;

      List<PlayerEitrData> validReceivers = new(PlayerPeerToData.Count);
      foreach (var playerEitrData in PlayerPeerToData.Values)
      {
        if (playerEitrData.Eitr > 0.1f && playerEitrData.Eitr < playerEitrData.EitrCapacity * 0.9f && playerEitrData.EitrCapacity > 10f && playerEitrData.EitrCapacity > EitrRegenCostPerInterval)
          validReceivers.Add(playerEitrData);
      }

      if (validReceivers.Count == 0) return 0f;

      var perPlayer = MathX.Clamp(maxEitrRecharge / validReceivers.Count, 0, EitrRegenCostPerInterval);
      var totalEitrUsed = 0f;

      foreach (var playerEitrData in validReceivers)
      {
        playerEitrData.Request_AddEitr(playerEitrData.PlayerId, perPlayer);
        totalEitrUsed += perPlayer;
      }

      var totalEnergyUsed = totalEitrUsed / EitrRegenCostPerInterval * EnergyChargePerInterval;

      return totalEnergyUsed;
    }

    public float TryRemoveEitrFromPlayers(float maxEnergyDrainable)
    {
      if (Mode != PowerConduitMode.Drain || PlayerPeerToData.Count == 0) return 0f;

      var totalEnergy = 0f;

      foreach (var playerData in PlayerPeerToData.Values)
      {
        if (totalEnergy >= maxEnergyDrainable)
          break;

        var playerEitr = playerData.Eitr;
        // do not fire eitr drain when low on eitr.
        if (playerEitr <= 2f) continue;
        TryGetNeededEitr(maxEnergyDrainable - totalEnergy, playerEitr, out var deltaEitrRegen, out var deltaEnergy);

        playerData.Eitr -= deltaEitrRegen;
        playerData.Request_UseEitr(playerData.PlayerId, deltaEitrRegen);
        totalEnergy += deltaEnergy;
      }

      return totalEnergy;
    }

    public float SimulateConduit(float energyAvailableOrDrainable, float deltaTime)
    {
      if (!UpdateSimulationTime(deltaTime)) return 0f;

      if (PlayerPeerToData.Count == 0 || energyAvailableOrDrainable <= 0f)
        return 0f;

      var deltaEnergy = 0f;

      if (Mode == PowerConduitMode.Charge)
      {
        deltaEnergy = AddEitrToPlayers(energyAvailableOrDrainable);
      }

      if (Mode == PowerConduitMode.Drain)
      {
        deltaEnergy = TryRemoveEitrFromPlayers(energyAvailableOrDrainable);
      }

      return deltaEnergy;
    }


    /// <summary>
    /// Mode Charge will remove/Demand from the PowerSystem.
    /// - this is per tick we will demand maximum of N players * Energy conversion of eitr. Cannot exceed our update tick.
    /// </summary>
    public float EstimateTotalDemand(float dt)
    {
      if (!CanRunSimulation(dt)) return 0f;
      if (Mode == PowerConduitMode.Drain) return 0f;
      var totalRemainingCapacity = MathX.Min(GetAllPlayerRemainingEitrCapacity() / EitrRegenCostPerInterval, PlayerPeerToData.Count * EnergyChargePerInterval);
      return totalRemainingCapacity;
    }

    /// <summary>
    /// Drain will Add/Supply to the PowerSystem.
    /// - this is per tick we will supply maximum of N players * Energy conversion of eitr. Cannot exceed our update tick.
    /// </summary>
    public float EstimateTotalSupply(float dt)
    {
      if (!CanRunSimulation(dt)) return 0f;
      if (Mode == PowerConduitMode.Charge) return 0f;
      var totalRemainingCapacity = MathX.Min(GetAllPlayerRemainingEitrCapacity(), PlayerPeerToData.Count * EnergyChargePerInterval);
      return totalRemainingCapacity;
    }

    private bool _isActive = true;
    public override bool IsActive => _isActive;
  }
}