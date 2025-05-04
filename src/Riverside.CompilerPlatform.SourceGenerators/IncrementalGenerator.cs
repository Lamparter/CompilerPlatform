using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Riverside.CompilerPlatform.
#if VISUALBASIC
VisualBasic.
#elif CSHARP
CSharp.
#endif
SourceGenerators;

/// <summary>
/// An incremental generator that simplifies source generation while providing full access to <see cref="IIncrementalGenerator"/> capabilities.
/// </summary>
/// <remarks>
/// This class abstracts over the Roslyn <see cref="IIncrementalGenerator"/> APIs, allowing derived classes
/// to simply provide generated code without dealing with pipeline complexity, while still
/// providing access to advanced features when needed.
/// </remarks>
public abstract class IncrementalGenerator : IIncrementalGenerator
{
    #region Properties
    /// <summary>
    /// The collection of generated source files.
    /// </summary>
    /// <remarks>
    /// Each syntax tree represents a source file to be added to the compilation.
    /// </remarks>
    public abstract List<SyntaxTree> Code { get; set; }

    /// <summary>
    /// Provides custom file names for generated source files.
    /// </summary>
    /// <remarks>
    /// Override this property to provide specific file names for your generated code.
    /// If null, default names (<c>GeneratedFile{index}.g.cs</c>) will be used.
    /// </remarks>
    protected virtual IList<string> FileNames => null;

    /// <summary>
    /// Gets or sets additional syntax trees to include in the source generation.
    /// </summary>
    /// <remarks>
    /// These syntax trees are processed alongside the main <see cref="Code"/> list, allowing generators
    /// to include auxiliary files without modifying the primary <see cref="Code"/> property.
    /// </remarks>
    protected virtual Dictionary<string, SyntaxTree> AdditionalSources => null;

    /// <summary>
    /// Gets a value indicating whether to suppress diagnostics from being reported during source generation.
    /// </summary>
    protected virtual bool SuppressDiagnostics => false;

    /// <summary>
    /// Gets a collection of additional texts available to the generator.
    /// </summary>
    /// <remarks>
    /// This provides access to additional files that were passed to the compilation.
    /// </remarks>
    protected Dictionary<string, string> AdditionalTexts { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the parser options to use when parsing source code.
    /// </summary>
    /// <remarks>
    /// Override this property to customize the parser options used for syntax tree creation.
    /// </remarks>
    protected virtual ParseOptions ParserOptions => null;

    /// <summary>
    /// Gets metadata references available to the generator.
    /// </summary>
    protected List<MetadataReference> MetadataReferences { get; } = new List<MetadataReference>();
    #endregion

    #region Events
    /// <summary>
    /// Called before source generation begins.
    /// </summary>
    /// <param name="compilation">The current compilation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected virtual void OnBeforeGeneration(Compilation compilation, CancellationToken cancellationToken) { }

    /// <summary>
    /// Called after source generation completes.
    /// </summary>
    /// <param name="context">The source production context.</param>
    protected virtual void OnAfterGeneration(SourceProductionContext context) { }

    /// <summary>
    /// Called when an additional file is discovered during initialization.
    /// </summary>
    /// <param name="filePath">The path of the additional file.</param>
    /// <param name="content">The content of the additional file.</param>
    protected virtual void OnAdditionalFileDiscovered(string filePath, string content) { }

    /// <summary>
    /// Called when metadata references are available.
    /// </summary>
    /// <param name="references">The metadata references collection.</param>
    protected virtual void OnMetadataReferencesAvailable(IReadOnlyList<MetadataReference> references) { }
    #endregion

    /// <summary>
    /// Customizes how a syntax tree is converted to source text.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to convert.</param>
    /// <returns>The source text representation.</returns>
    protected virtual string GetSourceText(SyntaxTree syntaxTree)
    {
        return syntaxTree?.ToString() ?? string.Empty;
    }

    private static string GetGeneratedFileName(string value)
    {
        return value + ".g." +
#if VisualBasic
        "vb"
#elif CSHARP
        "cs"
#endif
        ;
    }

    #region Wrappers
    /// <summary>
    /// Provides an opportunity for derived classes to customize the initialization pipeline.
    /// </summary>
    /// <remarks>
    /// Override this method to add custom pipeline stages or transformations.
    /// This is called before the standard pipeline is set up, allowing for advanced customization.
    /// </remarks>
    /// <param name="context">The initialization context.</param>
    protected virtual void CustomizeInitialization(IncrementalGeneratorInitializationContext context) { }

    /// <summary>
    /// Initializes the source generator.
    /// </summary>
    /// <param name="context">The initialization context provided by the Roslyn compiler.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Allow advanced customization first
        CustomizeInitialization(context);

        // Register for metadata references
        var metadataProvider = context.CompilationProvider.Select((compilation, _) => compilation.References.ToList());
        context.RegisterSourceOutput(metadataProvider, (ctx, references) =>
        {
            MetadataReferences.Clear();
            MetadataReferences.AddRange(references);
            OnMetadataReferencesAvailable(references);
        });

        // Register for additional files
        IncrementalValuesProvider<(string, string)> additionalFilesProvider = context.AdditionalTextsProvider
            .Select((text, cancellationToken) =>
            {
                string filePath = text.Path;
                string content = text.GetText(cancellationToken)?.ToString() ?? string.Empty;
                return (filePath, content);
            });

        // Process additional files
        context.RegisterSourceOutput(
            additionalFilesProvider,
            (sourceContext, fileInfo) =>
            {
                string filePath = fileInfo.Item1;
                string content = fileInfo.Item2;

                // Store in AdditionalTexts for later use
                AdditionalTexts[filePath] = content;

                // Notify derived classes
                OnAdditionalFileDiscovered(filePath, content);
            });

        // Set up analysis options provider
        IncrementalValueProvider<AnalyzerConfigOptionsProvider> optionsProvider = context.AnalyzerConfigOptionsProvider;

        // Register main source generation pipeline
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(optionsProvider),
            (sourceProductionContext, tuple) =>
            {
                Compilation compilation = tuple.Left;
                AnalyzerConfigOptionsProvider options = tuple.Right;

                // Ensure Code is not null
                if (Code == null)
                {
                    return;
                }

                try
                {
                    // Call the before generation hook
                    OnBeforeGeneration(compilation, sourceProductionContext.CancellationToken);

                    // Add each source file from the Code collection
                    for (int i = 0; i < Code.Count; i++)
                    {
                        if (Code[i] != null)
                        {
                            // Get the file name (custom or default)
                            string fileName = (FileNames != null && i < FileNames.Count)
                                ? FileNames[i]
                                : GetGeneratedFileName($"Generated_{i + 1}");

                            // Get the source text using the customizable method
                            string sourceText = GetSourceText(Code[i]);

                            // Add the source to the compilation
                            sourceProductionContext.AddSource(fileName, sourceText);
                        }
                    }

                    // Process additional sources if provided
                    if (AdditionalSources != null)
                    {
                        foreach (var source in AdditionalSources)
                        {
                            if (source.Value != null)
                            {
                                string sourceText = GetSourceText(source.Value);
                                sourceProductionContext.AddSource(source.Key, sourceText);
                            }
                        }
                    }

                    // Call the after generation hook
                    OnAfterGeneration(sourceProductionContext);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (!SuppressDiagnostics)
                    {
                        // Report exceptions that occur during generation
                        sourceProductionContext.ReportDiagnostic(
                            CreateDiagnostic(
                                "GEN001",
                                "Source Generation Error",
                                $"An error occurred during source generation: {ex.Message}",
                                DiagnosticSeverity.Error));
                    }
                }
            });
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Helper method for derived classes to create a diagnostic.
    /// </summary>
    /// <param name="id">The diagnostic ID.</param>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="location">The location associated with the diagnostic.</param>
    /// <returns>A diagnostic instance.</returns>
    protected static Diagnostic CreateDiagnostic(
        string id,
        string title,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Warning,
        Location location = null)
    {
        return Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                message,
                "SourceGeneration",
                severity,
                isEnabledByDefault: true),
            location ?? Location.None);
    }

    /// <summary>
    /// Helper method to create a syntax diagnostic with location information.
    /// </summary>
    /// <param name="node">The syntax node where the diagnostic occurred.</param>
    /// <param name="id">The diagnostic ID.</param>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>A diagnostic instance with location information.</returns>
    protected static Diagnostic CreateSyntaxDiagnostic(
        SyntaxNode node,
        string id,
        string title,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Warning)
    {
        Location location = node?.GetLocation() ?? Location.None;
        return CreateDiagnostic(id, title, message, severity, location);
    }

    /// <summary>
    /// Helper method to create a source text from a string.
    /// </summary>
    /// <param name="source">The source code as a string.</param>
    /// <returns>A SourceText instance.</returns>
    protected static SourceText CreateSourceText(string source)
    {
        return SourceText.From(source ?? string.Empty);
    }

    /// <summary>
    /// Helper method to create a transformation for incremental values.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="provider">The input provider.</param>
    /// <param name="transform">The transform function.</param>
    /// <returns>A provider for the transformed values.</returns>
    protected static IncrementalValuesProvider<TOut> Transform<TIn, TOut>(
        IncrementalValuesProvider<TIn> provider,
        Func<TIn, CancellationToken, TOut> transform)
    {
        return provider.Select(transform);
    }

    /// <summary>
    /// Helper method to combine multiple incremental value providers.
    /// </summary>
    /// <typeparam name="T1">The type of the first provider.</typeparam>
    /// <typeparam name="T2">The type of the second provider.</typeparam>
    /// <param name="provider1">The first provider.</param>
    /// <param name="provider2">The second provider.</param>
    /// <returns>A combined provider.</returns>
    protected static IncrementalValueProvider<(T1, T2)> Combine<T1, T2>(
        IncrementalValueProvider<T1> provider1,
        IncrementalValueProvider<T2> provider2)
    {
        return provider1.Combine(provider2);
    }
    #endregion
}
