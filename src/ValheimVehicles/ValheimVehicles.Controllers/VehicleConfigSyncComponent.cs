#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Enums;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.SharedScripts;

#endregion

  namespace ValheimVehicles.Controllers;

  public class VehicleConfigSyncComponent : PrefabConfigSync<VehicleCustomConfig, IVehicleConfig>, IVehicleSharedProperties
  {
    private VehicleManager? _vehicle;
    private BoxCollider? FloatCollider => _vehicle != null ? _vehicle.FloatCollider : null;
    private VehicleFloatationMode _cachedFloatationMode = VehicleFloatationMode.Average;
    private float _cachedFloatationHeight = 0;

    public Action? OnLoadSubscriptions;

    public override void Awake()
    {
      _vehicle = GetComponent<VehicleManager>();
      base.Awake();
    }

    public override void OnLoad()
    {
      OnLoadSubscriptions?.Invoke();
    }

  #region IRPCSync

    /// <summary>
    /// This adds a retryGuard.
    /// </summary>
    public override void RegisterRPCListeners()
    {
      if (retryGuard == null) return;
      if (hasRegisteredRPCListeners) return;
      if (!this.IsNetViewValid(out var netView))
      {
        retryGuard.Retry(RegisterRPCListeners, 1);
        return;
      }

      base.RegisterRPCListeners();

      rpcHandler?.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);
      rpcHandler?.Register<ZPackage>(nameof(RPC_SyncFloatationMode), RPC_SyncFloatationMode);

      hasRegisteredRPCListeners = true;
    }

  #endregion

    public VehicleFloatationMode GetWaterFloatationHeightMode()
    {
      if (Config.HasCustomFloatationHeight) return VehicleFloatationMode.Custom;
      return PhysicsConfig.HullFloatationColliderLocation.Value;
    }

    public void Request_SyncFloatationMode(bool isCustom, float relativeHeight)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      var pkg = new ZPackage();
      pkg.Write(isCustom);
      pkg.Write(relativeHeight);
      netView.InvokeRPC(netView.GetZDO().GetOwner(), nameof(RPC_SyncFloatationMode), pkg);
    }

    private void RPC_SyncFloatationMode(long sender, ZPackage pkg)
    {
      if (!this.IsNetViewValid(out var netView) || !netView.IsOwner()) return;

      var isCustom = pkg.ReadBool();
      var relativeHeight = pkg.ReadSingle();

      var updated = new VehicleCustomConfig();
      updated.ApplyFrom(Config);

      updated.HasCustomFloatationHeight = isCustom;
      updated.CustomFloatationHeight = relativeHeight;

      CommitConfigChange(updated); // saves + broadcasts
    }

    public void SendRPCToAllClients(List<long> clients, string methodName, bool skipLocal = false)
    {
      if (!this.IsNetViewValid(out var netView)) return;

      // TODO ensure that GetZDO owner is the same as Player.GetPlayerID otherwise it could be GetOwner but that is the ownerId of the player's current netview.
      var currentPlayer = Player.m_localPlayer.GetPlayerID();
      var nvOwner = netView.m_zdo.GetOwner();
      var hasSentSyncToOwner = false;


      clients.ForEach(clientId =>
      {
        if (!skipLocal && clientId == nvOwner)
        {
          hasSentSyncToOwner = true;
        }
        if (currentPlayer == clientId)
        {
          SyncVehicleBounds();
          return;
        }

        rpcHandler?.InvokeRPC(clientId, nameof(RPC_SyncBounds));
      });

      if (!hasSentSyncToOwner)
      {
        Invoke(nameof(methodName), 0.5f);
      }
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

      var playerIds = OnboardController.m_localPlayers.Where(x => x != null).Select(x => x.GetPlayerID()).ToList();
      SendRPCToAllClients(playerIds, nameof(RPC_SyncBounds), false);
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
    public VehicleLandMovementController? LandMovementController
    {
      get;
      set;
    } = null!;
    public VehicleManager Manager
    {
      get;
      set;
    } = null!;

    public bool IsControllerValid => Manager.IsControllerValid;

    public bool IsInitialized => Manager.IsInitialized;

    public bool IsDestroying => Manager.IsDestroying;

  #endregion

  }