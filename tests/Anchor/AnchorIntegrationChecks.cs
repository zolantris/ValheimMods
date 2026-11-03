using System;
using System.Collections;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.BepInExConfig;

// Disposable-world integration checks. Compile only into an isolated test host, never the mod.
// Portions adapted from ValheimRAFTAnchorFix. See licenses/ValheimRAFTAnchorFix-MIT.txt.
public static partial class AnchorIntegrationChecks
{
    private const string Valid = "L4zerShark.RaftAnchorFix.v1.valid";
    private const string Position = "L4zerShark.RaftAnchorFix.v1.position";
    private const string Rotation = "L4zerShark.RaftAnchorFix.v1.rotation";
    private const string Axes = "L4zerShark.RaftAnchorFix.v2.lockedAxes";
    private const RigidbodyConstraints HorizontalConstraints = RigidbodyConstraints.FreezePositionX |
        RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    private sealed class SavedCase
    {
        public string marker;
        public float[] position, rotation;
        public string anchorMarker;
        public float[] anchorStart;
        public long world;
        public int clientPid, serverPid;
    }
    private static string CasePath(string run) => Path.Combine(Directory.GetParent(run).FullName, "anchor-restart.json");
    private static Type Plugin => typeof(AnchorConfig);
    private static Type Lock => AccessTools.TypeByName("ValheimVehicles.Controllers.VehicleAnchorPose");
    private static ConfigEntry<bool> Horizontal => (ConfigEntry<bool>)AccessTools.Field(Plugin, "HorizontalLock").GetValue(null);
    private static ConfigEntry<bool> Vertical => (ConfigEntry<bool>)AccessTools.Field(Plugin, "VerticalLock").GetValue(null);
    private static bool Hold(VehicleMovementController ship) { return (bool)AccessTools.Method(Lock, "Hold").Invoke(null, new object[] { ship }); }
    private static void SyncAnchor(VehicleMovementController ship) { AccessTools.Method(ship.GetType(), "SyncAnchor").Invoke(ship, null); }
    private static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
    private static bool Finite(Vector3 value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
        !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    private static bool Released(VehicleMovementController ship) =>
        (ship.m_body.constraints & (RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ)) == 0;

    public static void LeaveFixture(VehicleMovementController ship)
    {
        var player = Player.m_localPlayer;
        if (!ship || !player) return;
        if (ship.OnboardController != null && ship.OnboardController.m_localPlayers.Contains(player))
        {
            AccessTools.Method(typeof(Player), "StopDoodadControl", Type.EmptyTypes).Invoke(player, null);
            var collider = player.GetComponent<Collider>();
            if (collider)
            {
                ship.OnboardController.HandlePlayerExitVehicleBounds(collider);
                ship.OnboardController.DebounceExitVehicleBounds();
            }
        }
        // Destroying a marked boat must never destroy its parented test player.
        if (player.transform.IsChildOf(ship.transform))
        {
            player.AttachStop();
            player.transform.SetParent(null, true);
        }
    }
    private static void CheckPose(Action<string, bool, string> record, string name, VehicleMovementController ship, Vector3 p, Quaternion r)
    {
        float distance = HorizontalDistance(ship.m_body.position, p), angle = RotationError(ship.m_body.rotation, r);
        record(name, distance < 0.002f && angle < 0.02f,
            "horizontalErrorMeters=" + distance + "; freeHeight=" + ship.m_body.position.y + "; angleErrorDegrees=" + angle);
    }

    private static void MoveBody(VehicleMovementController ship, Vector3 position)
    {
        ship.m_body.position = position;
        ship.transform.position = position;
        Physics.SyncTransforms();
    }

    private static void AxisChecks(Action<string, bool, string> record, VehicleMovementController ship)
    {
        var zdo = ship.m_nview.GetZDO();
        Vector3 parked = zdo.GetVec3(Position, Vector3.zero);
        Vector3 live = ship.m_body.position;
        zdo.RemoveInt(Axes);
        zdo.Set(Position, new Vector3(parked.x, live.y - 20f, parked.z));
        Hold(ship);
        record("migration.v1-height-ignored", Math.Abs(ship.m_body.position.y - live.y) < 0.002f &&
            HorizontalDistance(ship.m_body.position, parked) < 0.002f && zdo.GetInt(Axes, -1) == 1,
            "Actual v1 snapshot with no axis mask and obsolete height preserves X/Z while adopting horizontal-only mode.");

        // Direct config assignments are scoped test inputs, not proof of network
        // configuration synchronization. Defaults are restored before yielding.
        Vertical.Value = true;
        Hold(ship);
        Vector3 bothReference = zdo.GetVec3(Position, Vector3.zero);
        record("config.vertical-on-captures-current", Math.Abs(bothReference.y - live.y) < 0.002f &&
            HorizontalDistance(bothReference, parked) < 0.002f && ship.m_body.constraints == RigidbodyConstraints.FreezeAll,
            "Enabling Y captures its current height and retains already-locked X/Z.");
        MoveBody(ship, bothReference + new Vector3(2, 3, 4));
        Hold(ship);
        record("config.both-axes-held", Vector3.Distance(ship.m_body.position, bothReference) < 0.002f,
            "All enabled position axes return to the captured reference.");

        Horizontal.Value = false;
        Hold(ship);
        Vector3 horizontallyMoved = bothReference + new Vector3(3, 0, 4);
        MoveBody(ship, horizontallyMoved);
        Hold(ship);
        record("config.horizontal-off-preserves-valid", Vector3.Distance(ship.m_body.position, horizontallyMoved) < 0.002f &&
            zdo.GetBool(Valid) && zdo.GetInt(Axes, -1) == 2 &&
            ship.m_body.constraints == (RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation),
            "Unlocking X/Z allows movement without invalidating the Y/rotation snapshot.");
        Horizontal.Value = true;
        Hold(ship);
        Vector3 horizontalRecapture = zdo.GetVec3(Position, Vector3.zero);
        record("config.horizontal-on-captures-current", HorizontalDistance(horizontalRecapture, horizontallyMoved) < 0.002f &&
            Math.Abs(horizontalRecapture.y - bothReference.y) < 0.002f && Vector3.Distance(ship.m_body.position, horizontallyMoved) < 0.002f,
            "Re-enabling X/Z captures the new horizontal location and preserves the still-locked Y reference.");

        Horizontal.Value = false; Vertical.Value = false;
        Hold(ship);
        Vector3 freelyMoved = horizontallyMoved + new Vector3(1, 2, 1);
        MoveBody(ship, freelyMoved);
        ship.m_body.linearVelocity = new Vector3(1, 2, 3);
        Hold(ship);
        record("config.both-off-keeps-rotation-only", ship.m_body.constraints == RigidbodyConstraints.FreezeRotation &&
            Vector3.Distance(ship.m_body.position, freelyMoved) < 0.002f &&
            Vector3.Distance(ship.m_body.linearVelocity, new Vector3(1, 2, 3)) < 0.002f && zdo.GetBool(Valid),
            "Both position locks disabled: position and velocity remain free; rotation stays locked.");

        Horizontal.Value = true;
        Hold(ship);
        Vertical.Value = true;
        Hold(ship);
        record("config.vertical-reenabled-after-motion", Vector3.Distance(zdo.GetVec3(Position, Vector3.zero), freelyMoved) < 0.002f &&
            Vector3.Distance(ship.m_body.position, freelyMoved) < 0.002f,
            "Each re-enabled axis captures the moved position instead of the historical snapshot.");
        Vertical.Value = false;
        Hold(ship);
        record("config.defaults-restored", Horizontal.Value && !Vertical.Value && ship.m_body.constraints == HorizontalConstraints &&
            zdo.GetBool(Valid) && zdo.GetInt(Axes, -1) == 1, "Default lock mode restored before continued physics/network checks.");
    }

    private static VehicleAnchorMechanismController FindAnchor(VehicleMovementController ship, string marker)
    {
        return AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>()
            .FirstOrDefault(a => a && a.MovementController == ship && a.GetComponent<ZNetView>() &&
                a.GetComponent<ZNetView>().IsValid() &&
                a.GetComponent<ZNetView>().GetZDO().GetString("AnchorIntegrationAnchor") == marker);
    }

    private static void CheckAnchorGeometry(Action<string, bool, string> record, string name,
        VehicleMovementController ship, string marker, Vector3 canonicalRest)
    {
        var anchor = FindAnchor(ship, marker);
        if (!anchor || !anchor.anchorTransform) throw new InvalidOperationException("Exact saved physical anchor required for geometry check: " + name);
        Vector3 rest = anchor.GetAnchorStartLocalPosition();
        record(name + ".rest-preserved", Vector3.Distance(rest, canonicalRest) < 0.002f,
            "Actual rest=" + rest.ToString("F5") + "; independently captured fresh rest=" + canonicalRest.ToString("F5"));
        float groundDistance = anchor.GetDistanceToGround();
        float localDrop = rest.y - anchor.anchorTransform.localPosition.y;
        var bodyField = AccessTools.Field(typeof(AnchorMechanismController), "anchorRb");
        var body = bodyField.GetValue(anchor) as Rigidbody;
        Vector3 restWorld = anchor.anchorTransform.parent.TransformPoint(rest);
        float worldDrop = restWorld.y - anchor.anchorTransform.position.y;
        bool usableClearance = !float.IsNaN(groundDistance) && !float.IsInfinity(groundDistance);
        record(name + ".depth-clearance", usableClearance,
            "Fixture ground distance=" + groundDistance +
            "m; deployment is clamped between zero and 40m below the raised rest.");
        if (!usableClearance) throw new InvalidOperationException("Fixture clearance cannot establish the deployed-anchor assertion: " + groundDistance + "m.");
        string geometryState = JsonConvert.SerializeObject(new {
            local = anchor.anchorTransform.localPosition.ToString("F5"),
            restWorld = restWorld.ToString("F5"), localDrop,
            world = anchor.anchorTransform.position.ToString("F5"),
            bodyPosition = body ? body.position.ToString("F5") : "missing",
            bodyKinematic = body && body.isKinematic,
            state = anchor.currentState.ToString(), enabled = anchor.enabled,
            active = anchor.gameObject.activeInHierarchy, Physics.autoSyncTransforms,
            components = anchor.GetComponents<AnchorMechanismController>().Select(a => new {
                type = a.GetType().FullName, state = a.currentState.ToString(), a.enabled,
                rest = a.GetAnchorStartLocalPosition().ToString("F5"),
                body = bodyField.GetValue(a) is Rigidbody rb ? rb.position.ToString("F5") : "missing"
            }).ToArray()
        });
        // Native lowering/reeling move the child body in world-space down/up.
        // A tilted or moving parent therefore need not preserve the endpoint's local X/Z.
        // Canonical rest and the ship's horizontal position have separate strict checks.
        record(name + ".anchored-visual", ship.isAnchored && anchor.currentState == AnchorState.Anchored &&
            body && Finite(body.position) && Finite(restWorld) && Finite(anchor.anchorTransform.position) &&
            !float.IsNaN(groundDistance) && !float.IsInfinity(groundDistance) &&
            body.isKinematic && Vector3.Distance(anchor.anchorTransform.position,
                restWorld + Vector3.down * Mathf.Clamp(groundDistance, 0f, 40f)) < 0.05f &&
            Vector3.Distance(body.position, anchor.anchorTransform.position) < 0.05f,
            "Actual settled physical anchor: ground distance=" + groundDistance + "; world vertical drop=" + worldDrop +
            "; expected world-down deployment is ground distance clamped to 0..40m. " + geometryState);
    }

    private static void CompletionChecks(Action<string, bool, string> record, VehicleMovementController ship,
        string anchorMarker, Vector3 canonicalRest)
    {
        var mechanisms = AccessTools.Field(typeof(VehiclePiecesController), "m_anchorMechanismComponents").GetValue(ship.PiecesController) as IList;
        var anchor = AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>()
            .FirstOrDefault(a => a && a.MovementController == ship && mechanisms != null && mechanisms.Contains(a));
        if (!anchor || !ship.m_nview.IsOwner()) throw new InvalidOperationException("An owner and real registered physical anchor are required for completion checks.");
        var holding = AccessTools.Field(typeof(VehicleMovementController), "_isHoldingAnchor");
        bool previousHolding = (bool)holding.GetValue(ship);
        AnchorState previousState = ship.vehicleAnchorState;
        try
        {
            holding.SetValue(ship, false);
            ship.SetAnchor(AnchorState.Anchored);
            ship.PiecesController.UpdateAnchorState(AnchorState.Anchored);
            // Re-run the actual piece startup path on this disposable fixture.
            // Visual initialization must not release a parked ship.
            AccessTools.Method(typeof(VehicleAnchorMechanismController), "Start").Invoke(anchor, null);
            record("anchor.startup-recovered-suppressed", ship.isAnchored &&
                ship.m_nview.GetZDO().GetInt("VehicleAnchorState") == (int)AnchorState.Anchored &&
                anchor.currentState == AnchorState.Anchored && ship.m_nview.GetZDO().GetBool(Valid),
                "Physical Start preserved the saved anchor without publishing a release.");
            CheckAnchorGeometry(record, "anchor.startup-geometry", ship, anchorMarker, canonicalRest);
            Vector3 lowered = anchor.anchorTransform.localPosition;
            AccessTools.Method(typeof(AnchorMechanismController), "GuardAgainstAnchorSyncIssue").Invoke(anchor, null);
            record("anchor.geometry-delayed-guard", Vector3.Distance(anchor.anchorTransform.localPosition, lowered) < 0.002f,
                "Actual delayed parent-change guard preserves the active anchored visual; before=" + lowered.ToString("F5") +
                "; after=" + anchor.anchorTransform.localPosition.ToString("F5"));

            foreach (var terminal in new[] { AnchorState.Recovered, AnchorState.Anchored })
            {
                AnchorState transition = terminal == AnchorState.Recovered ? AnchorState.Reeling : AnchorState.Lowering;
                holding.SetValue(ship, false);
                ship.SetAnchor(transition);
                ship.PiecesController.UpdateAnchorState(transition);
                if (ship.vehicleAnchorState != transition || ship.m_nview.GetZDO().GetInt("VehicleAnchorState") != (int)transition ||
                    anchor.currentState != transition) throw new InvalidOperationException("Physical completion fixture did not enter " + transition);
                holding.SetValue(ship, true);
                // Call the real physical state transition, which invokes OnAnchorStateChange,
                // sends the owner RPC and updates other registered anchors normally.
                anchor.UpdateAnchorState(terminal, VehicleAnchorMechanismController.GetCurrentStateTextStatic(terminal, false));
                bool allPhysical = AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>()
                    .Where(a => a && a.MovementController == ship && mechanisms.Contains(a)).All(a => a.currentState == terminal);
                record("anchor.held-completion-" + terminal.ToString().ToLowerInvariant(),
                    ship.vehicleAnchorState == terminal && ship.m_nview.GetZDO().GetInt("VehicleAnchorState") == (int)terminal &&
                    allPhysical && (bool)holding.GetValue(ship) &&
                    ship.m_nview.GetZDO().GetBool(Valid) == (terminal == AnchorState.Anchored),
                    "Actual physical " + transition + " -> " + terminal + " completion while held: local=" + ship.vehicleAnchorState +
                    "; saved=" + ship.m_nview.GetZDO().GetInt("VehicleAnchorState") + "; allPhysical=" + allPhysical +
                    "; heldRestored=" + holding.GetValue(ship));
            }
        }
        finally
        {
            holding.SetValue(ship, false);
            try
            {
                ship.SetAnchor(previousState);
                ship.PiecesController.UpdateAnchorState(previousState);
            }
            finally { holding.SetValue(ship, previousHolding); }
        }
    }

    public static IEnumerator Run(Action<string, bool, string> record, string run, string expectedCharacter, int serverProcessId)
    {
        var player = Player.m_localPlayer;
        if (!player || player.IsDead() || player.GetPlayerName() != expectedCharacter) throw new InvalidOperationException("Living disposable test player required.");
        Vector3 origin = player.transform.position;
        Quaternion originRotation = player.transform.rotation;
        bool autoDelay = VehicleGuiMenuConfig.HasAutoAnchorDelay.Value;
        bool autoRelease = PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value;
        bool wasGod = player.InGodMode();
        bool horizontal = Horizontal.Value, vertical = Vertical.Value;
        bool configSave = Horizontal.ConfigFile.SaveOnConfigSet;
        GameObject boat = null;
        VehicleMovementController ship = null;
        ZDOID boatId = ZDOID.None;
        bool keepFixture = false;
        string marker = Guid.NewGuid().ToString("N");
        try
        {
            record("config.defaults", (bool)Horizontal.DefaultValue && !(bool)Vertical.DefaultValue,
                "Declared defaults: horizontal locked, vertical unlocked.");
            Horizontal.ConfigFile.SaveOnConfigSet = false;
            Horizontal.Value = true; Vertical.Value = false;
            VehicleGuiMenuConfig.HasAutoAnchorDelay.Value = true;
            var prefab = ZNetScene.instance.GetPrefab("ValheimVehicles_WaterVehicleShip");
            if (!prefab) throw new InvalidOperationException("Water vehicle prefab missing.");
            // Keep the test boat outside the onboard triggers of earlier lab boats.
            // Remove only explicitly marked anchor fixtures in this disposable world.
            foreach (var previous in VehicleMovementController.Instances.Where(s => s && s.m_nview && s.m_nview.IsValid() && s.m_nview.GetZDO().GetBool("AnchorIntegrationFixture")).ToArray())
            {
                LeaveFixture(previous);
                if (previous.PiecesController != null)
                    foreach (var piece in previous.PiecesController.m_pieces.ToArray())
                        if (piece && piece.IsValid()) { piece.ClaimOwnership(); ZNetScene.instance.Destroy(piece.gameObject); }
                previous.m_nview.ClaimOwnership();
                ZNetScene.instance.Destroy(previous.gameObject);
            }
            Vector3 p = FindWater(new Vector3(5000, 0, 0));
            player.SetGodMode(true);
            player.transform.position = p + new Vector3(8, 4, 0);
            player.GetComponent<Rigidbody>().position = player.transform.position;
            ZNet.instance.SetReferencePosition(player.transform.position);
            yield return new WaitForSeconds(12);
            origin = player.transform.position;
            boat = UnityEngine.Object.Instantiate(prefab, p, Quaternion.Euler(0, 27, 0));
            var view = boat.GetComponent<ZNetView>();
            boatId = view.GetZDO().m_uid;
            view.GetZDO().Set("AnchorIntegrationFixture", true);
            view.GetZDO().Set("AnchorIntegrationCase", marker);
            var manager = boat.GetComponent<VehicleManager>();
            float deadline = Time.realtimeSinceStartup + 50;
            while (Time.realtimeSinceStartup < deadline && (!manager.IsInitialized || manager.PiecesController == null || !manager.PiecesController.IsInitialPieceActivationComplete))
                yield return new WaitForSeconds(1);
            record("raft.initialized", manager.IsInitialized && manager.PiecesController.IsInitialPieceActivationComplete, "Real water-vehicle prefab and starter hull loaded.");
            ship = manager.MovementController;
            if (!ship) throw new InvalidOperationException("No movement controller.");
            var anchorPrefab = ZNetScene.instance.GetPrefab("ValheimVehicles_ShipAnchor_wood");
            if (!anchorPrefab) throw new InvalidOperationException("Anchor prefab missing.");
            // Capture the untouched prefab child before instantiation or any anchor-state callback.
            // Later checks compare this saved value, never a potentially corrupted reloaded instance.
            Vector3 canonicalRest = anchorPrefab.transform.Find("anchor").localPosition;
            var anchor = UnityEngine.Object.Instantiate(anchorPrefab, boat.transform.TransformPoint(new Vector3(0, 2, 0)), boat.transform.rotation);
            anchor.GetComponent<ZNetView>().GetZDO().Set("AnchorIntegrationAnchor", marker);
            manager.PiecesController.AddNewPiece(anchor.GetComponent<ZNetView>());
            yield return new WaitForSeconds(8);
            view.ClaimOwnership();
            ship.UpdateAnchorCapabilities();
            record("anchor.saved-anchor-releasable", ship.CanAnchor, "A saved anchor retains the release capability during piece loading.");
            record("raft.anchor-capability", ship.CanAnchor, "Actual wooden anchor attached to vehicle.");
            var physicalAnchor = FindAnchor(ship, marker);
            record("geometry.fresh-rest", physicalAnchor && Vector3.Distance(physicalAnchor.GetAnchorStartLocalPosition(), canonicalRest) < 0.002f,
                "Fresh physical rest must match the untouched pre-instantiation prefab child: " + canonicalRest.ToString("F5"));

            ship.SetAnchor(AnchorState.Recovered);
            ship.SetAnchor(AnchorState.Anchored);
            p = ship.m_body.position;
            Quaternion r = ship.m_body.rotation;
            Hold(ship);
            record("anchor.snapshot-created", view.GetZDO().GetBool(Valid), "Owner stored pose on existing anchored ship.");
            ship.CanAnchor = false;
            SyncAnchor(ship);
            record("anchor.loading-preserves-state", ship.isAnchored, "Loading gap no longer resets the saved anchor.");
            ship.UpdateAnchorCapabilities();
            ship.m_body.isKinematic = false;
            ship.m_body.linearVelocity = new Vector3(3, 2.5f, 4);
            view.GetZDO().Set(ZDOVars.s_velHash, new Vector3(3, 1.25f, 4));
            view.GetZDO().Set(ZDOVars.s_bodyVelHash, new Vector3(3, 2.5f, 4));
            Hold(ship);
            record("anchor.free-vertical-velocity", ship.m_body.constraints == HorizontalConstraints &&
                Vector3.Distance(ship.m_body.linearVelocity, new Vector3(0, 2.5f, 0)) < 0.002f &&
                Vector3.Distance(view.GetZDO().GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero), new Vector3(0, 2.5f, 0)) < 0.002f &&
                Vector3.Distance(view.GetZDO().GetVec3(ZDOVars.s_velHash, Vector3.zero), new Vector3(0, 1.25f, 0)) < 0.002f,
                "Holding removes X/Z velocity while preserving body Y velocity and each network Y velocity.");
            ship.m_body.AddForce(new Vector3(30, 15, 30), ForceMode.VelocityChange);
            ship.m_body.AddTorque(new Vector3(8, 8, 8), ForceMode.VelocityChange);
            float minY = ship.m_body.position.y, maxY = minY;
            int dynamicSamples = 0;
            deadline = Time.realtimeSinceStartup + 3f;
            bool collisionsEnabled = ship.m_body.detectCollisions;
            try
            {
                // Nearby terrain/trees must not turn this axis-constraint check into
                // a collision test. Native movement and synchronization still run.
                ship.m_body.detectCollisions = false;
                while (Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForFixedUpdate();
                    if (!ship.m_body.isKinematic)
                    {
                        dynamicSamples++;
                        ship.m_body.AddForce(Vector3.up * 2f, ForceMode.VelocityChange);
                    }
                    minY = Math.Min(minY, ship.m_body.position.y); maxY = Math.Max(maxY, ship.m_body.position.y);
                }
            }
            finally
            {
                if (ship && ship.m_body) ship.m_body.detectCollisions = collisionsEnabled;
            }
            CheckPose(record, "anchor.horizontal-impulse-held", ship, p, r);
            record("anchor.vertical-physics-moves", maxY - minY > 0.02f,
                "Controlled upward impulses with boat collision detection temporarily disabled; measured height range=" + (maxY - minY) + "m; dynamic samples=" + dynamicSamples + ". Native physics/sync remain active; this does not establish ocean wave amplitude.");
            record("anchor.zero-horizontal-network-velocity", HorizontalDistance(view.GetZDO().GetVec3(ZDOVars.s_bodyVelHash, Vector3.one), Vector3.zero) < 0.002f &&
                HorizontalDistance(view.GetZDO().GetVec3(ZDOVars.s_velHash, Vector3.one), Vector3.zero) < 0.002f, "No stale X/Z velocity for extrapolation or ownership handoff.");
            AxisChecks(record, ship);
            CompletionChecks(record, ship, marker, canonicalRest);
            RegressionChecks(record, ship, physicalAnchor);

            ship.SetAnchor(AnchorState.Reeling);
            record("anchor.release-clears-snapshot", !view.GetZDO().GetBool(Valid) && Released(ship),
                "Raising anchor releases constraints and invalidates old parking pose.");
            Vector3 releaseStart = ship.m_body.position;
            ship.m_body.isKinematic = false;
            ship.m_body.AddForce(new Vector3(3, 0, 0), ForceMode.VelocityChange);
            yield return new WaitForSeconds(0.4f);
            record("anchor.release-allows-motion", HorizontalDistance(releaseStart, ship.m_body.position) > 0.01f, "Horizontal physics resumes after direct state release; helm animation is tested separately.");
            ship.SetAnchor(AnchorState.Anchored);
            p = ship.m_body.position; r = ship.m_body.rotation;
            record("anchor.new-position", Vector3.Distance(view.GetZDO().GetVec3(Position, Vector3.zero), p) < 0.002f, "Dropping anchor again records its new position.");

            PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value = true;
            AccessTools.Method(ship.GetType(), "RPC_SpeedChange").Invoke(ship, new object[] { ZNet.GetUID(), (int)Ship.Speed.Slow });
            record("anchor.speed-option-releases", !view.GetZDO().GetBool(Valid) && view.GetZDO().GetInt("VehicleAnchorState") != 2,
                "The optional speed-key release is persisted and remains usable.");
            PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value = autoRelease;
            ship.SetAnchor(AnchorState.Anchored);

            // Exercise the same-frame path where teleport first takes ownership.
            view.GetZDO().SetOwner(ZNet.instance.GetServerPeer().m_uid);
            ship.SetIsTeleporting(true);
            record("anchor.teleport-suspends", !Hold(ship) && !view.GetZDO().GetBool(Valid), "Teleport bypass invalidates old snapshot.");
            var relocated = ship.m_body.position + new Vector3(4, 0, 0);
            ship.transform.position = relocated; ship.m_body.position = relocated;
            view.GetZDO().SetPosition(relocated);
            ship.SetIsTeleporting(false);
            Hold(ship);
            CheckPose(record, "anchor.teleport-new-location", ship, relocated, ship.m_body.rotation);
            ship.isPlayerHaulingVehicle = true;
            record("anchor.hauling-suspends", !Hold(ship), "Hauling bypasses parking constraints.");
            ship.isPlayerHaulingVehicle = false;
            Hold(ship);
            p = ship.m_body.position; r = ship.m_body.rotation;

            var zdo = view.GetZDO();
            zdo.SetOwner(ZNet.instance.GetServerPeer().m_uid);
            uint revision = zdo.DataRevision;
            ship.m_body.position += new Vector3(1, 0, 0);
            Hold(ship);
            CheckPose(record, "network.nonowner-pose", ship, p, r);
            record("network.nonowner-no-writes", !view.IsOwner() && revision == zdo.DataRevision, "Remote instance restores locally without changing owner ZDO data.");
            var sync = ship.GetComponent<VehicleZSyncTransform>();
            if (!sync || !(bool)AccessTools.Field(sync.GetType(), "_isReady").GetValue(sync) || !sync.m_syncPosition || !sync.m_syncBodyVelocity)
                throw new InvalidOperationException("Actual initialized position/velocity synchronization required for remote Y check.");
            // Controlled replication input, followed by the real ClientSync method.
            // All writes here belong to fixture setup; the method itself must not write ZDO data.
            ship.m_body.isKinematic = false;
            MoveBody(ship, p);
            zdo.SetPosition(p + new Vector3(1, 2, 1));
            zdo.Set(ZDOVars.s_velHash, Vector3.zero);
            zdo.Set(ZDOVars.s_bodyVelHash, new Vector3(1, 1.75f, 1));
            AccessTools.Field(sync.GetType(), "m_lastUpdateFrame").SetValue(sync, -1);
            revision = zdo.DataRevision;
            sync.ClientSync(0.1f);
            record("network.client-sync-interpolates-y", ship.transform.position.y > p.y + 0.01f && ship.transform.position.y < p.y + 1.99f &&
                HorizontalDistance(ship.transform.position, p) < 0.002f && Math.Abs(ship.m_body.linearVelocity.y - 1.75f) < 0.002f &&
                HorizontalDistance(ship.m_body.linearVelocity, Vector3.zero) < 0.002f,
                "Actual non-owner ClientSync interpolated Y=" + ship.transform.position.y + "; originalY=" + p.y + "; retained Y velocity=" + ship.m_body.linearVelocity.y);
            record("network.client-sync-no-writes", !view.IsOwner() && revision == zdo.DataRevision,
                "Actual ClientSync plus native anchor hold preserved the owner's ZDO data revision.");
            // Helm ownership handoff is exercised by the copied-ship helm probe.
            // A bare SetAnchor call is not an authorized helm input to queue.
            ship.SetAnchor(AnchorState.Reeling);
            record("network.release-before-zdo", !Hold(ship) && Released(ship) && zdo.GetBool(Valid),
                "Controlled non-owner RPC timing: release is immediate while authoritative ZDO still says anchored.");
            // Simulate the delayed owner field arriving; this fixture deliberately
            // controls local replication state, not multiple human player clients.
            zdo.Set("VehicleAnchorState", (int)AnchorState.Reeling);
            Hold(ship);
            ship.SetAnchor(AnchorState.Anchored);
            record("network.drop-before-zdo", Hold(ship) && ship.m_body.constraints == HorizontalConstraints,
                "Controlled non-owner RPC timing: drop locks X/Z while preserving free Y before owner ZDO arrives.");
            zdo.Set("VehicleAnchorState", (int)AnchorState.Anchored);
            Hold(ship);
            yield return new WaitForSeconds(2);
            view.ClaimOwnership();
            Hold(ship);
            CheckPose(record, "network.ownership-return", ship, p, r);

            // A real zone unload destroys the instance but retains the ZDO. Returning
            // must create a fresh instance at the exact persisted parking position.
            yield return PendingRemovalChecks(record, ship, marker);
            yield return RemovalChecks(record, ship, marker, canonicalRest);
            GeometryBoundaryChecks(record, ship, FindAnchor(ship, marker));
            yield return WaterChecks(record, ship);
            p = ship.m_body.position; r = ship.m_body.rotation;
            string beforeInstance = boat.GetInstanceID().ToString();
            File.WriteAllText(Path.Combine(run, "raft-fixture.json"), JsonConvert.SerializeObject(new {
                boatId = boatId.ToString(), position = new[] { p.x, p.y, p.z }, rotation = new[] { r.x, r.y, r.z, r.w }, beforeInstance }, Formatting.Indented));
            Vector3 away = origin + new Vector3(1200, 80, 0);
            player.SetGodMode(true);
            player.transform.position = away;
            player.GetComponent<Rigidbody>().position = away;
            ZNet.instance.SetReferencePosition(away);
            deadline = Time.realtimeSinceStartup + 55;
            while (boat && Time.realtimeSinceStartup < deadline) yield return new WaitForSeconds(1);
            record("zone.instance-unloaded", !boat, "Original ship instance was destroyed by zone streaming.");
            if (boat) throw new InvalidOperationException("Zone did not unload; cannot claim reload tested.");
            player.transform.position = origin;
            player.GetComponent<Rigidbody>().position = origin;
            ZNet.instance.SetReferencePosition(origin);
            deadline = Time.realtimeSinceStartup + 60;
            while (!boat && Time.realtimeSinceStartup < deadline)
            {
                var loaded = ZNetScene.instance.FindInstance(boatId);
                if (loaded) boat = loaded.gameObject;
                yield return new WaitForSeconds(1);
            }
            record("zone.instance-reloaded", boat, "Network object recreated from retained ZDO.");
            if (!boat) throw new InvalidOperationException("Ship did not reload.");
            manager = boat.GetComponent<VehicleManager>();
            deadline = Time.realtimeSinceStartup + 40;
            while ((!manager.IsInitialized || manager.MovementController == null) && Time.realtimeSinceStartup < deadline) yield return new WaitForSeconds(1);
            ship = manager.MovementController;
            yield return new WaitForSeconds(5);
            CheckPose(record, "zone.pose-preserved", ship, p, r);
            record("zone.anchor-preserved", ship.isAnchored && ship.m_nview.GetZDO().GetInt("VehicleAnchorState") == 2, "Anchor remains down after real unload/reload.");
            record("zone.snapshot-preserved", ship.m_nview.GetZDO().GetBool(Valid), "Parking snapshot survived streaming.");
            CheckAnchorGeometry(record, "zone.anchor-geometry", ship, marker, canonicalRest);
            if (File.Exists(Path.Combine(run, "keep-raft-fixture")))
            {
                File.WriteAllText(CasePath(run), JsonConvert.SerializeObject(new SavedCase {
                    marker = marker, position = new[] {p.x,p.y,p.z}, rotation = new[] {r.x,r.y,r.z,r.w},
                    anchorMarker = marker, anchorStart = new[] {canonicalRest.x, canonicalRest.y, canonicalRest.z},
                    world = ZNet.instance.GetWorldUID(), clientPid = System.Diagnostics.Process.GetCurrentProcess().Id, serverPid = serverProcessId
                }, Formatting.Indented));
                keepFixture = true;
                record("restart.fixture-retained", true, "Tracked fixture retained only in the disposable world for a process restart test.");
            }
        }
        finally
        {
            Horizontal.Value = horizontal; Vertical.Value = vertical;
            Horizontal.ConfigFile.SaveOnConfigSet = configSave;
            VehicleGuiMenuConfig.HasAutoAnchorDelay.Value = autoDelay;
            PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value = autoRelease;
            if (player)
            {
                player.SetGodMode(wasGod);
                player.transform.SetPositionAndRotation(origin, originRotation);
                player.GetComponent<Rigidbody>().position = origin;
                ZNet.instance.SetReferencePosition(origin);
            }
            if (boat && !keepFixture)
            {
                LeaveFixture(ship);
                var manager = boat.GetComponent<VehicleManager>();
                if (manager && manager.PiecesController)
                    foreach (var piece in manager.PiecesController.m_pieces.ToArray())
                        if (piece && piece.IsValid()) { piece.ClaimOwnership(); ZNetScene.instance.Destroy(piece.gameObject); }
                var view = boat.GetComponent<ZNetView>();
                if (view && view.IsValid()) { view.ClaimOwnership(); ZNetScene.instance.Destroy(boat); }
            }
        }
    }

    public static IEnumerator RunRestart(Action<string, bool, string> record, string run, int serverProcessId)
    {
        var saved = JsonConvert.DeserializeObject<SavedCase>(File.ReadAllText(CasePath(run)));
        record("restart.new-processes", saved.clientPid != System.Diagnostics.Process.GetCurrentProcess().Id && saved.serverPid != serverProcessId, "Both client and dedicated server have restarted.");
        record("restart.same-world", saved.world == ZNet.instance.GetWorldUID(), "Reloaded the same disposable saved world.");
        VehicleMovementController ship = null;
        float deadline = Time.realtimeSinceStartup + 60;
        while (!ship && Time.realtimeSinceStartup < deadline)
        {
            ship = VehicleMovementController.Instances.FirstOrDefault(x => x && x.m_nview && x.m_nview.IsValid() && x.m_nview.GetZDO().GetString("AnchorIntegrationCase") == saved.marker);
            yield return new WaitForSeconds(1);
        }
        record("restart.fixture-loaded", ship, "Resolved the exact saved fixture by its unique marker, independent of remapped ZDO IDs.");
        if (!ship) yield break;
        try
        {
            bool ownerBeforeClaim = ship.m_nview.IsOwner();
            ship.m_nview.ClaimOwnership();
            int ownerSamples = 0, samples = 0;
            float windowStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - windowStart < 30)
            {
                if (ship.m_nview.IsOwner()) ownerSamples++;
                samples++;
                Hold(ship);
                yield return new WaitForFixedUpdate();
            }
            float observedSeconds = Time.realtimeSinceStartup - windowStart;
            record("restart.owner-path-observed", ship.m_nview.IsOwner() && ownerSamples == samples && samples > 0 && observedSeconds >= 30,
                "Owner before explicit claim=" + ownerBeforeClaim + "; owner at end=" + ship.m_nview.IsOwner() +
                "; owner samples=" + ownerSamples + "/" + samples + "; seconds=" + observedSeconds + "; Hold called each observed physics tick.");
            record("restart.no-anchor-data-writes", AnchorWriteObserver.Count(saved.marker) == 0,
                "State and parking-snapshot setter calls since process startup=" + AnchorWriteObserver.Count(saved.marker));
            var p = new Vector3(saved.position[0], saved.position[1], saved.position[2]);
            var r = new Quaternion(saved.rotation[0], saved.rotation[1], saved.rotation[2], saved.rotation[3]);
            CheckPose(record, "restart.pose-preserved", ship, p, r);
            record("restart.anchor-and-snapshot", ship.isAnchored && ship.m_nview.GetZDO().GetBool(Valid), "Anchor state and custom snapshot survived disk save/reload.");
            if (saved.anchorStart == null || saved.anchorStart.Length != 3 || string.IsNullOrEmpty(saved.anchorMarker))
                throw new InvalidOperationException("Restart geometry requires a newly recorded canonical anchor reference from the main suite.");
            CheckAnchorGeometry(record, "restart.anchor-geometry", ship, saved.anchorMarker,
                new Vector3(saved.anchorStart[0], saved.anchorStart[1], saved.anchorStart[2]));
            ship.m_nview.ClaimOwnership();
            ship.SetAnchor(AnchorState.Reeling);
            record("restart.release-works", !ship.m_nview.GetZDO().GetBool(Valid) && Released(ship), "Direct state release still works after restart; helm interaction is tested separately.");
        }
        finally
        {
            LeaveFixture(ship);
            var manager = ship.Manager;
            if (manager && manager.PiecesController)
                foreach (var piece in manager.PiecesController.m_pieces.ToArray())
                    if (piece && piece.IsValid()) { piece.ClaimOwnership(); ZNetScene.instance.Destroy(piece.gameObject); }
            if (ship && ship.m_nview && ship.m_nview.IsValid()) { ship.m_nview.ClaimOwnership(); ZNetScene.instance.Destroy(ship.gameObject); }
        }
    }
}
