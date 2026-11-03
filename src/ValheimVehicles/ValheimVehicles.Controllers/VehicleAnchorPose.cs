// Portions adapted from ValheimRAFTAnchorFix. See licenses/ValheimRAFTAnchorFix-MIT.txt.
using System.Runtime.CompilerServices;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Controllers
{
    internal static class VehicleAnchorPose
    {
        internal const string ValidKey = "L4zerShark.RaftAnchorFix.v1.valid";
        internal const string PositionKey = "L4zerShark.RaftAnchorFix.v1.position";
        internal const string RotationKey = "L4zerShark.RaftAnchorFix.v1.rotation";
        internal const string LockedAxesKey = "L4zerShark.RaftAnchorFix.v2.lockedAxes";
        private const int Horizontal = 1, Vertical = 2, AllAxes = Horizontal | Vertical;
        private const string AnchorKey = "VehicleAnchorState";
        private sealed class LocalState
        {
            internal bool Held, CaptureCurrent;
            internal RigidbodyConstraints PreviousConstraints;
            internal int PendingAnchor = -1;
            internal float PendingSince;
            internal Vector3 PendingPosition;
            internal Quaternion PendingRotation;
        }
        private static readonly ConditionalWeakTable<VehicleMovementController, LocalState> States =
            new ConditionalWeakTable<VehicleMovementController, LocalState>();

        internal static bool IsAnchored(VehicleMovementController ship)
        {
            return ship && ship.m_nview && ship.m_nview.IsValid() &&
                ship.m_nview.GetZDO().GetInt(AnchorKey) == (int)AnchorState.Anchored;
        }

        internal static bool TryPendingAnchor(VehicleMovementController ship, out AnchorState anchor)
        {
            anchor = AnchorState.Idle;
            if (!ship || !ship.m_nview || !ship.m_nview.IsValid() || !States.TryGetValue(ship, out var state) || state.PendingAnchor < 0)
                return false;
            // RPCs may beat ZDO replication. Apply the immediate local transition,
            // but never let an old RPC outrank owner data indefinitely.
            if (ship.m_nview.IsOwner() || ship.m_nview.GetZDO().GetInt(AnchorKey) == state.PendingAnchor ||
                Time.realtimeSinceStartup - state.PendingSince >= 2f)
            {
                state.PendingAnchor = -1;
                return false;
            }
            anchor = (AnchorState)state.PendingAnchor;
            return true;
        }

        internal static void ReleaseLocal(VehicleMovementController ship)
        {
            if (!ship || !States.TryGetValue(ship, out var state) || !state.Held) return;
            if (ship.m_body) ship.m_body.constraints = state.PreviousConstraints;
            state.Held = false;
        }

        internal static void Suspend(VehicleMovementController ship)
        {
            if (!ship) return;
            ReleaseLocal(ship);
            States.GetOrCreateValue(ship).CaptureCurrent = true;
            if (ship.m_nview && ship.m_nview.IsValid() && ship.m_nview.IsOwner())
                ship.m_nview.GetZDO().Set(ValidKey, false);
        }

        internal static void AnchorChanged(VehicleMovementController ship, AnchorState anchor)
        {
            if (!ship || !ship.m_nview || !ship.m_nview.IsValid()) return;
            var local = States.GetOrCreateValue(ship);
            if (!ship.m_nview.IsOwner() && ship.m_body)
            {
                local.PendingAnchor = (int)anchor;
                local.PendingSince = Time.realtimeSinceStartup;
                local.PendingPosition = ship.m_body.position;
                local.PendingRotation = ship.m_body.rotation;
            }
            else local.PendingAnchor = -1;
            if (anchor != AnchorState.Anchored)
            {
                // A remote client does not get to invalidate the owner's snapshot.
                if (ship.m_nview.IsOwner()) ship.m_nview.GetZDO().Set(ValidKey, false);
                ReleaseLocal(ship);
                States.GetOrCreateValue(ship).CaptureCurrent = true;
                return;
            }
            // Newly dropping the anchor captures the live body, not the last
            // network tick's transform. Repeated Anchored RPCs retain the snapshot.
            if (ship.m_nview.IsOwner() && !ship.m_nview.GetZDO().GetBool(ValidKey))
                States.GetOrCreateValue(ship).CaptureCurrent = true;
            Hold(ship);
        }

        internal static bool Hold(VehicleMovementController? ship)
        {
            if (!ship || !ship.m_nview || !ship.m_nview.IsValid() || !ship.m_body || ship.Manager == null)
                return false;
            var zdo = ship.m_nview.GetZDO();
            var state = States.GetOrCreateValue(ship);
            int configuredAxes = (AnchorConfig.HorizontalLock.Value ? Horizontal : 0) |
                (AnchorConfig.VerticalLock.Value ? Vertical : 0);
            bool pending = TryPendingAnchor(ship, out var pendingAnchor);
            if (pending ? pendingAnchor != AnchorState.Anchored : !IsAnchored(ship))
            {
                ReleaseLocal(ship);
                if (ship.m_nview.IsOwner() && zdo.GetBool(ValidKey)) zdo.Set(ValidKey, false);
                return false;
            }
            // A docked child must follow its parent; deliberate relocation gets a
            // fresh parking position afterwards. Never fight upstream teleports.
            if (ship.Manager.IsLandVehicle || ship.Manager.VehicleParent != null || zdo.GetInt("MBParentId") != 0 ||
                ship.isCreative || ship.IsTeleporting || ship.isPlayerHaulingVehicle)
            {
                Suspend(ship);
                return false;
            }
            Vector3 position;
            Quaternion rotation;
            // Version 1 held every axis. Its old height is ignored whenever the
            // vertical lock is disabled, without changing existing saved X/Z.
            int savedAxes = zdo.GetInt(LockedAxesKey, AllAxes) & AllAxes;
            int appliedAxes = configuredAxes;
            bool valid = pending || zdo.GetBool(ValidKey);
            if (pending)
            {
                position = state.PendingPosition;
                rotation = state.PendingRotation;
                valid = Finite(position) && Finite(rotation);
            }
            else if (valid)
            {
                position = zdo.GetVec3(PositionKey, zdo.GetPosition());
                rotation = zdo.GetQuaternion(RotationKey, zdo.GetRotation());
                valid = Finite(position) && Finite(rotation);
            }
            else { position = default; rotation = default; }
            if (!valid)
            {
                // On load, use the saved ZDO transform before water physics can
                // change it. Existing boats need no conversion or replacement.
                bool current = state.CaptureCurrent && ship.m_nview.IsOwner();
                position = current ? ship.m_body.position : zdo.GetPosition();
                rotation = current ? ship.m_body.rotation : zdo.GetRotation();
                if (!Finite(position) || !Finite(rotation)) { ReleaseLocal(ship); return false; }
                if (ship.m_nview.IsOwner())
                {
                    zdo.Set(PositionKey, position);
                    zdo.Set(RotationKey, rotation);
                    zdo.Set(LockedAxesKey, configuredAxes);
                    zdo.Set(ValidKey, true);
                }
                savedAxes = configuredAxes;
            }
            else if (!pending && savedAxes != configuredAxes)
            {
                if (ship.m_nview.IsOwner())
                {
                    // An axis may have moved while unlocked. Lock it where it is
                    // now, retaining the reference for axes that stayed locked.
                    int newlyLocked = configuredAxes & ~savedAxes;
                    position = ProjectPosition(position, ship.m_body.position, newlyLocked);
                    if (!Finite(position)) { ReleaseLocal(ship); return false; }
                    zdo.Set(PositionKey, position);
                    zdo.Set(LockedAxesKey, configuredAxes);
                }
                else
                {
                    // Wait for the owner's new reference rather than snapping a
                    // newly locked remote axis back to its historical snapshot.
                    appliedAxes &= savedAxes;
                }
            }
            state.CaptureCurrent = false;
            if (!state.Held)
            {
                state.PreviousConstraints = ship.m_body.constraints;
                state.Held = true;
            }
            ship.vehicleAnchorState = AnchorState.Anchored;
            RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotation;
            if ((appliedAxes & Horizontal) != 0)
                constraints |= RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            if ((appliedAxes & Vertical) != 0) constraints |= RigidbodyConstraints.FreezePositionY;
            // Hold runs several times per tick. Rigidbody setters can wake the body,
            // so assign only on change; otherwise a settled remote replica can be
            // woken again right after ClientSync puts it to sleep.
            if (ship.m_body.constraints != constraints) ship.m_body.constraints = constraints;
            if (!ship.m_body.isKinematic)
            {
                Vector3 velocity = ship.m_body.linearVelocity;
                Vector3 heldVelocity = ProjectPosition(velocity, Vector3.zero, appliedAxes);
                if (!velocity.Equals(heldVelocity)) ship.m_body.linearVelocity = heldVelocity;
                if (!ship.m_body.angularVelocity.Equals(Vector3.zero)) ship.m_body.angularVelocity = Vector3.zero;
            }
            // Non-kinematic client sync can update the Transform before the body.
            // Preserve each one's free coordinates so clamping does not undo its Y interpolation.
            Vector3 bodyPosition = ProjectPosition(ship.m_body.position, position, appliedAxes);
            Vector3 transformPosition = ProjectPosition(ship.transform.position, position, appliedAxes);
            // Physics may normalize a quaternion or round a position on assignment.
            // Allow for float precision at the saved world position without
            // moving the reference, so small rounding errors cannot accumulate.
            float positionToleranceSqr = PositionToleranceSqr(position);
            if ((ship.m_body.position - bodyPosition).sqrMagnitude > positionToleranceSqr) ship.m_body.position = bodyPosition;
            if (!SameRotation(ship.m_body.rotation, rotation)) ship.m_body.rotation = rotation;
            if ((ship.transform.position - transformPosition).sqrMagnitude > positionToleranceSqr ||
                !SameRotation(ship.transform.rotation, rotation))
                ship.transform.SetPositionAndRotation(transformPosition, rotation);
            if (ship.m_nview.IsOwner())
            {
                if (!zdo.GetPosition().Equals(bodyPosition)) zdo.SetPosition(bodyPosition);
                if (!zdo.GetRotation().Equals(rotation)) zdo.SetRotation(rotation);
                SetIfChanged(zdo, ZDOVars.s_velHash, appliedAxes);
                SetIfChanged(zdo, ZDOVars.s_bodyVelHash, appliedAxes);
                if (!zdo.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero).Equals(Vector3.zero))
                    zdo.Set(ZDOVars.s_bodyAVelHash, Vector3.zero);
            }
            return true;
        }

        private static float PositionToleranceSqr(Vector3 reference)
        {
            // 0.1 mm, or about two float ULPs at this coordinate if larger
            // (one ULP exceeds 0.1 mm beyond 1024 m from the world origin).
            float magnitude = Mathf.Max(Mathf.Abs(reference.x), Mathf.Max(Mathf.Abs(reference.y), Mathf.Abs(reference.z)));
            float tolerance = Mathf.Max(0.0001f, magnitude * 2.5e-7f);
            return tolerance * tolerance;
        }

        private static bool SameRotation(Quaternion first, Quaternion second)
        {
            first = first.normalized;
            second = second.normalized;
            float sign = Quaternion.Dot(first, second) < 0f ? -1f : 1f;
            float x = first.x - sign * second.x, y = first.y - sign * second.y;
            float z = first.z - sign * second.z, w = first.w - sign * second.w;
            // Compare equivalent unit quaternions directly. Angle's dot product
            // can report a false change when a stored quaternion is slightly short of unit length.
            return x * x + y * y + z * z + w * w <= 0.0000000001f;
        }

        private static void SetIfChanged(ZDO zdo, int hash, int axes)
        {
            Vector3 current = zdo.GetVec3(hash, Vector3.zero);
            Vector3 held = ProjectPosition(current, Vector3.zero, axes);
            if (!current.Equals(held)) zdo.Set(hash, held);
        }

        private static Vector3 ProjectPosition(Vector3 current, Vector3 reference, int axes)
        {
            if ((axes & Horizontal) != 0) { current.x = reference.x; current.z = reference.z; }
            if ((axes & Vertical) != 0) current.y = reference.y;
            return current;
        }

        private static bool Number(float x) { return !float.IsNaN(x) && !float.IsInfinity(x); }
        private static bool Finite(Vector3 x) { return Number(x.x) && Number(x.y) && Number(x.z); }
        private static bool Finite(Quaternion x)
        {
            float length = x.x * x.x + x.y * x.y + x.z * x.z + x.w * x.w;
            return Number(x.x) && Number(x.y) && Number(x.z) && Number(x.w) && length > 0.99f && length < 1.01f;
        }
    }
}
