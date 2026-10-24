// Portions adapted from ValheimRAFTAnchorFix. See licenses/ValheimRAFTAnchorFix-MIT.txt.
using UnityEngine;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Controllers;

public partial class VehicleMovementController
{
  private const string AnchorStateKey = "VehicleAnchorState";
  private const float AnchorRequestLifetime = 5f;

  private sealed class PendingAnchorInput
  {
    internal Player Player = null!;
    internal SteeringWheelComponent Wheel = null!;
    internal long PlayerId, World;
    internal int OriginalState;
    internal float Started;
  }

  private PendingAnchorInput? _pendingAnchorInput;

  private bool TryGetLocalAnchorHelm(out Player player, out SteeringWheelComponent wheel)
  {
    player = Player.m_localPlayer;
    wheel = null!;
    if (!m_nview || !m_nview.IsValid() || Manager == null || !CanAnchor ||
        Manager.IsLandVehicle || IsFlying() || !player || player.IsDead() || player.IsTeleporting() ||
        !player.IsAttached() || !player.m_attachPoint || !HaveControllingPlayer() ||
        m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L) != player.GetPlayerID() ||
        ZNet.instance == null || ZNet.World == null ||
        (!ZNet.instance.IsServer() && ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)) return false;
    if (player.m_doodadController is not SteeringWheelComponent localWheel || !localWheel) return false;
    wheel = localWheel;
    return wheel.ControllersInstance != null && wheel.ControllersInstance.MovementController == this;
  }

  private bool IsSameAnchorRequest(PendingAnchorInput request, Player player, SteeringWheelComponent wheel)
  {
    return ZNet.instance != null && m_nview != null &&
           Time.realtimeSinceStartup - request.Started < AnchorRequestLifetime &&
           request.Player == player && request.Wheel == wheel && request.PlayerId == player.GetPlayerID() &&
           request.World == ZNet.instance.GetWorldUID() &&
           m_nview.GetZDO().GetInt(AnchorStateKey) == request.OriginalState;
  }

  // The helm can attach before physics ownership settles. Keep that input without
  // starting an animation whose owner-only completion would be lost.
  private bool QueueAnchorInputDuringHandoff()
  {
    if (!CanAnchor || GetAnchorKeyUp() || !GetAnchorKeyDown() || _isHoldingAnchor) return false;
    if (!TryGetLocalAnchorHelm(out var player, out var wheel) || m_nview == null) return false;
    if (_pendingAnchorInput != null)
    {
      if (IsSameAnchorRequest(_pendingAnchorInput, player, wheel))
      {
        _isHoldingAnchor = true;
        return true;
      }
      _pendingAnchorInput = null;
    }
    if (!_isControlHandoffInProgress) return false;
    _pendingAnchorInput = new PendingAnchorInput
    {
      Player = player,
      Wheel = wheel,
      PlayerId = player.GetPlayerID(),
      World = ZNet.instance.GetWorldUID(),
      OriginalState = m_nview.GetZDO().GetInt(AnchorStateKey),
      Started = Time.realtimeSinceStartup
    };
    _isHoldingAnchor = true;
    return true;
  }

  private void DispatchPendingAnchorInput()
  {
    var request = _pendingAnchorInput;
    if (request == null) return;
    if (!TryGetLocalAnchorHelm(out var player, out var wheel) || !IsSameAnchorRequest(request, player, wheel))
    {
      _pendingAnchorInput = null;
      return;
    }
    if (m_nview == null || _isControlHandoffInProgress) return;
    _pendingAnchorInput = null; // Clear before callbacks can re-enter.
    var wasHolding = _isHoldingAnchor;
    try
    {
      _isHoldingAnchor = false;
      SendSetAnchor(request.OriginalState == (int)AnchorState.Anchored ? AnchorState.Reeling : AnchorState.Lowering);
    }
    finally
    {
      _isHoldingAnchor = wasHolding;
    }
  }

  internal void ReleaseAnchorAfterRemoval()
  {
    if (!m_nview || !m_nview.IsValid() || Manager == null || Manager.IsLandVehicle || IsFlying()) return;
    _pendingAnchorInput = null;
    var wasHolding = _isHoldingAnchor;
    try
    {
      _isHoldingAnchor = false;
      SendSetAnchor(AnchorState.Idle);
    }
    finally { _isHoldingAnchor = wasHolding; }
  }

  internal void CompleteAnchorTransition(VehicleAnchorMechanismController anchor, AnchorState state)
  {
    if (!m_nview || !m_nview.IsValid() || !m_nview.IsOwner() || PiecesController == null ||
        !PiecesController.HasAnchorMechanism(anchor) || anchor.currentState != state) return;
    var previous = state == AnchorState.Anchored ? AnchorState.Lowering : AnchorState.Reeling;
    var completing = (state == AnchorState.Anchored || state == AnchorState.Recovered) &&
                     vehicleAnchorState == previous && m_nview.GetZDO().GetInt(AnchorStateKey) == (int)previous;
    // Other mechanism state changes mirror the ship state. Publishing them back
    // can overwrite the saved state with a stale local value, for example the
    // nested Idle -> Lowering step when a mirrored Anchored reaches an idle anchor.
    if (!completing) return;
    var wasHolding = _isHoldingAnchor;
    try
    {
      // The input debounce must not suppress a physical lowering/reeling completion.
      _isHoldingAnchor = false;
      SendSetAnchor(state);
    }
    finally
    {
      _isHoldingAnchor = wasHolding;
    }
  }
}
