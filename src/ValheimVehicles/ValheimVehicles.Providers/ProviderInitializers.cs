using Jotunn;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.ValheimVehicles.Providers;

public static class ProviderInitializers
{
  /// <summary>
  /// Responsible for initializing all ValheimRAFT / ValheimVehicle provider overrides
  /// </summary>
  ///
  /// <note>
  /// This is a new pattern to ensure this Mod can test code in isolation but still reference valheim code at runtime
  /// </note>
  public static void InitProviders()
  {
    new WearNTearIntegrationProvider().Init();

    // bind loggers.
    ConvexHullCalculator.LogDebug = Logger.LogDebug;
    ConvexHullCalculator.LogMessage = Logger.LogMessage;
  }
}