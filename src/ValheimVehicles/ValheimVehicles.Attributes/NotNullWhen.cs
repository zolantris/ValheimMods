using System;

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
  public NotNullWhenAttribute(bool returnValue)
  {
    ReturnValue = returnValue;
  }
  public bool ReturnValue { get; }
}