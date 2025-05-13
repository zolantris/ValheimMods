// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using ValheimVehicles.Interfaces;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerSourceComponent : PowerNodeComponentBase, IPowerSource
  {

    private static readonly Vector3 powerCoreMaxScale = Vector3.one;
    private static readonly Vector3 powerCoreMinScale = Vector3.one * 0.3f;
    [SerializeField] private float maxOutputWatts = 100f;
    [SerializeField] private float fuelCapacity = 100f;
    [SerializeField] private float fuelConsumptionRate = 1f; // per second
    [SerializeField] public bool isRunning = true;

    [SerializeField] public float currentFuel;
    [SerializeField] public Transform powerCoreTransform;
    [SerializeField] public Transform animatedInnerCoreTransform;
    [SerializeField] public Transform animatedOuterCoreTransform;
    [SerializeField] public AnimatedMaterialController animatedOuterCore;
    [SerializeField] public AnimatedMaterialController animatedInnerCore;
    public bool hasLoadedInitialData = false;

    public override bool IsActive => isRunning && currentFuel > 0f;

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

      // animatedOuterCoreTransform == null
      // if (animatedInnerCoreTransform == null || powerCoreTransform == null)
      // {
      //   LoggerProvider.LogError("Invalid Transforms found. This is an error with the prefab");
      // }
      // // animatedOuterCore = CreateAnimatedMaterialController(animatedOuterCoreTransform);
      // animatedInnerCore = CreateAnimatedMaterialController(animatedInnerCoreTransform);
#endif
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
      // UpdatePowerCoreAnimations(animatedInnerCore);
      // UpdatePowerCoreAnimations(animatedOuterCore);

      if (!isRunning || currentFuel <= 0f) return;

      var fuelUsed = fuelConsumptionRate * Time.fixedDeltaTime;
      currentFuel = Mathf.Max(0f, currentFuel - fuelUsed);
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

    public float RequestAvailablePower(float deltaTime, bool isDemanding)
    {
      if (!IsActive || !isDemanding) return 0f;

      var fuelNeeded = fuelConsumptionRate * deltaTime;
      if (currentFuel < fuelNeeded)
      {
        isRunning = false;
        return 0f;
      }

      currentFuel -= fuelNeeded;
      return maxOutputWatts * deltaTime;
    }

    public float GetFuelLevel()
    {
      return currentFuel;
    }
    public float GetFuelCapacity()
    {
      return fuelCapacity;
    }
    public bool IsRunning => isRunning;
    public void Refuel(float amount)
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