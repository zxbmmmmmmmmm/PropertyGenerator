using System;

namespace PropertyGenerator.Avalonia;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class DoNotGenerateOnPropertyChangedAttribute : Attribute
{
    
}
