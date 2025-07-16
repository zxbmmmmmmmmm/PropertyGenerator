using System;
using Avalonia;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedStyledPropertyAttribute: Attribute
{
    public object? DefaultValue { get; set; }
    
    public bool Inherits { get ; set; }
    
    public BindingMode DefaultBindingMode { get ; set; } = BindingMode.OneWay;
    
    public string? Validate { get ; set;}
    
    public string? Coerce { get ; set; }
    
    public bool EnableDataValidation { get ; set; }
}

