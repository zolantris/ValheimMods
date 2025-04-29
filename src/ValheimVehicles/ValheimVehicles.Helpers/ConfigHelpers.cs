using BepInEx.Configuration;

namespace ValheimVehicles.Helpers;

public static class ConfigHelpers
{
  public static ConfigDescription CreateConfigDescription(string description,
    bool isAdmin = false,
    bool isAdvanced = false, AcceptableValueBase? acceptableValues = null)
  {
    return new ConfigDescription(
      description,
      acceptableValues,
      new ConfigurationManagerAttributes()
      {
        IsAdminOnly = isAdmin,
        IsAdvanced = isAdvanced,
      }
    );
  }
}