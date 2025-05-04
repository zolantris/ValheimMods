#region

  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.Config;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.SharedScripts;

#endregion

  namespace ValheimVehicles.Controllers;

  public class VehicleConfigSyncComponent : PrefabConfigRPCSync<VehicleCustomConfig>, IVehicleSharedProperties
  {
    private VehicleManager _vehicle;
    private BoxCollider? FloatCollider => _vehicle.FloatCollider;
    private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
    private float _cachedFloatationHeight = 0;

    public override void Awake()
    {
      base.Awake();
      _vehicle = GetComponent<VehicleManager>();
    }

  #region IRPCSync

    /// <summary>
    /// This adds a retryGuard.
    /// </summary>
    public override void RegisterRPCListeners()
    {
      if (!this.IsNetViewValid(out var netView))
      {
        retryGuard.Retry(RegisterRPCListeners, 1);
        return;
      }

      base.RegisterRPCListeners();
      rpcHandler?.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

      hasRegisteredRPCListeners = true;
    }

  #endregion

    public VehicleFloatationMode GetWaterFloatationHeightMode()
    {
      if (CustomConfig.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
      return PhysicsConfig.HullFloatationColliderLocation.Value;
    }

    public void SendSyncFloatationMode(bool isCustom, float relativeHeight)
    {
      // do nothing for land-vehicles.
      if (_vehicle.IsLandVehicle) return;

      // set config immediately, but it might not be updated if the ranges are out of bounds
      CustomConfig.HasCustomFloatationHeight = isCustom;
      CustomConfig.CustomFloatationHeight = relativeHeight;

      SendPrefabConfig();
    }

    public void SendRPCToAllClients(List<long> clients, string methodName, bool skipLocal = false)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var currentPlayer = Player.m_localPlayer.GetPlayerID();
      var nvOwner = netView.m_zdo.GetOwner();
      var hasSentSyncToOwner = false;


      OnboardController.m_localPlayers.ForEach(x =>
      {
        var playerOwnerId = x.GetPlayerID();

        if (playerOwnerId == nvOwner)
        {
          hasSentSyncToOwner = true;
        }
        if (currentPlayer == playerOwnerId)
        {
          SyncVehicleBounds();
          return;
        }

        rpcHandler.InvokeRPC(playerOwnerId, nameof(RPC_SyncBounds));
      });

      Invoke(nameof(methodName), 0.5f);
    }

    /// <summary>
    /// bounds sync for all players on the boat.
    /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
    ///
    /// Also should prevent desyncs if we can synchronize it.
    /// </summary>
    public void SendSyncBounds()
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (!this || OnboardController == null) return;

      // TODO ensure that GetZDO owner is the same as Player.GetPlayerID otherwise it could be GetOwner but that is the ownerId of the player's current netview.
      var currentPlayer = Player.m_localPlayer.GetPlayerID();
      var nvOwner = netView.m_zdo.GetOwner();
      var hasSentSyncToOwner = false;

      OnboardController.m_localPlayers.ForEach(x =>
      {
        var playerOwnerId = x.GetPlayerID();

        if (playerOwnerId == nvOwner)
        {
          hasSentSyncToOwner = true;
        }
        if (currentPlayer == playerOwnerId)
        {
          SyncVehicleBounds();
          return;
        }

        netView.InvokeRPC(playerOwnerId, nameof(RPC_SyncBounds));
      });

      if (!hasSentSyncToOwner)
      {
        netView.InvokeRPC(nvOwner, nameof(RPC_SyncBounds));
      }


    }

    /// <summary>
    ///   Forces a resync of bounds for all players on the ship, this may need to be
    ///   only from host but then would require syncing all collider data that updates
    ///   in the OnBoundsUpdate
    /// </summary>
    public void RPC_SyncBounds(long sender)
    {
      if (!PiecesController) return;
      SyncVehicleBounds();
    }

    public void SyncVehicleBounds()
    {
      if (PiecesController == null) return;
      PiecesController.RequestBoundsRebuild();
    }

  #region IVehicleSharedProperties

    public VehiclePiecesController? PiecesController
    {
      get;
      set;
    } = null!;
    public VehicleMovementController? MovementController
    {
      get;
      set;
    } = null!;
    public VehicleConfigSyncComponent? VehicleConfigSync
    {
      get;
      set;
    } = null!;
    public VehicleOnboardController? OnboardController
    {
      get;
      set;
    } = null!;
    public VehicleWheelController? WheelController
    {
      get;
      set;
    } = null!;
    public VehicleManager? Manager
    {
      get;
      set;
    } = null!;

    public bool IsControllerValid
    {
      get;
    }
    public bool IsInitialized
    {
      get;
      set;
    }
    public bool IsDestroying
    {
      get;
      set;
    }

  #endregion

  }