using UnityEngine;
using ValheimVehicles.Interfaces;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Config;

public class SwivelCustomConfig : ISerializableConfig<SwivelCustomConfig>
{
  // ZDO keys
  private const string Key_Mode = "swivel_mode";
  private const string Key_MaxTurnAnglePerSecond = "swivel_turnSpeed";
  private const string Key_MaxTurnAngle = "swivel_maxTurnAngle";
  private const string Key_MaxInclineZ = "swivel_maxInclineZ";
  private const string Key_TurningLerpSpeed = "swivel_turnLerpSpeed";
  private const string Key_MinTrackingRange = "swivel_minTrackingRange";
  private const string Key_MaxTrackingRange = "swivel_maxTrackingRange";
  private const string Key_IsDoorOpen = "swivel_isDoorOpen";
  private const string Key_DoorLerpSpeed = "swivel_doorLerpSpeed";
  private const string Key_HingeMode = "swivel_hingeMode";
  private const string Key_ZHingeDirection = "swivel_zHingeDir";
  private const string Key_YHingeDirection = "swivel_yHingeDir";
  private const string Key_MaxYAngle = "swivel_maxYAngle";

  // Backing fields
  public SwivelMode Mode { get; set; } = SwivelMode.DoorMode;
  public float MaxTurnAnglePerSecond { get; set; } = 90f;
  public float MaxTurnAngle { get; set; } = 90f;
  public float MaxInclineZ { get; set; } = 90f;
  public float TurningLerpSpeed { get; set; } = 50f;
  public float MinTrackingRange { get; set; } = 5f;
  public float MaxTrackingRange { get; set; } = 50f;
  public bool IsDoorOpen { get; set; } = false;
  public float DoorLerpSpeed { get; set; } = 100f;
  public SwivelComponent.DoorHingeMode HingeMode { get; set; } = SwivelComponent.DoorHingeMode.YOnly;
  public SwivelComponent.HingeDirection ZHingeDirection { get; set; } = SwivelComponent.HingeDirection.Forward;
  public SwivelComponent.HingeDirection YHingeDirection { get; set; } = SwivelComponent.HingeDirection.Forward;
  public float MaxYAngle { get; set; } = 90f;

  public void Serialize(ZPackage package)
  {
    package.Write((int)Mode);
    package.Write(MaxTurnAnglePerSecond);
    package.Write(MaxTurnAngle);
    package.Write(MaxInclineZ);
    package.Write(TurningLerpSpeed);
    package.Write(MinTrackingRange);
    package.Write(MaxTrackingRange);
    package.Write(IsDoorOpen);
    package.Write(DoorLerpSpeed);
    package.Write((int)HingeMode);
    package.Write((int)ZHingeDirection);
    package.Write((int)YHingeDirection);
    package.Write(MaxYAngle);
  }

  public void Save(ZDO zdo, SwivelCustomConfig config)
  {
    zdo.Set(Key_Mode, (int)config.Mode);
    zdo.Set(Key_MaxTurnAnglePerSecond, config.MaxTurnAnglePerSecond);
    zdo.Set(Key_MaxTurnAngle, config.MaxTurnAngle);
    zdo.Set(Key_MaxInclineZ, config.MaxInclineZ);
    zdo.Set(Key_TurningLerpSpeed, config.TurningLerpSpeed);
    zdo.Set(Key_MinTrackingRange, config.MinTrackingRange);
    zdo.Set(Key_MaxTrackingRange, config.MaxTrackingRange);
    zdo.Set(Key_IsDoorOpen, config.IsDoorOpen);
    zdo.Set(Key_DoorLerpSpeed, config.DoorLerpSpeed);
    zdo.Set(Key_HingeMode, (int)config.HingeMode);
    zdo.Set(Key_ZHingeDirection, (int)config.ZHingeDirection);
    zdo.Set(Key_YHingeDirection, (int)config.YHingeDirection);
    zdo.Set(Key_MaxYAngle, config.MaxYAngle);
  }

  public SwivelCustomConfig Load(ZDO zdo)
  {
    return new SwivelCustomConfig
    {
      Mode = (SwivelMode)zdo.GetInt(Key_Mode, (int)SwivelMode.DoorMode),
      MaxTurnAnglePerSecond = zdo.GetFloat(Key_MaxTurnAnglePerSecond, 90f),
      MaxTurnAngle = zdo.GetFloat(Key_MaxTurnAngle, 90f),
      MaxInclineZ = zdo.GetFloat(Key_MaxInclineZ, 90f),
      TurningLerpSpeed = zdo.GetFloat(Key_TurningLerpSpeed, 50f),
      MinTrackingRange = zdo.GetFloat(Key_MinTrackingRange, 5f),
      MaxTrackingRange = zdo.GetFloat(Key_MaxTrackingRange, 50f),
      IsDoorOpen = zdo.GetBool(Key_IsDoorOpen),
      DoorLerpSpeed = zdo.GetFloat(Key_DoorLerpSpeed, 100f),
      HingeMode = (SwivelComponent.DoorHingeMode)zdo.GetInt(Key_HingeMode, (int)SwivelComponent.DoorHingeMode.YOnly),
      ZHingeDirection = (SwivelComponent.HingeDirection)zdo.GetInt(Key_ZHingeDirection, (int)SwivelComponent.HingeDirection.Forward),
      YHingeDirection = (SwivelComponent.HingeDirection)zdo.GetInt(Key_YHingeDirection, (int)SwivelComponent.HingeDirection.Forward),
      MaxYAngle = zdo.GetFloat(Key_MaxYAngle, 90f)
    };
  }

  public SwivelCustomConfig Deserialize(ZPackage package)
  {
    return new SwivelCustomConfig
    {
      Mode = (SwivelMode)package.ReadInt(),
      MaxTurnAnglePerSecond = package.ReadSingle(),
      MaxTurnAngle = package.ReadSingle(),
      MaxInclineZ = package.ReadSingle(),
      TurningLerpSpeed = package.ReadSingle(),
      MinTrackingRange = package.ReadSingle(),
      MaxTrackingRange = package.ReadSingle(),
      IsDoorOpen = package.ReadBool(),
      DoorLerpSpeed = package.ReadSingle(),
      HingeMode = (SwivelComponent.DoorHingeMode)package.ReadInt(),
      ZHingeDirection = (SwivelComponent.HingeDirection)package.ReadInt(),
      YHingeDirection = (SwivelComponent.HingeDirection)package.ReadInt(),
      MaxYAngle = package.ReadSingle()
    };
  }
}