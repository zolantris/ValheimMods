namespace ValheimVehicles.SharedScripts.Enums
{
  public abstract class PrefabTiers
  {
    private const string TierName = "tier";
    public const string Tier1 = "tier1"; // wood
    public const string Tier2 = "tier2"; // copper/bronze
    public const string Tier3 = "tier3"; // iron or iron + copper/bronze-hybrids
    public const string Tier4 = "tier4"; // silver
    public const string Tier5 = "tier5"; // blackmetal
    public const string Tier6 = "tier6"; // mistlands
    public const string Tier7 = "tier7"; // ashlands
    public const string Tier8 = "tier8"; // deepnorth TBD

    public static int GetTierValue(string val)
    {
      var intVal = val.Replace(TierName, "");
      var isValid = int.TryParse(intVal, out var tierValue);
      return !isValid ? -1 : tierValue;
    }

    public static string GetTierMaterialTranslation(string val) =>
      $"$valheim_vehicles_material_{val}";
  }
}