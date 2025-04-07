// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ValheimVehicles.SharedScripts.Validation
{

  public static class StaticFieldValidator
  {
    private const string ModIssuesPage = "https://github.com/zolantris/ValheimMods/issues";
    /// <summary>
    /// Validates public static fields and optionally instance fields.
    /// </summary>
    public static void ValidateRequiredNonNullFields(Type type, object? instance = null, string? context = null)
    {
      context ??= type.Name;

      if (instance != null)
      {
        ValidateInstanceFields(instance, context);
      }

      ValidateStaticFields(type, context);
    }

    /// <summary>
    /// Generic overload for type-based validation.
    /// </summary>
    public static void ValidateRequiredNonNullFields<T>(object? instance = null, string? context = null)
    {
      ValidateRequiredNonNullFields(typeof(T), instance, context);
    }

    /// <summary>
    /// Instance-based validation with an option to skip static field validation.
    /// </summary>
    public static void ValidateRequiredNonNullFields(object instance, string? context = null, bool skipStatic = false)
    {
      if (instance == null) throw new ArgumentNullException(nameof(instance));

      var type = instance.GetType();
      context ??= type.Name;

      ValidateInstanceFields(instance, context);

      if (!skipStatic)
      {
        ValidateStaticFields(type, context);
      }
    }

    private static void ValidateStaticFields(Type type, string context)
    {
      var staticFields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

      foreach (var field in staticFields)
      {
        if (ShouldSkip(field)) continue;

        var value = field.GetValue(null);
        if (value == null)
        {
          LoggerProvider.LogError($"[{context}] Static field '{field.Name}' is null. This can cause a null reference exception in code. Report this error to ");
        }
      }
    }

    private static void ValidateInstanceFields(object instance, string context)
    {
      var type = instance.GetType();
      var instanceFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      foreach (var field in instanceFields)
      {
        if (ShouldSkip(field)) continue;

        if (field.IsDefined(typeof(CompilerGeneratedAttribute), false)) continue;

        var value = field.GetValue(instance);
        if (value == null)
        {
          LoggerProvider.LogError($"[{context}] Instance field '{field.Name}' is null. Report this issue to {ModIssuesPage}");
        }
      }
    }

    private static bool ShouldSkip(FieldInfo field)
    {
      return field.IsDefined(typeof(SkipValidationAttribute), false);
    }
  }
}