/*
 * base values only for Config
 *
 * All of these values can be overriden by user config.
 */

namespace ValheimVehicles.Propulsion.Sail;

static class SailAreaForce
{
  public static readonly float Tier1 = 5f;
  public static readonly float Tier2 = 10f;
  public static readonly float Tier3 = 20f;
  public static readonly float Tier4 = 40f;
  public static readonly float CustomTier1AreaForceMultiplier = 0.5f;
  public static readonly bool HasPropulsionConfigOverride = false;
}