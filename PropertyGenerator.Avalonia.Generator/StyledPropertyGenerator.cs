using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using PropertyGenerator.Avalonia.Generator.Extensions;
using PropertyGenerator.Avalonia.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PropertyGenerator.Avalonia.Generator;

[Generator]
public class StyledPropertyGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "PropertyGenerator.Avalonia.GeneratedStyledPropertyAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
        var propertySymbols = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: (node, _) => node is PropertyDeclarationSyntax pds && pds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: (ctx, _) => ctx)
            .Where(ctx => (ctx.TargetSymbol as IPropertySymbol) is not null)!;

        var compilationAndProperties = context.CompilationProvider.Combine(propertySymbols.Collect());

        context.RegisterSourceOutput(compilationAndProperties, (spc, source) =>
        {
            var compilation = source.Left;
            var ctx = source.Right;
            var properties = ctx.Select(p => p.TargetSymbol as IPropertySymbol)
                .Where(p => p is not null)
                .Cast<IPropertySymbol>()
                .Where(p => p.ContainingType is not null)
                .ToImmutableArray();

            if (properties.IsDefaultOrEmpty) return;
            var model = ctx.First().SemanticModel;

            foreach (var group in properties.GroupBy<IPropertySymbol, INamedTypeSymbol>(p => p.ContainingType, SymbolEqualityComparer.Default))
            {
                var containingClass = group.Key;
                if (containingClass is null || !InheritsFrom(containingClass, "Avalonia.StyledElement"))
                {
                    continue;
                }

                var sourceCode = GenerateClassSource(compilation, containingClass, group.ToList(),model);
                spc.AddSource($"{containingClass.ContainingNamespace.ToDisplayString()}.{containingClass.Name}.g.cs", 
                    SourceText.From(sourceCode, Encoding.UTF8));
            }
        });
    }
    
    
    private static string GenerateClassSource(Compilation compilation, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, SemanticModel model)
    {
        var complicationUnit = CompilationUnit()
            .AddMembers(GenerateContent(compilation, classSymbol, properties, model))
            //.AddMembers(GeneratePropertyChangedCallbacks(compilation, classSymbol, properties))
            .WithLeadingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)));

        return SyntaxTree(complicationUnit.NormalizeWhitespace()).ToString();
    }

    private static NamespaceDeclarationSyntax GenerateContent(Compilation compilation, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, SemanticModel model)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var classDeclaration = ClassDeclaration(className).AddModifiers(Token(SyntaxKind.PartialKeyword));
        foreach (var prop in properties)
        {
            classDeclaration = classDeclaration.AddMembers(GenerateStyledProperty(compilation, classSymbol, prop, model));
            classDeclaration = classDeclaration.AddMembers(GenerateField(compilation, classSymbol, prop));
            classDeclaration = classDeclaration.AddMembers(GenerateGetMethod(compilation, classSymbol, prop));
            classDeclaration = classDeclaration.AddMembers(GenerateSetMethod(compilation, classSymbol, prop));
            classDeclaration = classDeclaration.AddMembers(GenerateChangedMethod(compilation, classSymbol, prop));
        }

        var namespaceDeclarationSyntax = NamespaceDeclaration(ParseName(namespaceName)).AddMembers(classDeclaration);
        return namespaceDeclarationSyntax;
    }

    private static NamespaceDeclarationSyntax GeneratePropertyChangedCallbacks(Compilation compilation, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var callbacksClassDeclaration = ClassDeclaration("PropertyChangedCallbacks").AddModifiers(Token(SyntaxKind.FileKeyword),Token(SyntaxKind.SealedKeyword));
        callbacksClassDeclaration = callbacksClassDeclaration
            .AddMembers(
                FieldDeclaration(
                        VariableDeclaration(IdentifierName("PropertyChangedCallbacks"))
                            .AddVariables(
                                VariableDeclarator(Identifier("Instance")).WithInitializer(EqualsValueClause(ImplicitObjectCreationExpression()))))
                    .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)));

        foreach (var property in properties)
        {
            callbacksClassDeclaration = callbacksClassDeclaration
                .AddMembers(GeneratePropertyChangedMethod(property));
        }

        //var unsafeAccessorsDeclaration = 

        var namespaceDeclarationSyntax = NamespaceDeclaration(IdentifierName("PropertyGenerator.Avalonia.Generator"))
            .AddUsings(UsingDirective(IdentifierName("global::System.Runtime.CompilerServices")))
            .AddUsings(UsingDirective(IdentifierName("global::Avalonia")))
            .AddMembers(callbacksClassDeclaration);
        return namespaceDeclarationSyntax;
    }

    private static MethodDeclarationSyntax GeneratePropertyChangedMethod(IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var methodDeclaration = MethodDeclaration(IdentifierName("PropertyChangedCallback"), Identifier(propertyName))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword),Token(SyntaxKind.StaticKeyword),Token(SyntaxKind.ReadOnlyKeyword))
            .AddBodyStatements(
            ReturnStatement(
                ImplicitObjectCreationExpression()
                .AddArgumentListArguments(
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Instance"),
                            IdentifierName($"On{propertyName}Changed"))))));
        return methodDeclaration;
    }

    private static FieldDeclarationSyntax GenerateStyledProperty(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol, SemanticModel model)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var styledPropertySymbolName = compilation.GetTypeByMetadataName("Avalonia.StyledProperty`1")!
            .Construct([propertySymbol.Type], [propertySymbol.NullableAnnotation])
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var avaloniaPropertySymbolName = compilation.GetTypeByMetadataName("Avalonia.AvaloniaProperty")!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var attribute = propertySymbol.GetAttributes().FirstOrDefault(p
            =>
                p.AttributeClass!.ToDisplayString() == AttributeFullName
        )!;
        bool? inherits = null;
        string? bindingMode = null;
        string? validate = null;
        string? coerce = null;
        bool? enableDataValidation = null;
        foreach (var namedArgument in attribute.NamedArguments)
        {
            var value = namedArgument.Value.Value;
            switch (namedArgument.Key)
            {
                case "Inherits":
                    inherits = (bool?)value;
                    break;
                case "DefaultBindingMode":
                    bindingMode = (string?)value;
                    break;
                case "Validate":
                    validate = (string?)value;
                    break;
                case "Coerce":
                    coerce = (string?)value;
                    break;
                case "EnableDataValidation":
                    enableDataValidation = (bool?)value;
                    break;
            }
        }
        var defaultValue = GetDefaultValue(
            attribute,
            propertySymbol,
            classSymbol,
            model,
            CancellationToken.None);

        List<ArgumentSyntax> arguments =
        [
            Argument(NameOfExpression(propertyName))
                .WithNameColon(NameColon(IdentifierName("name")))
        ];

        if (defaultValue is not AvaloniaPropertyDefaultValue.UnsetValue)
        {
            arguments.Add(Argument(IdentifierName(defaultValue.ToString()))
                .WithNameColon(NameColon(IdentifierName("defaultValue"))));
        }

        if (validate is not null)
        {
            arguments.Add(Argument(IdentifierName(validate))
                .WithNameColon(NameColon(IdentifierName("validate"))));
        }

        if (coerce is not null)
        {
            arguments.Add(Argument(IdentifierName(coerce))
                .WithNameColon(NameColon(IdentifierName("coerce"))));
        }

        if (enableDataValidation is not null)
        {
            arguments.Add(Argument(IdentifierName(enableDataValidation.ToString().ToLower()))
                .WithNameColon(NameColon(IdentifierName("enableDataValidation"))));
        }

        if (inherits is not null)
        {
            arguments.Add(Argument(IdentifierName(inherits.ToString().ToLower()))
                .WithNameColon(NameColon(IdentifierName("inherits"))));
        }

        var fieldDeclaration = FieldDeclaration( //StyledProperty
                VariableDeclaration(
                        IdentifierName(styledPropertySymbolName))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier($"{propertyName}Property"))
                        .WithInitializer(EqualsValueClause(InvocationExpression(MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(avaloniaPropertySymbolName),
                                GenericName(Identifier("Register"))
                                    .AddTypeArgumentListArguments(
                                        IdentifierName(className),
                                        ParseTypeName(propertyType))))
                            .AddArgumentListArguments([..arguments]))))))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(GeneratedCodeAttribute())));
                

        return fieldDeclaration;
    }

    private static PropertyDeclarationSyntax GenerateField(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var propertyDeclaration =
            PropertyDeclaration(ParseTypeName(propertyType), Identifier(propertyName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .AddBodyStatements(
                            LocalDeclarationStatement(
                                VariableDeclaration(
                                        IdentifierName(propertyType))
                                    .AddVariables(
                                        VariableDeclarator(Identifier("__value"))
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    InvocationExpression(IdentifierName("GetValue"))
                                                        .AddArgumentListArguments(Argument(IdentifierName($"{propertyName}Property"))))))))
                        .AddBodyStatements(
                            ExpressionStatement(InvocationExpression(IdentifierName($"On{propertyName}Get"))
                                .AddArgumentListArguments(
                                Argument(
                                    IdentifierName($"__value")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))
                        .AddBodyStatements(
                            ReturnStatement(IdentifierName("__value"))),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .AddBodyStatements(
                            ExpressionStatement(InvocationExpression(IdentifierName($"On{propertyName}Set"))
                                .AddArgumentListArguments(
                                    Argument(
                                        IdentifierName($"value")).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)))))
                        .AddBodyStatements(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName("SetValue"))
                                    .AddArgumentListArguments(
                                        Argument(IdentifierName($"{propertyName}Property")),
                                        Argument(IdentifierName("value")))))
                        .AddBodyStatements(
                            ExpressionStatement(
                                InvocationExpression(IdentifierName($"On{propertyName}Changed"))
                                    .AddArgumentListArguments(
                                        Argument(IdentifierName("value"))))))
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(GeneratedCodeAttribute())),
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(IdentifierName("global::System.Diagnostics.DebuggerNonUserCode")))),
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(IdentifierName("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage")))));

        return propertyDeclaration;
    }
    
    private static MethodDeclarationSyntax GenerateGetMethod(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;

        var onGet = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"On{propertyName}Get"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("propertyValue"))
                    .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                    .WithType(IdentifierName(propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(GeneratedCodeAttribute())));
        return onGet;
    }

    private static MethodDeclarationSyntax GenerateSetMethod(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;

        var onSet = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"On{propertyName}Set"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("propertyValue"))
                    .WithModifiers(TokenList(Token(SyntaxKind.RefKeyword)))
                    .WithType(IdentifierName(propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(GeneratedCodeAttribute())));
        return onSet;
    }

    private static MethodDeclarationSyntax GenerateChangedMethod(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;

        var onSet = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"On{propertyName}Changed"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("newValue"))
                    .WithType(IdentifierName(propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(GeneratedCodeAttribute())));
        return onSet;
    }

    private static AttributeSyntax GeneratedCodeAttribute()
    {
        return Attribute(IdentifierName("global::System.CodeDom.Compiler.GeneratedCode"))
            .AddArgumentListArguments(
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("PropertyGenerator.Avalonia.Generator"))),
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("1.0.0.0"))));
    }

    private static AvaloniaPropertyDefaultValue GetDefaultValue(
    AttributeData attributeData,
    IPropertySymbol propertySymbol,
    ITypeSymbol? metadataTypeSymbol,
    SemanticModel semanticModel,
    CancellationToken token)
    {
        // First, check if we have a callback
        if (attributeData.TryGetNamedArgument("DefaultValueCallback", out TypedConstant defaultValueCallback))
        {
            // This must be a valid 'string' value
            if (defaultValueCallback is { Type.SpecialType: SpecialType.System_String, Value: string { Length: > 0 } methodName })
            {
                // Check that we can find a potential candidate callback method
                if (TryFindDefaultValueCallbackMethod(propertySymbol, methodName, out IMethodSymbol? methodSymbol))
                {
                    // Validate the method has a valid signature as well
                    if (IsDefaultValueCallbackValid(propertySymbol, methodSymbol))
                    {
                        return new AvaloniaPropertyDefaultValue.Callback(methodName);
                    }
                }
            }

            // Invalid callback, the analyzer will emit an error
            return AvaloniaPropertyDefaultValue.Null.Instance;
        }

        token.ThrowIfCancellationRequested();

        // Next, check whether the default value is explicitly set or not
        if (attributeData.TryGetNamedArgument("DefaultValue", out TypedConstant defaultValue))
        {
            // If the explicit value is anything other than 'null', we can return it directly
            if (!defaultValue.IsNull)
            {
                return new AvaloniaPropertyDefaultValue.Constant(TypedConstantInfo.Create(defaultValue));
            }

            // If we do have a default value, we also want to check whether it's the special 'UnsetValue' placeholder.
            // To do so, we get the application syntax, find the argument, then get the operation and inspect it.
            if (attributeData.ApplicationSyntaxReference?.GetSyntax(token) is AttributeSyntax attributeSyntax)
            {
                foreach (AttributeArgumentSyntax attributeArgumentSyntax in attributeSyntax.ArgumentList?.Arguments ?? [])
                {
                    // Let's see whether the current argument is the one that set the 'DefaultValue' property
                    if (attributeArgumentSyntax.NameEquals?.Name.Identifier.Text is "DefaultValue")
                    {
                        IOperation? operation = semanticModel.GetOperation(attributeArgumentSyntax.Expression, token);

                        // Double check that it's a constant field reference (it could also be a literal of some kind, etc.)
                        if (operation is IFieldReferenceOperation { Field: { Name: "UnsetValue" } fieldSymbol })
                        {
                            // Last step: we want to validate that the reference is actually to the special placeholder
                            //if (fieldSymbol.ContainingType!.HasFullyQualifiedMetadataName(WellKnownTypeNames.GeneratedDependencyProperty))
                            //{
                                return new AvaloniaPropertyDefaultValue.UnsetValue();
                            //}
                        }
                    }
                }
            }

            // Otherwise, the value has been explicitly set to 'null', so let's respect that
            return AvaloniaPropertyDefaultValue.Null.Instance;
        }

        token.ThrowIfCancellationRequested();

        // In all other cases, we'll automatically use the default value of the type in question.
        // First we need to special case non nullable values, as for those we need 'default'.
        if (!propertySymbol.Type.IsDefaultValueNull())
        {
            // We need special logic to handle cases where the metadata type is different. For instance,
            // the XAML initialization won't work if the metadata type on a property is just 'object'.
            ITypeSymbol effectiveMetadataTypeSymbol = metadataTypeSymbol ?? propertySymbol.Type;

            // For non nullable types, we return 'default(T)', unless we can optimize for projected types
            return new AvaloniaPropertyDefaultValue.Default(
                TypeName: propertySymbol.Type.GetFullyQualifiedName());
        }

        // For all other ones, we can just use the 'null' placeholder again
        return AvaloniaPropertyDefaultValue.Null.Instance;
    }



    /// <summary>
    /// Tries to find a candidate default value callback method for a given property.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/> currently being targeted by the analyzer.</param>
    /// <param name="methodName">The name of the default value callback method to look for.</param>
    /// <param name="methodSymbol">The <see cref="IMethodSymbol"/> for the resulting default value callback candidate method, if found.</param>
    /// <returns>Whether <paramref name="methodSymbol"/> could be found.</returns>
    public static bool TryFindDefaultValueCallbackMethod(IPropertySymbol propertySymbol, string methodName, [NotNullWhen(true)] out IMethodSymbol? methodSymbol)
    {
        ImmutableArray<ISymbol> memberSymbols = propertySymbol.ContainingType!.GetMembers(methodName);

        foreach (ISymbol member in memberSymbols)
        {
            // Ignore all other member types
            if (member is not IMethodSymbol candidateSymbol)
            {
                continue;
            }

            // Match the exact method name too
            if (candidateSymbol.Name == methodName)
            {
                methodSymbol = candidateSymbol;

                return true;
            }
        }

        methodSymbol = null;

        return false;
    }

    /// <summary>
    /// Checks whether a given default value callback method is valid for a given property.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/> currently being targeted by the analyzer.</param>
    /// <param name="methodSymbol">The <see cref="IMethodSymbol"/> for the candidate default value callback method to validate.</param>
    /// <returns>Whether <paramref name="methodSymbol"/> is a valid default value callback method for <paramref name="propertySymbol"/>.</returns>
    public static bool IsDefaultValueCallbackValid(IPropertySymbol propertySymbol, IMethodSymbol methodSymbol)
    {
        // We need methods which are static and with no parameters (and that are not explicitly implemented)
        if (methodSymbol is not { IsStatic: true, Parameters: [], ExplicitInterfaceImplementations: [] })
        {
            return false;
        }

        // We have a candidate, now we need to match the return type. First,
        // we just check whether the return is 'object', or an exact match.
        if (methodSymbol.ReturnType.SpecialType is SpecialType.System_Object ||
            SymbolEqualityComparer.Default.Equals(propertySymbol.Type, methodSymbol.ReturnType))
        {
            return true;
        }

        // Otherwise, try to see if the return is the type argument of a nullable value type
        return propertySymbol.Type.IsNullableValueTypeWithUnderlyingType(methodSymbol.ReturnType);
    }


    private static bool InheritsFrom(INamedTypeSymbol? classSymbol, string baseTypeName)
    {
        var current = classSymbol;
        while (current is not null)
        {
            if (current.BaseType?.ToDisplayString() == baseTypeName)
                return true;
            if (current.ToDisplayString() == baseTypeName)
                return true;
            current = current.BaseType;
        }
        return false;
    }
    
    /// <summary>
    /// Generate the following code
    /// <code>
    /// nameof(<paramref name="name" />)
    /// </code>
    /// </summary>
    /// <returns>NameOfExpression</returns>
    internal static InvocationExpressionSyntax NameOfExpression(string name) => NameOfExpression(IdentifierName(name));

    /// <summary>
    /// Generate the following code
    /// <code>
    /// nameof(<paramref name="expressionSyntax" />)
    /// </code>
    /// </summary>
    /// <returns>NameOfExpression</returns>
    internal static InvocationExpressionSyntax NameOfExpression(ExpressionSyntax expressionSyntax) => InvocationExpression(IdentifierName("nameof"), ArgumentList().AddArguments(Argument(expressionSyntax)));
}