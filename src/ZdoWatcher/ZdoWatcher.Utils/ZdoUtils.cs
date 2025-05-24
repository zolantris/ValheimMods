namespace ZdoWatcher.ZdoWatcher.Utils;

public static class ZdoUtils
{
  /// <summary>
  /// This is a one-way conversion. It is not necessary to reverse this. Consider using a Dictionary if you need to match this output + retrieve it later during same game session, Otherwise compare ZDOID to ZDOID.
  /// </summary>
  /// <param name="zdoid"></param>
  /// <returns></returns>
  public static int ZdoIdToId(ZDOID zdoid)
  {
    return (int)zdoid.UserID + (int)zdoid.ID;
  }
}