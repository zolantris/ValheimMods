using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;

// Portions adapted from ValheimRAFTAnchorFix. See licenses/ValheimRAFTAnchorFix-MIT.txt.
namespace AnchorIntegrationTests
{
    // Private disposable-world checks. These force only the handoff timing flag;
    // they do not simulate or claim a real network ownership transfer.
    internal static class HelmQueueChecks
    {
        private const string PatchId = "local.anchor.integration.queuechecks";
        private static readonly FieldInfo Handoff = AccessTools.Field(typeof(VehicleMovementController), "_isControlHandoffInProgress");
        private static readonly FieldInfo Doodad = AccessTools.Field(typeof(Player), "m_doodadController");
        private static VehicleMovementController countedShip;
        private static AnchorState countedState;
        private static int effectiveRequests;
        private static bool observeRemoteDispatch;
        private static int remoteDispatches;
        private sealed class Result { internal bool Success; }

        internal static IEnumerator Run(VehicleMovementController ship, SteeringWheelComponent wheel, Player player,
            Action<string, bool, string> record, Action<bool, bool> inject, Action assertIsolation)
        {
            if (assertIsolation == null || inject == null || record == null) throw new ArgumentNullException("Private guard/input/report callbacks required.");
            assertIsolation();
            if (countedShip || !ControlsReady(ship, wheel, player) || !ship.m_nview.IsOwner() || (bool)Handoff.GetValue(ship))
                throw new InvalidOperationException("An owned, settled, locally controlled test ship is required.");
            AnchorState original = ship.isAnchored ? AnchorState.Anchored : AnchorState.Recovered;
            if (!Settled(ship, original)) throw new InvalidOperationException("Physical anchors must be settled before queue checks.");
            var patches = new Harmony(PatchId);
            bool forcedHandoff = false;
            countedShip = ship;
            try
            {
                patches.Patch(AccessTools.Method(typeof(VehicleMovementController), "SendSetAnchor"),
                    prefix: new HarmonyMethod(typeof(HelmQueueChecks), nameof(BeforeSend)),
                    postfix: new HarmonyMethod(typeof(HelmQueueChecks), nameof(AfterSend)));
                // Exercise the controlling non-owner branch without sending a synthetic
                // ownership packet to the real server. The normal RPC method boundary is observed.
                long localOwner = ship.m_nview.GetZDO().GetOwner();
                try
                {
                    ship.m_nview.GetZDO().SetOwner(ZNet.instance.GetServerPeer().m_uid);
                    observeRemoteDispatch = true; remoteDispatches = 0;
                    Press(inject);
                    record("queue.nonowner-uses-normal-dispatch", remoteDispatches == 1 &&
                        AccessTools.Field(typeof(VehicleMovementController), "_pendingAnchorInput").GetValue(ship) == null,
                        "Controlled non-owner input reaches SendSetAnchor immediately, without an expiring queue; network send intercepted for this assertion.");
                }
                finally { observeRemoteDispatch = false; ship.m_nview.GetZDO().SetOwner(localOwner); }
                record("queue.timing-scope", true, "Controlled handoff timing flag on an already owned ship; actual helm key handler and normal animation run. No owner IDs are changed.");
                AnchorState target = original == AnchorState.Anchored ? AnchorState.Recovered : AnchorState.Anchored;
                countedState = target == AnchorState.Recovered ? AnchorState.Reeling : AnchorState.Lowering;
                effectiveRequests = 0;
                assertIsolation(); Handoff.SetValue(ship, true); forcedHandoff = true;
                Press(inject);
                yield return new WaitForSeconds(.1f);
                assertIsolation(); Press(inject);
                yield return new WaitForSeconds(.1f);
                record("queue.coalesced-while-pending", effectiveRequests == 0 && Settled(ship, original),
                    "Two distinct helm presses while handoff is held must leave the anchor unchanged; effective requests=" + effectiveRequests);
                Handoff.SetValue(ship, false); forcedHandoff = false;
                var wait = new Result();
                yield return WaitForState(ship, target, wait, assertIsolation);
                record("queue.dispatches-once", wait.Success && effectiveRequests == 1,
                    "After timing flag clears, exactly one effective transitional request must complete. count=" + effectiveRequests + "; completed=" + wait.Success);
                if (!wait.Success) goto Restore;

                assertIsolation(); Press(inject);
                yield return WaitForState(ship, original, wait, assertIsolation);
                if (!wait.Success) { record("queue.prepare-cancellation", false, "Could not restore original anchor through the helm."); goto Restore; }

                effectiveRequests = 0;
                assertIsolation(); Handoff.SetValue(ship, true); forcedHandoff = true;
                Press(inject);
                yield return new WaitForSeconds(.1f);
                AccessTools.Method(typeof(Player), "StopDoodadControl", Type.EmptyTypes).Invoke(player, null);
                yield return new WaitForSeconds(.2f);
                assertIsolation(); Handoff.SetValue(ship, false); forcedHandoff = false;
                yield return new WaitForSeconds(.5f);
                record("queue.cancelled-on-helm-release", !ControlsReady(ship, wheel, player) &&
                    effectiveRequests == 0 && Settled(ship, original),
                    "Leaving the helm before dispatch must cancel its queued input. count=" + effectiveRequests);

                yield return RegainControl(ship, wheel, player, wait, assertIsolation);
                record("queue.reacquired-control", wait.Success, "Normal wheel interaction regained settled ownership/control.");
                if (!wait.Success) goto Restore;
                yield return new WaitForSeconds(.3f);
                record("queue.cancelled-input-not-replayed", effectiveRequests == 0 && Settled(ship, original),
                    "Reacquiring the helm must not replay the cancelled input. count=" + effectiveRequests);
                effectiveRequests = 0;
                assertIsolation(); Press(inject);
                yield return WaitForState(ship, target, wait, assertIsolation);
                record("queue.fresh-input-after-cancellation", wait.Success && effectiveRequests == 1,
                    "A fresh helm press must work after cancellation. count=" + effectiveRequests + "; completed=" + wait.Success);
                if (!wait.Success) goto Restore;

                countedState = original == AnchorState.Recovered ? AnchorState.Reeling : AnchorState.Lowering;
                effectiveRequests = 0;
                assertIsolation(); Handoff.SetValue(ship, true); forcedHandoff = true;
                Press(inject);
                yield return new WaitForSeconds(5.3f);
                assertIsolation(); Handoff.SetValue(ship, false); forcedHandoff = false;
                yield return new WaitForSeconds(.3f);
                record("queue.expired-input-not-replayed", effectiveRequests == 0 && Settled(ship, target),
                    "An input held beyond its five-second lifetime must expire without changing the anchor.");
                Press(inject);
                yield return WaitForState(ship, original, wait, assertIsolation);
                record("queue.fresh-input-after-expiry", wait.Success && effectiveRequests == 1,
                    "A fresh press after expiry must still complete exactly once. count=" + effectiveRequests);

            Restore:
                if (forcedHandoff) { Handoff.SetValue(ship, false); forcedHandoff = false; }
                var restored = new Result();
                yield return RegainControl(ship, wheel, player, restored, assertIsolation);
                if (restored.Success && !Settled(ship, original))
                {
                    assertIsolation(); Press(inject);
                    yield return WaitForState(ship, original, restored, assertIsolation);
                }
                record("queue.original-state-restored", restored.Success && Settled(ship, original) && ControlsReady(ship, wheel, player),
                    "Original terminal anchor state and local helm control restored through normal input.");
            }
            finally
            {
                try { inject(false, true); }
                finally
                {
                    try { if (forcedHandoff && ship) Handoff.SetValue(ship, false); }
                    finally { countedShip = null; patches.UnpatchSelf(); }
                }
            }
        }

        private static void Press(Action<bool, bool> inject) { inject(true, false); inject(false, true); }
        private static bool ControlsReady(VehicleMovementController ship, SteeringWheelComponent wheel, Player player) =>
            ship && ship.m_nview && ship.m_nview.IsValid() && player && !player.IsDead() && player.IsAttached() &&
            ReferenceEquals(Doodad.GetValue(player), wheel) && wheel && wheel.ControllersInstance != null &&
            wheel.ControllersInstance.MovementController == ship && ship.m_nview.GetZDO().GetLong(ZDOVars.s_user, 0) == player.GetPlayerID();
        private static bool Settled(VehicleMovementController ship, AnchorState target)
        {
            if (!ship || !ship.m_nview || !ship.m_nview.IsValid() || ship.vehicleAnchorState != target ||
                ship.m_nview.GetZDO().GetInt("VehicleAnchorState") != (int)target) return false;
            var anchors = AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>().Where(a => a && a.MovementController == ship).ToArray();
            return anchors.Length > 0 && anchors.All(a => a.currentState == target || target == AnchorState.Recovered && a.currentState == AnchorState.Idle);
        }
        private static IEnumerator WaitForState(VehicleMovementController ship, AnchorState target, Result result, Action guard)
        {
            result.Success = false;
            float end = Time.realtimeSinceStartup + 30, stable = 0;
            while (Time.realtimeSinceStartup < end)
            {
                guard();
                if (Settled(ship, target))
                {
                    if (stable == 0) stable = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - stable >= .3f) { result.Success = true; yield break; }
                }
                else stable = 0;
                yield return new WaitForSeconds(.05f);
            }
        }
        private static IEnumerator RegainControl(VehicleMovementController ship, SteeringWheelComponent wheel, Player player, Result result, Action guard)
        {
            result.Success = false;
            float end = Time.realtimeSinceStartup + 18, retryAt = 0;
            int attempts = 0;
            while (Time.realtimeSinceStartup < end)
            {
                guard();
                if (ControlsReady(ship, wheel, player) && ship.m_nview.IsOwner() && !(bool)Handoff.GetValue(ship))
                { result.Success = true; yield break; }
                if (!player.IsAttached() && attempts < 2 && Time.realtimeSinceStartup >= retryAt)
                {
                    // A tiny fixture can drop the player off its deck on release.
                    // Reposition only the test player; acquire controls through Interact.
                    player.transform.position = wheel.transform.position + Vector3.up;
                    player.GetComponent<Rigidbody>().position = player.transform.position;
                    yield return new WaitForSeconds(.5f);
                    guard();
                    wheel.Interact(player, false, false);
                    attempts++;
                    // Repeated requests cancel upstream's pending takeover timer.
                    // Allow its configured maximum five-second delay to finish.
                    retryAt = Time.realtimeSinceStartup + 6f;
                }
                yield return new WaitForSeconds(.05f);
            }
        }
        private static bool BeforeSend(VehicleMovementController __instance, out int __state)
        {
            __state = __instance == countedShip && __instance.m_nview && __instance.m_nview.IsValid() ? __instance.m_nview.GetZDO().GetInt("VehicleAnchorState") : -1;
            if (observeRemoteDispatch && __instance == countedShip) { remoteDispatches++; return false; }
            return true;
        }
        private static void AfterSend(VehicleMovementController __instance, AnchorState state, int __state)
        {
            if (__instance == countedShip && state == countedState && __state >= 0 && __state != (int)state &&
                __instance.m_nview && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner() &&
                __instance.m_nview.GetZDO().GetInt("VehicleAnchorState") == (int)state) effectiveRequests++;
        }
    }
}
