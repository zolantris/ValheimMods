// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using ValheimVehicles.SharedScripts.Modules;

namespace ValheimVehicles.SharedScripts.PowerSystem.Compute
{
  /// <summary>
  /// Simulates behavior of Eitr charging/draining for a power conduit.
  /// </summary>
  public partial class PowerConduitData : PowerSystemComputeData
  {
    public readonly List<long> PlayerIds = new();

    public PowerConduitMode Mode = PowerConduitMode.Drain;

    public static float MaxEitrCapMargin = 0.1f;

    // eitr vapor is the player eitr. This is different from Eitr fuel.
    public static float EitrVaporToEnergyRatio = 0.01f; // default is 100 eitr vapor = 1 unit of energy.
    public static float RechargeRate = 10f;

    public PowerConduitData() {}

    // overrides (in partial method for integrations)
    public Func<float, float> OnChargeSimulate = _ => 0f;
    public Func<float, float> OnDrainSimulate = _ => 0f;

    public float DeltaTime { get; set; } = 0.01f;
    public float DeltaTimeRechargeRate => DeltaTime * RechargeRate;

    public void UpdateActiveStatus()
    {
      if (PlayerIds.Count > 0)
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
      return playerEitr * EitrVaporToEnergyRatio;
    }

    /// <summary>
    /// An abstraction for Player object which allows for binding player getters to this struct and making it testable via Nunit.
    /// </summary>
    public class PlayerEitrData
    {
      private PowerConduitData conduitData;
      public Func<float> GetEitr = () => 0f;
      public Func<float> GetEitrCapacity = () => 0f;
      public long PlayerId;
      public float Eitr => GetEitr();
      public float EitrCapacity => GetEitr() - GetEitrCapacity();

      // methods that fire RPC_For Integration. But can be used to test via the stub.
      public Action<long, float> Request_AddEitr = (_, _) => {};
      public Action<float> Request_UseEitr = (_) => {};

      public PlayerEitrData(PowerConduitData conduit)
      {
        conduitData = conduit;
      }
    }

    public Dictionary<long, PlayerEitrData> PlayerDataById;


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
        total += playerEitrData.EitrCapacity * threshold - playerEitrData.Eitr;
      }
      return total;
    }

    /// <summary>
    /// Returns the remaining eitr required to fill a player bar to the threshold capacity.
    /// </summary>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public float GetAllPlayerRemainingEitr(float threshold = 0.9f)
    {
      var total = 0f;
      foreach (var playerEitrData in PlayerDataById.Values)
      {
        total += playerEitrData.EitrCapacity * threshold - playerEitrData.Eitr;
      }
      return total;
    }

    /// <summary>
    /// Get the next deltaTime Eitr to remove.
    /// </summary>
    [UsedImplicitly]
    public bool TryGetNeededEitr(float remainingEnergy, float eitrVaporAvailable, out float deltaEitr, out float deltaEnergy)
    {
      deltaEitr = 0f;
      deltaEnergy = 0f;
      if (remainingEnergy < 0.01f) return false;

      var eitrVaporToEnergy = eitrVaporAvailable * EitrVaporToEnergyRatio * DeltaTimeRechargeRate;
      if (remainingEnergy < eitrVaporToEnergy)
      {
        deltaEitr = remainingEnergy / eitrVaporToEnergy;
        deltaEnergy = remainingEnergy;
        return true;
      }
      deltaEnergy = MathX.Min(remainingEnergy, eitrVaporToEnergy);

      return true;
    }

    public float SimulateConduit(float energyAvailableOrDrainable, float deltaTime)
    {
      DeltaTime = deltaTime;
      if (PlayerIds.Count == 0 || energyAvailableOrDrainable <= 0f)
        return 0f;

      // var allPlayerEitr = GetAllPlayerEitr();
      // var potentialEitrConvertedEnergy = ConvertPlayerEitrToEnergy(allPlayerEitr);



      var deltaEnergy = Mode switch
      {
        PowerConduitMode.Charge => OnChargeSimulate(energyAvailableOrDrainable),
        PowerConduitMode.Drain => OnDrainSimulate(energyAvailableOrDrainable),
        _ => 0f
      };

      return deltaEnergy;
    }
    public float EstimateTotalDemand()
    {
      return GetAllPlayerEitr() * DeltaTimeRechargeRate;
    }

    private bool _isActive = true;
    public override bool IsActive => _isActive;
  }
}