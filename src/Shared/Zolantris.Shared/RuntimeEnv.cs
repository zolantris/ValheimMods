using UnityEngine;
namespace Zolantris.Shared;

public static class RuntimeEnv
{
  public static bool IsHeadlessServer()
  {
    return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
  }
}