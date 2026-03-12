using System;
using System.Collections.Generic;
using System.Linq;

namespace Riverside.CompilerPlatform.SourceGenerators;

// Not yet ready for production in any way

/// <summary>
/// Provides a base class for implementing incremental source generators that produce and manage collections of
/// generated source files.
/// </summary>
/// <remarks>
/// This generator type is simple and easy to use, just like its parent <see cref="IncrementalGenerator"/>.
/// The most significant difference between both generator types is that this one makes the source generation process <i>even simpler</i> by abstracting the way context and source generation is handled.
/// However, this results in a lot less freedom with what can be built with <see cref="SimpleGenerator"/> compared to <see cref="IncrementalGenerator"/>.
/// </remarks>
public abstract class SimpleGenerator : IncrementalGenerator
{
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
	/// Gets a collection of additional texts available to the generator.
	/// </summary>
	/// <remarks>
	/// This provides access to additional files that were passed to the compilation.
	/// </remarks>
	protected Dictionary<string, string> AdditionalTexts { get; } = [];

	/// <summary>
	/// Gets the parser options to use when parsing source code.
	/// </summary>
	/// <remarks>
	/// Override this property to customise the parser options used for syntax tree creation.
	/// </remarks>
	protected virtual ParseOptions ParserOptions => null;

	/// <summary>
	/// Called when an additional file is discovered during initialization.
	/// </summary>
	/// <param name="filePath">The path of the additional file.</param>
	/// <param name="content">The content of the additional file.</param>
	protected virtual void OnAdditionalFileDiscovered(string filePath, string content) { }

	internal override void Initialize(IncrementalGeneratorInitializationContext context, out bool cancelling)
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

				Context = new(compilation, options);

				// Ensure Code is not null
				if (Code == null)
				{
					return;
				}

				try
				{
					// Call the before generation hook
					OnBeforeGeneration(Context, sourceProductionContext.CancellationToken);

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
		cancelling = true;
	}
}
