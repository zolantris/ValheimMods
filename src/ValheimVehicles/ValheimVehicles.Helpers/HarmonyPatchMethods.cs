using System;
using System.Reflection;

namespace ValheimVehicles.Helpers;

public class HarmonyPatchMethods
{
  public static bool HasMatchingParameterTypes(int genericParameterCount, Type[] types,
    ParameterInfo[] parameters)
  {
    if (parameters.Length < genericParameterCount || parameters.Length != types.Length)
    {
      return false;
    }

    var num = 0;
    for (var i = 0; i < parameters.Length; i++)
    {
      if (parameters[i].ParameterType.IsGenericParameter)
      {
        num++;
      }
      else if (types[i] != parameters[i].ParameterType)
      {
        return false;
      }
    }

    return num == genericParameterCount;
  }

  public static MethodInfo GetGenericMethod(Type type, string name, int genericParameterCount,
    Type[] types)
  {
    var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                  BindingFlags.Public | BindingFlags.NonPublic |
                                  BindingFlags.GetField | BindingFlags.SetField |
                                  BindingFlags.GetProperty | BindingFlags.SetProperty);
    foreach (var methodInfo in methods)
    {
      if (methodInfo.IsGenericMethod && methodInfo.ContainsGenericParameters &&
          methodInfo.Name == name &&
          HasMatchingParameterTypes(genericParameterCount, types, methodInfo.GetParameters()))
      {
        return methodInfo;
      }
    }

    return null;
  }
}