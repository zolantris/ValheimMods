using System;
using Jotunn;
using UnityEngine;

namespace ValheimRAFT.MoveableBaseRootComponent;

public class Delegate : MonoBehaviour
{
  private Client m_client;
  private Server m_server;

  /**
   * instance is used as a passthrough, the client will always run a server sync command and the server will always notify the client when a method is fired from there
   */
  public MoveBaseRoot Instance { get; set; }


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
      m_client = gameObject.AddComponent<Client>();
      Instance = m_client;
      return;
    }

    m_server = gameObject.AddComponent<Server>();
    Instance = m_server;
  }
}