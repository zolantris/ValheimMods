using System;
using Jotunn;
using UnityEngine;

namespace ValheimRAFT.MoveableBaseRootComponent;

public class Delegate : MoveBaseRoot
{
  private Client m_client;
  private Server m_server;
  public MonoBehaviour Instance { get; set; }

  /**
   * @todo confirm that Awake will have ZNet.instance available
   */
  public void Awake()
  {
    if (!ZNet.instance)
    {
      ZLog.LogError(
        "Critical ERROR, ZNet.instance not available on Awake in ValheimRAFT.MoveableBaseRootComponent.Delegate");
    }

    if (ZNet.instance.IsClientInstance())
    {
      m_client = new Client();
      Instance = m_client;
    }

    if (ZNet.IsSinglePlayer || ZNet.instance.IsServerInstance())
    {
      m_server = new Server();
      Instance = m_server;
    }
  }
}