using ValheimVehicles.Config;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.UI;
namespace ValheimVehicles.Components;

/// <summary>
/// This gets around the non-generic usage of MonoBehavior. We have to directly make this class extension and then are able to AddComponent etc.
/// </summary>
public class SwivelConfigRPCSync : PrefabConfigRPCSync<SwivelCustomConfig, ISwivelConfig>
{
  public override void Awake()
  {
    base.Awake();
    rpcHandler?.Register<int>(nameof(RPC_SetMotionState), RPC_SetMotionState);
  }
  public void RequestNextMotionState(MotionState nextMotionState)
  {
    if (!this.IsNetViewValid(out var netView)) return;
    netView.InvokeRPC(nameof(RPC_SetMotionState), (int)nextMotionState);
  }

  public void RPC_SetMotionState(long sender, int state)
  {
    var nextMotionState = (MotionState)state;
    if (this.IsNetViewValid(out var netView) && netView.GetZDO().GetOwner() == sender)
    {
      netView.m_zdo.Set(SwivelCustomConfig.Key_MotionState, state);
      SyncMotionState(nextMotionState);
    }
  }

  /// <summary>
  /// Matches the data exactly. But would require the client to first set this value.
  /// </summary>
  public void SyncMotionState()
  {
    if (!this.IsNetViewValid(out var netView)) return;
    controller.MotionState = (MotionState)netView.GetZDO().GetInt(SwivelCustomConfig.Key_MotionState, (int)controller.MotionState);
  }

  /// <summary>
  /// This is unsafe but fast.
  /// </summary>
  /// <param name="state"></param>
  public void SyncMotionState(MotionState state)
  {
    controller.MotionState = state;
  }
}