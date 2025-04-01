using UnityEngine;

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
namespace ValheimVehicles.SharedScripts
{
  public class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
  {
    public static T? Instance { get; protected set; }

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(this);
        throw new System.Exception("An instance of this singleton already exists.");
      }
      else
      {
        Instance = (T)this;
      }
    }
  }
}
