#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Integrations.PowerSystem;
  using ValheimVehicles.Interfaces;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Helpers;
  using ValheimVehicles.ValheimVehicles.RPC;

#endregion

  namespace ValheimVehicles.Integrations;

  /// <summary>
  /// Integration component for SwivelComponent which allow it to work in Valheim.
  /// - Handles Data Syncing.
  /// - Handles config menu opening
  /// - [TODO] Handles lever system GUI. Allowing connections to a lever so a Swivel can be triggered remotely.
  /// - [TODO] Add a wire prefab which is a simple Tag prefab that allows connecting a Swivel to a lever to be legit.
  /// </summary>
  /// <logic>
  /// - OnDestroy the Swivel must remove all references of itself. Alternatively, we could remove unfound swivels.
  /// - Swivels are components that must have a persistentID
  /// - Swivels can function outside a vehicle.
  /// - Swivels can function inside the hierarchy of a vehicle. This requires setting the children of swivels and escaping out of any logic that sets the parent to the VehiclePiecesController container.
  /// </logic>
  ///
  /// Notes
  /// IRaycastPieceActivator is used for simplicity. It will easily match any component extending this in unity.
  public sealed class SwivelComponentBridge : SwivelComponent, IPieceActivatorHost, IPieceController, IRaycastPieceActivator, INetView, IPrefabConfig<SwivelCustomConfig>
  {
    public VehiclePiecesController? m_vehiclePiecesController;
    public VehicleManager? m_vehicle => m_vehiclePiecesController == null ? null : m_vehiclePiecesController.Manager;

    private int _persistentZdoId;
    public static readonly Dictionary<int, SwivelComponentBridge> ActiveInstances = [];
    public List<ZNetView> m_pieces = [];
    public List<ZNetView> m_tempPieces = [];

    private SwivelPieceActivator _pieceActivator = null!;
    public static float turnTime = 50f;

    private HoverFadeText m_hoverFadeText;
    public SwivelConfigSync prefabConfigSync;
    public SwivelCustomConfig Config => this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync).Config;
    public ChildZSyncTransform childZsyncTransform;
    public readonly SwivelMotionStateTracker motionTracker = new();

    private bool _hasInitPrefabSync = false;
    private int swivelId = 0;
    private PowerConsumerBridge powerConsumerIntegration;
    public static Dictionary<ZDO, SwivelComponentBridge> ZdoToComponent = new();
    public ZDO? _currentZdo;

    // sync values
    public static bool CanAllClientsSync = true;
    public static bool ShouldSkipClientOnlyUpdate = true;
    public static bool ShouldSyncClientOnlyUpdate = false;

    public override int SwivelPersistentId
    {
      get
      {
        if (swivelId == 0)
        {
          swivelId = GetPersistentId();
        }
        return swivelId;
      }
    }

    public override void Awake()
    {
      base.Awake();

      if (!prefabConfigSync)
      {
        this.GetOrCache(ref prefabConfigSync, ref _hasInitPrefabSync);
      }

      _IsReady = false;

      m_nview = GetComponent<ZNetView>();
      // required for syncing the animated component across clients.
      // childZsyncTransform = animatedTransform.gameObject.AddComponent<ChildZSyncTransform>();
      // childZsyncTransform.m_syncPosition = true;
      // childZsyncTransform.m_syncRotation = true;
      // childZsyncTransform.m_syncBodyVelocity = true;

      SetupHoverFadeText();

      SetupPieceActivator();
    }

    public override void SetMotionState(MotionState state)
    {
      if (!prefabConfigSync) return;
      if (prefabConfigSync.IsBroadcastSuppressed)
      {
        base.SetMotionState(state);
        return;
      }

      if (!this.IsNetViewValid(out var netView))
        return;

      var currentState = MotionState;
      if (state == currentState)
      {
        LoggerProvider.LogDebug("[Swivel] SetMotionState called with same state. Ignoring.");
        return;
      }

      if (!netView.IsOwner())
      {
        if (!ShouldSkipClientOnlyUpdate)
        {
          Request_SetMotionState(state);
        }

        // this is likely unstable
        if (ShouldSyncClientOnlyUpdate)
        {
          prefabConfigSync.Load();
          base.SetMotionState(prefabConfigSync.Config.MotionState);
        }
        return;
      }

      if (prefabConfigSync != null && prefabConfigSync.Config != null)
      {
        prefabConfigSync.Config.MotionState = state;
      }
      base.SetMotionState(state);

      Request_SetMotionState(state);

      if (state is MotionState.ToTarget or MotionState.ToStart)
      {
        motionTracker.UpdateMotion(
          animatedTransform.localPosition,
          GetCurrentTargetPosition(),
          animatedTransform.localRotation,
          GetCurrentTargetRotation(),
          computedInterpolationSpeed
        );
      }
    }

    public override void Request_NextMotionState()
    {
      if (!this.IsNetViewValid(out var netView)) return;
      if (prefabConfigSync.Config.MotionState != MotionState)
      {
        LoggerProvider.LogWarning("SwivelComponentBridge.Request_NextMotionState called but the motion state is not the same as the current state. This is likely a bug.");
      }

      prefabConfigSync.Load(false, [SwivelCustomConfig.Key_MotionState]);
      LastMotionState = MotionState;
      SwivelPrefabConfigRPC.Request_NextMotion(netView.GetZDO(), MotionState);

      // if (_nextMotionCoroutine != null)
      // {
      //   StopCoroutine(_nextMotionCoroutine);
      //   _nextMotionCoroutine = null;
      // }
      // else
      // {
      //   // _nextMotionCoroutine = StartCoroutine(NextMotionStateAwaiter());
      // }
    }

    public Coroutine? _nextMotionCoroutine;
    public MotionState LastMotionState = MotionState.AtStart;

    /// <summary>
    /// Workaround to fix the problem with a client requiring two presses to get the button to update.
    /// </summary>
    // public IEnumerator NextMotionStateAwaiter()
    // {
    //   yield return new WaitForFixedUpdate();
    //   if (LastMotionState == MotionState)
    //   {
    //     Request_NextMotionState();
    //   }
    //   _nextMotionCoroutine = null;
    // }
    public void Request_SetMotionState(MotionState state)
    {
      if (!this.IsNetViewValid(out var netView)) return;
      SwivelPrefabConfigRPC.Request_SetMotionState(netView.GetZDO(), state);
    }

    public void SetupHoverFadeText()
    {
      m_hoverFadeText = HoverFadeText.CreateHoverFadeText(transform);
      m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;
      m_hoverFadeText.Hide();
    }

    public void OnEnable()
    {
      var persistentId = GetPersistentId();
      if (persistentId != 0 && !ActiveInstances.ContainsKey(persistentId))
      {
        ActiveInstances.Add(persistentId, this);
      }
    }

    public void SetupPieceActivator()
    {
      _pieceActivator = gameObject.AddComponent<SwivelPieceActivator>();
      _pieceActivator.Init(this);
      _pieceActivator.OnActivationComplete = OnActivationComplete;
      _pieceActivator.OnInitComplete = OnInitComplete;
    }

    public void OnDisable()
    {
      if (_currentZdo != null)
      {
        ZdoToComponent.Remove(_currentZdo);
      }

      var persistentId = GetPersistentId();
      if (persistentId != 0 && ActiveInstances.TryGetValue(persistentId, out _))
      {
        ActiveInstances.Remove(persistentId);
      }
    }

    public override void Update()
    {
      if (!CanRunSwivelDuringUpdate) return;
      if (!CanRunBaseUpdate()) return;
      base.Update();
      GuardedMotionCheck();
    }


    public override void FixedUpdate()
    {
      if (!CanRunSwivelDuringFixedUpdate) return;
      if (!CanRunBaseUpdate()) return;

      base.FixedUpdate();
      GuardedMotionCheck();
    }

    public void GuardedMotionCheck()
    {
      // guarded update on DemandState which can desync.
      // if (MotionState is MotionState.AtStart or MotionState.AtTarget && powerConsumerIntegration && powerConsumerIntegration.IsDemanding && powerConsumerIntegration.Data.zdo != null && powerConsumerIntegration.Data.zdo.IsValid())
      // {
      //   powerConsumerIntegration.Data.SetDemandState(false);
      //   PowerSystemRPC.Request_UpdatePowerConsumer(powerConsumerIntegration.Data.zdo.m_uid, powerConsumerIntegration.Data);
      // }
    }

    public bool CanRunBaseUpdate()
    {
      if (!CanUpdate()) return false;
      // no server only running of this update.
      if (!ZNet.instance || ZNet.instance.IsDedicated()) return false;

      if (!CanAllClientsSync)
      {
        if (!this.IsNetViewValid(out var netView))
        {
          return false;
        }
        // non-owners do not update. All logic is done via RPC Child sync.
        if (!netView.IsOwner())
        {
          return false;
        }
      }

      return true;
    }


    public static bool TryAddPieceToSwivelContainer(int persistentId, ZNetView netViewPrefab)
    {
      if (!ActiveInstances.TryGetValue(persistentId, out var swivelComponentIntegration))
      {
        LoggerProvider.LogDev("No instance of SwivelComponentIntegration found for persistentId: " + persistentId + "This could mean the swivel is not yet loaded or the associated items did not get removed when the Swivel was destroyed.");
        return false;
      }

      return true;
    }

    public void AddPieceToParent(Transform pieceTransform)
    {
      pieceTransform.SetParent(piecesContainer);
    }

    public void StartActivatePendingSwivelPieces()
    {
      if (!_pieceActivator) return;
      _pieceActivator.StartActivatePendingPieces();
    }

    public void OnActivationComplete()
    {
      _IsReady = true;
    }

    public void Register() {}

    protected override void OnDestroy()
    {
      base.OnDestroy();
      StopAllCoroutines();
      Cleanup();
    }

    public void Cleanup()
    {
      if (!isActiveAndEnabled) return;
      if (ZNetScene.instance == null || Game.instance == null) return;
      foreach (var nvPiece in m_pieces)
      {
        if (nvPiece == null || nvPiece.GetZDO() == null) continue;
        nvPiece.transform.SetParent(null);
      }
    }


    /// <summary>
    /// Returns true if the item is part of a SwivelContainer even if it does not parent the item to the swivel container if it does not exist yet. 
    /// </summary>
    /// <param name="netView"></param>
    /// <param name="zdo"></param>
    /// <returns></returns>
    public static bool TryAddPieceToSwivelContainer(ZNetView netView, ZDO zdo)
    {
      if (!TryGetSwivelParentId(zdo, out var swivelParentId))
      {
        return false;
      }
      TryAddPieceToSwivelContainer(swivelParentId, netView);
      return true;
    }

    public void AddNewPiece(ZNetView netView)
    {

      // do not add a swivel within a swivel. This could cause some really weird behaviors so it's not supported.
      if (netView.name.StartsWith(PrefabNames.SwivelPrefabName)) return;
      if (netView == null || netView.GetZDO() == null) return;
      var persistentId = GetPersistentId();
      if (persistentId == 0) return;
      var zdo = netView.GetZDO();
      zdo.Set(VehicleZdoVars.SwivelParentId, persistentId);

      m_hoverFadeText.Show();
      m_hoverFadeText.transform.position = transform.position + Vector3.up * 2f;
      m_hoverFadeText.currentText = ModTranslations.Swivel_Connected;

      // must call this otherwise everything is in world position. 
      AddPieceToParent(netView.transform);

      netView.m_zdo.Set(VehicleZdoVars.MBRotationVecHash,
        netView.transform.localRotation.eulerAngles);
      netView.m_zdo.Set(VehicleZdoVars.MBPositionHash,
        netView.transform.localPosition);

      AddPiece(netView);
    }

    public static bool TryGetSwivelParentId(ZDO? zdo, out int swivelParentId)
    {
      swivelParentId = 0;
      if (zdo == null) return false;
      swivelParentId = zdo.GetInt(VehicleZdoVars.SwivelParentId);
      return swivelParentId != 0;
    }

    public static bool IsSwivelParent(ZDO? zdo)
    {
      if (zdo == null) return false;
      return zdo.GetInt(VehicleZdoVars.SwivelParentId) != 0;
    }

    public override void Start()
    {
      base.Start();
      m_vehiclePiecesController = GetComponentInParent<VehiclePiecesController>();
      _pieceActivator.StartInitPersistentId();
      prefabConfigSync.Load();
    }

    /// <summary>
    /// Inefficient no-cache way to update the debugger arrow. This is not meant to be called a bunch.
    /// </summary>
    private void AdjustDebuggerArrowLocation()
    {
      var colliders = piecesContainer.GetComponentsInChildren<Collider>();
      Bounds? bounds = null;
      foreach (var collider in colliders)
      {
        var colliderBounds = collider.bounds;
        bounds ??= new Bounds(colliderBounds.center, Vector3.zero);
        bounds.Value.Encapsulate(colliderBounds);
      }

      if (bounds.HasValue)
      {
        directionDebuggerArrow.position = bounds.Value.center;
      }
      else
      {
        directionDebuggerArrow.position = Player.m_localPlayer.m_body.ClosestPointOnBounds(Player.m_localPlayer.transform.up * 3f);
      }
    }

    public override void InitPowerConsumer()
    {
      powerConsumerIntegration = gameObject.AddComponent<PowerConsumerBridge>();
      swivelPowerConsumer = powerConsumerIntegration.Logic;
      UpdatePowerConsumer();
      UpdateBasePowerConsumption();
    }

    public override void UpdatePowerConsumer()
    {
      if (!ZNet.instance) return;
      if (!this.IsNetViewValid(out var netView)) return;
      if (ZNet.instance.IsServer())
      {
        base.UpdatePowerConsumer();
        OnPowerConsumerUpdate();
      }
    }

    private bool _isAuthoritativeMotionActive;
    private MotionState _motionState;
    private double _motionStartTime;
    private float _motionDuration;
    /// <summary>
    /// For interpolated eventing.
    /// </summary>
    /// <param name="motionUpdate"></param>
    public void SetMotionUpdate(SwivelMotionUpdate motionUpdate)
    {
      _isAuthoritativeMotionActive = true;
      _motionState = motionUpdate.MotionState;
      _motionStartTime = motionUpdate.StartTime;
      _motionDuration = motionUpdate.Duration;
    }

    public override void SwivelUpdate()
    {
      if (_isAuthoritativeMotionActive)
      {
        var now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;
        var t = Mathf.Clamp01((float)((now - _motionStartTime) / _motionDuration));

        // Move
        if (mode == SwivelMode.Move)
        {
          Vector3 from, to;
          if (_motionState == MotionState.ToTarget)
          {
            from = startLocalPosition;
            to = startLocalPosition + movementOffset;
          }
          else
          {
            from = startLocalPosition + movementOffset;
            to = startLocalPosition;
          }
          animatedTransform.localPosition = Vector3.Lerp(from, to, t);
        }
        // Rotate
        if (mode == SwivelMode.Rotate)
        {
          Quaternion from, to;
          if (_motionState == MotionState.ToTarget)
          {
            from = Quaternion.identity;
            to = CalculateRotationTarget();
          }
          else
          {
            from = CalculateRotationTarget();
            to = Quaternion.identity;
          }
          animatedTransform.localRotation = Quaternion.Slerp(from, to, t);
        }
        // No MotionState update! Wait for server.
        return;
      }

      base.SwivelUpdate();
    }

    public static float ComputeMotionDuration(SwivelCustomConfig config, MotionState direction)
    {
      // Match your speed multipliers—if you want to make these data-driven, add to config/ZDO!
      const float MoveMultiplier = 0.2f;
      const float RotateMultiplier = 3f;

      // Safety clamp, never divide by zero
      var speed = Mathf.Max(
        config.InterpolationSpeed *
        (config.Mode == SwivelMode.Move ? MoveMultiplier : RotateMultiplier),
        0.001f
      );

      if (config.Mode == SwivelMode.Move)
      {
        // Always use start as base, plus MovementOffset
        Vector3 from, to;
        if (direction == MotionState.ToTarget)
        {
          from = Vector3.zero; // startLocalPosition is always zero in pure data configs
          to = config.MovementOffset;
        }
        else
        {
          from = config.MovementOffset;
          to = Vector3.zero;
        }

        var distance = Vector3.Distance(from, to);
        return Mathf.Max(distance / speed, 0.01f); // Never less than 10ms
      }
      if (config.Mode == SwivelMode.Rotate)
      {
        // Calculate hingeEndEuler as your SwivelComponent does
        var hingeEndEuler = Vector3.zero;
        if ((config.HingeAxes & HingeAxis.X) != 0)
          hingeEndEuler.x = config.MaxEuler.x;
        if ((config.HingeAxes & HingeAxis.Y) != 0)
          hingeEndEuler.y = config.MaxEuler.y;
        if ((config.HingeAxes & HingeAxis.Z) != 0)
          hingeEndEuler.z = config.MaxEuler.z;

        Quaternion from, to;
        if (direction == MotionState.ToTarget)
        {
          from = Quaternion.identity;
          to = Quaternion.Euler(hingeEndEuler);
        }
        else
        {
          from = Quaternion.Euler(hingeEndEuler);
          to = Quaternion.identity;
        }
        var angle = Quaternion.Angle(from, to); // in degrees
        return Mathf.Max(angle / speed, 0.01f);
      }
      // fallback
      return 0.01f;
    }

    /// <summary>
    /// For all logic to sync updates to server or local user.
    /// </summary>
    public void OnPowerConsumerUpdate()
    {
      if (!swivelPowerConsumer) return;
      if (!this.IsNetViewValid(out var netView))
      {
        return;
      }

      // powerConsumerIntegration.Data.Load();
      //
      // var isOwner = netView.IsOwner();
      // if (!isOwner || !powerConsumerIntegration)
      // {
      //   // possible infinity update here...
      //   PowerSystemRPC.Request_UpdatePowerConsumer(netView.GetZDO().m_uid, swivelPowerConsumer.Data);
      // }
      // else
      // {
      //   powerConsumerIntegration.UpdateNetworkedData();
      // }
    }

    public void OnInitComplete()
    {
      StartActivatePendingSwivelPieces();
    }

    public override Quaternion CalculateTargetWindDirectionRotation()
    {
      if (!m_vehicle || !m_vehicle.MovementController)
      {
        var windDir = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : transform.forward;

        // these calcs are probably all wrong.
        // var dir = Utils.YawFromDirection(transform.InverseTransformDirection(windDir));
        var dir = Quaternion.LookRotation(
          -Vector3.Lerp(windDir,
            Vector3.Normalize(windDir - piecesContainer.forward), turnTime),
          piecesContainer.up);

        return Quaternion.RotateTowards(piecesContainer.transform.rotation, dir, 30f * Time.fixedDeltaTime);
      }
      // use the sync mast
      return m_vehicle.MovementController!.m_mastObject.transform.localRotation;
    }

  #region IBasePieceActivator

    public int GetPersistentId()
    {
      if (!this.IsNetViewValid(out var netView))
      {
        return 0;
      }
      return PersistentIdHelper.GetPersistentIdFrom(netView, ref _persistentZdoId);
    }

    public ZNetView? GetNetView()
    {
      return m_nview;
    }
    public Transform GetPieceContainer()
    {
      return piecesContainer;
    }

  #endregion

  #region IPieceController

    public bool CanDestroy()
    {
      return GetPieceCount() == 0;
    }

    private static string _ComponentName => PrefabNames.SwivelPrefabName;
    public string ComponentName => _ComponentName;

    public int GetPieceCount()
    {
      if (m_nview == null || m_nview.GetZDO() == null) return m_pieces.Count;
      var pieceCount = m_nview.GetZDO().GetInt(VehicleZdoVars.MBPieceCount, m_pieces.Count);
      return pieceCount;
    }

    public bool CanRaycastHitPiece()
    {
      return GetPieceCount() > 0;
    }

    public static bool CanDestroySwivel(ZNetView? netView)
    {
      try
      {
        if (netView == null) return false;
        var swivelController = netView.GetComponent<SwivelComponentBridge>();
        if (swivelController == null)
        {
          LoggerProvider.LogDebug("Bailing vehicle deletion attempt: Valheim Attempted to delete a vehicle that matched the ValheimVehicle prefab instance but there was no VehicleBaseController or VehiclePiecesController found. This could mean there is a mod breaking the vehicle registration of ValheimRAFT.");
          return false;
        }
        var hasPendingPieces =
          BasePieceActivatorComponent.m_pendingPieces.TryGetValue(swivelController.GetPersistentId(),
            out var pendingPieces);
        var hasPieces = swivelController.GetPieceCount() != 0;

        // if there are pending pieces, do not let vehicle be destroyed
        if (pendingPieces != null && hasPendingPieces && pendingPieces.Count > 0)
          return false;

        return !hasPieces;
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning("A error was thrown in CanDestroyVehicle. This likely means the vehicle is corrupt. This check will protect the vehicle from being deleted.");
        return false;
      }
    }

    public void AddPiece(ZNetView nv, bool isNew = false)
    {
      if (nv == null)
      {
        BasePieceActivatorComponent.AddPendingPiece(GetPersistentId(), nv);
        return;
      }
      AddPieceToParent(nv.transform);

      if (m_vehiclePiecesController != null)
      {
        m_vehiclePiecesController.AddPiece(nv);
      }

      PieceActivatorHelpers.FixPieceMeshes(nv);
      m_pieces.Add(nv);

      TogglePlacementContainer(m_pieces.Count == 0);
    }

    public void AddCustomPiece(ZNetView nv, bool isNew = false)
    {
      LoggerProvider.LogWarning("CustomPieces not supported for SwivelComponentIntegration. This is likely a bug. Please report this to the mod author.");
      return;
    }

    public void AddCustomPiece(GameObject prefab, bool isNew = false)
    {
      LoggerProvider.LogWarning("CustomPieces not supported for SwivelComponentIntegration. This is likely a bug. Please report this to the mod author.");
      return;
    }


    public void TogglePlacementContainer(bool isZeroPieces)
    {
      connectorContainer.gameObject.SetActive(isZeroPieces);
    }

    public void RemovePiece(ZNetView nv)
    {
      if (nv == null) return;
      m_pieces.Remove(nv);

      if (m_vehiclePiecesController != null)
      {
        m_vehiclePiecesController.RemovePiece(nv);
      }

      var currentPieceCount = GetPieceCount();
      var hasZeroPieces = currentPieceCount == 0;
      if (hasZeroPieces)
      {
        m_hoverFadeText.Hide();
      }
      TogglePlacementContainer(hasZeroPieces);
    }
    public void TrySetPieceToParent(ZNetView netView)
    {
      if (netView == null) return;
      AddPieceToParent(netView.transform);
    }

    ///
    /// <summary>
    ///   prevents ship destruction on m_nview null
    ///   - if null it would prevent getting the ZDO information for the ship pieces
    /// </summary>
    /// 
    public void DestroyPiece(WearNTear wnt)
    {
      if (wnt == null) return;
      var nv = wnt.GetComponent<ZNetView>();
      RemovePiece(nv);
    }

  #endregion

    public ZNetView? m_nview
    {
      get;
      set;
    }
  }