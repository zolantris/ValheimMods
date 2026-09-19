using Zolantris.Shared;

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
using ValheimVehicles.Controllers;
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

      // Do not include containers outside a vehicle if the vehicle is nearby
      // other container sources. This would effectively steal those items.
      if (IsPiecesController &&
          !PrefabNames.IsVehiclePiecesContainer(container.transform.root.name))
      {
        return;
      }

      if (!IsPiecesController)
      {
        var distance =
          Vector3.Distance(container.transform.position, transform.position);

        if (distance > MaxContainerSearchRadius)
        {
          return;
        }
      }

      // Only subscribe the first time this container is added.
      if (!_nearbyContainers.Add(container))
      {
        return;
      }

      container.m_inventory.m_onChanged += () => OnContainerChanged(container);
    }
#endif

    public static CannonballVariant GetAmmoVariantFromToken(string tokenId)
    {
      if (tokenId == PrefabItemNameToken.CannonSolidAmmo)
      {
        return CannonballVariant.Solid;
      }
      if (tokenId == PrefabItemNameToken.CannonExplosiveAmmo)
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

      var pieceController = VehiclePiecesController.GetVehiclePiecesController(gameObject);

      if (pieceController != null)
      {
        var containersOnShip = pieceController.GetComponentsInChildren<Container>();
        if (containersOnShip != null)
        {
          foreach (var container in containersOnShip)
          {
            TryAddNearbyContainer(container);
          }
        }
      }
      else
      {
        foreach (var container in containersCopy)
        {
          TryAddNearbyContainer(container);
        }
      }
#endif
      yield return null;
      yield return UpdateAvailableAmmoTypes();
    }

#if VALHEIM

    private void UpdatePartialRemainingAmmo()
    {
      _hasAction = false;

      var localQueue = _queuedInventoryUpdates.ToArray();
      _queuedInventoryUpdates.Clear();

      foreach (var queuedInventoryUpdate in localQueue)
      {
        UpdateAvailableFromInventory(
          queuedInventoryUpdate,
          ref _solidAmmo,
          ref _explosiveAmmo,
          true);
      }

      _solidAmmo = Math.Max(0, _solidAmmo);
      _explosiveAmmo = Math.Max(0, _explosiveAmmo);
    }

    private void UpdateAvailableFromInventory(
      Container nearbyContainer,
      ref int currentSolidAmmo,
      ref int currentExplosiveAmmo,
      bool isDiffUpdate)
    {
      if (nearbyContainer == null) return;
      if (!nearbyContainer.isActiveAndEnabled) return;

      var inventory = nearbyContainer.GetInventory();
      if (inventory == null) return;

      var localSolidAmmo =
        inventory.CountItems(PrefabItemNameToken.CannonSolidAmmo);

      var localExplosiveAmmo =
        inventory.CountItems(PrefabItemNameToken.CannonExplosiveAmmo);

      if (isDiffUpdate)
      {
        if (InventorySnapshotData.TryGetValue(
              inventory,
              out var snapshotData))
        {
          currentSolidAmmo -= snapshotData.solidAmmo;
          currentExplosiveAmmo -= snapshotData.explosiveAmmo;
        }
      }

      currentSolidAmmo += localSolidAmmo;
      currentExplosiveAmmo += localExplosiveAmmo;

      InventorySnapshotData[inventory] = new AmmoInventoryData
      {
        explosiveAmmo = localExplosiveAmmo,
        solidAmmo = localSolidAmmo
      };
    }
#endif
    private IEnumerator UpdateAvailableAmmoTypes()
    {
      if (HasUnlimitedAmmo)
      {
        _explosiveAmmo = 999;
        _solidAmmo = 999;
        yield break;
      }

#if VALHEIM
      var currentExplosiveAmmo = 0;
      var currentSolidAmmo = 0;

      if (!IsHandheld)
      {
        // This is an authoritative full recount.
        // Discard old snapshots and rebuild them from the inventories
        // we're actually counting.
        InventorySnapshotData.Clear();

        foreach (var nearbyContainer in _nearbyContainers)
        {
          UpdateAvailableFromInventory(
            nearbyContainer,
            ref currentSolidAmmo,
            ref currentExplosiveAmmo,
            false);
        }
      }
      else
      {
        var player = GetComponentInParent<Player>();

        if (player == null ||
            player.IsDead() ||
            player.IsTeleporting())
        {
          yield break;
        }

        var inventory = player.GetInventory();
        if (inventory == null)
        {
          yield break;
        }

        currentSolidAmmo +=
          inventory.CountItems(PrefabItemNameToken.CannonSolidAmmo);

        currentExplosiveAmmo +=
          inventory.CountItems(PrefabItemNameToken.CannonExplosiveAmmo);
      }

      _explosiveAmmo = Math.Max(0, currentExplosiveAmmo);
      _solidAmmo = Math.Max(0, currentSolidAmmo);
#endif

      yield return null;
    }

    private void RemoveAndUpdateAmmoTypes(
      int ammoToRemoveSolid,
      int ammoToRemoveExplosive)
    {
      if (HasUnlimitedAmmo)
      {
        _explosiveAmmo = 999;
        _solidAmmo = 999;
        return;
      }

      if (ammoToRemoveExplosive <= 0 &&
          ammoToRemoveSolid <= 0)
      {
        return;
      }

#if VALHEIM
      var remainingSolidToRemove = ammoToRemoveSolid;
      var remainingExplosiveToRemove = ammoToRemoveExplosive;

      if (!IsHandheld)
      {
        //
        // Phase 1: remove the requested ammunition.
        //
        // We can stop removing once both requests are satisfied.
        //
        foreach (var nearbyContainer in _nearbyContainers)
        {
          if (nearbyContainer == null) continue;
          if (!nearbyContainer.isActiveAndEnabled) continue;

          var inventory = nearbyContainer.GetInventory();
          if (inventory == null) continue;

          if (remainingSolidToRemove > 0)
          {
            var localSolidAmmo =
              inventory.CountItems(
                PrefabItemNameToken.CannonSolidAmmo);

            if (localSolidAmmo > 0)
            {
              var requestedRemoval =
                Math.Min(
                  remainingSolidToRemove,
                  localSolidAmmo);

              ValheimInventoryCompat.RemoveItemWithRemainder(
                inventory,
                PrefabItemNameToken.CannonSolidAmmo,
                requestedRemoval,
                out var remainder);

              var actuallyRemoved =
                Math.Max(0, requestedRemoval - remainder);

              remainingSolidToRemove -= actuallyRemoved;
            }
          }

          if (remainingExplosiveToRemove > 0)
          {
            var localExplosiveAmmo =
              inventory.CountItems(
                PrefabItemNameToken.CannonExplosiveAmmo);

            if (localExplosiveAmmo > 0)
            {
              var requestedRemoval =
                Math.Min(
                  remainingExplosiveToRemove,
                  localExplosiveAmmo);

              ValheimInventoryCompat.RemoveItemWithRemainder(
                inventory,
                PrefabItemNameToken.CannonExplosiveAmmo,
                requestedRemoval,
                out var remainder);

              var actuallyRemoved =
                Math.Max(0, requestedRemoval - remainder);

              remainingExplosiveToRemove -= actuallyRemoved;
            }
          }

          if (remainingSolidToRemove <= 0 &&
              remainingExplosiveToRemove <= 0)
          {
            break;
          }
        }
      }
      else
      {
        var player = GetComponentInParent<Player>();

        if (player == null ||
            player.IsDead() ||
            player.IsTeleporting())
        {
          return;
        }

        var inventory = player.GetInventory();
        if (inventory == null)
        {
          return;
        }

        if (remainingSolidToRemove > 0)
        {
          var localSolidAmmo =
            inventory.CountItems(
              PrefabItemNameToken.CannonSolidAmmo);

          if (localSolidAmmo > 0)
          {
            var requestedRemoval =
              Math.Min(
                remainingSolidToRemove,
                localSolidAmmo);

            ValheimInventoryCompat.RemoveItemWithRemainder(
              inventory,
              PrefabItemNameToken.CannonSolidAmmo,
              requestedRemoval,
              out var remainder);

            var actuallyRemoved =
              Math.Max(0, requestedRemoval - remainder);

            remainingSolidToRemove -= actuallyRemoved;
          }
        }

        if (remainingExplosiveToRemove > 0)
        {
          var localExplosiveAmmo =
            inventory.CountItems(
              PrefabItemNameToken.CannonExplosiveAmmo);

          if (localExplosiveAmmo > 0)
          {
            var requestedRemoval =
              Math.Min(
                remainingExplosiveToRemove,
                localExplosiveAmmo);

            ValheimInventoryCompat.RemoveItemWithRemainder(
              inventory,
              PrefabItemNameToken.CannonExplosiveAmmo,
              requestedRemoval,
              out var remainder);

            var actuallyRemoved =
              Math.Max(0, requestedRemoval - remainder);

            remainingExplosiveToRemove -= actuallyRemoved;
          }
        }
      }

      if (remainingSolidToRemove > 0)
      {
        LoggerProvider.LogWarning(
          $"Could not remove {remainingSolidToRemove} of " +
          $"{ammoToRemoveSolid} requested solid ammo.");
      }

      if (remainingExplosiveToRemove > 0)
      {
        LoggerProvider.LogWarning(
          $"Could not remove {remainingExplosiveToRemove} of " +
          $"{ammoToRemoveExplosive} requested explosive ammo.");
      }

      //
      // Phase 2: authoritative recount.
      //
      // Do NOT reuse totals gathered during the removal loop because that
      // loop intentionally exits as soon as removal is satisfied.
      //
      var currentSolidAmmo = 0;
      var currentExplosiveAmmo = 0;

      if (!IsHandheld)
      {
        // Cannon removal suppresses OnContainerChanged(), so rebuild the
        // snapshots here at the same time we rebuild the cached totals.
        InventorySnapshotData.Clear();

        foreach (var nearbyContainer in _nearbyContainers)
        {
          UpdateAvailableFromInventory(
            nearbyContainer,
            ref currentSolidAmmo,
            ref currentExplosiveAmmo,
            false);
        }
      }
      else
      {
        var player = GetComponentInParent<Player>();

        if (player != null &&
            !player.IsDead() &&
            !player.IsTeleporting())
        {
          var inventory = player.GetInventory();

          if (inventory != null)
          {
            currentSolidAmmo =
              inventory.CountItems(
                PrefabItemNameToken.CannonSolidAmmo);

            currentExplosiveAmmo =
              inventory.CountItems(
                PrefabItemNameToken.CannonExplosiveAmmo);
          }
        }
      }

      _solidAmmo = Math.Max(0, currentSolidAmmo);
      _explosiveAmmo = Math.Max(0, currentExplosiveAmmo);
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
      if (!isActiveAndEnabled) return;
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