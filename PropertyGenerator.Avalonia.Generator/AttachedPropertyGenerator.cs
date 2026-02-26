using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using PropertyGenerator.Avalonia.Generator.Extensions;
using PropertyGenerator.Avalonia.Generator.Helpers;
using PropertyGenerator.Avalonia.Generator.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static PropertyGenerator.Avalonia.Generator.Helpers.PropertyGenerationHelper;

namespace PropertyGenerator.Avalonia.Generator;

[Generator]
public class AttachedPropertyGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "PropertyGenerator.Avalonia.GenerateAttachedPropertyAttribute`2";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var ownerTypes = context.ForAttributeWithMetadataNameAndOptions(
                AttributeFullName,
                predicate: (node, _) => node is ClassDeclarationSyntax classNode,
                transform: (ctx, _) => ctx.TargetSymbol)
            .Where(owner => owner is not null);

        var compilationAndOwnerTypes = context.CompilationProvider.Combine(ownerTypes.Collect());

        context.RegisterSourceOutput(compilationAndOwnerTypes, (spc, source) =>
        {
            var compilation = source.Left;
            var ctx = source.Right;

            if (ctx.IsDefaultOrEmpty)
                return;

            var distinctOwnerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var ownerType in ctx)
            {
                if (ownerType is INamedTypeSymbol s)
                {
                    distinctOwnerTypes.Add(s);
                }
            }

            foreach (var ownerType in distinctOwnerTypes)
            {
                // Check if the containing class is partial
                if (!DiagnosticHelper.CheckContainingTypeIsPartial(spc, ownerType, "GenerateAttachedPropertyAttribute"))
                    continue;

                var attributes = ownerType.GetAttributes().Where(IsAttachedPropertyAttribute).ToList();
                if (attributes.Count == 0)
                {
                    continue;
                }

                var candidates = new HashSet<AttachedPropertyCandidate>();
                foreach (var attribute in attributes)
                {
                    // Try to get the attached property name, which should be the first constructor argument of the attribute.
                    // If the name is not valid, skip this attribute.
                    if (!attribute.TryGetConstructorArgument<string>(0, out var name) ||
                        string.IsNullOrWhiteSpace(name) ||
                        !SyntaxFacts.IsValidIdentifier(name))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            GeneratorDiagnostics.InvalidAttachedPropertyName,
                            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            name ?? ""));
                        continue;
                    }

                    // Get type arguments
                    if (attribute.AttributeClass?.TypeArguments is not
                        [INamedTypeSymbol hostType, INamedTypeSymbol valueType])
                    {
                        continue;
                    }

                    candidates.Add(new AttachedPropertyCandidate(attribute, name, hostType, valueType));
                }

                var sourceCode = GenerateClassSource(compilation, ownerType, DistinctByName(spc, candidates));

                spc.AddSource(
                    $"{ownerType.ContainingNamespace.ToDisplayString()}.{ownerType.Name}.Attached.g.cs",
                    SourceText.From(sourceCode, Encoding.UTF8));
                continue;

                static IEnumerable<AttachedPropertyCandidate> DistinctByName(
                    SourceProductionContext spc,
                    IEnumerable<AttachedPropertyCandidate> source
                )
                {
                    var seenKeys = new HashSet<string>();
                    foreach (var element in source)
                    {
                        // Add returns true if the element was added (it was not already present)
                        if (seenKeys.Add(element.Name))
                        {
                            yield return element;
                        }
                        else
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                GeneratorDiagnostics.DuplicateAttachedPropertyName,
                                element.Attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                                element.Name, element.HostType.Name
                            ));
                        }
                    }
                }
            }
        });
    }

    private string GenerateClassSource(
        Compilation compilation,
        INamedTypeSymbol classSymbol,
        IEnumerable<AttachedPropertyCandidate> properties
    )
    {
        var compilationUnit = CompilationUnit()
            .AddMembers(GenerateContent(compilation, classSymbol, properties))
            .WithLeadingTrivia(
                ParseLeadingTrivia("// <auto-generated/>\r\n")
                    .Add(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)))
                    .Add(Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true))));

        return SyntaxTree(compilationUnit.NormalizeWhitespace()).ToString();
    }

    private static NamespaceDeclarationSyntax GenerateContent(
        Compilation compilation,
        INamedTypeSymbol ownerType,
        IEnumerable<AttachedPropertyCandidate> properties
    )
    {
        var namespaceName = ownerType.ContainingNamespace.ToDisplayString();
        var className = ownerType.Name;
        var doNotGenerateOnPropertyChangedAttributeSymbols =
            compilation.GetTypesByMetadataName("PropertyGenerator.Avalonia.DoNotGenerateOnPropertyChangedAttribute");
        var generateOnPropertyChanged =
            !ownerType.HasAttributeWithAnyType(doNotGenerateOnPropertyChangedAttributeSymbols)
            && !compilation.Assembly.HasAttributeWithAnyType(doNotGenerateOnPropertyChangedAttributeSymbols);

        var classDeclaration = ClassDeclaration(className).AddModifiers(Token(SyntaxKind.PartialKeyword));
        foreach (var prop in properties)
        {
            classDeclaration = classDeclaration.AddMembers(
                GenerateFieldDeclaration(compilation, prop),
                GenerateGetMethodDeclaration(prop),
                GenerateSetMethodDeclaration(prop),
                GenerateRegistrationMethod(compilation, ownerType, prop, generateOnPropertyChanged)
            );

            if (generateOnPropertyChanged)
            {
                classDeclaration = classDeclaration.AddMembers(GenerateChangedMethodDeclaration(prop),
                    GenerateChangedMethodWithOldValueDeclaration(prop),
                    GenerateChangedMethodWithArgsDeclaration(prop));
            }
        }

        return NamespaceDeclaration(ParseName(namespaceName)).AddMembers(classDeclaration);
    }

    private static FieldDeclarationSyntax GenerateFieldDeclaration(
        Compilation compilation,
        AttachedPropertyCandidate attachedProperty
    )
    {
        var propertyName = attachedProperty.Name;
        var attachedPropertyTypeName = compilation.GetTypeByMetadataName("Avalonia.AttachedProperty`1")!
            .Construct(attachedProperty.ValueType).GetFullyQualifiedNameWithNullabilityAnnotations();

        return FieldDeclaration(
                VariableDeclaration(IdentifierName(attachedPropertyTypeName))
                    .WithVariables(
                        SingletonSeparatedList(VariableDeclarator(Identifier($"{propertyName}Property"))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(
                                    IdentifierName($"Register{propertyName}Property")))))))
            .AddModifiers(
                Token(SyntaxKind.PublicKeyword),
                Token(SyntaxKind.StaticKeyword),
                Token(SyntaxKind.ReadOnlyKeyword))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())))
            .WithLeadingTrivia(ParseLeadingTrivia(
                $$"""
                  /// <summary>
                  /// The backing <see cref="global::Avalonia.AttachedProperty{TValue}"/> instance for the {{propertyName}} attached property.
                  /// </summary>

                  """));
    }

    private static MethodDeclarationSyntax GenerateGetMethodDeclaration(AttachedPropertyCandidate attachedProperty)
    {
        var propertyName = attachedProperty.Name;

        return MethodDeclaration(attachedProperty.ValueType.GetTypeSyntax(), Identifier($"Get{propertyName}"))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(Parameter(Identifier("host"))
                .WithType(attachedProperty.HostType.GetTypeSyntax()))
            .WithExpressionBody(
                ArrowExpressionClause(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("host"),
                                IdentifierName("GetValue")))
                        .AddArgumentListArguments(Argument(IdentifierName($"{propertyName}Property")))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }

    private static MethodDeclarationSyntax GenerateSetMethodDeclaration(AttachedPropertyCandidate attachedProperty)
    {
        var propertyName = attachedProperty.Name;

        return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier($"Set{propertyName}"))
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("host")).WithType(attachedProperty.HostType.GetTypeSyntax()),
                Parameter(Identifier("value")).WithType(attachedProperty.ValueType.GetTypeSyntax()))
            .WithExpressionBody(
                ArrowExpressionClause(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("host"),
                                IdentifierName("SetValue")))
                        .AddArgumentListArguments(
                            Argument(IdentifierName($"{propertyName}Property")),
                            Argument(IdentifierName("value")))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }

    private static MethodDeclarationSyntax GenerateChangedMethodDeclaration(AttachedPropertyCandidate attachedProperty)
    {
        return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier($"On{attachedProperty.Name}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("host")).WithType(attachedProperty.HostType.GetTypeSyntax()),
                Parameter(Identifier("e"))
                    .WithType(IdentifierName("global::Avalonia.AvaloniaPropertyChangedEventArgs")))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }

    private static MethodDeclarationSyntax GenerateChangedMethodWithOldValueDeclaration(
        AttachedPropertyCandidate attachedProperty
    )
    {
        return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier($"On{attachedProperty.Name}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("host")).WithType(attachedProperty.HostType.GetTypeSyntax()),
                Parameter(Identifier("newValue")).WithType(attachedProperty.ValueType.GetTypeSyntax()))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }

    private static MethodDeclarationSyntax GenerateChangedMethodWithArgsDeclaration(
        AttachedPropertyCandidate attachedProperty
    )
    {
        return MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier($"On{attachedProperty.Name}PropertyChanged"))
            .AddModifiers(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("host")).WithType(attachedProperty.HostType.GetTypeSyntax()),
                Parameter(Identifier("oldValue")).WithType(attachedProperty.ValueType.GetTypeSyntax()),
                Parameter(Identifier("newValue")).WithType(attachedProperty.ValueType.GetTypeSyntax()))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));
    }

    private static ParenthesizedLambdaExpressionSyntax GenerateChangedMethodForwarder(AttachedPropertyCandidate attachedProperty)
    {
        var valueTypeSyntax = attachedProperty.ValueType.GetTypeSyntax();
        var changeIdentifier = IdentifierName("change");
        var oldValueAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            changeIdentifier,
            IdentifierName("OldValue"));
        var newValueAccess = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            changeIdentifier,
            IdentifierName("NewValue"));

        var oldValueExpression = attachedProperty.ValueType.IsValueType &&
                                 attachedProperty.ValueType.NullableAnnotation != NullableAnnotation.Annotated
            ? CastExpression(valueTypeSyntax, SuppressNullableWarningExpression(oldValueAccess))
            : CastExpression(valueTypeSyntax, oldValueAccess);
        var newValueExpression = attachedProperty.ValueType.IsValueType &&
                                 attachedProperty.ValueType.NullableAnnotation != NullableAnnotation.Annotated
            ? CastExpression(valueTypeSyntax, SuppressNullableWarningExpression(newValueAccess))
            : CastExpression(valueTypeSyntax, newValueAccess);

        return ParenthesizedLambdaExpression(
                Block(
                    ExpressionStatement(
                        InvocationExpression(
                                IdentifierName($"On{attachedProperty.Name}PropertyChanged"))
                            .AddArgumentListArguments(
                                Argument(IdentifierName("host")),
                                Argument(changeIdentifier))),
                    ExpressionStatement(
                        InvocationExpression(
                                IdentifierName($"On{attachedProperty.Name}PropertyChanged"))
                            .AddArgumentListArguments(
                                Argument(IdentifierName("host")),
                                Argument(newValueExpression))),
                    ExpressionStatement(
                        InvocationExpression(
                                IdentifierName($"On{attachedProperty.Name}PropertyChanged"))
                            .AddArgumentListArguments(
                                Argument(IdentifierName("host")),
                                Argument(oldValueExpression),
                                Argument(newValueExpression)))))
            .WithParameterList(
                ParameterList(SeparatedList<ParameterSyntax>([
                    Parameter(Identifier("host")),
                    Parameter(Identifier("change"))
                ])));
    }

    private static MethodDeclarationSyntax GenerateRegistrationMethod(
        Compilation compilation,
        INamedTypeSymbol ownerType,
        AttachedPropertyCandidate attachedProperty,
        bool generateOnPropertyChanged
    )
    {
        var propertyName = attachedProperty.Name;
        var attachedPropertyTypeName = compilation.GetTypeByMetadataName("Avalonia.AttachedProperty`1")!
            .Construct(attachedProperty.ValueType).GetFullyQualifiedNameWithNullabilityAnnotations();
        var registerInvocation = GenerateRegisterAttachedInvocation(compilation, ownerType, attachedProperty);

        return MethodDeclaration(IdentifierName(attachedPropertyTypeName),
                Identifier($"Register{propertyName}Property"))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword))
            .WithBody(Block(GenerateBlockStatements()))
            .AddAttributeLists(AttributeList(SingletonSeparatedList(GeneratedCodeAttribute())));

        IEnumerable<StatementSyntax> GenerateBlockStatements()
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(IdentifierName(attachedPropertyTypeName))
                    .AddVariables(
                        VariableDeclarator(Identifier("property"))
                            .WithInitializer(EqualsValueClause(registerInvocation))));
            if (generateOnPropertyChanged)
                yield return ExpressionStatement(
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("global::Avalonia.AvaloniaObjectExtensions"),
                                GenericName(Identifier("AddClassHandler"))
                                    .AddTypeArgumentListArguments(attachedProperty.HostType
                                        .GetTypeSyntax())))
                        .AddArgumentListArguments(
                            Argument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("property"),
                                    IdentifierName("Changed"))),
                            Argument(GenerateChangedMethodForwarder(attachedProperty))));

            yield return ReturnStatement(IdentifierName("property"));
        }
    }

    private static InvocationExpressionSyntax GenerateRegisterAttachedInvocation(
        Compilation compilation,
        INamedTypeSymbol ownerType,
        AttachedPropertyCandidate attachedProperty
    )
    {
        var ownerTypeName = ownerType.GetFullyQualifiedName();
        var avaloniaPropertySymbolName = compilation.GetTypeByMetadataName("Avalonia.AvaloniaProperty")!
            .GetFullyQualifiedName();
        var defaultValue =
            DefaultValueHelper.GetDefaultValue(attachedProperty.Attribute, ownerType, attachedProperty.ValueType, CancellationToken.None);
        var propertyName = attachedProperty.Name;

        List<ArgumentSyntax> arguments =
        [
            Argument(
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(propertyName)))
                .WithNameColon(NameColon(IdentifierName("name")))
        ];

        if (defaultValue is not AvaloniaPropertyDefaultValue.UnsetValue)
        {
            if (defaultValue is AvaloniaPropertyDefaultValue.Callback callback)
            {
                arguments.Add(Argument(InvocationExpression(IdentifierName(callback.MethodName)))
                    .WithNameColon(NameColon(IdentifierName("defaultValue"))));
            }
            else
            {
                arguments.Add(Argument(ParseExpression(defaultValue.ToString()))
                    .WithNameColon(NameColon(IdentifierName("defaultValue"))));
            }
        }

        if (attachedProperty.Attribute.TryGetNamedArgument("Validate", out var validate) &&
            validate.Value is string validateMethod)
        {
            arguments.Add(Argument(IdentifierName(validateMethod))
                .WithNameColon(NameColon(IdentifierName("validate"))));
        }

        if (attachedProperty.Attribute.TryGetNamedArgument("Coerce", out var coerce) &&
            coerce.Value is string coerceMethod)
        {
            arguments.Add(Argument(IdentifierName(coerceMethod))
                .WithNameColon(NameColon(IdentifierName("coerce"))));
        }

        if (attachedProperty.Attribute.TryGetNamedArgument("Inherits", out var inherits))
        {
            arguments.Add(Argument(IdentifierName(inherits.Value!.ToString().ToLower()))
                .WithNameColon(NameColon(IdentifierName("inherits"))));
        }

        if (attachedProperty.Attribute.TryGetNamedArgument("DefaultBindingMode", out var defaultBindingMode))
        {
            arguments.Add(Argument(IdentifierName(TypedConstantInfo.Create(defaultBindingMode).ToString()))
                .WithNameColon(NameColon(IdentifierName("defaultBindingMode"))));
        }

        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(avaloniaPropertySymbolName),
                    GenericName(Identifier("RegisterAttached"))
                        .AddTypeArgumentListArguments(
                            IdentifierName(ownerTypeName),
                            attachedProperty.HostType.GetTypeSyntax(),
                            attachedProperty.ValueType.GetTypeSyntax())))
            .AddArgumentListArguments([.. arguments]);
    }

    private static bool IsAvaloniaObject(INamedTypeSymbol ownerType) =>
        ownerType.InheritsFromFullyQualifiedMetadataName("Avalonia.AvaloniaObject")
        || ownerType.HasFullyQualifiedMetadataName("Avalonia.AvaloniaObject");

    private static bool IsAttachedPropertyAttribute(AttributeData attributeData)
    {
        return attributeData.AttributeClass is
        {
            MetadataName: "GenerateAttachedPropertyAttribute`2",
            ContainingNamespace: { } containingNamespace
        } && containingNamespace.ToDisplayString() == "PropertyGenerator.Avalonia";
    }

    private sealed record AttachedPropertyCandidate(
        AttributeData Attribute,
        string Name,
        ITypeSymbol HostType,
        ITypeSymbol ValueType
    );
}
