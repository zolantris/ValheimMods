// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Config;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  /// <summary>
  /// Simulates behavior of Eitr charging/draining for a power conduit.
  /// </summary>
  public class PowerConduitData : PowerDataBase
  {
    public readonly List<long> PlayerIds = new();
    public readonly List<Player> Players = new();

    public PowerConduitMode Mode = PowerConduitMode.Drain;

    private const float MaxEitrCapMargin = 0.1f;
    private const float EitrToEnergyRatio = 40f;

    public bool IsActive => Players.Count > 0;
    public bool HasPlayersWithEitr => GetAverageEitr(Players) > 0f;
    // ----------------------------------------
    // Static Helpers
    // ----------------------------------------

    public static PowerConduitMode GetConduitVariant(ZDO zdo)
    {
      return GetConduitVariant(zdo.m_prefab);
    }

    public static PowerConduitMode GetConduitVariant(int prefabHash)
    {
      if (prefabHash == PrefabNameHashes.Mechanism_Power_Consumer_Charge_Plate)
        return PowerConduitMode.Charge;

      if (prefabHash == PrefabNameHashes.Mechanism_Power_Consumer_Drain_Plate)
        return PowerConduitMode.Drain;

      LoggerProvider.LogWarning($"[PowerConduitData] Unexpected prefabHash: {prefabHash}");
      return PowerConduitMode.Drain;
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

    // ----------------------------------------
    // Lifecycle
    // ----------------------------------------

    public void ResolvePlayersFromIds()
    {
      Players.Clear();
      foreach (var id in PlayerIds)
      {
        var player = Player.GetPlayer(id);
        if (player != null)
          Players.Add(player);
      }
    }

    public float EstimateDemand()
    {
      if (Mode != PowerConduitMode.Charge || Players.Count == 0)
        return 0f;

      var total = 0f;
      foreach (var player in Players)
      {
        total += Mathf.Max(0f, player.m_maxEitr - player.m_eitr);
      }

      return total / EitrToEnergyRatio;
    }

    // ----------------------------------------
    // Simulation
    // ----------------------------------------

    public float SimulateConduit(float energyAvailableOrDrainable)
    {
      if (Players.Count == 0 || energyAvailableOrDrainable <= 0f)
        return 0f;

      return Mode switch
      {
        PowerConduitMode.Charge => AddEitrToPlayers(energyAvailableOrDrainable),
        PowerConduitMode.Drain => SubtractEitrFromPlayers(energyAvailableOrDrainable),
        _ => 0f
      };
    }

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
        PowerConduitPlateComponentIntegration.Request_AddEitr(player, perPlayer);
        totalUsed += perPlayer;
      }

      return totalUsed;
    }

    public float SubtractEitrFromPlayers(float maxEnergyDrainable)
    {
      Players.RemoveAll(x => !x);
      if (Players.Count == 0 || maxEnergyDrainable <= 0f) return 0f;

      var maxDrainEitr = maxEnergyDrainable * EitrToEnergyRatio;
      var remainingEitrToDrain = maxDrainEitr;

      List<Player> validPlayers = new(Players.Count);
      foreach (var player in Players)
      {
        if (player.HaveEitr(0.01f))
          validPlayers.Add(player);
      }

      if (validPlayers.Count == 0) return 0f;

      var attempts = 0;
      while (remainingEitrToDrain > 0f && attempts++ < 5)
      {
        var perPlayer = remainingEitrToDrain / validPlayers.Count;
        List<Player> stillValid = new(validPlayers.Count);

        foreach (var player in validPlayers)
        {
          if (player.HaveEitr(perPlayer))
          {
            player.UseEitr(perPlayer);
            remainingEitrToDrain -= perPlayer;
            stillValid.Add(player);
          }
        }

        if (stillValid.Count == 0) break;
        validPlayers = stillValid;
      }

      var totalEnergyGained = (maxDrainEitr - remainingEitrToDrain) / EitrToEnergyRatio;
      return totalEnergyGained;
    }

    // Legacy method, safe to remove when confident in new simulation logic
    public float TryDrainEitrFromPlayers(float deltaTime)
    {
      if (Mode != PowerConduitMode.Drain || Players.Count == 0) return 0f;

      var totalDrainedEitr = 0f;
      foreach (var player in Players)
      {
        if (player == null || !player.HaveEitr(0.01f)) continue;

        var available = player.m_eitr;
        var eitrToDrain = PowerConduitPlateComponent.drainRate * deltaTime;
        var actualDrain = Mathf.Min(eitrToDrain, available);

        player.UseEitr(actualDrain);
        totalDrainedEitr += actualDrain;

        if (totalDrainedEitr >= PowerConduitPlateComponent.drainRate * deltaTime * Players.Count)
          break;
      }

      var fuelGained = totalDrainedEitr / PowerConduitPlateComponent.eitrToFuelRatio;
      return fuelGained * PowerConduitPlateComponent.chargeRate;
    }

    public string GetHoverText()
    {
      var baseString = $"{ModTranslations.PowerConduit_DrainPlate_Name}";

      if (PowerNetworkController.CanShowNetworkData || PrefabConfig.PowerNetwork_ShowAdditionalPowerInformationByDefault.Value)
      {
        var stateText = PowerNetworkController.GetDrainMechanismActivationStatus(IsActive, HasPlayersWithEitr);
        baseString += "\n";
        baseString += $"[{stateText}]";
      }

      return baseString;
    }
  }
}