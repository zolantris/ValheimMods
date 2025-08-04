using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Core
{
  public class ComponentAITest : MonoBehaviour
  {
    public void Update()
    {
      Hello();
    }
    public void Hello()
    {
      LoggerProvider.LogDebugDebounced("hello world");
    }
  }
}
