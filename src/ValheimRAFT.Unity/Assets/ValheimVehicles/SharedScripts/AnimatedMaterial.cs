// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

#region

using UnityEngine;

#endregion

namespace ValheimVehicles.SharedScripts
{
  [RequireComponent(typeof(Renderer))]
  public class AnimatedMaterialUpdate : MonoBehaviour
  {
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    [Header("Material Setup")]
    [SerializeField] private bool useInstanceMaterial = true;

    [Header("Global Time Control")]
    [SerializeField] private float timeMultiplier = 0.25f;

    [Header("Random Seed Drift Control")]
    [SerializeField] private bool enableRandomSeedDrift = true;
    [SerializeField] private float seedDriftInterval = 5f;
    [Range(0f, 1f)] [SerializeField] private float seedDriftStrength = 0.1f;

    [Header("Main Texture Offset Animation")]
    [SerializeField] private bool animateMainTex = true;
    [SerializeField] private Vector2 mainTexSpeedMin = Vector2.zero;
    [SerializeField] private Vector2 mainTexSpeedMax = Vector2.right;

    [Header("Main Texture Tiling Animation")]
    [SerializeField] private bool animateMainTexTiling;
    [SerializeField] private Vector2 mainTexTilingBase = Vector2.one;
    [SerializeField] private Vector2 mainTexTilingAmplitude = Vector2.zero;
    [SerializeField] private float mainTexTilingFrequency = 1f;

    [Header("Emission Texture Offset Animation")]
    [SerializeField] private bool animateEmissionTex = true;
    [SerializeField] private Vector2 emissionSpeedMin = Vector2.zero;
    [SerializeField] private Vector2 emissionSpeedMax = Vector2.right;

    [Header("Emission Texture Tiling Animation")]
    [SerializeField] private bool animateEmissionTiling;
    [SerializeField] private Vector2 emissionTilingBase = Vector2.one;
    [SerializeField] private Vector2 emissionTilingAmplitude = Vector2.zero;
    [SerializeField] private float emissionTilingFrequency = 1f;

    [Header("Emission Color Animation")]
    [SerializeField] private bool animateEmissionColor = true;
    [SerializeField] private Color baseEmissionColor = Color.white;
    [ColorUsage(false, true)] [SerializeField] private float emissionIntensityMin = 0.1f;
    [ColorUsage(false, true)] [SerializeField] private float emissionIntensityMax = 1.5f;
    [SerializeField] private float emissionColorFrequency = 1f;
    [SerializeField] private bool usePingPong;

    private Vector2 _emissionOffsetSeed;

    private float _emissionPhase;
    private Vector2 _emissionScrollSpeed;
    private Vector2 _emissionTargetSeed;
    private Vector2 _mainTexOffsetSeed;
    private Vector2 _mainTexScrollSpeed;
    private Vector2 _mainTexTargetSeed;

    private Material _material;
    private float _seedDriftTimer;

    private void Awake()
    {
      var renderer = GetComponent<Renderer>();
      _material = useInstanceMaterial ? renderer.material : renderer.sharedMaterial;

      InitMainTex();
      InitEmissionTex();
      InitEmissionColor();
    }

    private void Update()
    {
      float t = Time.time;
      UpdateSeedDrift(Time.deltaTime);
      UpdateMainTex(t);
      UpdateEmissionTex(t);
      UpdateEmissionColor(t);
    }

    private void InitMainTex()
    {
      if (animateMainTex)
      {
        _mainTexScrollSpeed = new Vector2(
          Random.Range(mainTexSpeedMin.x, mainTexSpeedMax.x),
          Random.Range(mainTexSpeedMin.y, mainTexSpeedMax.y)
        );
        _mainTexOffsetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
        _mainTexTargetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
      }

      if (!animateMainTexTiling)
        _material.SetTextureScale(MainTex, mainTexTilingBase);
    }

    private void InitEmissionTex()
    {
      if (animateEmissionTex)
      {
        _emissionScrollSpeed = new Vector2(
          Random.Range(emissionSpeedMin.x, emissionSpeedMax.x),
          Random.Range(emissionSpeedMin.y, emissionSpeedMax.y)
        );
        _emissionOffsetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
        _emissionTargetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
      }

      if (!animateEmissionTiling)
        _material.SetTextureScale(EmissionMap, emissionTilingBase);
    }

    private void InitEmissionColor()
    {
      if (!animateEmissionColor) return;

      _material.EnableKeyword("_EMISSION");
      _emissionPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void UpdateMainTex(float t)
    {
      if (animateMainTex)
      {
        Vector2 offset = _mainTexOffsetSeed + _mainTexScrollSpeed * (t * timeMultiplier);
        _material.SetTextureOffset(MainTex, offset);
      }

      if (animateMainTexTiling)
      {
        float wave = Oscillate(mainTexTilingFrequency);
        Vector2 dynamicTiling = mainTexTilingBase + mainTexTilingAmplitude * wave;
        _material.SetTextureScale(MainTex, dynamicTiling);
      }
    }

    private void UpdateEmissionTex(float t)
    {
      if (animateEmissionTex)
      {
        Vector2 offset = _emissionOffsetSeed + _emissionScrollSpeed * (t * timeMultiplier);
        _material.SetTextureOffset(EmissionMap, offset);
      }

      if (animateEmissionTiling)
      {
        float wave = Oscillate(emissionTilingFrequency);
        Vector2 dynamicTiling = emissionTilingBase + emissionTilingAmplitude * wave;
        _material.SetTextureScale(EmissionMap, dynamicTiling);
      }
    }

    private void UpdateEmissionColor(float t)
    {
      if (!animateEmissionColor) return;

      float cycle = usePingPong
        ? Mathf.PingPong((t + _emissionPhase) * emissionColorFrequency * timeMultiplier, 1f)
        : 0.5f * (1f + Mathf.Sin((t + _emissionPhase) * emissionColorFrequency * timeMultiplier * Mathf.PI * 2f));

      float intensity = Mathf.Lerp(emissionIntensityMin, emissionIntensityMax, cycle);
      _material.SetColor(EmissionColor, baseEmissionColor.linear * intensity);
    }

    private float Oscillate(float frequency, float phase = 0f)
    {
      return Mathf.Sin((Time.time + phase) * frequency * timeMultiplier * Mathf.PI * 2f);
    }

    private void UpdateSeedDrift(float deltaTime)
    {
      if (!enableRandomSeedDrift) return;

      _seedDriftTimer += deltaTime;

      float progress = Mathf.Clamp01(_seedDriftTimer / seedDriftInterval);

      _mainTexOffsetSeed = Vector2.Lerp(_mainTexOffsetSeed, _mainTexTargetSeed, seedDriftStrength * progress);
      _emissionOffsetSeed = Vector2.Lerp(_emissionOffsetSeed, _emissionTargetSeed, seedDriftStrength * progress);

      if (_seedDriftTimer >= seedDriftInterval)
      {
        _seedDriftTimer = 0f;
        _mainTexTargetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
        _emissionTargetSeed = new Vector2(Random.value * 10f, Random.value * 10f);
      }
    }
  }
}
