using System;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DoNotGenerateOnPropertyChangedAttribute : Attribute
{
    
}