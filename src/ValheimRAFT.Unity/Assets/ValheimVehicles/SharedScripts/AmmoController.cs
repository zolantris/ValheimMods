#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;
#if VALHEIM
  using ValheimVehicles.Components;
#endif

#endregion

#if VALHEIM
  using System.Linq;
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
      private CoroutineHandle _containerFindRoutine = null!;
      private CoroutineHandle _ammoUpdateRoutine = null!;
      public static bool HasUnlimitedAmmo = false;

#if VALHEIM
      private HashSet<Container> _nearbyContainers = new();
#endif

      [Header("Distances")]
      public static float MaxContainerDistance = 50f;
      public static float MaxContainerSearchRadius => MaxContainerDistance / 2f;

      [Header("Ammo")]
      [Tooltip("Amount of ammo in a TargetController area")]
      [SerializeField] private int _explosiveAmmo;
      [SerializeField] private int _solidAmmo;
      [SerializeField] public bool IsHandheld;
      [SerializeField] private bool _canTriggerChangeUpdate = true;

#if VALHEIM
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

      public void SetAmmoByVariant(CannonballVariant variant, int amount)
      {
        if (variant == CannonballVariant.Solid)
        {
          _solidAmmo = amount;
        }
        if (variant == CannonballVariant.Explosive)
        {
          _explosiveAmmo = amount;
        }
      }

      public static int UpdateConsumedAmmo(CannonballVariant variant, int requestAmount, ref int ammoSolidUsage, ref int ammoExplosiveUsage, int totalSolid, int totalExplosive)
      {
        var delta = 0;
        if (variant == CannonballVariant.Solid)
        {
          var remaining = totalSolid - ammoSolidUsage;
          delta = Mathf.Max(0, Mathf.Min(requestAmount, remaining));
          ammoSolidUsage += delta;
          return delta;
        }
        if (variant == CannonballVariant.Explosive)
        {
          var remaining = totalExplosive - ammoExplosiveUsage;
          delta = Mathf.Max(0, Mathf.Min(requestAmount, remaining));
          ammoExplosiveUsage += delta;
          return delta;
        }

        LoggerProvider.LogWarning($"Unexpected variant {variant}. Could not subtract ammo.");
        return delta;
      }

      private void Awake()
      {
        IsPiecesController = PrefabNames.IsVehiclePiecesContainer(transform.root.name);
        IsHandheld = transform.name.Contains("handheld");
        InitCoroutines();
      }

      private void InitCoroutines()
      {
        _ammoUpdateRoutine ??= new CoroutineHandle(this);
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
        InitCoroutines();

#if VALHEIM
        ValheimContainerTracker.OnContainerAddSubscriptions += TryAddNearbyContainer;
#endif
      }


      private void OnDisable()
      {
        // bail on handheld.
        if (IsHandheld) return;
        Instances.Remove(this);
#if VALHEIM
        ValheimContainerTracker.OnContainerAddSubscriptions -= TryAddNearbyContainer;
#endif
      }

      private void Start()
      {
        StartNearbyContainers();

        InvokeRepeating(nameof(StartNearbyContainers), 5f, 15f);
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
          if (distance > MaxContainerDistance)
          {
            continue;
          }
          containerAmmoController.StartNearbyContainers();
        }
      }

      public void StartNearbyContainers()
      {
        if (IsHandheld)
        {
          _ammoUpdateRoutine.Start(UpdateAvailableAmmoTypes());
          return;
        }
        if (_containerFindRoutine.IsRunning)
        {
          return;
        }
        _containerFindRoutine.Start(UpdateNearbyContainers());
      }


#if VALHEIM
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

      public static CannonballVariant GetAmmoVariantFromToken(string tokenId)
      {
        if (tokenId == SolidAmmoToken)
        {
          return CannonballVariant.Solid;
        }
        if (tokenId == ExplosiveAmmoToken)
        {
          return CannonballVariant.Explosive;
        }

        LoggerProvider.LogWarning($"Unexpected token {tokenId}");
        return CannonballVariant.Solid;
      }

      private IEnumerator UpdateNearbyContainers()
      {
#if VALHEIM

        yield return new WaitForFixedUpdate();
        // copy it in case modification can happen.
        var containersCopy = ValheimContainerTracker.ActiveContainers.ToList();

        foreach (var container in containersCopy)
        {
          TryAddNearbyContainer(container);
        }
#endif
        yield return null;
        yield return UpdateAvailableAmmoTypes();
      }

#if VALHEIM
      // public void UpdateAmmoFromInventory(Inventory inventory, List<ItemDrop.ItemData>? ammoTypes, ref int currentExplosiveAmmo, ref int currentSolidAmmo, bool shouldRemove)
      // {
      //   if (ammoTypes == null) return;
      //   foreach (var ammoType in ammoTypes)
      //   {
      //     if (ammoType.m_shared.m_name == ExplosiveAmmoToken)
      //     {
      //       if (shouldRemove)
      //       {
      //         ValheimInventoryCompat.RemoveItemWithRemainder(inventory, ExplosiveAmmoToken, currentSolidAmmo, out var remainder);
      //       }
      //       else
      //       {
      //         currentExplosiveAmmo += ammoType.m_stack;
      //       }
      //       continue;
      //     }
      //     if (ammoType.m_shared.m_name == SolidAmmoToken)
      //     {
      //       if (shouldRemove)
      //       {
      //         ValheimInventoryCompat.RemoveItemWithRemainder(inventory, SolidAmmoToken, currentSolidAmmo, out var remainder);
      //       }
      //       else
      //       {
      //         currentSolidAmmo += ammoType.m_stack;
      //       }
      //       continue;
      //     }
      //     LoggerProvider.LogDebug($"found other ammo {ammoType.m_shared.m_name}");
      //   }
      // }


      private void UpdatePartialRemainingAmmo()
      {
        var localQueue = _queuedInventoryUpdates.ToArray();
        _queuedInventoryUpdates.Clear();

        foreach (var queuedInventoryUpdate in localQueue)
        {
          UpdateAvailableFromInventory(queuedInventoryUpdate, ref _explosiveAmmo, ref _solidAmmo, true);
        }

        _solidAmmo = Math.Max(0, _solidAmmo);
        _explosiveAmmo = Math.Max(0, _explosiveAmmo);
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

        InventorySnapshotData[inventory] = new AmmoInventoryData { explosiveAmmo = localExplosiveAmmo, solidAmmo = localSolidAmmo };
      }
#endif
      private IEnumerator UpdateAvailableAmmoTypes()
      {
        if (HasUnlimitedAmmo)
        {
          _explosiveAmmo = 999;
          _solidAmmo = 999;
        }

#if VALHEIM
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
          if (player == null || player.IsDead() || player.IsTeleporting()) yield break;
          var inventory = player.GetInventory();
          if (inventory == null) yield break;
          currentSolidAmmo += inventory.CountItems(SolidAmmoToken);
          currentExplosiveAmmo += inventory.CountItems(ExplosiveAmmoToken);
        }

        _explosiveAmmo = currentExplosiveAmmo;
        _solidAmmo = currentSolidAmmo;
#endif
        yield return null;
      }

      private void RemoveAndUpdateAmmoTypes(int ammoToRemoveSolid, int ammoToRemoveExplosive)
      {
        if (HasUnlimitedAmmo)
        {
          _explosiveAmmo = 999;
          _solidAmmo = 999;
          return;
        }

        if (ammoToRemoveExplosive <= 0 && ammoToRemoveSolid <= 0) return;

#if VALHEIM
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
            if (currentAmmoToRemoveExplosive <= 0 && currentAmmoToRemoveSolid <= 0) break;
            var inventory = nearbyContainer.GetInventory();
            if (inventory == null) continue;
            var localSolidAmmo = inventory.CountItems(SolidAmmoToken);
            var localExplosiveAmmo = inventory.CountItems(ExplosiveAmmoToken);

            if (currentAmmoToRemoveSolid > 0 && localSolidAmmo > 0)
            {
              var amountToRemove = Math.Max(0, Math.Min(currentAmmoToRemoveSolid, localSolidAmmo));
              ValheimInventoryCompat.RemoveItemWithRemainder(inventory, SolidAmmoToken, amountToRemove, out var remainder);
              amountToRemove -= remainder;
              localSolidAmmo -= amountToRemove;
              currentAmmoToRemoveSolid -= amountToRemove;
            }

            if (currentAmmoToRemoveExplosive > 0 && localExplosiveAmmo > 0)
            {
              var amountToRemove = Math.Max(0, Math.Min(currentAmmoToRemoveExplosive, localExplosiveAmmo));
              ValheimInventoryCompat.RemoveItemWithRemainder(inventory, ExplosiveAmmoToken, amountToRemove, out var remainder);
              amountToRemove -= remainder;

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
            ValheimInventoryCompat.RemoveItemWithRemainder(inventory, SolidAmmoToken, amountToRemove, out var remainder);
            amountToRemove -= remainder;
            localSolidAmmo -= amountToRemove;
            currentAmmoToRemoveSolid -= amountToRemove;
          }

          if (currentAmmoToRemoveExplosive > 0 && localExplosiveAmmo > 0)
          {
            var amountToRemove = Math.Min(currentAmmoToRemoveExplosive, localExplosiveAmmo);
            ValheimInventoryCompat.RemoveItemWithRemainder(inventory, ExplosiveAmmoToken, amountToRemove, out var remainder);
            amountToRemove -= remainder;
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

        _explosiveAmmo = Math.Max(0, currentExplosiveAmmo);
        _solidAmmo = Math.Max(0, currentSolidAmmo);
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
            RemoveAndUpdateAmmoTypes(delta, 0);
            break;
          case CannonballVariant.Explosive:
            RemoveAndUpdateAmmoTypes(0, delta);
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

      private bool _hasAction;

#if VALHEIM
      /// <summary>
      /// To be called whenever a container that we watch updates.
      /// </summary>
      private void OnContainerChanged(Container container)
      {
        if (!_canTriggerChangeUpdate) return;
        _queuedInventoryUpdates.Add(container);
        if (_hasAction)
        {
          CancelInvoke(nameof(UpdatePartialRemainingAmmo));
          _hasAction = false;
        }
        if (gameObject.activeInHierarchy)
        {
          Invoke(nameof(UpdatePartialRemainingAmmo), 5f);
          _hasAction = true;
        }
      }
#endif
    }
  }