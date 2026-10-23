using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ValheimVehicles.Controllers;

public static partial class AnchorIntegrationChecks
{
    public static IEnumerator RunStorm(Action<string, bool, string> record, string run, Action<bool> setWeather)
    {
        var saved = JsonConvert.DeserializeObject<SavedCase>(File.ReadAllText(CasePath(run)));
        VehicleMovementController ship = null;
        float deadline = Time.realtimeSinceStartup + 60;
        while (!ship && Time.realtimeSinceStartup < deadline)
        {
            ship = VehicleMovementController.Instances.FirstOrDefault(s => s && s.m_nview && s.m_nview.IsValid() &&
                s.m_nview.GetZDO().GetString("AnchorIntegrationCase") == saved.marker);
            yield return new WaitForSeconds(1);
        }
        if (!ship) throw new InvalidOperationException("Retained offshore fixture not loaded.");
        bool vertical = Vertical.Value, horizontal = Horizontal.Value, sway = ship.HasOceanSwayDisabled;
        bool saveConfig = Horizontal.ConfigFile.SaveOnConfigSet;
        var player = Player.m_localPlayer;
        bool god = player.InGodMode();
        WaterVolume water = null;
        try
        {
            player.SetGodMode(true);
            Horizontal.ConfigFile.SaveOnConfigSet = false;
            Horizontal.Value = true;
            Vertical.Value = false;
            ship.m_nview.ClaimOwnership();
            setWeather(true);
            deadline = Time.realtimeSinceStartup + 60;
            while ((EnvMan.instance.GetCurrentEnvironment()?.m_name != "ThunderStorm" || EnvMan.instance.GetWindIntensity() < .99f) &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForSeconds(1);
            record("storm.environment-ready", EnvMan.instance.GetCurrentEnvironment()?.m_name == "ThunderStorm" &&
                EnvMan.instance.GetWindIntensity() >= .99f,
                "Real ThunderStorm environment; normalized wind=" + EnvMan.instance.GetWindIntensity() + "; no artificial body impulses.");
            foreach (int phase in new[] {0, 1, 2})
            {
                ship.RPC_SetOceanSway(0, phase == 1);
                Vertical.Value = phase == 2;
                Hold(ship);
                var zdo = ship.m_nview.GetZDO();
                Vector3 reference = zdo.GetVec3(Position, Vector3.zero);
                Quaternion rotation = zdo.GetQuaternion(Rotation, Quaternion.identity);
                float start = Time.realtimeSinceStartup, duration = phase == 2 ? 30 : 150;
                float minY = ship.m_body.position.y, maxY = minY, minWater = float.MaxValue, maxWater = float.MinValue;
                float drift = 0, angle = 0, heightError = 0, minWind = 1;
                int samples = 0, dynamic = 0, beached = 0, stormSamples = 0, ownerSamples = 0;
                while (Time.realtimeSinceStartup - start < duration)
                {
                    yield return new WaitForFixedUpdate();
                    samples++;
                    if (!ship.m_body.isKinematic) dynamic++;
                    if (ship.isBeached) beached++;
                    if (ship.m_nview.IsOwner()) ownerSamples++;
                    if (EnvMan.instance.GetCurrentEnvironment()?.m_name == "ThunderStorm") stormSamples++;
                    minWind = Mathf.Min(minWind, EnvMan.instance.GetWindIntensity());
                    float level = Floating.GetWaterLevel(reference, ref water);
                    minWater = Mathf.Min(minWater, level); maxWater = Mathf.Max(maxWater, level);
                    Vector3 actual = ship.m_body.position;
                    minY = Mathf.Min(minY, actual.y); maxY = Mathf.Max(maxY, actual.y);
                    drift = Mathf.Max(drift, HorizontalDistance(actual, reference));
                    heightError = Mathf.Max(heightError, Mathf.Abs(actual.y - reference.y));
                    angle = Mathf.Max(angle, RotationError(ship.m_body.rotation, rotation));
                }
                string name = phase == 2 ? "storm.vertical-lock" : phase == 1 ? "storm.sway-disabled" : "storm.sway-enabled";
                string detail = "seconds=" + (Time.realtimeSinceStartup-start) + "; samples=" + samples +
                    "; stormSamples=" + stormSamples + "; minWind=" + minWind + "; ownerSamples=" + ownerSamples +
                    "; dynamic=" + dynamic + "; beached=" + beached + "; minY=" + minY + "; maxY=" + maxY +
                    "; minWater=" + minWater + "; maxWater=" + maxWater + "; maxDrift=" + drift +
                    "; maxAngle=" + angle + "; maxHeightError=" + heightError + "; savedPosition=" + reference.ToString("F8");
                record(name + ".weather-and-waves", stormSamples == samples && minWind >= .99f && maxWater-minWater > 1,
                    detail);
                record(name + ".hold", drift < .002f && angle < .02f && ownerSamples == samples && dynamic > samples*.9f && beached == 0,
                    detail);
                record(name + ".height", phase == 2 ? heightError < .002f && ship.m_body.constraints == RigidbodyConstraints.FreezeAll :
                    maxY-minY > .5f && ship.m_body.constraints == HorizontalConstraints, detail);
                record(name + ".saved-reference", zdo.GetVec3(Position, Vector3.zero).Equals(reference) &&
                    zdo.GetQuaternion(Rotation, Quaternion.identity).Equals(rotation), "Reference keys stayed unchanged during this phase. " + detail);
            }
        }
        finally
        {
            Horizontal.Value = horizontal; Vertical.Value = vertical;
            Horizontal.ConfigFile.SaveOnConfigSet = saveConfig;
            ship.RPC_SetOceanSway(0, sway);
            Hold(ship);
            setWeather(false);
            player.SetGodMode(god);
        }
    }
}
