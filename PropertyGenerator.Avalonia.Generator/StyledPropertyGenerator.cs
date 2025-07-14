using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PropertyGenerator.Avalonia;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PropertyGenerator.Avalonia.Generator;

[Generator]
public class StyledPropertyGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "PropertyGenerator.Avalonia.GeneratedStyledPropertyAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

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
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var classDeclaration = ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword));

        foreach (var prop in properties)
        {
            classDeclaration = classDeclaration.AddMembers(GenerateStyledProperty(compilation, classSymbol, prop));
            classDeclaration = classDeclaration.AddMembers(GenerateField(compilation, classSymbol, prop));
        }

        var complicationUnit = CompilationUnit()
            .AddUsings(
                UsingDirective(
                    IdentifierName("Avalonia")))
            .AddMembers(
                NamespaceDeclaration(ParseName(namespaceName))
                .AddMembers(classDeclaration));

        return SyntaxTree(complicationUnit.NormalizeWhitespace()).ToString();
    }
    private static FieldDeclarationSyntax GenerateStyledProperty(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var styledPropertySymbol = compilation.GetTypeByMetadataName("Avalonia.StyledProperty`1")!;
        var avaloniaPropertySymbol = compilation.GetTypeByMetadataName("Avalonia.AvaloniaProperty")!;


        var fieldDeclaration = FieldDeclaration(//StyledProperty
            VariableDeclaration(
                ParseTypeName(
                    styledPropertySymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            .WithVariables(
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier("IsStartedProperty"))
                    .WithInitializer(
                        EqualsValueClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(avaloniaPropertySymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                                    GenericName(
                                        Identifier("Register"))
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SeparatedList<TypeSyntax>(
                                                [
                                                    IdentifierName(className),
                                                    Token(SyntaxKind.CommaToken),
                                                    PredefinedType(Token(SyntaxKind.BoolKeyword))
                                                ])))))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList<ArgumentSyntax>(
                                        [
                                            Argument(
                                                InvocationExpression(
                                                    IdentifierName(
                                                        Identifier(
                                                            TriviaList(),
                                                            SyntaxKind.NameOfKeyword,
                                                            "nameof",
                                                            "nameof",
                                                            TriviaList())))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SingletonSeparatedList(
                                                            Argument(
                                                                IdentifierName(propertyName)))))),
                                            Token(SyntaxKind.CommaToken),
                                            Argument(
                                                PostfixUnaryExpression(
                                                    SyntaxKind.SuppressNullableWarningExpression,
                                                    LiteralExpression(
                                                        SyntaxKind.DefaultLiteralExpression,
                                                        Token(SyntaxKind.DefaultKeyword))))
                                            .WithNameColon(
                                                NameColon(
                                                    IdentifierName("defaultValue")))]))))))));
        return fieldDeclaration;
    }

    private static PropertyDeclarationSyntax GenerateField(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var propertyDeclaration =
            PropertyDeclaration(
                PredefinedType(
                    Token(SyntaxKind.BoolKeyword)),
                Identifier("IsStarted"))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.PartialKeyword)))
            .WithAccessorList(
                AccessorList(
                    List(
                        [AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause(
                                    InvocationExpression(
                                        IdentifierName("GetValue"))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName($"{propertyName}Property")))))))
                            .WithSemicolonToken(
                                Token(SyntaxKind.SemicolonToken)),
                            AccessorDeclaration(
                                SyntaxKind.SetAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause(
                                    InvocationExpression(
                                        IdentifierName("SetValue"))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SeparatedList<ArgumentSyntax>(
                                                new SyntaxNodeOrToken[]{
                                                    Argument(
                                                        IdentifierName($"{propertyName}Property")),
                                                    Token(SyntaxKind.CommaToken),
                                                    Argument(
                                                        IdentifierName("value"))})))))
                            .WithSemicolonToken(
                                Token(SyntaxKind.SemicolonToken))])));
        return propertyDeclaration;
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
}