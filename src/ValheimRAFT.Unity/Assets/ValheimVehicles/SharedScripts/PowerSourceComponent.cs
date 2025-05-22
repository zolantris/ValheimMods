// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerSourceComponent : PowerNodeComponentBase, IPowerSource
  {
    // if it shares a storage on same gameobject/prefab
    [SerializeField] public bool IsStorage = false;

    private static readonly Vector3 powerCoreMaxScale = Vector3.one;
    private static readonly Vector3 powerCoreMinScale = Vector3.one * 0.3f;
    [SerializeField] private float maxOutputWatts = 100f;
    [SerializeField] private float baseFuelCapacity = 100f;
    [SerializeField] private float fuelCapacity = 100f;
    // Fuel units per second
    [SerializeField] public float fuelConsumptionRate = 0.5f;
    // Watts per 1 unit of fuel
    [SerializeField] public float fuelEnergyYield = 1f;
    // Combinator with fuelEnergyYield
    [SerializeField] public float fuelEfficiency = 10f;

    [SerializeField] public static float BaseFuelEfficiency = 1f;
    [SerializeField] public static float CoalFuelEfficiency = 1f;
    [SerializeField] public static float SurtlingCoreFuelEfficiency = 3f;
    [SerializeField] public static float EitrFuelEfficiency = 12f;

    [SerializeField] public bool isRunning = false;

    [SerializeField] public float currentFuel;
    [SerializeField] public Transform powerCoreTransform;
    [SerializeField] public Transform animatedInnerCoreTransform;
    [SerializeField] public Transform animatedOuterCoreTransform;
    [SerializeField] public AnimatedMaterialController animatedOuterCore;
    [SerializeField] public AnimatedMaterialController animatedInnerCore;
    [SerializeField] public FuelType fuelType = FuelType.Eitr;

    public bool hasLoadedInitialData = false;

    private bool _isActive = true;
    // todo may need to turn it off so having it point to another var is good.
    public override bool IsActive => _isActive;

    protected override void Awake()
    {
      base.Awake();

      if (canSelfRegisterToNetwork)
      {
        PowerNetworkController.RegisterPowerComponent(this); // or RegisterNode(this)
      }

      powerCoreTransform = transform.Find("meshes/power_core");

#if DEBUG
      // animatedInnerCoreTransform = transform.Find("meshes/power_core/animated_inner_core");
      // animatedOuterCoreTransform = transform.Find("meshes/power_core/animated_outer_core");
      //
      // if (animatedInnerCoreTransform == null || powerCoreTransform == null)
      // {
      //   LoggerProvider.LogError("Invalid Transforms found. This is an error with the prefab");
      // }
      // if (animatedInnerCoreTransform)
      // {
      //   animatedInnerCore = CreateAnimatedMaterialController(animatedInnerCoreTransform);
      // }
      // if (animatedOuterCoreTransform)
      // {
      //   animatedOuterCore = CreateAnimatedMaterialController(animatedOuterCoreTransform);
      // }
#endif
    }

    protected virtual void Start()
    {
      IsStorage = GetComponent<PowerStorageComponent>() != null;
      UpdateFuelCapacity();
      UpdateFuelEfficiency();
    }

    protected virtual void OnDestroy()
    {
      if (canSelfRegisterToNetwork)
      {
        PowerNetworkController.UnregisterPowerComponent(this);
      }
    }


    private void FixedUpdate()
    {
      UpdatePowerCoreSize();
    }

    public AnimatedMaterialController CreateAnimatedMaterialController(Transform objTransform)
    {
      var current = objTransform.GetComponent<AnimatedMaterialController>();
      if (!current)
      {
        current = objTransform.gameObject.AddComponent<AnimatedMaterialController>();
      }
      current.mainTexTilingBase = Vector2.one * 5;
      current.InitMainTex();
      return current;
    }

    public void UpdatePowerCoreSize()
    {
      powerCoreTransform.localScale = Vector3.Lerp(powerCoreMinScale, powerCoreMaxScale, currentFuel / fuelCapacity);
    }

    public void UpdatePowerCoreAnimations(AnimatedMaterialController animatedPowerCore)
    {
      if (fuelCapacity <= 0f && animatedPowerCore.enabled)
      {
        animatedPowerCore.enabled = false;
      }
      if (fuelCapacity > 0f && !animatedPowerCore.enabled)
      {
        animatedPowerCore.enabled = true;
      }
    }

    public void SetFuelCapacity(float val)
    {
      baseFuelCapacity = val;
      fuelCapacity = IsStorage ? val * 0.5f : val;
    }
    public void SetFuelConsumptionRate(float val)
    {
      fuelConsumptionRate = val;
    }

    private float _lastProducedEnergy = 0f;

    /// <summary>
    /// TODO Update the fuel states base on Data property changes
    /// </summary>
    /// <returns></returns>
    public void UpdateFuelStates(PowerSourceData powerSourceData)
    {

    }

    public float RequestAvailablePower(float deltaTime, float supplyFromSources, float totalDemand, bool isDemanding)
    {
      if (!IsActive)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      var remainingDemand = totalDemand - supplyFromSources;
      if (!isDemanding || remainingDemand <= 0f)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      if (!isRunning)
        SetRunning(true);

      var maxEnergy = maxOutputWatts * deltaTime;
      var energyToProduce = Mathf.Min(remainingDemand, maxEnergy);

      // Limit based on fuel consumption rate
      var maxFuelUsable = fuelConsumptionRate * deltaTime;
      var maxEnergyFromFuel = maxFuelUsable * fuelEnergyYield * fuelEfficiency;

      // Cap energy to available fuel and consumption rate
      energyToProduce = Mathf.Min(energyToProduce, maxEnergyFromFuel);

      var requiredFuel = energyToProduce / (fuelEnergyYield * fuelEfficiency);
      if (currentFuel < requiredFuel)
      {
        SetRunning(false);
        _lastProducedEnergy = 0f;
        return 0f;
      }

      _lastProducedEnergy = energyToProduce;
      return energyToProduce;
    }

    public void CommitEnergyUsed(float energyUsed)
    {
      var requiredFuel = energyUsed / (fuelEnergyYield * fuelEfficiency);
      currentFuel = Mathf.Max(0f, currentFuel - requiredFuel);
    }


    public void UpdateFuelEfficiency()
    {
      fuelEfficiency = GetFuelEfficiency(fuelType);
    }

    public void UpdateFuelCapacity()
    {
      SetFuelCapacity(baseFuelCapacity);
    }

    public static float GetFuelEfficiency(FuelType val)
    {
      return val switch
      {
        FuelType.Coal => BaseFuelEfficiency * CoalFuelEfficiency,
        FuelType.SurtlingCore => BaseFuelEfficiency * SurtlingCoreFuelEfficiency,
        FuelType.Eitr => BaseFuelEfficiency * EitrFuelEfficiency,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public float GetFuelLevel()
    {
      return currentFuel;
    }
    public void SetFuelLevel(float val)
    {
      currentFuel = Mathf.Clamp(val, 0f, fuelCapacity);
    }
    public float GetFuelCapacity()
    {
      return fuelCapacity;
    }
    public bool IsRunning => isRunning;
    public void AddFuel(float amount)
    {
      var space = fuelCapacity - currentFuel;
      var toAdd = Mathf.Min(space, amount);
      currentFuel += toAdd;
    }

    public void SetRunning(bool state)
    {
      isRunning = state;
    }
  }
}