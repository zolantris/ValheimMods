namespace ValheimVehicles.ModSupport;

public class ContainerTracker
{
  private static readonly HashSet<Container> _activeContainers = new();
  private static float lastSanitize = 0f;
  private static float sanitizeInterval = 5f;
  public static HashSet<Container> ActiveContainers => _activeContainers;
  
  public void AddContainer(Container container)
  {
    
  }
}