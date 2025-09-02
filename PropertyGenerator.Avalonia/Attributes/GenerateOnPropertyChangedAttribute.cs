using System;
using Avalonia.Data;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class GenerateOnPropertyChangedAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; set; } = propertyName;
}
