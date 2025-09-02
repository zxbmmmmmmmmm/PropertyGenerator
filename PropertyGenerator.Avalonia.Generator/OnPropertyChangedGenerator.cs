using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PropertyGenerator.Avalonia.Generator.Extensions;
using PropertyGenerator.Avalonia.Generator.Helpers;
using PropertyGenerator.Avalonia.Generator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static PropertyGenerator.Avalonia.Generator.Helpers.PropertyGenerationHelper;
using PropertyTuple = (Microsoft.CodeAnalysis.IPropertySymbol PropertySymbol, Microsoft.CodeAnalysis.SemanticModel);
using GenerationTargetTuple = (Microsoft.CodeAnalysis.INamedTypeSymbol TargetClass,
    Microsoft.CodeAnalysis.IPropertySymbol PropertySymbol, Microsoft.CodeAnalysis.SemanticModel SemanticModel);

namespace PropertyGenerator.Avalonia.Generator;

[Generator]
public class OnPropertyChangedGenerator : IIncrementalGenerator
{
    private const string DirectAttributeFullName = "PropertyGenerator.Avalonia.GeneratedDirectPropertyAttribute";
    private const string StyledAttributeFullName = "PropertyGenerator.Avalonia.GeneratedStyledPropertyAttribute";
    private const string GenerateOnPropertyChangedAttributeFullName = "PropertyGenerator.Avalonia.GenerateOnPropertyChangedAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var directPropertySymbols = context.ForAttributeWithMetadataNameAndOptions(
                DirectAttributeFullName,
                predicate: (node, _) => node.IsValidPropertyDeclaration(),
                transform: (ctx, _) => (PropertySymbol: (IPropertySymbol) ctx.TargetSymbol, ctx.SemanticModel))
            .Where(ctx => ctx.PropertySymbol.ContainingType is not null);

        var styledPropertySymbols = context.ForAttributeWithMetadataNameAndOptions(
                StyledAttributeFullName,
                predicate: (node, _) => node.IsValidPropertyDeclaration(),
                transform: (ctx, _) => (PropertySymbol: (IPropertySymbol) ctx.TargetSymbol, ctx.SemanticModel))
            .Where(ctx => ctx.PropertySymbol.ContainingType is not null);

        var onPropertyChangedSymbols = context.ForAttributeWithMetadataNameAndOptions(
                GenerateOnPropertyChangedAttributeFullName,
                predicate: (node, _) => true,
                transform: (ctx, _) => (PropertySymbol: (INamedTypeSymbol) ctx.TargetSymbol, ctx.SemanticModel));

        var combinedProperties =
            directPropertySymbols.Collect()
            .Combine(styledPropertySymbols.Collect())
            .Combine(onPropertyChangedSymbols.Collect());

        var compilationAndProperties = context.CompilationProvider.Combine(combinedProperties);

        context.RegisterSourceOutput(compilationAndProperties, (spc, source) =>
        {
            var compilation = source.Left;
            var ((directProps, styledProps), onPropertyChangedAttributeTargets) = source.Right;
            if (directProps.IsDefaultOrEmpty && styledProps.IsDefaultOrEmpty && onPropertyChangedAttributeTargets.IsDefaultOrEmpty)
                return;

            var generationTargets = new List<GenerationTargetTuple>();

            // 直接和样式属性，其生成目标就是它们所在的类
            generationTargets.AddRange(directProps.Select(p => (p.PropertySymbol.ContainingType, p.PropertySymbol, p.SemanticModel)));
            generationTargets.AddRange(styledProps.Select(p => (p.PropertySymbol.ContainingType, p.PropertySymbol, p.SemanticModel)));

            // 获取attribute中对应的属性
            var generateOnPropertyChangedAttributeSymbol = compilation.GetTypeByMetadataName(GenerateOnPropertyChangedAttributeFullName);
            if (generateOnPropertyChangedAttributeSymbol is not null)
            {
                foreach (var (targetClass, semanticModel) in onPropertyChangedAttributeTargets)
                {
                    var attributes = targetClass.GetAttributes().Where(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, generateOnPropertyChangedAttributeSymbol));

                    foreach (var attributeData in attributes)
                    {
                        if (attributeData.ConstructorArguments.Length == 0)
                        {
                            continue;
                        }

                        var propertyNameTypedConstant = attributeData.ConstructorArguments[0];
                        if (propertyNameTypedConstant.Kind != TypedConstantKind.Primitive || propertyNameTypedConstant.Value is not string propertyName)
                        {
                            continue;
                        }

                        var propertySymbol = targetClass.GetMembers(propertyName)
                            .OfType<IPropertySymbol>()
                            .FirstOrDefault();

                        // 递归查找基类中名称一致的属性
                        if (propertySymbol is null)
                        {
                            var baseType = targetClass.BaseType;
                            while (baseType is not null)
                            {
                                propertySymbol = baseType.GetMembers(propertyName)
                                    .OfType<IPropertySymbol>()
                                    .FirstOrDefault();
                                if (propertySymbol is not null)
                                {
                                    break;
                                }
                                baseType = baseType.BaseType;
                            }
                        }

                        if (propertySymbol is not null)
                        {
                            // 关键改动：将属性与应用特性的类（targetClass）关联，而不是属性所在的类
                            generationTargets.Add((targetClass, propertySymbol, semanticModel));
                        }
                    }
                }
            }

            foreach (var group in generationTargets.GroupBy<GenerationTargetTuple, INamedTypeSymbol?>(p => p.TargetClass, SymbolEqualityComparer.Default))
            {
                var containingClass = group.Key;
                if (containingClass is null || !containingClass.InheritsFromFullyQualifiedMetadataName("Avalonia.AvaloniaObject"))
                {
                    continue;
                }

                var doNotGenerateOnPropertyChangedAttributeSymbols = compilation.GetTypesByMetadataName("PropertyGenerator.Avalonia.DoNotGenerateOnPropertyChangedAttribute");
                var generateOnPropertyChanged = !containingClass.HasAttributeWithAnyType(doNotGenerateOnPropertyChangedAttributeSymbols)
                                                  && !compilation.Assembly.HasAttributeWithAnyType(doNotGenerateOnPropertyChangedAttributeSymbols);

                if (!generateOnPropertyChanged)
                {
                    continue;
                }

                var propertyTuples = group.Select(g => (g.PropertySymbol, g.SemanticModel)).ToList();

                var currentDirectProps = propertyTuples.Where(p => p.PropertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == DirectAttributeFullName)).ToList();
                var currentStyledProps = propertyTuples.Where(p => p.PropertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == StyledAttributeFullName)).ToList();
                var currentOnChangedProps = propertyTuples.Except(currentDirectProps).Except(currentStyledProps).ToList();


                var sourceCode = GenerateClassSource(containingClass, currentStyledProps.Concat(currentOnChangedProps).ToList(), currentDirectProps, currentOnChangedProps);
                spc.AddSource($"{containingClass.ContainingNamespace.ToDisplayString()}.{containingClass.Name}.OnPropertyChanged.g.cs",
                    SourceText.From(sourceCode, Encoding.UTF8));
            }
        });
    }

    private static string GenerateClassSource(INamedTypeSymbol classSymbol, ICollection<PropertyTuple> styledProperties, ICollection<PropertyTuple> directProperties, ICollection<PropertyTuple> onChangedProperties)
    {
        var complicationUnit = CompilationUnit()
            .AddMembers(GenerateContent(classSymbol, styledProperties, directProperties, onChangedProperties))
            .WithLeadingTrivia(
                ParseLeadingTrivia("// <auto-generated/>\r\n")
                .Add(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)))
                .Add(Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true))));

        return SyntaxTree(complicationUnit.NormalizeWhitespace()).ToString();
    }

    private static NamespaceDeclarationSyntax GenerateContent(INamedTypeSymbol classSymbol, ICollection<PropertyTuple> styledProperties, ICollection<PropertyTuple> directProperties, ICollection<PropertyTuple> onChangedProperties)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var classDeclaration = ClassDeclaration(className)
            .AddModifiers(Token(SyntaxKind.PartialKeyword))
            .AddMembers(GenerateOnPropertyChangedOverride(styledProperties, directProperties))
            .WithLeadingTrivia(ParseLeadingTrivia($"/// <inheritdoc cref=\"{className}\"/>\r\n"));

        foreach (var (property, _) in onChangedProperties)
        {
            classDeclaration = classDeclaration.AddMembers(GenerateChangedMethod(property));
        }

        return NamespaceDeclaration(ParseName(namespaceName)).AddMembers(classDeclaration);
    }

    private static MemberDeclarationSyntax[] GenerateChangedMethod(IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.GetTypeSyntax();
        var avaloniaPropertyChangedEventArgs = IdentifierName("global::Avalonia.AvaloniaPropertyChangedEventArgs");

        return
        [
            MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), $"On{propertyName}PropertyChanged")
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddParameterListParameters(Parameter(Identifier("change")).WithType(avaloniaPropertyChangedEventArgs))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), $"On{propertyName}PropertyChanged")
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddParameterListParameters(Parameter(Identifier("newValue")).WithType(propertyType))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
            MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), $"On{propertyName}PropertyChanged")
                .AddModifiers(Token(SyntaxKind.PartialKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("oldValue")).WithType(propertyType),
                    Parameter(Identifier("newValue")).WithType(propertyType))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
        ];
    }

    private static MethodDeclarationSyntax GenerateOnPropertyChangedOverride(
        ICollection<PropertyTuple> styledProperties,
        ICollection<PropertyTuple> directProperties)
    {
        var changeIdentifier = IdentifierName("change");
        var propertyNameAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                changeIdentifier,
                IdentifierName("Property")),
            IdentifierName("Name"));

        var switchSections = styledProperties.Concat(directProperties)
            .Select(p =>
            {
                var propertyName = p.PropertySymbol.Name;
                var propertyType = p.PropertySymbol.Type;
                var typeSyntax = propertyType.GetTypeSyntax();

                var oldValueAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, changeIdentifier, IdentifierName("OldValue"));
                var newValueAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, changeIdentifier, IdentifierName("NewValue"));

                var oldValueArg = Argument(CastExpression(typeSyntax, oldValueAccess));
                var newValueArg = Argument(CastExpression(typeSyntax, newValueAccess));

                if (propertyType.IsValueType && propertyType.NullableAnnotation != NullableAnnotation.Annotated)
                {
                    oldValueArg = Argument(CastExpression(typeSyntax, SuppressNullableWarningExpression(oldValueAccess)));
                    newValueArg = Argument(CastExpression(typeSyntax, SuppressNullableWarningExpression(newValueAccess)));
                }

                return SwitchSection()
                    .AddLabels(
                        CaseSwitchLabel(
                            InvocationExpression(IdentifierName("nameof"))
                                .AddArgumentListArguments(Argument(IdentifierName(propertyName)))))
                    .AddStatements(
                        ExpressionStatement(
                            InvocationExpression(IdentifierName($"On{propertyName}PropertyChanged"))
                                .AddArgumentListArguments(Argument(changeIdentifier))),
                        ExpressionStatement(
                            InvocationExpression(IdentifierName($"On{propertyName}PropertyChanged"))
                                .AddArgumentListArguments(newValueArg)),
                        ExpressionStatement(
                            InvocationExpression(IdentifierName($"On{propertyName}PropertyChanged"))
                                .AddArgumentListArguments(oldValueArg, newValueArg)),
                        BreakStatement());
            })
            .ToArray();

        return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier("OnPropertyChanged"))
            .AddModifiers(
                Token(SyntaxKind.ProtectedKeyword),
                Token(SyntaxKind.OverrideKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("change"))
                    .WithType(IdentifierName("global::Avalonia.AvaloniaPropertyChangedEventArgs")))
            .WithBody(
                Block(
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                BaseExpression(),
                                IdentifierName("OnPropertyChanged")))
                            .AddArgumentListArguments(Argument(changeIdentifier))),
                    SwitchStatement(propertyNameAccess)
                        .AddSections(switchSections)))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }
}
