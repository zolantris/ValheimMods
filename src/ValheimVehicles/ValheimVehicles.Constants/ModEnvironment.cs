namespace ValheimVehicles.Constants;

/// <summary>
/// Way to abstract some environment values without having to do this per file.
/// </summary>
public static class ModEnvironment
{
#if DEBUG
  public const bool IsRelease = false;
  public const bool IsDebug = true;
#else
  public const bool IsRelease = true;
  public const bool IsDebug = false;
#endif
}