// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;
using Random = System.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{
    public static class ZoneTerrainGenerator
    {
        private static readonly float _baseHeight = 0f; // Keep terrain centered at world origin
        private static float _hillFrequency = 0.02f;
        private static float _hillAmplitude = 6f;
        private static float _mountainFrequency = 0.005f;
        private static float _mountainAmplitude = 45f;

        private static int _seed = 1337;
        private static Vector2 _hillOffset = Vector2.zero;
        private static Vector2 _mountainOffset = Vector2.zero;

        public static void SetSeed(int seed)
        {
            _seed = seed;
            var rand = new Random(seed);

            // Random offsets to decorrelate noise
            _hillOffset = new Vector2(rand.Next(0, 10000), rand.Next(0, 10000));
            _mountainOffset = new Vector2(rand.Next(0, 10000), rand.Next(0, 10000));

            // Randomize terrain shape characteristics within usable bounds
            _hillFrequency = Mathf.Lerp(0.01f, 0.05f, (float)rand.NextDouble());
            _hillAmplitude = Mathf.Lerp(2f, 10f, (float)rand.NextDouble());

            _mountainFrequency = Mathf.Lerp(0.002f, 0.01f, (float)rand.NextDouble());
            _mountainAmplitude = Mathf.Lerp(20f, 70f, (float)rand.NextDouble());
        }

        public static float SampleHeight(Vector3 worldPos)
        {
            float x = worldPos.x;
            float z = worldPos.z;

            float hill = Mathf.PerlinNoise(
                x * _hillFrequency + _hillOffset.x,
                z * _hillFrequency + _hillOffset.y) * _hillAmplitude;

            float mountain = Mathf.PerlinNoise(
                x * _mountainFrequency + _mountainOffset.x,
                z * _mountainFrequency + _mountainOffset.y) * _mountainAmplitude;

            float valley = Mathf.PerlinNoise(
                x * _mountainFrequency + _mountainOffset.x + 1000f,
                z * _mountainFrequency + _mountainOffset.y + 1000f) * (_mountainAmplitude * 0.5f);

            float height = _baseHeight + hill + mountain - valley;
            return height;
        }

        // Optional manual overrides
        public static void SetHillFrequency(float value) => _hillFrequency = value;
        public static void SetHillAmplitude(float value) => _hillAmplitude = value;
        public static void SetMountainFrequency(float value) => _mountainFrequency = value;
        public static void SetMountainAmplitude(float value) => _mountainAmplitude = value;
    }
}
