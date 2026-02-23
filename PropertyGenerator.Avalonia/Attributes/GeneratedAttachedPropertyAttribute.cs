using System;
using Avalonia;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class GeneratedAttachedPropertyAttribute<THost, TValue> : Attribute
    where THost : AvaloniaObject
{
    public string Name { get; }

    public object? DefaultValue { get; set; }

    public string? DefaultValueCallback { get; set; }

    public bool Inherits { get; set; }

    public BindingMode DefaultBindingMode { get; set; } = BindingMode.OneWay;

    public string? Validate { get; set; }

    public string? Coerce { get; set; }

    public bool EnableDataValidation { get; set; }

    public GeneratedAttachedPropertyAttribute(string name)
    {
        Name = name;
    }
}
