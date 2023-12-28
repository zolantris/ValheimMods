namespace ValheimRAFT;

static class SailAreaForce
{
  public static readonly float Tier1 = 5f;
  public static readonly float Tier2 = 10f;
  public static readonly float Tier3 = 20f;
  public static readonly float CustomTier1AreaForceMultiplier = 0.5f;

  public static bool HasPropulsionConfigOverride = false;
  public static float SailAreaThrottle = 20f;
}