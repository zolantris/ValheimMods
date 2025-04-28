using System;
using UnityEngine;
namespace ValheimVehicles.Controllers;

[Serializable]
public class RetryGuard
{
  private int _retryCount;
  private readonly int _maxRetries;
  private readonly MonoBehaviour _monoBehaviour;

  public RetryGuard(MonoBehaviour monoBehaviour, int maxRetries = 50)
  {
    _monoBehaviour = monoBehaviour;
    _maxRetries = maxRetries;
    _retryCount = 0;
  }

  public bool CanRetry => _retryCount < _maxRetries;

  public void Retry(Action method, float delaySeconds)
  {
    if (!CanRetry)
      return;

    _retryCount++;
    _monoBehaviour.Invoke(method.Method.Name, delaySeconds);
  }

  public void Reset()
  {
    _retryCount = 0;
  }
}