#region
// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
using UnityEngine;
using Random = System.Random;

#endregion

namespace ValheimVehicles.SharedScripts
{



  namespace ValheimVehicles.SharedScripts
  {
    public static class TerrainHeightSampler
    {
      private const float Scale = 0.005f;
      private const float RidgeMultiplier = 0.5f;
      private static float _hillFrequency;
      private static float _hillAmplitude;
      private static float _mountainFrequency;
      private static float _mountainAmplitude;
      private static Vector2 _hillOffset;
      private static Vector2 _mountainOffset;

      public static void ConfigureFromSeed(int seed)
      {
        var rand = new Random(seed);
        _hillOffset = new Vector2(rand.Next(0, 10000), rand.Next(0, 10000));
        _mountainOffset = new Vector2(rand.Next(0, 10000), rand.Next(0, 10000));

        _hillFrequency = Mathf.Lerp(0.01f, 0.05f, (float)rand.NextDouble());
        _hillAmplitude = Mathf.Lerp(2f, 10f, (float)rand.NextDouble());

        _mountainFrequency = Mathf.Lerp(0.002f, 0.01f, (float)rand.NextDouble());
        _mountainAmplitude = Mathf.Lerp(20f, 70f, (float)rand.NextDouble());
      }

      // Primary terrain sampling entry
      public static float SampleHeight(Vector3 worldPos, int worldSeed)
      {
        ConfigureFromSeed(worldSeed);
        
        float baseNoise = GenerateFractalNoise(worldPos, worldSeed, 4, 0.5f, 2f);
        float ridgeNoise = Mathf.Abs(GenerateFractalNoise(worldPos + Vector3.one * 999f, worldSeed + 12345, 3, 0.6f, 2.1f));

        float height = Mathf.Clamp01(baseNoise + RidgeMultiplier * ridgeNoise);
        return height;
      }

      private static float GenerateFractalNoise(Vector3 pos, int seed, int octaves, float persistence, float lacunarity)
      {
        float amplitude = 1f;
        float frequency = Scale;
        float total = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
          float nx = (pos.x + seed * 1000) * frequency;
          float nz = (pos.z + seed * 1000) * frequency;

          float noise = Mathf.PerlinNoise(nx, nz);
          total += noise * amplitude;

          maxValue += amplitude;
          amplitude *= persistence;
          frequency *= lacunarity;
        }

        return total / maxValue;
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

        return hill + mountain - valley;
      }
    }
  }

}