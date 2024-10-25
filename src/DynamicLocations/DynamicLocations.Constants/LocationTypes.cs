namespace DynamicLocations.Constants;

public enum LocationVariation
{
  Spawn,
  Logout
}

/// <summary>
/// For ModAPI and a way to cleanly match types if needing to send from string
/// </summary>
public static class LocationVariationUtils
{
  public const string LogoutString = "logout";
  public const string SpawnString = "spawn";

  /// <summary>
  /// Quick way to cast to the actual enum.
  /// </summary>
  /// todo Dictionary map of enum to string might be more efficient
  /// <param name="locationVarationString"></param>
  /// <returns></returns>
  public static LocationVariation? ToLocationVaration(
    string? locationVarationString)
  {
    return locationVarationString?.ToLower() switch
    {
      LogoutString => LocationVariation.Logout,
      SpawnString => LocationVariation.Spawn,
      _ => null
    };
  }
}