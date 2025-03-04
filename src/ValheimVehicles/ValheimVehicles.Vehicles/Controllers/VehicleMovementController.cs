#region

  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Serialization;
  using ValheimRAFT;
  using ValheimRAFT.Config;
  using ValheimRAFT.Patches;
  using ValheimVehicles.Config;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Helpers;
  using ValheimVehicles.Prefabs;
  using ValheimVehicles.Prefabs.Registry;
  using ValheimVehicles.Propulsion.Rudder;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.ValheimVehicles.Vehicles.Structs;
  using ValheimVehicles.Vehicles.Components;
  using ValheimVehicles.Vehicles.Controllers;
  using ValheimVehicles.Vehicles.Enums;
  using ValheimVehicles.Vehicles.Interfaces;
  using ValheimVehicles.Vehicles.Structs;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.Vehicles;

  public class VehicleMovementController : ValheimBaseGameShip, IVehicleMovement,
    IValheimShip,
    IMonoUpdater
  {
    public enum DirectionChange
    {
      Forward,
      Backward,
      Stop
    }

    public enum PhysicsTarget
    {
      VehicleShip,
      VehiclePieces
    }

    public enum VehiclePhysicsState
    {
      Land,
      Air,
      Sea,
      Submerged
    }

    private const float InitialTargetHeight = 0f;

    public const string m_attachAnimation = "Standing Torch Idle right";

    public const float m_balanceForce = 0.03f;

    public const float m_liftForce = 20f;

    // combo of Z and X enum
    public const RigidbodyConstraints FreezeBothXZ = (RigidbodyConstraints)80;

    public const string vehicleKeyPrefix = "valheim_vehicle";

    private const float MaxFlightOffset = 10000f;

    private const string InUseMessage = "$msg_inuse";

    public static PhysicsTarget PhysicsSyncTarget = PhysicsTarget.VehicleShip;

    public static bool CanRunSidewaysWaterForceUpdate = true;
    public static bool CanRunBackWaterForce = true;
    public static bool CanRunForwardWaterForce = true;
    public static bool CanRunRightWaterForce = true;
    public static bool CanRunLeftWaterForce = true;

    public static float maxFlyingHeight = 5000f;

    private static float buoyancyThreshold = -1f;

    public bool isAnchored;

    public SteeringWheelComponent lastUsedWheelComponent;
    public Vector3 detachOffset = new(0f, 0.5f, 0f);
    public float m_rudderSpeed = 0.5f;
    public VehicleZSyncTransform zsyncTransform;

    public bool isBeached;

    [FormerlySerializedAs("TargetHeightObj")]
    public GameObject DebugTargetHeightObj;

    public Vector3 lastPosition = Vector3.zero;
    public float lastPositionUpdate;

    public string vehicleMapKey = "";

    public float floatSizeOverride;

    public float groundHeightPaddingOffset = 2f;

    public Vector3 currentUpwardsForce = Vector3.zero;
    public Vector3 currentUpwardsForceVelocity = Vector3.zero;

    public bool CanApplyWaterEdgeForce = true;
    public bool CanAnchor;

    public float lastFlyingDt;
    public bool cachedFlyingValue;

    public float cachedMaxDepthOffset;

    public float directionalForceUnanchored = 0.15f;
    public float directionalForceSubmerged = 0.05f;
    public float directionalForceAnchored = 0.075f;
    public float maxForce = 0.5f;

    public Vector3 vehicleAutomaticCenterOfMassPoint = Vector3.zero;
    public Transform DamageColliders = null!;
    public VehicleRamAoe? vehicleRam;

    public bool isHoldingBreak;

    public bool shouldUpdateLandInputs = true;

    public float localCenterOfMassOffset;
    public RigidbodyConstraints FreezeXZRotation = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;

    public bool isPlayerHaulingVehicle;
    public Player? HaulingPlayer;
    public RopeAnchorComponent? HaulingRopeComponent;

    public float centerOfMassForceOffsetDifferenceMultiplier = 1f;

    public static float haulMoveSpeed = 1.5f;
    public static float haulRotationSpeed = 1f;

    private ShipFloatation? _currentShipFloatation;

    private Coroutine? _debouncedForceTakeoverControlsInstance;

    // todo remove if unused or make this a getter. Might not be safe from null references though.
    // private GameObject _piecesContainer;
    private GameObject _ghostContainer;
    private bool _hasRegister;
    private ImpactEffect _impactEffect;

    // flying mechanics
    private bool _isAscending;
    private bool _isDescending;
    private bool _isAscendToggled = false;
    private bool _isDescendToggled = false;

    // prevents updating repeatedly firing while the key is down
    private bool _isHoldingAnchor;
    private float _lastHighestGroundPoint;

    private bool _prevGravity = true;

    private float _previousTargetHeight;

    private VehiclePhysicsState _previousVehiclePhysicsState;

    // The rudder force multiplier applied to the ship speed
    private float _rudderForce = 1f;

    private VehicleShip? _vehicle;

    private AnchorState _vehicleAnchorState = AnchorState.Idle;

    private GameObject _vehiclePiecesContainerInstance;

    private Vector3 anchorSmoothVelocity = Vector3.zero;

    private bool HasPendingAnchor;

    private float lastForceUpdateTimer;

    private float lastSyncTargetHeight;
    private float lastUpdateWindControlStatus;

    internal bool m_backwardPressed;

    private bool m_cachedWindControlStatus;

    internal bool m_forwardPressed;

    internal float m_rudder;
    internal float m_rudderValue;

    internal float m_sendRudderTime;

    private float maxYLinearVelocity = 10f;
    private GUIStyle myButtonStyle;
    private float prevBackUpwardsForce;
    private float prevCenterUpwardsForce;

    private float prevFrontUpwardForce;
    private float prevLeftUpwardsForce;
    private float prevRightUpwardsForce;
    private Ship.Speed vehicleSpeed;

    private float vehicleStatSyncTimer;

    // hauling config values
    public static float minHaulFollowDistance = 2f;
    public static float maxHaulFollowDistance = 50f;
    public static float HaulingOffsetLowestPointBuffer = 1f;
    public float distanceMovedSinceHaulTick = 0f;

    public VehicleOnboardController? OnboardController =>
      _vehicle.OnboardController;

    public AnchorState vehicleAnchorState
    {
      get => _vehicleAnchorState;
      set
      {
        _vehicleAnchorState = value;
        isAnchored = IsAnchorDropped(_vehicleAnchorState);
      }
    }

    public IVehicleShip? VehicleInstance => _vehicle;

    public VehiclePiecesController? PiecesController =>
      VehicleInstance?.PiecesController;

    public VehicleWheelController? WheelController =>
      VehicleInstance?.WheelController;

    public BoxCollider? OnboardCollider =>
      _vehicle != null ? _vehicle.OnboardCollider : null;

    public Rigidbody rigidbody => m_body;
    public Transform AttachPoint { get; set; }
    public bool HasOceanSwayDisabled { get; set; }

    public GameObject RudderObject { get; set; }

    public bool isCreative => VehicleInstance?.Instance?.isCreative ?? false;
    private Ship.Speed VehicleSpeed => GetSpeedSetting();

    public Transform ShipDirection { get; set; } = null!;

    public static List<VehicleMovementController> Instances { get; } = [];

    public static List<IMonoUpdater> MonoUpdaterInstances { get; } = [];

    public static float staminaHaulCost = 5;
    public static bool ShouldHaulingLineSnapOnZeroStamina = false;
    
    public BoxCollider? FloatCollider =>
      _vehicle != null ? _vehicle.FloatCollider : null;

    public Transform ControlGuiPosition
    {
      get => m_controlGuiPos;
      set => m_controlGuiPos = value;
    }

    private bool IsNotFlying =>
      !IsFlying();

    public static bool HasPieceSyncTarget =>
      PhysicsSyncTarget == PhysicsTarget.VehiclePieces;

    public ShipFloatation ShipFloatationObj
    {
      get
      {
        if (_currentShipFloatation != null) return _currentShipFloatation.Value;
        _currentShipFloatation = GetShipFloatationObj();
        return _currentShipFloatation.Value;
      }
    }

    public static bool IsBallastAndFlightDisabled =>
      !PropulsionConfig.AllowFlight.Value &&
      !WaterConfig.WaterBallastEnabled.Value;


    // todo getButtonDef and listen for press state directly
    public bool GetAscendKeyPress =>
      ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump");

    // todo getButtonDef and listen for press state directly
    public bool GetDescendKeyPress =>
      ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch");

    public bool CanDescend =>
      WaterConfig.WaterBallastEnabled.Value && IsNotFlying || IsFlying();

    internal override void Awake()
    {
      AwakeSetupShipComponents();
      DamageColliders = VehicleShip.GetVehicleMovementDamageColliders(transform);
      m_nview = GetComponent<ZNetView>();

      var excludedLayers = LayerMask.GetMask("piece", "piece_nonsolid");
      m_body.excludeLayers = excludedLayers;

      if (!m_nview) m_nview = GetComponent<ZNetView>();

      if (PropulsionConfig.AllowFlight.Value) OnFlightChangePolling();

      base.Awake();
    }

    public void Start()
    {
      Setup();
      UpdateLandVehicleHeightIfBelowGround();
    }

    public void SetHaulingVehicle(Player? player, RopeAnchorComponent? ropeComponent, bool isHauling)
    {
      distanceMovedSinceHaulTick = 0f;

      isPlayerHaulingVehicle = isHauling;
      HaulingPlayer = isHauling ? player : null;
      HaulingRopeComponent = isHauling ? ropeComponent : null;
    }

    // private void UpdateBreakingControls()
    // {
    //   if (WheelController == null) return;
    //   var isBreakingPressed = Input.GetKeyDown(KeyCode.Space);
    //   if (isBreakingPressed && !isHoldingBreak)
    //   {
    //     SendSetAnchor(WheelController.isBreaking ? AnchorState.Anchored : AnchorState.Recovered);
    //     isHoldingBreak = true;
    //   }
    //
    //   // we do not handle setting anchor state if it's still anchored and not breaking. This should only be handled if the break toggle is pressed.
    //   if (!isBreakingPressed)
    //   {
    //     isHoldingBreak = false;
    //   }
    // }

    private void Update()
    {
      if (VehicleInstance?.Instance == null) return;
      if (VehicleInstance.Instance.IsLandVehicleFromPrefab)
      {
        var hasControllingPlayer = HaveControllingPlayer();

        // Edge case but the player could be detached. This guard allows the next anchor click if returning to the controls.
        if (!hasControllingPlayer)
        {
          if (_isHoldingAnchor) _isHoldingAnchor = false;
          return;
        }

        OnAnchorKeyPress();
        // UpdateBreakingControls();
        return;
      }

      OnControllingWithHotKeyPress();

      // only should be run on Ships for now.
      // todo split updates between land and ships.
      AutoVerticalFlightUpdate();
    }

    private void OnEnable()
    {
      Instances.Add(this);
      MonoUpdaterInstances.Add(this);
      StartCoroutine(nameof(ShipFixRoutine));
    }

    private void OnDisable()
    {
      Instances.Remove(this);
      MonoUpdaterInstances.Remove(this);
      StopCoroutine(nameof(ShipFixRoutine));
    }

    private void OnDestroy()
    {
      if (_hasRegister) UnRegisterRPCListeners();

      RemovePlayersBeforeDestroyingBoat();

      CancelInvoke(nameof(SyncTargetHeight));
    }

    private void OnCollisionEnter(Collision collision)
    {
      if (PiecesController == null) return;
      if (collision.relativeVelocity.magnitude < 3f && collision.collider.transform.root.name.StartsWith(PrefabNames.LandVehicle) && collision.collider.transform.root != transform)
      {
        var rootNv = collision.collider.transform.root.GetComponent<ZNetView>();
        if (rootNv)
        {
          PiecesController.AddTemporaryPiece(rootNv);
        }
        return;
      }

      if (collision.collider.gameObject.layer == LayerHelpers.TerrainLayer) return;
      if (collision.collider.name == "tread_mesh")
      {
        // Physics.IgnoreCollision(collision.collider, collision.collider, true);
      }
      if (vehicleRam == null) return;
      var isCharacterLayer = collision.gameObject.layer == LayerHelpers.CharacterLayer;
      if (isCharacterLayer)
      {
#if DEBUG
        Logger.LogDebug("Hit character");
#endif
        vehicleRam.OnCollisionEnterHandler(collision);
      }
      else if (LayerHelpers.IsContainedWithinMask(collision.collider.gameObject.layer, LayerHelpers.PhysicalLayers))
      {
        vehicleRam.OnCollisionEnterHandler(collision);
      }
    }

    private void OnCollisionStay(Collision collision)
    {
      if (PiecesController == null) return;
      if (collision.transform.root == transform || collision.transform.root == PiecesController.transform)
      {
        // PiecesController.m_vehicleCollisionManager.AddColliderToVehicle(collision.collider, true);
        return;
      }

#if DEBUG
      // allows landvehicles within the vehicle, requires a PiecesController reparent likely.
      if (collision.relativeVelocity.magnitude < 2 && collision.transform.root.name.StartsWith(PrefabNames.LandVehicle) && collision.contactCount > 0)
      {
        // should only ignore for convexHull collider (not the damage trigger variant)
        var thisCollider = collision.contacts[0].thisCollider;
        if (thisCollider.name.StartsWith("ValheimVehicles_ConvexHull") || thisCollider.name.StartsWith("convex_tread_collider"))
        {
          Physics.IgnoreCollision(collision.collider, thisCollider, true);
        }
        if (transform.root.name.StartsWith(PrefabNames.LandVehicle))
        {
          PiecesController.AddTemporaryPiece(transform.root.GetComponent<ZNetView>());
        }
      }
#endif

      if (vehicleRam != null && LayerHelpers.IsContainedWithinMask(collision.collider.gameObject.layer, LayerHelpers.PhysicalLayers))
      {
        vehicleRam.OnCollisionEnterHandler(collision);
      }
    }

    private void OnTriggerEnter(Collider collider)
    {
      if (vehicleRam == null) return;
      if (collider.gameObject.layer == LayerHelpers.TerrainLayer) return;
      var colliderName = collider.name;
      vehicleRam.OnTriggerEnterHandler(collider);
    }

    public void CustomFixedUpdate(float deltaTime)
    {
      if (IsInvalid()) return;

      try
      {


        if (VehicleDebugConfig.AutoShowVehicleColliders.Value &&
            DebugTargetHeightObj != null)
        {
          DebugTargetHeightObj.transform.position =
            VectorUtils.MergeVectors(transform.position,
              Vector3.up * TargetHeight);
          DebugTargetHeightObj.transform.localScale = FloatCollider.size;
        }
        if (WheelController != null && !WheelController.IsVehicleReady)
        {
          InitLandVehicleWheels();
          m_body.isKinematic = true;
          return;
        }

        if (_vehicle != null && _vehicle
              .PiecesController != null && !_vehicle
              .PiecesController.isInitialActivationComplete)
        {
          m_body.isKinematic = true;
          return;
        }

        if (PropulsionConfig.AllowFlight.Value ||
            WaterConfig.WaterBallastEnabled.Value)
          SyncTargetHeight();

        UpdateShipWheelTurningSpeed();

        /*
         * creative mode should not allow movement, and applying force on an object will cause errors, when the object is kinematic
         */
        if (isCreative) return;
        if (!isPlayerHaulingVehicle && m_body.isKinematic)
        {
          m_body.isKinematic = false;
        }
        else if (isPlayerHaulingVehicle && HaulingPlayer != null && HaulingPlayer.transform.root == PiecesController.transform.root)
        {
          m_body.isKinematic = false;
        }

        VehicleMovementUpdatesOwnerOnly();
        VehiclePhysicsFixedUpdateAllClients();

      }
      catch (Exception e)
      {
        OnValidationFailure(e);
      }
    }

    /// <summary>
    ///   Unused, but required for IMonoUpdaters which Valheim uses to sync client and
    ///   server lifecycle updates
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="time"></param>
    public void CustomUpdate(float deltaTime, float time)
    {
      if (PiecesController == null) return;
      // PiecesController.Sync();
    }

    public void CustomLateUpdate(float deltaTime)
    {
      SyncShip();
    }

    public Transform m_controlGuiPos { get; set; }

    public void UpdateRudder(float dt, bool haveControllingPlayer)
    {
      if (!m_rudderObject) return;

      var b = Quaternion.Euler(0f,
        m_rudderRotationMax * (0f - m_rudderValue), 0f);
      if (haveControllingPlayer)
      {
        if (VehicleSpeed == Ship.Speed.Slow)
        {
          m_rudderPaddleTimer += dt;
          b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * 6f) * 20f,
            0f);
        }
        else if (VehicleSpeed == Ship.Speed.Back)
        {
          m_rudderPaddleTimer += dt;
          b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * -3f) * 40f,
            0f);
        }
      }

      m_rudderObject.transform.localRotation =
        Quaternion.Slerp(m_rudderObject.transform.localRotation, b, 0.5f);
    }

    public new float GetShipYawAngle()
    {
      var mainCamera = Utils.GetMainCamera();
      if (mainCamera == null) return 0f;

      return 0f -
             Utils.YawFromDirection(
               mainCamera.transform.InverseTransformDirection(ShipDirection
                 .forward));
    }

    public bool IsPlayerInBoat(Player player)
    {
      var currentPlayerOnBoat =
        OnboardController.m_localPlayers.Contains(player);
      if (currentPlayerOnBoat) return true;

      if (player.transform.root != null &&
          player.transform.root.name.Contains(PrefabNames
            .VehiclePiecesContainer))
        return true;

      return WaterZoneUtils.IsOnboard(player);
    }

    public bool IsPlayerInBoat(long playerId)
    {
      var playerFromId = Player.GetPlayer(playerId);
      return playerFromId != null && IsPlayerInBoat(playerFromId);
    }

    public bool IsPlayerInBoat(ZDOID zdoid)
    {
      foreach (var player in OnboardController.m_localPlayers)
        if (player.GetZDOID() == zdoid)
          return true;

      return false;
    }

    public float GetWindAngle()
    {
      // moder power support
      var isWindPowerActive = IsWindControllActive();

      var windDir = isWindPowerActive
        ? ShipDirection.forward
        : EnvMan.instance.GetWindDir();
      return 0f -
             Utils.YawFromDirection(
               ShipDirection.InverseTransformDirection(windDir));
    }

    float IValheimShip.GetWindAngleFactor()
    {
      return GetWindAngleFactor();
    }

    public void Forward()
    {
      SendSpeedChange(DirectionChange.Forward);
    }

    public void Backward()
    {
      SendSpeedChange(DirectionChange.Backward);
    }

    public void Stop()
    {
      SendSpeedChange(DirectionChange.Stop);
    }

    public void UpdateControlls(float dt)
    {
      UpdateControls(dt);
    }

    public Ship.Speed GetSpeedSetting()
    {
      return vehicleSpeed;
    }

    public float GetRudderValue()
    {
      return m_rudderValue;
    }

    public float GetRudder()
    {
      return m_rudder;
    }

    /// <summary>
    ///   Handles updating direction controls, update Controls is called within the
    ///   FixedUpdate of VehicleShip
    /// </summary>
    /// <param name="dt"></param>
    public void UpdateControls(float dt)
    {
      if (m_nview.IsOwner())
      {
        m_nview.GetZDO().Set(ZDOVars.s_forward, (int)vehicleSpeed);
        m_nview.GetZDO().Set(ZDOVars.s_rudder, m_rudderValue);
        return;
      }

      if (Time.time - m_sendRudderTime > 1f)
      {
        if (HasPendingAnchor) return;
        vehicleSpeed = (Ship.Speed)m_nview.GetZDO().GetInt(ZDOVars.s_forward);
        m_rudderValue = m_nview.GetZDO().GetFloat(ZDOVars.s_rudder);
      }

      if (WheelController != null)
      {
        WheelController.SetTurnInput(m_rudderValue);
        UpdateLandVehicleStatsIfNecessary();
      }
    }

    public void ApplyControlls(Vector3 dir)
    {
      ApplyControls(dir);
    }

    /// <summary>
    ///   Updates based on the controls provided
    /// </summary>
    /// <param name="dir"></param>
    public void ApplyControls(Vector3 dir)
    {
      var isForward = dir.z > 0.5;
      var isBackward = dir.z < -0.5;

      if (isForward && !m_forwardPressed)
        SendSpeedChange(DirectionChange.Forward);

      if (isBackward && !m_backwardPressed)
        SendSpeedChange(DirectionChange.Backward);

      var fixedDeltaTime = Time.fixedDeltaTime;
      var num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(m_rudderValue));
      m_rudder = dir.x * num;
      m_rudderValue += m_rudder * m_rudderSpeed * fixedDeltaTime;
      m_rudderValue = Mathf.Clamp(m_rudderValue, -1f, 1f);

      if (Time.time - m_sendRudderTime > 0.2f)
      {
        // deadzone logic to allow rudder to be centered.
        // allows updating rudder but zeros it out quickly in a deadzone.
        if (IsWithinWheelDeadZone()) m_rudderValue = 0.0f;

        m_sendRudderTime = Time.time;
        SyncRudder(m_rudderValue);
      }

      m_forwardPressed = isForward;
      m_backwardPressed = isBackward;
    }

    public float TargetHeight { get; private set; }


    public void Descend()
    {
      UpdateAnchorOnVertical();

      if (!PropulsionConfig.AllowFlight.Value &&
          !WaterConfig.WaterBallastEnabled.Value)
      {
        UpdateTargetHeight(0f);
      }
      else
      {
        if (!FloatCollider) return;

        var maxVerticalOffset = GetMaxVerticalOffset();
        UpdateTargetHeight(TargetHeight - maxVerticalOffset);
      }
    }

    public void Ascend()
    {
      UpdateAnchorOnVertical();

      if (IsBallastAndFlightDisabled)
      {
        UpdateTargetHeight(0f);
      }
      else
      {
        if (!FloatCollider) return;

        var maxVerticalOffset = GetMaxVerticalOffset();

        UpdateTargetHeight(TargetHeight + maxVerticalOffset);
      }
    }

    public void SendSetAnchor(AnchorState state)
    {
      if (_isHoldingAnchor)
      {
        Logger.LogDebug(
          $"skipped due to IsUpdatingAnchorState: {_isHoldingAnchor}");
        return;
      }

      if (m_nview.GetZDO().GetInt(VehicleZdoVars.VehicleAnchorState) ==
          (int)state)
        return;

      if (HasPendingAnchor)
      {
        // Might need to rethink this if it's heavy performance hit. Maybe a coroutine if calling cancel invoke is constant.
        CancelInvoke(nameof(DelayedAnchor));
        HasPendingAnchor = false;
      }

      SetAnchor(state);
      if (state == AnchorState.Anchored) SendSpeedChange(DirectionChange.Stop);

      m_nview.InvokeRPC(nameof(RPC_SetAnchor), (int)state);
    }

    public VehiclePhysicsState GetCachedVehiclePhysicsState()
    {
      return _previousVehiclePhysicsState;
    }

    public void StartPlayerCollisionAfterTeleport(Collider collider, Character character)
    {
      StartCoroutine(EnableCollisionAfterTeleport(PiecesController.convexHullMeshColliders, collider, character));
    }

    public IEnumerator EnableCollisionAfterTeleport(List<MeshCollider> IgnoredColliders, Collider collisionCollider, Character? character)

    {
      foreach (var collider in IgnoredColliders)
      {
        Physics.IgnoreCollision(collider, collisionCollider, true);
      }
      var isTeleporting = true;
      while (isTeleporting)
      {
        isTeleporting = character.IsTeleporting();
        if (!isTeleporting) break;
        yield return new WaitForFixedUpdate();
      }
      var isCharacterOnboard = !WaterZoneUtils.IsOnboard(character);
      if (isCharacterOnboard)
      {
        foreach (var collider in IgnoredColliders)
        {
          Physics.IgnoreCollision(collider, collisionCollider, false);
        }
      }
    }

    public static bool IsAnchorDropped(AnchorState val)
    {
      return val == AnchorState.Anchored;
    }

    public ShipFloatation? GetShipFloatation()
    {
      return _currentShipFloatation;
    }

    /// <summary>
    ///   Reels the anchor on vertical commands.
    /// </summary>
    public void UpdateAnchorOnVertical()
    {
      if (isAnchored && vehicleAnchorState != AnchorState.Reeling)
        SendSetAnchor(AnchorState.Reeling);
    }

    // unfortunately, the current approach does not allow increasing this beyond 1f otherwise it causes massive jitters when changing altitude.
    private float GetMaxVerticalOffset()
    {
      if (PropulsionConfig.AllowFlight.Value)
        if (IsFlying() ||
            OnboardCollider.bounds.max.y > ZoneSystem.instance.m_waterLevel ||
            TargetHeight - 2f > GetSurfaceOffsetWaterVehicleOnly())
          return PropulsionConfig.FlightClimbingOffset.Value;

      return PropulsionConfig.BallastClimbingOffset.Value;
    }

    public Rigidbody GetRigidbody()
    {
      if (m_body) return m_body;
      if (!m_body) m_body = GetComponent<Rigidbody>();

      return m_body;
    }

    /// <summary>
    ///   Removes player from boat if not null, disconnects can make the player null
    /// </summary>
    private void RemovePlayersBeforeDestroyingBoat()
    {
      if (OnboardController == null) return;
      foreach (var mPlayer in OnboardController.m_localPlayers.ToList())
      {
        if (mPlayer == null) continue;
        mPlayer.transform.SetParent(null);
      }

      OnboardController.m_localPlayers.Clear();
    }

    /// <summary>
    ///   Sets the rudderForce and returns it's value
    /// </summary>
    /// noting that rudderforce must be negative when speed is Backwards
    /// <returns></returns>
    private float GetRudderForcePerSpeed()
    {
      if (!ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
      {
        _rudderForce = 1f;
        return _rudderForce;
      }

      switch (VehicleSpeed)
      {
        case Ship.Speed.Stop:
          _rudderForce = 0f;
          break;
        case Ship.Speed.Back:
          _rudderForce =
            Mathf.Clamp(PhysicsConfig.VehicleRudderSpeedBack.Value,
              0,
              PhysicsConfig.MaxLinearVelocity.Value);
          break;
        case Ship.Speed.Slow:
          _rudderForce = Mathf.Clamp(
            PhysicsConfig.VehicleRudderSpeedSlow.Value, 0,
            PhysicsConfig.MaxLinearVelocity.Value);
          break;
        case Ship.Speed.Half:
          _rudderForce = Mathf.Clamp(
            PhysicsConfig.VehicleRudderSpeedHalf.Value, 0,
            PhysicsConfig.MaxLinearVelocity.Value);
          break;
        case Ship.Speed.Full:
          _rudderForce = Mathf.Clamp(
            PhysicsConfig.VehicleRudderSpeedFull.Value, 0,
            PhysicsConfig.MaxLinearVelocity.Value);
          break;
        default:
          Logger.LogError(
            $"Speed value could not handle this variant, {VehicleSpeed}");
          _rudderForce = 1f;
          break;
      }

      return _rudderForce;
    }

    /**
     * adds guard for when ship controls do not exist on the ship.
     * - previous ship would assume m_shipControlls was connected because it was part of the base prefab
     */
    public bool HaveControllingPlayer()
    {
      if (OnboardController == null) return false;
      if (OnboardController.m_localPlayers.Count != 0)
        return HaveValidUser();

      return false;
    }

    /// <summary>
    ///   caps the vehicle speeds to these values
    /// </summary>
    public void UpdateVehicleSpeedThrottle()
    {
      if (!_vehicle) return;
      maxYLinearVelocity = PhysicsConfig.MaxLinearYVelocity.Value;
      m_body.maxLinearVelocity = PhysicsConfig.MaxLinearVelocity.Value;

      if (_vehicle!.IsLandVehicle)
      {
        // heavily throttle turning to prevent infinite spin issues especially uphill.
        m_body.maxAngularVelocity = Mathf.Min(PhysicsConfig.MaxAngularVelocity.Value, 0.3f);
      }
      else
      {
        m_body.maxAngularVelocity = PhysicsConfig.MaxAngularVelocity.Value;
      }
    }

    /// <summary>
    /// Meant to be called for the owner's physics so the velocity never exceeds a specific value.
    ///
    /// This will only update the Y velocity if it exceeds the values 
    /// </summary>
    public void ClampVehicleAcceleration()
    {
      var currentVelocity = m_body.velocity;

      // if anchored the vehicle will not raise/lower as much.
      var dynamicYLinearVelocity =
        isAnchored ? maxYLinearVelocity * 0.1f : maxYLinearVelocity;

      if (!(Mathf.Abs(currentVelocity.y) > dynamicYLinearVelocity)) return;
      // Clamp Y velocity while keeping X and Z unaffected
      currentVelocity.y = Mathf.Sign(currentVelocity.y) * dynamicYLinearVelocity;

      m_body.velocity = currentVelocity;
    }

    /// <summary>
    /// TODO move all colliders into pieces controller. And only keep onboard collider. Other colliders are not needed now with generated meshes 
    /// </summary>
    public void InitColliders()
    {
      if (PiecesController == null) return;
      var vehicleMovementObj = VehicleShip.GetVehicleMovementTransform(transform);
      ShipDirection =
        vehicleMovementObj.transform.Find(PrefabNames
          .VehicleShipMovementOrientation);
    }

    public void SetupImpactEffect()
    {
      _impactEffect = GetComponent<ImpactEffect>();

      // fallback assignment
      if (!_impactEffect)
      {
        _impactEffect = gameObject.AddComponent<ImpactEffect>();
        _impactEffect.m_triggerMask = LayerHelpers.PhysicalLayers;
        _impactEffect.m_toolTier = 1000;
      }

      _impactEffect.m_nview = m_nview;
      _impactEffect.m_body = m_body;
      _impactEffect.m_hitType = HitData.HitType.Boat;
      _impactEffect.m_interval = 0.5f;
      _impactEffect.m_minVelocity = 0.1f;
    }

    /**
     * A performant way to guard against ship problems
     */
    private IEnumerator ShipFixRoutine()
    {
      while (isActiveAndEnabled)
      {
        yield return FixShipRotation();
        // FixShipPosition();
        yield return new WaitForSeconds(5f);
      }
    }

    public void SetupPhysicsSync()
    {
      if (!zsyncTransform)
        zsyncTransform =
          GetComponent<VehicleZSyncTransform>();

      if (zsyncTransform) zsyncTransform.m_body = GetRigidbody();
    }

    /// <summary>
    ///   Meant to change physics targets when config is updated
    /// </summary>
    /// <param name="val"></param>
    public static void SetPhysicsSyncTarget(VehiclePhysicsMode val)
    {
      // PhysicsSyncTarget = val == VehiclePhysicsMode.DesyncedJointRigidbodyBody
      //   ? PhysicsTarget.VehiclePieces
      // : PhysicsTarget.VehicleShip;
      PhysicsSyncTarget = PhysicsTarget.VehicleShip;
      foreach (var vehicleMovementController in Instances)
        vehicleMovementController.SetupPhysicsSync();
    }

    public void SetupZsyncTransform()
    {
      zsyncTransform = GetComponent<VehicleZSyncTransform>();
      if (zsyncTransform == null) return;
      zsyncTransform.m_syncPosition = true;
      zsyncTransform.m_syncBodyVelocity = true;
      zsyncTransform.m_syncRotation = true;
    }

    public void AwakeSetupShipComponents()
    {
      _vehicle = GetComponent<VehicleShip>();
      ShipDirection = transform.Find(
        $"vehicle_movement/{PrefabNames.VehicleShipMovementOrientation}");

      GetRigidbody();
      SetupZsyncTransform();
      SetupPhysicsSync();
      // SetupImpactEffect();
      InitColliders();
      UpdateVehicleSpeedThrottle();

      if (!(bool)m_mastObject)
        m_mastObject = new GameObject
        {
          name = PrefabNames.VehicleSailMast,
          transform = { parent = transform }
        };

      if (!(bool)m_sailObject)
        m_sailObject = new GameObject
        {
          name = PrefabNames.VehicleSail,
          transform = { parent = transform }
        };
    }

    /// <summary>
    /// Flips the ship, and also protects the players from being launched into space by the flip.
    /// </summary>
    /// <returns></returns>
    public IEnumerator FixShipRotation()
    {
      if (_vehicle == null) yield break;
      var eulerAngles = transform.rotation.eulerAngles;
      var eulerX = eulerAngles.x;
      var eulerY = eulerAngles.y;
      var eulerZ = eulerAngles.z;

      var transformedX = eulerX;
      var transformedZ = eulerZ;
      var shouldUpdate = false;

      if (_vehicle.IsLandVehicle)
      {
        if (Mathf.Abs(eulerX) is > 90 and < 270)
        {
          transformedX = 0;
          shouldUpdate = true;
        }

        if (Mathf.Abs(eulerZ) is > 90 and < 270)
        {
          transformedZ = 0;
          shouldUpdate = true;
        }
      }
      else
      {
        if (Mathf.Abs(eulerX) is > 65 and < 295)
        {
          transformedX = 0;
          shouldUpdate = true;
        }

        if (Mathf.Abs(eulerZ) is > 65 and < 295)
        {
          transformedZ = 0;
          shouldUpdate = true;
        }
      }

      if (shouldUpdate)
      {
        List<Player> onboardPlayers = [];
        List<Player> modifiedKinematicPlayers = [];
        if (OnboardController != null)
        {
          onboardPlayers = OnboardController.m_localPlayers.ToList();
          foreach (var onboardPlayer in onboardPlayers)
            if (!onboardPlayer.IsAttached())
            {
              onboardPlayer.m_body.isKinematic = true;
              modifiedKinematicPlayers.Add(onboardPlayer);
            }
        }
        transform.rotation =
          Quaternion.Euler(transformedX, eulerY, transformedZ);
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;


        Physics.SyncTransforms();
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;

        foreach (var modifiedKinematicPlayer in modifiedKinematicPlayers)
          if (modifiedKinematicPlayer.m_body.isKinematic)
          {
            modifiedKinematicPlayer.m_body.isKinematic = false;
            modifiedKinematicPlayer.m_body.velocity = Vector3.zero;
          }

        onboardPlayers.Clear();
        modifiedKinematicPlayers.Clear();
      }

      yield return null;
    }


    /// <summary>
    /// This is required for preventing the vehicle from colliding with any piece
    /// </summary>
    private void UpdateRemovePieceCollisionExclusions()
    {
      if (!isActiveAndEnabled)
      {
        return;
      }
      if (PiecesController == null || PiecesController.IsInvalid())
      {
        Invoke(nameof(UpdateRemovePieceCollisionExclusions), 5f);
        return;
      }
      
      var excludedLayers = LayerMask.GetMask("piece_nonsolid");

      if (!m_body) GetRigidbody();

      if (m_body)
      {
        m_body.includeLayers = LayerHelpers.PhysicalLayers;
        m_body.excludeLayers = excludedLayers;
      }
    }

    public void Setup()
    {
      // this delay is added to prevent added items from causing collisions in the brief moment they are not ignoring collisions.
      Invoke(nameof(UpdateRemovePieceCollisionExclusions), 5f);

      if (!m_nview) m_nview = GetComponent<ZNetView>();

      if (!m_body) m_body = GetComponent<Rigidbody>();

      var zdoAnchorState =
        (AnchorState)m_nview.GetZDO()
          .GetInt(VehicleZdoVars.VehicleAnchorState);
      vehicleAnchorState = zdoAnchorState;

      StartCoroutine(ShipFixRoutine());

      InitializeRPC();
      SyncShip();
    }

    public void DEBUG_VisualizeFloatPoint()
    {
      DebugTargetHeightObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
      DebugTargetHeightObj.name = "DEBUG_TargetHeightObj";
      DebugTargetHeightObj.transform.SetParent(transform);

      var meshRenderer = DebugTargetHeightObj.GetComponent<MeshRenderer>();
      var meshFilter = DebugTargetHeightObj.GetComponent<MeshFilter>();
      Destroy(meshRenderer);
      Destroy(meshFilter);
      DebugTargetHeightObj.layer = LayerHelpers.NonSolidLayer;

      DebugTargetHeightObj.transform.rotation = FloatCollider.transform.rotation;
      DebugTargetHeightObj.transform.localScale =
        FloatCollider.size;
    }

    public string GetVehicleMapKey()
    {
      return $"{vehicleKeyPrefix}_{m_nview.GetZDO().GetOwner()}";
    }

    public void UpdateVehicleLocation(float deltaTime)
    {
      if (lastPositionUpdate < 3)
        lastPositionUpdate += deltaTime;
      else
        lastPositionUpdate = deltaTime;

      if (Vector3.Distance(lastPosition, transform.position) < 3f) return;

      var mode = Minimap.m_instance.m_mode;
      if (vehicleMapKey != "")
        ZoneSystem.m_instance.RemoveGlobalKey(vehicleMapKey);

      vehicleMapKey = GetVehicleMapKey();

      if (vehicleMapKey != "")
      {
        ZoneSystem.m_instance.SetGlobalKey(vehicleMapKey);
        Minimap.m_instance.SetMapMode(mode);
      }
    }

    /// <summary>
    /// Must call Physics Sync Transforms
    /// </summary>
    /// <param name="steeringWheelRotation"></param>
    public void UpdateShipDirection(Quaternion steeringWheelRotation)
    {
      var rotation = Quaternion.Euler(0, steeringWheelRotation.eulerAngles.y, 0);
      if (ShipDirection == null)
      {
        ShipDirection = transform;
        return;
      }
      if (ShipDirection.localRotation.Equals(rotation)) return;
      // for some reason we have to rotate these parent colliders by half and the child again by half.
      ShipDirection.localRotation = rotation;
    }
    private Vector3 CalculateAnchorStopVelocity(Vector3 currentVelocity, float smoothTime = 2f)
    {
      return Vector3.SmoothDamp(currentVelocity, Vector3.zero,
        ref anchorSmoothVelocity, smoothTime);
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position,
      ForceMode forceMode)
    {
      m_body.AddForceAtPosition(force, position, forceMode);
    }

    /// <summary>
    /// Very inaccurate. Needs to be updated to use a multiplication for forward position.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public float GetFloatSizeFromDirection(Vector3 direction)
    {
#if DEBUG
      if (floatSizeOverride != 0f) return floatSizeOverride;
#endif
      if (PiecesController == null) return 0f;

      var bounds = PiecesController.convexHullComponent.GetConvexHullBounds(true);

      // var isRotated90Degrees = Mathf.Approximately(Mathf.Abs(ShipDirection.localRotation.eulerAngles.y), 90f) || Mathf.Approximately(Mathf.Abs(ShipDirection.localRotation.eulerAngles.y), 270f);

      if (direction == Vector3.right)
      {
        // if (isRotated90Degrees)
        // {
        //   return bounds.extents.z;
        // }
        return bounds.extents.x;
      }

      // if (isRotated90Degrees)
      // {
      //   return bounds.extents.x;
      // }
      return bounds.extents.z;
    }

    private float GetHighestGroundPoint(ShipFloatation shipFloatation)
    {
      // Create an array of ground levels
      float[] groundLevels =
      [
        shipFloatation.GroundLevelCenter,
        shipFloatation.GroundLevelBack,
        shipFloatation.GroundLevelForward,
        shipFloatation.GroundLevelLeft,
        shipFloatation.GroundLevelRight
      ];
      _lastHighestGroundPoint = groundLevels.Max();
      // Use LINQ to find the maximum value
      return _lastHighestGroundPoint;
    }

    public static void UpdateYPosition(Transform targetTransform,
      float newYPosition)
    {
      var localPosition = targetTransform.localPosition;
      targetTransform.localPosition =
        new Vector3(localPosition.x, newYPosition, localPosition.z);
    }

    /// <summary>
    ///   This functionality adds guards for target height. If it detects the vehicle
    ///   in a stuck state or nearing the bottom where it could collide it will push
    ///   the vehicle upwards.
    /// </summary>
    private void UpdateColliderPositions()
    {
      if (PiecesController == null) return;
      var convexHullRelativeBounds =
        PiecesController.convexHullComponent.GetConvexHullBounds(true);

      // var expectedLowestBlockingColliderPoint =
      //   BlockingCollider.transform.position.y - BlockingCollider.bounds.extents.y;
      var expectedLowestBlockingColliderPoint =
        PiecesController.transform.position.y +
        convexHullRelativeBounds.min.y;

      if (IsFlying() || !WaterConfig.WaterBallastEnabled.Value) return;

      // ForceUpdates TargetPosition in case the blocking collider is below the ground. This would cause issues, as the vehicle would then not be able to ascend properly.
      if (_lastHighestGroundPoint > expectedLowestBlockingColliderPoint)
      {
        // will be a positive number.
        var heightDifference = _lastHighestGroundPoint -
                               expectedLowestBlockingColliderPoint;
        // we force update it.
        UpdateTargetHeight(TargetHeight + heightDifference);
      }

      // ForceUpdateIfBelowGround, this can happen if driving the vehicle forwards into the ground.
      // Prevents the ship from exceeding the lowest height above the water
      // if (_lastHighestGroundPoint > OnboardCollider.bounds.min.y)
      // {
      //   // will be a positive number.
      //   var heightDifference = _lastHighestGroundPoint -
      //                          OnboardCollider.bounds.min.y;
      //   // we force update it.
      //   UpdateTargetHeight(TargetHeight + heightDifference);
      // }

      // super stuck. do a direct update. But protect the players from being launched. Yikes.
      if (_lastHighestGroundPoint > PiecesController.transform.position.y +
          convexHullRelativeBounds.center.y)
      {
        // force updates the vehicle to this position.
        var position = transform.position;
        position = new Vector3(position.x,
          position.y + (_lastHighestGroundPoint -
                        convexHullRelativeBounds.center.y),
          position.z);
        transform.position = position;
        UpdateTargetHeight(0);
      }
    }

    /// <summary>
    ///   Abstraction in case this needs padding to prevent conflict with ballast
    ///   height.
    ///   - Adds waterlevel offset to target height as increasing this height should be
    ///   the target height + waterheight
    /// </summary>
    /// <returns></returns>
    public float GetFlyingTargetHeight()
    {
      return TargetHeight;
      // if (TargetHeight > 30f)
      // {
      //   return TargetHeight;
      // }
      //
      // return BlockingCollider.transform.position.y;
    }

    public void Flying_UpdateShipBalancingForce()
    {
      var centerPosition = ShipDirection.position;
      var shipForward = ShipDirection.forward;
      var shipRight = ShipDirection.right;

      var front = centerPosition +
                  shipForward * GetFloatSizeFromDirection(shipForward);
      var back = centerPosition -
                 shipForward * GetFloatSizeFromDirection(shipForward);
      var left = centerPosition -
                 shipRight * GetFloatSizeFromDirection(shipRight);
      var right = centerPosition +
                  shipRight * GetFloatSizeFromDirection(shipRight);

      var frontForce = m_body.GetPointVelocity(front);
      var backForce = m_body.GetPointVelocity(back);
      var leftForce = m_body.GetPointVelocity(left);
      var rightForce = m_body.GetPointVelocity(right);

      var flyingTargetHeight = GetFlyingTargetHeight();

      // Calculate the target upwards forces for each position
      var frontUpwardsForce = GetUpwardsForce(
        flyingTargetHeight,
        front.y + frontForce.y, m_balanceForce);
      var backUpwardsForce = GetUpwardsForce(
        flyingTargetHeight,
        back.y + backForce.y, m_balanceForce);
      var leftUpwardsForce = GetUpwardsForce(
        flyingTargetHeight,
        left.y + leftForce.y, m_balanceForce);
      var rightUpwardsForce = GetUpwardsForce(
        flyingTargetHeight,
        right.y + rightForce.y, m_balanceForce);
      var centerUpwardsForce = GetUpwardsForce(
        flyingTargetHeight,
        centerPosition.y + m_body.velocity.y, m_liftForce);

      // Smooth time must always be greater than 3f
      var smoothValue = 3f + PropulsionConfig.VerticalSmoothingSpeed.Value * 10f;

      // Smoothly transition the forces towards the target values using SmoothDamp
      frontUpwardsForce = Mathf.SmoothDamp(prevFrontUpwardForce,
        frontUpwardsForce, ref prevFrontUpwardForce,
        smoothValue);
      backUpwardsForce = Mathf.SmoothDamp(prevBackUpwardsForce, backUpwardsForce,
        ref prevBackUpwardsForce, smoothValue);
      leftUpwardsForce = Mathf.SmoothDamp(prevLeftUpwardsForce, leftUpwardsForce,
        ref prevLeftUpwardsForce, smoothValue);
      rightUpwardsForce = Mathf.SmoothDamp(prevRightUpwardsForce,
        rightUpwardsForce, ref prevRightUpwardsForce,
        smoothValue);
      centerUpwardsForce = Mathf.SmoothDamp(prevCenterUpwardsForce,
        centerUpwardsForce, ref prevCenterUpwardsForce,
        smoothValue);

      // Apply the smoothed forces at the corresponding positions
      AddForceAtPosition(Vector3.up * frontUpwardsForce, front,
        PhysicsConfig.flyingVelocityMode.Value);
      AddForceAtPosition(Vector3.up * backUpwardsForce, back,
        PhysicsConfig.flyingVelocityMode.Value);
      AddForceAtPosition(Vector3.up * leftUpwardsForce, left,
        PhysicsConfig.flyingVelocityMode.Value);
      AddForceAtPosition(Vector3.up * rightUpwardsForce, right,
        PhysicsConfig.flyingVelocityMode.Value);
      AddForceAtPosition(Vector3.up * centerUpwardsForce, centerPosition,
        PhysicsConfig.flyingVelocityMode.Value);
    }

    /// <summary>
    ///   Forward velocity relative to the ShipDirection instead of the rigidbody
    /// </summary>
    /// <returns></returns>
    public float GetForwardVelocity()
    {
      var velocity = m_body.velocity;

      // Convert the velocity from world space to the local space of ShipDirection
      var localVelocity = ShipDirection.InverseTransformDirection(velocity);

      // Get the forward velocity relative to ShipDirection
      var forwardVelocity = Vector3.Dot(localVelocity, Vector3.forward);
      return forwardVelocity;
    }

    public void UpdateAndFreezeRotation()
    {
      var isApproxZeroX = Mathf.Approximately(m_body.rotation.eulerAngles.x, 0);
      var isApproxZeroZ = Mathf.Approximately(m_body.rotation.eulerAngles.z, 0);

      if (!isApproxZeroX || !isApproxZeroZ)
      {
        m_body.constraints = RigidbodyConstraints.None;
        var newRotation = Quaternion.Euler(0, m_body.rotation.eulerAngles.y, 0);
        m_body.MoveRotation(newRotation);
      }

      if (m_body.constraints != FreezeBothXZ) m_body.constraints = FreezeBothXZ;
    }

    public void UpdateFlyingVehicle()
    {
      UpdateVehicleStats(VehiclePhysicsState.Air);
      UpdateAndFreezeRotation();
      // early exit if anchored.
      if (UpdateAnchorVelocity()) return;

      m_body.WakeUp();
      Flying_UpdateShipBalancingForce();

      if (!PropulsionConfig.FlightHasRudderOnly.Value)
        ApplySailForce(this, true);
    }

    public float GetLandVehicleSpeedInput()
    {
      if (isAnchored)
      {
        return 0;
      }

      return vehicleSpeed switch
      {
        Ship.Speed.Stop => 0,
        Ship.Speed.Back => -PhysicsConfig.VehicleLandSpeedBack.Value,
        Ship.Speed.Slow => PhysicsConfig.VehicleLandSpeedSlow.Value,
        Ship.Speed.Half => PhysicsConfig.VehicleLandSpeedHalf.Value,
        Ship.Speed.Full => PhysicsConfig.VehicleLandSpeedFull.Value,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public VehicleWheelController.AccelerationType GetLandVehicleSpeed()
    {
      if (isAnchored)
      {
        return 0;
      }

      if (isAnchored)
      {
        return VehicleWheelController.AccelerationType.Stop;
      }

      return vehicleSpeed switch
      {
        Ship.Speed.Stop => VehicleWheelController.AccelerationType.Stop,
        Ship.Speed.Back => VehicleWheelController.AccelerationType.Low,
        Ship.Speed.Slow => VehicleWheelController.AccelerationType.Low,
        Ship.Speed.Half => VehicleWheelController.AccelerationType.Medium,
        Ship.Speed.Full => VehicleWheelController.AccelerationType.High,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public void InitLandVehicleWheels()
    {
      if (WheelController == null || PiecesController == null) return;
      if (WheelController.wheelColliders.Count == 0)
      {
        m_body.Sleep();
        m_body.isKinematic = true;
        var bounds = PiecesController.convexHullComponent.GetConvexHullBounds(true);

        WheelController.Initialize(bounds);
      }
    }
    public void UpdateCenterOfMass()
    {
      if (PiecesController == null) return;
      // resets the mass to center. But then we will offset it if there is an offset provided.
      m_body.automaticCenterOfMass = true;
      m_body.ResetCenterOfMass();
      vehicleAutomaticCenterOfMassPoint = m_body.centerOfMass;

      if (Mathf.Approximately(PhysicsConfig.VehicleCenterOfMassOffset.Value, 0f)) return;

      var centerOfMass = m_body.centerOfMass;
      var convexHullBounds = PiecesController.convexHullComponent.GetConvexHullBounds(true);

      var offset = PhysicsConfig.VehicleCenterOfMassOffset.Value * convexHullBounds.size.y;

      // adds a fake offset until bounds are larger to stabilize a new vehicle
      if (convexHullBounds.size.y < 4f)
      {
        offset = 4f;
      }

      localCenterOfMassOffset = offset;

      if (!(offset > 0.1f)) return;
      centerOfMass.y -= offset;
      m_body.centerOfMass = centerOfMass;
      m_body.automaticCenterOfMass = false;
    }
    public void UpdateLandVehicleStats()
    {
      m_body.angularDrag = PhysicsConfig.landAngularDrag.Value;
      m_body.drag = PhysicsConfig.landDrag.Value;

      // heavily throttle turning to prevent infinite spin issues especially uphill.
      m_body.maxAngularVelocity = Mathf.Min(PhysicsConfig.MaxAngularVelocity.Value, 0.3f);
      m_body.maxLinearVelocity = PhysicsConfig.MaxLinearVelocity.Value;

#if DEBUG
      if (PhysicsConfig.VehicleLandLockXZRotation.Value)
      {
        UpdateAndFreezeRotation();
      }
      else
      {
        if (m_body.constraints == FreezeBothXZ)
          m_body.constraints = RigidbodyConstraints.None;
      }
#endif
    }

    /// <summary>
    /// TODO this logic is meant to protect vehicles from going underground. But it was triggering a bit too much.
    ///
    /// TODO might need to move this to treads as these treads should be above the land.
    /// </summary>
    public void UpdateLandVehicleHeightIfBelowGround()
    {
      if (OnboardCollider.bounds.min.y + 2f < ShipFloatationObj.AverageGroundLevel)
      {
        var position = transform.position;

        // var lerpedMovement = Mathf.Lerp(OnboardCollider.bounds.min.y, ShipFloatationObj.AverageGroundLevel, Time.fixedDeltaTime);
        m_body.MovePosition(new Vector3(position.x, ShipFloatationObj.AverageGroundLevel + 3f, position.z));
        m_body.velocity = Vector3.zero;
        m_body.angularVelocity = Vector3.zero;
      }
    }

    public void UpdateVehicleLandSpeed()
    {
      UpdateVehicleStats(VehiclePhysicsState.Land);
      if (WheelController == null) return;
      if (UpdateAnchorVelocity())
      {
        if (!WheelController.IsBraking)
        {
          WheelController.SetBrake(true);
        }
      }

      var isOwner = m_nview.IsOwner();
      if (!isOwner) return;

      if (shouldUpdateLandInputs)
      {
        UpdateLandVehicleStatsIfNecessary();
        WheelController.VehicleMovementFixedUpdateOwnerClient();
      }
    }

    public void UpdateLandVehicleStatsIfNecessary()
    {
      if (WheelController == null) return;
      var landSpeed = GetLandVehicleSpeed();
      var isForward = VehicleSpeed != Ship.Speed.Back;
      var landInputMovementMultiplier = GetLandVehicleSpeedInput();
      WheelController!.inputMovement = landInputMovementMultiplier;
      if (landSpeed != WheelController!.accelerationType || WheelController.isForward != isForward)
      {
        WheelController.forwardDirection = ShipDirection;
        WheelController.UpdateAccelerationValues(landSpeed, isForward);
      }
    }

    private void UpdateFlightStats()
    {
      m_angularDamping = PhysicsConfig.flightSteerForce.Value;
      m_damping = PhysicsConfig.flightDamping.Value;
      m_dampingSideway = PhysicsConfig.flightSidewaysDamping.Value;
      m_sailForceFactor = PhysicsConfig.flightSailForceFactor.Value;
      m_stearForce = PhysicsConfig.flightSteerForce.Value;

      var drag = PhysicsConfig.flightDrag.Value;
      var angularDrag = PhysicsConfig.flightAngularDrag.Value;

      VehicleInstance?.PiecesController?.SyncRigidbodyStats(drag, angularDrag,
        true);
    }

    public void UpdateSubmergedStats()
    {
      m_angularDamping =
        PhysicsConfig.submersibleAngularDamping.Value;
      m_damping = PhysicsConfig.submersibleDamping.Value;
      m_dampingSideway = PhysicsConfig.submersibleSidewaysDamping.Value;
      m_sailForceFactor = PhysicsConfig.submersibleSailForceFactor.Value;
      m_stearForce = PhysicsConfig.submersibleSteerForce.Value;

      var drag = PhysicsConfig.submersibleDrag.Value;
      var angularDrag = PhysicsConfig.submersibleAngularDrag.Value;

      VehicleInstance?.PiecesController?.SyncRigidbodyStats(drag, angularDrag,
        false);
    }

    public void UpdateWaterStats()
    {
      m_angularDamping = PhysicsConfig.waterAngularDamping.Value;
      m_damping = PhysicsConfig.waterDamping.Value;
      m_dampingSideway = PhysicsConfig.waterSidewaysDamping.Value;
      m_sailForceFactor = PhysicsConfig.waterSailForceFactor.Value;
      m_stearForce = PhysicsConfig.waterSteerForce.Value;

      var drag = PhysicsConfig.waterDrag.Value;
      var angularDrag = PhysicsConfig.waterAngularDrag.Value;

      VehicleInstance?.PiecesController?.SyncRigidbodyStats(drag, angularDrag,
        false);
    }

    /// <summary>
    ///   Updates all physics stats. Calls alot but should not do much
    /// </summary>
    /// Todo debounce this when values do not change
    /// <param name="flight"></param>
    /// <param name="submerged"></param>
    /// <param name="forceUpdate">
    ///   Used to force update physics when values for physics
    ///   properties change
    /// </param>
    public void UpdateVehicleStats(VehiclePhysicsState currentPhysicsState,
      bool forceUpdate = false)
    {
      if (!forceUpdate)
      {

        if (vehicleStatSyncTimer is > 0f and < 30f &&
            _previousVehiclePhysicsState == currentPhysicsState)
        {
          vehicleStatSyncTimer += Time.fixedDeltaTime;
          return;
        }

        // only reset timer if forceupdate not provided
        vehicleStatSyncTimer = Time.fixedDeltaTime;
      }

      UpdateCenterOfMass();

      _previousVehiclePhysicsState = currentPhysicsState;

      switch (currentPhysicsState)
      {

        case VehiclePhysicsState.Land:
          UpdateLandVehicleStats();
          break;
        case VehiclePhysicsState.Air:
          UpdateFlightStats();
          break;
        case VehiclePhysicsState.Sea:
          UpdateWaterStats();
          break;
        case VehiclePhysicsState.Submerged:
          UpdateSubmergedStats();
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(currentPhysicsState), currentPhysicsState, null);
      }

      // todo determine if we change this what happens.
      m_force = PhysicsConfig.force.Value;
      // todo determine if we change this what happens.
      m_forceDistance = PhysicsConfig.forceDistance.Value;

      m_stearVelForceFactor = 1.3f;
      m_backwardForce = PhysicsConfig.backwardForce.Value;
    }

    /// <summary>
    ///   If the vehicle is anchored there is less sway
    /// </summary>
    /// TODO add config for this.
    /// <returns></returns>
    public float GetDirectionalForce()
    {
      if (isBeached) return 0;

      if (IsSubmerged()) return directionalForceSubmerged;

      if (!isAnchored) return directionalForceUnanchored;

      return directionalForceAnchored;
    }

    public void UpdateWaterForce(ShipFloatation shipFloatation)
    {
      var shipLeft = shipFloatation.ShipLeft;
      var shipForward = shipFloatation.ShipForward;
      var shipBack = shipFloatation.ShipBack;
      var shipRight = shipFloatation.ShipRight;
      var waterLevelLeft = shipFloatation.WaterLevelLeft;
      var waterLevelRight = shipFloatation.WaterLevelRight;
      var waterLevelForward = shipFloatation.WaterLevelForward;
      var waterLevelBack = shipFloatation.WaterLevelBack;
      var currentDepth = shipFloatation.CurrentDepth;
      var worldCenterOfMass = m_body.worldCenterOfMass;

      if (shipFloatation.IsAboveBuoyantLevel) return;

      m_body.WakeUp();

      // TODO swap with damage from environment such as ashlands
      // if (m_waterImpactDamage > 0f)
      //   UpdateWaterImpactForce(currentDepth, Time.fixedDeltaTime);

      // Calculate the forces for left, right, forward, and backward directions
      var leftForce = new Vector3(shipLeft.x, waterLevelLeft, shipLeft.z);
      var rightForce = new Vector3(shipRight.x, waterLevelRight, shipRight.z);
      var forwardForce =
        new Vector3(shipForward.x, waterLevelForward, shipForward.z);
      var backwardForce = new Vector3(shipBack.x, waterLevelBack, shipBack.z);

      // Get fixedDeltaTime and the delta force multiplier
      var deltaForceMultiplier =
        Time.fixedDeltaTime * PhysicsConfig.waterDeltaForceMultiplier.Value;

      // Calculate the current depth force multiplier
      var currentDepthForceMultiplier =
        Mathf.Clamp01(currentDepth /
                      PhysicsConfig.forceDistance.Value);

      // Calculate the target upwards force based on the current depth
      var upwardForceVector = Vector3.up * PhysicsConfig.force.Value *
                              currentDepthForceMultiplier;

      // Apply the smoothed upwards force
      AddForceAtPosition(upwardForceVector, worldCenterOfMass,
        PhysicsConfig.floatationVelocityMode.Value);

      // sideways force

      if (!CanRunSidewaysWaterForceUpdate) return;

      // todo rename variables for this section to something meaningful
      // todo abstract this to a method
      var forward = ShipDirection.forward;
      var deltaForward = Vector3.Dot(m_body.velocity, forward);
      var right = ShipDirection.right;
      var deltaRight = Vector3.Dot(m_body.velocity, right);
      var velocity = m_body.velocity;
      var deltaUp = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping *
                    currentDepthForceMultiplier;

      var deltaForwardClamp = deltaForward * deltaForward *
                              Mathf.Sign(deltaForward) *
                              m_dampingForward *
                              currentDepthForceMultiplier;

      // the water pushes the boat in the direction of the wind (sort of). Higher multipliers of m_dampingSideway will actually decrease this effect.
      var deltaRightClamp = deltaRight * deltaRight * Mathf.Sign(deltaRight) *
                            m_dampingSideway *
                            currentDepthForceMultiplier;

      // todo might want to remove this clamp as it could throttle things too much.
      velocity.y -= Mathf.Clamp(deltaUp, -1f, 1f);

      velocity -= forward * Mathf.Clamp(deltaForwardClamp, -1f, 1f);
      velocity -= right * Mathf.Clamp(deltaRightClamp, -1f, 1f);

      if (velocity.magnitude > m_body.velocity.magnitude)
        velocity = velocity.normalized * m_body.velocity.magnitude;

      m_body.velocity = velocity;
      var angularVelocity = m_body.angularVelocity;
      angularVelocity -=
        angularVelocity * m_angularDamping * currentDepthForceMultiplier;
      m_body.angularVelocity = angularVelocity;


      var forwardUpwardForce = Mathf.Clamp(
        (forwardForce.y - shipForward.y) * GetDirectionalForce(), 0f - maxForce,
        maxForce);
      var backwardsUpwardForce = Mathf.Clamp(
        (backwardForce.y - shipBack.y) * GetDirectionalForce(), 0f - maxForce,
        maxForce);
      var leftUpwardForce =
        Mathf.Clamp((leftForce.y - shipLeft.y) * GetDirectionalForce(),
          0f - maxForce, maxForce);
      var rightUpwardForce = Mathf.Clamp(
        (rightForce.y - shipRight.y) * GetDirectionalForce(),
        0f - maxForce, maxForce);

      forwardUpwardForce = Mathf.Sign(forwardUpwardForce) *
                           Mathf.Abs(Mathf.Pow(forwardUpwardForce, 2f));
      backwardsUpwardForce = Mathf.Sign(backwardsUpwardForce) *
                             Mathf.Abs(Mathf.Pow(backwardsUpwardForce, 2f));
      leftUpwardForce = Mathf.Sign(leftUpwardForce) *
                        Mathf.Abs(Mathf.Pow(leftUpwardForce, 2f));
      rightUpwardForce = Mathf.Sign(rightUpwardForce) *
                         Mathf.Abs(Mathf.Pow(rightUpwardForce, 2f));

      var centerOffMassDifference = Vector3.up * localCenterOfMassOffset * centerOfMassForceOffsetDifferenceMultiplier;

      if (CanRunForwardWaterForce)
        AddForceAtPosition(Vector3.up * forwardUpwardForce * deltaForceMultiplier,
          shipForward - centerOffMassDifference,
          PhysicsConfig.rudderVelocityMode.Value);

      if (CanRunBackWaterForce)
        AddForceAtPosition(
          Vector3.up * backwardsUpwardForce * deltaForceMultiplier, shipBack - centerOffMassDifference,
          PhysicsConfig.rudderVelocityMode.Value);

      if (CanRunLeftWaterForce)
        AddForceAtPosition(Vector3.up * leftUpwardForce * deltaForceMultiplier,
          shipLeft - centerOffMassDifference,
          PhysicsConfig.rudderVelocityMode.Value);
      if (CanRunRightWaterForce)
        AddForceAtPosition(Vector3.up * rightUpwardForce * deltaForceMultiplier,
          shipRight - centerOffMassDifference,
          PhysicsConfig.rudderVelocityMode.Value);
    }

    public void UpdateShipFloatation(ShipFloatation shipFloatation)
    {
      var vehicleState = IsSubmerged() ? VehiclePhysicsState.Sea : VehiclePhysicsState.Submerged;
      UpdateVehicleStats(vehicleState);

      UpdateWaterForce(shipFloatation);

      if (CanApplyWaterEdgeForce) ApplyEdgeForce(Time.fixedDeltaTime);

      if (HasOceanSwayDisabled)
      {
        UpdateAndFreezeRotation();
      }
      else
      {
        if (m_body.constraints == FreezeBothXZ)
          m_body.constraints = RigidbodyConstraints.None;
      }

      if (UpdateAnchorVelocity()) return;

      ApplySailForce(this);
    }

    /// <summary>
    ///   Used to stop the ship and prevent further velocity calcs if anchored.
    /// </summary>
    /// <returns></returns>
    private bool UpdateAnchorVelocity()
    {
      if (OnboardController.m_localPlayers.Count != 0 &&
          !isAnchored) return false;

      var anchoredVelocity = CalculateAnchorStopVelocity(m_body.velocity);
      var anchoredAngularVelocity =
        CalculateAnchorStopVelocity(m_body.angularVelocity);

      m_body.velocity = anchoredVelocity;
      m_body.angularVelocity = anchoredAngularVelocity;
      return true;
    }

    /// <summary>
    ///   TODO this is for getting the points from the ship hull mesh
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="shipForward"></param>
    /// <param name="meshTransform"></param>
    /// <returns></returns>
    public static Dictionary<string, Vector3> GetExtremePointsRelativeToDirection(
      Mesh mesh, Vector3 shipForward, Transform meshTransform)
    {
      // Dictionary to store the results
      var extremePoints = new Dictionary<string, Vector3>
      {
        { "ForwardLeft", Vector3.zero },
        { "ForwardRight", Vector3.zero },
        { "BackwardLeft", Vector3.zero },
        { "BackwardRight", Vector3.zero }
      };

      // Extreme distances for each point
      var extremeDistances = new Dictionary<string, float>
      {
        { "ForwardLeft", float.MinValue },
        { "ForwardRight", float.MinValue },
        { "BackwardLeft", float.MinValue },
        { "BackwardRight", float.MinValue }
      };

      // Normalize the forward direction
      shipForward = shipForward.normalized;

      // Create a rotation that aligns the shipForward with Vector3.forward
      var rotationToShipSpace =
        Quaternion.FromToRotation(shipForward, Vector3.forward);

      // Get the vertices in world space
      var vertices = mesh.vertices.Select(v => meshTransform.TransformPoint(v));

      // Transform vertices into ship space
      foreach (var vertex in vertices)
      {
        // Rotate the vertex into the ship's local space
        var localPoint = rotationToShipSpace * vertex;

        // Classify the vertex into a quadrant
        if (localPoint.z >= 0 && localPoint.x <= 0) // Forward-Left
        {
          var distance = localPoint.magnitude;
          if (distance > extremeDistances["ForwardLeft"])
          {
            extremeDistances["ForwardLeft"] = distance;
            extremePoints["ForwardLeft"] = vertex;
          }
        }
        else if (localPoint.z >= 0 && localPoint.x > 0) // Forward-Right
        {
          var distance = localPoint.magnitude;
          if (distance > extremeDistances["ForwardRight"])
          {
            extremeDistances["ForwardRight"] = distance;
            extremePoints["ForwardRight"] = vertex;
          }
        }
        else if (localPoint.z < 0 && localPoint.x <= 0) // Backward-Left
        {
          var distance = localPoint.magnitude;
          if (distance > extremeDistances["BackwardLeft"])
          {
            extremeDistances["BackwardLeft"] = distance;
            extremePoints["BackwardLeft"] = vertex;
          }
        }
        else if (localPoint.z < 0 && localPoint.x > 0) // Backward-Right
        {
          var distance = localPoint.magnitude;
          if (distance > extremeDistances["BackwardRight"])
          {
            extremeDistances["BackwardRight"] = distance;
            extremePoints["BackwardRight"] = vertex;
          }
        }
      }

      return extremePoints;
    }

//   public (Vector3, Vector3, Vector3, Vector3) GetShipForcePoints()
//   {
//     if (OnboardCollider == null || ShipDirection == null)
//       return (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
//
//     // Get world-space bounds of the collider
//     var worldBounds = GetWorldBounds(OnboardCollider);
//
//     // Extract min/max extents dynamically
//     var minX = worldBounds.min.x;
//     var maxX = worldBounds.max.x;
//     var minZ = worldBounds.min.z;
//     var maxZ = worldBounds.max.z;
//     var centerX = (minX + maxX) / 2;
//     var centerY = worldBounds.center.y; // Midpoint for consistent Y-level
//
//     // Define force application points in world space
//     var forwardPoint = new Vector3(centerX, centerY, maxZ); // Center of the forward face
//     var backPoint = new Vector3(centerX, centerY, minZ); // Center of the back face
//     var rightPoint = new Vector3(maxX, centerY, (minZ + maxZ) / 2); // Center of the right face
//     var leftPoint = new Vector3(minX, centerY, (minZ + maxZ) / 2); // Center of the left face
//
//     return (forwardPoint, backPoint, rightPoint, leftPoint);
//   }
//
// // Helper function to get the world-space bounds of a BoxCollider
//   private Bounds GetWorldBounds(BoxCollider collider)
//   {
//     var t = collider.transform;
//     var center = t.TransformPoint(collider.center);
//     var size = Vector3.Scale(collider.size, t.lossyScale);
//
//     return new Bounds(center, size);
//   }


// Helper function to get the world-space bounds of a BoxCollider
    // private Bounds GetWorldBounds(BoxCollider collider)
    // {
    //   var t = collider.transform;
    //   var center = t.TransformPoint(collider.center);
    //   var size = Vector3.Scale(collider.size, t.lossyScale);
    //
    //   return new Bounds(center, size);
    // }
    //
    /// <summary>
    /// Overly Complicated way to transform a rotated ShipDirection and get points from a box collider that does not rotate in world space.
    /// </summary>
    /// Do not change this logic without testing rotating VehicleWheel in all 90 degree increments.
    /// - Noting that 90 degree increments can get off even with this logic during reloads of the game. There is likely more to this rotation logic.
    /// todo it would be simpler to get the position of the box collider corners. But not sure what needs to be done. This code works.
    /// <returns></returns>
    public (Vector3, Vector3, Vector3, Vector3) GetShipForcePoints()
    {
      if (FloatCollider == null || ShipDirection == null)
        return (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);

      Physics.SyncTransforms();

      // Get the root object (the Rigidbody holder) dynamically
      var rootRigidbody = FloatCollider.GetComponentInParent<Rigidbody>();
      var rootTransform = rootRigidbody ? rootRigidbody.transform : FloatCollider.transform.root;

      // Get local position relative to the root (not directly ShipDirection)
      var localPosition = rootTransform.InverseTransformPoint(FloatCollider.transform.position);

      // Get the half-size of the collider
      var size = FloatCollider.size;
      var halfSize = size * 0.5f;

      // var forward = ShipDirection.forward;
      // var isXZSwapped = Mathf.Abs(Vector3.Dot(forward, Vector3.right)) > Mathf.Abs(Vector3.Dot(forward, Vector3.forward));

      // Use ShipDirection's rotation directly (since it's responsible for movement)
      var shipRotation = ShipDirection.rotation;

      // todo might need this for some edge cases.
      // Define local offset points (these will be rotated)
      // var localOffsets = isXZSwapped
      //   ? new Vector3[]
      //   {
      //     new(0, 0, halfSize.x), // Forward
      //     new(0, 0, -halfSize.x), // Back
      //     new(halfSize.z, 0, 0), // Right
      //     new(-halfSize.z, 0, 0) // Left
      //   }
      //   : new Vector3[]
      //   {
      //     new(0, 0, halfSize.z), // Forward
      //     new(0, 0, -halfSize.z), // Back
      //     new(halfSize.x, 0, 0), // Right
      //     new(-halfSize.x, 0, 0) // Left
      //   };
      var localOffsets = new Vector3[]
      {
        new(0, 0, halfSize.z), // Forward
        new(0, 0, -halfSize.z), // Back
        new(halfSize.x, 0, 0), // Right
        new(-halfSize.x, 0, 0) // Left
      };

      // Convert all points using world-space rotation from ShipDirection
      var worldCenter = rootTransform.TransformPoint(localPosition);
      var worldPoints = new Vector3[localOffsets.Length];

      for (var i = 0; i < localOffsets.Length; i++)
      {
        worldPoints[i] = worldCenter + shipRotation * localOffsets[i];
      }

      return (worldPoints[0], worldPoints[1], worldPoints[2], worldPoints[3]);
    }

    /// <summary>
    /// This does to account for rotation so is less accurate than GetShipForcePoints
    /// </summary>
    /// <returns></returns>
    public (Vector3, Vector3, Vector3, Vector3) GetShipForcePointsFromClosestOnBounds()
    {
      if (FloatCollider == null || ShipDirection == null)
        return (Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);

      Physics.SyncTransforms();

      var size = FloatCollider.size;
      var largestXZ = Mathf.Max(size.x, size.z);

      var colliderWorldCenter = FloatCollider.bounds.center;
      // var shipRotation = ShipDirection.rotation;
      var forward = ShipDirection.forward;
      var right = ShipDirection.right;

      var shipForward = FloatCollider.ClosestPointOnBounds(colliderWorldCenter + forward * largestXZ);
      var shipBackward = FloatCollider.ClosestPointOnBounds(colliderWorldCenter - forward * largestXZ);
      var shipRight = FloatCollider.ClosestPointOnBounds(colliderWorldCenter + right * largestXZ);
      var shipLeft = FloatCollider.ClosestPointOnBounds(colliderWorldCenter - right * largestXZ);

      return (shipForward, shipBackward, shipRight, shipLeft);
    }

    public ShipFloatation GetShipFloatationObj()
    {
      if (ShipDirection == null || PiecesController == null || FloatCollider == null || OnboardCollider == null)
        return new ShipFloatation();
      var worldCenterOfMass = m_body.worldCenterOfMass;
      worldCenterOfMass.y = FloatCollider.bounds.center.y;
      var (shipForward, shipBackward, shipRight, shipLeft) = GetShipForcePoints();

      // min does not matter but max does as the ship when attempting to reach flight mode will start bouncing badly.
      var clampedTargetHeight = Mathf.Clamp(TargetHeight, cachedMaxDepthOffset,
        GetSurfaceOffsetWaterVehicleOnly());

      var waterLevelCenter =
        Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter) +
        clampedTargetHeight;
      var waterLevelLeft =
        Floating.GetWaterLevel(shipLeft, ref m_previousLeft) +
        clampedTargetHeight;
      var waterLevelRight =
        Floating.GetWaterLevel(shipRight, ref m_previousRight) +
        clampedTargetHeight;
      var waterLevelForward =
        Floating.GetWaterLevel(shipForward,
          ref m_previousForward) +
        clampedTargetHeight;
      var waterLevelBack =
        Floating.GetWaterLevel(shipBackward,
          ref m_previousBack) +
        clampedTargetHeight;
      var averageWaterHeight =
        (waterLevelCenter + waterLevelLeft + waterLevelRight + waterLevelForward +
         waterLevelBack) /
        5f;
      var lowestWaterHeight = Mathf.Min(waterLevelCenter, waterLevelLeft, waterLevelRight, waterLevelForward, waterLevelBack);
      var maxWaterHeight = Mathf.Max(waterLevelCenter, waterLevelLeft, waterLevelRight, waterLevelForward, waterLevelBack, 30f);

      var groundLevelCenter =
        ZoneSystem.instance.GetGroundHeight(worldCenterOfMass);
      var groundLevelLeft =
        ZoneSystem.instance.GetGroundHeight(shipLeft);
      var groundLevelRight =
        ZoneSystem.instance.GetGroundHeight(shipRight);
      var groundLevelForward =
        ZoneSystem.instance.GetGroundHeight(shipForward);
      var groundLevelBack =
        ZoneSystem.instance.GetGroundHeight(shipBackward);

      var averageGroundLevel = (groundLevelCenter + groundLevelLeft + groundLevelRight + groundLevelForward + groundLevelBack) / 5f;
      var maxGroundLevel = Mathf.Max(groundLevelCenter, groundLevelLeft, groundLevelRight, groundLevelForward, groundLevelBack);
      // floatation point, does not need to be collider, could just be a value in MovementController 
      // todo possibly add target height so it will not apply upward force if the target height is already considered in the normal zone
      // a negative will have no upward force. A positive will have upward force.
      // negatives will be when the averagewaterheight is below the vehicle. IE gravity should be doing it's job.
      var currentDepth =
        averageWaterHeight - FloatCollider.bounds.min.y;

      // for adding a speed cost when nearing 0 rapidly decreases speed, but allows movement up to double the No-float height allow for energy to still push the vehicle to overcome being stuck.
      var buoyancySpeedMultiplier = Mathf.Lerp(0f, 1f, Mathf.Clamp01(currentDepth - buoyancyThreshold * 2));

      var isInvalid = false;
      if (averageWaterHeight <= -10000 ||
          averageWaterHeight < m_disableLevel)
      {
        currentDepth = 30 + clampedTargetHeight;
        isInvalid = true;
      }

      // var isAboveBuoyantLevel = currentDepth > m_disableLevel || isInvalid;
      var isAboveBuoyantLevel = currentDepth < buoyancyThreshold || isInvalid;

      if (isInvalid)
      {
        buoyancySpeedMultiplier = 0f;
      }

      return new ShipFloatation
      {
        AverageWaterHeight = averageWaterHeight,
        AverageGroundLevel = averageGroundLevel,
        MaxGroundLevel = maxGroundLevel,
        MaxWaterHeight = maxWaterHeight,
        LowestWaterHeight = lowestWaterHeight,
        CurrentDepth = currentDepth,
        IsAboveBuoyantLevel = isAboveBuoyantLevel,
        BuoyancySpeedMultiplier = buoyancySpeedMultiplier,
        IsInvalid = isInvalid,
        ShipLeft = shipLeft,
        ShipForward = shipForward,
        ShipBack = shipBackward,
        ShipRight = shipRight,
        WaterLevelLeft = waterLevelLeft,
        WaterLevelRight = waterLevelRight,
        WaterLevelForward = waterLevelForward,
        WaterLevelBack = waterLevelBack,
        GroundLevelLeft = groundLevelLeft,
        GroundLevelRight = groundLevelRight,
        GroundLevelForward = groundLevelForward,
        GroundLevelBack = groundLevelBack,
        GroundLevelCenter = groundLevelCenter
      };
    }

// Updates gravity and target height (which is used to compute gravity)
    public void UpdateGravity()
    {
      var isGravityEnabled = !IsFlying();
      if (_prevGravity == isGravityEnabled && m_body.useGravity == _prevGravity &&
          zsyncTransform.m_useGravity == _prevGravity) return;
      _prevGravity = isGravityEnabled;

      // The client cannot have this enabled as it adds additional velocity that is then faught against when zsynctransform is run.
      m_body.useGravity = IsOwner() && isGravityEnabled;
      zsyncTransform.m_useGravity = isGravityEnabled;
    }

    public void UpdateShipCreativeModeRotation()
    {
      if (!isCreative) return;
      var rotationY = m_body.rotation.eulerAngles.y;

      if (PatchController.HasGizmoMod)
      {
        if (PatchConfig.ComfyGizmoPatchCreativeHasNoRotation.Value)
          rotationY = 0;
        else
          rotationY =
            ComfyGizmo_Patch.GetNearestSnapRotation(m_body.rotation.eulerAngles
              .y);
      }

      var rotationWithoutTilt = Quaternion.Euler(0, rotationY, 0);
      m_body.rotation = rotationWithoutTilt;
    }

    // invalid component booleans to avoid checking component existence per frame.
    private bool _isInvalid = true;
    private bool _isWheelControllerInvalid = true;

    public bool IsInvalid()
    {
      if (!_isInvalid)
      {
        return false;
      }

      _isInvalid = !isActiveAndEnabled || m_body == null || m_floatcollider == null ||
                   FloatCollider == null || !_vehicle || !VehicleInstance?.PiecesController || !VehicleInstance?.OnboardController || !m_nview ||
                   m_nview.m_zdo == null ||
                   !ShipDirection;
      return _isInvalid;
    }

    public void UpdateValidComponentChecks()
    {
      if (_isWheelControllerInvalid)
      {
        _isWheelControllerInvalid = VehicleInstance!.WheelController == null;
      }
    }

    public void OnValidationFailure(Exception e)
    {
#if DEBUG
      Logger.LogDebug($"Error occurred after isInvalid return false. This means validation will need to be re-run. \nErrorMessage:\n{e}");
#endif
      _isInvalid = true;
      _isWheelControllerInvalid = true;
    }

    /// <summary>
    ///   Only Updates for the controlling player. Only players are synced
    /// </summary>
    public void VehiclePhysicsFixedUpdateAllClients()
    {
      if (IsInvalid()) return;
      try
      {
        var deltaTime = Time.fixedDeltaTime;
        UpdateValidComponentChecks();
        UpdateGravity();

        var hasControllingPlayer = HaveControllingPlayer();

        // Sets values based on m_speed
        UpdateShipWheelTurningSpeed();
        UpdateShipSpeed(hasControllingPlayer);

        //base ship direction controls
        UpdateControls(deltaTime);
        UpdateSail(deltaTime);

        // rudder direction
        UpdateRudder(deltaTime, hasControllingPlayer);

        // raft pieces transforms
        SyncVehicleRotationDependentItems();

        if (!_isWheelControllerInvalid)
        {
          VehicleInstance!.WheelController!.VehicleMovementFixedUpdateAllClients();
        }
      }
      catch (Exception e)
      {
        OnValidationFailure(e);
      }
    }

    public void ForceStopHauling(bool shouldCallComponent = false)
    {
      if (shouldCallComponent && HaulingRopeComponent != null)
      {
        HaulingRopeComponent.StopHauling();
      }

      HaulingRopeComponent = null;
      HaulingPlayer = null;
      isPlayerHaulingVehicle = false;
      distanceMovedSinceHaulTick = 0f;

      if (m_body.isKinematic)
      {
        m_body.isKinematic = false;
      }
    }

    private static float HaulingBreakPushForce = 30f; 
    
    public void UpdateVehicleFromHaulPosition()
    {
      if (!isPlayerHaulingVehicle) return;
      if (HaulingPlayer == null || HaulingRopeComponent == null)
      {
        ForceStopHauling(true);
        return;
      }

      var shouldSkipSnappingLeash = HaulingPlayer.InGhostMode() || HaulingPlayer.InGodMode() || HaulingPlayer.IsDebugFlying();

#if DEBUG
      shouldSkipSnappingLeash = false;
#endif

      // costs stamina of you move the vehicle beyond 1meter distance every FixedUpdate tick.
      if (distanceMovedSinceHaulTick > 1 && HaulingPlayer.m_stamina > staminaHaulCost)
      {
        HaulingPlayer.UseStamina(staminaHaulCost);
        distanceMovedSinceHaulTick = 0f;
      }

      if (!shouldSkipSnappingLeash && HaulingPlayer.IsEncumbered())
      {
        Logger.LogDebug("Hauling player is encumbered, you cannot pull a vehicle while encumbered");
        ForceStopHauling(true);
        return;
      }

      m_body.isKinematic = true;

      // Calculate direction based on pivot’s forward
      var position = m_body.position;
      var rotation = m_body.rotation;
      var haulingPlayerPosition = HaulingPlayer.transform.position;

      var playerClosestPointToVehicle = OnboardCollider!.ClosestPointOnBounds(haulingPlayerPosition);
      var distanceToPlayerVehicleCollider = Vector3.Distance(playerClosestPointToVehicle, haulingPlayerPosition);
      if (distanceToPlayerVehicleCollider < minHaulFollowDistance) return;

      var direction = (new Vector3(haulingPlayerPosition.x, m_body.position.y, haulingPlayerPosition.z) - position).normalized;
      var directionFromPlayerToAnchor = (HaulingRopeComponent.transform.position - haulingPlayerPosition).normalized;

      // Force drops the vehicle if you run out of stamina.
      if (!shouldSkipSnappingLeash && HaulingPlayer.m_stamina < staminaHaulCost)
      {
        var nextDamage = staminaHaulCost * distanceMovedSinceHaulTick;
        if (nextDamage + 5f > HaulingPlayer.GetHealth())
        {
          ForceStopHauling(true);
          return;
        }

        if (nextDamage > 1)
        {
          // makes the player lose health as they continue to haul.
          var hit = new HitData()
          {
            m_hitType = HitData.HitType.PlayerHit,
            m_backstabBonus = 1,
            m_point = haulingPlayerPosition,
            m_attacker = HaulingPlayer.GetZDOID(),
            m_ignorePVP = true,
            m_damage =
            {
              m_slash = nextDamage * 0.5f,
              m_pierce = nextDamage * 0.5f
            },
            m_ranged = false,
            m_hitCollider = HaulingPlayer.m_collider,
            m_toolTier = 100,
            m_dodgeable = false,
            m_blockable = false,
            m_pushForce = HaulingBreakPushForce,
            m_dir = directionFromPlayerToAnchor
          };
          hit.SetAttacker(HaulingPlayer);

          HaulingPlayer.Damage(hit);
          distanceMovedSinceHaulTick = 0f;
        }

        if (ShouldHaulingLineSnapOnZeroStamina && HaulingPlayer.m_stamina < 1)
        {
          ForceStopHauling(true);
        }
      }

      // Move the rigidbody in that direction
      var newPosition = position + direction * haulMoveSpeed * Time.fixedDeltaTime;

      var bodyOffsetBetweenLowestPoint = OnboardCollider.bounds.min.y - position.y;

      // we need to know if we are near the ground. If not we add an offset positive or negative so the vehicle hits near the ground while dragged
      float deltaLowestPoint;

      if (ShipFloatationObj.MaxWaterHeight > ShipFloatationObj.MaxGroundLevel)
      {
        deltaLowestPoint = ShipFloatationObj.MaxWaterHeight - FloatCollider!.bounds.min.y;
      }
      else
      {
        deltaLowestPoint = ShipFloatationObj.MaxGroundLevel - OnboardCollider.bounds.min.y;
      }
      // directly updates this to prevent accidents.
      newPosition.y += bodyOffsetBetweenLowestPoint + deltaLowestPoint + HaulingOffsetLowestPointBuffer;

      var haulingRopePosition = HaulingRopeComponent.transform.position;
      var deltaDistanceFromAnchorPoint = Vector3.Distance(haulingPlayerPosition, haulingRopePosition);

      if (deltaDistanceFromAnchorPoint < minHaulFollowDistance)
      {
        // do nothing. We do not allow hauling vehicle within this distance.
        return;
      }

      if (deltaDistanceFromAnchorPoint > maxHaulFollowDistance)
      {
        ForceStopHauling(true);
        return;
      }

      m_body.MovePosition(newPosition);

      // increment distance so we can make it cost stamina per 1 meter used.
      distanceMovedSinceHaulTick += Vector3.Distance(position, newPosition);

      // Calculate desired rotation
      var targetRotation = Quaternion.LookRotation(direction, Vector3.up);

      // Adjust rotation to align with the pivot’s forward
      var adjustedRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y - ShipDirection.rotation.eulerAngles.y, 0);

      // Apply rotation smoothly
      m_body.MoveRotation(Quaternion.Slerp(rotation, adjustedRotation, haulRotationSpeed * Time.fixedDeltaTime));
    }

    /// <summary>
    ///   The owner of the vehicle netview will only be able to fire these updates
    /// </summary>
    /// Physics syncs on 1 client are better otherwise the ships will desync across clients and both will stutter
    public void VehicleMovementUpdatesOwnerOnly()
    {
      if (IsInvalid()) return;
      
      var hasOwner = m_nview.HasOwner();
      var owner = m_nview.IsOwner();
      if (!VehicleDebugConfig.SyncShipPhysicsOnAllClients.Value && !owner && hasOwner ||
          isBeached) return;

      _currentShipFloatation = GetShipFloatationObj();

      if (isPlayerHaulingVehicle && HaulingPlayer != null && HaulingPlayer.transform.root != PiecesController.transform.root)
      {
        UpdateVehicleFromHaulPosition();
        return;
      }

      // for land-vehicle prefab only.
      if (_vehicle!.IsLandVehicleFromPrefab)
      {
        UpdateVehicleLandSpeed();
        return;
      }

      UpdateColliderPositions();

      var isFlying = IsFlying();

      if (!ShipFloatationObj.IsAboveBuoyantLevel || !isFlying)
        if (m_body.constraints != RigidbodyConstraints.None)
          m_body.constraints = RigidbodyConstraints.None;

      if (!ShipFloatationObj.IsAboveBuoyantLevel && !isFlying)
        UpdateShipFloatation(ShipFloatationObj);
      else if (isFlying)
        UpdateFlyingVehicle();
      else if (PrefabConfig.EnableLandVehicles.Value && _vehicle.IsLandVehicle)
        UpdateVehicleLandSpeed();

      // both flying and floatation use this
      ApplyRudderForce();
      ClampVehicleAcceleration();
    }

    public bool IsSailUp()
    {
      if (VehicleSpeed != Ship.Speed.Half) return VehicleSpeed == Ship.Speed.Full;

      return true;
    }

    public void UpdateSailSize(float dt)
    {
      var num = 0f;
      var speed = VehicleSpeed;

      switch (speed)
      {
        case Ship.Speed.Back:
          num = 0.1f;
          break;
        case Ship.Speed.Half:
          num = 0.5f;
          break;
        case Ship.Speed.Full:
          num = 1f;
          break;
        case Ship.Speed.Slow:
          num = 0.1f;
          break;
        case Ship.Speed.Stop:
          num = 0.1f;
          break;
      }

      var localScale = m_sailObject.transform.localScale;
      var flag = Mathf.Abs(localScale.y - num) < 0.01f;
      if (!flag)
      {
        localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
        m_sailObject.transform.localScale = localScale;
      }

      // if ((bool)m_sailCloth)
      // {
      //   if (speed == Ship.Speed.Stop || speed == Ship.Speed.Slow || speed == Ship.Speed.Back)
      //   {
      //     if (flag && m_sailCloth.enabled)
      //     {
      //       m_sailCloth.enabled = false;
      //     }
      //   }
      //   else if (flag)
      //   {
      //     if (!m_sailWasInPosition)
      //     {
      //       Utils.RecreateComponent(ref m_sailCloth);
      //     }
      //   }
      //   else
      //   {
      //     m_sailCloth.enabled = true;
      //   }
      // }

      m_sailWasInPosition = flag;
    }

    /// <summary>
    ///   CrossWind dir
    /// </summary>
    /// <param name="movementController"></param>
    /// <returns></returns>
    public static Vector3 GetCrossWindDirForShip(
      VehicleMovementController movementController)
    {
      var isWindPowerActive = movementController.IsWindControllActive();

      var windDir = isWindPowerActive
        ? movementController.ShipDirection.forward
        : EnvMan.instance.GetWindDir();
      windDir = Vector3.Cross(
        Vector3.Cross(windDir, movementController.ShipDirection.up),
        movementController.ShipDirection.up);
      return windDir;
    }

    public static float GetWindSailTurnTime(Vector3 windDir,
      VehicleMovementController movementController)
    {
      return 0.5f +
             Vector3.Dot(movementController.ShipDirection.forward, windDir) *
             0.5f;
    }

    public void UpdateSail(float deltaTime)
    {
      UpdateSailSize(deltaTime);
      var windDir = GetCrossWindDirForShip(this);
      var t = GetWindSailTurnTime(windDir, this);

      switch (VehicleSpeed)
      {
        case Ship.Speed.Full:
        case Ship.Speed.Half:
        {
          var to = Quaternion.LookRotation(
            -Vector3.Lerp(windDir,
              Vector3.Normalize(windDir - ShipDirection.forward), t),
            ShipDirection.up);
          m_mastObject.transform.rotation =
            Quaternion.RotateTowards(m_mastObject.transform.rotation, to,
              30f * deltaTime);
          break;
        }
        case Ship.Speed.Back:
        {
          var from =
            Quaternion.LookRotation(-ShipDirection.forward, ShipDirection.up);
          var to2 = Quaternion.LookRotation(-windDir, ShipDirection.up);
          to2 = Quaternion.RotateTowards(from, to2, 80f);
          m_mastObject.transform.rotation =
            Quaternion.RotateTowards(m_mastObject.transform.rotation, to2,
              30f * deltaTime);
          break;
        }
      }
    }


    /**
     * In theory, we can just make the sailComponent and mastComponent parents of the masts/sails of the ship. This will make any mutations to those parents in sync with the sail changes
     */
    private void SyncVehicleRotationDependentItems()
    {
      if (!isActiveAndEnabled) return;

      if (VehicleInstance?.PiecesController == null) return;
      foreach (var mast in VehicleInstance.PiecesController.m_mastPieces
                 .ToList())
      {
        if (!(bool)mast)
        {
          VehicleInstance.PiecesController.m_mastPieces.Remove(mast);
          continue;
        }

        if (mast.m_allowSailRotation &&
            PropulsionConfig.AllowBaseGameSailRotation.Value)
        {
          var newRotation = m_mastObject.transform.localRotation;
          mast.transform.localRotation = newRotation;
        }

        if (mast.m_allowSailShrinking)
        {
          if (mast.m_sailObject.transform.localScale !=
              m_sailObject.transform.localScale)
            mast.m_sailCloth.enabled = false;
          mast.m_sailObject.transform.localScale =
            m_sailObject.transform.localScale;
          mast.m_sailCloth.enabled = true;
        }
        else
        {
          mast.m_sailObject.transform.localScale = Vector3.one;
          mast.m_sailCloth.enabled = !mast.m_disableCloth;
        }
      }

      foreach (var rudder in VehicleInstance.PiecesController.m_rudderPieces
                 .ToList())
      {
        if (!(bool)rudder)
        {
          VehicleInstance.PiecesController.m_rudderPieces.Remove(rudder);
          continue;
        }

        if (!rudder.PivotPoint)
        {
          Logger.LogError("No pivot point detected for rudder");
          continue;
        }

        var newRotation = Quaternion.Slerp(
          rudder.PivotPoint.localRotation,
          Quaternion.Euler(0f, m_rudderRotationMax * (0f - GetRudderValue()) * 2,
            0f), 0.5f);
        rudder.PivotPoint.localRotation = newRotation;
      }

      foreach (var wheel in VehicleInstance.PiecesController
                 ._steeringWheelPieces
                 .ToList())
        if (!(bool)wheel)
          VehicleInstance.PiecesController._steeringWheelPieces.Remove(wheel);
        else if (wheel.wheelTransform != null)
          wheel.wheelTransform.localRotation = Quaternion.Slerp(
            wheel.wheelTransform.localRotation,
            Quaternion.Euler(
              m_rudderRotationMax * (0f - m_rudderValue) *
              wheel.m_wheelRotationFactor, 0f, 0f), 0.5f);
    }

    private float GetInterpolatedWindAngleFactor()
    {
      var windIntensity = IsWindControllActive()
        ? 1f
        : EnvMan.instance.GetWindIntensity();
      var interpolatedWindIntensity = Mathf.Lerp(0.25f, 1f, windIntensity);

      var windAngleFactor = GetWindAngleFactor();
      windAngleFactor *= interpolatedWindIntensity;
      return windAngleFactor;
    }

    private float GetSailForceEnergy(float sailSize, float windAngleFactor)
    {
      return windAngleFactor * m_sailForceFactor * sailSize;
    }

    /// <summary>
    ///   Considered flying when below the negative waterLevel target height. The
    ///   FloatCollider/Blocking will not update below these values.
    /// </summary>
    /// We cache this to avoid flying state infinitely when near the boarder of flying/not flight
    /// <returns></returns>
    public bool IsFlying()
    {
      if (!PropulsionConfig.AllowFlight.Value) return false;

      // this allows for the check to run the first time.
      if (lastFlyingDt is > 0f and < 2f)
      {
        lastFlyingDt += Time.fixedDeltaTime;
        return cachedFlyingValue;
      }

      lastFlyingDt = Time.fixedDeltaTime;

      // The vehicle is out of the water.
      // var pieceOffset =
      //   TargetHeight + OnboardCollider.bounds.min.y;
      cachedFlyingValue = TargetHeight > ZoneSystem.instance.m_waterLevel +
        GetSurfaceOffsetWaterVehicleOnly();

      return cachedFlyingValue;
    }

    /// <summary>
    ///   Checks that the vehicle is fully below water and ballast is enabled.
    ///   Todo may need to add more settings, but this getter should be for switching
    ///   to submerged physics
    ///   - Assert that the top of the vehicle is underwater.
    ///   - Assert that not flying
    /// </summary>
    /// <returns></returns>
    public bool IsSubmerged()
    {
      return IsNotFlying && WaterConfig.WaterBallastEnabled.Value &&
             OnboardCollider.bounds.max.y < ZoneSystem.instance.m_waterLevel;
    }

    private Vector3 GetSailForce(float sailSize, float dt, bool isFlying)
    {
      var windDir = IsWindControllActive()
        ? Vector3.zero
        : EnvMan.instance.GetWindDir();
      var windAngleFactorInterpolated = GetInterpolatedWindAngleFactor();

      Vector3 target;
      if (isFlying)
        target = Vector3.Normalize(ShipDirection.forward) *
                 (windAngleFactorInterpolated * m_sailForceFactor * sailSize);
      else
        target = Vector3.Normalize(windDir + ShipDirection.forward) *
                 GetSailForceEnergy(sailSize, windAngleFactorInterpolated);

      m_sailForce = Vector3.SmoothDamp(m_sailForce, target,
        ref m_windChangeVelocity, 1f, 99f);

      // add a buoyancy force multiplier for when near stuck to slow down velocity.
      m_sailForce *= ShipFloatationObj.BuoyancySpeedMultiplier;


      return Vector3.ClampMagnitude(m_sailForce, 20f);
    }

    private new float GetWindAngleFactor()
    {
      if (IsWindControllActive()) return 1f;
      var num =
        Vector3.Dot(EnvMan.instance.GetWindDir(), -ShipDirection!.forward);
      var num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
      var num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
      return num2 * num3;
    }

    private Vector3 GetAdditiveSteerForce(float directionMultiplier)
    {
      if (_rudderForce == 0 || VehicleSpeed == Ship.Speed.Stop || isAnchored)
        return Vector3.zero;

      var shipAdditiveSteerForce = ShipDirection.right *
                                   (m_stearForce * (0f - m_rudderValue) *
                                    directionMultiplier);

      if (ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
        shipAdditiveSteerForce *= Mathf.Clamp(_rudderForce, 1f, 10f);

      // Adds additional speeds to turning
      if (PiecesController?.m_rudderPieces.Count > 0)
        shipAdditiveSteerForce *= PropulsionConfig.TurnPowerWithRudder.Value;
      else
        shipAdditiveSteerForce *= PropulsionConfig.TurnPowerNoRudder.Value;

      return shipAdditiveSteerForce;
    }

    /// <summary>
    /// For adding a single AOE component in the top level collider component for meshcolliders.
    /// TODO determine if this is best place for this function.
    ///
    /// This does not need to be tracked as a Ram piece as every gameobject added to this PiecesController ignores the meshColliders provided
    /// </summary>
    public void TryAddRamAoeToVehicle()
    {
      if (_vehicle == null) return;

      var isLandVehicleFromPrefab = _vehicle.IsLandVehicleFromPrefab;
      if (isLandVehicleFromPrefab && !RamConfig.LandVehiclesAreRams.Value || !isLandVehicleFromPrefab && !RamConfig.WaterVehiclesAreRams.Value)
      {
        return;
      }
      
      vehicleRam = DamageColliders.gameObject.GetComponent<VehicleRamAoe>();

      if (vehicleRam == null)
        vehicleRam = DamageColliders.gameObject
          .AddComponent<VehicleRamAoe>();
      // negative check, should never hit this...
      if (vehicleRam == null) return;
      if (vehicleRam.m_nview == null) vehicleRam.m_nview = m_nview;

      vehicleRam.m_RamType = isLandVehicleFromPrefab ? RamPrefabs.RamType.LandVehicle : RamPrefabs.RamType.WaterVehicle;
      vehicleRam.m_vehicle = _vehicle;
    }

    public Vector3 GetRudderPosition()
    {
      // var forwardPos = ShipDirection.forward;
      if (PiecesController == null) return ShipDirection.position;
      var hasRudderPrefab = PiecesController.m_rudderPieces.Count > 0;
      if (!hasRudderPrefab) return ShipDirection.position;
      return PiecesController.m_rudderPieces[0].transform.position;
    }

    private bool IsFloatColliderAboveWater()
    {
      if (_currentShipFloatation == null) return false;
      return m_floatcollider.bounds.min.y >
             _currentShipFloatation.Value.AverageWaterHeight;
    }

    /// <summary>
    ///   Sets the speed of the ship with rudder speed added to it.
    /// </summary>
    /// Does not apply for stopped or anchored states
    private void ApplyRudderForce()
    {
      if (VehicleSpeed == Ship.Speed.Stop ||
          isAnchored) return;

      var flying = IsFlying();
      // Stop steering when above the water. Applying force is bad...
      if (!flying && !IsSubmerged() &&
          ShipFloatationObj.IsAboveBuoyantLevel)
        return;

      var forward = ShipDirection.forward;
      var direction = Vector3.Dot(m_body.velocity, forward);
      var rudderForce = GetRudderForcePerSpeed();
      // steer offset will need to be size x or size z depending on location of rotation.
      // todo GetFloatSizeFromDirection may not be needed anymore.
      // This needs to use blocking collider otherwise the float collider pushes the vehicle upwards but the blocking collider cannot push downwards.
      // todo set this to the rudder point
      // rear steering is size z. Center steering is size.z/2
      var steerOffset = m_body.worldCenterOfMass -
                        forward * GetFloatSizeFromDirection(Vector3.forward);

      var steeringVelocityDirectionFactor = direction * m_stearVelForceFactor;
      var steerOffsetForce = ShipDirection.right *
                             (steeringVelocityDirectionFactor *
                              (0f - m_rudderValue) *
                              Time.fixedDeltaTime);

      // add a buoyancy force multiplier for when near stuck to slow down velocity.
      if (!flying)
      {
        steerOffsetForce *= ShipFloatationObj.BuoyancySpeedMultiplier;
      }

      AddForceAtPosition(
        steerOffsetForce,
        steerOffset, PhysicsConfig.rudderVelocityMode.Value);

      // needs to always be 1 otherwise nothing happens when setting rudder defaults to 0 speed. Alternative is clamping GetRudderForcePerSpeed to 1. But this could be inaccurate for stopping and other steering values.
      var steerSpeed = Mathf.Max(rudderForce, 1f);
      var steerForce = forward *
                       (m_backwardForce * steerSpeed *
                        (1f - Mathf.Abs(m_rudderValue)));

      var directionMultiplier =
        VehicleSpeed != Ship.Speed.Back ? 1 : -1;
      steerForce *= directionMultiplier;

      // todo see if this is necessary. This logic is from the Base game Ship

      if (ValheimRaftPlugin.Instance.AllowCustomRudderSpeeds.Value)
        steerForce += GetAdditiveSteerForce(directionMultiplier);
      else if (VehicleSpeed is Ship.Speed.Back or Ship.Speed.Slow)
        steerForce += GetAdditiveSteerForce(directionMultiplier);

      switch (flying)
      {
        case true:
          transform.rotation =
            Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
          break;
        case false:
          // add a buoyancy force multiplier for when near stuck to slow down velocity.
          steerForce *= _currentShipFloatation.Value.BuoyancySpeedMultiplier;
          break;
      }

      AddForceAtPosition(steerForce * Time.fixedDeltaTime, steerOffset,
        PhysicsConfig.turningVelocityMode.Value);
    }

    private static void ApplySailForce(VehicleMovementController instance,
      bool isFlying = false)
    {
      if (instance == null || instance.isAnchored || instance.m_body == null || instance.ShipDirection == null) return;

      var sailArea = 0f;

      if (instance.PiecesController != null)
        sailArea =
          instance.PiecesController.GetSailingForce();

      // intellij seems to think 1370 does not have enough guards if this check is at the top of the function.
      if (instance == null) return;

      switch (instance.VehicleSpeed)
      {
        case Ship.Speed.Full:
          break;
        case Ship.Speed.Half:
          sailArea *= 0.5f;
          break;
        case Ship.Speed.Slow:
          sailArea = 0;
          break;
        case Ship.Speed.Stop:
        case Ship.Speed.Back:
        default:
          sailArea = 0f;
          break;
      }

      var sailForce =
        instance.GetSailForce(sailArea, Time.fixedDeltaTime, isFlying);

      var position = instance.m_body.worldCenterOfMass;

      instance.AddForceAtPosition(
        sailForce,
        position,
        PhysicsConfig.sailingVelocityMode.Value);
    }


    /// <summary>
    /// Todo integrate this and confirm it works. This will help avoid any one player from updating too quickly.
    ///
    /// Also should prevent desyncs if we can synchronize it.
    /// </summary>
    public void SendSyncBounds()
    {
      if (m_nview == null) return;

      OnboardController.m_localPlayers.ForEach(x =>
      {
        m_nview.InvokeRPC(x.GetPlayerID(), nameof(RPC_SyncBounds));
      });
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

    public void OnFlightChangePolling()
    {
      CancelInvoke(nameof(SyncTargetHeight));
      if (!IsBallastAndFlightDisabled)
      {
        InvokeRepeating(nameof(SyncTargetHeight), 2f, 2f);
      }
      else
      {
        TargetHeight = 0f;
        SetTargetHeight(TargetHeight);
      }
    }

    /// <summary>
    ///   Updates the rudder turning speed based on the shipShip.Speed. Higher speeds
    ///   will make turning the rudder harder
    /// </summary>
    /// m_rudder = rotation speed of rudder icon
    /// m_rudderValue = position of rudder
    /// m_rudderSpeed = the force speed applied when moving the ship
    public void UpdateShipWheelTurningSpeed()
    {
      switch (GetSpeedSetting())
      {
        case Ship.Speed.Stop:
        case Ship.Speed.Back:
        case Ship.Speed.Slow:
          m_rudderSpeed = 2f;
          break;
        case Ship.Speed.Half:
          m_rudderSpeed = 1.5f;
          break;
        case Ship.Speed.Full:
          m_rudderSpeed = 1f;
          break;
        default:
          Logger.LogError(
            $"Speed value could not handle this variant, {vehicleSpeed}");
          m_rudderSpeed = 1f;
          break;
      }
    }

    /// <summary>
    ///   Meant for realism and testing but will allow the ship to continue for a bit
    ///   even when the player is logged out on server.
    ///   Will not anchor if the vehicle suddenly has a player aboard
    /// </summary>
    public void DelayedAnchor()
    {
      if (OnboardController.m_localPlayers.Count > 0) return;
      HasPendingAnchor = false;
      SendSetAnchor(AnchorState.Anchored);
    }

    /// <summary>
    ///   Will always send true for anchor state. Not meant to remove anchor on delay
    /// </summary>
    public void SendDelayedAnchor()
    {
      if (VehicleDebugConfig.HasAutoAnchorDelay.Value)
      {
        var autoDelayInMS = VehicleDebugConfig.AutoAnchorDelayTime.Value * 1000f;
        Invoke(nameof(DelayedAnchor),
          autoDelayInMS);
        HasPendingAnchor = true;
        return;
      }

      SendSetAnchor(AnchorState.Anchored);
    }


    public void UpdateShipSpeed(bool hasControllingPlayer)
    {
      if (isAnchored && vehicleSpeed != Ship.Speed.Stop)
      {
        vehicleSpeed = Ship.Speed.Stop;
        // force resets rudder to 0 degree position
        m_rudderValue = 0f;
      }

      var isUncontrolledRowing = !hasControllingPlayer &&
                                 vehicleSpeed is Ship.Speed.Slow
                                   or Ship.Speed.Back &&
                                 !PropulsionConfig.SlowAndReverseWithoutControls
                                   .Value;
      if (isUncontrolledRowing) SendSpeedChange(DirectionChange.Stop);
    }

    private void SetTargetHeight(float val)
    {
      switch (PropulsionConfig.AllowFlight.Value)
      {
        case false:
          m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, 0f);
          break;
        case true:
          m_nview?.m_zdo.Set(VehicleZdoVars.VehicleTargetHeight, val);
          break;
      }
    }

    private void SyncTargetHeight()
    {
      if (!VehicleInstance?.NetView) return;
      if (VehicleInstance.NetView == null) return;

      var zdoTargetHeight = VehicleInstance.NetView.m_zdo.GetFloat(
        VehicleZdoVars.VehicleTargetHeight,
        TargetHeight);
      TargetHeight = zdoTargetHeight;
    }

    internal void RPC_TargetHeight(long sender, float value)
    {
      TargetHeight = value;
    }

    private void InitializeRPC()
    {
      if (m_nview && !_hasRegister)
      {
        RegisterRPCListeners();
        _hasRegister = true;
      }
    }

    private void UnRegisterRPCListeners()
    {
      // ship piece bounds syncing
      m_nview.Unregister(nameof(RPC_SyncBounds));

      // boat target height
      m_nview.Unregister(nameof(RPC_TargetHeight));

      // ship speed
      m_nview.Unregister(nameof(RPC_SpeedChange));

      // anchor logic
      m_nview.Unregister(nameof(RPC_SetAnchor));

      // rudder direction
      m_nview.Unregister(nameof(RPC_Rudder));

      // boat sway
      m_nview.Unregister(nameof(RPC_SetOceanSway));

      // steering
      m_nview.Unregister(nameof(RPC_RequestControl));
      m_nview.Unregister(nameof(RPC_RequestResponse));
      m_nview.Unregister(nameof(RPC_ReleaseControl));

      // sync_everything
      m_nview.Unregister(nameof(RPC_SyncVehicleConfig));

      _hasRegister = false;
    }

    public void SyncVehicleConfig()
    {
      var currentVehicleConfig = new VehicleConfig()
      {
        treadDistance = 5f
      };
      m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_SyncVehicleConfig), new VehicleConfig());
    }
    /// <summary>
    /// This is meant to sync all individual vehicle config values. When a value changes we must emit this to all clients.
    ///
    /// TODO move to vehicle if this makes sense since this config effects everything, not just movement properties.
    /// </summary>
    private void RPC_SyncVehicleConfig(long sender, VehicleConfig vehicleConfig)
    {
      if (_vehicle == null) return;
      _vehicle.VehicleConfig = vehicleConfig;
      // todo add all vehicle client syncs here
      // todo have an interface / struct for all vehicle config values. This should be serialized via the RPC sync.
    }

    private void RegisterRPCListeners()
    {
      // ship piece bounds syncing
      m_nview.Register(nameof(RPC_SyncBounds), RPC_SyncBounds);

      // ship target height
      m_nview.Register<float>(nameof(RPC_TargetHeight), RPC_TargetHeight);

      // ship speed
      m_nview.Register<int>(nameof(RPC_SpeedChange), RPC_SpeedChange);

      // anchor logic
      m_nview.Register<int>(nameof(RPC_SetAnchor), RPC_SetAnchor);

      // rudder direction
      m_nview.Register<float>(nameof(RPC_Rudder), RPC_Rudder);

      // boat sway
      m_nview.Register<bool>(nameof(RPC_SetOceanSway), RPC_SetOceanSway);

      // steering
      m_nview.Register<long>(nameof(RPC_RequestControl), RPC_RequestControl);
      m_nview.Register<bool, long, long>(nameof(RPC_RequestResponse),
        RPC_RequestResponse);
      m_nview.Register<long>(nameof(RPC_ReleaseControl),
        RPC_ReleaseControl);

      // vehicle config
      m_nview.Register<VehicleConfig>(nameof(RPC_SyncVehicleConfig), RPC_SyncVehicleConfig);

      _hasRegister = true;
    }


    /**
     * Will not be supported in v3.x.x
     */
    public void DEPRECATED_InitializeRudderWithShip(
      SteeringWheelComponent steeringWheel, Ship ship)
    {
      m_nview = ship.m_nview;
      ship.m_controlGuiPos = steeringWheel.transform;
      var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
      if (rudderAttachPoint != null) AttachPoint = rudderAttachPoint;

      InitializeRPC();
    }

    public void AssignShipControls(Player player)
    {
      if (PiecesController != null &&
          PiecesController._steeringWheelPieces.Count > 0)
        player.m_doodadController = PiecesController._steeringWheelPieces[0];
    }

    /// <summary>
    ///   Generic nuke for all controllers if the wheel is removed.
    /// </summary>
    /// <param name="vehicleMovementController"></param>
    public static void RemoveAllShipControls(
      VehicleMovementController? vehicleMovementController)
    {
      if (vehicleMovementController == null) return;
      foreach (var mPlayer in vehicleMovementController.OnboardController
                 .m_localPlayers)
        mPlayer.m_doodadController = null;
    }

    public void InitializeWheelWithShip(
      SteeringWheelComponent steeringWheel)
    {
      _vehicle.m_controlGuiPos = transform;

      var rudderAttachPoint = steeringWheel.transform.Find("attachpoint");
      if (rudderAttachPoint != null) AttachPoint = rudderAttachPoint.transform;
    }

    internal void ForceTakeoverControls(long playerId)
    {
      Logger.LogDebug("Calling ForceSetOwner");
      var prevOwnerId = GetUser();
      var prevPlayerOwner = Player.GetPlayer(prevOwnerId);
      OnControlsHandOff(Player.GetPlayer(playerId), prevPlayerOwner);
    }

    /// <summary>
    ///   Force sets the owner after RPC fails to be sent.
    /// </summary>
    public IEnumerator DebouncedForceTakeoverControls(long playerId)
    {
      if (_vehicle.NetView == null) yield break;
      yield return new WaitForSeconds(2f);
      ForceTakeoverControls(playerId);
    }

    /// <summary>
    ///   Cancels the invocation which forces the owner to be the new player.
    /// </summary>
    public void CancelDebounceTakeoverControls()
    {
      if (_debouncedForceTakeoverControlsInstance != null)
        StopCoroutine(_debouncedForceTakeoverControlsInstance);
    }

    public void SendRequestControl(long playerId)
    {
      if (m_nview == null) return;
      CancelDebounceTakeoverControls();
      m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_RequestControl),
        playerId);
    }

    /// <summary>
    ///   We have to sync the new user id across all clients but only the owner of the
    ///   zdo can set the new user id.
    ///   - This will fall back to a force owner takeover from the invoker if the owner
    ///   is somehow un-responsive.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="targetPlayerId"></param>
    private void RPC_RequestControl(long sender, long targetPlayerId)
    {
      if (m_nview == null || ZNet.instance == null) return;
      CancelDebounceTakeoverControls();

      _debouncedForceTakeoverControlsInstance =
        StartCoroutine(DebouncedForceTakeoverControls(targetPlayerId));

      var previousUserId = GetUser();
      var isInBoat = WaterZoneUtils.IsOnboard(Player.GetPlayer(targetPlayerId));

      if (!m_nview.IsOwner())
      {
        if (ModEnvironment.IsDebug) Logger.LogDebug("Not zdo owner, skipping...");

        return;
      }

      if (ModEnvironment.IsDebug)
        if (!isInBoat)
          Logger.LogDebug(
            "RPC_RequestControl requested the owner to give control but they are not within the boat.");

      if (!isInBoat) return;

      // the previous user could be invalid so always makes the current user valid if so.
      m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_RequestResponse),
        true, targetPlayerId, previousUserId);
    }

    private void RPC_ReleaseControl(long sender, long playerId)
    {
      CancelDebounceTakeoverControls();
      if (m_nview == null) return;

      var previousUser = GetUser();
      var previousPlayer = Player.GetPlayer(previousUser);
      EjectPreviousPlayerFromControls(previousPlayer);

      if (m_nview.IsOwner()) m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
    }

    /// <summary>
    ///   Simple surface offset.
    ///   - This should always return a positive value. This value will be the
    ///   converted to negative in the offset for TargetHeight
    /// </summary>
    /// <returns></returns>
    public float GetMaxAboveSurfaceFromOnboardExtents()
    {
      return OnboardCollider.bounds.extents.y *
             WaterConfig.AboveSurfaceBallastMaxShipSizeAboveWater.Value;
    }

    /// <summary>
    ///   More complex surface offset including mass calculation
    ///   This should always return a positive value. This value will be the converted
    ///   to negative in the offset for TargetHeight
    /// </summary>
    /// <returns></returns>
    public float GetMaxAboveSurfaceFromShipWeight()
    {
      if (PiecesController == null) return 1000f;

      var TotalMass = PiecesController.TotalMass;

      // prevents divison of zero
      if (TotalMass <= 0f) TotalMass = 1000f;

      var convexHullBounds = PiecesController.convexHullComponent.GetConvexHullBounds(true);
      // prevent zero value
      var volumeOfShip = Mathf.Max(convexHullBounds.size.x *
                                   convexHullBounds.size.y *
                                   convexHullBounds.size.z, 1f);

      // we assume the ship actually only occupies a smaller volume
      volumeOfShip *= 0.5f;

      // as the total mass to Volume number approaches zero it will allow almost 100% of the ship above the surface
      // if the ship is extremely heavy it will sink up to it's full height.
      var totalMassToVolume = Mathf.Clamp(TotalMass / volumeOfShip, 0f, 2f);

      // gives us a negative number if the mass is larger than the volume.
      var multiplier = 1 - totalMassToVolume;
      return GetMaxAboveSurfaceFromOnboardExtents() * multiplier;
    }

    public float GetSurfaceOffsetWaterVehicleOnly()
    {
      if (IsBallastAndFlightDisabled) return 0f;

      if (WaterConfig.EXPERIMENTAL_AboveSurfaceBallastUsesShipMass.Value)
      {
        var aboveSurfaceVal = GetMaxAboveSurfaceFromShipWeight();
        return aboveSurfaceVal;
      }

      if (WaterConfig.WaterBallastEnabled.Value)
      {
        var aboveSurfaceVal = GetMaxAboveSurfaceFromOnboardExtents();
        return aboveSurfaceVal;
      }

      return 0f;
    }

    /// <summary>
    ///   A negative offset pushing collider lower, forcing ship higher
    ///   todo This will need to be leverage for calculating ship high in flight too
    /// </summary>
    /// <returns></returns>
    public float GetSurfaceOffset()
    {
      if (PropulsionConfig.AllowFlight.Value)
        // fly up to 2000f in the sky.
        return MaxFlightOffset;

      return GetSurfaceOffsetWaterVehicleOnly();
    }

    /// <summary>
    ///   Max depth the vehicle can go underwater or on ground.
    ///   A (in water) positive offset that pushes the collider upwards, dropping the
    ///   vehicle lower
    /// </summary>
    public float GetMaxDepthOffset()
    {
      var highestGroundPoint = GetHighestGroundPoint(ShipFloatationObj);
      cachedMaxDepthOffset =
        highestGroundPoint - ZoneSystem.instance.m_waterLevel;
      return cachedMaxDepthOffset;
    }

    /// <summary>
    ///   Supports force updates in case we need to update the target based on an
    ///   emergency.
    /// </summary>
    /// <param name="rawValue">
    ///   Negative makes the ship go upwards, positive makes the
    ///   ship go downwards
    /// </param>
    /// <param name="forceUpdate"></param>
    public void UpdateTargetHeight(float rawValue, bool forceUpdate = false)
    {
      if (VehicleInstance?.Instance == null || m_nview == null) return;

      _previousTargetHeight = TargetHeight;
      var maxSurfaceLevelOffset = GetSurfaceOffset();
      var maxDepthOffset = GetMaxDepthOffset();

      var clampedValue = Mathf.Clamp(
        rawValue,
        maxDepthOffset, maxSurfaceLevelOffset);

      var canForceUpdate = lastForceUpdateTimer == 0f && forceUpdate;
      var timeMult = canForceUpdate
        ? 0.5f
        : Time.fixedDeltaTime * PropulsionConfig.VerticalSmoothingSpeed.Value * 10;

      TargetHeight = Mathf.Lerp(_previousTargetHeight, clampedValue, timeMult);

      if (Mathf.Approximately(_previousTargetHeight, TargetHeight)) return;


      m_nview.GetZDO().Set(VehicleZdoVars.VehicleTargetHeight, TargetHeight);

      VehicleInstance.Instance.UpdateShipEffects();

      if (lastSyncTargetHeight > 2f)
      {
        m_nview.InvokeRPC(0, nameof(RPC_TargetHeight), TargetHeight);
        lastSyncTargetHeight = 0f;
      }
      else
      {
        lastSyncTargetHeight += Time.fixedDeltaTime;
      }

      if (forceUpdate && lastForceUpdateTimer is >= 0f and < 0.2f)
        lastForceUpdateTimer += Time.fixedDeltaTime;
      else if (lastForceUpdateTimer > 0f) lastForceUpdateTimer = 0f;
    }

    public void AutoAscendUpdate()
    {
      if (!_isAscending || _isAscendToggled || _isDescendToggled) return;
      Ascend();
    }

    public void AutoDescendUpdate()
    {
      if (!_isDescending || _isDescendToggled || _isAscendToggled) return;
      Descend();
    }

    public void AutoVerticalFlightUpdate()
    {
      if (!PropulsionConfig.AllowFlight.Value ||
          !PropulsionConfig.FlightVerticalToggle.Value ||
          isAnchored) return;

      if (Mathf.Approximately(TargetHeight, 200f) ||
          Mathf.Approximately(TargetHeight, ZoneSystem.instance.m_waterLevel))
      {
        ClearAutoClimbState();
        return;
      }

      AutoAscendUpdate();
      AutoDescendUpdate();
    }


    private void ToggleAutoDescend()
    {
      if (IsNotFlying)
      {
        ClearAutoClimbState();
        return;
      }

      if (!PropulsionConfig.FlightVerticalToggle.Value) return;

      if (!_isDescendToggled && _isDescending)
      {
        ClearAutoClimbState();
        return;
      }

      _isAscending = false;
      _isDescending = true;
    }

    public static bool ShouldHandleControls()
    {
      Character character = Player.m_localPlayer;
      if (!character) return false;

      var isAttachedToShip = character.IsAttachedToShip();
      var isAttached = character.IsAttached();

      return isAttached && isAttachedToShip;
    }

    public static void DEPRECATED_OnFlightControls(
      MoveableBaseShipComponent mbShip)
    {
      if (ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump"))
        mbShip.Ascend();
      else if (ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch"))
        mbShip.Descent();
    }

    public void ClearAutoClimbState()
    {
      _isAscending = false;
      _isDescending = false;
    }

    public void OnFlightControls()
    {
      if (isCreative || isPlayerHaulingVehicle || IsBallastAndFlightDisabled || isAnchored) return;

      var isAscendKeyPressed = GetAscendKeyPress;
      var isDescendKeyPressed = GetDescendKeyPress;

      if (isAscendKeyPressed && isDescendKeyPressed)
      {
        _isAscendToggled = false;
        _isDescendToggled = false;
        _isDescending = false;
        _isAscending = false;
        return;
      }

      if (PropulsionConfig.FlightVerticalToggle.Value)
      {
        if (isAscendKeyPressed)
        {
          _isAscendToggled = !isDescendKeyPressed && isAscendKeyPressed;
          _isDescendToggled = false;
        }
        if (isDescendKeyPressed)
        {
          _isDescendToggled = !isAscendKeyPressed && isDescendKeyPressed;
          _isAscendToggled = false;
        }
      }

      if (isAscendKeyPressed)
      {
        Ascend();
        ToggleAutoAscend();
        return;
      }

      if (!CanDescend && isDescendKeyPressed && TargetHeight != 0f)
      {
        UpdateTargetHeight(0f);
        return;
      }

      if (CanDescend && isDescendKeyPressed)
      {
        Descend();
        ToggleAutoDescend();
      }
    }

    public static bool GetAnchorKeyUp()
    {
      if (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
          "False" &&
          ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
          "Not set")
      {
        var mainKeyString = ValheimRaftPlugin.Instance.AnchorKeyboardShortcut
          .Value.MainKey
          .ToString();
        var buttonDownDynamic =
          ZInput.GetButtonUp(mainKeyString);

        if (buttonDownDynamic)
          Logger.LogDebug($"Dynamic Anchor Button down: {mainKeyString}");

        return buttonDownDynamic ||
               ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsUp();
      }

      var isPressingRun =
        ZInput.GetButtonUp("Run") || ZInput.GetButtonUp("JoyRun");
      var isPressingJoyRun = ZInput.GetButtonUp("JoyRun");

      return isPressingRun || isPressingJoyRun;
    }

    public static bool GetAnchorKeyDown()
    {
      if (ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
          "False" &&
          ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.ToString() !=
          "Not set")
      {
        var mainKeyString = ValheimRaftPlugin.Instance.AnchorKeyboardShortcut
          .Value.MainKey
          .ToString();
        var buttonDownDynamic =
          ZInput.GetButtonDown(mainKeyString);

        if (buttonDownDynamic)
          Logger.LogDebug($"Dynamic Anchor Button down: {mainKeyString}");

        return buttonDownDynamic ||
               ValheimRaftPlugin.Instance.AnchorKeyboardShortcut.Value.IsDown();
      }

      var isPressingRun =
        ZInput.GetButtonDown("Run") || ZInput.GetButtonDown("JoyRun");
      var isPressingJoyRun = ZInput.GetButtonDown("JoyRun");

      return isPressingRun || isPressingJoyRun;
    }

    private void OnAnchorKeyPress()
    {
      if (!CanAnchor) return;
      if (GetAnchorKeyUp())
      {
        _isHoldingAnchor = false;
        return;
      }

      var isAnchorKeyDown = GetAnchorKeyDown();
      if (!isAnchorKeyDown)
      {
        if (_isHoldingAnchor) _isHoldingAnchor = false;

        return;
      }

      if (_isHoldingAnchor)
      {
        Logger.LogDebug(
          "Anchor key skipped due to update already fired currently pending");
        return;
      }

      Logger.LogDebug("Anchor Keydown is pressed");
      var flag = HaveControllingPlayer();
      if (flag && Player.m_localPlayer.IsAttached() &&
          Player.m_localPlayer.m_attachPoint &&
          Player.m_localPlayer.m_doodadController != null)
      {
        Logger.LogDebug("toggling vehicleShip anchor");
        ToggleAnchor();
        _isHoldingAnchor = true;
      }
      else
      {
        Logger.LogDebug("Player not controlling ship, skipping");
      }
    }

    private void OnControllingWithHotKeyPress()
    {
      var hasControllingPlayer = HaveControllingPlayer();

      // Edge case but the player could be detached. This guard allows the next anchor click if returning to the controls.
      if (!hasControllingPlayer)
      {
        if (_isHoldingAnchor) _isHoldingAnchor = false;

        return;
      }

      OnAnchorKeyPress();
      OnFlightControls();
    }


/*
 * Toggle the ship anchor and emit the event to other players so their client can update
 */
    public void ToggleAnchor()
    {
      // Flying does not animate anchor.
      if (IsFlying() || VehicleInstance!.IsLandVehicle)
      {
        SendSetAnchor(!isAnchored ? AnchorState.Anchored : AnchorState.Recovered);
        if (WheelController != null)
        {
          WheelController.SetBrake(isAnchored);
        }
        return;
      }

      var newState = isAnchored
        ? AnchorState.Reeling
        : AnchorState.Lowering;

      SendSetAnchor(newState);
    }


    private void ToggleAutoAscend()
    {
      if (IsNotFlying)
      {
        ClearAutoClimbState();
        return;
      }

      if (!PropulsionConfig.FlightVerticalToggle.Value) return;

      if (!_isAscendToggled && _isAscending)
      {
        ClearAutoClimbState();
        return;
      }

      _isAscending = true;
      _isDescending = false;
    }

    public void SyncRudder(float rudder)
    {
      m_nview.InvokeRPC(0L, nameof(RPC_Rudder), rudder);
    }


    internal void RPC_Rudder(long sender, float value)
    {
      m_rudderValue = value;
      if (WheelController != null)
      {
        WheelController.SetTurnInput(Mathf.Clamp(m_rudderValue, -1, 1));
      }
    }

    /// <summary>
    ///   Setter method for anchor, directly calling this before invoking ZDO call will
    ///   cause de-syncs so this should only be used in the RPC
    /// </summary>
    /// <param name="anchorState"></param>
    /// <returns></returns>
    private AnchorState HandleSetAnchor(AnchorState anchorState)
    {
      var isAnchorDropped = IsAnchorDropped(anchorState);
      if (isAnchorDropped)
      {
        ClearAutoClimbState();
        // only stops speed if the anchor is dropped.
        vehicleSpeed = Ship.Speed.Stop;
      }

      return anchorState;
    }

    public void SendToggleOceanSway()
    {
      m_nview.InvokeRPC(0, nameof(RPC_SetOceanSway), !HasOceanSwayDisabled);
    }

    public void RPC_SetOceanSway(long sender, bool state)
    {
      if (!m_nview) return;
      var isOwner = m_nview.IsOwner();
      if (isOwner)
      {
        var zdo = m_nview.GetZDO();
        zdo.Set(VehicleZdoVars.VehicleOceanSway, state);
      }

      SyncShip();
    }

    /// <summary>
    ///   SyncZDOs if they are out of alignment
    /// </summary>
    private void SyncShip()
    {
      if (ZNetView.m_forceDisableInit || zsyncTransform == null ||
          PiecesController == null) return;
      SyncAnchor();
      SyncOceanSway();
    }

    private void SyncOceanSway()
    {
      if (ZNetView.m_forceDisableInit) return;
      if (!m_nview) return;

      var zdo = m_nview.GetZDO();
      if (zdo == null) return;

      var isEnabled = zdo.GetBool(VehicleZdoVars.VehicleOceanSway);
      HasOceanSwayDisabled = isEnabled;
    }

    private void SyncAnchor()
    {
      if (ZNetView.m_forceDisableInit) return;
      if (!isActiveAndEnabled) return;
      // exit if we do not have anchor prefab or m_nview
      if (!CanAnchor || m_nview == null)
      {
        vehicleAnchorState = AnchorState.Idle;
        return;
      }

      if (m_nview.isActiveAndEnabled != true) return;

      var isNotAnchoredWithNobodyOnboard =
        OnboardController.m_localPlayers.Count == 0 && !isAnchored;

      if (isNotAnchoredWithNobodyOnboard)
      {
        if (VehicleDebugConfig.HasAutoAnchorDelay.Value) return;
        SendSetAnchor(AnchorState.Anchored);
        return;
      }

      var zdoAnchorState =
        AnchorMechanismController.GetSafeAnchorState(m_nview.GetZDO()
          .GetInt(VehicleZdoVars.VehicleAnchorState, (int)vehicleAnchorState));

      if (vehicleAnchorState != zdoAnchorState)
      {
        Logger.LogDebug(
          $"anchorState {vehicleAnchorState} zdoAnchorState {zdoAnchorState}");
        vehicleAnchorState = zdoAnchorState;
      }

      if (PiecesController != null)
        PiecesController.UpdateAnchorState(vehicleAnchorState);
    }

    /// <summary>
    ///   Method to be called only from a direct setter (as a fallback) or
    ///   RPC_SetAnchor
    /// </summary>
    /// <param name="state"></param>
    /// <param name="hasOverride"></param>
    public void SetAnchor(AnchorState state, bool hasOverride = false)
    {
      var newFlags = HandleSetAnchor(state);
      Logger.LogDebug(
        $"Setting anchor to: {state} the new movementFlag should be {newFlags}");

      if (m_nview.IsOwner() || hasOverride)
      {
        var zdo = m_nview.GetZDO();
        zdo.Set(VehicleZdoVars.VehicleAnchorState, (int)state);
      }

      vehicleAnchorState = state;
    }

    public void RPC_SetAnchor(long sender, int state)
    {
      var safeAnchorState = AnchorMechanismController.GetSafeAnchorState(state);
      if (PiecesController != null)
        PiecesController.UpdateAnchorState(safeAnchorState);
      SetAnchor(safeAnchorState);
    }

    /// <summary>
    ///   Generate syncing fixes to fix "accidents" were the player is not detected in
    ///   other clients and not put within the ship.
    /// </summary>
    /// <param name="player"></param>
    public void FixPlayerParent(Player player)
    {
      if (PiecesController == null || player.transform.root == null) return;
      if (player.transform.root != PiecesController.transform)
        player.transform.SetParent(PiecesController.transform);
    }

    public void UpdatePlayerOnShip(Player player)
    {
      OnboardController.TryAddPlayerIfMissing(player);
      FixPlayerParent(player);
    }

    public void OnControlsHandOff(Player? targetPlayer, Player? previousPlayer)

    {
      if (targetPlayer == null || !VehicleInstance?.Instance ||
          PiecesController == null ||
          m_nview == null || m_nview.m_zdo == null)
        return;

      EjectPreviousPlayerFromControls(previousPlayer);
      UpdatePlayerOnShip(targetPlayer);
      UpdateVehicleSpeedThrottle();
      VehicleOnboardController.AddOrRemovePlayerBlockingCamera(targetPlayer);

      var isLocalPlayer = targetPlayer == Player.m_localPlayer;

      var previousUserId = previousPlayer?.GetPlayerID() ?? 0L;
      // the person controlling the ship should control physics
      var playerOwner = targetPlayer.GetOwner();

      m_nview.GetZDO().SetOwner(playerOwner);
      if (previousUserId != targetPlayer.GetPlayerID() || previousUserId == 0L)
        m_nview.GetZDO().Set(ZDOVars.s_user, targetPlayer.GetPlayerID());

      Logger.LogDebug("Changing ship owner to " + playerOwner +
                      $", name: {targetPlayer.GetPlayerName()}");

      SyncVehicleBounds();
      var attachTransform = lastUsedWheelComponent.AttachPoint;

      // local player only.
      if (isLocalPlayer) targetPlayer.StartDoodadControl(lastUsedWheelComponent);

      if (attachTransform == null) return;

      // non-local player too as this will show them controlling the object.
      targetPlayer.AttachStart(attachTransform, null,
        false, false,
        true, m_attachAnimation, detachOffset);

      if (VehicleInstance.Instance != null &&
          lastUsedWheelComponent.wheelTransform != null)
        VehicleInstance.Instance.m_controlGuiPos =
          lastUsedWheelComponent.wheelTransform;
    }

    private void EjectPreviousPlayerFromControls(Player? player)
    {
      if (player == null) return;
      player.m_doodadController = null;
      player.AttachStop();
    }

    private void RPC_RequestResponse(long sender, bool granted,
      long targetPlayerId, long previousPlayerId)
    {
      CancelDebounceTakeoverControls();

      if (!Player.m_localPlayer || !VehicleInstance?.Instance) return;

      if (granted)
      {
        OnControlsHandOff(Player.GetPlayer(targetPlayerId),
          Player.GetPlayer(previousPlayerId));
        // lets the player know they are disconnected.
        if (Player.m_localPlayer ==
            Player.GetPlayer(previousPlayerId) &&
            targetPlayerId != previousPlayerId)
          Player.m_localPlayer.Message(MessageHud.MessageType.Center,
            "$valheim_vehicles_wheel_ejected");
      }

      if (!granted &&
          Player.m_localPlayer == Player.GetPlayer(targetPlayerId))
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, InUseMessage);
    }


    public bool IsWithinWheelDeadZone()
    {
      return m_rudderValue >= -PropulsionConfig.WheelDeadZone.Value &&
             m_rudderValue <=
             PropulsionConfig.WheelDeadZone.Value;
    }

    public bool IsPlayerOnboardLocalShip(Player player)
    {
      return OnboardController.m_localPlayers.Contains(player);
    }

    public void SendSpeedChange(DirectionChange directionChange)
    {
      if (isAnchored) return;

      switch (directionChange)
      {
        case DirectionChange.Forward:
          SetForward();
          break;
        case DirectionChange.Backward:
          SetBackward();
          break;
        case DirectionChange.Stop:
        default:
          vehicleSpeed = Ship.Speed.Stop;
          break;
      }

      m_nview.InvokeRPC(0, nameof(RPC_SpeedChange), (int)vehicleSpeed);
    }

    internal void RPC_SpeedChange(long sender, int speed)
    {
      if (isAnchored && PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
        vehicleAnchorState = HandleSetAnchor(AnchorState.Reeling);

      vehicleSpeed = (Ship.Speed)speed;

      UpdateLandVehicleStatsIfNecessary();
    }


    private void SetForward()
    {
      if (isAnchored && !PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
      {
        vehicleSpeed = Ship.Speed.Stop;
        return;
      }

      switch (vehicleSpeed)
      {
        case Ship.Speed.Stop:
          vehicleSpeed = Ship.Speed.Slow;
          break;
        case Ship.Speed.Slow:
          vehicleSpeed = Ship.Speed.Half;
          break;
        case Ship.Speed.Half:
          vehicleSpeed = Ship.Speed.Full;
          break;
        case Ship.Speed.Back:
          vehicleSpeed = Ship.Speed.Stop;
          break;
        case Ship.Speed.Full:
          break;
        default:
          Logger.LogWarning($"Recieved a unknown Ship.Speed, {vehicleSpeed}");
          break;
      }
    }

    private void SetBackward()
    {
      if (isAnchored && !PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value)
      {
        vehicleSpeed = Ship.Speed.Stop;
        return;
      }

      switch (vehicleSpeed)
      {
        case Ship.Speed.Stop:
          vehicleSpeed = Ship.Speed.Back;
          break;
        case Ship.Speed.Slow:
          vehicleSpeed = Ship.Speed.Stop;
          break;
        case Ship.Speed.Half:
          vehicleSpeed = Ship.Speed.Slow;
          break;
        case Ship.Speed.Full:
          vehicleSpeed = Ship.Speed.Half;
          break;
        case Ship.Speed.Back:
          break;
      }
    }

    public void SendReleaseControl(Player player)
    {
      CancelDebounceTakeoverControls();
      if (m_nview == null) return;
      if (!m_nview.IsValid()) return;
      m_nview.InvokeRPC(ZRoutedRpc.Everybody, nameof(RPC_ReleaseControl),
        player.GetPlayerID());
    }

    public bool HaveValidUser()
    {
      var user = GetUser();
      if (VehicleInstance?.Instance == null) return false;
      return user != 0L && IsPlayerInBoat(user);
    }

    /// <summary>
    /// If moder power is enabled
    /// </summary>
    /// <returns></returns>
    public bool IsWindControllActive()
    {
      if (OnboardController == null) return false;
      if (lastUpdateWindControlStatus < 2f)
      {
        lastUpdateWindControlStatus += Time.fixedDeltaTime;
        return m_cachedWindControlStatus;
      }

      foreach (var player in OnboardController.m_localPlayers)
        if (player.GetSEMan()
            .HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
        {
          m_cachedWindControlStatus = true;
          return m_cachedWindControlStatus;
        }

      m_cachedWindControlStatus = false;
      return m_cachedWindControlStatus;
    }

    /// <summary>
    ///   Todo may need to cache this as this can be called a lot of times.
    /// </summary>
    /// <returns></returns>
    private long GetUser()
    {
      if (!m_nview) return 0L;
      return !m_nview.IsValid()
        ? 0L
        : m_nview.GetZDO().GetLong(ZDOVars.s_user);
    }
  }