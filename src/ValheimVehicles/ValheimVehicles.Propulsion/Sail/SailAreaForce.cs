/*
 * base values only for Config
 *
 * All of these values can be overriden by user config.
 */

namespace ValheimVehicles.Propulsion.Sail;

static class SailAreaForce
{
  public static readonly float Tier1 = 5f;
  public static readonly float Tier2 = 7.5f;
  public static readonly float Tier3 = 10f;
  public static readonly float CustomTier1AreaForceMultiplier = 0.5f;
  public static readonly bool HasPropulsionConfigOverride = false;
}