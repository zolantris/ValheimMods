using BepInEx;
using BepInEx.Configuration;

namespace DamageControl
{
  public class DamageSourceConfig
  {
    /**
     * Prevents going over X Damage so a damage mod does not cause issues with some tweaked spells/items etc
     */
    public ConfigEntry<int> DamageCap;

    /**
     * Allows for setting max damage
     */
    public ConfigEntry<bool> DamageCapEnabled;

    /**
     * Allows for setting damage to specific building types
     */
    public ConfigEntry<float> DamageMultiplier;

    /**
     * allows users to toggle this specific damage modifier on or off
     */
    public ConfigEntry<bool> DamageMultiplierEnabled;

    public DamageSourceConfig Init(BaseUnityPlugin plugin, string section, string key,
      string description)
    {
      DamageMultiplierEnabled =
        plugin.Config.Bind(section, $"{key}_enable_damageMultiplier", true,
          "Enable ${description}");
      DamageMultiplier =
        plugin.Config.Bind(section, $"{key}_damage_multiplier", 1f,
          "Set the damage multiplier for ${description}");
      DamageCapEnabled =
        plugin.Config.Bind(section, $"{key}_enable_damage_cap", false,
          "EnableDamageCap for ${description}");
      DamageCap =
        plugin.Config.Bind(section, $"{key}_damage_cap", 200,
          "Set the damage cap for ${description}");
      return this;
    }
  }
}