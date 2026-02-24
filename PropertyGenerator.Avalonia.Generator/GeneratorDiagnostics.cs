using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace PropertyGenerator.Avalonia.Generator;

internal static class GeneratorDiagnostics
{
    private const string Category = "PropertyGenerator.Avalonia";

    public static readonly DiagnosticDescriptor InvalidPropertyDeclaration = new(
        id: "PGA1001",
        title: "Invalid generated property declaration",
        messageFormat: "Property '{0}' is not valid for {1}: {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMustInheritAvaloniaObject = new(
        id: "PGA1002",
        title: "Containing type must inherit AvaloniaObject",
        messageFormat: "Type '{0}' must inherit from 'Avalonia.AvaloniaObject' to use {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReferencedMethodNotFound = new(
        id: "PGA1003",
        title: "Referenced method not found",
        messageFormat: "Could not find method '{0}' for '{1}' on type '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReferencedMethodHasInvalidSignature = new(
        id: "PGA1004",
        title: "Referenced method has invalid signature",
        messageFormat: "Method '{0}' for '{1}' has an invalid signature, expected: {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidDirectAccessorConfiguration = new(
        id: "PGA1005",
        title: "Invalid direct property accessor configuration",
        messageFormat: "Invalid direct property accessor configuration for '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAttachedPropertyName = new(
        id: "PGA1006",
        title: "Invalid attached property name",
        messageFormat: "Attached property name '{0}' is invalid, use a non-empty valid C# identifier",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    //public static readonly DiagnosticDescriptor InvalidAttachedHostType = new(
    //    id: "PGA1007",
    //    title: "Invalid attached host type",
    //    messageFormat: "Attached property '{0}' has host type '{1}', which must inherit from 'Avalonia.AvaloniaObject'",
    //    category: Category,
    //    defaultSeverity: DiagnosticSeverity.Error,
    //    isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "PGA1007",
        title: "Containing type must be partial",
        messageFormat: "Type '{0}' must be partial to generate members for '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateAttachedPropertyName = new(
        id: "PGA1008",
        title: "Duplicate attached property name",
        messageFormat: "Attached property '{0}' is declared multiple times on '{1}', and only the first valid declaration is generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenerateOnPropertyChangedTargetNotFound = new(
        id: "PGA1009",
        title: "GenerateOnPropertyChanged target property not found",
        messageFormat: "Could not find property '{0}' on '{1}' or its base types for GenerateOnPropertyChanged",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenerateOnPropertyChangedDisabled = new(
        id: "PGA1010",
        title: "GenerateOnPropertyChanged disabled",
        messageFormat: "GenerateOnPropertyChanged on '{0}' is ignored because DoNotGenerateOnPropertyChanged is applied",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
