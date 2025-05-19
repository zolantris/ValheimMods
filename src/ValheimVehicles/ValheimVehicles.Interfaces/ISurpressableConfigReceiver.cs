using System;
namespace ValheimVehicles.Interfaces;

public interface ISuppressableConfigReceiver
{
  void SuppressConfigSync(Action action);
}