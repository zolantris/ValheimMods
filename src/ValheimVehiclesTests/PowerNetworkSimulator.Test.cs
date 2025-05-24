using NUnit.Framework;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.Tests.PowerSystem;

/// <summary>
/// TODO will need to remove all refs of Unity for this to work in the class chains. Not going to work unless a pure data model is used.
/// </summary>
[TestFixture]
public class PowerSimulationTests
{
  private const float DeltaTime = 1f;

  [Test]
  public void ConduitData_EstimateTotalDemand_Returns_ExpectedValue()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    conduit.Mode = PowerConduitMode.Charge;

    var currentEitr = 50f;
    playerData.GetEitr = () => currentEitr;
    playerData.GetEitrCapacity = () => 100f;

    simulationData.Conduits.Add(conduit);
    simulationData.Storages.Add(new PowerStorageData { Energy = 0f, EnergyCapacity = 100f });

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(conduit.EstimateTotalDemand(DeltaTime), Is.LessThanOrEqualTo(40f).Within(0.0001f), "Conduit should report a demand if eitr exists");

    currentEitr = 100f;

    Assert.That(conduit.EstimateTotalDemand(DeltaTime), Is.EqualTo(0f), "Conduit should report zero demand if eitr is zero after next update.");
  }

  [Test]
  public void ConduitData_EstimateTotalDemand_Handles_Zero()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    conduit.Mode = PowerConduitMode.Charge;
    playerData.GetEitr = () => 0f;
    playerData.GetEitrCapacity = () => 0f;

    simulationData.Conduits.Add(conduit);
    simulationData.Storages.Add(new PowerStorageData { Energy = 0f, EnergyCapacity = 100f });

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(conduit.EstimateTotalDemand(DeltaTime), Is.EqualTo(0f), "Returns at zero when at zero and zero capacity");
  }

  [Test]
  public void ConduitData_EstimateTotalDemand_Handles_MaxCapacity()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    conduit.Mode = PowerConduitMode.Charge;
    playerData.GetEitr = () => 100f;
    playerData.GetEitrCapacity = () => 100f;

    simulationData.Conduits.Add(conduit);
    simulationData.Storages.Add(new PowerStorageData { Energy = 100f, EnergyCapacity = 100f });

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(conduit.EstimateTotalDemand(DeltaTime), Is.EqualTo(0f), "At fully charged eitr total demand is zero.");
  }

  [Test]
  public void ConduitData_EstimateTotalSupply_OutputsPlayerEitrAsEnergy()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    conduit.Mode = PowerConduitMode.Drain;

    var currentEitr = 200f;
    playerData.GetEitr = () => currentEitr;
    playerData.GetEitrCapacity = () => 200f;
    playerData.Request_UseEitr = (val) => currentEitr -= val;

    simulationData.Conduits.Add(conduit);

    var storage = new PowerStorageData { Energy = 100f, EnergyCapacity = 100f };
    simulationData.Storages.Add(storage);

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(conduit.EstimateTotalSupply(DeltaTime), Is.EqualTo(200).Within(0.0001f), "Total supply shows correct value");
    storage.Energy = 50f;


    Assert.That(playerData.Eitr, Is.EqualTo(200f), "Eitr is unchanged on player");
    var discharge = conduit.SimulateConduit(storage.EnergyCapacityRemaining, DeltaTime);
    Assert.That(discharge, Is.EqualTo(50f).Within(0.0001f), "Can discharge estimated total supply per tick");
    Assert.That(playerData.Eitr, Is.EqualTo(195f).Within(0.0001f), "Eitr is decreased on player");

    var count = 0;
    while (playerData.Eitr > 0f && count < 1000)
    {
      discharge = conduit.SimulateConduit(storage.EnergyCapacityRemaining, DeltaTime);
      count++;
    }

    Assert.That(playerData.Eitr, Is.EqualTo(0), "Eitr is decreased on player");
    Assert.That(count, Is.EqualTo(39), "Eitr is completely decreased on player after 8 ticks");
  }


  [Test]
  public void Simulate_NoConsumers_NoFuel_StorageUnchanged()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    var storage = new PowerStorageData { Energy = 50f, EnergyCapacity = 100f };
    simulationData.Storages.Add(storage);

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(storage.Energy, Is.EqualTo(50f), "Storage energy should remain unchanged when there is no input or demand.");
  }

  public void GeneratePlayerDataForConduit(out PowerConduitData conduit, out PowerConduitData.PlayerEitrData playerData)
  {
    conduit = new PowerConduitData();
    var playerEitr = 5f;
    var playerMaxEitr = 50f;
    playerData = new PowerConduitData.PlayerEitrData(conduit)
    {
      PlayerId = 1,
      GetEitr = () => playerEitr,
      GetEitrCapacity = () => playerMaxEitr
    };
    conduit.PlayerDataById.Add(playerData.PlayerId, playerData);
  }


  [Test]
  public void Simulate_NoInputOrDemand_AllUnitsUnchanged()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };

    var storage1 = new PowerStorageData { Energy = 75f, EnergyCapacity = 100f };
    var storage2 = new PowerStorageData { Energy = 25f, EnergyCapacity = 50f };
    simulationData.Storages.Add(storage1);
    simulationData.Storages.Add(storage2);

    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    simulationData.Conduits.Add(conduit);

    var consumer = new PowerConsumerData { BasePowerConsumption = 0f };
    simulationData.Consumers.Add(consumer);

    var source = new PowerSourceData { Fuel = 0f, FuelEnergyYield = 10f, FuelEfficiency = 1f };
    simulationData.Sources.Add(source);

    var initialStorage1 = storage1.Energy;
    var initialStorage2 = storage2.Energy;
    var initialSourceFuel = source.Fuel;
    var initialConsumerDemand = consumer.GetRequestedEnergy(simulationData.DeltaTime);

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(storage1.Energy, Is.EqualTo(initialStorage1), "Storage 1 should remain unchanged.");
    Assert.That(storage2.Energy, Is.EqualTo(initialStorage2), "Storage 2 should remain unchanged.");
    Assert.That(source.Fuel, Is.EqualTo(initialSourceFuel), "Source fuel should remain unchanged.");
    Assert.That(consumer.GetRequestedEnergy(simulationData.DeltaTime), Is.EqualTo(initialConsumerDemand), "Consumer demand should remain unchanged.");
  }
  [Test]
  public void Simulate_Fuel_IsConsumed_And_Storage_Increases_UntilFuelZero()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };

    var storage = new PowerStorageData { Energy = 0f, EnergyCapacity = 800f };
    simulationData.Storages.Add(storage);

    var source = new PowerSourceData { Fuel = 10f, FuelEnergyYield = 10f, FuelEfficiency = 1f };
    simulationData.Sources.Add(source);

    // Bail after a reasonable number of ticks
    var maxTicks = 40;
    var tick = 0;
    var previousStorage = storage.Energy;
    var previousFuel = source.Fuel;

    while (source.Fuel > 0f && tick < maxTicks)
    {
      PowerSystemSimulator.Simulate(simulationData);

      // Assert that storage increases and fuel decreases each tick (unless full)
      Assert.That(storage.Energy, Is.GreaterThanOrEqualTo(previousStorage), "Storage should never decrease.");
      Assert.That(source.Fuel, Is.LessThanOrEqualTo(previousFuel), "Fuel should never increase.");

      previousStorage = storage.Energy;
      previousFuel = source.Fuel;
      tick++;
    }

    // After loop: storage increased, fuel is zero or almost zero
    Assert.That(storage.Energy, Is.GreaterThanOrEqualTo(100f), "Storage should have gained energy from fuel.");
    Assert.That(source.Fuel, Is.EqualTo(0f).Within(0.0001), "Fuel should be zero after simulation completes.");
  }

  [Test]
  public void ConduitsRechargeStorage_WhenActive()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };
    var storage = new PowerStorageData { Energy = 0f, EnergyCapacity = 800f };
    simulationData.Storages.Add(storage);
    var source = new PowerSourceData { Fuel = 0f, FuelEnergyYield = 10f, FuelEfficiency = 1f };
    simulationData.Sources.Add(source);

    var currentEitr = 100f;
    var hasCalledAddPlayerEitr = false;
    var hasCalledRemovePlayerEitr = false;

    var GetPlayerEitrMock = () => currentEitr;
    var Request_UseEitrMock = (float val) =>
    {
      hasCalledRemovePlayerEitr = true;
      currentEitr -= val;
      return currentEitr;
    };
    var Request_AddEitrMock = (float val) =>
    {
      hasCalledAddPlayerEitr = true;
      currentEitr += val;
    };

    var rechargeConduit = new PowerConduitData
    {
      // drains power from player to storage
      Mode = PowerConduitMode.Drain
    };
    var player = new PowerConduitData.PlayerEitrData(rechargeConduit)
    {
      PlayerId = 1234,
      Request_UseEitr = (val) => Request_UseEitrMock(val),
      Request_AddEitr = (id, val) => Request_AddEitrMock(val),
      GetEitr = GetPlayerEitrMock,
      GetEitrCapacity = () => 100f
    };
    rechargeConduit.PlayerDataById.Add(player.PlayerId, player);


    simulationData.Conduits.Add(rechargeConduit);

    // Bail after a reasonable number of ticks
    var maxTicks = 20;
    var tick = 0;
    var previousStorage = storage.Energy;
    var previousFuel = source.Fuel;

    // while (source.Fuel > 0f && tick < maxTicks)
    // {
    //   PowerSystemSimulator.Simulate(simulationData);
    //
    //   // Assert that storage increases and fuel decreases each tick (unless full)
    //   Assert.That(storage.Energy, Is.GreaterThanOrEqualTo(previousStorage), "Storage should never decrease.");
    //   Assert.That(source.Fuel, Is.LessThanOrEqualTo(previousFuel), "Fuel should never increase.");
    //
    //   previousStorage = storage.Energy;
    //   previousFuel = source.Fuel;
    //   tick++;
    // }
    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(hasCalledRemovePlayerEitr, Is.True, "RemovePlayerEitr method should have been called.");
    Assert.That(hasCalledAddPlayerEitr, Is.False, "AddPlayerEitr method should not have been called.");
  }

  // [Test]
  // public void Simulate_ShouldAllocateEnergyCorrectly_WhenSufficientSupply()
  // {
  //   var simulationData = new PowerSimulationData { DeltaTime = DeltaTime };
  //
  //   simulationData.Sources.Add(new PowerSourceData { Fuel = 100f, FuelEnergyYield = 10f, FuelEfficiency = 1f });
  //   simulationData.Storages.Add(new PowerStorageData { StoredEnergy = 100f, MaxEnergy = 200f });
  //   var consumerData = new PowerConsumerData
  //   {
  //     BasePowerConsumption = 50f
  //   };
  //
  //   simulationData.Consumers.Add(consumerData);
  //
  //   PowerSystemSimulator.Simulate(simulationData);
  //
  //   Assert.AreEqual(150f, simulationData.Storages[0].StoredEnergy, "Storage should receive correct energy.");
  //   Assert.AreEqual(0f, simulationData.Consumers[0].GetRequestedEnergy(simulationData.DeltaTime), "Consumer energy request should be fulfilled.");
  //   Assert.Less(simulationData.Sources[0].Fuel, 100f, "Fuel should be consumed.");
  // }
  //
  // [Test]
  // public void Simulate_ShouldHandleInsufficientSupplyCorrectly()
  // {
  //   var simulationData = new PowerSimulationData { DeltaTime = DeltaTime };
  //
  //   simulationData.Sources.Add(new PowerSourceData { Fuel = 1f, FuelEnergyYield = 1f, FuelEfficiency = 1f });
  //   simulationData.Storages.Add(new PowerStorageData { StoredEnergy = 100f, MaxEnergy = 200f });
  //   var consumerData = new PowerConsumerData
  //   {
  //     BasePowerConsumption = 150f
  //   };
  //
  //   simulationData.Consumers.Add(consumerData);
  //
  //   PowerSystemSimulator.Simulate(simulationData);
  //
  //   Assert.Less(simulationData.Storages[0].StoredEnergy, 200f, "Storage should not fully recharge due to limited supply.");
  //   Assert.Greater(simulationData.Consumers[0].GetRequestedEnergy(simulationData.DeltaTime), 0f, "Consumer should not have full demand met.");
  // }
  //
  // [Test]
  // public void Simulate_ShouldNotRechargeFromOwnDischarge()
  // {
  //   var simulationData = new PowerSimulationData { DeltaTime = DeltaTime };
  //
  //   var storage = new PowerStorageData { StoredEnergy = 100f, MaxEnergy = 200f };
  //   simulationData.Storages.Add(storage);
  //   var consumerData = new PowerConsumerData
  //   {
  //     BasePowerConsumption = 80f
  //   };
  //
  //   simulationData.Consumers.Add(consumerData);
  //
  //   PowerSystemSimulator.Simulate(simulationData);
  //
  //   Assert.AreEqual(20f, storage.StoredEnergy, "Storage should correctly discharge and not recharge itself.");
  //   Assert.AreEqual(0f, simulationData.Consumers[0].GetRequestedEnergy(simulationData.DeltaTime), "Consumer should receive requested energy.");
  // }
  //
  // [Test]
  // public void Simulate_ShouldCommitFuelCorrectly_AfterSimulation()
  // {
  //   var simulationData = new PowerSimulationData { DeltaTime = DeltaTime };
  //
  //   var source = new PowerSourceData { Fuel = 10f, FuelEnergyYield = 10f, FuelEfficiency = 1f };
  //   simulationData.Sources.Add(source);
  //   var consumerData = new PowerConsumerData
  //   {
  //     BasePowerConsumption = 50f
  //   };
  //   simulationData.Consumers.Add(consumerData);
  //
  //   PowerSystemSimulator.Simulate(simulationData);
  //
  //   Assert.Less(source.Fuel, 10f, "Fuel should be committed correctly after providing energy.");
  //   Assert.AreEqual(0f, simulationData.Consumers[0].GetRequestedEnergy(simulationData.DeltaTime), "Consumer should have fully received requested energy.");
  // }
  //
  // [Test]
  // public void Simulate_ShouldCorrectlyHandleMultipleSourcesAndStorages()
  // {
  //   var simulationData = new PowerSimulationData { DeltaTime = DeltaTime };
  //
  //   simulationData.Sources.Add(new PowerSourceData { Fuel = 50f, FuelEnergyYield = 10f, FuelEfficiency = 1f });
  //   simulationData.Sources.Add(new PowerSourceData { Fuel = 30f, FuelEnergyYield = 5f, FuelEfficiency = 1f });
  //   simulationData.Storages.Add(new PowerStorageData { StoredEnergy = 20f, MaxEnergy = 100f });
  //   simulationData.Storages.Add(new PowerStorageData { StoredEnergy = 50f, MaxEnergy = 100f });
  //   var consumerData = new PowerConsumerData
  //   {
  //     BasePowerConsumption = 80f
  //   };
  //   simulationData.Consumers.Add(consumerData);
  //
  //
  //   PowerSystemSimulator.Simulate(simulationData);
  //
  //   var totalStorageEnergy = simulationData.Storages.Sum(s => s.StoredEnergy);
  //   Assert.Greater(totalStorageEnergy, 70f, "Storages should have correctly recharged.");
  //   Assert.AreEqual(0f, simulationData.Consumers[0].GetRequestedEnergy(simulationData.DeltaTime), "Consumer should have received requested energy.");
  // }
}