namespace ValheimVehicles.Helpers;

public static class ZNetViewExtensions
{
  public static bool IsValid(ZNetView? netView, out ZNetView validNetView)
  {
    validNetView = netView;
    if (netView != null && netView.IsValid())
    {
      return true;
    }
    return false;
  }
}