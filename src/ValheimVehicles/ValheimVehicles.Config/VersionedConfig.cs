using System;
using ValheimRAFT;
namespace ValheimVehicles.Config;

public class VersionedConfig
{
  private static string _minorVersionKey = "";
  public static string GetDynamicMinorVersionKey()
  {
    if (_minorVersionKey != string.Empty)
    {
      return _minorVersionKey;
    }

    var version = ValheimRaftPlugin.Version; // e.g., "3.0.5"
    var parsedVersion = System.Version.Parse(version);
    _minorVersionKey = $"MaxVehicleLinearVelocity_{parsedVersion.Major}.{parsedVersion.Minor}.x";
    return _minorVersionKey;
  }
}