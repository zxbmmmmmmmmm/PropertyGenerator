using System;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedStyledPropertyAttribute : Attribute
{
    public object? DefaultValue { get; set; }
}

