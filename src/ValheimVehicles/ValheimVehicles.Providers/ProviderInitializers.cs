using BepInEx;
using BepInEx.Logging;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Providers;

public static class ProviderInitializers
{
  /// <summary>
  /// Responsible for initializing all ValheimRAFT / ValheimVehicle provider overrides
  /// </summary>
  ///
  /// <note>
  /// This is a new pattern to ensure this Mod can test code in isolation but still reference valheim code at runtime
  /// </note>
  public static void InitProviders(ManualLogSource Logger)
  {
    new WearNTearIntegrationProvider().Init();
    LoggerProvider.Setup(Logger);
  }
}