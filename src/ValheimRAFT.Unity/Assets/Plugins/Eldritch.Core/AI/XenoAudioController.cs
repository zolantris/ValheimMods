using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Eldritch.Core
{
  public class XenoAudioController : MonoBehaviour
  {
    [Header("Clips - Variations (optional)")]
    public AudioClip[] idleClips;
    public AudioClip[] hissClips;
    public AudioClip[] roarClips;
    public AudioClip[] hurtClips;
    public AudioClip[] foleyClips; // footsteps/body
    public AudioClip[] attackClips;
    public AudioClip[] impactClips; // world impacts

    [Header("Primary Voice Source (single)")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Pools")]
    [SerializeField] private int foleyPoolSize = 3;
    [SerializeField] private int attackPoolSize = 3;
    [SerializeField] private int impactPoolSize = 4;

    [Header("3D Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f;
    public float minDistance = 1.5f;
    public float maxDistance = 25f;

    [Header("Rate Limits (seconds)")]
    public float globalMinInterval = 0.05f;
    public float voiceMinInterval = 0.35f;
    public float foleyMinInterval = 0.05f;
    public float attackMinInterval = 0.08f;
    public float hurtMinInterval = 0.20f;
    public float impactMinInterval = 0.08f;

    [Header("Pitch/Volume Jitter")]
    public Vector2 voicePitchRange = new(0.95f, 1.05f);
    public Vector2 foleyPitchRange = new(0.95f, 1.05f);
    public Vector2 attackPitchRange = new(0.95f, 1.05f);
    public Vector2 hurtPitchRange = new(0.95f, 1.05f);
    public Vector2 impactPitchRange = new(0.95f, 1.05f);
    [Range(0f, 1f)] public float voiceVolume = 1f;
    [Range(0f, 1f)] public float foleyVolume = 0.85f;
    [Range(0f, 1f)] public float attackVolume = 0.95f;
    [Range(0f, 1f)] public float hurtVolume = 0.9f;
    [Range(0f, 1f)] public float impactVolume = 1f;

    [Header("Behavior")]
    public bool allowStealOldest = true; // if busy, steal the source finishing soonest
    public bool dropOnBusy; // if true, drop new sounds when no free source

    [Header("Audio Mixer (optional)")]
    public AudioMixerGroup voiceGroup;
    public AudioMixerGroup foleyGroup;
    public AudioMixerGroup attackGroup;
    public AudioMixerGroup impactGroup;

    // Pools
    private readonly List<AudioSource> _foleyPool = new();
    private readonly List<AudioSource> _attackPool = new();
    private readonly List<AudioSource> _impactPool = new();

    // PlayOneShot end-time tracking for reliable busy/steal logic
    private readonly Dictionary<AudioSource, float> _sourceEndTimes = new();

    // Rate-limit bookkeeping
    private float _lastGlobalTime;
    private float _lastVoiceTime;
    private float _lastFoleyTime;
    private float _lastAttackTime;
    private float _lastHurtTime;
    private float _lastImpactTime;
    private readonly Dictionary<AudioClip, float> _lastClipTime = new();

    private void Awake()
    {
      // Ensure a primary voice source exists and is configured
      if (!voiceSource)
      {
        voiceSource = GetComponent<AudioSource>();
        if (!voiceSource)
          voiceSource = gameObject.AddComponent<AudioSource>();
      }
      ConfigureSource(voiceSource, voiceGroup);

      // Build category pools
      BuildPool(_foleyPool, foleyPoolSize, "Audio_Foley");
      BuildPool(_attackPool, attackPoolSize, "Audio_Attack");
      BuildPool(_impactPool, impactPoolSize, "Audio_Impact");
    }

    private void ConfigureSource(AudioSource src, AudioMixerGroup group = null)
    {
      src.playOnAwake = false;
      src.spatialBlend = spatialBlend;
      src.minDistance = minDistance;
      src.maxDistance = maxDistance;
      src.rolloffMode = AudioRolloffMode.Linear;
      if (group) src.outputAudioMixerGroup = group;
    }

    private void BuildPool(List<AudioSource> pool, int size, string childPrefix)
    {
      pool.Clear();
      for (var i = 0; i < Mathf.Max(0, size); i++)
      {
        var go = new GameObject($"{childPrefix}_{i}");
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        var group = childPrefix.Contains("Foley")
          ? foleyGroup
          : childPrefix.Contains("Attack")
            ? attackGroup
            : childPrefix.Contains("Impact")
              ? impactGroup
              : null;
        ConfigureSource(src, group);
        _sourceEndTimes[src] = 0f;
        pool.Add(src);
      }
    }

    // ------------- Public API -------------

    public bool PlayIdle()
    {
      return TryPlayVoice(Pick(idleClips), voiceMinInterval);
    }
    public bool PlayHiss()
    {
      return TryPlayVoice(Pick(hissClips), voiceMinInterval);
    }
    public bool PlayRoar()
    {
      return TryPlayVoice(Pick(roarClips), voiceMinInterval);
    }

    public bool PlayHurt()
    {
      var clip = Pick(hurtClips);
      if (!CanPlayNow(globalMinInterval, ref _lastGlobalTime)) return false;
      if (!CanPlayNow(hurtMinInterval, ref _lastHurtTime)) return false;
      if (!CanPlayClip(clip, hurtMinInterval)) return false;
      PlayOn(voiceSource, clip, hurtVolume, hurtPitchRange);
      MarkPlayed(clip);
      return true;
    }

    public bool PlayFoley(AudioClip clip = null)
    {
      if (clip == null) clip = Pick(foleyClips);
      if (!CanPlayNow(globalMinInterval, ref _lastGlobalTime)) return false;
      if (!CanPlayNow(foleyMinInterval, ref _lastFoleyTime)) return false;
      if (!CanPlayClip(clip, foleyMinInterval)) return false;
      if (!TryAcquire(_foleyPool, out var src)) return false;
      PlayOn(src, clip, foleyVolume, foleyPitchRange);
      MarkPlayed(clip);
      return true;
    }

    public bool PlayAttack(AudioClip clip = null)
    {
      if (clip == null) clip = Pick(attackClips);
      if (!CanPlayNow(globalMinInterval, ref _lastGlobalTime)) return false;
      if (!CanPlayNow(attackMinInterval, ref _lastAttackTime)) return false;
      if (!CanPlayClip(clip, attackMinInterval)) return false;
      if (!TryAcquire(_attackPool, out var src)) return false;
      PlayOn(src, clip, attackVolume, attackPitchRange);
      MarkPlayed(clip);
      return true;
    }

    public bool PlayImpact(Vector3 worldPos, AudioClip clip = null)
    {
      if (clip == null) clip = Pick(impactClips);
      if (!CanPlayNow(globalMinInterval, ref _lastGlobalTime)) return false;
      if (!CanPlayNow(impactMinInterval, ref _lastImpactTime)) return false;
      if (!CanPlayClip(clip, impactMinInterval)) return false;
      if (!TryAcquire(_impactPool, out var src)) return false;
      src.transform.position = worldPos;
      PlayOn(src, clip, impactVolume, impactPitchRange);
      MarkPlayed(clip);
      return true;
    }

    // ------------- Helpers -------------

    private static AudioClip Pick(AudioClip[] clips)
    {
      if (clips == null || clips.Length == 0) return null;
      var i = Random.Range(0, clips.Length);
      return clips[i];
    }

    private bool TryAcquire(List<AudioSource> pool, out AudioSource src)
    {
      for (var i = 0; i < pool.Count; i++)
        if (!IsBusy(pool[i]))
        {
          src = pool[i];
          return true;
        }

      if (dropOnBusy)
      {
        src = null;
        return false;
      }
      if (allowStealOldest && pool.Count > 0)
      {
        src = pool[0];
        var minRemaining = Remaining(src);
        for (var i = 1; i < pool.Count; i++)
        {
          var r = Remaining(pool[i]);
          if (r < minRemaining)
          {
            minRemaining = r;
            src = pool[i];
          }
        }
        src.Stop();
        _sourceEndTimes[src] = 0f;
        return true;
      }
      src = null;
      return false;
    }

    private bool IsBusy(AudioSource s)
    {
      return Time.time < RemainingEndTime(s);
    }

    private float RemainingEndTime(AudioSource s)
    {
      return _sourceEndTimes.TryGetValue(s, out var end) ? end : 0f;
    }

    private float Remaining(AudioSource s)
    {
      var end = RemainingEndTime(s);
      return Mathf.Max(0f, end - Time.time);
    }

    private void PlayOn(AudioSource src, AudioClip clip, float volume, Vector2 pitchRange)
    {
      if (!clip || !src) return;
      var pitch = Random.Range(pitchRange.x, pitchRange.y);
      src.pitch = pitch;
      src.PlayOneShot(clip, volume);
      var dur = clip.length / Mathf.Max(0.01f, pitch); // pitch affects perceived duration
      _sourceEndTimes[src] = Time.time + dur;
    }

    private bool CanPlayNow(float interval, ref float last)
    {
      var now = Time.time;
      if (now - last < interval) return false;
      last = now;
      return true;
    }

    private bool CanPlayClip(AudioClip clip, float minRepeat)
    {
      if (!clip) return false;
      var now = Time.time;
      if (_lastClipTime.TryGetValue(clip, out var t) && now - t < minRepeat) return false;
      return true;
    }

    private void MarkPlayed(AudioClip clip)
    {
      if (!clip) return;
      _lastClipTime[clip] = Time.time;
    }

    private bool TryPlayVoice(AudioClip clip, float minInterval)
    {
      if (!clip) return false;
      if (!CanPlayNow(globalMinInterval, ref _lastGlobalTime)) return false;
      if (!CanPlayNow(minInterval, ref _lastVoiceTime)) return false;
      if (!CanPlayClip(clip, minInterval)) return false;
      PlayOn(voiceSource, clip, voiceVolume, voicePitchRange);
      MarkPlayed(clip);
      return true;
    }
  }
}