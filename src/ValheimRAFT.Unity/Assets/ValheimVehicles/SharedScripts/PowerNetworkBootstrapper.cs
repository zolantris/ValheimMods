// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle


namespace ValheimVehicles.SharedScripts.PowerSystem
{
  public class PowerNetworkBootstrapper : SingletonBehaviour<PowerNetworkBootstrapper>
  {
    private PowerNetworkController _controller;

    public override void OnAwake()
    {
      _controller = gameObject.AddComponent<PowerNetworkController>();
    }

    public static void Register(IPowerNode node)
    {
      if (Instance && Instance._controller != null)
      {
        Instance._controller.RegisterNode(node);
      }
    }
  }
}