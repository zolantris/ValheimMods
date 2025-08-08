namespace Zolantris.Shared.Interfaces
{
  public interface IAnimatorIKRelayReceiver
  {
    public void OnAnimatorIKRelay(int layerIndex);
    public void OnAnimatorOverride(); // full override of animator.
  }
}