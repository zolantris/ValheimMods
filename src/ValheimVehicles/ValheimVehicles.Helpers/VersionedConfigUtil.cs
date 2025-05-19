using ValheimVehicles.Compat;
namespace ValheimVehicles.Helpers;

public class VersionedConfigUtil
{
  private static string _minorVersionKey = "";
  public static string GetDynamicMinorVersionKey()
  {
    if (_minorVersionKey != string.Empty)
    {
      return _minorVersionKey;
    }

    var parsedVersion = System.Version.Parse(ValheimRAFT_API.GetPluginVersion());
    _minorVersionKey = $"{parsedVersion.Major}.{parsedVersion.Minor}.x";
    return _minorVersionKey;
  }
}