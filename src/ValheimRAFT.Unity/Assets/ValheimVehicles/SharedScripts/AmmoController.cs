#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

#if !UNITY_2022 && !UNITY_EDITOR
using System.Linq;
using StructLinq;
using ValheimVehicles.ModSupport;
#endif
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
    [SerializeField] private int _explosiveAmmo;
    [SerializeField] private int _solidAmmo;
    [SerializeField] public bool IsHandheld;
    [SerializeField] private bool _canTriggerChangeUpdate = true;

#if !UNITY_2022 && !UNITY_EDITOR
    private Dictionary<Inventory, AmmoInventoryData> InventorySnapshotData = new();
    public HashSet<Container> _queuedInventoryUpdates = new();
#endif
    public struct AmmoInventoryData
    {
      public int explosiveAmmo;
      public int solidAmmo;
    }


    public int SolidAmmo => _solidAmmo;
    public int ExplosiveAmmo => _explosiveAmmo;

    public bool IsPiecesController;

    private void Awake()
    {
      IsPiecesController = PrefabNames.IsVehiclePiecesContainer(transform.root.name);
      IsHandheld = transform.name.Contains("handheld");
    }

    private void InitCoroutines()
    {
      _containerFindRoutine ??= new CoroutineHandle(this);
    }

    private void OnTransformParentChanged()
    {
      IsPiecesController = PrefabNames.IsVehiclePiecesContainer(transform.root.name);
    }

    private void OnEnable()
    {
      // bail on handheld.
      if (IsHandheld) return;
      Instances.Add(this);
      
#if !UNITY_2022 && !UNITY_EDITOR
      ValheimContainerTracker.OnContainerAddSubscriptions += TryAddNearbyContainer;
#endif
    }


    private void OnDisable()
    {
      // bail on handheld.
      if (IsHandheld) return;
      Instances.Remove(this);
#if !UNITY_2022 && !UNITY_EDITOR
      ValheimContainerTracker.OnContainerAddSubscriptions -= TryAddNearbyContainer;
#endif
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
      if (IsHandheld) return;
      if (_containerFindRoutine.IsRunning)
      {
        _containerFindRoutine.Stop();
      }
      _containerFindRoutine.Start(UpdateNearbyContainers());
    }


#if !UNITY_EDITOR && !UNITY_2022
    public void TryAddNearbyContainer(Container container)
    {
      if (container == null) return;
      if (!container.isActiveAndEnabled) return;
      if (container.m_inventory == null) return;

      // do not include containers outside a vehicle if the vehicle is nearby other container sources. This would effectively steal those items.
      if (IsPiecesController && !PrefabNames.IsVehiclePiecesContainer(container.transform.root.name))
      {
        return;
      }

      var distance = Vector3.Distance(container.transform.position, transform.position);
      if (distance > MaxContainerSearchRadius)
      {
        return;
      }


      _nearbyContainers.Add(container);
      container.m_inventory.m_onChanged += () => OnContainerChanged(container);
    }
#endif

    private const string ExplosiveAmmoToken = "$valheim_vehicles_cannonball_explosive";
    private const string SolidAmmoToken = "$valheim_vehicles_cannonball_solid";

    private IEnumerator UpdateNearbyContainers()
    {
#if !UNITY_EDITOR && !UNITY_2022

      // copy it in case modification can happen.
      var containersCopy = ValheimContainerTracker.ActiveContainers.ToList();

      foreach (var container in containersCopy)
      {
        TryAddNearbyContainer(container);
      }
#endif
      yield return null;
    }

#if !UNITY_EDITOR && !UNITY_2022
    public void UpdateAmmoFromInventory(Inventory inventory, List<ItemDrop.ItemData>? ammoTypes, ref int currentExplosiveAmmo, ref int currentSolidAmmo, bool shouldRemove)
    {
      if (ammoTypes == null) return;
      foreach (var ammoType in ammoTypes)
      {
        if (ammoType.m_shared.m_name == ExplosiveAmmoToken)
        {
          if (shouldRemove)
          {
            inventory.RemoveItem(ExplosiveAmmoToken, currentExplosiveAmmo);
          }
          else
          {
            currentExplosiveAmmo += ammoType.m_stack;
          }
          continue;
        }
        if (ammoType.m_shared.m_name == SolidAmmoToken)
        {
          if (shouldRemove)
          {
            inventory.RemoveItem(SolidAmmoToken, currentSolidAmmo);
          }
          else
          {
            currentSolidAmmo += ammoType.m_stack;
          }
          continue;
        }
        LoggerProvider.LogDebug($"found other ammo {ammoType.m_shared.m_name}");
      }
    }


    private void UpdatePartialRemainingAmmo()
    {
      var localQueue = _queuedInventoryUpdates.ToArray();
      _queuedInventoryUpdates.Clear();

      foreach (var queuedInventoryUpdate in localQueue)
      {
        UpdateAvailableFromInventory(queuedInventoryUpdate, ref _explosiveAmmo, ref _solidAmmo, true);
      }

      _solidAmmo = Math.Min(0, _solidAmmo);
      _explosiveAmmo = Math.Min(0, _explosiveAmmo);
    }

    private void UpdateAvailableFromInventory(Container nearbyContainer, ref int currentSolidAmmo, ref int currentExplosiveAmmo, bool isDiffUpdate)
    {
      if (nearbyContainer == null) return;
      if (!nearbyContainer.isActiveAndEnabled) return;
      var inventory = nearbyContainer.GetInventory();
      if (inventory == null) return;
      var localSolidAmmo = inventory.CountItems(SolidAmmoToken);
      var localExplosiveAmmo = inventory.CountItems(ExplosiveAmmoToken);

      if (isDiffUpdate)
      {
        if (InventorySnapshotData.TryGetValue(inventory, out var snapshotData))
        {
          currentSolidAmmo -= snapshotData.explosiveAmmo;
          currentExplosiveAmmo -= snapshotData.solidAmmo;
        }
      }

      currentSolidAmmo += localSolidAmmo;
      currentExplosiveAmmo += localExplosiveAmmo;

      InventorySnapshotData[inventory] = new AmmoInventoryData { explosiveAmmo = currentExplosiveAmmo, solidAmmo = currentSolidAmmo };
    }
#endif
    private void UpdateAvailableAmmoTypes()
    {
      if (HasUnlimitedAmmo)
      {
        _explosiveAmmo = 999;
        _solidAmmo = 999;
      }

#if !UNITY_EDITOR && !UNITY_2022
      var currentExplosiveAmmo = 0;
      var currentSolidAmmo = 0;

      if (!IsHandheld)
      {
        foreach (var nearbyContainer in _nearbyContainers)
        {
          UpdateAvailableFromInventory(nearbyContainer, ref currentSolidAmmo, ref currentExplosiveAmmo, false);
        }
      }
      else
      {
        var player = GetComponentInParent<Player>();
        if (player == null || player.IsDead() || player.IsTeleporting()) return;
        var inventory = player.GetInventory();
        if (inventory == null) return;
        currentSolidAmmo += inventory.CountItems(SolidAmmoToken);
        currentExplosiveAmmo += inventory.CountItems(ExplosiveAmmoToken);
      }

      _explosiveAmmo = currentExplosiveAmmo;
      _solidAmmo = currentSolidAmmo;
#endif
    }

    private void RemoveAndUpdateAmmoTypes(int ammoToRemoveSolid, int ammoToRemoveExplosive)
    {
      if (HasUnlimitedAmmo)
      {
        _explosiveAmmo = 999;
        _solidAmmo = 999;
      }

#if !UNITY_EDITOR && !UNITY_2022
      var currentAmmoToRemoveSolid = ammoToRemoveSolid;
      var currentAmmoToRemoveExplosive = ammoToRemoveExplosive;

      var currentSolidAmmo = 0;
      var currentExplosiveAmmo = 0;

      if (!IsHandheld)
      {
        foreach (var nearbyContainer in _nearbyContainers)
        {
          if (nearbyContainer == null) continue;
          if (!nearbyContainer.isActiveAndEnabled) continue;
          var inventory = nearbyContainer.GetInventory();
          if (inventory == null) continue;
          var localSolidAmmo = inventory.CountItems(SolidAmmoToken);
          var localExplosiveAmmo = inventory.CountItems(ExplosiveAmmoToken);

          if (currentAmmoToRemoveSolid > 0 && localSolidAmmo > 0)
          {
            var amountToRemove = Math.Min(currentAmmoToRemoveSolid, localSolidAmmo);
            inventory.RemoveItem(SolidAmmoToken, amountToRemove);
            localSolidAmmo -= amountToRemove;
            currentAmmoToRemoveSolid -= amountToRemove;
          }

          if (currentAmmoToRemoveExplosive > 0 && localExplosiveAmmo > 0)
          {
            var amountToRemove = Math.Min(currentAmmoToRemoveExplosive, localExplosiveAmmo);
            inventory.RemoveItem(ExplosiveAmmoToken, amountToRemove);
            localExplosiveAmmo -= amountToRemove;
            currentAmmoToRemoveExplosive -= amountToRemove;
          }

          currentSolidAmmo += localSolidAmmo;
          currentExplosiveAmmo += localExplosiveAmmo;
        }
      }
      else
      {
        var player = GetComponentInParent<Player>();
        if (player == null || player.IsDead() || player.IsTeleporting()) return;
        var inventory = player.GetInventory();
        if (inventory == null) return;
        var localSolidAmmo = inventory.CountItems(SolidAmmoToken);
        var localExplosiveAmmo = inventory.CountItems(ExplosiveAmmoToken);

        if (currentAmmoToRemoveSolid > 0 && localSolidAmmo > 0)
        {
          var amountToRemove = Math.Min(currentAmmoToRemoveSolid, localSolidAmmo);
          inventory.RemoveItem(SolidAmmoToken, amountToRemove);
          localSolidAmmo -= amountToRemove;
          currentAmmoToRemoveSolid -= amountToRemove;
        }

        if (currentAmmoToRemoveExplosive > 0 && localExplosiveAmmo > 0)
        {
          var amountToRemove = Math.Min(currentAmmoToRemoveExplosive, localExplosiveAmmo);
          inventory.RemoveItem(ExplosiveAmmoToken, amountToRemove);
          localExplosiveAmmo -= amountToRemove;
          currentAmmoToRemoveExplosive -= amountToRemove;
        }

        currentSolidAmmo += localSolidAmmo;
        currentExplosiveAmmo += localExplosiveAmmo;
      }

      if (currentAmmoToRemoveSolid > 0)
      {
        LoggerProvider.LogWarning($"Unexpectedly could not remove all ammo requested {currentExplosiveAmmo}.");
      }

      if (currentAmmoToRemoveExplosive > 0)
      {
        LoggerProvider.LogWarning($"Unexpectedly could not remove all ammo requested {currentExplosiveAmmo}.");
      }

      _explosiveAmmo = currentExplosiveAmmo;
      _solidAmmo = currentSolidAmmo;
#endif
    }

    public int GetAmmoAmountFromCannonballVariant(CannonballVariant variant)
    {
      return variant == CannonballVariant.Solid ? _solidAmmo : _explosiveAmmo;
    }

    public void OnAmmoChangedFromVariant(CannonballVariant variant, int delta)
    {
      switch (variant)
      {
        case CannonballVariant.Solid:
          _solidAmmo = Math.Min(0, _solidAmmo - delta);
          break;
        case CannonballVariant.Explosive:
          _explosiveAmmo = Math.Min(0, _explosiveAmmo - delta);
          break;
      }
    }

    /// <summary>
    /// To be called by TargetController or after the player fires with HandheldCannon.
    /// </summary>
    public void OnAmmoChanged(int ammoToRemoveSolid, int ammoToRemoveExplosive)
    {
      // guards against self triggering of OnContainerChanged when RemoveItem is called.
      _canTriggerChangeUpdate = false;
      RemoveAndUpdateAmmoTypes(ammoToRemoveSolid, ammoToRemoveExplosive);
      _canTriggerChangeUpdate = true;
    }

  #if !UNITY_EDITOR && !UNITY_2022
    /// <summary>
    /// To be called whenever a container that we watch updates.
    /// </summary>
    public void OnContainerChanged(Container container)
    {
      if (!_canTriggerChangeUpdate) return;
      _queuedInventoryUpdates.Add(container);
      CancelInvoke(nameof(UpdatePartialRemainingAmmo));
      Invoke(nameof(UpdatePartialRemainingAmmo), 5f);
    }
#endif
  }
}