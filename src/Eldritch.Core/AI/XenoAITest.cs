using UnityEngine;
using Zolantris.Shared;

namespace Eldritch.Core
{
  public class XenoAITest : MonoBehaviour
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
