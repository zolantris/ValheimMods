using System;
using UnityEngine;
namespace ValheimVehicles.Controllers;

[Serializable]
public class RetryGuard
{
  private int _retryCount;
  private readonly int _maxRetries;
  private readonly int _additiveDelayPerRetry = 0;
  private readonly MonoBehaviour _monoBehaviour;

  public RetryGuard(MonoBehaviour monoBehaviour, int maxRetries = 10, int additionalDelayPerRetry = 0)
  {
    _monoBehaviour = monoBehaviour;
    _maxRetries = maxRetries;
    _retryCount = 0;
    _additiveDelayPerRetry = additionalDelayPerRetry;
  }

  public bool CanRetry => _monoBehaviour != null && _monoBehaviour.isActiveAndEnabled && _retryCount < _maxRetries;

  private float GetDelay(float delaySeconds)
  {
    return delaySeconds + _additiveDelayPerRetry * _retryCount;
  }

  public void Retry(Action method, float delaySeconds = 0.5f)
  {
    if (!CanRetry)
      return;

    _retryCount++;

    var computedDelay = GetDelay(delaySeconds);
    _monoBehaviour.Invoke(method.Method.Name, computedDelay);
  }

  public void Reset()
  {
    _retryCount = 0;
  }
}