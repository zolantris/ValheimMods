// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerSourceComponent : PowerNodeComponentBase
  {

    private static readonly Vector3 powerCoreMaxScale = Vector3.one;
    private static readonly Vector3 powerCoreMinScale = Vector3.one * 0.3f;
    [SerializeField] private float maxOutputWatts = 100f;
    [SerializeField] private float fuelCapacity = 100f;
    [SerializeField] private float fuelConsumptionRate = 1f; // per second
    [SerializeField] public bool isRunning = true;

    [SerializeField] private float currentFuel;
    [SerializeField] public Transform powerCoreTransform;
    [SerializeField] public Transform animatedInnerCoreTransform;
    [SerializeField] public Transform animatedOuterCoreTransform;
    [SerializeField] public AnimatedMaterialController animatedOuterCore;
    [SerializeField] public AnimatedMaterialController animatedInnerCore;

    public override bool IsActive => isRunning && currentFuel > 0f;

    protected override void Awake()
    {
      base.Awake();
      
      powerCoreTransform = transform.Find("meshes/power_core");
      animatedInnerCoreTransform = transform.Find("meshes/power_core/animated_inner_core");
      animatedOuterCoreTransform = transform.Find("meshes/power_core/animated_outer_core");
      animatedOuterCore = CreateAnimatedMaterialController(animatedOuterCoreTransform);
      animatedInnerCore = CreateAnimatedMaterialController(animatedInnerCoreTransform);
    }

    private void FixedUpdate()
    {
      UpdatePowerCoreSize();
      UpdatePowerCoreAnimations(animatedInnerCore);
      UpdatePowerCoreAnimations(animatedOuterCore);
      
      if (!isRunning || currentFuel <= 0f) return;

      float fuelUsed = fuelConsumptionRate * Time.fixedDeltaTime;
      currentFuel = Mathf.Max(0f, currentFuel - fuelUsed);
    }

    public AnimatedMaterialController CreateAnimatedMaterialController(Transform objTransform)
    {
      var current = objTransform.GetComponent<AnimatedMaterialController>();
      if (!current)
      {
        current = objTransform.gameObject.AddComponent<AnimatedMaterialController>();
      }
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

    public float RequestAvailablePower(float deltaTime)
    {
      return IsActive ? maxOutputWatts * deltaTime : 0f;
    }

    public float GetFuelLevel() => currentFuel;
    public float GetFuelCapacity() => fuelCapacity;

    public float Refuel(float amount)
    {
      var space = fuelCapacity - currentFuel;
      var toAdd = Mathf.Min(space, amount);
      currentFuel += toAdd;
      return toAdd;
    }

    public void SetRunning(bool state) => isRunning = state;
  }
}