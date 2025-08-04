using Avalonia.Data;
using System;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedDirectPropertyAttribute : Attribute
{
    public object? DefaultValue { get; set; }

    public string? DefaultValueCallback { get; set; }

    public string? Getter { get; set; }

    public string? Setter { get; set; }

    public BindingMode DefaultBindingMode { get; set; } = BindingMode.OneWay;

    public string? Coerce { get; set; }

    public bool EnableDataValidation { get; set; }

    public GeneratedDirectPropertyAttribute(object? defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public GeneratedDirectPropertyAttribute()
    {
    }
}
