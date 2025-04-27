using ValheimVehicles.Config;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.Components;

using System.Collections.Generic;
using UnityEngine;

public class VehicleShipEffects : MonoBehaviour, IMonoUpdater
{
  public Transform m_shadow;

  public float m_offset = 0.01f;

  public float m_minimumWakeVel = 5f;

  public GameObject m_speedWakeRoot;

  public GameObject m_wakeSoundRoot;

  public GameObject m_inWaterSoundRoot;

  public float m_audioFadeDuration = 2f;

  public AudioSource m_sailSound;

  public float m_sailFadeDuration = 1f;

  public GameObject m_splashEffects;

  private ParticleSystem[] m_wakeParticles;

  private float m_sailBaseVol = 1f;

  private readonly List<KeyValuePair<AudioSource, float>> m_wakeSounds = new();

  private readonly List<KeyValuePair<AudioSource, float>> m_inWaterSounds =
    new();

  private WaterVolume m_previousWaterVolume;

  private Rigidbody m_body;

  private VehicleShip m_ship;

  public static List<VehicleShipEffects> Instances { get; } = [];
  public static List<IMonoUpdater> MonoUpdaters = [];

  public static void CloneShipEffectsToInstance(VehicleShipEffects instance,
    ShipEffects shipEffects)
  {
    instance.m_splashEffects = shipEffects.m_splashEffects;
    instance.m_shadow = shipEffects.m_shadow;
    instance.m_minimumWakeVel = shipEffects.m_minimumWakeVel;
    instance.m_speedWakeRoot = shipEffects.m_speedWakeRoot;
    instance.m_inWaterSoundRoot = shipEffects.m_inWaterSoundRoot;
    instance.m_wakeSoundRoot = shipEffects.m_wakeSoundRoot;
    instance.m_audioFadeDuration = shipEffects.m_audioFadeDuration;
    instance.m_sailSound = shipEffects.m_sailSound;
    instance.m_sailFadeDuration = shipEffects.m_sailFadeDuration;
  }

  private void Awake()
  {
    var componentInParent = GetComponentInParent<ZNetView>();
    if ((bool)componentInParent && componentInParent.GetZDO() == null)
    {
      enabled = false;
      return;
    }

    m_body = GetComponentInParent<Rigidbody>();
    m_ship = GetComponentInParent<VehicleShip>();
    if ((bool)m_speedWakeRoot)
      m_wakeParticles =
        m_speedWakeRoot.GetComponentsInChildren<ParticleSystem>();

    if ((bool)m_wakeSoundRoot)
    {
      var componentsInChildren =
        m_wakeSoundRoot.GetComponentsInChildren<AudioSource>();
      foreach (var audioSource in componentsInChildren)
      {
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        m_wakeSounds.Add(
          new KeyValuePair<AudioSource, float>(audioSource,
            audioSource.volume));
      }
    }

    if ((bool)m_inWaterSoundRoot)
    {
      var componentsInChildren =
        m_inWaterSoundRoot.GetComponentsInChildren<AudioSource>();
      foreach (var audioSource2 in componentsInChildren)
      {
        audioSource2.pitch = Random.Range(0.9f, 1.1f);
        m_inWaterSounds.Add(
          new KeyValuePair<AudioSource, float>(audioSource2,
            audioSource2.volume));
      }
    }

    if ((bool)m_sailSound)
    {
      m_sailBaseVol = m_sailSound.volume;
      m_sailSound.pitch = Random.Range(0.9f, 1.1f);
    }
  }

  private void OnEnable()
  {
    MonoUpdaters.Add(this);
    Instances.Add(this);
  }

  private void OnDisable()
  {
    MonoUpdaters.Remove(this);
    Instances.Remove(this);
  }

  public void CustomFixedUpdate(float deltaTime)
  {
    // throw new System.NotImplementedException();
  }

  public void CustomUpdate(float deltaTime, float time)
  {
    // throw new System.NotImplementedException();
  }

  public void CustomLateUpdate(float deltaTime)
  {
    if (!Floating.IsUnderWater(transform.position, ref m_previousWaterVolume))
    {
      m_shadow.gameObject.SetActive(false);
      SetWake(false, deltaTime);
      FadeSounds(m_inWaterSounds, false, deltaTime);
      return;
    }

    m_shadow.gameObject.SetActive(true);
    var flag = m_body.velocity.magnitude > m_minimumWakeVel;
    FadeSounds(m_inWaterSounds, true, deltaTime);

    // flying and submerged states should never have a wake.
    if (m_ship.MovementController != null &&
        (m_ship.MovementController.IsSubmerged() ||
         m_ship.MovementController.IsFlying()))
    {
      m_speedWakeRoot.SetActive(false);
    }
    else
    {
      if (m_ship.MovementController != null)
      {
        var speedWakePos = m_speedWakeRoot.transform.position;
        speedWakePos.y = m_ship.MovementController.ShipFloatationObj
          .AverageWaterHeight;
        m_speedWakeRoot.transform.position = speedWakePos;
      }

      m_speedWakeRoot.SetActive(true);
      SetWake(flag, deltaTime);
    }

    if (m_sailSound != null && m_ship.MovementController != null)
    {
      var target = m_ship!.MovementController!.IsSailUp() ? m_sailBaseVol : 0f;
      FadeSound(m_sailSound, target, m_sailFadeDuration, deltaTime);
    }

    if (m_splashEffects != null && m_ship.MovementController != null)
      m_splashEffects.SetActive(m_ship!.OnboardController!.HasPlayersOnboard);
  }

  private void SetWake(bool enabled, float dt)
  {
    var wakeParticles = m_wakeParticles;
    for (var i = 0; i < wakeParticles.Length; i++)
    {
      var emission = wakeParticles[i].emission;
      emission.enabled = enabled;
    }

    FadeSounds(m_wakeSounds, enabled, dt);
  }

  private void FadeSounds(List<KeyValuePair<AudioSource, float>> sources,
    bool enabled, float dt)
  {
    foreach (var source in sources)
      if (enabled)
        FadeSound(source.Key, source.Value, m_audioFadeDuration, dt);
      else
        FadeSound(source.Key, 0f, m_audioFadeDuration, dt);
  }

  private static void FadeSound(AudioSource source, float target,
    float fadeDuration, float dt)
  {
    var maxDelta = dt / fadeDuration;
    if (target > 0f)
    {
      if (!source.isPlaying) source.Play();

      source.volume = Mathf.MoveTowards(source.volume, target, maxDelta);
    }
    else if (source.isPlaying)
    {
      source.volume = Mathf.MoveTowards(source.volume, 0f, maxDelta);
      if (source.volume <= 0f) source.Stop();
    }
  }
}