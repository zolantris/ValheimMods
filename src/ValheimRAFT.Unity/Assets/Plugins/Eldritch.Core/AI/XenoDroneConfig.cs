namespace Eldritch.Core
{
  /// <summary>
  /// Always should be a static reference for all Xeno Monobehaviors as to avoid desync issues and serializeation problems
  /// </summary>
  public static class XenoDroneConfig
  {
    public static readonly XenoHuntBehaviorConfig xenoHuntBehaviorConfig = new();
  }
}