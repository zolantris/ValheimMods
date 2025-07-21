using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace ModSync.Utils;

using System.Text.RegularExpressions;

public static class VariableResolver
{
  private static readonly Regex VarPattern = new(@"\$\{(?<name>[a-zA-Z0-9_]+)\}");

  public static void RecursivelyResolveObject(object obj, IReadOnlyDictionary<string, string> resolvedVars, Regex ignoredKeysRegex)
  {
    if (obj == null) return;

    var type = obj.GetType();

    // If string, resolve variables
    if (type == typeof(string))
      return; // Strings as properties are handled below

    // If it's a dictionary, recurse on values
    if (typeof(IDictionary).IsAssignableFrom(type))
    {
      var dict = (IDictionary)obj;
      foreach (var key in dict.Keys)
      {
        var value = dict[key];
        if (value is string s)
          dict[key] = InterpolateVars(s, resolvedVars);
        else
          RecursivelyResolveObject(value, resolvedVars, ignoredKeysRegex);
      }
      return;
    }

    // If it's a list/array, recurse on items
    if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
    {
      var list = (IEnumerable)obj;
      foreach (var item in list)
        RecursivelyResolveObject(item, resolvedVars, ignoredKeysRegex);
      return;
    }

    // For normal objects, iterate fields/properties
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
    {
      if (ignoredKeysRegex.IsMatch(field.Name)) continue;
      if (field.FieldType == typeof(string))
      {
        var val = (string)field.GetValue(obj);
        if (val != null)
          field.SetValue(obj, InterpolateVars(val, resolvedVars));
      }
      else if (!field.FieldType.IsValueType)
      {
        var val = field.GetValue(obj);
        if (val != null)
          RecursivelyResolveObject(val, resolvedVars, ignoredKeysRegex);
      }
    }

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (ignoredKeysRegex.IsMatch(prop.Name)) continue;
      if (!prop.CanRead || !prop.CanWrite) continue;

      if (prop.PropertyType == typeof(string))
      {
        var val = (string)prop.GetValue(obj);
        if (val != null)
          prop.SetValue(obj, InterpolateVars(val, resolvedVars));
      }
      else if (!prop.PropertyType.IsValueType)
      {
        var val = prop.GetValue(obj);
        if (val != null)
          RecursivelyResolveObject(val, resolvedVars, ignoredKeysRegex);
      }
    }
  }

  public static string InterpolateVars(string input, IReadOnlyDictionary<string, string> vars)
  {
    if (string.IsNullOrEmpty(input)) return input;
    // Use a fast approach; can use Regex if you want more robust handling.
    int start;
    while ((start = input.IndexOf("${")) != -1)
    {
      var end = input.IndexOf('}', start + 2);
      if (end == -1) break;
      var varName = input.Substring(start + 2, end - (start + 2));
      if (vars.TryGetValue(varName, out var value))
        input = input.Substring(0, start) + value + input.Substring(end + 1);
      else
        input = input.Substring(0, start) + input.Substring(end + 1); // Remove unknown vars
    }
    return input;
  }

  /// <summary>
  /// Resolves all variables, recursively, into a new dictionary.
  /// Throws on circular refs.
  /// </summary>
  public static Dictionary<string, string> ResolveAll(Dictionary<string, string> variables)
  {
    var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var kv in variables)
    {
      resolved[kv.Key] = ResolveSingle(kv.Key, variables, resolved, new HashSet<string>());
    }
    return resolved;
  }

  /// <summary>
  /// Resolves a single variable, replacing any ${var} references.
  /// </summary>
  private static string ResolveSingle(
    string key,
    Dictionary<string, string> variables,
    Dictionary<string, string> resolved,
    HashSet<string> stack)
  {
    if (resolved.TryGetValue(key, out var already)) return already;
    if (!variables.TryGetValue(key, out var value))
    {
      throw new InvalidOperationException($"Variable not found in variables: key: <{key}>. Exiting to prevent creating folder in wrong place.");
    }

    if (!stack.Add(key))
      throw new InvalidOperationException($"Circular variable reference detected: {string.Join(" -> ", stack)} -> {key}");

    var result = VarPattern.Replace(value, match =>
    {
      var varName = match.Groups["name"].Value;
      // recurse
      return ResolveSingle(varName, variables, resolved, stack);
    });

    stack.Remove(key);
    resolved[key] = result;
    return result;
  }

  /// <summary>
  /// Given a string, replaces any ${var} with the resolved variable.
  /// </summary>
  public static string Interpolate(string value, IReadOnlyDictionary<string, string> resolvedVars)
  {
    if (string.IsNullOrEmpty(value)) return value;
    return VarPattern.Replace(value, match =>
    {
      var varName = match.Groups["name"].Value;
      return resolvedVars.TryGetValue(varName, out var resolved) ? resolved : match.Value;
    });
  }
}