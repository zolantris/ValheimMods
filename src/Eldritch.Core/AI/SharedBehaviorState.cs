using UnityEngine;
namespace Eldritch.Core
{
  internal interface IBehaviorState
  {
    float DecisionTimer { get; set; }
  }

  public interface IBehaviorSharedState
  {
    public Transform PrimaryTarget { get; set; }
    public Transform Self { get; set; }
    public float DeltaPrimaryTarget { get; set; }
  }

  public struct BehaviorStateSync : IBehaviorSharedState
  {
    public Transform PrimaryTarget
    {
      get;
      set;
    }
    public Transform Self
    {
      get;
      set;
    }
    public float DeltaPrimaryTarget
    {
      get;
      set;
    }
  }

}