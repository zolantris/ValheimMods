// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

namespace ValheimVehicles.SharedScripts.UI
{
  public interface ICannonPersistentConfig
  {
    public CannonballVariant AmmoVariant { get; set; }
    public CannonFiringMode CannonFiringMode { get; set; }
  }
}