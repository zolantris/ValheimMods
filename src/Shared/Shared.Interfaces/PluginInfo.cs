namespace Shared.Interfaces;

public interface PluginInfo
{
  string Name { get; }
  string Version { get; }
  string Guid { get; }
}