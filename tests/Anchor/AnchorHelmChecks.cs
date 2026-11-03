using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;

namespace AnchorIntegrationTests
{
    // Test host only. Uses a marked disposable fixture and real wheel interaction.
    // Input is injected into the real key handler; ownership timing tests are explicit.
    public static class AnchorHelmChecks
    {
        private static bool injecting, down, up;
        private static bool KeyDown(ref bool __result) { if (!injecting) return true; __result = down; return false; }
        private static bool KeyUp(ref bool __result) { if (!injecting) return true; __result = up; return false; }

        public static IEnumerator Run(Action<string, bool, string> record, string run, string character, int serverPid, Action guard)
        {
            guard();
            var player = Player.m_localPlayer;
            if (!player || player.GetPlayerName() != character) throw new InvalidOperationException("Disposable player required.");
            var saved = JObject.Parse(File.ReadAllText(Path.Combine(Directory.GetParent(run).FullName, "anchor-restart.json")));
            if ((long)saved["world"] != ZNet.instance.GetWorldUID()) throw new InvalidOperationException("Fixture world mismatch.");
            string marker = (string)saved["marker"];
            var ship = VehicleMovementController.Instances.FirstOrDefault(s => s && s.m_nview && s.m_nview.IsValid() &&
                s.m_nview.GetZDO().GetString("AnchorIntegrationCase") == marker);
            if (!ship || ship.Manager.IsLandVehicle || !ship.isAnchored) throw new InvalidOperationException("Saved anchored fixture required.");
            var initialPosition = player.transform.position;
            var initialRotation = player.transform.rotation;
            bool wasGod = player.InGodMode();
            bool autoRelease = PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value;
            float takeoverTime = VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.Value;
            bool saveConfig = VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.ConfigFile.SaveOnConfigSet;
            var harmony = new Harmony("local.anchor.integration.helm");
            GameObject wheelObject = null;
            GameObject floorObject = null;
            try
            {
                player.SetGodMode(true);
                VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.ConfigFile.SaveOnConfigSet = false;
                VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.Value = 5f;
                PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value = false;
                var prefab = ZNetScene.instance.GetPrefab("ValheimVehicles_ShipSteeringWheel");
                if (!prefab) throw new InvalidOperationException("Wheel prefab missing.");
                floorObject = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("wood_floor"), ship.transform.TransformPoint(new Vector3(0, 1.5f, -2)), ship.transform.rotation);
                ship.PiecesController.AddNewPiece(floorObject.GetComponent<ZNetView>());
                wheelObject = UnityEngine.Object.Instantiate(prefab, ship.transform.TransformPoint(new Vector3(0, 2, -2)), ship.transform.rotation);
                ship.PiecesController.AddNewPiece(wheelObject.GetComponent<ZNetView>());
                yield return new WaitForSeconds(8);
                var wheel = wheelObject.GetComponent<SteeringWheelComponent>();
                if (!wheel) throw new InvalidOperationException("Wheel component missing.");
                player.transform.position = wheel.transform.position + Vector3.up;
                player.GetComponent<Rigidbody>().position = player.transform.position;
                ZNet.instance.SetReferencePosition(player.transform.position);
                yield return new WaitForSeconds(.5f);
                long ownerBefore = ship.m_nview.GetZDO().GetOwner();
                wheel.Interact(player, false, false);
                float deadline = Time.realtimeSinceStartup + 12;
                var handoff = AccessTools.Field(typeof(VehicleMovementController), "_isControlHandoffInProgress");
                while (Time.realtimeSinceStartup < deadline && (!player.IsAttached() || !ship.HaveControllingPlayer() || !ship.m_nview.IsOwner() || (bool)handoff.GetValue(ship)))
                    yield return new WaitForSeconds(.05f);
                bool controls = player.IsAttached() && ReferenceEquals(AccessTools.Field(typeof(Player), "m_doodadController").GetValue(player), wheel) &&
                    ship.HaveControllingPlayer() && ship.m_nview.IsOwner() && ship.m_nview.GetZDO().GetLong(ZDOVars.s_user) == player.GetPlayerID() && !(bool)handoff.GetValue(ship);
                record("helm.normal-interaction", controls, "Wheel interaction with takeover setting at five seconds. Attached=" + player.IsAttached() + "; controlling=" + ship.HaveControllingPlayer() + "; onboard count=" + ship.OnboardController.m_localPlayers.Count + "; initial owner was local=" + (ownerBefore == ZDOMan.GetSessionID()) + ". Does not force a missing-RPC fallback.");
                if (!controls) yield break;
                harmony.Patch(AccessTools.Method(typeof(VehicleMovementController), "GetAnchorKeyDown"), prefix: new HarmonyMethod(typeof(AnchorHelmChecks), nameof(KeyDown)));
                harmony.Patch(AccessTools.Method(typeof(VehicleMovementController), "GetAnchorKeyUp"), prefix: new HarmonyMethod(typeof(AnchorHelmChecks), nameof(KeyUp)));
                Action<bool, bool> inject = (pressed, released) => {
                    guard();
                    try { injecting = true; down = pressed; up = released; AccessTools.Method(typeof(VehicleMovementController), "OnAnchorKeyPress").Invoke(ship, null); }
                    finally { injecting = down = up = false; }
                };
                for (int i = 0; i < 4; i++)
                {
                    AnchorState expected = ship.isAnchored ? AnchorState.Recovered : AnchorState.Anchored;
                    inject(true, false); inject(false, true);
                    deadline = Time.realtimeSinceStartup + 30;
                    while (Time.realtimeSinceStartup < deadline && !Settled(ship, expected)) yield return new WaitForSeconds(.05f);
                    record("helm.transition-" + (i + 1), Settled(ship, expected), "Expected=" + expected + "; ship=" + ship.vehicleAnchorState + "; ZDO=" + ship.m_nview.GetZDO().GetInt("VehicleAnchorState") + "; physical anchors=" + string.Join(",", AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>().Where(a => a && a.MovementController == ship).Select(a => a.currentState.ToString())));
                    if (!Settled(ship, expected)) yield break;
                    yield return new WaitForSeconds(.3f);
                }
                yield return HelmQueueChecks.Run(ship, wheel, player, record, inject, guard);
                bool parked = ship.isAnchored && ship.m_nview.GetZDO().GetBool("L4zerShark.RaftAnchorFix.v1.valid");
                record("helm.restart-reference-ready", parked, "The final actual anchor drop supplies the expected pose for the subsequent disk-restart check.");
                if (parked)
                {
                    var zdo = ship.m_nview.GetZDO();
                    var p = zdo.GetVec3("L4zerShark.RaftAnchorFix.v1.position", ship.m_body.position);
                    var r = zdo.GetQuaternion("L4zerShark.RaftAnchorFix.v1.rotation", ship.m_body.rotation);
                    saved["position"] = new JArray(p.x, p.y, p.z);
                    saved["rotation"] = new JArray(r.x, r.y, r.z, r.w);
                    saved["clientPid"] = System.Diagnostics.Process.GetCurrentProcess().Id;
                    saved["serverPid"] = serverPid;
                    File.WriteAllText(Path.Combine(Directory.GetParent(run).FullName, "anchor-restart.json"), saved.ToString());
                }
            }
            finally
            {
                harmony.UnpatchSelf();
                injecting = down = up = false;
                if (player)
                {
                    AccessTools.Method(typeof(Player), "StopDoodadControl", Type.EmptyTypes).Invoke(player, null);
                    AnchorIntegrationChecks.LeaveFixture(ship);
                    player.SetGodMode(wasGod);
                    player.transform.SetPositionAndRotation(initialPosition, initialRotation);
                    player.GetComponent<Rigidbody>().position = initialPosition;
                    ZNet.instance.SetReferencePosition(initialPosition);
                }
                VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.Value = takeoverTime;
                PropulsionConfig.ShouldLiftAnchorOnSpeedChange.Value = autoRelease;
                VehicleGlobalConfig.ForceVehicleOwnerShipTakeoverTime.ConfigFile.SaveOnConfigSet = saveConfig;
                if (wheelObject) { var view = wheelObject.GetComponent<ZNetView>(); view.ClaimOwnership(); ZNetScene.instance.Destroy(wheelObject); }
                if (floorObject) { var view = floorObject.GetComponent<ZNetView>(); view.ClaimOwnership(); ZNetScene.instance.Destroy(floorObject); }
            }
        }

        private static bool Settled(VehicleMovementController ship, AnchorState expected)
        {
            if (!ship || ship.vehicleAnchorState != expected || ship.m_nview.GetZDO().GetInt("VehicleAnchorState") != (int)expected) return false;
            var anchors = AnchorMechanismController.Instances.OfType<VehicleAnchorMechanismController>().Where(a => a && a.MovementController == ship).ToArray();
            return anchors.Length > 0 && anchors.All(a => a.currentState == expected || expected == AnchorState.Recovered && a.currentState == AnchorState.Idle);
        }
    }
}
