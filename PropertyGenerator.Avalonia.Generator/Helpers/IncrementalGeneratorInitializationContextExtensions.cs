using System;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace PropertyGenerator.Avalonia.Generator.Helpers;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    /// <inheritdoc cref="SyntaxValueProvider.ForAttributeWithMetadataName"/>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataNameAndOptions<T>(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContextWithOptions, CancellationToken, T> transform)
    {
        // Invoke 'ForAttributeWithMetadataName' normally, but just return the context directly
        var syntaxContext = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate,
            static (context, token) => context);

        // Do the same for the analyzer config options
        var configOptions = context.AnalyzerConfigOptionsProvider.Select(static (provider, token) => provider.GlobalOptions);

        // Merge the two and invoke the provided transform on these two values. Neither value
        // is equatable, meaning the pipeline will always re-run until this point. This is
        // intentional: we don't want any symbols or other expensive objects to be kept alive
        // across incremental steps, especially if they could cause entire compilations to be
        // rooted, which would significantly increase memory use and introduce more GC pauses.
        // In this specific case, flowing non equatable values in a pipeline is therefore fine.
        return syntaxContext.Combine(configOptions).Select((input, token) => transform(new GeneratorAttributeSyntaxContextWithOptions(input.Left, input.Right), token));
    }
}
