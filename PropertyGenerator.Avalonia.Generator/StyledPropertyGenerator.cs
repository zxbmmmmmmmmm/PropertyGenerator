using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
            .AddUsings(UsingDirective(IdentifierName("Avalonia")))
            .AddMembers(NamespaceDeclaration(ParseName(namespaceName)).AddMembers(classDeclaration))
            .WithLeadingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)),
                Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)));

        return SyntaxTree(complicationUnit.NormalizeWhitespace()).ToString();
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
        var generatedCodeSymbolName = compilation.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCode")!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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
                                .WithNameColon(NameColon(IdentifierName("defaultValue")))))))));
        return fieldDeclaration;
    }

    private static PropertyDeclarationSyntax GenerateField(Compilation compilation, INamedTypeSymbol classSymbol, IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var className = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var propertyDeclaration =
            PropertyDeclaration(ParseTypeName(propertyType),Identifier(propertyName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(InvocationExpression(IdentifierName("GetValue"))
                            .AddArgumentListArguments(Argument(IdentifierName($"{propertyName}Property")))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(InvocationExpression(IdentifierName("SetValue"))
                            .AddArgumentListArguments(
                                Argument(IdentifierName($"{propertyName}Property")),
                                Argument(IdentifierName("value")))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
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