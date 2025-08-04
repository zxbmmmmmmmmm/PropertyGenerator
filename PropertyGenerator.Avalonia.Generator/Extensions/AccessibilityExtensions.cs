using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace PropertyGenerator.Avalonia.Generator.Extensions;

public static class AccessibilityExtensions
{
    public static SyntaxToken[] GetAccessibilityModifiers(this Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => [Token(SyntaxKind.PublicKeyword)],
            Accessibility.Protected => [Token(SyntaxKind.ProtectedKeyword)],
            Accessibility.Internal => [Token(SyntaxKind.InternalKeyword)],
            Accessibility.ProtectedOrInternal => [Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ProtectedKeyword)],
            Accessibility.ProtectedAndInternal => [Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.InternalKeyword)],
            Accessibility.Private => [Token(SyntaxKind.PrivateKeyword)],
            _ => [],
        };
    }
}
