// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using UnityEngine;
namespace ValheimVehicles.SharedScripts.UI
{
  public interface ICannonConfig
  {
    public int AmmoCount { get; set; }
    public Cannonball.CannonballType AmmoType { get; set; }
  }
}