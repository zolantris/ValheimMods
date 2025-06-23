namespace ValheimVehicles.Interfaces;

public interface IVehicleConfig
{
  public string Version
  {
    get;
    set;
  }

  public float TreadDistance
  {
    get;
    set;
  }

  public float TreadLength
  {
    get;
    set;
  }

  public float TreadHeight
  {
    get;
    set;
  }

  public float TreadScaleX
  {
    get;
    set;
  }

  public bool HasCustomFloatationHeight
  {
    get;
    set;
  }

  public float CustomFloatationHeight
  {
    get;
    set;
  }

  public float CenterOfMassOffset
  {
    get;
    set;
  }

  public bool ForceDocked
  {
    get;
    set;
  }
}