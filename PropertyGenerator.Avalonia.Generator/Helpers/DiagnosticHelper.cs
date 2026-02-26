using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PropertyGenerator.Avalonia.Generator.Extensions;

namespace PropertyGenerator.Avalonia.Generator.Helpers;

internal static class DiagnosticHelper
{
    public static bool CheckContainingTypeIsPartial(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        string attributeName
    )
    {
        if (containingClass.DeclaringSyntaxReferences
            .Any(r => r.GetSyntax() is ClassDeclarationSyntax cls &&
                      cls.Modifiers.Any(SyntaxKind.PartialKeyword)))
        {
            return true;
        }

        spc.ReportDiagnostic(Diagnostic.Create(
            GeneratorDiagnostics.ContainingTypeMustBePartial,
            containingClass.Locations.FirstOrDefault(),
            containingClass.Name, attributeName));
        return false;
    }

    public static bool CheckInheritsAvaloniaObject(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        string attributeName
    )
    {
        if (containingClass.InheritsFromFullyQualifiedMetadataName("Avalonia.AvaloniaObject"))
        {
            return true;
        }

        spc.ReportDiagnostic(Diagnostic.Create(
            GeneratorDiagnostics.TypeMustInheritAvaloniaObject,
            containingClass.Locations.FirstOrDefault(),
            containingClass.Name, attributeName));
        return false;
    }

    public static bool CheckPropertyDeclaration(
        SourceProductionContext spc,
        IPropertySymbol property,
        string attributeName
    )
    {
        var propertySyntax = (PropertyDeclarationSyntax)property.DeclaringSyntaxReferences[0].GetSyntax();

        if (!propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidPropertyDeclaration,
                property.Locations.FirstOrDefault(),
                property.Name, attributeName, "property must be partial"));
            return false;
        }

        if (propertySyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidPropertyDeclaration,
                property.Locations.FirstOrDefault(),
                property.Name, attributeName, "property must not be static"));
            return false;
        }

        if (propertySyntax.Type.IsKind(SyntaxKind.RefType))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidPropertyDeclaration,
                property.Locations.FirstOrDefault(),
                property.Name, attributeName, "property must not be ref-returning"));
            return false;
        }

        return true;
    }

    public static bool CheckStyledPropertyMethodReferences(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        IPropertySymbol property,
        AttributeData attribute
    )
    {
        var hasError = false;

        if (attribute.TryGetNamedArgument<string>("Validate", out var validateName) && !string.IsNullOrEmpty(validateName))
        {
            if (!CheckMethodReference(spc, containingClass, property, validateName!,
                    "Validate",
                    m => m.Parameters.Length == 1 && m.ReturnType.SpecialType == SpecialType.System_Boolean,
                    $"static bool {validateName}({property.Type.ToDisplayString()} value)"))
            {
                hasError = true;
            }
        }

        if (attribute.TryGetNamedArgument<string>("Coerce", out var coerceName) && !string.IsNullOrEmpty(coerceName))
        {
            if (!CheckCoerceMethodReference(spc, containingClass, property, coerceName!))
            {
                hasError = true;
            }
        }

        return !hasError;
    }

    public static bool CheckDirectPropertyMethodReferences(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        IPropertySymbol property,
        AttributeData attribute
    )
    {
        var hasError = false;

        var hasGetter = attribute.TryGetNamedArgument<string>("Getter", out var getterName) && !string.IsNullOrEmpty(getterName);
        var hasSetter = attribute.TryGetNamedArgument<string>("Setter", out var setterName) && !string.IsNullOrEmpty(setterName);

        // PGA1005: Setter without Getter
        if (hasSetter && !hasGetter)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.InvalidDirectAccessorConfiguration,
                property.Locations.FirstOrDefault(),
                property.Name, "Setter cannot be specified without Getter"));
            hasError = true;
        }

        // Validate Getter callback
        if (hasGetter)
        {
            if (!CheckMethodReference(spc, containingClass, property, getterName!,
                    "Getter",
                    m => m.Parameters.Length == 1 && !m.ReturnsVoid,
                    $"static {property.Type.ToDisplayString()} {getterName}({containingClass.ToDisplayString()} owner)"))
            {
                hasError = true;
            }
        }

        // Validate Setter callback
        if (hasSetter && hasGetter)
        {
            if (!CheckMethodReference(spc, containingClass, property, setterName!,
                    "Setter",
                    m => m.Parameters.Length == 2 && m.ReturnsVoid,
                    $"static void {setterName}({containingClass.ToDisplayString()} owner, {property.Type.ToDisplayString()} value)"))
            {
                hasError = true;
            }
        }

        // Validate Coerce callback
        if (attribute.TryGetNamedArgument<string>("Coerce", out var coerceName) && !string.IsNullOrEmpty(coerceName))
        {
            if (!CheckCoerceMethodReference(spc, containingClass, property, coerceName!))
            {
                hasError = true;
            }
        }

        return !hasError;
    }

    private static bool CheckCoerceMethodReference(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        IPropertySymbol property,
        string coerceName
    )
    {
        return CheckMethodReference(spc, containingClass, property, coerceName,
            "Coerce",
            m => m.Parameters.Length == 2 && !m.ReturnsVoid,
            $"static {property.Type.ToDisplayString()} {coerceName}(IAvaloniaObject owner, {property.Type.ToDisplayString()} value)");
    }

    private static bool CheckMethodReference(
        SourceProductionContext spc,
        INamedTypeSymbol containingClass,
        IPropertySymbol property,
        string methodName,
        string parameterName,
        System.Func<IMethodSymbol, bool> signaturePredicate,
        string expectedSignature
    )
    {
        var methods = FindMethods(containingClass, methodName);
        if (methods.Count == 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.ReferencedMethodNotFound,
                property.Locations.FirstOrDefault(),
                methodName, parameterName, containingClass.Name));
            return false;
        }

        if (!methods.Any(signaturePredicate))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                GeneratorDiagnostics.ReferencedMethodHasInvalidSignature,
                property.Locations.FirstOrDefault(),
                methodName, parameterName, expectedSignature));
            return false;
        }

        return true;
    }

    public static List<IMethodSymbol> FindMethods(INamedTypeSymbol type, string name)
    {
        var methods = new List<IMethodSymbol>();
        for (var t = (ITypeSymbol?)type; t != null; t = t.BaseType)
        {
            foreach (var member in t.GetMembers(name))
            {
                if (member is IMethodSymbol method)
                    methods.Add(method);
            }
        }
        return methods;
    }
}
