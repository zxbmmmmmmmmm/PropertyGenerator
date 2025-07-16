using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                transform: (ctx, _) => (IPropertySymbol)ctx.TargetSymbol)
            .Where(symbol => symbol is not null)!;

        var compilationAndProperties = context.CompilationProvider.Combine(propertySymbols.Collect());

        context.RegisterSourceOutput(compilationAndProperties, (spc, source) =>
        {
            var compilation = source.Left;
            var properties = source.Right;
            
            if (properties.IsDefaultOrEmpty) return;

            foreach (var group in properties.GroupBy<IPropertySymbol, INamedTypeSymbol>(p => p.ContainingType, SymbolEqualityComparer.Default))
            {
                var containingClass = group.Key;
                if (containingClass is null || !InheritsFrom(containingClass, "Avalonia.StyledElement"))
                {
                    continue;
                }
                
                var sourceCode = GenerateClassSource(compilation, containingClass, group.ToList());
                spc.AddSource($"{containingClass.ContainingNamespace.ToDisplayString()}.{containingClass.Name}.g.cs", 
                    SourceText.From(sourceCode, Encoding.UTF8));
            }
        });
    }
    
    
    private static string GenerateClassSource(Compilation compilation, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
    {
        var complicationUnit = CompilationUnit()
            .AddMembers(GenerateContent(compilation, classSymbol, properties))
            //.AddMembers(GeneratePropertyChangedCallbacks(compilation, classSymbol, properties))
            .WithLeadingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)));

        return SyntaxTree(complicationUnit.NormalizeWhitespace()).ToString();
    }

    private static NamespaceDeclarationSyntax GenerateContent(Compilation compilation, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var classDeclaration = ClassDeclaration(className).AddModifiers(Token(SyntaxKind.PartialKeyword));
        foreach (var prop in properties)
        {
            classDeclaration = classDeclaration.AddMembers(GenerateStyledProperty(compilation, classSymbol, prop));
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

    private static FieldDeclarationSyntax GenerateStyledProperty(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var styledPropertySymbolName = compilation.GetTypeByMetadataName("Avalonia.StyledProperty`1")!
            .Construct([propertySymbol.Type], [propertySymbol.NullableAnnotation])
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var avaloniaPropertySymbolName = compilation.GetTypeByMetadataName("Avalonia.AvaloniaProperty")!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var attribute = propertySymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass!.MetadataName == AttributeFullName)!;
            
        object? defaultValue = null;

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Value.Value is { } value)
            {
                switch (namedArgument.Key)
                {
                    case "DefaultValue":
                        defaultValue = value;
                        break;
                }
            }
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
                            .AddArgumentListArguments(
                                Argument(NameOfExpression(propertyName)),
                                Argument(PostfixUnaryExpression(
                                        SyntaxKind.SuppressNullableWarningExpression,
                                        LiteralExpression(SyntaxKind.DefaultLiteralExpression)))
                                    .WithNameColon(NameColon(IdentifierName("defaultValue")))))))))
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