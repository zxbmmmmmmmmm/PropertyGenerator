using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PropertyGenerator.Avalonia.Generator.Helpers;

public static class SyntaxNodeExtensions
{
    /// <inheritdoc cref="SyntaxNode.FirstAncestorOrSelf{TNode}(Func{TNode, bool}?, bool)"/>
    public static TNode? FirstAncestor<TNode>(this SyntaxNode node, Func<TNode, bool>? predicate = null, bool ascendOutOfTrivia = true)
        where TNode : SyntaxNode
    {
        // Traverse all parents and find the first one of the target type
        for (var parentNode = GetParent(node, ascendOutOfTrivia);
             parentNode is not null;
             parentNode = GetParent(parentNode, ascendOutOfTrivia))
        {
            if (parentNode is TNode candidateNode && predicate?.Invoke(candidateNode) != false)
            {
                return candidateNode;
            }
        }

        return null;

        // Helper method ported from 'SyntaxNode'
        static SyntaxNode? GetParent(SyntaxNode node, bool ascendOutOfTrivia)
        {
            var parent = node.Parent;

            if (parent is null && ascendOutOfTrivia)
            {
                if (node is IStructuredTriviaSyntax structuredTrivia)
                {
                    parent = structuredTrivia.ParentTrivia.Token.Parent;
                }
            }

            return parent;
        }
    }

    /// <summary>
    /// Checks whether a given property declaration has valid syntax.
    /// </summary>
    /// <param name="node">The input node to validate.</param>
    /// <returns>Whether <paramref name="node"/> is a valid property.</returns>
    internal static bool IsValidPropertyDeclaration(this SyntaxNode node)
    {
        // The node must be a property declaration with two accessors
        if (node is not PropertyDeclarationSyntax { AccessorList.Accessors: { Count: 2 } accessors, AttributeLists.Count: > 0 } property)
        {
            return false;
        }

        // The property must be partial (we'll check that it's a declaration from its symbol)
        if (!property.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return false;
        }

        // Static properties are not supported
        if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return false;
        }

        // The accessors must be a get and a set (with any accessibility)
        if (accessors[0].Kind() is not (SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration) ||
            accessors[1].Kind() is not (SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration))
        {
            return false;
        }

        return true;
    }
}
