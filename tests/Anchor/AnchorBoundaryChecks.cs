using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;

public static partial class AnchorIntegrationChecks
{
    private static void PrecisionBoundaryChecks(Action<string, bool, string> record, VehicleMovementController ship)
    {
        var zdo = ship.m_nview.GetZDO();
        Vector3 saved = zdo.GetVec3(Position, Vector3.zero), before = ship.m_body.position;
        int axes = zdo.GetInt(Axes);
        bool vertical = Vertical.Value;
        long owner = zdo.GetOwner();
        try
        {
            Vertical.Value = true;
            var reference = new Vector3(10500.375f, 38.125f, -9500.125f);
            zdo.Set(Position, reference);
            zdo.Set(Axes, 3);
            MoveBody(ship, reference);
            Hold(ship);
            // Controlled one-ULP replica roundoff at world-edge coordinates.
            var rounded = reference;
            rounded.x = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(reference.x) + 1);
            rounded.z = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(reference.z) + 1);
            MoveBody(ship, rounded);
            zdo.SetOwner(ZNet.instance.GetServerPeer().m_uid);
            ship.m_body.linearVelocity = Vector3.zero;
            ship.m_body.angularVelocity = Vector3.zero;
            ship.m_body.Sleep();
            bool asleepBefore = ship.m_body.IsSleeping();
            float error = Vector3.Distance(ship.m_body.position, reference);
            for (int i = 0; i < 300; i++) Hold(ship);
            record("precision.world-edge-sleep", asleepBefore && ship.m_body.IsSleeping() && error > .0001f &&
                Vector3.Distance(ship.m_body.position, reference) < .004f,
                "Controlled replica at " + reference.ToString("F5") + "; injected error=" + error +
                "m; 300 same-frame holds; sleeping before=" + asleepBefore + "; after=" + ship.m_body.IsSleeping());
            MoveBody(ship, reference + new Vector3(.02f, 0, .02f));
            Hold(ship);
            record("precision.world-edge-corrects-drift", Vector3.Distance(ship.m_body.position, reference) < .004f &&
                zdo.GetVec3(Position, Vector3.zero).Equals(reference),
                "A 2cm X/Z displacement is corrected against the unchanged reference, not incorporated into a new parking position.");
        }
        finally
        {
            zdo.SetOwner(owner);
            zdo.Set(Position, saved);
            zdo.Set(Axes, axes);
            Vertical.Value = vertical;
            MoveBody(ship, before);
            Hold(ship);
        }
    }

    private static IEnumerator PendingRemovalChecks(Action<string, bool, string> record,
        VehicleMovementController ship, string marker)
    {
        var controller = ship.PiecesController;
        var prefab = ZNetScene.instance.GetPrefab("ValheimVehicles_ShipAnchor_wood");
        var queued = (List<ZNetView>)AccessTools.Field(typeof(VehiclePiecesController), "_newPendingPiecesQueue").GetValue(controller);
        foreach (bool useQueue in new[] { false, true })
        {
            var active = FindAnchor(ship, marker);
            var pending = UnityEngine.Object.Instantiate(prefab, ship.transform.TransformPoint(new Vector3(0, 2, 0)), ship.transform.rotation);
            var view = pending.GetComponent<ZNetView>();
            view.GetZDO().Set("AnchorIntegrationAnchor", marker);
            int id = controller.GetPersistentId();
            if (!VehiclePiecesController.m_pendingPieces.TryGetValue(id, out var list))
                VehiclePiecesController.m_pendingPieces[id] = list = new List<ZNetView>();
            var target = useQueue ? queued : list;
            target.Add(view);
            try
            {
                ship.m_nview.ClaimOwnership();
                active.GetComponent<ZNetView>().ClaimOwnership();
                ship.SetAnchor(AnchorState.Anchored);
                Vector3 saved = ship.m_nview.GetZDO().GetVec3(Position, Vector3.zero);
                int writes = AnchorWriteObserver.Count(marker);
                // Keep the second piece in a controlled pending state until the
                // actual persistent deletion hook has run in this same frame.
                ZNetScene.instance.Destroy(active.gameObject);
                record("removal.pending-" + (useQueue ? "queue" : "list") + "-retains-lock",
                    Hold(ship) && ship.isAnchored && AnchorWriteObserver.Count(marker) == writes &&
                    ship.m_nview.GetZDO().GetVec3(Position, Vector3.zero).Equals(saved),
                    "Actual active-anchor deletion with the remaining real prefab held in the pending " +
                    (useQueue ? "queue" : "list") + "; no saved-state or parking-data writes.");
            }
            finally { target.Remove(view); }
            controller.AddNewPiece(view);
            yield return new WaitForSeconds(2);
        }
        var anchor = FindAnchor(ship, marker);
        bool complete = controller.IsInitialPieceActivationComplete;
        try
        {
            controller.IsInitialPieceActivationComplete = false;
            int writes = AnchorWriteObserver.Count(marker);
            AccessTools.Method(typeof(VehiclePiecesController), "OnAnchorPieceDestroyed").Invoke(controller,
                new object[] { anchor.GetComponent<ZNetView>() });
            record("removal.initial-loading-keeps-state", Hold(ship) && ship.isAnchored && AnchorWriteObserver.Count(marker) == writes,
                "Controlled incomplete-activation flag: the deletion hook conservatively retains state while other anchors may be unknown; no actual deletion in this assertion.");
        }
        finally { controller.IsInitialPieceActivationComplete = complete; }
    }

    private static void GeometryBoundaryChecks(Action<string, bool, string> record,
        VehicleMovementController ship, VehicleAnchorMechanismController anchor)
    {
        Vector3 originalPosition = anchor.transform.position;
        Quaternion originalRotation = anchor.transform.rotation;
        Vector3 rest = anchor.GetAnchorStartLocalPosition();
        var cache = AccessTools.Field(typeof(VehicleAnchorMechanismController), "_hasAnchorGround");
        try
        {
            // Controlled placements of the actual wooden anchor, not natural
            // sailing or animation tests. Restore the piece before yielding.
            foreach (float depth in new[] { .5f, 1f, 2f, 2.01f, 45f, -.5f })
            {
                anchor.transform.rotation = Quaternion.Euler(25, 40, 15);
                Vector3 raised = anchor.anchorTransform.parent.TransformPoint(rest);
                Vector3 offset = raised - anchor.transform.position;
                float ground = ZoneSystem.instance.GetGroundHeight(originalPosition);
                Vector3 target = new Vector3(originalPosition.x, ground + depth, originalPosition.z);
                anchor.transform.position = target - offset;
                cache.SetValue(anchor, false);
                anchor.UpdateAnchorPositionIfNotNearGround();
                var body = (Rigidbody)AccessTools.Field(typeof(AnchorMechanismController), "anchorRb").GetValue(anchor);
                Vector3 expected = target + Vector3.down * Mathf.Clamp(depth, 0, 40);
                record("geometry.tilted-depth-" + depth.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    Vector3.Distance(anchor.anchorTransform.position, expected) < .01f &&
                    Vector3.Distance(body.position, expected) < .01f &&
                    Vector3.Distance(anchor.GetAnchorStartLocalPosition(), rest) < .002f,
                    "Controlled 25-degree pitch/15-degree roll, ground clearance=" + depth +
                    "m; expected=" + expected.ToString("F5") + "; actual=" + anchor.anchorTransform.position.ToString("F5") +
                    "; body=" + body.position.ToString("F5") + "; placement method only, not drop-animation endpoint.");
            }
        }
        finally
        {
            anchor.transform.SetPositionAndRotation(originalPosition, originalRotation);
            cache.SetValue(anchor, false);
            anchor.UpdateAnchorPositionIfNotNearGround();
        }
    }
}
