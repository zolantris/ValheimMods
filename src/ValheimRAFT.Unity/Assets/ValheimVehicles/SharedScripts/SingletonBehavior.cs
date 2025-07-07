#region

using System;
using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
  {
    public static T? Instance { get; protected set; }

    public virtual void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject); // Destroy the whole GameObject, not just the component
        return;
      }

      Instance = (T)this;
      DontDestroyOnLoad(gameObject); // Persist across scenes
      OnAwake();
      OnPostAwake?.Invoke();
    }

    protected virtual void OnDestroy()
    {
      // Only clear if this instance is the current one
      if (Instance == this)
        Instance = null;
    }

    public static event Action? OnPostAwake;

    // For Editor/Domain reloads, re-create singleton if missing
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EditorDomainReloadInit()
    {
      if (Instance == null)
      {
        var go = new GameObject(typeof(T).Name);
        Instance = go.AddComponent<T>();
        DontDestroyOnLoad(go);
        Instance?.OnAwake();
        OnPostAwake?.Invoke();
      }
    }
#endif

    public virtual void OnAwake() { }
  }
}