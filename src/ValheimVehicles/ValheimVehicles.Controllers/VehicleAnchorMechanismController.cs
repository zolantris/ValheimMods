#region

  using System;
  using UnityEngine;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Constants;
  using ValheimVehicles.SharedScripts;

#endregion

  namespace ValheimVehicles.Controllers;

  /// <summary>
  /// An integration level component, meant to work with Valheim specific content / apis
  /// </summary>
  public class VehicleAnchorMechanismController : AnchorMechanismController
  {
    public const float maxAnchorDistance = 40f;

    public static void SyncHudAnchorValues()
    {
      HideAnchorTimer = HudConfig.HudAnchorMessageTimer.Value;
      HasAnchorTextHud = HudConfig.HudAnchorTextAboveAnchors.Value;
      foreach (var anchorMechanismController in Instances)
        anchorMechanismController.anchorTextSize =
          HudConfig.HudAnchorTextSize.Value;
    }

    public override void Awake()
    {
      base.Awake();
      CanUseHotkeys = false;
    }

    public VehicleMovementController? MovementController;
    private bool _hasAnchorGround;
    private Vector2 _anchorGroundXZ;
    private float _anchorGroundHeight;
    private float _nextAnchorGroundRefresh;

    protected override void Start()
    {
      base.Start();
      RestoreSavedAnchorVisual();
    }

    internal void RestoreSavedAnchorVisual()
    {
      if (!IsAnchorInitialized || MovementController == null || IsLandVehicle() ||
          MovementController.IsFlying() || !VehicleAnchorPose.IsAnchored(MovementController)) return;
      currentState = AnchorState.Anchored;
      UpdateHoverText(GetCurrentStateText());
      UpdateAnchorPositionIfNotNearGround();
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (currentState == AnchorState.Lowering) UpdateDistanceToGround();
    }

    private void LateUpdate()
    {
      if (currentState != AnchorState.Anchored || MovementController == null ||
          IsLandVehicle() || MovementController.IsFlying()) return;
      // The ship may bob while its deployed anchor stays on the seabed.
      UpdateAnchorPositionIfNotNearGround();
      UpdateRopeVisual();
    }

    private float GetAnchorGroundHeight(Vector3 position)
    {
      var xz = new Vector2(position.x, position.z);
      // A parked ship needs no terrain raycast every frame. Refresh periodically
      // for terrain edits, or when an unlocked ship moves horizontally.
      if (!_hasAnchorGround || Time.time >= _nextAnchorGroundRefresh ||
          (xz - _anchorGroundXZ).sqrMagnitude > 0.0001f)
      {
        _anchorGroundHeight = ZoneSystem.instance.GetGroundHeight(position);
        _anchorGroundXZ = xz;
        _nextAnchorGroundRefresh = Time.time + 1f;
        _hasAnchorGround = true;
      }
      return _anchorGroundHeight;
    }

    public float GetDistanceToGround()
    {
      var position = GetRaisedAnchorWorldPosition();
      var distanceFromAnchorToGround =
        position.y - ZoneSystem.instance.GetGroundHeight(position);
      return distanceFromAnchorToGround;
    }

    private Vector3 GetRaisedAnchorWorldPosition()
    {
      return anchorTransform.parent.TransformPoint(GetAnchorStartLocalPosition());
    }

    public void UpdateDistanceToGround()
    {
      var dtg = GetDistanceToGround();
      anchorDropDistance = Mathf.Clamp(dtg, 1f,
        maxAnchorDistance);
    }

    public static string GetCurrentStateTextStatic(AnchorState anchorState, bool isLandVehicle)
    {
      if (isLandVehicle)
      {
        return anchorState == AnchorState.Anchored ? ModTranslations.AnchorPrefab_breakingText : ModTranslations.AnchorPrefab_idleText;
      }

      return anchorState switch
      {
        AnchorState.Idle => "Idle",
        AnchorState.Lowering => ModTranslations.AnchorPrefab_loweringText,
        AnchorState.Anchored => ModTranslations.AnchorPrefab_anchoredText,
        AnchorState.Reeling => ModTranslations.AnchorPrefab_reelingText,
        AnchorState.Recovered => ModTranslations.AnchorPrefab_RecoveredAnchorText,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public bool IsLandVehicle()
    {
      return MovementController != null && MovementController.Manager != null && MovementController.Manager.IsLandVehicle;
    }

    public override string GetCurrentStateText()
    {
      return GetCurrentStateTextStatic(currentState, IsLandVehicle());
    }

    /// <summary>
    /// Place the deployed anchor below its raised rest, at the ground or the maximum drop distance.
    /// </summary>
    public void UpdateAnchorPositionIfNotNearGround()
    {
      // Piece activation can run before Awake captures the prefab's raised rest.
      if (!IsAnchorInitialized || !anchorTransform) return;
      var raisedPosition = GetRaisedAnchorWorldPosition();
      var deltaGround = raisedPosition.y - GetAnchorGroundHeight(raisedPosition);
      if (float.IsNaN(deltaGround) || float.IsInfinity(deltaGround)) return;
      // Lower in world space, matching the physical drop/reel direction even
      // when the boat or anchor mount is tilted.
      // Keep shallow anchors deployed instead of returning them to their raised
      // rest. A raised rest at or below the ground stays where it is.
      var drop = Mathf.Clamp(deltaGround, 0f, maxAnchorDistance);
      var deployedPosition = raisedPosition + Vector3.down * drop;
      if ((anchorTransform.position - deployedPosition).sqrMagnitude > 0.00000001f)
        anchorTransform.position = deployedPosition;
      AlignAnchorBody();
    }

    public override void OnAnchorStateChange(AnchorState newState)
    {
      // No callbacks for anchor when flying
      if (MovementController != null && MovementController.IsFlying())
      {
        return;
      }

      switch (newState)
      {
        case AnchorState.Idle:
          break;
        case AnchorState.Lowering:
          break;
        case AnchorState.Anchored:
          UpdateAnchorPositionIfNotNearGround();
          break;
        case AnchorState.Reeling:
          break;
        case AnchorState.Recovered:
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
      }

      if (MovementController != null && MovementController.m_nview.IsOwner())
        MovementController.CompleteAnchorTransition(this, newState);
    }
  }
