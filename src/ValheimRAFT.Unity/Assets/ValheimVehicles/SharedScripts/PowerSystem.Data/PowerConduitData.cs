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
    public readonly Dictionary<long, PlayerEitrData> PlayerDataById = new();

    // eitr vapor is the player eitr. This is different from Eitr fuel.
    public static float EitrVaporCostPerTick = 10f;
    public static float EnergyChargePerTick = 1f;
    public static float RechargeRate = 10f;

    // overrides (in partial method for integrations)

    public float DeltaTime { get; set; } = 0.01f;
    public float DeltaTimeRechargeRate => DeltaTime * RechargeRate;

    public void UpdateActiveStatus()
    {
      if (PlayerDataById.Count > 0)
      {
        _isActive = true;
      }
      else
      {
        _isActive = false;
      }
    }

    public float ConvertPlayerEitrToEnergy(float playerEitr)
    {
      return playerEitr * EitrVaporCostPerTick / EnergyChargePerTick;
    }

    /// <summary>
    /// An abstraction for Player object which allows for binding player getters to this struct and making it testable via Nunit.
    /// </summary>
    public class PlayerEitrData
    {
      public readonly PowerConduitData conduitData;
      public Func<float> GetEitr = () => 0f;
      public Func<float> GetEitrCapacity = () => 0f;
      public long PlayerId;
      public float Eitr => GetEitr();
      public float EitrCapacity => GetEitrCapacity();
      public float EitrCapacityRemaining => GetEitrCapacity() - GetEitr();

      // methods that fire RPC_For Integration. But can be used to test via the stub.
      public Action<long, float> Request_AddEitr = (_, _) => {};
      public Action<float> Request_UseEitr = (_) => {};

      public PlayerEitrData(PowerConduitData conduit)
      {
        conduitData = conduit;
      }
    }

    public static float EitrVaporToEnergyRatio = EitrVaporCostPerTick / EnergyChargePerTick;


    // <summary>
    /// Returns the total eitr of all players in the zone.
    /// </summary>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public float GetAllPlayerEitr(float threshold = 0.9f)
    {
      var total = 0f;
      foreach (var playerEitrData in PlayerDataById.Values)
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
      foreach (var playerEitrData in PlayerDataById.Values)
      {
        total += MathX.Clamp(playerEitrData.EitrCapacity * threshold - playerEitrData.Eitr, 0, playerEitrData.EitrCapacity);
      }
      return total;
    }

    /// <summary>
    /// Get the next deltaTime Eitr to remove.
    /// </summary>
    public bool TryGetNeededEitr(float remainingEnergy, float eitrVaporAvailable, out float deltaEitr, out float deltaEnergy)
    {
      var eitrProcessable = MathX.Min(eitrVaporAvailable, EitrVaporCostPerTick) * DeltaTimeRechargeRate;
      var energyProduced = EnergyChargePerTick * DeltaTimeRechargeRate;
      if (remainingEnergy < energyProduced)
      {
        // Clamp to remainingEnergy
        deltaEitr = remainingEnergy * EitrVaporToEnergyRatio;
        deltaEnergy = remainingEnergy;
        return true;
      }
      deltaEnergy = energyProduced;
      deltaEitr = eitrProcessable;
      return true;
    }

    public float TryRemoveEitrFromPlayers(float maxEnergyDrainable)
    {
      if (Mode != PowerConduitMode.Drain || PlayerDataById.Count == 0) return 0f;

      var totalEnergy = 0f;

      foreach (var playerData in PlayerDataById.Values)
      {
        if (totalEnergy >= maxEnergyDrainable)
          break;

        var playerEitr = playerData.Eitr;
        // do not fire eitr drain when low on eitr.
        if (playerEitr <= 2f) continue;
        TryGetNeededEitr(maxEnergyDrainable - totalEnergy, playerEitr, out var deltaEitrVapor, out var deltaEnergy);

        playerData.Request_UseEitr(deltaEitrVapor);
        totalEnergy += deltaEnergy;
      }

      return totalEnergy;
    }

    public float SimulateConduit(float energyAvailableOrDrainable, float deltaTime)
    {
      DeltaTime = deltaTime;
      if (PlayerDataById.Count == 0 || energyAvailableOrDrainable <= 0f)
        return 0f;


      var deltaEnergy = 0f;

      if (Mode == PowerConduitMode.Charge)
      {
        return 0f;
      }

      if (Mode == PowerConduitMode.Drain)
      {
        deltaEnergy = TryRemoveEitrFromPlayers(energyAvailableOrDrainable);
      }

      return deltaEnergy;
    }


    /// <summary>
    /// Mode Charge will remove/Demand from the PowerSystem.
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public float EstimateTotalDemand(float dt)
    {
      DeltaTime = dt;
      if (Mode == PowerConduitMode.Drain) return 0f;
      return GetAllPlayerRemainingEitrCapacity() * EitrVaporToEnergyRatio * DeltaTimeRechargeRate;
    }

    /// <summary>
    /// Drain will Add/Supply to the PowerSystem
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public float EstimateTotalSupply(float dt)
    {
      DeltaTime = dt;
      if (Mode == PowerConduitMode.Charge) return 0f;
      return GetAllPlayerEitr() * EitrVaporToEnergyRatio * DeltaTimeRechargeRate;
    }

    private bool _isActive = true;
    public override bool IsActive => _isActive;
  }
}