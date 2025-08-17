using UnityEngine;
namespace Eldritch.Core
{
  public interface IXenoAI
  {
    Transform Transform { get; }
    bool IsManualControlling { get; }
    bool IsSleeping();
    void StopSleeping();
    void StartSleeping();
    // Add more methods/events relevant to your AI brain
  }
}