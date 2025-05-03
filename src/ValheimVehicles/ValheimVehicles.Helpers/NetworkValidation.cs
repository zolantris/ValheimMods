namespace ValheimVehicles.Helpers;

public static class NetworkValidation
{
  public static bool IsNetViewValid(ZNetView? netView, out ZNetView validNetView)
  {
    validNetView = netView;
    if (netView != null && netView.m_zdo != null && netView.IsValid())
    {
      return true;
    }
    return false;
  }

  public static bool IsCurrentGameHealthy()
  {
    if (ZNet.instance == null || ZNetScene.instance == null)
    {
      return false;
    }
    return true;
  }
}