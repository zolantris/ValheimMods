// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;

namespace ValheimVehicles.SharedScripts.Validation
{
  [AttributeUsage(AttributeTargets.Field)]
  public sealed class SkipValidationAttribute : Attribute
  {
  }
}