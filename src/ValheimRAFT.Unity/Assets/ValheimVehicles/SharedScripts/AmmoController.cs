using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StructLinq;
using UnityEngine;
using ValheimVehicles.ModSupport;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts
{
  /// <summary>
  /// ContainerAmmoController is meant to be run with TargetController
  /// </summary>
  [RequireComponent(typeof(TargetController))]
  public class AmmoController : MonoBehaviour
  {
    private static readonly HashSet<AmmoController> Instances = new();
    private CoroutineHandle _containerFindRoutine;
    private CoroutineHandle _ammoUpdateRoutine;
    public static bool HasUnlimitedAmmo = false;

#if !UNITY_2022 && !UNITY_EDITOR
    private List<Container> _nearbyContainers = new();
#endif

    [Header("Distances")]
    public static float MaxResetDistance = 50f;
    public static float MaxContainerSearchRadius => MaxResetDistance / 2f;

    [Header("Ammo")]
    [Tooltip("Amount of ammo in a TargetController area")]
    [SerializeField] private int _ammo = 0;
    public int Ammo => _ammo;

    public bool isPiecesController = false;

    private void Awake()
    {
      isPiecesController = PrefabNames.IsVehiclePiecesContainer(transform.root.name);
    }

    private void InitCoroutines()
    {
      _containerFindRoutine ??= new CoroutineHandle(this);
    }

    private void OnTransformParentChanged()
    {
      isPiecesController = PrefabNames.IsVehiclePiecesContainer(transform.root.name);
    }

    private void OnEnable()
    {
      Instances.Add(this);
      ValheimContainerTracker.OnContainerAddSubscriptions += TryAddOrRemoveNearbyContainer;
    }


    private void OnDisable()
    {
      Instances.Remove(this);
      ValheimContainerTracker.OnContainerAddSubscriptions -= TryAddOrRemoveNearbyContainer;
    }

    /// <summary>
    /// To be called when placing a Container piece nearby active cannon areas.
    /// </summary>
    /// <param name="position"></param>
    private static void ForceUpdateAllNearbyInstances(Vector3 position)
    {
      Instances.RemoveWhere(x => x == null);
      foreach (var containerAmmoController in Instances)
      {
        if (containerAmmoController == null) continue;
        var distance = Vector3.Distance(containerAmmoController.transform.position, position);
        // Only trigger an update on containers if nearby.
        if (distance > MaxResetDistance)
        {
          continue;
        }
        containerAmmoController.StartNearbyContainers();
      }
    }

    public void StartNearbyContainers()
    {
      if (_containerFindRoutine.IsRunning)
      {
        _containerFindRoutine.Stop();
      }
      _containerFindRoutine.Start(UpdateNearbyContainers());
    }


    public void TryAddOrRemoveNearbyContainer(Container container)
    {
#if !UNITY_EDITOR && !UNITY_2022
      if (container == null) return;
      if (!container.isActiveAndEnabled) return;
      // do not include containers outside a vehicle if the vehicle is nearby other container sources. This would effectively steal those items.
      if (isPiecesController && !PrefabNames.IsVehiclePiecesContainer(container.transform.root.name))
      {
        return;
      }

      var distance = Vector3.Distance(container.transform.position, transform.position);
      if (distance > MaxContainerSearchRadius)
      {
        return;
      }

      _nearbyContainers.Add(container);
#endif
    }

    private IEnumerator UpdateNearbyContainers()
    {
#if !UNITY_EDITOR && !UNITY_2022

      // copy it in case modification can happen.
      var containersCopy = ValheimContainerTracker.ActiveContainers.ToList();

      foreach (var container in containersCopy)
      {
        TryAddOrRemoveNearbyContainer(container);
      }
      yield return null;
#endif
    }

    private int GetAvailableAmmo()
    {
      if (HasUnlimitedAmmo)
      {
        return 999;
      }

#if !UNITY_EDITOR && !UNITY_2022
      var ammoCount = 0;

      foreach (var nearbyContainer in _nearbyContainers)
      {
        var explosiveAmmo = nearbyContainer.m_inventory.GetAmmoItem("$m");
        var solidAmmo = nearbyContainer.m_inventory.GetAmmoItem("$m");
      }
#endif

      return _ammo;
    }

    private void OnAmmoUpdate(float deltaAmmo)
    {
      foreach (var container in _nearbyContainers)
      {
      }
    }
  }
}