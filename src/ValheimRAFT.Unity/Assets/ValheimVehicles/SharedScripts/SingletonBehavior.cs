#region

  using UnityEngine;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
  {
    public static T? Instance { get; protected set; }

    public void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(this);
        return;
      }
      Instance = (T)this;
      OnAwake();
    }

    protected virtual void OnAwake() {}
  }
}