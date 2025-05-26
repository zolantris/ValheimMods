// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using System;
using UnityEngine;
using ValheimVehicles.SharedScripts.PowerSystem.Compute;

#endregion

namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerSourceComponent : PowerNodeComponentBase
  {
    private static readonly Vector3 powerCoreMaxScale = Vector3.one;
    private static readonly Vector3 powerCoreMinScale = Vector3.one * 0.3f;

    [SerializeField] public Transform powerCoreTransform;
    [SerializeField] public Transform powerCoreEnergyCoreInnerTransform;
    [SerializeField] public Transform animatedInnerCoreTransform;
    [SerializeField] public Transform animatedOuterCoreTransform;
    [SerializeField] public AnimatedMaterialController animatedOuterCore;
    [SerializeField] public AnimatedMaterialController animatedInnerCore;
    [SerializeField] public AnimatedMachineComponent powerRotator;
    [SerializeField] public Transform powerRotatorTransform;
    public Vector3 powerRotatorDischargeDirection = Vector3.down;
    public float Fuel => Data.Fuel;
    public float FuelCapacity => Data.FuelCapacity;
    public FuelType fuelType => Data.fuelType;
    public bool isRunning => Data.IsRunning;

    public bool hasLoadedInitialData = false;

    private bool _isActive = true;
    // todo may need to turn it off so having it point to another var is good.
    public override bool IsActive => _isActive;
    public bool IsRunning => isRunning;
    private PowerSourceData m_data = new();
    public virtual PowerSourceData Data => m_data;

    public void SetData(PowerSourceData data)
    {
      m_data = data;
    }

    protected override void Awake()
    {
      base.Awake();

#if UNITY_EDITOR
      // todo make a simplified unity method for registering and testing these consumers with our PowerManager.
      //
      // if (canSelfRegisterToNetwork)
      // {
      //   PowerNetworkController.RegisterPowerComponent(this); // or RegisterNode(this)
      // }
#endif

      powerRotatorTransform = transform.Find("meshes/power_rotator");
      if (!powerRotatorTransform)
      {
        throw new Exception("PowerStorageComponent: PowerRotatorTransform not found");
      }
      var rotator = powerRotatorTransform.GetComponent<AnimatedMachineComponent>();
      if (!rotator)
      {
        rotator = powerRotatorTransform.gameObject.AddComponent<AnimatedMachineComponent>();
      }
      rotator.HasRotation = true;
      powerRotator = rotator;


      //power core
      powerCoreTransform = transform.Find("meshes/power_core");
      powerCoreEnergyCoreInnerTransform = transform.Find("meshes/power_core/energy_core_inner");
      animatedInnerCoreTransform = transform.Find("meshes/power_core/animated_inner_core");
      animatedOuterCoreTransform = transform.Find("meshes/power_core/animated_outer_core");

      // disabled, their shaders are not looking great in valheim
      animatedInnerCoreTransform.gameObject.SetActive(false);
      animatedOuterCoreTransform.gameObject.SetActive(false);

      // looks better in valheim than the animated ones.
      powerCoreEnergyCoreInnerTransform.gameObject.SetActive(true);
#if DEBUG
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
    }

    protected virtual void OnDestroy()
    {
    }


    public void FixedUpdate()
    {
      if (!Data.IsValid) return;
      UpdatePowerCoreSize();
      UpdatePowerRotationAnimations();
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
      if (!Data.IsValid) return;
      powerCoreTransform.localScale = Vector3.Lerp(powerCoreMinScale, powerCoreMaxScale, Fuel / FuelCapacity);
    }

    public void UpdatePowerCoreAnimations(AnimatedMaterialController animatedPowerCore)
    {
      if (!Data.IsValid) return;
      if (FuelCapacity <= 0f && animatedPowerCore.enabled)
      {
        animatedPowerCore.enabled = false;
      }
      if (FuelCapacity > 0f && !animatedPowerCore.enabled)
      {
        animatedPowerCore.enabled = true;
      }
    }

    /// <summary>
    /// To be run in a network manager or directly in the setter as a mutation
    /// </summary>
    public void UpdatePowerRotationAnimations()
    {
      if (!Data.IsValid) return;
      // disable when at 0 or at capacity
      if ((!IsActive || !isRunning) && powerRotator.enabled)
      {
        powerRotator.enabled = false;
        return;
      }

      if (isRunning && !powerRotator.enabled)
      {
        powerRotator.enabled = true;
      }

      if (powerRotator.enabled && isRunning)
      {
        powerRotator.RotationalVector = powerRotatorDischargeDirection;
      }
    }
  }
}