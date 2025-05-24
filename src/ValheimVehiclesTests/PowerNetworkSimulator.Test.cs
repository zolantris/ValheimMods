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
    var storage2 = new PowerStorageData { Energy = 50f, EnergyCapacity = 100f };
    simulationData.Storages.Add(storage1);
    simulationData.Storages.Add(storage2);

    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    playerData.GetEitr = () => 0f;
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

    Assert.That(storage1.Energy + storage2.Energy, Is.EqualTo(initialStorage1 + initialStorage2), "Energy is conserved");
    Assert.That(storage1.Energy, Is.EqualTo(100f), "Storage 1 gets energy from storage 2.");
    Assert.That(storage2.Energy, Is.EqualTo(25f), "Storage 2 has 25f remaining.");

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
  public void Simulate_CanFillEnergyStoragesToMaxAndNotDecreaseFuelFurther()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };

    var storage1 = new PowerStorageData { Energy = 75f, EnergyCapacity = 100f };
    var storage2 = new PowerStorageData { Energy = 25f, EnergyCapacity = 100f };
    simulationData.Storages.Add(storage1);
    simulationData.Storages.Add(storage2);

    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    simulationData.Conduits.Add(conduit);

    var consumer = new PowerConsumerData { BasePowerConsumption = 0f };
    simulationData.Consumers.Add(consumer);

    var source = new PowerSourceData { Fuel = 50f, FuelEnergyYield = 10f, FuelEfficiency = 1f };
    simulationData.Sources.Add(source);

    // Bail after a reasonable number of ticks
    var maxTicks = 30;
    var increment = 0;

    while (source.Fuel > 0f && increment < maxTicks)
    {
      PowerSystemSimulator.Simulate(simulationData);
      increment++;
    }

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(storage1.Energy, Is.EqualTo(storage1.EnergyCapacity), "Storage 1 should be full.");
    Assert.That(storage2.Energy, Is.EqualTo(storage2.EnergyCapacity), "Storage 2 should be full.");

    Assert.That(increment, Is.EqualTo(30));
    Assert.That(source.Fuel, Is.EqualTo(43f));
  }

  [Test]
  public void Simulate_CanKeepConsumersActive_While_Net_DrainingEnergy()
  {
    var simulationData = new PowerSimulationData { DeltaTime = 1f };

    var storage1 = new PowerStorageData { Energy = 100f, EnergyCapacity = 1000f };
    var storage2 = new PowerStorageData { Energy = 1000f, EnergyCapacity = 1000f };
    simulationData.Storages.Add(storage1);
    simulationData.Storages.Add(storage2);

    GeneratePlayerDataForConduit(out var conduit, out var playerData);
    simulationData.Conduits.Add(conduit);

    var consumer = new PowerConsumerData { BasePowerConsumption = 100f, _isActive = true, IsDemanding = true };
    simulationData.Consumers.Add(consumer);

    var source = new PowerSourceData { Fuel = 1f, FuelEnergyYield = 1f, FuelEfficiency = 1f };
    simulationData.Sources.Add(source);

    // Bail after a reasonable number of ticks
    var maxTicks = 4;
    var increment = 0;

    while (increment < maxTicks)
    {
      PowerSystemSimulator.Simulate(simulationData);
      increment++;
      Assert.That(consumer.IsActive, Is.True, "Consumer should always be active while power is in storage.");
    }

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(storage1.Energy, Is.EqualTo(0), "Storage 1 should be empty. Lowest storage size is recharged last.");
    Assert.That(storage2.Energy, Is.EqualTo(826f), "Storage 2 should be near full");

    Assert.That(consumer.IsActive, Is.True, "Consumer should always be active while power is in storage.");


    Assert.That(increment, Is.EqualTo(4));
    Assert.That(source.Fuel, Is.EqualTo(0f));
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

    PowerSystemSimulator.Simulate(simulationData);

    Assert.That(hasCalledRemovePlayerEitr, Is.True, "RemovePlayerEitr method should have been called.");
    Assert.That(hasCalledAddPlayerEitr, Is.False, "AddPlayerEitr method should not have been called.");
  }
}