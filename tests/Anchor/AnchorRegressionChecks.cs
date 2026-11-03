using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;

public static partial class AnchorIntegrationChecks
{
    private static float RotationError(Quaternion a, Quaternion b)
    {
        double dot = (double)a.x*b.x + (double)a.y*b.y + (double)a.z*b.z + (double)a.w*b.w;
        double aa = (double)a.x*a.x + (double)a.y*a.y + (double)a.z*a.z + (double)a.w*a.w;
        double bb = (double)b.x*b.x + (double)b.y*b.y + (double)b.z*b.z + (double)b.w*b.w;
        return (float)(2 * Math.Acos(Math.Min(1, Math.Abs(dot) / Math.Sqrt(aa * bb))) * 180 / Math.PI);
    }

    private static Vector3 FindWater(Vector3 origin)
    {
        float level = ZoneSystem.instance.m_waterLevel;
        for (int radius = 128; radius <= 2048; radius += 128)
        for (int angle = 0; angle < 360; angle += 30)
        {
            var p = origin + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            float ground = WorldGenerator.instance.GetHeight(p.x, p.z);
            if (Mathf.Abs(p.x) > 4000 && p.magnitude < 8500 && ground < level - 10 && ground > level - 30 &&
                new[] {Vector3.left, Vector3.right, Vector3.forward, Vector3.back}.All(d =>
                    WorldGenerator.instance.GetHeight(p.x + d.x * 20, p.z + d.z * 20) < level - 8))
                return new Vector3(p.x, level + 2, p.z);
        }
        throw new InvalidOperationException("No open-water fixture beyond 4000 m found.");
    }

    private static void RegressionChecks(Action<string, bool, string> record,
        VehicleMovementController ship, VehicleAnchorMechanismController anchor)
    {
        var zdo = ship.m_nview.GetZDO();
        string marker = zdo.GetString("AnchorIntegrationCase");
        var saved = zdo.GetVec3(Position, Vector3.zero);
        // Deliberately displace the body so an invalid recapture cannot pass a pose-only test.
        MoveBody(ship, ship.m_body.position + Vector3.right * 3);
        int writes = AnchorWriteObserver.Count(marker);
        anchor.currentState = AnchorState.Idle;
        anchor.UpdateAnchorState(AnchorState.Anchored, "");
        record("regression.mirrored-idle-does-not-write", AnchorWriteObserver.Count(marker) == writes &&
            zdo.GetBool(Valid) && zdo.GetInt("VehicleAnchorState") == (int)AnchorState.Anchored &&
            zdo.GetVec3(Position, Vector3.zero).Equals(saved), "Nested Idle/Lowering/Anchored callbacks must not publish or recapture.");
        ship.vehicleAnchorState = AnchorState.Idle;
        anchor.UpdateAnchorState(AnchorState.Idle, "");
        record("regression.stale-local-does-not-write", AnchorWriteObserver.Count(marker) == writes &&
            zdo.GetBool(Valid) && zdo.GetInt("VehicleAnchorState") == (int)AnchorState.Anchored,
            "A stale local Idle callback cannot overwrite the saved Anchored state.");
        Hold(ship);
        AccessTools.Method(typeof(VehicleAnchorMechanismController), "RestoreSavedAnchorVisual").Invoke(anchor, null);

        ship.SetAnchor(AnchorState.Recovered);
        var prior = RigidbodyConstraints.FreezeRotationX;
        ship.m_body.constraints = prior;
        ship.SetAnchor(AnchorState.Anchored);
        ship.SetAnchor(AnchorState.Recovered);
        record("regression.exact-constraints-restored", ship.m_body.constraints == prior,
            "Release restored exactly the prior FreezeRotationX constraint.");
        ship.m_body.constraints = RigidbodyConstraints.None;
        ship.SetAnchor(AnchorState.Anchored);
        Vertical.Value = true;
        Hold(ship);
        uint revision = zdo.DataRevision;
        for (int i = 0; i < 300; i++) Hold(ship);
        record("regression.idempotent-hold", revision == zdo.DataRevision,
            "300 same-frame settled Hold calls leave the ZDO revision unchanged; not a 300-tick physics benchmark.");
        zdo.SetOwner(ZNet.instance.GetServerPeer().m_uid);
        ship.m_body.isKinematic = false;
        ship.m_body.linearVelocity = Vector3.zero;
        ship.m_body.angularVelocity = Vector3.zero;
        ship.m_body.Sleep();
        string sleepBefore = "beforeSleeping=" + ship.m_body.IsSleeping() + "; constraints=" + ship.m_body.constraints +
            "; positionError=" + Vector3.Distance(ship.m_body.position, zdo.GetVec3(Position, Vector3.zero)) +
            "; bodyRotation=" + ship.m_body.rotation.ToString("F8") + "; savedRotation=" + zdo.GetQuaternion(Rotation, Quaternion.identity).ToString("F8") +
            "; bodyPosition=" + ship.m_body.position.ToString("F8") + "; transform=" + ship.transform.position.ToString("F8");
        Hold(ship);
        record("regression.remote-stays-asleep", ship.m_body.IsSleeping(), "Unchanged hold must not wake a sleeping remote replica. " + sleepBefore);
        ship.m_nview.ClaimOwnership();
        Vertical.Value = false;
        Hold(ship);
        PrecisionBoundaryChecks(record, ship);
    }

    private static IEnumerator RemovalChecks(Action<string, bool, string> record,
        VehicleMovementController ship, string marker, Vector3 canonicalRest)
    {
        var anchor = FindAnchor(ship, marker);
        var prefab = ZNetScene.instance.GetPrefab("ValheimVehicles_ShipAnchor_wood");
        var extra = UnityEngine.Object.Instantiate(prefab, ship.transform.TransformPoint(new Vector3(2, 2, 0)), ship.transform.rotation);
        ship.PiecesController.AddNewPiece(extra.GetComponent<ZNetView>());
        yield return new WaitForSeconds(2);
        ship.m_nview.ClaimOwnership();
        ship.SetAnchor(AnchorState.Anchored);
        extra.GetComponent<ZNetView>().ClaimOwnership();
        ZNetScene.instance.Destroy(extra);
        yield return new WaitForSeconds(1);
        record("removal.other-anchor-retains-lock", Hold(ship) && ship.isAnchored,
            "Destroying one of two physical anchors retains the saved lock.");
        anchor.GetComponent<ZNetView>().ClaimOwnership();
        ZNetScene.instance.Destroy(anchor.gameObject);
        yield return new WaitForSeconds(1);
        record("removal.last-anchor-releases", !Hold(ship) && Released(ship) &&
            ship.m_nview.GetZDO().GetInt("VehicleAnchorState") == (int)AnchorState.Idle &&
            !ship.m_nview.GetZDO().GetBool(Valid), "Actual owner ZNetScene.Destroy of the last anchor releases a ship without a wheel; hammer input itself is not automated.");
        var replacement = UnityEngine.Object.Instantiate(prefab, ship.transform.TransformPoint(new Vector3(0, 2, 0)), ship.transform.rotation);
        replacement.GetComponent<ZNetView>().GetZDO().Set("AnchorIntegrationAnchor", marker);
        ship.PiecesController.AddNewPiece(replacement.GetComponent<ZNetView>());
        yield return new WaitForSeconds(2);
        ship.UpdateAnchorCapabilities();
        ship.SetAnchor(AnchorState.Anchored);
        record("removal.replacement-can-anchor", Hold(ship) && ship.CanAnchor, "New anchor restores normal anchoring.");
    }

    private static IEnumerator WaterChecks(Action<string, bool, string> record, VehicleMovementController ship)
    {
        // No impulses, collision overrides, or kinematic overrides in this observation.
        ship.SetAnchor(AnchorState.Recovered);
        var p = ship.m_body.position;
        p.y = ZoneSystem.instance.m_waterLevel + 1;
        MoveBody(ship, p);
        ship.m_body.rotation = Quaternion.Euler(0, 27, 0);
        ship.transform.rotation = ship.m_body.rotation;
        ship.m_body.linearVelocity = Vector3.zero;
        ship.m_body.angularVelocity = Vector3.zero;
        yield return new WaitForSeconds(15);
        ship.m_nview.ClaimOwnership();
        ship.SetAnchor(AnchorState.Anchored);
        bool previousSway = ship.HasOceanSwayDisabled;
        try
        {
            foreach (bool disabled in new[] {false, true})
            {
                ship.RPC_SetOceanSway(0, disabled);
                var zdo = ship.m_nview.GetZDO();
                p = zdo.GetVec3(Position, Vector3.zero);
                var r = zdo.GetQuaternion(Rotation, Quaternion.identity);
                float minY = ship.m_body.position.y, maxY = minY, drift = 0, angle = 0, unityAngle = 0;
                Quaternion largestRotation = r;
                int dynamic = 0, samples = 0, beached = 0;
                float start = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - start < 150)
                {
                    yield return new WaitForFixedUpdate();
                    samples++;
                    if (!ship.m_body.isKinematic) dynamic++;
                    if (ship.isBeached) beached++;
                    minY = Mathf.Min(minY, ship.m_body.position.y);
                    maxY = Mathf.Max(maxY, ship.m_body.position.y);
                    drift = Mathf.Max(drift, HorizontalDistance(ship.m_body.position, p));
                    float currentAngle = RotationError(ship.m_body.rotation, r);
                    if (currentAngle > angle) { angle = currentAngle; largestRotation = ship.m_body.rotation; }
                    unityAngle = Mathf.Max(unityAngle, Quaternion.Angle(ship.m_body.rotation, r));
                }
                float ground = ZoneSystem.instance.GetGroundHeight(ship.m_body.position);
                string scope = disabled ? "water.sway-disabled" : "water.sway-enabled";
                string detail = "seconds=" + (Time.realtimeSinceStartup-start) + "; samples=" + samples +
                    "; dynamic=" + dynamic + "; beached=" + beached + "; ground=" + ground +
                    "; seaLevel=" + ZoneSystem.instance.m_waterLevel + "; minY=" + minY + "; maxY=" + maxY +
                    "; maxDrift=" + drift + "; maxAngle=" + angle + "; maxUnnormalizedUnityAngle=" + unityAngle +
                    "; savedPosition=" + p.ToString("F8") + "; referenceRotation=" + r.ToString("F8") + "; largestRotation=" + largestRotation.ToString("F8");
                record(scope + ".floating", dynamic > samples * .9f && beached == 0 &&
                    ground < minY - 5 && minY > ZoneSystem.instance.m_waterLevel - 5 && maxY < ZoneSystem.instance.m_waterLevel + 8, detail);
                record(scope + ".holds-position", drift < .002f && angle < .02f, detail);
                record(scope + ".saved-reference-unchanged", zdo.GetVec3(Position, Vector3.zero).Equals(p) &&
                    zdo.GetQuaternion(Rotation, Quaternion.identity).Equals(r) && Mathf.Abs(p.x) > 4000,
                    "Every sample measured against the fixed saved keys, starting immediately after the sway toggle; " + detail);
                record(scope + ".free-y", ship.m_body.constraints == HorizontalConstraints && maxY-minY > .001f, detail);
            }
        }
        finally { ship.RPC_SetOceanSway(0, previousSway); }
    }
}
