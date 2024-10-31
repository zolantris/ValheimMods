using ValheimVehicles.Vehicles.Controllers;

namespace ValheimVehicles.Vehicles.Components;

/// <summary>
/// This is meant to keep additional Character specific data within each character to avoid conflicts.
/// It does not extend character as this would create an additional pointer (and then require management in lifecycle).
/// </summary>
public class CharacterOnboardData
{
  public Character character;

  public ZDOID zdoid = ZDOID.None;

  // can turn null
  public VehicleOnboardController? controller;
  public bool IsUnderwater = false;
  private WaterVolume? _prevWaterVolume;

  public void UpdateUnderwaterStatus(bool? forceIsUnderwater = null)
  {
    if (forceIsUnderwater != null)
    {
      IsUnderwater = forceIsUnderwater.Value;
      return;
    }

    IsUnderwater =
      Floating.IsUnderWater(character.transform.position, ref _prevWaterVolume);
  }

  public bool IsSwimming
  {
    get
    {
      if (IsUnderwater) return false;
      return character.IsSwimming();
    }
  }

  public CharacterOnboardData(Character characterInstance,
    VehicleOnboardController controllerInstance)
  {
    character = characterInstance;
    zdoid = character.GetZDOID();
    controller = controllerInstance;
  }
}