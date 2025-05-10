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

      // for actions listening to this behavior but not part of the component directly.
      public static event Action? OnPostAwake;
      public virtual void OnAwake() {}

      public virtual void Awake()
      {
        if (Instance != null && Instance != this)
        {
          Destroy(this);
          return;
        }
        Instance = (T)this;
        OnAwake();
        OnPostAwake?.Invoke();
      }
    }
  }