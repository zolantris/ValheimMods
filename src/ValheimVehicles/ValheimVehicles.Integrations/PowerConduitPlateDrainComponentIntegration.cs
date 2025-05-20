using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Helpers;
using ValheimVehicles.Integrations;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Helpers;
using ValheimVehicles.SharedScripts.PowerSystem;
using ValheimVehicles.SharedScripts.PowerSystem.Interfaces;

public class PowerConduitPlateDrainComponentIntegration : PowerConduitPlateComponentIntegration
{
  protected override void Start()
  {
    base.Start();
    Logic.mode = PowerConduitMode.Drain;
  }
}