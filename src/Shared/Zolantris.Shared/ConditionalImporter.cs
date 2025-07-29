using System;

namespace Zolantris.Shared.ModIntegrations;

public interface IModIntegrationApi
{
  public void RunIntegration();
}

public static class ConditionalImporter
{
  public static bool ImportConditionally(string modGuid, string targetClass)
  {
    var _parentType = Type.GetType("Namespace.ParentClassName");

    if (_parentType != null)
    {
      // Parent class exists, you can safely use it
      LoggerProvider.LogDebug(
        $"Conditional {modGuid} found. {targetClass} will now run");
      var conditionalClass =
        Activator.CreateInstance(_parentType) as IModIntegrationApi;
      conditionalClass?.RunIntegration();
      return true;
    }

    else
    {
      LoggerProvider.LogDebug(
        $"Conditional {modGuid} not found. Exiting");
      return false;
    }
  }
}

// private void ImportClass()
// {
//   // Assuming ParentClass is in the same namespace, you can instantiate or use it here
//   var parentInstance = Activator.CreateInstance(_parentType);
//   // Perform operations with parentInstance
// }
//
// }