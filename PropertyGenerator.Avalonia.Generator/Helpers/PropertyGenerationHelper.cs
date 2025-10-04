using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PropertyGenerator.Avalonia.Generator.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using PropertyTuple = (Microsoft.CodeAnalysis.IPropertySymbol PropertySymbol, Microsoft.CodeAnalysis.SemanticModel SemanticModel);
namespace PropertyGenerator.Avalonia.Generator.Helpers;

internal class PropertyGenerationHelper
{
    public static PropertyDeclarationSyntax GeneratePropertyDeclaration(IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type;

        var propertyDeclaration =
            PropertyDeclaration(propertyType.GetTypeSyntax(), Identifier(propertyName))
                .AddModifiers([.. propertySymbol.DeclaredAccessibility.GetAccessibilityModifiers(), Token(SyntaxKind.PartialKeyword)]);
        if (propertySymbol.GetMethod is { } getMethod)
        {
            var accessorDeclarationSyntax = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(IdentifierName("GetValue"))
                    .AddArgumentListArguments(Argument(IdentifierName($"{propertyName}Property")))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            if (getMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                accessorDeclarationSyntax = accessorDeclarationSyntax.AddModifiers([.. getMethod.DeclaredAccessibility.GetAccessibilityModifiers()]);
            propertyDeclaration = propertyDeclaration.AddAccessorListAccessors(accessorDeclarationSyntax);
        }

        if (propertySymbol.SetMethod is { } setMethod)
        {
            var accessorDeclarationSyntax = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithExpressionBody(ArrowExpressionClause(InvocationExpression(IdentifierName("SetValue"))
                    .AddArgumentListArguments(
                        Argument(IdentifierName($"{propertyName}Property")),
                        Argument(IdentifierName("value")))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            if (setMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                accessorDeclarationSyntax = accessorDeclarationSyntax.AddModifiers([.. setMethod.DeclaredAccessibility.GetAccessibilityModifiers()]);
            propertyDeclaration = propertyDeclaration.AddAccessorListAccessors(accessorDeclarationSyntax);
        }
        return propertyDeclaration;
    }

    public static MemberDeclarationSyntax[] GenerateChangedMethod(IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;

        var methodDeclaration = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"On{propertyName}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("newValue"))
                    .WithType(propertySymbol.Type.GetTypeSyntax()))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(GeneratedCodeAttribute())));

        var methodDeclarationWithOldValue = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier($"On{propertyName}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(Parameter(Identifier("oldValue"))
                    .WithType(propertySymbol.Type.GetTypeSyntax()),
                Parameter(Identifier("newValue"))
                    .WithType(propertySymbol.Type.GetTypeSyntax()))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));

        var methodDeclarationWithArgs = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"On{propertyName}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(Parameter(Identifier("e"))
                .WithType(IdentifierName("global::Avalonia.AvaloniaPropertyChangedEventArgs")))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
        return [methodDeclaration, methodDeclarationWithOldValue, methodDeclarationWithArgs];
    }

    public static AttributeSyntax GeneratedCodeAttribute()
    {
        return Attribute(IdentifierName("global::System.CodeDom.Compiler.GeneratedCode"))
            .AddArgumentListArguments(AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("PropertyGenerator.Avalonia.Generator"))),
                AttributeArgument(LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    Literal("1.2.4.0"))));
    }

    public static PostfixUnaryExpressionSyntax SuppressNullableWarningExpression(ExpressionSyntax operand)
    {
        return PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, operand);
    }

    public static InvocationExpressionSyntax NameOfExpression(string name) => NameOfExpression(IdentifierName(name));


    public static InvocationExpressionSyntax NameOfExpression(ExpressionSyntax expressionSyntax) => InvocationExpression(IdentifierName("nameof"), ArgumentList().AddArguments(Argument(expressionSyntax)));
}
