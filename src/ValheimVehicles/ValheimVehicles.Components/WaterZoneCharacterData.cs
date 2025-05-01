using ValheimVehicles.Controllers;

namespace ValheimVehicles.Components;

public interface ICharacterOnboardData
{
  public bool IsUnderwater { get; set; }
  public bool IsWithinWaterMask { get; set; }
  public bool IsWithinShip { get; set; }
  public Character character { get; set; }
  public ZDOID zdoId { get; set; }
}

/// <summary>
/// This is meant to keep additional Character specific data within each character to avoid conflicts.
/// It does not extend character as this would create an additional pointer (and then require management in lifecycle).
/// </summary>
public class WaterZoneCharacterData : ICharacterOnboardData
{
  public bool IsUnderwater { get; set; }
  public bool IsWithinWaterMask { get; set; }
  public bool IsWithinShip { get; set; }
  public Character character { get; set; }
  public ZDOID zdoId { get; set; }
  public ZDOID controllerZdoId { get; set; }

  // can turn null
  public VehicleOnboardController? OnboardController;

  public VehicleBaseController? VehicleShip =>
    OnboardController?.vehicleShip?.Instance;

  public WaterZoneController? WaterZoneController;
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

  public WaterZoneCharacterData(Character characterInstance,
    WaterZoneController? waterZoneController = null)
  {
    waterZoneController = waterZoneController;
    character = characterInstance;
    zdoId = character.GetZDOID();
    OnboardController = null;
    controllerZdoId =
      waterZoneController.GetComponent<ZNetView>().GetZDO().m_uid;
  }

  public WaterZoneCharacterData(Character characterInstance,
    VehicleOnboardController? onboardControllerInstance)
  {
    character = characterInstance;
    zdoId = character.GetZDOID();
    OnboardController = onboardControllerInstance;
  }
}