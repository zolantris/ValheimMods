using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace Eldritch.Core
{


  public class XenoAudioController : MonoBehaviour
  {
    [Header("Routing / Anchor")]
    [Tooltip("Optional parent for all audio sources (e.g., head/face bone). If null, uses this transform.")]
    [SerializeField] private Transform audioAnchor;

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

    // Idle loop configuration (continuous ambience)
    [Header("Idle Loop (continuous)")]
    public bool enableIdleLoop = true;
    public AudioClip idleLoopClip; // optional; falls back to idleClips[0]
    [Range(0f, 1f)] public float idleLoopVolume = 0.35f;
    public Vector2 idleLoopPitchRange = new(0.98f, 1.02f);
    [Tooltip("Within this distance the idle is most audible.")]
    public float idleNearDistance = 2f;
    [Tooltip("Beyond this distance the idle is effectively inaudible.")]
    public float idleMaxAudibleDistance = 12f;
    [Tooltip("Optional custom rolloff curve (x: distance in meters, y: volume multiplier). If empty, a default curve is generated.")]
    public AnimationCurve idleLoopRolloff;

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

    // Dedicated idle loop source
    [SerializeField] [HideInInspector] private AudioSource _idleSource;

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

    [SerializeField] [Tooltip("Name for the idle loop child under the anchor.")]
    private string idleChildName = "Audio_IdleLoop";
    private Transform EffectiveAnchor()
    {
      return audioAnchor != null ? audioAnchor : transform;
    }


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

      // Prepare idle loop source
      EnsureIdleLoopSource();
    }

    private void OnEnable()
    {
      if (enableIdleLoop) StartIdleLoop();
    }

    private void OnDisable()
    {
      StopIdleLoop();
    }

    private void OnValidate()
    {
      // --- clamp your existing serialized values here as you already do ---

      // Edit mode: DO NOT CREATE. Only rebind + reconfigure if present.
      if (!Application.isPlaying)
      {
        RebindIdleSourceIfNeeded(); // only finds existing
        ReparentIdleSourceToAnchor(); // moves under audioAnchor if set
        if (_idleSource != null)
          ApplyIdleConfig(_idleSource); // safe editor-time property sync
        return;
      }

      // Play mode: safe to fully ensure configuration/creation
      EnsureIdleLoopSource();
    }
    private void RebindIdleSourceIfNeeded()
    {
      if (_idleSource != null) return;

      // Try to find by name under current anchor
      var anchor = EffectiveAnchor();
      var found = FindChildAudioByName(anchor, idleChildName);
      if (found != null) _idleSource = found;
    }
    private void ReparentIdleSourceToAnchor()
    {
      if (_idleSource == null) return;
      var anchor = EffectiveAnchor();
      if (_idleSource.transform.parent != anchor)
      {
#if UNITY_EDITOR
        // keep editor operations undoable
        Undo.SetTransformParent(_idleSource.transform, anchor, "Reparent Audio Source");
        _idleSource.transform.localPosition = Vector3.zero;
        _idleSource.transform.localRotation = Quaternion.identity;
        _idleSource.transform.localScale = Vector3.one;
#else
        _idleSource.transform.SetParent(anchor, false);
#endif
      }
    }

    /// <summary>Play-mode only: ensure we have a correctly configured idle loop source.</summary>
    private void EnsureIdleLoopSource()
    {
      // If disabled, stop and exit
      if (!enableIdleLoop)
      {
        if (_idleSource && _idleSource.isPlaying) _idleSource.Stop();
        return;
      }

      // Rebind first if reference was lost
      if (_idleSource == null) RebindIdleSourceIfNeeded();

      // Only create in play mode
      if (_idleSource == null)
      {
        var anchor = EffectiveAnchor();
        var go = new GameObject(idleChildName);
        go.transform.SetParent(anchor, false);
        _idleSource = go.AddComponent<AudioSource>();
      }

      // Make sure it's under the anchor
      ReparentIdleSourceToAnchor();

      // Configure and (re)start
      ApplyIdleConfig(_idleSource);
      if (_idleSource.clip && !_idleSource.isPlaying)
        _idleSource.Play();
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      Gizmos.color = new Color(0f, 1f, 0.2f, 0.35f);
      Gizmos.DrawWireSphere(transform.position, idleNearDistance);
      Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f);
      Gizmos.DrawWireSphere(transform.position, idleMaxAudibleDistance);
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("Audio/Move Existing Children To Anchor")]
    private void Editor_MoveChildrenToAnchor()
    {
      RebindIdleSourceIfNeeded();
      ReparentIdleSourceToAnchor();
      if (_idleSource) ApplyIdleConfig(_idleSource);
    }

    [ContextMenu("Audio/Rebuild Idle (Play Mode Only)")]
    private void Editor_RebuildIdlePlayOnly()
    {
      if (Application.isPlaying)
      {
        // Clean any duplicates under the anchor first (optional)
        var anchor = EffectiveAnchor();
        var dup = FindChildAudioByName(anchor, idleChildName);
        if (dup && dup != _idleSource)
          Destroy(dup.gameObject);

        if (_idleSource) Destroy(_idleSource.gameObject);
        _idleSource = null;

        EnsureIdleLoopSource();
      }
      else
      {
        Debug.Log("Idle rebuild only occurs in Play mode to avoid editor-time spawning.");
      }
    }
#endif

    private void ConfigureSource(AudioSource src, AudioMixerGroup group = null)
    {
      src.playOnAwake = false;
      src.spatialBlend = spatialBlend;
      src.minDistance = minDistance;
      src.maxDistance = maxDistance;
      src.rolloffMode = AudioRolloffMode.Linear;
      if (group) src.outputAudioMixerGroup = group;
    }

    /// <summary>Utility: search immediate children under a root for an AudioSource by name.</summary>
    private AudioSource FindChildAudioByName(Transform root, string childName)
    {
      if (!root) return null;
      for (var i = 0; i < root.childCount; i++)
      {
        var c = root.GetChild(i);
        if (c.name == childName)
        {
          var a = c.GetComponent<AudioSource>();
          if (a) return a;
        }
      }
      return null;
    }

    /// <summary>Public helper to set the audio anchor at runtime or editor and reparent existing sources.</summary>
    public void SetAudioAnchor(Transform newAnchor, bool moveExistingChildren = true)
    {
      audioAnchor = newAnchor;
      if (moveExistingChildren)
      {
        ReparentIdleSourceToAnchor();
      }
#if UNITY_EDITOR
      EditorUtility.SetDirty(this);
#endif
    }


    /// <summary>Apply idle loop settings to the given source (safe in editor and play mode).</summary>
    private void ApplyIdleConfig(AudioSource src)
    {
      if (src == null) return;

      src.playOnAwake = false;
      src.loop = true;
      src.spatialBlend = spatialBlend;

      src.minDistance = 0.1f;
      src.maxDistance = Mathf.Max(0.1f, idleMaxAudibleDistance);
      src.rolloffMode = AudioRolloffMode.Custom;

      src.volume = Mathf.Clamp01(idleLoopVolume);
      src.pitch = Mathf.Clamp(Random.Range(idleLoopPitchRange.x, idleLoopPitchRange.y), 0.01f, 3f);

      // Pick clip but don't force creation
      var chosen = idleLoopClip != null ? idleLoopClip : Pick(idleClips);
      if (chosen != null) src.clip = chosen;

      // Custom rolloff curve
      var curve = idleLoopRolloff == null || idleLoopRolloff.length == 0
        ? BuildDefaultIdleCurve()
        : idleLoopRolloff;

      src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

      if (voiceGroup) src.outputAudioMixerGroup = voiceGroup;
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


    // Create a curve that's very quiet until close, then rises quickly near the creature
    private AnimationCurve BuildDefaultIdleCurve()
    {
      var near = Mathf.Max(0.01f, idleNearDistance);
      var max = Mathf.Max(near + 0.01f, idleMaxAudibleDistance);

      var keys = new[]
      {
        new Keyframe(0f, 1f), // at the source: full volume
        new Keyframe(near, 0.8f), // near distance: still strong
        new Keyframe(Mathf.Min(near * 2f, max), 0.15f), // quickly drops beyond near
        new Keyframe(max, 0f) // effectively silent at max
      };
      return new AnimationCurve(keys);
    }

    // ------------- Public API -------------

    public bool PlayIdle()
    {
      return TryPlayVoice(Pick(idleClips), voiceMinInterval);
    }

    // Public control for idle loop
    public void StartIdleLoop()
    {
      EnsureIdleLoopSource();
      if (!enableIdleLoop || _idleSource == null || _idleSource.clip == null) return;
      if (_idleSource.isPlaying) return;
      _idleSource.Play();
    }

    public void StopIdleLoop()
    {
      if (_idleSource && _idleSource.isPlaying) _idleSource.Stop();
    }

    public void RefreshIdleLoopSettings()
    {
      if (_idleSource == null) return;
      EnsureIdleLoopSource();
      if (_idleSource.isPlaying)
      {
        _idleSource.volume = Mathf.Clamp01(idleLoopVolume);
        _idleSource.pitch = Mathf.Clamp(Random.Range(idleLoopPitchRange.x, idleLoopPitchRange.y), 0.5f, 2f);
      }
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