// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ValheimVehicles.Helpers;

namespace ValheimVehicles.SharedScripts.Validation
{

  public static class ClassValidator
  {
    private const string ModIssuesPage = "https://github.com/zolantris/ValheimMods/issues";
    /// <summary>
    /// Validates public static fields and optionally instance fields.
    /// </summary>
    public static bool ValidateRequiredNonNullFields(Type type, object? instance = null, string? context = null, Regex? skipRegex = null, bool canLog = true)
    {
      context ??= type.Name;

      var isValid = true;
      if (instance != null)
      {
        isValid = ValidateInstanceFields(instance, context, skipRegex, canLog);
      }

      if (!ValidateStaticFields(type, context, skipRegex, canLog))
      {
        isValid = false;
      }

      return isValid;
    }

    /// <summary>
    /// Generic overload for type-based validation.
    /// </summary>
    public static bool ValidateRequiredNonNullFields<T>(object? instance = null, string? context = null, Regex? skipRegex = null, bool canLog = true)
    {
      var result = ValidateRequiredNonNullFields(typeof(T), instance, context, skipRegex, canLog);
      return result;
    }

    /// <summary>
    /// Instance-based validation with an option to skip static field validation.
    /// </summary>
    public static bool ValidateRequiredNonNullFields(object instance, string? context = null, bool skipStatic = true, Regex? skipRegex = null, bool canLog = true)
    {
      if (instance == null) throw new ArgumentNullException(nameof(instance));

      var type = instance.GetType();
      context ??= type.Name;

      var isValid = ValidateInstanceFields(instance, context, skipRegex, canLog);

      if (!skipStatic)
      {
        if (!ValidateStaticFields(type, context, skipRegex, canLog))
        {
          isValid = false;
        }
      }

      return isValid;
    }

    private static bool ValidateStaticFields(Type type, string context, Regex? skipRegex = null, bool canLog = true)
    {
      var staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

      var isValid = true;

      foreach (var field in staticFields)
      {
        if (ShouldSkip(field)) continue;

        var value = field.GetValue(null);
        if (skipRegex != null && skipRegex.IsMatch(field.Name)) continue;
        if (value == null)
        {
          if (canLog)
          {
            LoggerProvider.LogWarning($"[{context}] Static field '{field.Name}' is null. This can cause a null reference exception in code.");
          }
          isValid = false;
        }
      }

      return isValid;
    }

    private static bool ValidateInstanceFields(object instance, string context, Regex? skipRegex = null, bool canLog = true)
    {
      var type = instance.GetType();
      var instanceFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      var isValid = true;

      foreach (var field in instanceFields)
      {
        if (ShouldSkip(field)) continue;

        if (field.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;

        var value = field.GetValue(instance);
        if (skipRegex != null && skipRegex.IsMatch(field.Name)) continue;
        if (value == null)
        {
          if (canLog)
          {
            LoggerProvider.LogError($"[{context}] Instance field '{field.Name}' is null. Report this issue to {ModIssuesPage}");
          }
          isValid = false;
        }
      }

      return isValid;
    }

    private static bool ShouldSkip(FieldInfo field)
    {
      return field.IsDefined(typeof(SkipValidationAttribute), false);
    }
  }
}