// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle

using System;

namespace Zolantris.Shared
{
  [AttributeUsage(AttributeTargets.Field)]
  public sealed class SkipValidationAttribute : Attribute
  {
  }
}