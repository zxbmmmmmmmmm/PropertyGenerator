using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using PropertyGenerator.Avalonia.Generator.Extensions;
using PropertyGenerator.Avalonia.Generator.Models;

namespace PropertyGenerator.Avalonia.Generator.Helpers;

internal static class DefaultValueHelper
{
    public static AvaloniaPropertyDefaultValue GetDefaultValue(
        AttributeData attributeData,
        IPropertySymbol propertySymbol,
        SemanticModel semanticModel,
        CancellationToken token)
    {
        // First, check if we have a callback
        if (attributeData.TryGetNamedArgument("DefaultValueCallback", out var defaultValueCallback))
        {
            if (defaultValueCallback is { Type.SpecialType: SpecialType.System_String, Value: string { Length: > 0 } methodName })
            {
                if (TryFindDefaultValueCallbackMethod(propertySymbol, methodName, out var methodSymbol))
                {
                    if (IsDefaultValueCallbackValid(propertySymbol, methodSymbol))
                    {
                        return new AvaloniaPropertyDefaultValue.Callback(methodName);
                    }
                }
            }
            return AvaloniaPropertyDefaultValue.Null.Instance;
        }

        token.ThrowIfCancellationRequested();
        var hasDefaultValue = attributeData.TryGetConstructorArgument(0, out var defaultValue);
        if (!hasDefaultValue)
        {
            hasDefaultValue = attributeData.TryGetNamedArgument("DefaultValue", out defaultValue);
        }

        if (hasDefaultValue)
        {
            if (!defaultValue.IsNull)
            {
                return new AvaloniaPropertyDefaultValue.Constant(TypedConstantInfo.Create(defaultValue));
            }

            if (attributeData.ApplicationSyntaxReference?.GetSyntax(token) is AttributeSyntax attributeSyntax)
            {
                foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList?.Arguments ?? [])
                {
                    if (attributeArgumentSyntax.NameEquals?.Name.Identifier.Text is "DefaultValue")
                    {
                        var operation = semanticModel.GetOperation(attributeArgumentSyntax.Expression, token);
                        if (operation is IFieldReferenceOperation { Field: { Name: "UnsetValue" } })
                        {
                            return new AvaloniaPropertyDefaultValue.UnsetValue();
                        }
                    }
                }
            }
            return AvaloniaPropertyDefaultValue.Null.Instance;
        }

        token.ThrowIfCancellationRequested();

        if (!propertySymbol.Type.IsDefaultValueNull())
        {
            return new AvaloniaPropertyDefaultValue.Default(
                TypeName: propertySymbol.Type.GetFullyQualifiedName());
        }

        return AvaloniaPropertyDefaultValue.Null.Instance;
    }

    public static bool TryFindDefaultValueCallbackMethod(IPropertySymbol propertySymbol, string methodName, [NotNullWhen(true)] out IMethodSymbol? methodSymbol)
    {
        var memberSymbols = propertySymbol.ContainingType!.GetMembers(methodName);
        foreach (var member in memberSymbols)
        {
            if (member is IMethodSymbol candidateSymbol && candidateSymbol.Name == methodName)
            {
                methodSymbol = candidateSymbol;
                return true;
            }
        }
        methodSymbol = null;
        return false;
    }

    public static bool IsDefaultValueCallbackValid(IPropertySymbol propertySymbol, IMethodSymbol methodSymbol)
    {
        if (methodSymbol is not { IsStatic: true, Parameters: [], ExplicitInterfaceImplementations: [] })
        {
            return false;
        }

        if (methodSymbol.ReturnType.SpecialType is SpecialType.System_Object ||
            SymbolEqualityComparer.Default.Equals(propertySymbol.Type, methodSymbol.ReturnType))
        {
            return true;
        }

        return propertySymbol.Type.IsNullableValueTypeWithUnderlyingType(methodSymbol.ReturnType);
    }
}
