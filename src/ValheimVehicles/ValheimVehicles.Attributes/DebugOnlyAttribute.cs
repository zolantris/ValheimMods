using System;

namespace ValheimVehicles.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class DebugOnlyAttribute : Attribute
{
}